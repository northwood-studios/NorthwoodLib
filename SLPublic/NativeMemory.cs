using System;
using System.Runtime.InteropServices;

namespace SLPublic
{
	public sealed unsafe class NativeMemory : IDisposable
	{
		public readonly IntPtr Data;
		public readonly int Length;

		public NativeMemory(int size)
		{
			Data = Marshal.AllocCoTaskMem(size);
			Length = size;
			if (Length > 0)
				GC.AddMemoryPressure(Length);
		}

		public T* ToPointer<T>() where T : unmanaged
		{
			return (T*) Data.ToPointer();
		}

		private void Free()
		{
			Marshal.FreeCoTaskMem(Data);
			if (Length > 0)
				GC.RemoveMemoryPressure(Length);
		}

		public void Dispose()
		{
			Free();
			GC.SuppressFinalize(this);
		}

		~NativeMemory()
		{
			Free();
		}
	}
}
