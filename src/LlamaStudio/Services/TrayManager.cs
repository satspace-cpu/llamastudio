using Avalonia.Threading;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.Views;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace LlamaStudio.Services;

public class TrayManager : IDisposable
{
    readonly ISettings _settings;
    readonly IServerManager _serverManager;
    readonly IProfileManager _profileManager;
    readonly ILocalizationService _loc;
    readonly ILogService _log;
    readonly Action _showWindow;
    readonly Action<bool> _requestExit;

    NOTIFYICONDATA _nid = new();
    bool _isDisposed;
    uint _callbackMessage;
    IntPtr _hwndTray = IntPtr.Zero;
    IntPtr _hIcon = IntPtr.Zero;
    List<ServerProfile>? _cachedProfiles;
    WNDPROC? _wndProcDelegate;

    static TrayManager? _current;
    static Views.MonitoringWindow? _monitoringWindow;

    public TrayManager(
        ISettings settings,
        IServerManager serverManager,
        IProfileManager profileManager,
        ILocalizationService loc,
        ILogService log,
        Action showWindow,
        Action<bool> requestExit)
    {
        _settings = settings;
        _serverManager = serverManager;
        _profileManager = profileManager;
        _loc = loc;
        _log = log;
        _showWindow = showWindow;
        _requestExit = requestExit;

        _callbackMessage = RegisterWindowMessage("HERMASS_TRAY_CALLBACK");
    }

    public void Initialize()
    {
        _current = this;
        _hwndTray = CreateHiddenWindow();

        if (_hwndTray == IntPtr.Zero)
            return;

        _hIcon = CreateTrayIcon();

        _nid.Size = (uint)Marshal.SizeOf<NOTIFYICONDATA>();
        _nid.Wnd = _hwndTray;
        _nid.UID = 1;
        _nid.UCallbackMessage = _callbackMessage;
        _nid.UFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP;
        _nid.HIcon = _hIcon;

        _nid.szTip = new byte[128];
        var tipBytes = System.Text.Encoding.Unicode.GetBytes("LlamaStudio — llama.cpp Manager\0");
        System.Array.Copy(tipBytes, _nid.szTip, tipBytes.Length);

        bool result = Shell_NotifyIcon(NIM_ADD, ref _nid);
    }

    static IntPtr CreateTrayIcon()
    {
        try
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app-icon.ico");
            if (File.Exists(iconPath))
                return ExtractAssociatedIcon(IntPtr.Zero, iconPath, 0);
        }
        catch { }

