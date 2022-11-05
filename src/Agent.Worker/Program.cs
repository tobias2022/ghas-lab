// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Agent.Sdk;
using Agent.Sdk.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (PlatformUtil.UseLegacyHttpHandler)
            {
                AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
            }

            using (HostContext context = new HostContext(HostType.Worker))
            {
                return MainAsync(context, args).GetAwaiter().GetResult();
            }
        }

        private static async Task<int> MainAsync(IHostContext context, string[] args)
        {
            //ITerminal registers a CTRL-C handler, which keeps the Agent.Worker process running
            //and lets the Agent.Listener handle gracefully the exit.
            var term = context.GetService<ITerminal>();
            Tracing trace = context.GetTrace(nameof(Program));
            try
            {
                trace.Info($"Version: {BuildConstants.AgentPackage.Version}");
                trace.Info($"Commit: {BuildConstants.Source.CommitHash}");
                trace.Info($"Culture: {CultureInfo.CurrentCulture.Name}");
                trace.Info($"UI Culture: {CultureInfo.CurrentUICulture.Name}");
                context.WritePerfCounter("WorkerProcessStarted");

                // Validate args.
                ArgUtil.NotNull(args, nameof(args));
                ArgUtil.Equal(3, args.Length, nameof(args.Length));
                ArgUtil.NotNullOrEmpty(args[0], $"{nameof(args)}[0]");
                ArgUtil.Equal("spawnclient", args[0].ToLowerInvariant(), $"{nameof(args)}[0]");
                ArgUtil.NotNullOrEmpty(args[1], $"{nameof(args)}[1]");
                ArgUtil.NotNullOrEmpty(args[2], $"{nameof(args)}[2]");
                var worker = context.GetService<IWorker>();

                // Run the worker.
                return await worker.RunAsync(
                    pipeIn: args[1],
                    pipeOut: args[2]);
            }
            catch (AggregateException ex)
            {
                ExceptionsUtil.HandleAggregateException((AggregateException)ex, trace.Error);
            }
            catch (Exception ex)
            {
                // Populate any exception that cause worker failure back to agent.
                Console.WriteLine(ex.ToString());
                try
                {
                    trace.Error(ex);
                }
                catch (Exception e)
                {
                    // make sure we don't crash the app on trace error.
                    // since IOException will throw when we run out of disk space.
                    Console.WriteLine(e.ToString());
                }
            }

            return 1;
        }
    }
}
