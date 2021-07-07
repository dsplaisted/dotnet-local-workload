# .NET SDK Local Workload Installation Tool

This tool will help install .NET SDK workloads outside of the DOTNET_ROOT.  It's very rough and a stand-in for functionality that we expect to eventually add to the .NET SDK.  See https://github.com/dotnet/sdk/issues/18104.

Sample usage (from the `DotnetLocalWorkload` folder):

```
dotnet run -- install microsoft-net-sdk-blazorwebassembly-aot
```

This will download the workload packs for the specified workload and extract them into a folder under the ApplicationData folder.  It will also print out some environment variables that you will need to set in order for the .NET SDK to use those workloads.

```
To use local workloads, set the DOTNETSDK_WORKLOAD_MANIFEST_ROOTS environment variable to C:\Users\daplaist\AppData\Roaming\dotnet-local-workloads\sdk-manifests
To use local workloads, set the DOTNETSDK_WORKLOAD_PACK_ROOTS environment variable to C:\Users\daplaist\AppData\Roaming\dotnet-local-workloads\packs
```

After these messages there will be a lot more Console output, mostly for NuGet package downloading.  If you run the command again though it will see that the NuGet packages have already been downloaded and skip that part.

Once you've set those environment variables, then the .NET SDK should pick up the workload you installed with this tool.  You will need a recent preview build of the .NET SDK.  I tested with the latest version from `main` at the time I was testing it (6.0.100-preview.7.21356.4).  The released version of the Preview 6 SDK may also work with this.