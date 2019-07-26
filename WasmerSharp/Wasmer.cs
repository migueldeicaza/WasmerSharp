using System;
using System.Runtime.InteropServices;

namespace WasmerSharp {
	public enum ImportExportKind : uint {
		Function,
		Global,
		Memory,
		Table
	}

	public enum WasmerResult : uint {
		Ok = 1,
		Error = 2
	}

	public enum WasmerValueTag : uint {
		I32,
		I64,
		F32,
		F64
	}

	public struct WasmerByteArray {
		internal IntPtr bytes;
		internal uint bytesLen;

		public override string ToString ()
		{
			unsafe {
				var len = bytesLen > Int32.MaxValue ? Int32.MaxValue : (int)bytesLen;
				return System.Text.Encoding.UTF8.GetString ((byte*)bytes, len);
			}
		}

		internal byte [] ToByteArray ()
		{
			var len = bytesLen > Int32.MaxValue ? Int32.MaxValue : (int)bytesLen;
			var ret = new byte [len];
			Marshal.Copy (bytes, ret, 0, len);
			return ret;
		}

		public static WasmerByteArray FromString (string txt)
		{
			WasmerByteArray ret;
			
			ret.bytes = Marshal.StringToCoTaskMemAuto (txt);
			ret.bytesLen = (uint) System.Text.Encoding.UTF8.GetByteCount (txt);
			return ret;
		}
	}

	[StructLayout (LayoutKind.Explicit)]
	public struct WasmerValue {
		[FieldOffset (0)]
		public WasmerValueTag Tag;

		[FieldOffset (4)]
		public int I32;
		[FieldOffset (4)]
		public long I64;
		[FieldOffset (4)]
		public float F32;
		[FieldOffset (4)]
		public double F64;

		public object Encode ()
		{
			switch (Tag) {
			case WasmerValueTag.I32:
				return I32;
			case WasmerValueTag.I64:
				return I64;
			case WasmerValueTag.F32:
				return F32;
			case WasmerValueTag.F64:
				return F64;
			}
			return null;
		}
	}

	//
	// This wraps a native handle, it assumes that things can not be
	// disposed from anything but the owning thread, so you just dispose
	// objects explicitly.
	//
	public class WasmerNativeHandle : IDisposable {
		internal IntPtr handle;

		protected const string Library = "wasmer_runtime_c_api";

		internal WasmerNativeHandle (IntPtr handle)
		{
			this.handle = handle;
		}

		internal WasmerNativeHandle ()
		{
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void DisposeHandle ()
		{
		}

		public virtual void Dispose (bool disposing)
		{
			if (disposing)
				DisposeHandle ();
			handle = IntPtr.Zero;
		}

		[DllImport (Library)]
		extern static int wasmer_last_error_length ();
		[DllImport (Library)]
		extern static int wasmer_last_error_message (IntPtr buffer, int len);

		/// <summary>
		/// Returns the last error message that was raised by the Wasmer Runtime
		/// </summary>
		public string LastError {
			get {
				var len = wasmer_last_error_length ();
				var buf = Marshal.AllocHGlobal (len);
				wasmer_last_error_message (buf, len);
				var str = Marshal.PtrToStringAuto (buf);
				Marshal.FreeHGlobal (buf);
				return str;
			}
		}

	}

	/// <summary>
	/// Represents a WebAssembly module, created from a byte array containing the WebAssembly code.
	/// </summary>
	/// <remarks>
	/// 
	/// </remarks>
	public class WasmerModule : WasmerNativeHandle {
		internal WasmerModule (IntPtr handle) : base (handle) { }

		[DllImport (Library)]
		extern static WasmerResult wasmer_compile (out IntPtr handle, IntPtr body, uint len);
		
		/// <summary>
		/// Creates a new Module from the given WASM bytes pointed to by the specified address
		/// </summary>
		/// <param name="wasmBody">A pointer to a block of memory containing the WASM code to load into the module</param>
		/// <param name="bodyLength">The size of the wasmBody pointer</param>
		/// <returns>The WasmerModule instance, or null on error</returns>
		public static WasmerModule Create (IntPtr wasmBody, uint bodyLength)
		{
			if (wasmer_compile (out var handle, wasmBody, bodyLength) == WasmerResult.Ok) {
				return new WasmerModule (handle);
			} else
				return null;
		}

