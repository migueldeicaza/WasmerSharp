# WasmerSharp

.NET Bindings for the Wasmer Runtime.  This allows you to run WASM code 
in the same process as your .NET Code.    

If you are looking at a way of converting WebAssembly code into .NET IL, 
suitable to turn C and C++ code into cross-platform mobile IL, use 
Eric Sink's [Wasm2Cil](https://github.com/ericsink/wasm2cil) documented
[here](https://ericsink.com/entries/wasm_wasi_dotnet.html)

This binds Wasmer at version ab5f28851a676f9d3672f41d1608e34ddab470ff

# Install and use

The best way of using WasmerSharp is to add a reference to the
[WasmerSharp Nuget
package](https://www.nuget.org/packages/WasmerSharp/) and then follow
along "[Introduction to
WasmerSharp](https://migueldeicaza.github.io/WasmerSharp/articles/intro.html)"

The `StandaloneSample` directory contains a .NET core example that you
can use as a reference.

# Documentation

See the [Introduction to
WasmerSharp](https://migueldeicaza.github.io/WasmerSharp/articles/intro.html)
for a quick crash course on WasmerSharp.

[Wasmer API Documentation](https://migueldeicaza.github.io/WasmerSharp/api/WasmerSharp.html)

# Developing WasmerSharp

If you want to contribute to WasmerSharp, you will likely develop
against this tree, and not against the published NuGet package in the
`StandaloneSample` which is intended to be a public sample that works
with the public release.

WasmerSharp itself is a .NET Standard 2 library, so it works with .NET
Desktop, .NET Core and Mono.  You can use the projects in the `Tests`
directory to test against the WasmerSharp library built here, as
opposed to referencing the official NuGet package.

The bindings will need the Wasmer C runtime to be installed somewhere
accessible in your system (either in a location accessible to the
dynamic linker in your OS, or you must copy manually those libraries
into the development directory).

To obtain the native Wasmer C runtime, you can either download the
support library for your platform from [Wasmer
Releases](https://github.com/wasmerio/wasmer/releases) page or using
the toplevel makefile target "fetch-runtimes".  Those are named:

* `libwasmer_runtime_c_api.dylib` for MacOS
* `libwasmer_runtime_c_api.so` for Linux
* `wasmer_runtime_c_api.dll` for Windows

The runtime that you get needs to be copied in the appropriate locaion
in `bin/Debug` or `bin/Release` in those places.

If you want to work on the Wasmer runtime and produce the support
libraries for WasmerSharp, you would build Wasmer like this:

```
cargo build -p wasmer-runtime-c-api
```

And then copy the `target/debug/libwasmer_runtime_c_api.dylib` library
to the destination.

# LICENSE

This is licensed under the MIT License terms.
