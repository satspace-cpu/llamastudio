using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace LlamaStudio.Services;

public static class ClipboardImageHelper
{
    const uint CF_DIB = 8, CF_BITMAP = 2;

    [DllImport("user32.dll")] static extern bool OpenClipboard(IntPtr hWndNewOwner);
    [DllImport("user32.dll")] static extern bool CloseClipboard();
    [DllImport("user32.dll")] static extern IntPtr GetClipboardData(uint uFormat);
    [DllImport("user32.dll")] static extern bool IsClipboardFormatAvailable(uint format);
    [DllImport("kernel32.dll")] static extern IntPtr GlobalLock(IntPtr hMem);
    [DllImport("kernel32.dll")] static extern bool GlobalUnlock(IntPtr hMem);
    [DllImport("kernel32.dll")] static extern int GlobalSize(IntPtr hMem);

    static readonly object _lock = new object();

    public static bool TryGetImageFromClipboard(out byte[]? pngBytes)
    {
        pngBytes = null;

        lock (_lock)
        {
            if (!OpenClipboard(IntPtr.Zero))
                return false;

            try
            {
                bool gotImage = false;

                if (IsClipboardFormatAvailable(CF_DIB))
                {
                    var globalHandle = GetClipboardData(CF_DIB);
                    if (globalHandle != IntPtr.Zero)
                    {
                        var size = GlobalSize(globalHandle);
                        if (size > 0)
                        {
                            var srcPtr = GlobalLock(globalHandle);
                            if (srcPtr != IntPtr.Zero)
                            {
                                var buffer = new byte[size];
                                Marshal.Copy(srcPtr, buffer, 0, size);
                                try
                                {
                                    using var ms = new MemoryStream(buffer);
                                    ms.Position = 0;
                                    using var image = Image.FromStream(ms);
                                    pngBytes = ImageToPng(image);
                                    if (pngBytes != null && pngBytes.Length > 0)
                                        gotImage = true;
                                }
                                catch
                                {
                                    try
                                    {
                                        var bmpPath = Path.Combine(Path.GetTempPath(), $"llama_studio_dib_{DateTime.Now:yyyyMMdd_HHmmss_fff}.bmp");
                                        File.WriteAllBytes(bmpPath, buffer);
                                        using var fallbackImage = Image.FromFile(bmpPath);
                                        pngBytes = ImageToPng(fallbackImage);
                                        if (pngBytes != null && pngBytes.Length > 0)
                                            gotImage = true;
                                        try { File.Delete(bmpPath); } catch { }
                                    }
                                    catch { }
                                }
                                finally
                                {
                                    GlobalUnlock(globalHandle);
                                }
                            }
                        }
                    }
                }

                if (!gotImage && IsClipboardFormatAvailable(CF_BITMAP))
                {
                    var hBitmap = GetClipboardData(CF_BITMAP);
                    if (hBitmap != IntPtr.Zero)
                    {
                        try
                        {
                            using var bitmap = Image.FromHbitmap(hBitmap);
                            pngBytes = ImageToPng(bitmap);
                            if (pngBytes != null && pngBytes.Length > 0)
                                gotImage = true;
                        }
                        catch { }
                    }
                }

                return gotImage;
            }
            finally
            {
                CloseClipboard();
            }
        }
    }

    static byte[]? ImageToPng(Image image)
    {
        try
        {
            using var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
