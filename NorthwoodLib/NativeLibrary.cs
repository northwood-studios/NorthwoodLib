using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NorthwoodLib
{
	public static class NativeLibrary
	{
		private const string Kernel32 = "kernel32";
		private const string Libdl = "libdl";

		#region Windows
		[DllImport(Kernel32, EntryPoint = "LoadLibraryW", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern ModuleHandle LoadLibraryWindows(string name);

		[DllImport(Kernel32, EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi, SetLastError = true)]
		private static extern IntPtr GetProcessAddressWindows(ModuleHandle handle, string name);

		[DllImport(Kernel32, EntryPoint = "FreeLibrary", SetLastError = true)]
		private static extern bool FreeLibraryWindows(ModuleHandle handle);
		#endregion

		#region Libdl
		[DllImport(Libdl, EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
		private static extern ModuleHandle LoadLibraryLibdl(string name, DlopenFlag flags);

		[DllImport(Libdl, EntryPoint = "dlsym", CharSet = CharSet.Ansi)]
		private static extern IntPtr GetProcessAddressLibdl(ModuleHandle handle, string name);

		[DllImport(Libdl, EntryPoint = "dlclose")]
		private static extern int FreeLibraryLibdl(ModuleHandle handle);

		[DllImport(Libdl, EntryPoint = "dlerror", CharSet = CharSet.Ansi)]
		private static extern string GetErrorLibdl();

		// ReSharper disable UnusedMember.Local
		[Flags]
		private enum DlopenFlag
		{
			Lazy = 1,
			Now = 2,
			Global = 256,
			Local = 0,
			NoDelete = 4096,
			NoLoad = 4,
			DeepBind = 8
		}
		// ReSharper restore UnusedMember.Local
		#endregion

		public static ModuleHandle Load(string path)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				ModuleHandle handlewin = LoadLibraryWindows(path);
				return handlewin == ModuleHandle.Null ? throw new Win32Exception() : handlewin;
			}

			ModuleHandle handle = LoadLibraryLibdl(path, DlopenFlag.Lazy);
			return handle == ModuleHandle.Null ? throw new DlException() : handle;
		}

		public static void Free(ModuleHandle handle)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				if (!FreeLibraryWindows(handle))
					throw new Win32Exception();
				return;
			}

			if (FreeLibraryLibdl(handle) != 0)
				throw new DlException();
		}

		public static IntPtr GetFunctionPointer(this ModuleHandle handle, string name)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				IntPtr ptrwin = GetProcessAddressWindows(handle, name);
				return ptrwin == IntPtr.Zero ? throw new Win32Exception() : ptrwin;
			}

			IntPtr ptr = GetProcessAddressLibdl(handle, name);
			return ptr == IntPtr.Zero ? throw new DlException() : ptr;
		}

		public static T GetFunctionDelegate<T>(this ModuleHandle handle, string name) where T : Delegate
		{
			return Marshal.GetDelegateForFunctionPointer<T>(GetFunctionPointer(handle, name));
		}

		public readonly struct ModuleHandle : IEquatable<ModuleHandle>
		{
			public static readonly ModuleHandle Null = new ModuleHandle(IntPtr.Zero);

			private readonly IntPtr _handle;

			private ModuleHandle(IntPtr ptr)
			{
				_handle = ptr;
			}

			public static explicit operator IntPtr(ModuleHandle handle) => handle._handle;

			public bool Equals(ModuleHandle other)
			{
				return _handle == other._handle;
			}

			public override bool Equals(object obj)
			{
				return obj is ModuleHandle other && Equals(other);
			}

			public override int GetHashCode()
			{
				return _handle.GetHashCode();
			}

			public static bool operator ==(ModuleHandle left, ModuleHandle right)
			{
				return left.Equals(right);
			}

			public static bool operator !=(ModuleHandle left, ModuleHandle right)
			{
				return !left.Equals(right);
			}

			public override string ToString()
			{
				return _handle.ToString();
			}
		}

		public class DlException : Exception
		{
			internal DlException() : base(GetErrorLibdl())
			{
			}
		}
	}
}
