namespace LlamaStudio.Core.Interfaces;

public class HfModelInfo
{
	public string Repo { get; set; } = string.Empty;

	public string FileName { get; set; } = string.Empty;

	public long Size { get; set; }

	public string SizeDisplay => FormatSize(Size);

	public string Sha256 { get; set; } = string.Empty;

	public string LocalPath { get; set; } = string.Empty;

	public bool ExistsLocally { get; set; }

	private static string FormatSize(long bytes)
	{
		if (bytes >= 1048576)
		{
			if (bytes < 1073741824)
			{
				return $"{(double)bytes / 1048576.0:F1} MB";
			}
			return $"{(double)bytes / 1073741824.0:F2} GB";
		}
		return $"{bytes / 1024} KB";
	}
}
