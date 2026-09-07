using System.Runtime.InteropServices;

namespace LlamaStudio.Services;

[ComImport]
[Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileOpenDialog
{
	[PreserveSig]
	int Show(nint hwndOwner);

	void SetFileTypes();

	void SetFileTypeIndex(uint iFileType);

	void GetFileTypeIndex(out uint piFileType);

	void Advise();

	void Unadvise();

	void SetOptions(FOS fos);

	void GetOptions(out FOS pfos);

	void SetDefaultFolder(IShellItem psi);

	void SetFolder(IShellItem psi);

	void GetFolder(out IShellItem ppsi);

	void GetCurrentSelection(out IShellItem ppsi);

	void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);

	void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

	void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

	void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);

	void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

	void GetResult(out IShellItem ppsi);

	void AddPlace(IShellItem psi, int alignment);

	void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

	void Close(int hr);

	void SetClientGuid();

	void ClearClientData();

	void SetFilter();
}
