//
// Wasmer.cs: .NET bindings to the Wasmer engine
//
// Author:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WasmerSharp {
	/// <summary>
	/// Describes the kind of export or import
	/// </summary>
	public enum ImportExportKind : uint {
		/// <summary>
		/// The import or export is a Function
		/// </summary>
		Function,
		/// <summary>
		/// The import or export is a global
		/// </summary>
		Global,
		/// <summary>
		///  The import or export is a memory object
		/// </summary>
		Memory,
		/// <summary>
		/// The import or export is a table
		/// </summary>
		Table
	}

	internal enum WasmerResult : uint {
		Ok = 1,
		Error = 2
	}

	/// <summary>
	/// Describes the types exposed by the WasmerBridge
	/// </summary>
	public enum WasmerValueType : uint {
		/// <summary>
		/// The type is 32-bit integer
		/// </summary>
		Int32,
		/// <summary>
		/// The type is a 64 bit integer
		/// </summary>
		Int64,
		/// <summary>
		/// The type is a 32-bit floating point
		/// </summary>
		Float32,
		/// <summary>
		/// The type is a 64-bit floating point
		/// </summary>
		Float64
	}

	internal struct WasmerByteArray {
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
			ret.bytesLen = (uint)System.Text.Encoding.UTF8.GetByteCount (txt);
			return ret;
		}
	}

	/// <summary>
	/// This object can wrap an int, long, float or double.   The Tag property describes the actual payload, and the I32, I64, F32 and F64 fields provide access to the underlying data.   Implicit conversion from those data types to WasmerValue exist, and explicit conversions from a WasmerValue to those types exist.
	/// </summary>
	[StructLayout (LayoutKind.Explicit)]
	public struct WasmerValue {
		/// <summary>
		/// The underlying type for the value stored here.
		/// </summary>
		[FieldOffset (0)]
		public WasmerValueType Tag;

		/// <summary>
		/// The 32-bit integer component, when the Tag is Int32
		/// </summary>
		[FieldOffset (4)]
		public int I32;
		/// <summary>
		/// The 64-bit integer component, when the Tag is Int64
		/// </summary>
		[FieldOffset (4)]
		public long I64;
		/// <summary>
		/// The 32-bit floating point component, when the Tag is Float32
		/// </summary>
		[FieldOffset (4)]
		public float F32;

		/// <summary>
		/// The 64-bit floating point component, when the Tag is Float64
		/// </summary>
		[FieldOffset (4)]
		public double F64;

		/// <summary>
		/// Returns a boxed object that contains the underlying .NET type (int, long, float, double) based on the Tag for this value.
		/// </summary>
		/// <returns>The boxed value.</returns>
		public object Encode ()
		{
			switch (Tag) {
			case WasmerValueType.Int32:
				return I32;
			case WasmerValueType.Int64:
				return I64;
			case WasmerValueType.Float32:
				return F32;
			case WasmerValueType.Float64:
				return F64;
			}
			return null;
		}

		/// <summary>
		/// Returns the stored value as an int.   This will cast if the value is not a native int.
		/// </summary>
		/// <param name="val">The incoming WasmerValue.</param>
		public static explicit operator int (WasmerValue val)
		{
			switch (val.Tag) {
			case WasmerValueType.Int32:
				return val.I32;

			case WasmerValueType.Int64:
				return (int)val.I64;

			case WasmerValueType.Float32:
				return (int)val.F32;

			case WasmerValueType.Float64:
				return (int)val.F64;
			}
			throw new Exception ("Unknown WasmerValueType");
		}

		/// <summary>
		/// Returns the stored value as a long.   This will cast if the value is not a native long.
		/// </summary>
		/// <param name="val">The incoming WasmerValue.</param>
		public static explicit operator long (WasmerValue val)
		{
			switch (val.Tag) {
			case WasmerValueType.Int32:
				return val.I32;

			case WasmerValueType.Int64:
				return val.I64;

			case WasmerValueType.Float32:
				return (long)val.F32;

			case WasmerValueType.Float64:
				return (long)val.F64;
			}
			throw new Exception ("Unknown WasmerValueType");
		}

		/// <summary>
		/// Returns the stored value as a float.   This will cast if the value is not a native float.
		/// </summary>
		/// <param name="val">The incoming WasmerValue.</param>
		public static explicit operator float (WasmerValue val)
		{
			switch (val.Tag) {
			case WasmerValueType.Int32:
				return val.I32;

			case WasmerValueType.Int64:
				return (float)val.I64;

			case WasmerValueType.Float32:
				return val.F32;

			case WasmerValueType.Float64:
				return (float)val.F64;
			}
			throw new Exception ("Unknown WasmerValueType");
		}

		/// <summary>
		/// Returns the stored value as a double.   This will cast if the value is not a native double.
		/// </summary>
		/// <param name="val">The incoming WasmerValue.</param>

		public static explicit operator double (WasmerValue val)
		{
			switch (val.Tag) {
			case WasmerValueType.Int32:
				return val.I32;

			case WasmerValueType.Int64:
				return (double)val.I64;

			case WasmerValueType.Float32:
				return val.F32;

			case WasmerValueType.Float64:
				return val.F64;
			}
			throw new Exception ("Unknown WasmerValueType");
		}

		/// <summary>
		/// Creates a WasmerValue from an integer
		/// </summary>
		/// <param name="val">Integer value to wrap</param>
		public static implicit operator WasmerValue (int val)
		{
			return new WasmerValue () { I32 = val, Tag = WasmerValueType.Int32 };
		}

		/// <summary>
		/// Creates a WasmerValue from an long
		/// </summary>
		/// <param name="val">Long value to wrap</param>
		public static implicit operator WasmerValue (long val)
		{
			return new WasmerValue () { I64 = val, Tag = WasmerValueType.Int64 };
		}

		/// <summary>
		/// Creates a WasmerValue from an float
		/// </summary>
		/// <param name="val">Float value to wrap</param>
		public static implicit operator WasmerValue (float val)
		{
			return new WasmerValue () { F32 = val, Tag = WasmerValueType.Float32 };
		}

		/// <summary>
		/// Creates a WasmerValue from an double
		/// </summary>
		/// <param name="val">Double value to wrap</param>
		public static implicit operator WasmerValue (double val)
		{
			return new WasmerValue () { F64 = val, Tag = WasmerValueType.Float64 };
		}

	}

	/// <summary>
	/// This wraps a native handle and takes care of disposing the handles they wrap.
	/// Due to the design of the Wasmer API that can
	/// </summary>
	/// <remarks>
	/// produce a lot of values that need to be destroyed, and in an effort to balance
	/// the complexity that it would involve, this queues releases of data on either
	/// construction or on the main thread dispose method.
	/// </remarks>
	public class WasmerNativeHandle : IDisposable {
		static Queue<Tuple<Action<IntPtr>, IntPtr>> pendingReleases = new Queue<Tuple<Action<IntPtr>, IntPtr>> ();
		internal IntPtr handle;

		internal const string Library = "wasmer_runtime_c_api";

		/// <summary>
		///  Releases any pending objects that were queued for destruction
		/// </summary>
		internal static void Flush ()
		{
			while (pendingReleases.Count > 0) {
				var v = pendingReleases.Dequeue ();
				v.Item1 (v.Item2);
			}
		}

		internal WasmerNativeHandle (IntPtr handle)
		{
			this.handle = handle;
			Flush ();
		}

		internal WasmerNativeHandle ()
		{
			Flush ();
		}

		/// <summary>
		///  Disposes the object, releasing the unmanaged resources associated with it.
		/// </summary>
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		/// <summary>
		/// This method when called with disposing, should dispose right away,
		/// otherwise it should return an Action of IntPtr that can dispose the handle.
		/// </summary>
		/// <returns>The method to invoke on disposing, or null if there is no need to dispose</returns>
		internal virtual Action<IntPtr> GetHandleDisposer ()
		{
			return null;
		}

		internal virtual void Dispose (bool disposing)
		{
			var handleDisposer = GetHandleDisposer ();
			if (disposing) {
				if (handleDisposer != null)
					handleDisposer (handle);
				Flush ();
			} else if (handleDisposer != null) {
				lock (pendingReleases) {
					pendingReleases.Enqueue (new Tuple<Action<IntPtr>, IntPtr> (handleDisposer, handle));
				}
			}
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
	///    Use the Create method to create new instances of a module.
	/// </remarks>
	public class Module : WasmerNativeHandle {
		internal Module (IntPtr handle) : base (handle) { }

		[DllImport (Library)]
		extern static WasmerResult wasmer_compile (out IntPtr handle, IntPtr body, uint len);

		/// <summary>
		/// Creates a new Module from the given WASM bytes pointed to by the specified address
		/// </summary>
		/// <param name="wasmBody">A pointer to a block of memory containing the WASM code to load into the module</param>
		/// <param name="bodyLength">The size of the wasmBody pointer</param>
		/// <returns>The WasmerModule instance, or null on error.   You can use the LastError error property to get details on the error.</returns>
		public static Module Create (IntPtr wasmBody, uint bodyLength)
		{
			if (wasmer_compile (out var handle, wasmBody, bodyLength) == WasmerResult.Ok) {
				return new Module (handle);
			} else
				return null;
		}

		/// <summary>
		/// Creates a new Module from the given WASM bytes
		/// </summary>
		/// <param name="wasmBody">An array containing the WASM code to load into the module</param>
		/// <returns>The WasmerModule instance, or null on error.  You can use the LastError error property to get details on the error.</returns>
		public static Module Create (byte [] wasmBody)
		{
			if (wasmBody == null)
				throw new ArgumentException (nameof (wasmBody));
			unsafe {
				fixed (byte* p = &wasmBody [0]) {
					return Create ((IntPtr)p, (uint)wasmBody.Length);
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
		public ExportDescriptor [] ExportDescriptors {
			get {
				// Not worth surfacing all the Disposable junk, so we extract all the data in one go
				wasmer_export_descriptors (handle, out var exportsHandle);
				int len = wasmer_export_descriptors_len (handle);
				var result = new ExportDescriptor [len];
				for (int i = 0; i < len; i++) {
					var dhandle = wasmer_export_descriptors_get (exportsHandle, i);

					result [i] = new ExportDescriptor () {
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

		/// <summary>
		/// Creates a new Instance from the given wasm bytes and imports. 
		/// </summary>
		/// <param name="imports">The list of imports to pass, usually Function, Global and Memory</param>
		/// <returns>A Wasmer.Instance on success, or null on error.   You can use the LastError error property to get details on the error.</returns>
		public Instance Instatiate (params Import [] imports)
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

		[DllImport (Library)]
		extern static void wasmer_module_destroy (IntPtr handle);

		internal override Action<IntPtr> GetHandleDisposer ()
		{
			return wasmer_module_destroy;
		}

		[DllImport (Library)]
		extern static void wasmer_import_descriptors (IntPtr moduleHandle, out IntPtr importDescriptors);

		[DllImport (Library)]
		extern static void wasmer_import_descriptors_destroy (IntPtr handle);

		[DllImport (Library)]
		extern static IntPtr wasmer_import_descriptors_get (IntPtr descriptorsHandle, int idx);

		[DllImport (Library)]
		extern static int wasmer_import_descriptors_len (IntPtr handle);

		/// <summary>
		/// Returns the Import Descriptors for this module
		/// </summary>
		public ImportDescriptor [] ImportDescriptors {
			get {
				wasmer_import_descriptors (handle, out var importsHandle);
				var len = wasmer_import_descriptors_len (importsHandle);
				var res = new ImportDescriptor [len];
				for (int i = 0; i < len; i++) {
					res [i] = new ImportDescriptor (wasmer_import_descriptors_get (importsHandle, i));
				}
				wasmer_import_descriptors_destroy (importsHandle);
				return res;
			}
		}

		[DllImport (Library)]
		extern static WasmerResult wasmer_module_serialize (out IntPtr serialized, IntPtr handle);

		/// <summary>
		/// Serializes the module, the result can be turned into a byte array and saved.
		/// </summary>
		/// <returns>Null on error, or an instance of SerializedModule on success.  You can use the LastError error property to get details on the error.</returns>
		public SerializedModule Serialize ()
		{
			if (wasmer_module_serialize (out var serialized, handle) == WasmerResult.Ok) {
				return new SerializedModule (serialized);
			} else
				return null;
		}

		[DllImport (Library)]
		extern static byte wasmer_validate (IntPtr bytes, uint len);

		/// <summary>
		/// Validates a block of bytes for being a valid web assembly package.
		/// </summary>
		/// <param name="bytes">Pointer to the bytes that contain the webassembly payload</param>
		/// <param name="len">Length of the buffer.</param>
		/// <returns>True if this contains a valid webassembly package, false otherwise.</returns>
		public bool Validate (IntPtr bytes, uint len)
		{
			return wasmer_validate (bytes, len) != 0;
		}

		/// <summary>
		/// Validates a byte array for being a valid web assembly package.
		/// </summary>
		/// <param name="buffer">Array containing the webassembly package to validate</param>
		/// <returns>True if this contains a valid webassembly package, false otherwise.</returns>
		public bool Validate (byte [] buffer)
		{
			unsafe {
				fixed (byte* p = &buffer [0]) {
					return Validate ((IntPtr)p, (uint)buffer.Length);
				}
			}
		}
	}

	/// <summary>
	/// Represents an export from a web assembly module
	/// </summary>
	public class ExportDescriptor {
		/// <summary>
		///  Gets export descriptor kind
		/// </summary>
		public ImportExportKind Kind { get; internal set; }


		/// <summary>
		/// Gets name for the export descriptor
		/// </summary>
		public string Name { get; internal set; }
	}

	/// <summary>
	/// Represents an ExportedFunction from WebAssembly to .NET
	/// </summary>
	public class ExportFunction : WasmerNativeHandle {
		internal bool owns;
		internal ExportFunction (IntPtr handle, bool owns) : base (handle)
		{
			this.owns = owns;
		}

		[DllImport (Library)]
		unsafe extern static WasmerResult wasmer_export_func_call (IntPtr handle, WasmerValue* values, int valueLen, WasmerValue* results, int resultLen);

		/// <summary>
		/// Calls the function with the specified parameters
		/// </summary>
		/// <param name="values">The values to pass to the exported function.</param>
		/// <param name="results">The array with the results, it should have enough space to hold all the results</param>
		/// <returns></returns>
		public bool Call (WasmerValue [] values, WasmerValue [] results)
		{
			if (values == null)
				throw new ArgumentNullException (nameof (values));
			if (results == null)
				throw new ArgumentNullException (nameof (results));

			unsafe {
				fixed (WasmerValue* v = &values [0]) {
					fixed (WasmerValue* result = &results [0]) {
						return wasmer_export_func_call (handle, v, values.Length, result, results.Length) != 0;
					}
				}
			}
		}

		[DllImport (Library)]
		extern static void wasmer_import_func_destroy (IntPtr handle);
		internal override Action<IntPtr> GetHandleDisposer ()
		{
			if (owns)
				return wasmer_import_func_destroy;
			else
				return null;
		}

		[DllImport (Library)]
		unsafe extern static WasmerResult wasmer_export_func_params (IntPtr handle, WasmerValueType* p, uint pLen);

		[DllImport (Library)]
		extern static WasmerResult wasmer_export_func_params_arity (IntPtr handle, out uint result);

		/// <summary>
		/// Returns the parameter types for the exported function as an array.   Returns null on error. You can use the LastError error property to get details on the error.
		/// </summary>
		public WasmerValueType [] Parameters {
			get {
				if (wasmer_export_func_params_arity (handle, out var npars) == WasmerResult.Error)
					return null;
				var tags = new WasmerValueType [npars];
				unsafe {
					fixed (WasmerValueType* t = &tags [0]) {
						if (wasmer_export_func_params (handle, t, npars) == WasmerResult.Ok)
							return tags;
					}
				}
				return null;
			}
		}

		[DllImport (Library)]
		unsafe extern static WasmerResult wasmer_export_func_returns (IntPtr handle, WasmerValueType* ret, uint retLen);

		[DllImport (Library)]
		extern static WasmerResult wasmer_export_func_returns_arity (IntPtr handle, out uint result);

		/// <summary>
		/// Returns the return types for the exported function as an array.   Returns null on error. You can use the LastError error property to get details on the error.
		/// </summary>
		public WasmerValueType [] Returns {
			get {
				if (wasmer_export_func_returns_arity (handle, out var npars) == WasmerResult.Error)
					return null;
				var tags = new WasmerValueType [npars];
				unsafe {
					fixed (WasmerValueType* t = &tags [0]) {
						if (wasmer_export_func_returns (handle, t, npars) == WasmerResult.Ok)
							return tags;
					}
				}
				return null;
			}
		}

	}

	/// <summary>
	/// Represents an exported object from a Wasm Instance
	/// </summary>
	/// <remarks>
	/// <para>
	///   A module can declare a sequence of exports which are returned at
	///   instantiation time to the host environment.
	/// </para>
	/// <para>
	///    Exports have a name, which is required to be valid UTF-8, whose meaning is defined by the host environment, a type, indicating whether the export is a function, global, memory or table.
	/// </para>
	/// </remarks>
	public class Export : WasmerNativeHandle {
		internal Export (IntPtr handle) : base (handle) { }

		[DllImport (Library)]
		static extern ImportExportKind wasmer_export_kind (IntPtr handle);

		/// <summary>
		/// Gets the kind for the exported item
		/// </summary>
		public ImportExportKind Kind => wasmer_export_kind (handle);

		[DllImport (Library)]
		static extern WasmerByteArray wasmer_export_name (IntPtr handle);

		/// <summary>
		/// Gets the name for the export
		/// </summary>
		public string Name => wasmer_export_name (handle).ToString ();

		[DllImport (Library)]
		extern static IntPtr wasmer_export_to_func (IntPtr handle);

		/// <summary>
		/// Gets the exported function
		/// </summary>
		/// <returns>Null on error, or the exported function.  You can use the LastError error property to get details on the error.</returns>
		public ExportFunction GetExportFunction ()
		{
			var rh = wasmer_export_to_func (handle);
			if (rh != IntPtr.Zero)
				return new ExportFunction (rh, owns: false);
			return null;
		}

		[DllImport (Library)]
		extern static WasmerResult wasmer_export_to_memory (IntPtr handle, out IntPtr memory);

		/// <summary>
		/// Returns the memory object from the export
		/// </summary>
		/// <returns></returns>
		public Memory GetMemory ()
		{
			// DO WE OWN THE MEM HANDLE?
			if (wasmer_export_to_memory (handle, out var mem) == WasmerResult.Error)
				return null;
			return new Memory (mem);
		}

		[DllImport (Library)]
		extern static void wasmer_exports_destroy (IntPtr handle);

		internal override Action<IntPtr> GetHandleDisposer ()
		{
			return wasmer_exports_destroy;
		}
	}

	/// <summary>
	/// Represents the WebAssembly memory.   Memory is allocated in pages, which are 64k bytes in size.
	/// </summary>
	public class Memory : WasmerNativeHandle {
		bool owns;
		internal Memory (IntPtr handle, bool owns = true) : base (handle) {
			this.owns = owns;
		}

		[DllImport (Library)]
		extern static WasmerResult wasmer_memory_new (out IntPtr handle, Limits limits);

		/// <summary>
		///  Creates a memory block with the specified minimum and maxiumum limits
		/// </summary>
		/// <param name="minPages">Minimum number of allowed pages</param>
		/// <param name="maxPages">Optional, Maximum number of allowed pages</param>
		/// <returns>The object on success, or null on failure. You can use the LastError error property to get details on the error.</returns>
		public static Memory Create (uint minPages, uint? maxPages = null)
		{
			Limits limits;
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

		/// <summary>
		///  Constructor for memory, throws if there is an error.
		/// </summary>
		/// <param name="minPages">Minimum number of allowed pages</param>
		/// <param name="maxPages">Optional, Maximum number of allowed pages</param>
		
		public Memory  (uint minPages, uint? maxPages = null)
		{
			Limits limits;
			limits.min = minPages;

			if (maxPages.HasValue) {
				limits.max.hasSome = 1;
				limits.max.some = maxPages.Value;
			} else {
				limits.max.hasSome = 0;
				limits.max.some = 0;
			}

			if (wasmer_memory_new (out var xhandle, limits) == WasmerResult.Ok) 
				handle = xhandle;
			else
				throw new Exception ("Error creating the requested memory");
		}

		[DllImport (Library)]
		extern static void wasmer_memory_destroy (IntPtr handle);

		internal override Action<IntPtr> GetHandleDisposer ()
		{
			if (owns)
			    return wasmer_memory_destroy;
			return null;
		}

		[DllImport (Library)]
		extern static WasmerResult wasmer_memory_grow (IntPtr handle, uint data);

		/// <summary>
		/// Grows the memory by the specified amount of pages.
		/// </summary>
		/// <param name="deltaPages">The number of additional pages to grow</param>
		/// <returns>true on success, false on error.   You can use the LastError property to get more details on the error.</returns>
		public bool Grow (uint deltaPages)
		{
			return wasmer_memory_grow (handle, deltaPages) != 0;
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

		/// <summary>
		/// Returns a pointer to the memory backing this Memory instance.
		/// </summary>
		public IntPtr Data => wasmer_memory_data (handle);

	}

	/// <summary>
	/// Represents a Global variable instance, importable/exportable across multiple modules.
	/// </summary>
	public class Global : WasmerNativeHandle {
		internal Global (IntPtr handle) : base (handle) { }

		[DllImport (Library)]
		extern static IntPtr wasmer_global_new (WasmerValue value, byte mutable_);

		/// <summary>
		/// Creates a new global with the specified WasmerValue, or int, float, long and double which are implicitly convertible to it.
		/// </summary>
		/// <param name="val">The value to place on the global</param>
		/// <param name="mutable">Determines whether the global is mutable</param>
		public Global (WasmerValue val, bool mutable)
		{
			handle = wasmer_global_new (val, (byte)(mutable ? 1 : 0));
		}


		[DllImport (Library)]
		extern static void wasmer_global_destroy (IntPtr handle);

		internal override Action<IntPtr> GetHandleDisposer ()
		{
			return wasmer_global_destroy;
		}

		[DllImport (Library)]
		extern static WasmerValue wasmer_global_get (IntPtr handle);

		/// <summary>
		/// Returns the value stored in this global
		/// </summary>
		public WasmerValue Value {
			get {
				return wasmer_global_get (handle);
			}
		}

		[DllImport (Library)]
		extern static GlobalDescriptor wasmer_global_get_descriptor (IntPtr global);

		/// <summary>
		/// Determines whether this Global is mutable or not.
		/// </summary>
		public bool IsMutable {
			get {
				return wasmer_global_get_descriptor (handle).Mutable != 0;
			}
		}

		/// <summary>
		/// Returns the ValueType (type) of the global.
		/// </summary>
		public WasmerValueType ValueType {
			get {
				return wasmer_global_get_descriptor (handle).Type;
			}
		}

		[DllImport (Library)]
		extern static void wasmer_global_set (IntPtr global, WasmerValue value);

		/// <summary>
		/// Sets the value of the global to the provided value, which can be a WasmerValue, or an int, long, float or double
		/// </summary>
		/// <param name="value">The new value to set</param>
		public void Set (WasmerValue value)
		{
			wasmer_global_set (handle, value);
		}
	}

	[StructLayout (LayoutKind.Sequential)]
	internal struct GlobalDescriptor {
		internal byte Mutable;
		internal WasmerValueType Type;
	}

	/// <summary>
	/// The import descriptors for a WebAssembly module describe the type of each import, iits name and the module name it belongs to.
	/// </summary>
	public class ImportDescriptor : WasmerNativeHandle {
		internal ImportDescriptor (IntPtr handle) : base (handle) { }

		[DllImport (Library)]
		extern static ImportExportKind wasmer_import_descriptor_kind (IntPtr handle);

		/// <summary>
		/// Returns the descriptor kind
		/// </summary>
		public ImportExportKind DescriptorKind => wasmer_import_descriptor_kind (handle);

		[DllImport (Library)]
		extern static WasmerByteArray wasmer_import_descriptor_module_name (IntPtr handle);

		/// <summary>
		/// Gets module name for the import descriptor
		/// </summary>
		public string ModuleName => wasmer_import_descriptor_module_name (handle).ToString ();

		[DllImport (Library)]
		extern static WasmerByteArray wasmer_import_descriptor_name (IntPtr handle);

		/// <summary>
		/// Gets name for the import descriptor
		/// </summary>
		public string Name => wasmer_import_descriptor_name (handle).ToString ();

	}

	/// <summary>
	///  Support for surfacing .NET functions to the Wasm module.
	/// </summary>
	// This is WasmerImportFunc
	public class ImportFunction : WasmerNativeHandle {
		internal ImportFunction (IntPtr handle) : base (handle) { }

		internal static WasmerValueType ValidateTypeToTag (Type type)
		{
			if (type.IsByRef)
				throw new ArgumentException ("The provided method can not out/ref parameters");

			if (type == typeof (int)) {
				return WasmerValueType.Int32;
			} else if (type == typeof (long)) {
				return WasmerValueType.Int64;
			} else if (type == typeof (double)) {
				return WasmerValueType.Float64;
			} else if (type == typeof (float)) {
				return WasmerValueType.Float32;
			} else
				throw new ArgumentException ("The method can only contain parameters of type int, long, float and double");
		}

		[DllImport (Library)]
		extern static IntPtr wasmer_import_func_new (IntPtr func, WasmerValueType [] pars, int paramLen, WasmerValueType [] returns, int retLen);

		/// <summary>
		///    Creates an ImportFunction from a delegate method, the .NET method passed on the delegate will then be available to  be called by the Wasm runtime.
		/// </summary>
		/// <param name="method">The method to wrap.   The method can only contains int, long, float or double arguments.  The method return can include void, int, long, float and double. </param>

		public ImportFunction (Delegate method)
		{
			if (method == null)
				throw new ArgumentNullException (nameof (method));

			var methodInfo = method.Method;
			var methodPi = methodInfo.GetParameters ();
			if (methodPi.Length < 1)
				throw new ArgumentException ("The method must at least have one argument of type InstanceContext");

			var pars = new WasmerValueType [methodPi.Length-1];

			int i = 0;
			foreach (var pi in methodInfo.GetParameters ()) {
				var pt = pi.ParameterType;
				if (i == 0) {
					if (pt != typeof (InstanceContext)) {
						throw new ArgumentException ("The first method in the method must be of type InstanceContext");
					}
				} else {
					var vt = ValidateTypeToTag (pt);
					pars [i - 1] = vt;
				}
				i++;
			}

			WasmerValueType [] returnTag;

			var returnType = methodInfo.ReturnType;
			if (returnType == typeof (void)) {
				returnTag = new WasmerValueType [0];
			} else {
				returnTag = new WasmerValueType [1] { ValidateTypeToTag (returnType) };
			}

			handle = wasmer_import_func_new (
				Marshal.GetFunctionPointerForDelegate (method),
				pars, pars.Length, returnTag, returnTag.Length);
		}

		[DllImport (Library)]
		extern static void wasmer_import_func_destroy (IntPtr handle);

		internal override Action<IntPtr> GetHandleDisposer ()
		{
			return wasmer_import_func_destroy;
		}

		// I do not think these are necessary as these are just a way of getting the
		// data that is computed from the Delegate that creates the ImportFunc
		// wasmer_import_func_params
		// wasmer_import_func_params_arity
		// wasmer_import_func_returns
		// wasmer_import_func_returns_arity
	}

	/// <summary>
	/// Instances represents all the state associated with a module.   These are created by calling Module.Instantiate or by calling the Instance constructor.
	/// </summary>
	/// <remarks>
	/// At runtime, a module can be instantiated with a set of import values to produce an instance, which is an
	/// immutable tuple referencing all the state accessible to the running module. Multiple module instances
	/// can access the same shared state which is the basis for dynamic linking in WebAssembly.
	/// </remarks>
	public class Instance : WasmerNativeHandle {
		internal Instance (IntPtr handle) : base (handle) { }

		[DllImport (Library)]
		unsafe extern static WasmerResult wasmer_instance_call (IntPtr handle, string name, WasmerValue* par, uint parLen, WasmerValue* res, uint resLen);

		[DllImport (Library)]
		unsafe extern static WasmerResult wasmer_instantiate (out IntPtr handle, IntPtr buffer, uint len, wasmer_import* imports, int imports_len);

		/// <summary>
		/// Creates a new Instance from the given wasm bytes and imports. 
		/// </summary>
		/// <param name="wasm">Wasm byte code</param>
		/// <param name="imports">The list of imports to pass, usually Function, Global and Memory</param>
		/// <returns>A Wasmer.Instance on success, or null on error.   You can use the LastError error property to get details on the error.</returns>
		public Instance (byte [] wasm, params Import [] imports)
		{
			if (wasm == null)
				throw new ArgumentNullException (nameof (wasm));
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
					fixed (byte* bp = &wasm [0]) {
						if (wasmer_instantiate (out var result, (IntPtr)bp, (uint)wasm.Length, p, llimports.Length) == WasmerResult.Ok)
							handle = result;
						else
							throw new Exception ("Error instantiating from the provided wasm file" + LastError);
					}
				}
			}
		}
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
				uint plen = (uint) parameters.Length;

				// The API does not like to get a null value, so we need to pass a pointer to something
				// and a length of zero.
				if (plen == 0){
					parameters = new WasmerValue [1];
					parameters [0] = 0;
				}

				fixed (WasmerValue* p = &parameters [0]) {
					fixed (WasmerValue* r = &results [0]) {
						return wasmer_instance_call (handle, functionName, p, plen, r, (uint)results.Length) == WasmerResult.Ok;
					}
				}
			}
		}

		/// <summary>
		/// Calls the specified function with the provided arguments
		/// </summary>
		/// <param name="functionName">Namer of the exported function to call in the instane</param>
		/// <param name="args">The argument types are limited to int, long, float and double.</param>
		/// <returns>An array of values on success, null on error. You can use the LastError error property to get details on the error.</returns>
		public object [] Call (string functionName, params object [] args)
		{
			if (functionName == null)
				throw new ArgumentNullException (nameof (functionName));
			if (args == null)
				throw new ArgumentNullException (nameof (args));

			foreach (var a in args) {

			}
			var parsOut = new WasmerValue [args.Length];
			for (int i = 0; i < args.Length; i++) {
				var tag = ImportFunction.ValidateTypeToTag (args.GetType ());
				parsOut [i].Tag = tag;
				switch (tag) {
				case WasmerValueType.Int32:
					parsOut [i].I32 = (int)args [i];
					break;
				case WasmerValueType.Int64:
					parsOut [i].I64 = (long)args [i];
					break;
				case WasmerValueType.Float32:
					parsOut [i].F32 = (float)args [i];
					break;
				case WasmerValueType.Float64:
					parsOut [i].F64 = (double)args [i];
					break;
				}
			}

			// TODO: need to extract array length for return and other assorted bits
			var ret = new WasmerValue [1];
			if (Call (functionName, parsOut, ret)) {
				return new object [] { ret [0].Encode () };
			}
			return null;
		}

		[DllImport (Library)]
		extern static void wasmer_instance_destroy (IntPtr handle);

		internal override Action<IntPtr> GetHandleDisposer ()
		{
			if (gchandle.IsAllocated) {
				gchandle.Free ();
			}
			return wasmer_instance_destroy;
		}

		[DllImport (Library)]
		extern static void wasmer_instance_exports (IntPtr handle, out IntPtr exportsHandle);

		[DllImport (Library)]
		extern static IntPtr wasmer_exports_get (IntPtr handle, int idx);

		[DllImport (Library)]
		extern static int wasmer_exports_len (IntPtr handle);

		[DllImport (Library)]
		extern static void wasmer_exports_destroy (IntPtr handle);

		/// <summary>
		/// Returns an array with all the exports - the individual values must be manually disposed.
		/// </summary>
		/// <returns></returns>
		public Export [] Exports {
			get {

				wasmer_instance_exports (handle, out var exportsHandle);
				var len = wasmer_exports_len (exportsHandle);
				var result = new Export [len];
				for (int i = 0; i < len; i++) {
					result [i] = new Export (wasmer_exports_get (exportsHandle, i));
				}
				wasmer_exports_destroy (exportsHandle);
				return result;
			}
		}

		[DllImport (Library)]
		extern static void wasmer_instance_context_data_set (IntPtr handle, IntPtr data);

		GCHandle gchandle;

		/// <summary>
		/// Sets a global data field that can be accessed by all imported functions and retrieved by the InstanceContext.Data property.
		/// </summary>
		/// <param name="value">The value to pass to all the InstanceContext members</param>
		public void SetData (object value)
		{
			gchandle = GCHandle.Alloc (value);
			wasmer_instance_context_data_set (handle, GCHandle.ToIntPtr (gchandle));
		}
	}


	/// <summary>
	/// Represents a Wasmer Table.   Use the Create static method to create new instances of the table.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A table is similar to a linear memory whose elements, instead of being bytes, are opaque values of a
	/// particular table element type. This allows the table to contain values—like GC references,
	/// raw OS handles, or native pointers—that are accessed by WebAssembly code indirectly through an integer index.
	/// This feature bridges the gap between low-level, untrusted linear memory and high-level opaque
	/// handles/references at the cost of a bounds-checked table indirection.
	/// </para>
	/// <para>
	/// The table’s element type constrains the type of elements stored in the table and allows engines to
	/// avoid some type checks on table use. When a WebAssembly value is stored in a table, the value’s
	/// type must precisely match the element type. Depending on the operator/API used to store the value,
	/// this check may be static or dynamic. Just like linear memory, updates to a table are observed
	/// immediately by all instances that reference the table. Host environments may also allow storing
	/// non-WebAssembly values in tables in which case, as with imports, the meaning of using the value
	/// is defined by the host environment.
	/// </para>
	/// </remarks>
	public class Table : WasmerNativeHandle {
		internal Table (IntPtr handle) : base (handle) { }

		[DllImport (Library)]
		extern static void wasmer_table_destroy (IntPtr handle);

		internal override Action<IntPtr> GetHandleDisposer ()
		{
			return wasmer_table_destroy;
		}

		[DllImport (Library)]
		extern static WasmerResult wasmer_table_new (out IntPtr handle, Limits limits);

		/// <summary>
		/// Creates a new Table for the given descriptor
		/// </summary>
		/// <param name="min">Minimum number of elements to store on the table.</param>
		/// <param name="max">Optional, maximum number of elements to store on the table.</param>
		/// <returns>An instance of Table on success, or null on error.  You can use the LastError error property to get details on the error.</returns>
		public static Table Create (uint min, uint? max = null)
		{
			Limits limits;
			limits.min = min;

			if (max.HasValue) {
				limits.max.hasSome = 1;
				limits.max.some = max.Value;
			} else {
				limits.max.hasSome = 0;
				limits.max.some = 0;
			}
			if (wasmer_table_new (out var handle, limits) == WasmerResult.Ok)
				return new Table (handle);
			else
				return null;
		}

		[DllImport (Library)]
		extern static WasmerResult wasmer_table_grow (IntPtr handle, uint delta);

		/// <summary>
		/// Attemps to grow the table by the specified number of elements.
		/// </summary>
		/// <param name="delta">Number of elements to add to the table.</param>
		/// <returns>true on success, false on failure.  You can use the LastError error property to get details on the error.</returns>
		public bool Grow (uint delta)
		{
			return wasmer_table_grow (handle, delta) == WasmerResult.Ok;
		}

		[DllImport (Library)]
		extern static uint wasmer_table_length (IntPtr handle);

		/// <summary>
		/// Returns the current length of the given Table  
		/// </summary>
		public uint Length => wasmer_table_length (handle);
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
				throw new ArgumentNullException (nameof (moduleName));
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
		public Import (string moduleName, string importName, ImportFunction function)
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

	struct LimitOption {
		internal byte hasSome; // bool
		internal uint some;
	}

	internal struct Limits {
		internal uint min;
		internal LimitOption max;
	}

	/// <summary>
	/// Modules can either be serialized to byte arrays, or created from a serialized state (byte arrays).  This class provides this bridge.
	/// </summary>
	public class SerializedModule : WasmerNativeHandle {
		internal SerializedModule (IntPtr handle) : base (handle) { }

		[DllImport (Library)]
		extern static void wasmer_serialized_module_destroy (IntPtr handle);

		[DllImport (Library)]
		extern static WasmerResult wasmer_serialized_module_from_bytes (out IntPtr handle, IntPtr bytes, uint len);

		/// <summary>
		/// Creates a new SerializedModule from the provided buffer.
		/// </summary>
		/// <param name="bytes">Pointer to a region in memory containing the serialized module.</param>
		/// <param name="len">The number of bytes toe process from the buffer</param>
		/// <returns>Returns null on error, or an instance of SerializeModule on success.  You can use the LastError error property to get details on the error.</returns>
		public static SerializedModule FromBytes (IntPtr bytes, uint len)
		{
			if (wasmer_serialized_module_from_bytes (out var handle, bytes, len) == WasmerResult.Ok)
				return new SerializedModule (handle);
			else
				return null;
		}

		/// <summary>
		/// Creates a new SerializedModule from the provided byte array
		/// </summary>
		/// <param name="buffer">Array of bytes containing the serialized module.</param>
		/// <returns>Returns null on error, or an instance of SerializeModule on success.   You can use the LastError error property to get details on the error.</returns>
		public static SerializedModule FromBytes (byte [] buffer)
		{
			unsafe {
				fixed (byte* p = &buffer [0]) {
					return FromBytes ((IntPtr)p, (uint)buffer.Length);
				}
			}
		}

		[DllImport (Library)]
		extern static WasmerByteArray wasmer_serialized_module_bytes (IntPtr handle);

		/// <summary>
		/// Returns the serialized module as a byte array.
		/// </summary>
		/// <returns>The byte array for this serialized module</returns>
		public byte [] GetModuleBytes ()
		{
			return wasmer_serialized_module_bytes (handle).ToByteArray ();
		}

		internal override Action<IntPtr> GetHandleDisposer ()
		{
			return wasmer_serialized_module_destroy;
		}

		[DllImport (Library)]
		extern static WasmerResult wasmer_module_deserialize (out IntPtr module, IntPtr handle);

		/// <summary>
		/// Deserialize the given serialized module.
		/// </summary>
		/// <returns>Returns an instance of a Module, or null on error.  You can use the LastError error property to get details on the error. </returns>
		public Module Deserialize ()
		{
			if (wasmer_module_deserialize (out var moduleHandle, handle) == WasmerResult.Ok)
				return new Module (moduleHandle);
			return null;
		}
	}

	/// <summary>
	/// An instance of this type is provided as the first parameter of imported functions and can be used
	/// to get some contextual information from the callback to operate on: the global Data set for the
	/// instance as well as the associated memory.
	/// </summary>
	public struct InstanceContext {
		IntPtr handle;

		[DllImport (WasmerNativeHandle.Library)]
		extern static IntPtr wasmer_instance_context_data_get (IntPtr handle);

		/// <summary>
		/// Retrieves the global Data value that was set for this Instance.
		/// </summary>
		public object Data => GCHandle.FromIntPtr (wasmer_instance_context_data_get (handle));

		[DllImport (WasmerNativeHandle.Library)]
		extern static IntPtr wasmer_instance_context_memory (IntPtr handle, uint memoryId);

		/// <summary>
		/// The memory blob associated with the instance.   Currently this only supports idx=0
		/// </summary>
		/// <param name="idx">The index of the memory to retrieve, currently only supports one memory blob.</param>
		public Memory GetMemory (uint idx)
		{
			var b = wasmer_instance_context_memory (handle, idx);
			if (b == IntPtr.Zero)
				return null;
			return new Memory (b, owns: false);
		}
	}

#if false

	// Penbding bindigns: https://gist.github.com/migueldeicaza/32816d404e202840ee13ca9a7f0fe724
	public class TrampolineBufferBuilder : WasmerNativeHandle {
		internal TrampolineBufferBuilder (IntPtr handle) : base (handle) { }
	}

	public class TrampolineCallable: WasmerNativeHandle {
		internal TrampolineCallable(IntPtr handle) : base (handle) { }
	}

	public class TrampolineBuffer : WasmerNativeHandle {
		internal TrampolineBuffer (IntPtr handle) : base (handle) { }

		[DllImport (Library)]
		extern static void wasmer_trampoline_buffer_destroy (IntPtr handle);

		internal override Action<IntPtr> GetHandleDisposer ()
		{
			return wasmer_trampoline_buffer_destroy;
		}
	}
#endif
}