		/// <summary>
		/// Creates a new Module from the given WASM bytes
		/// </summary>
		/// <param name="wasmBody">An array containing the WASM code to load into the module</param>
		/// <returns>The WasmerModule instance, or null on error</returns>
		public static WasmerModule Create (byte [] wasmBody)
		{
			if (wasmBody == null)
				throw new ArgumentException (nameof (wasmBody));
			unsafe {
				fixed (byte* p = &wasmBody [0]) {
					return Create ((IntPtr)p, (uint) wasmBody.Length);
				}
			}
		}

		[DllImport (Library)]
		extern static void wasmer_export_descriptors (IntPtr handle, out IntPtr exportDescs);

		[DllImport (Library)]
		extern static void wasmer_export_descriptors_destroy (IntPtr handle);

		[DllImport (Library)]
		extern static int wasmer_export_descriptors_len (IntPtr handle);

		[DllImport (Library)]
		extern static IntPtr wasmer_export_descriptors_get (IntPtr descsHandle, int idx);

		[DllImport (Library)]
		extern static ImportExportKind wasmer_export_descriptor_kind (IntPtr handle);
		[DllImport (Library)]
		extern static WasmerByteArray wasmer_export_descriptor_name (IntPtr handle);

		/// <summary>
		/// Gets export descriptors for the given module
		/// </summary>
		public WasmerExportDescriptor [] ExportDescriptors {
			get {
				// Not worth surfacing all the Disposable junk, so we extract all the data in one go
				wasmer_export_descriptors (handle, out var exportsHandle);
				int len = wasmer_export_descriptors_len (handle);
				var result = new WasmerExportDescriptor [len];
				for (int i = 0; i < len; i++) {
					var dhandle = wasmer_export_descriptors_get (exportsHandle, i);

					result [i] = new WasmerExportDescriptor () {
						Kind = wasmer_export_descriptor_kind (dhandle),
						Name = wasmer_export_descriptor_name (dhandle).ToString ()
					};
				}
				wasmer_export_descriptors_destroy (exportsHandle);
				return result;
			}
		}

		[DllImport (Library)]
		unsafe extern static WasmerResult wasmer_module_instantiate (IntPtr handle, out IntPtr instance, wasmer_import* imports, int imports_len);

		public Instance Instatiate (Import [] imports)
		{
			if (imports == null)
				throw new ArgumentNullException (nameof (imports));

			var llimports = new wasmer_import [imports.Length];
			for (int i = 0; i < imports.Length; i++) {
				llimports [i].import_name = WasmerByteArray.FromString (imports [i].ImportName);
				llimports [i].module_name = WasmerByteArray.FromString (imports [i].ModuleName);
				llimports [i].tag = imports [i].Kind;
				llimports [i].value = imports [i].payload.handle;
			}
			unsafe {
				fixed (wasmer_import* p = &llimports [0]) {
					if (wasmer_module_instantiate (handle, out var result, p, llimports.Length) == WasmerResult.Ok)
						return new Instance (result);
					else
						return null;
				}
			}
		}
	}

	/// <summary>
	/// Represents an export from a web assembly module
	/// </summary>
	public class WasmerExportDescriptor {
		/// <summary>
		///  Gets export descriptor kind
		/// </summary>
		public ImportExportKind Kind { get; internal set; }


		/// <summary>
		/// Gets name for the export descriptor
		/// </summary>
		public string Name { get; internal set; }
	}

	public class WasmerExportFunc : WasmerNativeHandle {
		internal WasmerExportFunc (IntPtr handle) : base (handle) { }

		[DllImport (Library)]
		unsafe extern static WasmerResult wasmer_export_func_call (IntPtr handle, WasmerValue* values, int valueLen, WasmerValue* results, int resultLen);

		/// <summary>
		/// Calls the function with the specified parameters
		/// </summary>
		/// <param name="values">The values to pass to the exported function.</param>
		/// <param name="results">The array with the results, it should have enough space to hold all the results</param>
		/// <returns></returns>
		public WasmerResult Call (WasmerValue [] values, WasmerValue [] results)
		{
			if (values == null)
				throw new ArgumentNullException (nameof (values));
			if (results == null)
				throw new ArgumentNullException (nameof (results));

			unsafe {
				fixed (WasmerValue* v = &values [0]) {
					fixed (WasmerValue* result = &results [0]) {
						return wasmer_export_func_call (handle, v, values.Length, result, results.Length);
					}
				}
			}
		}

