using Avalonia.Controls;
using Avalonia.Input;
using System.Diagnostics;

namespace LlamaStudio.Views.Pages;

public partial class SupportPage : UserControl
{
    public SupportPage()
    {
        InitializeComponent();
    }

    void Avatar_Click(object? sender, PointerPressedEventArgs e)
    {
        OpenUrl("https://github.com/pytraveler");
    }

    void Profile_Click(object? sender, PointerPressedEventArgs e)
    {
        OpenUrl("https://github.com/pytraveler");
    }

    void Project_Click(object? sender, PointerPressedEventArgs e)
    {
        OpenUrl("https://github.com/pytraveler/LlamaServerLauncherAvalonia");
    }

    static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }
}
