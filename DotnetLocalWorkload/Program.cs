using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace DotnetLocalWorkload
{
    class Program
    {
        //  DOTNETSDK_WORKLOAD_MANIFEST_ROOTS
        //  DOTNETSDK_WORKLOAD_PACK_ROOTS
        static int Main(string[] args)
        {
            var rootCommand = new RootCommand();

            var installCommand = new Command("install");
            
            var workloadArgument = new Argument("workload")
            {
                Arity = ArgumentArity.OneOrMore,
            };
            installCommand.AddArgument(workloadArgument);
            rootCommand.Add(installCommand);

            var parseResult = rootCommand.Parse(args);

            if (parseResult.CommandResult.Command == installCommand)
            {
                var argumentResult = parseResult.ValueForArgument<List<string>>(workloadArgument);
                Console.WriteLine("Workloads to install: " + string.Join(' ', argumentResult));

                LocalWorkloadInstaller localWorkloadInstaller = new();

                var manifestRoots = Environment.GetEnvironmentVariable("DOTNETSDK_WORKLOAD_MANIFEST_ROOTS");
                if (string.IsNullOrEmpty(manifestRoots))
                {
                    Console.Error.WriteLine("DOTNETSDK_WORKLOAD_MANIFEST_ROOTS not set.");
                    return 1;
                }

                localWorkloadInstaller.SdkManifestRoots = manifestRoots.Split(Path.PathSeparator);

                var packRoots = Environment.GetEnvironmentVariable("DOTNETSDK_WORKLOAD_PACK_ROOTS");
                if (string.IsNullOrEmpty(packRoots))
                {
                    Console.Error.WriteLine("DOTNETSDK_WORKLOAD_PACK_ROOTS not set.");
                    return 1;
                }

                localWorkloadInstaller.SdkPackRoots = packRoots.Split(Path.PathSeparator);

                string dotnetPath = ResolveCommand("dotnet");

                localWorkloadInstaller.DotnetRoot = Path.GetDirectoryName(dotnetPath);

                string sdkVersion = ShellProcessRunner.Run(dotnetPath, "--version").GetOutput();

                localWorkloadInstaller.SdkVersion = sdkVersion;

                localWorkloadInstaller.Install();


            }
            else
            {
                Console.WriteLine("Unknown command");
                return 1;
            }



            return 0;
        }

        private static string ResolveCommand(string command)
        {
            string[] extensions = new string[] { string.Empty };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                extensions = extensions
                    .Concat(Environment.GetEnvironmentVariable("PATHEXT").Split(Path.PathSeparator))
                    .ToArray();
            }

            var paths = Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator);
            string result = extensions.SelectMany(ext => paths.Select(p => Path.Combine(p, command + ext)))
                .FirstOrDefault(File.Exists);

            if (result == null)
            {
                throw new InvalidOperationException("Could not resolve path to " + command);
            }

            return result;
        }
    }
}
