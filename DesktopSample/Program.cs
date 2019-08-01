//
// Wasmer.cs: .NET bindings to the Wasmer engine
//
// Author:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using WasmerSharp;
using System.IO;

class ExportSample {
	public void Print (InstanceContext ctx, int ptr, int len)
	{
	}
}

class MainClass {
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

	// This delegate is needed since .NET Core's GetFunctionPointerForDelegate does not allow
	// for generics to be passed.
	delegate void myCallback (InstanceContext ctx, int arg1, int arg2);

	public static void Main (string [] args)
	{
		//
		// Creates the imports for the instance
		//
		var func = new Import ("env", "_print_str", new ImportFunction ((myCallback) (Print)));
		var memory = new Import ("env", "memory", Memory.Create (minPages: 256, maxPages: 256));
		var global = new Import ("env", "__memory_base", new Global (1024, false));

		var wasm = File.ReadAllBytes ("target.wasm");

		var x = Module.Create (wasm);
		Console.WriteLine ("The loaded target.wasm has the following imports listed:");
		foreach (var import in x.ImportDescriptors) {
			Console.WriteLine ($"import: {import.Kind} {import.ModuleName}::{import.Name} ");
		}

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
}
