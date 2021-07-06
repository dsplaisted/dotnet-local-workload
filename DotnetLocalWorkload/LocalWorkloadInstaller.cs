using Microsoft.DotNet.Cli.NuGetPackageDownloader;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;

namespace DotnetLocalWorkload
{
    class LocalWorkloadInstaller
    {
        public string DotnetRoot { get; set; }
        public string SdkVersion { get; set; }

        public string WorkloadManifestRoot { get; set; }
        public string WorkloadPackRoot { get; set; }
        public string WorkloadNuGetPackageFolder { get; set; }

        public void Install(List<string> workloads)
        {
            Console.WriteLine($"dotnet root: {DotnetRoot}");
            Console.WriteLine($"SDK version: {SdkVersion}");
            Console.WriteLine($"Manifest root: {WorkloadManifestRoot}");
            Console.WriteLine($"Pack root: {WorkloadPackRoot}");
            Console.WriteLine($"Workload NuGet packages: {WorkloadNuGetPackageFolder}");

            var workloadManifestProvider = new SdkDirectoryWorkloadManifestProvider(DotnetRoot, SdkVersion);
            var workloadResolver = WorkloadResolver.Create(workloadManifestProvider, DotnetRoot, SdkVersion);

            var workloadPacksToInstall = workloads
                .SelectMany(workloadId => workloadResolver.GetPacksInWorkload(workloadId))
                .Distinct()
                .Select(packId => workloadResolver.TryGetPackInfo(packId))
                .Where(pack => pack != null)
                .ToList();

            if (workloadPacksToInstall.Any())
            {
                var downloader = new NuGetPackageDownloader(new DirectoryPath(WorkloadNuGetPackageFolder));
                foreach (var workloadPack in workloadPacksToInstall)
                {
                    Console.WriteLine($"Installing: {workloadPack.ResolvedPackageId} {workloadPack.Version}");
                    var packagePath = downloader.DownloadPackageAsync(new PackageId(workloadPack.ResolvedPackageId), new NuGetVersion(workloadPack.Version), new PackageSourceLocation(), downloadFolder: new DirectoryPath(WorkloadNuGetPackageFolder))
                        .GetAwaiter().GetResult();
                    Console.WriteLine($"Downloaded to {packagePath}");
                }
            }
        }
    }
}
