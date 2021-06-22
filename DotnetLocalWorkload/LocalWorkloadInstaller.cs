using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

namespace DotnetLocalWorkload
{
    class LocalWorkloadInstaller
    {
        public string DotnetRoot { get; set; }
        public string SdkVersion { get; set; }

        public string[] SdkManifestRoots { get; set; }
        public string[] SdkPackRoots { get; set; }

        public void Install()
        {
            Console.WriteLine($"dotnet root: {DotnetRoot}");
            Console.WriteLine($"SDK version: {SdkVersion}");
            Console.WriteLine($"Manifest roots: {string.Join(Path.PathSeparator, SdkManifestRoots)}");
            Console.WriteLine($"Pack roots: {string.Join(Path.PathSeparator, SdkPackRoots)}");
        }
    }
}
