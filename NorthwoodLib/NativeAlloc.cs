using System.Runtime.InteropServices;

namespace NorthwoodLib;

internal readonly unsafe ref struct NativeAlloc<T>(int count)
	where T : unmanaged
{
	private readonly nint _pointer = Marshal.AllocCoTaskMem(count * sizeof(T));

	public T* Pointer => (T*) _pointer;

	public void Dispose()
	{
		Marshal.FreeCoTaskMem(_pointer);
	}
}
