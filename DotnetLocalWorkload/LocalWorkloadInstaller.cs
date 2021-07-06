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
using static Microsoft.NET.Sdk.WorkloadManifestReader.WorkloadResolver;

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
                    var packagePath = Path.Combine(WorkloadNuGetPackageFolder, $"{workloadPack.ResolvedPackageId.ToLowerInvariant()}.{workloadPack.Version}.nupkg");
                    if (File.Exists(packagePath))
                    {
                        Console.WriteLine($"Already downloaded: {workloadPack.ResolvedPackageId} {workloadPack.Version}");
                    }
                    else
                    {
                        Console.WriteLine($"Downloading: {workloadPack.ResolvedPackageId} {workloadPack.Version}");
                        var actualPackagePath = downloader.DownloadPackageAsync(new PackageId(workloadPack.ResolvedPackageId), new NuGetVersion(workloadPack.Version), new PackageSourceLocation(), downloadFolder: new DirectoryPath(WorkloadNuGetPackageFolder))
                            .GetAwaiter().GetResult();
                        Console.WriteLine($"Downloaded to {actualPackagePath}");

                        if (actualPackagePath != packagePath)
                        {
                            Console.WriteLine("Download path was NOT expected path: " + packagePath);
                            packagePath = actualPackagePath;
                        }
                    }

                    if (PackIsInstalled(workloadPack))
                    {
                        Console.WriteLine($"Already installed: {workloadPack.ResolvedPackageId} {workloadPack.Version}");
                    }
                    else
                    {
                        string destination = GetPackPath(new[] { WorkloadPackRoot }, new WorkloadPackId(workloadPack.ResolvedPackageId), workloadPack.Version, workloadPack.Kind);
                        if (!Directory.Exists(Path.GetDirectoryName(destination)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destination));
                        }
                        if (IsSingleFilePack(workloadPack))
                        {
                            File.Copy(packagePath, destination);
                        }
                        else
                        {
                            downloader.ExtractPackageAsync(packagePath, new DirectoryPath(destination)).GetAwaiter().GetResult();
                        }
                    }
                }
            }
        }

        private bool PackIsInstalled(PackInfo packInfo)
        {
            if (IsSingleFilePack(packInfo))
            {
                return File.Exists(packInfo.Path);
            }
            else
            {
                return Directory.Exists(packInfo.Path);
            }
        }

        private bool IsSingleFilePack(PackInfo packInfo) => packInfo.Kind.Equals(WorkloadPackKind.Library) || packInfo.Kind.Equals(WorkloadPackKind.Template);

        private string GetPackPath(string[] dotnetRootPaths, WorkloadPackId packageId, string packageVersion, WorkloadPackKind kind)
        {
            string packPath = "";
            bool isFile;
            foreach (var rootPath in dotnetRootPaths)
            {
                switch (kind)
                {
                    case WorkloadPackKind.Framework:
                    case WorkloadPackKind.Sdk:
                        packPath = Path.Combine(rootPath, "packs", packageId.ToString(), packageVersion);
                        isFile = false;
                        break;
                    case WorkloadPackKind.Template:
                        packPath = Path.Combine(rootPath, "template-packs", packageId.GetNuGetCanonicalId() + "." + packageVersion.ToLowerInvariant() + ".nupkg");
                        isFile = true;
                        break;
                    case WorkloadPackKind.Library:
                        packPath = Path.Combine(rootPath, "library-packs", packageId.GetNuGetCanonicalId() + "." + packageVersion.ToLowerInvariant() + ".nupkg");
                        isFile = true;
                        break;
                    case WorkloadPackKind.Tool:
                        packPath = Path.Combine(rootPath, "tool-packs", packageId.ToString(), packageVersion);
                        isFile = false;
                        break;
                    default:
                        throw new ArgumentException($"The package kind '{kind}' is not known", nameof(kind));
                }

                bool packFound = isFile ?
                    File.Exists(packPath) :
                    Directory.Exists(packPath);

                if (packFound)
                {
                    break;
                }

            }
            return packPath;
        }
    }
}
