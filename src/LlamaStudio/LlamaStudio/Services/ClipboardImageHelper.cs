using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace LlamaStudio.Services;

public static class ClipboardImageHelper
{
	private const uint CF_DIB = 8u;

	private const uint CF_BITMAP = 2u;

	private static readonly object _lock = new object();

	[DllImport("user32.dll")]
	private static extern bool OpenClipboard(nint hWndNewOwner);

	[DllImport("user32.dll")]
	private static extern bool CloseClipboard();

	[DllImport("user32.dll")]
	private static extern nint GetClipboardData(uint uFormat);

	[DllImport("user32.dll")]
	private static extern bool IsClipboardFormatAvailable(uint format);

	[DllImport("kernel32.dll")]
	private static extern nint GlobalLock(nint hMem);

	[DllImport("kernel32.dll")]
	private static extern bool GlobalUnlock(nint hMem);

	[DllImport("kernel32.dll")]
	private static extern int GlobalSize(nint hMem);

	public static bool TryGetImageFromClipboard(out byte[]? pngBytes)
	{
		pngBytes = null;
		lock (_lock)
		{
			if (!OpenClipboard(IntPtr.Zero))
			{
				return false;
			}
			try
			{
				bool flag = false;
				if (IsClipboardFormatAvailable(8u))
				{
					nint clipboardData = GetClipboardData(8u);
					if (clipboardData != IntPtr.Zero)
					{
						int num = GlobalSize(clipboardData);
						if (num > 0)
						{
							nint num2 = GlobalLock(clipboardData);
							if (num2 != IntPtr.Zero)
							{
								byte[] array = new byte[num];
								Marshal.Copy(num2, array, 0, num);
								try
								{
									using MemoryStream memoryStream = new MemoryStream(array);
									memoryStream.Position = 0L;
									using Image image = Image.FromStream(memoryStream);
									pngBytes = ImageToPng(image);
									if (pngBytes != null && pngBytes.Length != 0)
									{
										flag = true;
									}
								}
								catch
								{
									try
									{
										string text = Path.Combine(Path.GetTempPath(), $"llama_studio_dib_{DateTime.Now:yyyyMMdd_HHmmss_fff}.bmp");
										File.WriteAllBytes(text, array);
										using Image fileImage = Image.FromFile(text);
										pngBytes = ImageToPng(fileImage);
										if (pngBytes != null && pngBytes.Length != 0)
										{
											flag = true;
										}
										try
										{
											File.Delete(text);
										}
										catch
										{
										}
									}
									catch
									{
									}
								}
								finally
								{
									GlobalUnlock(clipboardData);
								}
							}
						}
					}
				}
				if (!flag && IsClipboardFormatAvailable(2u))
				{
					nint clipboardData2 = GetClipboardData(2u);
					if (clipboardData2 != IntPtr.Zero)
					{
						try
						{
							using Bitmap clipboardImage = Image.FromHbitmap(clipboardData2);
							pngBytes = ImageToPng(clipboardImage);
							if (pngBytes != null && pngBytes.Length != 0)
							{
								flag = true;
							}
						}
						catch
						{
						}
					}
				}
				return flag;
			}
			finally
			{
				CloseClipboard();
			}
		}
	}

	private static byte[]? ImageToPng(Image image)
	{
		try
		{
			using MemoryStream memoryStream = new MemoryStream();
			image.Save(memoryStream, ImageFormat.Png);
			return memoryStream.ToArray();
		}
		catch
		{
			return null;
		}
	}
}
