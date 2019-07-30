# Introduction

Familiarity with some of the concepts and conventions of WebAssembly
will be useful to understand this introduction, but it is not necessary.

In WebAssembly "Imports" refer to objects that are imported into the
WebAssembly world, and "Exports" are the bits that are exposed by the
WebAssembly code.    

When you want to expose a .NET method to WebAssembly, you would create
an "ImportFunction" for example.

WasmerSharp can load WebAssembly code into a .NET application.  .NET
methods can be exposed to the WebAssembly application, and individual
entry points from WebAssembly can be invoked from C#.

The simplest hosting can be done by loading the WebAssembly package
into memory, creating a memory object, and then invoking a method in
WebAssembly, the following example shows this:

```
using WasmerShrap;

// This creates a memory block with a minimum of 256 64k pages
// and a maxium of 256 64k pages
var memory = Memory.Create (minPages: 256, maxPages: 256);

// Now we surface the memory as an import
var memoryImport = new Import ("env", "memory", memory)

// We load a webassembly file
var wasm = File.ReadAllBytes ("demo.wasm");

// Now we create an instance based on the WASM file, and the memory provided:
var instance = new Instance (wasm, memory);

// And now you can invoke some code from WebAssembly:
var ret = instance.Call ("hello_world");
if (ret == null){
   Console.WriteLine ("Error calling the method hello_world, status:" + instance.LastError);
else
   Console.WriteLine ("The method returned: " + ret);
```

The `Instance` class creates a WebAssembly instance out of the
WebAssembly code (in this case the `wasm` array) as well as a series
of imports that are the parameters to the Instance.  In the simple
example above, we only provided a block of memory - the minimum
necessary import, but you can also provide functions via
ImportFunction, Tables and define Global variables that can be shared
across Instances.

The `Instance` constructor takes a variable list of `Import`
instances, so you can provide multiple functions, tables, memory
blocks and globals.  This is a convenience constructor that exist for
quickly running Wasm code.  Generally, you could first create a
`Module`, and then instantiate the `Module` with the list of `Imports`
that you want.


# Exposing Global Variables to WebAssembly

You can use the `Global` type to create values that can be exposed to
the WebAssembly code.  Globals can either be mutable or immutable, and
you specify that at creation time.

One a Global is created, you need to name it and wrap it in an
`Import` type, and pass this to the `Instance` constructor.`

# Surfacing .NET code to WebAssembly

To surface code to WebAssembly, you create an instance of the
ImportFunction method.  This wraps a .NET method.  The method must
have the following requirements:

* The first parameter must be an `InstanceContext` parameter, which is
  used by the function to access the `Memory` object, and also an
  instance-level data payload.

* The parameters must be `Int32`, `Int64`, `Float` or `Double`.

If you need to pass additional information or complex data structures,
you can consider storing the contents of the data into the `Memory`
and passing the address to this method, or setting the address in a
`Global` variable and passing that one.

Once you do this, then you create an `Import` wrapper for it.

# Example

This function shows the use of `Global`, `Instance, `ImportFunction` in action.

```
        // This method is invoked by the WebAssembly code.
	public static void Print (InstanceContext ctx, int ptr, int len)
	{
		Console.WriteLine (".NET Print called");
		var memoryBase = ctx.GetMemory (0).Data;
		unsafe {
			var str = System.Text.Encoding.UTF8.GetString ((byte*)memoryBase + ptr, len);

			Console.WriteLine ("Received this utf string: [{0}]", str);
		}
	}

	public static void Main (string [] args)
	{
		//
		// Creates the imports for the instance
		//
		var func = new Import ("env", "_print_str", new ImportFunction ((Action<InstanceContext,int,int>) (Print)));
		var memory = new Import ("env", "memory", Memory.Create (minPages: 256, maxPages: 256));
		var global = new Import ("env", "__memory_base", new Global (1024, false));

		var wasm = File.ReadAllBytes ("target.wasm");

		//
		// Create an instance from the wasm bytes, the declared func, the memory we created and the global we have
		//
		var instance = new Instance (wasm, func, memory, global);

		//
		// Call the method defined in webassembly
		//
		var ret = instance.Call ("_hello_wasm");
		if (ret == null)
			Console.WriteLine ("error calling the method: " + instance.LastError);
		else
			Console.WriteLine ("__hello_wasm returned: " + ret);
	}
```