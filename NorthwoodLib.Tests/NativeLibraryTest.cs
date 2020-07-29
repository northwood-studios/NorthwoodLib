using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace NorthwoodLib.Tests
{
	public class NativeLibraryTest
	{
		[Theory]
		[InlineData("/")]
		[InlineData("invalidname.zip")]
		public void LoadDllFailTest(string path)
		{
			void Load() => NativeLibrary.Load(path);
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Assert.Throws<Win32Exception>(Load);
				return;
			}

			Assert.Throws<NativeLibrary.DlException>(Load);
		}

		private delegate int ProcessIdDelegate();

		[Fact]
		public void CallTest()
		{
			string module = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "kernel32" : "libdl";
			NativeLibrary.ModuleHandle moduleHandle = NativeLibrary.Load(module);
			string method = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "GetCurrentProcessId" : "getpid";
			Assert.Equal(Process.GetCurrentProcess().Id, moduleHandle.GetFunctionDelegate<ProcessIdDelegate>(method)());
			NativeLibrary.Free(moduleHandle);
		}

		[Fact]
		public void InvalidMethodTest()
		{
			string module = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "kernel32" : "libdl";
			NativeLibrary.ModuleHandle moduleHandle = NativeLibrary.Load(module);
			const string method = "invalid_method_name123456789";

			void GetFunctionPointer() => moduleHandle.GetFunctionPointer(method);
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Assert.Throws<Win32Exception>(GetFunctionPointer);
				return;
			}

			Assert.Throws<NativeLibrary.DlException>(GetFunctionPointer);

			void GetFunctionDelegate() => moduleHandle.GetFunctionDelegate<ProcessIdDelegate>(method);
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Assert.Throws<Win32Exception>(GetFunctionDelegate);
				return;
			}

			Assert.Throws<NativeLibrary.DlException>(GetFunctionDelegate);

			NativeLibrary.Free(moduleHandle);
		}
	}
}
