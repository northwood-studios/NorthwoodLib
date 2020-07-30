using System;
using System.Runtime.InteropServices;

namespace NorthwoodLib
{
	/// <summary>
	/// Stores a reference to unmanaged memory allocated with <see cref="Marshal.AllocCoTaskMem"/> and prevents it from leaking
	/// </summary>
	public sealed unsafe class NativeMemory : IDisposable
	{
		/// <summary>
		/// Pointer to allocated memory
		/// </summary>
		public readonly IntPtr Data;
		/// <summary>
		/// Allocated memory length
		/// </summary>
		public readonly int Length;

		/// <summary>
		/// Creates a <see cref="NativeMemory"/> with requested size
		/// </summary>
		/// <param name="size">Allocation size</param>
		public NativeMemory(int size)
		{
			Data = Marshal.AllocCoTaskMem(size);
			Length = size;
			if (Length > 0)
				GC.AddMemoryPressure(Length);
		}

		/// <summary>
		/// Converts the <see cref="IntPtr"/> to specified pointer type
		/// </summary>
		/// <typeparam name="T">Pointer type</typeparam>
		/// <returns>Pointer to allocated memory</returns>
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

		/// <summary>
		/// Frees allocated memory
		/// </summary>
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
