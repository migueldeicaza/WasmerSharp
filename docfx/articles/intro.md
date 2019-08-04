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
using WasmerSharp;

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

The [`Instance`](../api/WasmerSharp/WasmerSharp.Instance.html) class creates a WebAssembly instance out of the
WebAssembly code (in this case the `wasm` array) as well as a series
of imports that are the parameters to the Instance.  In the simple
example above, we only provided a block of memory - the minimum
necessary import, but you can also provide functions via
ImportFunction, Tables and define Global variables that can be shared
across Instances.

The [`Instance`](../api/WasmerSharp/WasmerSharp.Instance.html)
constructor takes a variable list of
[`Import`](../api/WasmerSharp/WasmerSharp.Import.html) instances, so
you can provide multiple functions, tables, memory blocks and globals.
This is a convenience constructor that exist for quickly running Wasm
code.  Generally, you could first create a
[`Module`](../api/WasmerSharp/WasmerSharp.Module.html), and then
instantiate the [`Module`](../api/WasmerSharp/WasmerSharp.Module.html)
with the list of [`Import`](../api/WasmerSharp/WasmerSharp.Import.html)
that you want.


# Exposing Global Variables to WebAssembly

You can use the [`Global`](../api/WasmerSharp/WasmerSharp.Global.html)
type to create values that can be exposed to the WebAssembly code.
Globals can either be mutable or immutable, and you specify that at
creation time.

One a Global is created, you need to name it and wrap it in an
[`Import`](../api/WasmerSharp/WasmerSharp.Import.html) type, and pass
this to the [`Instance`](../api/WasmerSharp/WasmerSharp.Instance.html)
constructor.`

# Surfacing .NET code to WebAssembly

To surface code to WebAssembly, you create an instance of the
[`ImportFunction`](../api/WasmerSharp/WasmerSharp.ImportFunction.html)
method.  This wraps a .NET method.  The method must have the following
requirements:

* The first parameter must be an [`InstanceContext`](../api/WasmerSharp/WasmerSharp.InstanceContext.html) parameter, which is
  used by the function to access the [`Memory`](../api/WasmerSharp/WasmerSharp.Memory.html) object, and also an
  instance-level data payload.

* The parameters must be `Int32`, `Int64`, `Float` or `Double`.

If you need to pass additional information or complex data structures,
you can consider storing the contents of the data into the [`Memory`](../api/WasmerSharp/WasmerSharp.Memory.html) 
and passing the address to this method, or setting the address in a
[`Global`](../api/WasmerSharp/WasmerSharp.Global.html)  variable and passing that one.

Once you do this, then you create an [`Import`](../api/WasmerSharp/WasmerSharp.Import.html) wrapper for it.

# Exploring Imports and Exports

If you have a WebAssembly file, you can load it using
[`Module.Create`](../api/WasmerSharp/WasmerSharp.Module.html)` and
then obtain a list of expectations (the `ImportDescriptors` property)
as well as the itemts it exports (the `ExportDescriptors` property).
Both return arrays that you can iterate with and contain things like
the `ModuleName`, `Name`, and the kind of descriptor.

# Example

This function shows the use of
[`Global`](../api/WasmerSharp/WasmerSharp.Global.html),
[`Instance`](../api/WasmerSharp/WasmerSharp.Instance.html) and
[`ImportFunction`](../api/WasmerSharp/WasmerSharp.ImportFunction.html)
in action.

```
// This method is invoked by the WebAssembly code.
public static void Print (InstanceContext ctx, int ptr, int len)
{
	Console.WriteLine (".NET Print called");
	var memoryBase = ctx.GetMemory (0).Data;
	unsafe {
		var str = Encoding.UTF8.GetString ((byte*)memoryBase + ptr, len);

		Console.WriteLine ("Received this utf string: [{0}]", str);
	}
}

delegate void PrintCallback (IntstanceContext ic, int par1, int par2);

public static void Main (string [] args)
{
	//
	// Creates the imports for the instance
	//
	var func = new Import ("env", "_print_str", 
	    new ImportFunction ((PrintCallback) (Print)));

	var memory = new Import ("env", "memory", 
	    Memory.Create (minPages: 256, maxPages: 256));

	var global = new Import ("env", "__memory_base", 
            new Global (1024, false));

	var wasm = File.ReadAllBytes ("target.wasm");

	//
	// Create an instance from the wasm bytes, the declared func, 
	// the memory we created and the global we have
	//
	var instance = new Instance (wasm, func, memory, global);

	//
	// Call the method defined in webassembly
	//
	var ret = instance.Call ("_hello_wasm");
	if (ret == null)
		Console.WriteLine ("error calling method {0}", instance.LastError);
	else
		Console.WriteLine ("__hello_wasm returned: " + ret);
}
```

