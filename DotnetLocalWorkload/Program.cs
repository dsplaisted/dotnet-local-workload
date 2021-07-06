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

            var installCommand = new Command("install")
            {
                new Option<string>("--local-workload-root",
                    getDefaultValue: () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "dotnet-local-workloads"))
            };
            
            var workloadArgument = new Argument("workload")
            {
                Arity = ArgumentArity.OneOrMore,
            };
            installCommand.AddArgument(workloadArgument);
            rootCommand.Add(installCommand);

            var parseResult = rootCommand.Parse(args);

            string localWorkloadRoot = parseResult.ValueForOption<string>("--local-workload-root");
            if (!Directory.Exists(localWorkloadRoot))
            {
                Directory.CreateDirectory(localWorkloadRoot);
            }

            string dotnetPath = ResolveCommand("dotnet");
            string sdkVersion = ShellProcessRunner.Run(dotnetPath, "--version").GetOutput();

            if (!Version.TryParse(sdkVersion.Split('-')[0], out var sdkVersionParsed))
            {
                throw new ArgumentException($"'{nameof(sdkVersion)}' should be a version, but get {sdkVersion}");
            }

            //static int Last2DigitsTo0(int versionBuild)
            //{
            //    return (versionBuild / 100) * 100;
            //}

            //var sdkVersionBand =
            //    $"{sdkVersionParsed.Major}.{sdkVersionParsed.Minor}.{Last2DigitsTo0(sdkVersionParsed.Build)}";


            if (parseResult.CommandResult.Command == installCommand)
            {
                var workloadsToInstall = parseResult.ValueForArgument<List<string>>(workloadArgument);

                LocalWorkloadInstaller localWorkloadInstaller = new()
                {
                    WorkloadManifestRoot = Path.Combine(localWorkloadRoot, "sdk-manifests"),
                    WorkloadPackRoot = Path.Combine(localWorkloadRoot, "packs"),
                    WorkloadNuGetPackageFolder = Path.Combine(localWorkloadRoot, "NuGetPackages")
                };
                localWorkloadInstaller.DotnetRoot = Path.GetDirectoryName(dotnetPath);
                localWorkloadInstaller.SdkVersion = sdkVersion;

                var manifestRoots = Environment.GetEnvironmentVariable("DOTNETSDK_WORKLOAD_MANIFEST_ROOTS")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
                if (!manifestRoots.Contains(localWorkloadInstaller.WorkloadManifestRoot))
                {
                    Console.WriteLine("To use local workloads, set the DOTNETSDK_WORKLOAD_MANIFEST_ROOTS environment variable to " + localWorkloadInstaller.WorkloadManifestRoot);
                }

                var packRoots = Environment.GetEnvironmentVariable("DOTNETSDK_WORKLOAD_PACK_ROOTS")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
                if (!packRoots.Contains(localWorkloadInstaller.WorkloadPackRoot))
                {
                    Console.WriteLine("To use local workloads, set the DOTNETSDK_WORKLOAD_PACK_ROOTS environment variable to " + localWorkloadInstaller.WorkloadPackRoot);
                }

                localWorkloadInstaller.Install(workloadsToInstall);
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
