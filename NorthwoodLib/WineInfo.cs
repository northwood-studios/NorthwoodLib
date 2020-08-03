using NorthwoodLib.Logging;
using NorthwoodLib.Pools;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NorthwoodLib
{
	public static unsafe class WineInfo
	{
#pragma warning disable IDE1006
		[StructLayout(LayoutKind.Sequential)]
		private readonly struct WinePatch
		{
			public readonly IntPtr author;
			public readonly IntPtr subject;
			public readonly int revision;
		}
#pragma warning restore IDE1006

		private const string Ntdll = "ntdll";

		/// <summary>
		/// Returns used Wine version https://wiki.winehq.org/Developer_FAQ#How_can_I_detect_Wine.3F
		/// </summary>
		/// <returns>Used Wine version</returns>
		[DllImport(Ntdll, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, EntryPoint = "wine_get_version")]
		private static extern string GetWineVersion();

		/// <summary>
		/// Returns used Wine build https://source.winehq.org/git/wine.git/blob/HEAD:/include/wine/library.h
		/// </summary>
		/// <returns>Used Wine build</returns>
		[DllImport(Ntdll, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, EntryPoint = "wine_get_build_id")]
		private static extern string GetWineBuildId();

		/// <summary>
		/// Returns used Wine Staging patches https://github.com/wine-staging/wine-staging/blob/master/patches/Staging/0003-loader-Add-commandline-option-patches-to-show-the-pa.patch
		/// </summary>
		/// <returns>Used Wine Staging patches</returns>
		[DllImport(Ntdll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "wine_get_patches")]
		private static extern WinePatch* GetWinePatches();

		/// <summary>
		/// Returns Wine host https://source.winehq.org/git/wine.git/blob/HEAD:/include/wine/library.h
		/// </summary>
		/// <returns>Used Wine host</returns>
		[DllImport(Ntdll, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, EntryPoint = "wine_get_host_version")]
		private static extern void GetWineHostVersion(out string sysname, out string release);

		/// <summary>
		/// Informs if user uses Wine. User can hide Wine so don't rely on this for uses other than diagnostic usage
		/// </summary>
		public static readonly bool UsesWine;

		/// <summary>
		/// Returns used Wine Version
		/// </summary>
		public static readonly string WineVersion;

		/// <summary>
		/// Returns used Wine Staging patches
		/// </summary>
		public static readonly string WinePatches;

		/// <summary>
		/// Returns used Wine host
		/// </summary>
		public static readonly string WineHost;

		static WineInfo()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				UsesWine = false;
				WineVersion = null;
				return;
			}

			try
			{
				string wineVersion = GetWineVersion();

				if (string.IsNullOrWhiteSpace(wineVersion))
				{
					UsesWine = false;
					WineVersion = null;
					return;
				}

				UsesWine = true;
				WineVersion = $"Wine {wineVersion}";
			}
			catch (Exception ex)
			{
				// not using Wine, ignore
				PlatformSettings.Log($"Wine not detected! {ex.Message}", LogType.Debug);
				UsesWine = false;
				WineVersion = null;
				return;
			}

			try
			{
				string wineBuild = GetWineBuildId();

				if (!string.IsNullOrWhiteSpace(wineBuild))
					WineVersion += $" {wineBuild}";
			}
			catch (Exception ex)
			{
				PlatformSettings.Log($"Wine build not detected! {ex.Message}", LogType.Debug);
			}

			if (TryGetPatches(out string info, out string patches))
			{
				WineVersion += info;
				WinePatches = patches;
			}

			try
			{
				GetWineHostVersion(out string sysname, out string release);
				WineHost = "Host:";
				if (!string.IsNullOrWhiteSpace(sysname))
					WineHost += $" {sysname}";
				if (!string.IsNullOrWhiteSpace(release))
					WineHost += $" {release}";
				WineVersion += $" {WineHost}";
			}
			catch (Exception ex)
			{
				PlatformSettings.Log($"Wine host not detected! {ex.Message}", LogType.Debug);
			}

			WineVersion = WineVersion.Trim();

			PlatformSettings.Log($"{WineVersion} detected!", LogType.Info);
		}

		private static bool TryGetPatches(out string info, out string patches)
		{
			info = null;
			patches = null;
			try
			{
				WinePatch* patch = GetWinePatches();

				if (patch == null)
				{
					PlatformSettings.Log("Wine Staging detected, but no patches found!", LogType.Debug);
					return false;
				}

				int patchCount = 0;
				StringBuilder patchBuilder = StringBuilderPool.Shared.Rent();
				while (patch->author != IntPtr.Zero)
				{
					patchCount++;
					// ReSharper disable once HeapView.BoxingAllocation
					patchBuilder.AppendFormat("Author: {0} Subject: {1} Revision: {2}", Marshal.PtrToStringAnsi(patch->author), Marshal.PtrToStringAnsi(patch->subject), patch->revision);
					patch++;
				}
				patches = patchBuilder.ToString();
				StringBuilderPool.Shared.Return(patchBuilder);
				// ReSharper disable once HeapView.BoxingAllocation
				info = $" (Staging - {patchCount} patches)";
				return true;
			}
			catch (Exception ex)
			{
				PlatformSettings.Log($"Wine Staging not detected! {ex.Message}", LogType.Debug);
			}

			return false;
		}
	}
}
