// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Agent.Sdk;
using Agent.Sdk.Knob;
using Agent.Sdk.Util;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(SelfUpdater))]
    public interface ISelfUpdater : IAgentService
    {
        Task<bool> SelfUpdate(AgentRefreshMessage updateMessage, IJobDispatcher jobDispatcher, bool restartInteractiveAgent, CancellationToken token);
    }

    public class SelfUpdater : AgentService, ISelfUpdater
    {
        private static string _packageType = BuildConstants.AgentPackage.PackageType;
        private static string _platform = BuildConstants.AgentPackage.PackageName;
        private static UpdaterKnobValueContext _knobContext = new UpdaterKnobValueContext();

        private PackageMetadata _targetPackage;
        private ITerminal _terminal;
        private IAgentServer _agentServer;
        private int _poolId;
        private int _agentId;
        private string _serverUrl;
        private VssCredentials _creds;
        private ILocationServer _locationServer;
        private bool _hashValidationDisabled;

        public override void Initialize(IHostContext hostContext)
        {
            ArgUtil.NotNull(hostContext, nameof(hostContext));
            base.Initialize(hostContext);

            _terminal = hostContext.GetService<ITerminal>();
            _agentServer = HostContext.GetService<IAgentServer>();
            var configStore = HostContext.GetService<IConfigurationStore>();
            var settings = configStore.GetSettings();
            _poolId = settings.PoolId;
            _agentId = settings.AgentId;
            _serverUrl = settings.ServerUrl;
            var credManager = HostContext.GetService<ICredentialManager>();
            _creds = credManager.LoadCredentials();
            _locationServer = HostContext.GetService<ILocationServer>();
            _hashValidationDisabled = AgentKnobs.DisableHashValidation.GetValue(_knobContext).AsBoolean();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA2000:Dispose objects before losing scope", MessageId = "invokeScript")]
        public async Task<bool> SelfUpdate(AgentRefreshMessage updateMessage, IJobDispatcher jobDispatcher, bool restartInteractiveAgent, CancellationToken token)
        {
            ArgUtil.NotNull(updateMessage, nameof(updateMessage));
            ArgUtil.NotNull(jobDispatcher, nameof(jobDispatcher));
            if (!await UpdateNeeded(updateMessage.TargetVersion, token))
            {
                Trace.Info($"Can't find available update package.");
                return false;
            }

            Trace.Info($"An update is available.");

            // Print console line that warn user not shutdown agent.
            await UpdateAgentUpdateStateAsync(StringUtil.Loc("UpdateInProgress"));
            await UpdateAgentUpdateStateAsync(StringUtil.Loc("DownloadAgent", _targetPackage.Version));

            await DownloadLatestAgent(token);
            Trace.Info($"Download latest agent and unzip into agent root.");

            // wait till all running job finish
            await UpdateAgentUpdateStateAsync(StringUtil.Loc("EnsureJobFinished"));

            await jobDispatcher.WaitAsync(token);
            Trace.Info($"All running jobs have exited.");

            // delete agent backup
            DeletePreviousVersionAgentBackup(token);
            Trace.Info($"Delete old version agent backup.");

            // generate update script from template
            await UpdateAgentUpdateStateAsync(StringUtil.Loc("GenerateAndRunUpdateScript"));

            string updateScript = GenerateUpdateScript(restartInteractiveAgent);
            Trace.Info($"Generate update script into: {updateScript}");

            // kick off update script
            Process invokeScript = new Process();
            if (PlatformUtil.RunningOnWindows)
            {
                invokeScript.StartInfo.FileName = WhichUtil.Which("cmd.exe", trace: Trace);
                invokeScript.StartInfo.Arguments = $"/c \"{updateScript}\"";
            }
            else
            {
                invokeScript.StartInfo.FileName = WhichUtil.Which("bash", trace: Trace);
                invokeScript.StartInfo.Arguments = $"\"{updateScript}\"";
            }
            invokeScript.Start();
            Trace.Info($"Update script start running");

            await UpdateAgentUpdateStateAsync(StringUtil.Loc("AgentExit"));

            return true;
        }

        private async Task<bool> UpdateNeeded(string targetVersion, CancellationToken token)
        {
            // when talk to old version tfs server, always prefer latest package.
            // old server won't send target version as part of update message.
            if (string.IsNullOrEmpty(targetVersion))
            {
                var packages = await _agentServer.GetPackagesAsync(_packageType, _platform, 1, token);
                if (packages == null || packages.Count == 0)
                {
                    Trace.Info($"There is no package for {_packageType} and {_platform}.");
                    return false;
                }

                _targetPackage = packages.FirstOrDefault();
            }
            else
            {
                _targetPackage = await _agentServer.GetPackageAsync(_packageType, _platform, targetVersion, token);
                if (_targetPackage == null)
                {
                    Trace.Info($"There is no package for {_packageType} and {_platform} with version {targetVersion}.");
                    return false;
                }
            }

            Trace.Info($"Version '{_targetPackage.Version}' of '{_targetPackage.Type}' package available in server.");
            PackageVersion serverVersion = new PackageVersion(_targetPackage.Version);
            Trace.Info($"Current running agent version is {BuildConstants.AgentPackage.Version}");
            PackageVersion agentVersion = new PackageVersion(BuildConstants.AgentPackage.Version);

            if (serverVersion.CompareTo(agentVersion) > 0)
            {
                return true;
            }

            if (AgentKnobs.DisableAgentDowngrade.GetValue(_knobContext).AsBoolean())
            {
                Trace.Info("Agent downgrade disabled, skipping update");
                return false;
            }

            // Always return true for newer agent versions unless they're exactly equal to enable auto rollback (this feature was introduced after 2.165.0)
            if (serverVersion.CompareTo(agentVersion) != 0)
            {
                _terminal.WriteLine(StringUtil.Loc("AgentDowngrade"));
                return true;
            }

            return false;
        }

        private bool HashValidation(string archiveFile)
        {
            if (_hashValidationDisabled)
            {
                Trace.Info($"Agent package hash validation disabled, so skipping it");
                return true;
            }

            // DownloadUrl for offline agent update is started from Url of ADO On-Premises
            // DownloadUrl for online agent update is started from Url of feed with agent packages
            bool isOfflineUpdate = _targetPackage.DownloadUrl.StartsWith(_serverUrl);
            if (isOfflineUpdate)
            {
                Trace.Info($"Skipping checksum validation for offline agent update");
                return true;
            }

            if (string.IsNullOrEmpty(_targetPackage.HashValue))
            {
                Trace.Warning($"Unable to perform the necessary checksum validation since the target package hash is missed");
                return false;
            }

            string expectedHash = _targetPackage.HashValue;
            string actualHash = IOUtil.GetFileHash(archiveFile);
            bool hashesMatch = StringUtil.AreHashesEqual(actualHash, expectedHash);

            if (hashesMatch)
            {
                Trace.Info($"Checksum validation succeeded");
                return true;
            }

            // A hash mismatch can occur in two cases:
            // 1) The archive is compromised
            // 2) The archive was not fully downloaded or was damaged during downloading
            // There is no way to determine the case so we just return false in both cases (without throwing an exception)
            Trace.Warning($"Checksum validation failed\n  Expected hash: '{expectedHash}'\n  Actual hash: '{actualHash}'");
            return false;
        }

        /// <summary>
        /// _work
        ///     \_update
        ///            \bin
        ///            \externals
        ///            \run.sh
        ///            \run.cmd
        ///            \package.zip //temp download .zip/.tar.gz
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task DownloadLatestAgent(CancellationToken token)
        {
            string latestAgentDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), Constants.Path.UpdateDirectory);
            IOUtil.DeleteDirectory(latestAgentDirectory, token);
            Directory.CreateDirectory(latestAgentDirectory);

            int agentSuffix = 1;
            string archiveFile = null;
            bool downloadSucceeded = false;
            bool validationSucceeded = false;

            try
            {
                // Download the agent, using multiple attempts in order to be resilient against any networking/CDN issues
                for (int attempt = 1; attempt <= Constants.AgentDownloadRetryMaxAttempts && !validationSucceeded; attempt++)
                {
                    // Generate an available package name, and do our best effort to clean up stale local zip files
                    while (true)
                    {
                        if (_targetPackage.Platform.StartsWith("win"))
                        {
                            archiveFile = Path.Combine(latestAgentDirectory, $"agent{agentSuffix}.zip");
                        }
                        else
                        {
                            archiveFile = Path.Combine(latestAgentDirectory, $"agent{agentSuffix}.tar.gz");
                        }

                        // The package name is generated, check if there is already a file with the same name and path
                        if (!string.IsNullOrEmpty(archiveFile) && File.Exists(archiveFile))
                        {
                            Trace.Verbose("Deleting latest agent package zip '{0}'", archiveFile);
                            try
                            {
                                // Such a file already exists, so try deleting it
                                IOUtil.DeleteFile(archiveFile);

                                // The file was successfully deleted, so we can use the generated package name
                                break;
                            }
                            catch (Exception ex)
                            {
                                // Couldn't delete the file for whatever reason, so generate another package name
                                Trace.Warning("Failed to delete agent package zip '{0}'. Exception: {1}", archiveFile, ex);
                                agentSuffix++;
                            }
                        }
                        else
                        {
                            // There is no a file with the same name and path, so we can use the generated package name
                            break;
                        }
                    }

                    // Allow a 15-minute package download timeout, which is good enough to update the agent from a 1 Mbit/s ADSL connection.
                    var timeoutSeconds = AgentKnobs.AgentDownloadTimeout.GetValue(_knobContext).AsInt();

                    Trace.Info($"Attempt {attempt}: save latest agent into {archiveFile}.");

                    using (var downloadTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    using (var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(downloadTimeout.Token, token))
                    {
                        try
                        {
                            Trace.Info($"Download agent: begin download");

                            //open zip stream in async mode
                            using (var handler = HostContext.CreateHttpClientHandler())
                            using (var httpClient = new HttpClient(handler))
                            using (var fs = new FileStream(archiveFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                            using (var result = await httpClient.GetStreamAsync(_targetPackage.DownloadUrl))
                            {
                                //81920 is the default used by System.IO.Stream.CopyTo and is under the large object heap threshold (85k).
                                await result.CopyToAsync(fs, 81920, downloadCts.Token);
                                await fs.FlushAsync(downloadCts.Token);
                            }

                            Trace.Info($"Download agent: finished download");

                            downloadSucceeded = true;
                            validationSucceeded = HashValidation(archiveFile);
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            Trace.Info($"Agent download has been canceled.");
                            throw;
                        }
                        catch (SocketException ex)
                        {
                            ExceptionsUtil.HandleSocketException(ex, _targetPackage.DownloadUrl, Trace.Warning);
                        }
                        catch (Exception ex)
                        {
                            if (downloadCts.Token.IsCancellationRequested)
                            {
                                Trace.Warning($"Agent download has timed out after {timeoutSeconds} seconds");
                            }

                            Trace.Warning($"Failed to get package '{archiveFile}' from '{_targetPackage.DownloadUrl}'. Exception {ex}");
                        }
                    }
                }

                if (!downloadSucceeded)
                {
                    throw new TaskCanceledException($"Agent package '{archiveFile}' failed after {Constants.AgentDownloadRetryMaxAttempts} download attempts.");
                }

                if (!validationSucceeded)
                {
                    throw new TaskCanceledException(@"Agent package checksum validation failed.
There are possible reasons why this happened:
  1) The agent package was compromised.
  2) The agent package was not fully downloaded or was corrupted during the download process.
You can skip checksum validation for the agent package by setting the environment variable DISABLE_HASH_VALIDATION=true");
                }

                // If we got this far, we know that we've successfully downloadeded the agent package
                if (archiveFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    ZipFile.ExtractToDirectory(archiveFile, latestAgentDirectory);
                }
                else if (archiveFile.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
                {
                    string tar = WhichUtil.Which("tar", trace: Trace);

                    if (string.IsNullOrEmpty(tar))
                    {
                        throw new NotSupportedException($"tar -xzf");
                    }

                    // tar -xzf
                    using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
                    {
                        processInvoker.OutputDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                        {
                            if (!string.IsNullOrEmpty(args.Data))
                            {
                                Trace.Info(args.Data);
                            }
                        });

                        processInvoker.ErrorDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                        {
                            if (!string.IsNullOrEmpty(args.Data))
                            {
                                Trace.Error(args.Data);
                            }
                        });

                        int exitCode = await processInvoker.ExecuteAsync(latestAgentDirectory, tar, $"-xzf \"{archiveFile}\"", null, token);
                        if (exitCode != 0)
                        {
                            throw new NotSupportedException($"Can't use 'tar -xzf' extract archive file: {archiveFile}. return code: {exitCode}.");
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException($"{archiveFile}");
                }

                Trace.Info($"Finished getting latest agent package at: {latestAgentDirectory}.");
            }
            finally
            {
                try
                {
                    // delete .zip file
                    if (!string.IsNullOrEmpty(archiveFile) && File.Exists(archiveFile))
                    {
                        Trace.Verbose("Deleting latest agent package zip: {0}", archiveFile);
                        IOUtil.DeleteFile(archiveFile);
                    }
                }
                catch (Exception ex)
                {
                    //it is not critical if we fail to delete the .zip file
                    Trace.Warning("Failed to delete agent package zip '{0}'. Exception: {1}", archiveFile, ex);
                }
            }

            if (!String.IsNullOrEmpty(AgentKnobs.DisableAuthenticodeValidation.GetValue(HostContext).AsString()))
            {
                Trace.Warning("Authenticode validation skipped for downloaded agent package since it is disabled currently by agent settings.");
            }
            else
            {
                if (PlatformUtil.RunningOnWindows)
                {
                    var isValid = this.VerifyAgentAuthenticode(latestAgentDirectory);
                    if (!isValid)
                    {
                        throw new Exception("Authenticode validation of agent assemblies failed.");
                    }
                    else
                    {
                        Trace.Info("Authenticode validation of agent assemblies passed successfully.");
                    }
                }
                else
                {
                    Trace.Info("Authenticode validation skipped since it's not supported on non-Windows platforms at the moment.");
                }
            }

            // copy latest agent into agent root folder
            // copy bin from _work/_update -> bin.version under root
            string binVersionDir = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"{Constants.Path.BinDirectory}.{_targetPackage.Version}");
            Directory.CreateDirectory(binVersionDir);
            Trace.Info($"Copy {Path.Combine(latestAgentDirectory, Constants.Path.BinDirectory)} to {binVersionDir}.");
            IOUtil.CopyDirectory(Path.Combine(latestAgentDirectory, Constants.Path.BinDirectory), binVersionDir, token);

            // copy externals from _work/_update -> externals.version under root
            string externalsVersionDir = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"{Constants.Path.ExternalsDirectory}.{_targetPackage.Version}");
            Directory.CreateDirectory(externalsVersionDir);
            Trace.Info($"Copy {Path.Combine(latestAgentDirectory, Constants.Path.ExternalsDirectory)} to {externalsVersionDir}.");
            IOUtil.CopyDirectory(Path.Combine(latestAgentDirectory, Constants.Path.ExternalsDirectory), externalsVersionDir, token);

            // copy and replace all .sh/.cmd files
            Trace.Info($"Copy any remaining .sh/.cmd files into agent root.");
            foreach (FileInfo file in new DirectoryInfo(latestAgentDirectory).GetFiles() ?? new FileInfo[0])
            {
                // Copy and replace the file.
                file.CopyTo(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), file.Name), true);
            }

            // for windows service back compat with old windows agent, we need make sure the servicehost.exe is still the old name
            // if the current bin folder has VsoAgentService.exe, then the new agent bin folder needs VsoAgentService.exe as well
            if (PlatformUtil.RunningOnWindows)
            {
                if (File.Exists(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Bin), "VsoAgentService.exe")))
                {
                    Trace.Info($"Make a copy of AgentService.exe, name it VsoAgentService.exe");
                    File.Copy(Path.Combine(binVersionDir, "AgentService.exe"), Path.Combine(binVersionDir, "VsoAgentService.exe"), true);
                    File.Copy(Path.Combine(binVersionDir, "AgentService.exe.config"), Path.Combine(binVersionDir, "VsoAgentService.exe.config"), true);

                    Trace.Info($"Make a copy of Agent.Listener.exe, name it VsoAgent.exe");
                    File.Copy(Path.Combine(binVersionDir, "Agent.Listener.exe"), Path.Combine(binVersionDir, "VsoAgent.exe"), true);
                    File.Copy(Path.Combine(binVersionDir, "Agent.Listener.dll"), Path.Combine(binVersionDir, "VsoAgent.dll"), true);

                    // in case of we remove all pdb file from agent package.
                    if (File.Exists(Path.Combine(binVersionDir, "AgentService.pdb")))
                    {
                        File.Copy(Path.Combine(binVersionDir, "AgentService.pdb"), Path.Combine(binVersionDir, "VsoAgentService.pdb"), true);
                    }

                    if (File.Exists(Path.Combine(binVersionDir, "Agent.Listener.pdb")))
                    {
                        File.Copy(Path.Combine(binVersionDir, "Agent.Listener.pdb"), Path.Combine(binVersionDir, "VsoAgent.pdb"), true);
                    }
                }
            }
        }

        private void DeletePreviousVersionAgentBackup(CancellationToken token)
        {
            // delete previous backup agent (back compat, can be remove after serval sprints)
            // bin.bak.2.99.0
            // externals.bak.2.99.0
            foreach (string existBackUp in Directory.GetDirectories(HostContext.GetDirectory(WellKnownDirectory.Root), "*.bak.*"))
            {
                Trace.Info($"Delete existing agent backup at {existBackUp}.");
                try
                {
                    IOUtil.DeleteDirectory(existBackUp, token);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Trace.Error(ex);
                    Trace.Info($"Catch exception during delete backup folder {existBackUp}, ignore this error try delete the backup folder on next auto-update.");
                }
            }

            // delete old bin.2.99.0 folder, only leave the current version and the latest download version
            var allBinDirs = Directory.GetDirectories(HostContext.GetDirectory(WellKnownDirectory.Root), "bin.*");
            if (allBinDirs.Length > 2)
            {
                // there are more than 2 bin.version folder.
                // delete older bin.version folders.
                foreach (var oldBinDir in allBinDirs)
                {
                    if (string.Equals(oldBinDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"bin"), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(oldBinDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"bin.{BuildConstants.AgentPackage.Version}"), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(oldBinDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"bin.{_targetPackage.Version}"), StringComparison.OrdinalIgnoreCase))
                    {
                        // skip for current agent version
                        continue;
                    }

                    Trace.Info($"Delete agent bin folder's backup at {oldBinDir}.");
                    try
                    {
                        IOUtil.DeleteDirectory(oldBinDir, token);
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        Trace.Error(ex);
                        Trace.Info($"Catch exception during delete backup folder {oldBinDir}, ignore this error try delete the backup folder on next auto-update.");
                    }
                }
            }

            // delete old externals.2.99.0 folder, only leave the current version and the latest download version
            var allExternalsDirs = Directory.GetDirectories(HostContext.GetDirectory(WellKnownDirectory.Root), "externals.*");
            if (allExternalsDirs.Length > 2)
            {
                // there are more than 2 externals.version folder.
                // delete older externals.version folders.
                foreach (var oldExternalDir in allExternalsDirs)
                {
                    if (string.Equals(oldExternalDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"externals"), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(oldExternalDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"externals.{BuildConstants.AgentPackage.Version}"), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(oldExternalDir, Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), $"externals.{_targetPackage.Version}"), StringComparison.OrdinalIgnoreCase))
                    {
                        // skip for current agent version
                        continue;
                    }

                    Trace.Info($"Delete agent externals folder's backup at {oldExternalDir}.");
                    try
                    {
                        IOUtil.DeleteDirectory(oldExternalDir, token);
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        Trace.Error(ex);
                        Trace.Info($"Catch exception during delete backup folder {oldExternalDir}, ignore this error try delete the backup folder on next auto-update.");
                    }
                }
            }
        }

        private string GenerateUpdateScript(bool restartInteractiveAgent)
        {
            int processId = Process.GetCurrentProcess().Id;
            string updateLog = Path.Combine(HostContext.GetDiagDirectory(), $"SelfUpdate-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")}.log");
            string agentRoot = HostContext.GetDirectory(WellKnownDirectory.Root);

            string templateName = "update.sh.template";
            if (PlatformUtil.RunningOnWindows)
            {
                templateName = "update.cmd.template";
            }

            string templatePath = Path.Combine(agentRoot, $"bin.{_targetPackage.Version}", templateName);
            string template = File.ReadAllText(templatePath);

            template = template.Replace("_PROCESS_ID_", processId.ToString());
            template = template.Replace("_AGENT_PROCESS_NAME_", $"Agent.Listener{IOUtil.ExeExtension}");
            template = template.Replace("_ROOT_FOLDER_", agentRoot);
            template = template.Replace("_EXIST_AGENT_VERSION_", BuildConstants.AgentPackage.Version);
            template = template.Replace("_DOWNLOAD_AGENT_VERSION_", _targetPackage.Version);
            template = template.Replace("_UPDATE_LOG_", updateLog);
            template = template.Replace("_RESTART_INTERACTIVE_AGENT_", restartInteractiveAgent ? "1" : "0");

            string scriptName = "_update.sh";
            if (PlatformUtil.RunningOnWindows)
            {
                scriptName = "_update.cmd";
            }

            string updateScript = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), scriptName);
            if (File.Exists(updateScript))
            {
                IOUtil.DeleteFile(updateScript);
            }

            File.WriteAllText(updateScript, template);
            return updateScript;
        }

        private async Task UpdateAgentUpdateStateAsync(string currentState)
        {
            _terminal.WriteLine(currentState);

            try
            {
                await _agentServer.UpdateAgentUpdateStateAsync(_poolId, _agentId, currentState);
            }
            catch (VssResourceNotFoundException)
            {
                // ignore VssResourceNotFoundException, this exception means the agent is configured against a old server that doesn't support report agent update detail.
                Trace.Info($"Catch VssResourceNotFoundException during report update state, ignore this error for backcompat.");
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                Trace.Info($"Catch exception during report update state, ignore this error and continue auto-update.");
            }
        }
        /// <summary>
        /// Verifies authenticode sign of agent assemblies
        /// </summary>
        /// <param name="agentFolderPath"></param>
        /// <returns></returns>
        private bool VerifyAgentAuthenticode(string agentFolderPath)
        {
            if (!Directory.Exists(agentFolderPath))
            {
                Trace.Error($"Agent folder {agentFolderPath} not found.");
                return false;
            }

            var agentDllFiles = Directory.GetFiles(agentFolderPath, "*.dll", SearchOption.AllDirectories);
            var agentExeFiles = Directory.GetFiles(agentFolderPath, "*.exe", SearchOption.AllDirectories);

            var agentAssemblies = agentDllFiles.Concat(agentExeFiles);
            Trace.Verbose(String.Format("Found {0} agent assemblies. Performing authenticode validation...", agentAssemblies.Count()));

            foreach (var assemblyFile in agentAssemblies)
            {
                FileInfo info = new FileInfo(assemblyFile);
                try
                {
                    InstallerVerifier.VerifyFileSignedByMicrosoft(info.FullName, this.Trace);
                }
                catch (Exception e)
                {
                    Trace.Error(e);
                    return false;
                }
            }

            return true;
        }
    }

    public class UpdaterKnobValueContext : IKnobValueContext
    {
        public string GetVariableValueOrDefault(string variableName)
        {
            return null;
        }

        public IScopedEnvironment GetScopedEnvironment()
        {
            return new SystemEnvironment();
        }
    }
}
