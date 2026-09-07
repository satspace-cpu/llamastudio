using System;

namespace LlamaStudio.Core.Enums;

public enum GpuSplitMode
{
	None,
	Layer,
	[Obsolete("The row split mode is no longer exposed. Existing profiles are migrated to Layer.")]
	Row,
	Tensor
}
