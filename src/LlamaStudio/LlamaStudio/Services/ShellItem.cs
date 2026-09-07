using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LlamaStudio.Services;

internal static class ShellItem
{
	public static IShellItem CreateFromPath(string path)
	{
		Guid riid = new Guid("43826D1E-E718-42EE-BC55-A8E261BC376A");
		IShellItem ppv;
		int num = SHCreateItemFromParsingName(path, IntPtr.Zero, ref riid, out ppv);
		if (num != 0)
		{
			throw new Win32Exception(num);
		}
		return ppv;
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, nint pbc, ref Guid riid, out IShellItem ppv);
}
