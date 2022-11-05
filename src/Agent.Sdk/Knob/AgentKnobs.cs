// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Agent.Sdk.Knob
{
    public class AgentKnobs
    {
        // Containers
        public static readonly Knob PreferPowershellHandlerOnContainers = new Knob(
            nameof(PreferPowershellHandlerOnContainers),
            "If true, prefer using the PowerShell handler on Windows containers for tasks that provide both a Node and PowerShell handler version.",
            new RuntimeKnobSource("agent.preferPowerShellOnContainers"),
            new EnvironmentKnobSource("AGENT_PREFER_POWERSHELL_ON_CONTAINERS"),
            new BuiltInDefaultKnobSource("true"));

        public static readonly Knob SetupDockerGroup = new Knob(
            nameof(SetupDockerGroup),
            "If true, allows the user to run docker commands without sudo",
            new RuntimeKnobSource("VSTS_SETUP_DOCKERGROUP"),
            new EnvironmentKnobSource("VSTS_SETUP_DOCKERGROUP"),
            new BuiltInDefaultKnobSource("true"));

        public static readonly Knob AllowMountTasksReadonlyOnWindows = new Knob(
            nameof(AllowMountTasksReadonlyOnWindows),
            "If true, allows the user to mount 'tasks' volume read-only on Windows OS",
            new RuntimeKnobSource("VSTS_SETUP_ALLOW_MOUNT_TASKS_READONLY"),
            new EnvironmentKnobSource("VSTS_SETUP_ALLOW_MOUNT_TASKS_READONLY"),
            new BuiltInDefaultKnobSource("true"));

        public static readonly Knob SkipPostExeceutionIfTargetContainerStopped = new Knob(
            nameof(SkipPostExeceutionIfTargetContainerStopped),
            "If true, skips post-execution step for tasks in case the target container has been stopped",
            new RuntimeKnobSource("AGENT_SKIP_POST_EXECUTION_IF_CONTAINER_STOPPED"),
            new EnvironmentKnobSource("AGENT_SKIP_POST_EXECUTION_IF_CONTAINER_STOPPED"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob MTUValueForContainerJobs = new Knob(
            nameof(MTUValueForContainerJobs),
            "Allow to specify MTU value for networks used by container jobs (useful for docker-in-docker scenarios in k8s cluster).",
            new EnvironmentKnobSource("AGENT_DOCKER_MTU_VALUE"),
            new BuiltInDefaultKnobSource(string.Empty));

        public static readonly Knob DockerNetworkCreateDriver = new Knob(
            nameof(DockerNetworkCreateDriver),
            "Allow to specify which driver will be used when creating docker network",
            new RuntimeKnobSource("agent.DockerNetworkCreateDriver"),
            new EnvironmentKnobSource("AZP_AGENT_DOCKER_NETWORK_CREATE_DRIVER"),
            new BuiltInDefaultKnobSource(string.Empty));

        public static readonly Knob DockerAdditionalNetworkOptions = new Knob(
            nameof(DockerNetworkCreateDriver),
            "Allow to specify additional command line options to 'docker network' command when creating network for new containers",
            new RuntimeKnobSource("agent.DockerAdditionalNetworkOptions"),
            new EnvironmentKnobSource("AZP_AGENT_DOCKER_ADDITIONAL_NETWORK_OPTIONS"),
            new BuiltInDefaultKnobSource(string.Empty));

        public static readonly Knob UseHostGroupId = new Knob(
            nameof(UseHostGroupId),
            "If true, use the same group ID (GID) as the user on the host on which the agent is running",
            new RuntimeKnobSource("agent.UseHostGroupId"),
            new EnvironmentKnobSource("AZP_AGENT_USE_HOST_GROUP_ID"),
            new BuiltInDefaultKnobSource("true"));

        // Directory structure
        public static readonly Knob AgentToolsDirectory = new Knob(
            nameof(AgentToolsDirectory),
            "The location to look for/create the agents tool cache",
            new EnvironmentKnobSource("AGENT_TOOLSDIRECTORY"),
            new EnvironmentKnobSource("agent.ToolsDirectory"),
            new BuiltInDefaultKnobSource(string.Empty));

        public static readonly Knob OverwriteTemp = new Knob(
            nameof(OverwriteTemp),
            "If true, the system temp variable will be overriden to point to the agent's temp directory.",
            new RuntimeKnobSource("VSTS_OVERWRITE_TEMP"),
            new EnvironmentKnobSource("VSTS_OVERWRITE_TEMP"),
            new BuiltInDefaultKnobSource("false"));

        // Tool configuration
        public static readonly Knob DisableFetchByCommit = new Knob(
            nameof(DisableFetchByCommit),
            "If true and server supports it, fetch the target branch by commit. Otherwise, fetch all branches and pull request ref to get the target branch.",
            new RuntimeKnobSource("VSTS.DisableFetchByCommit"),
            new EnvironmentKnobSource("VSTS_DISABLEFETCHBYCOMMIT"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob DisableFetchPruneTags = new Knob(
            nameof(DisableFetchPruneTags),
            "If true, disable --prune-tags in the fetches.",
            new RuntimeKnobSource("VSTS.DisableFetchPruneTags"),
            new EnvironmentKnobSource("VSTS_DISABLEFETCHPRUNETAGS"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob PreferGitFromPath = new Knob(
            nameof(PreferGitFromPath),
            "Determines which Git we will use on Windows. By default, we prefer the built-in portable git in the agent's externals folder, setting this to true makes the agent find git.exe from %PATH% if possible.",
            new RuntimeKnobSource("system.prefergitfrompath"),
            new EnvironmentKnobSource("system.prefergitfrompath"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob DisableGitPrompt = new Knob(
            nameof(DisableGitPrompt),
            "If true, git will not prompt on the terminal (e.g., when asking for HTTP authentication).",
            new RuntimeKnobSource("VSTS_DISABLE_GIT_PROMPT"),
            new EnvironmentKnobSource("VSTS_DISABLE_GIT_PROMPT"),
            new BuiltInDefaultKnobSource("true"));

        public static readonly Knob GitUseSecureParameterPassing = new Knob(
            nameof(GitUseSecureParameterPassing),
            "If true, don't pass auth token in git parameters",
            new RuntimeKnobSource("agent.GitUseSecureParameterPassing"),
            new EnvironmentKnobSource("AGENT_GIT_USE_SECURE_PARAMETER_PASSING"),
            new BuiltInDefaultKnobSource("true"));

        public static readonly Knob TfVCUseSecureParameterPassing = new Knob(
            nameof(TfVCUseSecureParameterPassing),
            "If true, don't pass auth token in TFVC parameters",
            new RuntimeKnobSource("agent.TfVCUseSecureParameterPassing"),
            new EnvironmentKnobSource("AGENT_TFVC_USE_SECURE_PARAMETER_PASSING"),
            new BuiltInDefaultKnobSource("true"));

        public const string QuietCheckoutRuntimeVarName = "agent.source.checkout.quiet";
        public const string QuietCheckoutEnvVarName = "AGENT_SOURCE_CHECKOUT_QUIET";

        public static readonly Knob QuietCheckout = new Knob(
            nameof(QuietCheckout),
            "Aggressively reduce what gets logged to the console when checking out source.",
            new RuntimeKnobSource(QuietCheckoutRuntimeVarName),
            new EnvironmentKnobSource(QuietCheckoutEnvVarName),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob UseNode10 = new Knob(
            nameof(UseNode10),
            "Forces the agent to use Node 10 handler for all Node-based tasks",
            new RuntimeKnobSource("AGENT_USE_NODE10"),
            new EnvironmentKnobSource("AGENT_USE_NODE10"),
            new BuiltInDefaultKnobSource("false"));

        // Agent logging
        public static readonly Knob AgentPerflog = new Knob(
            nameof(AgentPerflog),
            "If set, writes a perf counter trace for the agent. Writes to the location set in this variable.",
            new EnvironmentKnobSource("VSTS_AGENT_PERFLOG"),
            new BuiltInDefaultKnobSource(string.Empty));

        public static readonly Knob TraceVerbose = new Knob(
            nameof(TraceVerbose),
            "If set to anything, trace level will be verbose",
            new EnvironmentKnobSource("VSTSAGENT_TRACE"),
            new BuiltInDefaultKnobSource(string.Empty));

        public static readonly Knob DumpJobEventLogs = new Knob(
            nameof(DumpJobEventLogs),
            "If true, dump event viewer logs",
            new RuntimeKnobSource("VSTSAGENT_DUMP_JOB_EVENT_LOGS"),
            new EnvironmentKnobSource("VSTSAGENT_DUMP_JOB_EVENT_LOGS"),
            new BuiltInDefaultKnobSource("false"));

        // Diag logging
        public static readonly Knob AgentDiagLogPath = new Knob(
            nameof(AgentDiagLogPath),
            "If set to anything, the folder containing the agent diag log will be created here.",
            new EnvironmentKnobSource("AGENT_DIAGLOGPATH"),
            new BuiltInDefaultKnobSource(string.Empty));

        public static readonly Knob WorkerDiagLogPath = new Knob(
            nameof(WorkerDiagLogPath),
            "If set to anything, the folder containing the agent worker diag log will be created here.",
            new EnvironmentKnobSource("WORKER_DIAGLOGPATH"),
            new BuiltInDefaultKnobSource(string.Empty));

        // Timeouts
        public static readonly Knob AgentChannelTimeout = new Knob(
            nameof(AgentChannelTimeout),
            "Timeout for channel communication between agent listener and worker processes.",
            new EnvironmentKnobSource("VSTS_AGENT_CHANNEL_TIMEOUT"),
            new BuiltInDefaultKnobSource("30"));

        public static readonly Knob AgentDownloadTimeout = new Knob(
            nameof(AgentDownloadTimeout),
            "Amount of time in seconds to wait for the agent to download a new version when updating",
            new EnvironmentKnobSource("AZP_AGENT_DOWNLOAD_TIMEOUT"),
            new BuiltInDefaultKnobSource("1500")); // 25*60

        public static readonly Knob TaskDownloadTimeout = new Knob(
            nameof(TaskDownloadTimeout),
            "Amount of time in seconds to wait for the agent to download a task when starting a job",
            new EnvironmentKnobSource("VSTS_TASK_DOWNLOAD_TIMEOUT"),
            new BuiltInDefaultKnobSource("1200")); // 20*60

        public static readonly Knob TaskDownloadRetryLimit = new Knob(
            nameof(TaskDownloadRetryLimit),
            "Attempts to download a task when starting a job",
            new EnvironmentKnobSource("VSTS_TASK_DOWNLOAD_RETRY_LIMIT"),
            new BuiltInDefaultKnobSource("3"));

        // HTTP
        public const string LegacyHttpVariableName = "AZP_AGENT_USE_LEGACY_HTTP";
        public static readonly Knob UseLegacyHttpHandler = new DeprecatedKnob(
            nameof(UseLegacyHttpHandler),
            "Use the libcurl-based HTTP handler rather than .NET's native HTTP handler, as we did on .NET Core 2.1",
            "Legacy http handler will be removed in one of the next agent releases with migration to .Net Core 6. We are highly recommend to not use it.",
            new EnvironmentKnobSource(LegacyHttpVariableName),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob HttpRetryCount = new Knob(
            nameof(HttpRetryCount),
            "Number of times to retry Http requests",
            new EnvironmentKnobSource("VSTS_HTTP_RETRY"),
            new BuiltInDefaultKnobSource("3"));

        public static readonly Knob HttpTimeout = new Knob(
            nameof(HttpTimeout),
            "Timeout for Http requests",
            new EnvironmentKnobSource("VSTS_HTTP_TIMEOUT"),
            new BuiltInDefaultKnobSource("100"));

        public static readonly Knob HttpTrace = new Knob(
            nameof(HttpTrace),
            "Enable http trace if true",
            new EnvironmentKnobSource("VSTS_AGENT_HTTPTRACE"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob NoProxy = new Knob(
            nameof(NoProxy),
            "Proxy bypass list if one exists. Should be comma seperated",
            new EnvironmentKnobSource("no_proxy"),
            new BuiltInDefaultKnobSource(string.Empty));

        public static readonly Knob ProxyAddress = new Knob(
            nameof(ProxyAddress),
            "Proxy server address if one exists",
            new EnvironmentKnobSource("VSTS_HTTP_PROXY"),
            new EnvironmentKnobSource("http_proxy"),
            new BuiltInDefaultKnobSource(string.Empty));

        public static readonly Knob ProxyPassword = new SecretKnob(
            nameof(ProxyPassword),
            "Proxy password if one exists",
            new EnvironmentKnobSource("VSTS_HTTP_PROXY_PASSWORD"),
            new BuiltInDefaultKnobSource(string.Empty));

        public static readonly Knob ProxyUsername = new SecretKnob(
            nameof(ProxyUsername),
            "Proxy username if one exists",
            new EnvironmentKnobSource("VSTS_HTTP_PROXY_USERNAME"),
            new BuiltInDefaultKnobSource(string.Empty));

        // Secrets masking
        public static readonly Knob AllowUnsafeMultilineSecret = new Knob(
            nameof(AllowUnsafeMultilineSecret),
            "WARNING: enabling this may allow secrets to leak. Allows multi-line secrets to be set. Unsafe because it is possible for log lines to get dropped in agent failure cases, causing the secret to not get correctly masked. We recommend leaving this option off.",
            new RuntimeKnobSource("SYSTEM_UNSAFEALLOWMULTILINESECRET"),
            new EnvironmentKnobSource("SYSTEM_UNSAFEALLOWMULTILINESECRET"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob MaskUsingCredScanRegexes = new Knob(
            nameof(MaskUsingCredScanRegexes),
            "Use the CredScan regexes for masking secrets. CredScan is an internal tool developed at Microsoft to keep passwords and authentication keys from being checked in. This defaults to disabled, as there are performance problems with some task outputs.",
            new EnvironmentKnobSource("AZP_USE_CREDSCAN_REGEXES"),
            new BuiltInDefaultKnobSource("false"));

        // Misc
        public static readonly Knob DisableAgentDowngrade = new Knob(
            nameof(DisableAgentDowngrade),
            "Disable agent downgrades. Upgrades will still be allowed.",
            new EnvironmentKnobSource("AZP_AGENT_DOWNGRADE_DISABLED"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob PermissionsCheckFailsafe = new Knob(
            nameof(PermissionsCheckFailsafe),
            "Maximum depth of file permitted in directory hierarchy when checking permissions. Check to avoid accidentally entering infinite loops.",
            new EnvironmentKnobSource("AGENT_TEST_VALIDATE_EXECUTE_PERMISSIONS_FAILSAFE"),
            new BuiltInDefaultKnobSource("100"));

        public static readonly Knob DisableInputTrimming = new Knob(
            nameof(DisableInputTrimming),
            "By default, the agent trims whitespace and new line characters from all task inputs. Setting this to true disables this behavior.",
            new EnvironmentKnobSource("DISABLE_INPUT_TRIMMING"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob DecodePercents = new Knob(
            nameof(DecodePercents),
            "By default, the agent does not decodes %AZP25 as % which may be needed to allow users to work around reserved values. Setting this to true enables this behavior.",
            new RuntimeKnobSource("DECODE_PERCENTS"),
            new EnvironmentKnobSource("DECODE_PERCENTS"),
            new BuiltInDefaultKnobSource("true"));

        public static readonly Knob AllowTfvcUnshelveErrors = new Knob(
            nameof(AllowTfvcUnshelveErrors),
            "By default, the TFVC unshelve command does not throw errors e.g. when there's no mapping for one or more files shelved. Setting this to true enables this behavior.",
            new RuntimeKnobSource("ALLOW_TFVC_UNSHELVE_ERRORS"),
            new EnvironmentKnobSource("ALLOW_TFVC_UNSHELVE_ERRORS"),
            new BuiltInDefaultKnobSource("false"));

        // Set DISABLE_JAVA_CAPABILITY_HIGHER_THAN_9 variable with any value
        // to disable recognition of Java higher than 9
        public static readonly Knob DisableRecognitionOfJDKHigherThen9 = new Knob(
            nameof(DisableRecognitionOfJDKHigherThen9),
            "Recognize JDK and JRE >= 9 installed on the machine as agent capability. Setting any value to DISABLE_JAVA_CAPABILITY_HIGHER_THAN_9 is disabling this behavior",
            new EnvironmentKnobSource("DISABLE_JAVA_CAPABILITY_HIGHER_THAN_9"),
            new BuiltInDefaultKnobSource(string.Empty));

        // TODO: Added 5/27/21. Please remove within a month or two
        public static readonly Knob DisableBuildArtifactsToBlob = new Knob(
            nameof(DisableBuildArtifactsToBlob),
            "By default, the agent will upload build artifacts to Blobstore. Setting this to true will disable that integration. This variable is temporary and will be removed.",
            new RuntimeKnobSource("DISABLE_BUILD_ARTIFACTS_TO_BLOB"),
            new EnvironmentKnobSource("DISABLE_BUILD_ARTIFACTS_TO_BLOB"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob EnableIncompatibleBuildArtifactsPathResolution = new Knob(
            nameof(EnableIncompatibleBuildArtifactsPathResolution),
            "Return DownloadBuildArtifactsV1 target path resolution behavior back to how it was originally implemented. This breaks back compatibility with DownloadBuildArtifactsV0.",
            new RuntimeKnobSource("EnableIncompatibleBuildArtifactsPathResolution"),
            new EnvironmentKnobSource("EnableIncompatibleBuildArtifactsPathResolution"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob DisableAuthenticodeValidation = new Knob(
               nameof(DisableAuthenticodeValidation),
               "Disables authenticode validation for agent package during self update. Set this to any non-empty value to disable.",
               new EnvironmentKnobSource("DISABLE_AUTHENTICODE_VALIDATION"),
               new BuiltInDefaultKnobSource(string.Empty));

        public static readonly Knob DisableHashValidation = new Knob(
            nameof(DisableHashValidation),
            "If true, the agent will skip package hash validation during self-updating.",
            new EnvironmentKnobSource("DISABLE_HASH_VALIDATION"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob EnableVSPreReleaseVersions = new Knob(
            nameof(EnableVSPreReleaseVersions),
            "If true, the agent will include to seach VisualStudio prerelease versions to capabilities.",
            new EnvironmentKnobSource("ENABLE_VS_PRERELEASE_VERSIONS"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob DisableOverrideTfvcBuildDirectory = new Knob(
            nameof(DisableOverrideTfvcBuildDirectory),
            "Disables override of Tfvc build directory name by agentId on hosted agents (one tfvc repo used).",
            new RuntimeKnobSource("DISABLE_OVERRIDE_TFVC_BUILD_DIRECTORY"),
            new EnvironmentKnobSource("DISABLE_OVERRIDE_TFVC_BUILD_DIRECTORY"),
            new BuiltInDefaultKnobSource("false"));

        /// <remarks>We need to remove this knob - once Node 6 handler is dropped</remarks>
        public static readonly Knob DisableNode6DeprecationWarning = new Knob(
            nameof(DisableNode6DeprecationWarning),
            "Disables Node 6 deprecation warnings.",
            new RuntimeKnobSource("DISABLE_NODE6_DEPRECATION_WARNING"),
            new EnvironmentKnobSource("DISABLE_NODE6_DEPRECATION_WARNING"),
            new BuiltInDefaultKnobSource("true"));

        public static readonly Knob DisableTeePluginRemoval = new Knob(
            nameof(DisableTeePluginRemoval),
            "Disables removing TEE plugin after using it during checkout.",
            new RuntimeKnobSource("DISABLE_TEE_PLUGIN_REMOVAL"),
            new EnvironmentKnobSource("DISABLE_TEE_PLUGIN_REMOVAL"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob TeePluginDownloadRetryCount = new Knob(
            nameof(TeePluginDownloadRetryCount),
            "Number of times to retry downloading TEE plugin",
            new RuntimeKnobSource("TEE_PLUGIN_DOWNLOAD_RETRY_COUNT"),
            new EnvironmentKnobSource("TEE_PLUGIN_DOWNLOAD_RETRY_COUNT"),
            new BuiltInDefaultKnobSource("3"));

        public static readonly Knob DumpPackagesVerificationResult = new Knob(
            nameof(DumpPackagesVerificationResult),
            "If true, dumps info about invalid MD5 sums of installed packages",
            new RuntimeKnobSource("VSTSAGENT_DUMP_PACKAGES_VERIFICATION_RESULTS"),
            new EnvironmentKnobSource("VSTSAGENT_DUMP_PACKAGES_VERIFICATION_RESULTS"),
            new BuiltInDefaultKnobSource("false"));

        public static readonly Knob MaskedSecretMinLength = new Knob(
            nameof(MaskedSecretMinLength),
            "Minimum lenght of secret which will be masked in logs",
            new RuntimeKnobSource("AGENT_MIN_SECRET_LENGTH"),
            new EnvironmentKnobSource("AGENT_MIN_SECRET_LENGTH"),
            new BuiltInDefaultKnobSource("0"));
    }
}