		[DllImport (Library)]
		extern static void wasmer_import_func_destroy (IntPtr handle);
		protected override void DisposeHandle ()
		{
			wasmer_import_func_destroy (handle);
		}
	}

	public class WasmerExport : WasmerNativeHandle {
		internal WasmerExport (IntPtr handle) : base (handle) { }
	}

	public class WasmerExports : WasmerNativeHandle {
		internal WasmerExports (IntPtr handle) : base (handle) { }
	}

	/// <summary>
	/// Represents the WebAssembly memory.   Memory is allocated in pages, which are 64k bytes in size.
	/// </summary>
	public class Memory : WasmerNativeHandle {
		internal Memory (IntPtr handle) : base (handle) { }

		[DllImport (Library)]
		extern static WasmerResult wasmer_memory_new (out IntPtr handle, WasmerLimits limits);

		/// <summary>
		///  Creates a memory block with the specified minimum and maxiumum limits
		/// </summary>
		/// <param name="minPages">Minimum number of allowed pages</param>
		/// <param name="maxPages">Optional, Maximum number of allowed pages</param>
		/// <returns>The object on success, or null on failure.</returns>
		public static Memory Create (uint minPages, uint? maxPages = null)
		{
			WasmerLimits limits;
			limits.min = minPages;

			if (maxPages.HasValue) {
				limits.max.hasSome = 1;
				limits.max.some = maxPages.Value;
			} else {
				limits.max.hasSome = 0;
				limits.max.some = 0;
			}

			if (wasmer_memory_new (out var handle, limits) == WasmerResult.Ok) {
				return new Memory (handle);
			}
			return null;
		}

		[DllImport (Library)]
		extern static void wasmer_memory_destroy (IntPtr handle);

		protected override void DisposeHandle ()
		{
			wasmer_memory_destroy (handle);
		}

		[DllImport (Library)]
		extern static WasmerResult wasmer_memory_grow (IntPtr handle, uint data);

		/// <summary>
		/// Grows the memory by the specified amount of pages.
		/// </summary>
		/// <param name="deltaPages"></param>
		public WasmerResult Grow (uint deltaPages)
		{
			return wasmer_memory_grow (handle, deltaPages);
		}

		[DllImport (Library)]
		extern static uint wasmer_memory_length (IntPtr handle);

		/// <summary>
		/// Returns the current length in pages of the given memory 
		/// </summary>
		public uint PageLength => wasmer_memory_length (handle);

		[DllImport (Library)]
		extern static uint wasmer_memory_data_length (IntPtr handle);

		/// <summary>
		/// Returns the current length in bytes of the given memory 
		/// </summary>
		public uint DataLength => wasmer_memory_data_length (handle);

		[DllImport (Library)]
		extern static IntPtr wasmer_memory_data (IntPtr handle);

		public IntPtr Data => wasmer_memory_data (handle);

	}

	// Represents WasmerGlobal
	public class Global: WasmerNativeHandle {
		internal Global (IntPtr handle) : base (handle) { }
	}

	public struct WasmerGlobalDescriptor {
		public byte mutable;
		public WasmerValueTag kind;
	}

	public class WasmerImportDescriptor : WasmerNativeHandle {
		internal WasmerImportDescriptor (IntPtr handle) : base (handle) { }
	}

	public class WasmerImportDescriptors : WasmerNativeHandle {
		internal WasmerImportDescriptors (IntPtr handle) : base (handle) { }
	}

	/// <summary>
	///  Support for surfacing .NET functions to Wasm code
	/// </summary>
	// This is WasmerImportFunc
	public class Function: WasmerNativeHandle {
		internal Function (IntPtr handle) : base (handle) { }

