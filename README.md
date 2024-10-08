# Experimental Spin SDK for .NET based on NativeAOT-LLVM

This is an in-progress [Spin](https://github.com/fermyon/spin) SDK for .NET
based on the experimental
[NativeAOT-LLVM](https://github.com/dotnet/runtimelab/tree/feature/NativeAOT-LLVM)
fork of the .NET runtime.

As of this writing, .NET support for WASIp2 is still under development, so this
library relies on unofficial builds of the NativeAOT-LLVM compiler and standard
library.  Once WASIp2 support has been fully merged into the
`feature/NativeAOT-LLVM` branch and CI has been updated to publish official
packages for all major platforms (i.e. Windows, MacOS, and Linux), we'll switch
to those and publish an official package for this library.  Meanwhile, you'll
need to download the artifacts manually and point NuGet to them as described
below.

## Running the Examples

### Prerequisites

- [.NET 9.0 Preview 6 or later](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [Spin 2.7.0 or later](https://github.com/fermyon/spin/releases/tag/v2.7.0)
- (if using Linux) glibc 2.38 or later (available on Ubuntu 24.04 - see
  [dotnet/runtimelab#2605](https://github.com/dotnet/runtimelab/pull/2605))

Also, we need to download a few pre-release packages and tell NuGet where to
find them.  Set `platform=linux-arm64`, `platform=linux-x64`,
`platform=win-x64`, or `platform=osx-arm64` and run:

```
mkdir packages
curl -LO --output-dir packages https://github.com/dicej/spin-dotnet-sdk/releases/download/canary/Fermyon.Spin.SDK.0.1.0-dev.nupkg
curl -LO --output-dir packages https://github.com/dicej/spin-dotnet-sdk/releases/download/canary/Microsoft.DotNet.ILCompiler.LLVM.9.0.0-dev.nupkg
curl -LO --output-dir packages https://github.com/dicej/spin-dotnet-sdk/releases/download/canary/runtime.wasi-wasm.Microsoft.DotNet.ILCompiler.LLVM.9.0.0-dev.nupkg
curl -LO --output-dir packages https://github.com/dicej/spin-dotnet-sdk/releases/download/canary/runtime.$platform.Microsoft.DotNet.ILCompiler.LLVM.9.0.0-dev.nupkg
export NUGET_LOCAL_PATH=$(pwd)/packages
```

### Building and Running

Please file an issue if you have any trouble building or running these examples:

#### Hello World!

```
cd samples/http-hello
spin build -u
```

While Spin is running, you can switch to another terminal and run e.g.

```
curl -i localhost:3000
```

which should yield something like:

```
HTTP/1.1 200 OK
content-type: text/plain
transfer-encoding: chunked
date: Fri, 02 Aug 2024 19:45:33 GMT

hello, world!
```

#### Streaming and Outbound Requests

```
cd samples/http-streams
spin build -u
```

Then, in another terminal, use `curl` to send a request to the app:

```
curl -i -H 'content-type: text/plain' --data-binary @- localhost:3000/echo <<EOF
’Twas brillig, and the slithy toves
      Did gyre and gimble in the wabe:
All mimsy were the borogoves,
      And the mome raths outgrabe.
EOF
```

The above should echo the request body in the response.

In addition to the `/echo` endpoint, the app supports a `/hash-all` endpoint
which concurrently downloads one or more URLs and streams the SHA-256 hashes of
their contents.  You can test it with e.g.:

```
curl -i \
    -H 'url: https://webassembly.github.io/spec/core/' \
    -H 'url: https://www.w3.org/groups/wg/wasm/' \
    -H 'url: https://bytecodealliance.org/' \
    localhost:3000/hash-all
```

#### ASP.NET Core and JSON Serialization

**PLEASE NOTE: https://github.com/fermyon/spin/pull/2721 is required to run this example, so you'll need to build Spin from that branch until that PR is merged and released.**

This example currently relies on a [lightly-patched build of ASP.NET
Core](#aspnetcore-patches), which you'll need to download and record the
location of in an environment variable as follows:

```
curl -LO https://github.com/dicej/spin-dotnet-sdk/releases/download/canary/aspnetcore-wasi.zip
unzip aspnetcore-wasi.zip
export ASPNETCORE_WASI_PATH=$(pwd)/aspnetcore-wasi
```

With that done, you should be able to build and run the example:

```
cd samples/http-aspnetcore
spin build -u
```

Then, in another terminal, use `curl` to send a requests to the app:

```
curl -i localhost:3000/
curl -i localhost:3000/weatherforecast
curl -i localhost:3000/mystaticpage.html
curl -i localhost:3000/echo -H 'content-type: text/plain' --data-binary @- <<EOF
’Twas brillig, and the slithy toves
      Did gyre and gimble in the wabe:
All mimsy were the borogoves,
      And the mome raths outgrabe.
EOF
curl -i localhost:3000/data -H 'content-type: application/json' -d '{"Greeting":"Hola"}'
```

## TODOs and Open Questions

- This currently borrows (and tweaks) some MSBuild configuration from [componentize-dotnet](https://github.com/bytecodealliance/componentize-dotnet).  We should upstream the changes and use the `ByteCodeAlliance.Componentize.DotNet.WitBindgen` package instead.
- Currently, application code must explicitly add the `runtime.wasi-wasm.Microsoft.DotNet.ILCompiler.LLVM` and `runtime.${os}-${arch}.Microsoft.DotNet.ILCompiler.LLVM` `PackageReference`s as well as the `Fermyon.Spin.SDK` reference.  Is it possible to use only the latter and have the former pulled in automatically?
- Upstream the changes to the .NET runtime and AspNetCore, and update CI for the latter to produce `wasi-wasm` packages.  This will allow us to use official packages instead of hand-built ones.

## Credits

- As mentioned above, this uses some MSBuild configuration from [componentize-dotnet](https://github.com/bytecodealliance/componentize-dotnet)
- The ASP.NET Core example was ported from [Steve Sanderson's .NET WASI SDK prototype](https://github.com/SteveSandersonMS/dotnet-wasi-sdk), and WasiHttpServer.cs contains code which is based on that work.

## AspNetCore Patches

The `aspnetcore-wasi.zip` file was created by copying the `shared/Microsoft.AspNetCore.App/9.0.0-preview.7.24406.2/*.dll` files from a Linux/ARM64 installation of .NET 9 preview 7, then replacing the `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.FileProviders.Physical` assemblies with versions built with the following patches:

```diff
diff --git a/src/libraries/Microsoft.Extensions.Hosting/src/Internal/ConsoleLifetime.netcoreapp.cs b/src/libraries/Microsoft.Extensions.Hosting/src/Internal/ConsoleLifetime.netcoreapp.cs
index 256697d927b..a2bf6e18064 100644
--- a/src/libraries/Microsoft.Extensions.Hosting/src/Internal/ConsoleLifetime.netcoreapp.cs
+++ b/src/libraries/Microsoft.Extensions.Hosting/src/Internal/ConsoleLifetime.netcoreapp.cs
@@ -15,10 +15,13 @@ public partial class ConsoleLifetime : IHostLifetime

         private partial void RegisterShutdownHandlers()
         {
-            Action<PosixSignalContext> handler = HandlePosixSignal;
-            _sigIntRegistration = PosixSignalRegistration.Create(PosixSignal.SIGINT, handler);
-            _sigQuitRegistration = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, handler);
-            _sigTermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, handler);
+            if (!OperatingSystem.IsWasi())
+            {
+                Action<PosixSignalContext> handler = HandlePosixSignal;
+                _sigIntRegistration = PosixSignalRegistration.Create(PosixSignal.SIGINT, handler);
+                _sigQuitRegistration = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, handler);
+                _sigTermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, handler);
+            }
         }

         private void HandlePosixSignal(PosixSignalContext context)
@@ -31,9 +34,12 @@ private void HandlePosixSignal(PosixSignalContext context)

         private partial void UnregisterShutdownHandlers()
         {
-            _sigIntRegistration?.Dispose();
-            _sigQuitRegistration?.Dispose();
-            _sigTermRegistration?.Dispose();
+            if (!OperatingSystem.IsWasi())
+            {
+                _sigIntRegistration?.Dispose();
+                _sigQuitRegistration?.Dispose();
+                _sigTermRegistration?.Dispose();
+            }
         }
     }
 }

diff --git a/src/libraries/Microsoft.Extensions.FileProviders.Physical/src/PhysicalFileProvider.cs b/src/libraries/Microsoft.Extensions.FileProviders.Physical/src/PhysicalFileProvider.cs
index 925df989275..71fa429b4b5 100644
--- a/src/libraries/Microsoft.Extensions.FileProviders.Physical/src/PhysicalFileProvider.cs
+++ b/src/libraries/Microsoft.Extensions.FileProviders.Physical/src/PhysicalFileProvider.cs
@@ -163,8 +163,8 @@ internal PhysicalFilesWatcher CreateFileWatcher()

             FileSystemWatcher? watcher;
 #if NET
-            //  For browser/iOS/tvOS we will proactively fallback to polling since FileSystemWatcher is not supported.
-            if (OperatingSystem.IsBrowser() || (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()) || OperatingSystem.IsTvOS())
+            //  For WASI/browser/iOS/tvOS we will proactively fallback to polling since FileSystemWatcher is not supported.
+            if (OperatingSystem.IsWasi() || OperatingSystem.IsBrowser() || (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()) || OperatingSystem.IsTvOS())
             {
                 UsePollingFileWatcher = true;
                 UseActivePolling = true;
```