        // Fallback: create default H icon programmatically
        try
        {
            using var bmp = new Bitmap(32, 32);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, 32, 32),
                Color.FromArgb(160, 50, 255),
                Color.FromArgb(80, 100, 255),
                System.Drawing.Drawing2D.LinearGradientMode.Vertical);
            using var pen = new Pen(brush, 4f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            g.DrawLine(pen, 10, 6, 10, 26);
            g.DrawLine(pen, 22, 6, 22, 26);
            g.DrawLine(pen, 10, 16, 22, 16);
            return bmp.GetHicon();
        }
        catch { }

        return IntPtr.Zero;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string lpIconPath, uint iconIndex);

    IntPtr CreateHiddenWindow()
    {
        string className = "LlamaStudioTrayWindow";
        var wndProc = new WNDPROC(TrayWndProc);
        _wndProcDelegate = wndProc;
        IntPtr procPtr = Marshal.GetFunctionPointerForDelegate(wndProc);

        IntPtr hInstance = Marshal.GetHINSTANCE(
            Assembly.GetExecutingAssembly().GetModules()[0]);

        IntPtr classPtr = Marshal.StringToHGlobalUni(className);
        var wc = new WNDCLASSW
        {
            Style = 0,
            lpfnWndProc = procPtr,
            CBandClassExtra = 0,
            CBandWindowExtra = 0,
            HInstance = hInstance,
            hIcon = IntPtr.Zero,
            hCursor = IntPtr.Zero,
            hbrBackground = IntPtr.Zero,
            lpszMenuName = IntPtr.Zero,
            lpszClassName = classPtr
        };

        RegisterClassW(ref wc);

        var hwnd = CreateWindowEx(
            0,
            className,
            string.Empty,
            WS_POPUP,
            0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        return hwnd;
    }

     static IntPtr TrayWndProc(IntPtr hwnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        if (_current != null && uMsg == _current._callbackMessage)
        {
            uint mouseEvent = (uint)lParam;

            if (mouseEvent == WM_CONTEXTMENU || mouseEvent == WM_RBUTTONUP)
            {
                GetCursorPos(out var pt);
                Dispatcher.UIThread.Post(() => _ = _current!.ShowContextMenu(new Avalonia.Vector(pt.X, pt.Y)));
            }
            else if (mouseEvent == WM_LBUTTONDBLCLK)
            {
                Dispatcher.UIThread.Post(_current._showWindow);
            }
            return IntPtr.Zero;
        }

        return DefWindowProc(hwnd, uMsg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _current = null;

        try { Shell_NotifyIcon(NIM_DELETE, ref _nid); } catch { }
        if (_hIcon != IntPtr.Zero) { DestroyIcon(_hIcon); _hIcon = IntPtr.Zero; }
        if (_hwndTray != IntPtr.Zero) { DestroyWindow(_hwndTray); _hwndTray = IntPtr.Zero; }
    }

    public async Task ShowContextMenu(Avalonia.Vector screenPos)
    {
        if (_isDisposed) return;

        try
        {
            var status = await _serverManager.GetStatusAsync();
            bool isRunning = status.State == ServerState.Running || status.State == ServerState.Starting;

            _cachedProfiles = await _profileManager.GetAllProfilesAsync();
            string activeProfileId = _settings.LastSelectedProfileId.Trim();

            var hMenu = CreatePopupMenu();

            var hProfilesMenu = CreatePopupMenu();
            if (_cachedProfiles != null && _cachedProfiles.Count > 0)
            {
                for (int i = 0; i < _cachedProfiles.Count; i++)
                {
                    var p = _cachedProfiles[i];
                    bool isActive = p.Id.Trim() == activeProfileId;
                    var menuText = isActive ? $"✓ {p.Name}" : p.Name;
                    AppendMenu(hProfilesMenu, MF_STRING, (uint)(100 + i), menuText);
                }
            }
            else
            {
                AppendMenu(hProfilesMenu, MF_GRAYED | MF_STRING, 0, "(no profiles)");
            }

            AppendMenu(hMenu, MF_POPUP, (uint)hProfilesMenu, _loc.T("tray.profiles"));
            AppendMenu(hMenu, MF_SEPARATOR, 0, string.Empty);

            if (isRunning)
            {
                AppendMenu(hMenu, MF_STRING, 1, _loc.T("tray.stop_server"));
                AppendMenu(hMenu, MF_STRING, 2, _loc.T("tray.restart_server"));
            }
            else
            {
                AppendMenu(hMenu, MF_STRING, 1, _loc.T("tray.start_server"));
            }

            AppendMenu(hMenu, MF_SEPARATOR, 0, string.Empty);
            AppendMenu(hMenu, MF_STRING, 5, "📊 Мониторинг");
            AppendMenu(hMenu, MF_STRING, 3, _loc.T("tray.show"));
            AppendMenu(hMenu, MF_STRING, 4, _loc.T("tray.exit"));

            SetForegroundWindow(_hwndTray);
            uint result = TrackPopupMenu(hMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON, (int)screenPos.X, (int)screenPos.Y, 0, _hwndTray, IntPtr.Zero);
            DestroyMenu(hMenu);

            if (result > 0)
                await HandleMenuCommand(result, isRunning);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Tray context menu error", "Tray");
        }
    }

    async Task HandleMenuCommand(uint cmdId, bool serverRunning)
    {
        try
        {
            switch (cmdId)
            {
                case 1:
                    if (serverRunning)
                        await _serverManager.StopAsync();
                    else
                    {
                        var profile = await GetActiveProfileAsync();
                        if (profile != null)
                            await _serverManager.StartAsync(profile);
                    }
                    break;
                case 2:
                    await _serverManager.StopAsync();
                    var rp = await GetActiveProfileAsync();
                    if (rp != null)
                        await _serverManager.StartAsync(rp);
                    break;
                case 3:
                    Dispatcher.UIThread.Post(_showWindow);
                    break;
                case 4:
                    _requestExit(true);
                    break;
                case 5:
                    Dispatcher.UIThread.Post(ToggleMonitoringWindow);
                    break;
                default:
                    if (cmdId >= 100 && _cachedProfiles != null)
                    {
                        int idx = (int)cmdId - 100;
                        if (idx >= 0 && idx < _cachedProfiles.Count)
                        {
                            var sp = _cachedProfiles[idx];
                            _settings.LastSelectedProfileId = sp.Id;
                            if (serverRunning)
                            {
                                await _serverManager.StopAsync();
                                await Task.Delay(1500);
                                await _serverManager.StartAsync(sp);
                            }
                            else
                                await _serverManager.StartAsync(sp);
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Tray menu command {cmdId} failed", "Tray");
        }
    }

    static void ToggleMonitoringWindow()
    {
        try
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (lifetime == null) return;

            if (_monitoringWindow != null && _monitoringWindow.IsVisible)
            {
                _monitoringWindow.Hide();
            }
            else if (_monitoringWindow != null)
            {
                _monitoringWindow.Show();
                _monitoringWindow.Activate();
            }
            else
            {
                var vm = App.Services?.GetService(typeof(ViewModels.MonitoringViewModel)) as ViewModels.MonitoringViewModel;
                if (vm != null)
                {
                    _monitoringWindow = new Views.MonitoringWindow(vm);
                    _monitoringWindow.Closed += (_, _) => _monitoringWindow = null;
                    _monitoringWindow.Show();
                }
            }
        }
        catch { }
    }

    // Win32
    const uint NIF_ICON = 0x2, NIF_MESSAGE = 0x1, NIF_TIP = 0x4;
    const uint NIM_ADD = 0x0, NIM_MODIFY = 0x1, NIM_DELETE = 0x2;
    const uint MF_STRING = 0x0, MF_POPUP = 0x10, MF_SEPARATOR = 0x800, MF_GRAYED = 0x1;
    const uint TPM_RETURNCMD = 0x100;
    const uint TPM_RIGHTBUTTON = 0x2;
    const uint WS_POPUP = 0x80000000;
    const uint WM_RBUTTONUP = 0x205, WM_CONTEXTMENU = 0x79, WM_LBUTTONDBLCLK = 0x203;

    delegate IntPtr WNDPROC(IntPtr hwnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WNDCLASSW
    {
        public uint Style;
        public IntPtr lpfnWndProc;
        public int CBandClassExtra, CBandWindowExtra;
        public IntPtr HInstance, hIcon, hCursor, hbrBackground, lpszMenuName, lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct NOTIFYICONDATA
    {
        public uint Size;
        public IntPtr Wnd;
        public uint UID;
        public uint UFlags;
        public uint UCallbackMessage;
        public IntPtr HIcon;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] szTip;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA pData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern ushort RegisterClassW(ref WNDCLASSW lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string? lpWindowName,
        uint dwStyle, int X, int Y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    static extern IntPtr DefWindowProc(IntPtr hwnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern IntPtr DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    static extern uint TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int reserved, IntPtr hwnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hwnd);

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int X, Y;
    }

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    async Task<LlamaStudio.Core.Models.ServerProfile?> GetActiveProfileAsync()
    {
        LlamaStudio.Core.Models.ServerProfile? profile = null;
        if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
            profile = await _profileManager.GetProfileAsync(_settings.LastSelectedProfileId);
        profile ??= await _profileManager.GetDefaultProfileAsync();
        return profile;
    }
}