		internal static WasmerValueTag ValidateTypeToTag (Type type)
		{
			if (type.IsByRef) 
				throw new ArgumentException ("The provided method can not out/ref parameters");
			
			if (type == typeof (int)) {
				return WasmerValueTag.I32;
			} else if (type == typeof (long)) {
				return WasmerValueTag.I64;
			} else if (type == typeof (double)) {
				return WasmerValueTag.F64;
			} else if (type == typeof (float)) {
				return WasmerValueTag.F32;
			} else
				throw new ArgumentException ("The method can only contain parameters of type int, long, float and double");
		}

		[DllImport (Library)]
		extern static IntPtr wasmer_import_func_new (IntPtr func, WasmerValueTag [] pars, int paramLen, WasmerValueTag [] returns, int retLen);

		/// <summary>
		///    Creates a WasmerImportFunc from a delegate method.
		/// </summary>
		/// <param name="method">The method to wrap.   The method can only contains int, long, float or double arguments.  The method return can include void, int, long, float and double. </param>
	
		public Function (Delegate method)
		{
			if (method == null)
				throw new ArgumentNullException (nameof (method));

			var methodInfo = method.Method;
			var methodPi = methodInfo.GetParameters ();
			var pars = new WasmerValueTag [methodPi.Length];
			
			int i = 0;
			foreach (var pi in methodInfo.GetParameters ()) {
				var pt = pi.ParameterType;
				var vt = ValidateTypeToTag (pt);
				pars [i++] = vt;
				
				i++;
			}

			WasmerValueTag [] returnTag;

			var returnType = methodInfo.ReturnType;
			if (returnType == typeof (void)) {
				returnTag = new WasmerValueTag [0];
			} else {
				returnTag = new WasmerValueTag [1] { ValidateTypeToTag (returnType) };
			}
			
			handle = wasmer_import_func_new (
				Marshal.GetFunctionPointerForDelegate (method),
				pars, pars.Length, returnTag, returnTag.Length);
		}
	}

	public class Instance : WasmerNativeHandle {
		internal Instance (IntPtr handle) : base (handle) { }

		[DllImport (Library)]
		unsafe extern static WasmerResult wasmer_instance_call (IntPtr handle, string name, WasmerValue* par, uint parLen, WasmerValue* res, uint resLen);

		/// <summary>
		/// Calls the specified function with the provided arguments
		/// </summary>
		/// <param name="functionName">Namer of the exported function to call in the instane</param>
		/// <param name="parameters">The parameters to pass to the function</param>
		/// <param name="results">The array where the return values are returned</param>
		/// <returns>True on success, false on failure</returns>
		public bool Call (string functionName, WasmerValue [] parameters, WasmerValue [] results)
		{
			unsafe {
				fixed (WasmerValue* p = &parameters [0]) {
					fixed (WasmerValue* r = &results [0]) {
						return wasmer_instance_call (handle, functionName, p, (uint) parameters.Length, r, (uint) results.Length) == WasmerResult.Ok;
					}
				}
			}
		}

		/// <summary>
		/// Calls the specified function with the provided arguments
		/// </summary>
		/// <param name="functionName">Namer of the exported function to call in the instane</param>
		/// <param name="args">The argument types are limited to int, long, float and double.</param>
		/// <returns>An array of values on success, null on error.</returns>
		public object [] Call (string functionName, object [] args)
		{
			if (functionName == null)
				throw new ArgumentNullException (nameof (functionName));
			if (args == null)
				throw new ArgumentNullException (nameof (args));

			foreach (var a in args) {
				
			}
			var parsOut = new WasmerValue [args.Length];
			for (int i = 0; i < args.Length; i++) {
				var tag = Function.ValidateTypeToTag (args.GetType ());
				parsOut [i].Tag = tag;
				switch (tag) {
				case WasmerValueTag.I32:
					parsOut [i].I32 = (int) args [i];
					break;
				case WasmerValueTag.I64:
					parsOut [i].I64 = (long)args [i];
					break;
				case WasmerValueTag.F32:
					parsOut [i].F32 = (float)args [i];
					break;
				case WasmerValueTag.F64:
					parsOut [i].F64 = (double)args [i];
					break;
				}
			}
			// TODO: need to extract array lenght for return and other assorted bits
			var ret = new WasmerValue [1];
			if (Call (functionName, parsOut, ret)) {
				return new object [] { ret [0].Encode () };
			}
			return null;
		}
	}

