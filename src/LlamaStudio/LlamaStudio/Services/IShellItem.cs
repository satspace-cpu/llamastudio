using System.Runtime.InteropServices;

namespace LlamaStudio.Services;

[ComImport]
[Guid("43826D1E-E718-42EE-BC55-A8E261BC376A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
	void BindToHandler();

	void GetParent();

	void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

	void GetAttributes();

	void Compare();
}
