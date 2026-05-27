namespace NorthwoodLib;

/// <summary>
/// Provides common math utilities
/// </summary>
public static class MathUtils
{
	/// <summary>
	/// Returns upper 16 bits of an <see langword="uint"/>
	/// </summary>
	/// <param name="i">Input value</param>
	/// <returns>Upper 16 bits</returns>
	public static ushort Hiword(uint i) => (ushort) (i >> 16);
	/// <summary>
	/// Returns lower 16 bits of an <see langword="uint"/>
	/// </summary>
	/// <param name="i">Input value</param>
	/// <returns>Lower 16 bits</returns>
	public static ushort Loword(uint i) => (ushort) i;
}