	public class WasmerInstanceContext : WasmerNativeHandle {
		internal WasmerInstanceContext (IntPtr handle) : base (handle) { }
	}

	// WasmerTable
	public class Table : WasmerNativeHandle {
		internal Table (IntPtr handle) : base (handle) { }
	}

	internal struct wasmer_import {
		internal WasmerByteArray module_name;
		internal WasmerByteArray import_name;
		internal ImportExportKind tag;
		internal IntPtr value;
	}

	/// <summary>
	/// Use this class to create the various Import objects (Globals, Memory, Function and Tables)
	/// </summary>
	public class Import {
		/// <summary>
		/// The module name for this import
		/// </summary>
		public string ModuleName { get; private set; }
		/// <summary>
		///  The name for this import
		/// </summary>
		public string ImportName { get; private set; }
		/// <summary>
		/// The kind of import
		/// </summary>
		public ImportExportKind Kind { get; private set; }
		internal WasmerNativeHandle payload;

		/// <summary>
		/// Creates a Memory import.
		/// </summary>
		/// <param name="moduleName">The module name for this import</param>
		/// <param name="importName">The name for this import, if not specified, it will default to "memory"</param>
		/// <param name="memory">The memory object to import</param>
		public Import (string moduleName, string importName, Memory memory)
		{
			if (moduleName == null)
				throw new ArgumentNullException(nameof (moduleName));
			if (memory == null)
				throw new ArgumentNullException (nameof (memory));
			ModuleName = moduleName;
			ImportName = importName ?? "memory";
			Kind = ImportExportKind.Memory;
			payload = memory;
		}

		/// <summary>
		/// Creates a Global import.
		/// </summary>
		/// <param name="moduleName">The module name for this import</param>
		/// <param name="importName">The name for this import.</param>
		/// <param name="global">The global object to import</param>
		public Import (string moduleName, string importName, Global global)
		{
			if (moduleName == null)
				throw new ArgumentNullException (nameof (moduleName));
			if (global == null)
				throw new ArgumentNullException (nameof (global));
			ModuleName = moduleName;
			ImportName = importName ?? "memory";
			Kind = ImportExportKind.Global;
			payload = global;
		}

		/// <summary>
		/// Creates a Function import.
		/// </summary>
		/// <param name="moduleName">The module name for this import</param>
		/// <param name="importName">The name for this import</param>
		/// <param name="function">The function to import</param>
		public Import (string moduleName, string importName, Function function)
		{
			if (moduleName == null)
				throw new ArgumentNullException (nameof (moduleName));
			if (importName == null)
				throw new ArgumentNullException (nameof (importName));
			if (function == null)
				throw new ArgumentNullException (nameof (function));
			ModuleName = moduleName;
			ImportName = importName;
			Kind = ImportExportKind.Function;
			payload = function;
		}

		/// <summary>
		/// Creates a Table import.
		/// </summary>
		/// <param name="moduleName">The module name for this import</param>
		/// <param name="importName">The name for this import</param>
		/// <param name="table">The table to import</param>
		public Import (string moduleName, string importName, Table table)
		{
			if (moduleName == null)
				throw new ArgumentNullException (nameof (moduleName));
			if (importName == null)
				throw new ArgumentNullException (nameof (importName));
			if (table == null)
				throw new ArgumentNullException (nameof (table));
			ModuleName = moduleName;
			ImportName = importName;
			Kind = ImportExportKind.Function;
			payload = table;
		}
	}

	public struct WasmerLimitOption {
		internal byte hasSome; // bool
		internal uint some;
	}

	internal struct WasmerLimits {
		internal uint min;
		internal WasmerLimitOption max;
	}

	public class WasmerSerializedModule : WasmerNativeHandle {
		internal WasmerSerializedModule (IntPtr handle) : base (handle) { }
	}

	public class WasmerTrampolineBufferBuilder : WasmerNativeHandle {
		internal WasmerTrampolineBufferBuilder (IntPtr handle) : base (handle) { }
	}

	public class WasmerTrampolineCallable: WasmerNativeHandle {
		internal WasmerTrampolineCallable(IntPtr handle) : base (handle) { }
	}

	public class WasmerTrampolineBuffer : WasmerNativeHandle {
		internal WasmerTrampolineBuffer (IntPtr handle) : base (handle) { }
	}

}
