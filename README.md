# WasmerSharp

.NET Bindings for the Wasmer Runtime.  This allows you to run WASM code 
in the same process as your .NET Code.    

If you are looking at a way of converting WebAssembly code into .NET IL, 
suitable to turn C and C++ code into cross-platform mobile IL, use 
Eric Sink's [Wasm2Cil](https://github.com/ericsink/wasm2cil) documented
[here](https://ericsink.com/entries/wasm_wasi_dotnet.html)

This binds Wasmer at version ab5f28851a676f9d3672f41d1608e34ddab470ff

# Install

The Wasmer bindings are a .NET Standard library, and they will need
the Wasmer C runtime to be installed somewhere accessible in your
system (either the same directory as the DLL, or in a location
accessible to the dynamic linker).

To obtain the native Wasmer C runtime, you can build Wasmer like this:

```
cargo build -p wasmer-runtime-c-api
```

And then copy the `target/debug/libwasmer_runtime_c_api.dylib` library
to the destination.

# Documentation

See the [Introduction to
WasmerSharp](https://migueldeicaza.github.io/WasmerSharp/articles/intro.html)
for a quick crash course on WasmerSharp.

[Wasmer API Documentation](https://migueldeicaza.github.io/WasmerSharp/api/WasmerSharp.html)

# LICENSE

This is licensed under the MIT License terms.
