using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.ViewModels;
using LlamaStudio.Views;

namespace LlamaStudio.Services;

public class TrayManager : IDisposable
{
	private delegate nint WNDPROC(nint hwnd, uint uMsg, nint wParam, nint lParam);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct WNDCLASSW
	{
		public uint Style;

		public nint lpfnWndProc;

		public int CBandClassExtra;

		public int CBandWindowExtra;

		public nint HInstance;

		public nint hIcon;

		public nint hCursor;

		public nint hbrBackground;

		public nint lpszMenuName;

		public nint lpszClassName;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct NOTIFYICONDATA
	{
		public uint Size;

		public nint Wnd;

		public uint UID;

		public uint UFlags;

		public uint UCallbackMessage;

		public nint HIcon;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
		public byte[] szTip;
	}

	private struct POINT
	{
		public int X;

		public int Y;
	}

	private readonly ISettings _settings;

	private readonly IServerManager _serverManager;

	private readonly IProfileManager _profileManager;

	private readonly ILocalizationService _loc;

	private readonly ILogService _log;

	private readonly Action _showWindow;

	private readonly Action<bool> _requestExit;

	private NOTIFYICONDATA _nid;

	private bool _isDisposed;

	private uint _callbackMessage;

	private nint _hwndTray = IntPtr.Zero;

	private nint _hIcon = IntPtr.Zero;

	private List<ServerProfile>? _cachedProfiles;

	private WNDPROC? _wndProcDelegate;

	/// <summary>
	/// True only after Windows accepted the notification icon. The window-close
	/// handler must use this value: a helper window alone does not guarantee
	/// that the user can restore a hidden main window.
	/// </summary>
	public bool IsAvailable { get; private set; }

	private static TrayManager? _current;

	private static readonly object _currentLock = new object();

	private static MonitoringWindow? _monitoringWindow;

	private nint _classPtr = IntPtr.Zero;

	private const uint NIF_ICON = 2u;

	private const uint NIF_MESSAGE = 1u;

	private const uint NIF_TIP = 4u;

	private const uint NIM_ADD = 0u;

	private const uint NIM_MODIFY = 1u;

	private const uint NIM_DELETE = 2u;

	private const uint MF_STRING = 0u;

	private const uint MF_POPUP = 16u;

	private const uint MF_SEPARATOR = 2048u;

	private const uint MF_GRAYED = 1u;

	private const uint TPM_RETURNCMD = 256u;

	private const uint TPM_RIGHTBUTTON = 2u;

	private const uint WS_POPUP = 2147483648u;

	private const uint WM_RBUTTONUP = 517u;

	private const uint WM_CONTEXTMENU = 121u;

	private const uint WM_LBUTTONDBLCLK = 515u;

	public TrayManager(ISettings settings, IServerManager serverManager, IProfileManager profileManager, ILocalizationService loc, ILogService log, Action showWindow, Action<bool> requestExit)
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
		IsAvailable = false;
		lock (_currentLock)
		{
			_current = this;
		}
		_hwndTray = CreateHiddenWindow();
		if (_hwndTray != IntPtr.Zero)
		{
			_hIcon = CreateTrayIcon();
			_nid.Size = (uint)Marshal.SizeOf<NOTIFYICONDATA>();
			_nid.Wnd = _hwndTray;
			_nid.UID = 1u;
			_nid.UCallbackMessage = _callbackMessage;
			_nid.UFlags = 7u;
			_nid.HIcon = _hIcon;
			_nid.szTip = new byte[128];
			byte[] bytes = Encoding.Unicode.GetBytes(_loc.T("tray.tooltip") + "\0");
			Array.Copy(bytes, _nid.szTip, bytes.Length);
			IsAvailable = Shell_NotifyIcon(0u, ref _nid);
			if (!IsAvailable)
			{
				_log.Warning("Windows did not accept the notification-area icon", "Tray");
			}
		}
	}

	private static nint CreateTrayIcon()
	{
		try
		{
			string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app-icon.ico");
			if (File.Exists(text))
			{
				return ExtractAssociatedIcon(IntPtr.Zero, text, 0u);
			}
		}
		catch
		{
		}
		try
		{
			using Bitmap bitmap = new Bitmap(32, 32);
			using Graphics graphics = Graphics.FromImage(bitmap);
			graphics.Clear(Color.Transparent);
			using LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, 32, 32), Color.FromArgb(160, 50, 255), Color.FromArgb(80, 100, 255), LinearGradientMode.Vertical);
			using Pen pen = new Pen(brush, 4f)
			{
				StartCap = LineCap.Round,
				EndCap = LineCap.Round
			};
			graphics.DrawLine(pen, 10, 6, 10, 26);
			graphics.DrawLine(pen, 22, 6, 22, 26);
			graphics.DrawLine(pen, 10, 16, 22, 16);
			return bitmap.GetHicon();
		}
		catch
		{
		}
		return IntPtr.Zero;
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern nint ExtractAssociatedIcon(nint hInst, string lpIconPath, uint iconIndex);

	private nint CreateHiddenWindow()
	{
		string text = "LlamaStudioTrayWindow";
		nint functionPointerForDelegate = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate = TrayWndProc);
		nint hINSTANCE = Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]);
		_classPtr = Marshal.StringToHGlobalUni(text);
		WNDCLASSW lpWndClass = new WNDCLASSW
		{
			Style = 0u,
			lpfnWndProc = functionPointerForDelegate,
			CBandClassExtra = 0,
			CBandWindowExtra = 0,
			HInstance = hINSTANCE,
			hIcon = IntPtr.Zero,
			hCursor = IntPtr.Zero,
			hbrBackground = IntPtr.Zero,
			lpszMenuName = IntPtr.Zero,
			lpszClassName = _classPtr
		};
		RegisterClassW(ref lpWndClass);
		return CreateWindowEx(0u, text, string.Empty, 2147483648u, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, hINSTANCE, IntPtr.Zero);
	}

	private static nint TrayWndProc(nint hwnd, uint uMsg, nint wParam, nint lParam)
	{
		TrayManager current;
		lock (_currentLock)
		{
			current = _current;
		}
		if (current != null && uMsg == current._callbackMessage)
		{
			switch ((uint)lParam)
			{
			case 121u:
			case 517u:
			{
				GetCursorPos(out var pt);
				Dispatcher.UIThread.Post(delegate
				{
					current.ShowContextMenu(new Vector(pt.X, pt.Y));
				});
				break;
			}
			case 515u:
				Dispatcher.UIThread.Post(current._showWindow);
				break;
			}
			return IntPtr.Zero;
		}
		return DefWindowProc(hwnd, uMsg, wParam, lParam);
	}

	public void Dispose()
	{
		if (!_isDisposed)
		{
			_isDisposed = true;
			IsAvailable = false;
			lock (_currentLock)
			{
				_current = null;
			}
			try
			{
				Shell_NotifyIcon(2u, ref _nid);
			}
			catch
			{
			}
			if (_hIcon != IntPtr.Zero)
			{
				DestroyIcon(_hIcon);
				_hIcon = IntPtr.Zero;
			}
			if (_hwndTray != IntPtr.Zero)
			{
				DestroyWindow(_hwndTray);
				_hwndTray = IntPtr.Zero;
			}
			if (_classPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_classPtr);
				_classPtr = IntPtr.Zero;
			}
		}
	}

	public async Task ShowContextMenu(Vector screenPos)
	{
		if (_isDisposed)
		{
			return;
		}
		try
		{
			ServerStatus serverStatus = await _serverManager.GetStatusAsync();
			bool isRunning = serverStatus.State == ServerState.Running || serverStatus.State == ServerState.Starting;
			_cachedProfiles = (await _profileManager.GetAllProfilesAsync()).Where((ServerProfile p) => string.IsNullOrWhiteSpace(p.ModelPath) || File.Exists(p.ModelPath)).ToList();
			string text = _settings.LastSelectedProfileId.Trim();
			nint hMenu = CreatePopupMenu();
			nint num = CreatePopupMenu();
			if (_cachedProfiles != null && _cachedProfiles.Count > 0)
			{
				for (int i = 0; i < _cachedProfiles.Count; i++)
				{
					ServerProfile serverProfile = _cachedProfiles[i];
					string lpNewItem = ((serverProfile.Id.Trim() == text) ? ("✓ " + serverProfile.Name) : serverProfile.Name);
					AppendMenu(num, 0u, (uint)(100 + i), lpNewItem);
				}
			}
			else
			{
				AppendMenu(num, 1u, 0u, _loc.T("tray.no_profiles"));
			}
			AppendMenu(hMenu, 16u, (uint)num, _loc.T("tray.profiles"));
			AppendMenu(hMenu, 2048u, 0u, string.Empty);
			if (isRunning)
			{
				AppendMenu(hMenu, 0u, 1u, _loc.T("tray.stop_server"));
				AppendMenu(hMenu, 0u, 2u, _loc.T("tray.restart_server"));
			}
			else
			{
				AppendMenu(hMenu, 0u, 1u, _loc.T("tray.start_server"));
			}
			AppendMenu(hMenu, 2048u, 0u, string.Empty);
			AppendMenu(hMenu, 0u, 5u, _loc.T("tray.monitoring"));
			AppendMenu(hMenu, 0u, 3u, _loc.T("tray.show"));
			AppendMenu(hMenu, 0u, 4u, _loc.T("tray.exit"));
			SetForegroundWindow(_hwndTray);
			uint num2 = TrackPopupMenu(hMenu, 258u, (int)screenPos.X, (int)screenPos.Y, 0, _hwndTray, IntPtr.Zero);
			DestroyMenu(hMenu);
			if (num2 != 0)
			{
				await HandleMenuCommand(num2, isRunning);
			}
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Tray context menu error", "Tray");
		}
	}

	private async Task HandleMenuCommand(uint cmdId, bool serverRunning)
	{
		try
		{
			switch (cmdId)
			{
			case 1u:
			{
				if (serverRunning)
				{
					await _serverManager.StopAsync();
					return;
				}
				ServerProfile serverProfile2 = await GetActiveProfileAsync();
				if (serverProfile2 != null)
				{
					await _serverManager.StartAsync(serverProfile2);
				}
				return;
			}
			case 2u:
			{
				await _serverManager.StopAsync();
				ServerProfile serverProfile = await GetActiveProfileAsync();
				if (serverProfile != null)
				{
					await _serverManager.StartAsync(serverProfile);
				}
				return;
			}
			case 3u:
				Dispatcher.UIThread.Post(_showWindow);
				return;
			case 4u:
				_requestExit(obj: true);
				return;
			case 5u:
				Dispatcher.UIThread.Post(ToggleMonitoringWindow);
				return;
			case 0u:
			case 6u:
			case 7u:
			case 8u:
			case 9u:
			case 10u:
			case 11u:
			case 12u:
			case 13u:
			case 14u:
			case 15u:
			case 16u:
			case 17u:
			case 18u:
			case 19u:
			case 20u:
			case 21u:
			case 22u:
			case 23u:
			case 24u:
			case 25u:
			case 26u:
			case 27u:
			case 28u:
			case 29u:
			case 30u:
			case 31u:
			case 32u:
			case 33u:
			case 34u:
			case 35u:
			case 36u:
			case 37u:
			case 38u:
			case 39u:
			case 40u:
			case 41u:
			case 42u:
			case 43u:
			case 44u:
			case 45u:
			case 46u:
			case 47u:
			case 48u:
			case 49u:
			case 50u:
			case 51u:
			case 52u:
			case 53u:
			case 54u:
			case 55u:
			case 56u:
			case 57u:
			case 58u:
			case 59u:
			case 60u:
			case 61u:
			case 62u:
			case 63u:
			case 64u:
			case 65u:
			case 66u:
			case 67u:
			case 68u:
			case 69u:
			case 70u:
			case 71u:
			case 72u:
			case 73u:
			case 74u:
			case 75u:
			case 76u:
			case 77u:
			case 78u:
			case 79u:
			case 80u:
			case 81u:
			case 82u:
			case 83u:
			case 84u:
			case 85u:
			case 86u:
			case 87u:
			case 88u:
			case 89u:
			case 90u:
			case 91u:
			case 92u:
			case 93u:
			case 94u:
			case 95u:
			case 96u:
			case 97u:
			case 98u:
			case 99u:
				return;
			}
			if (_cachedProfiles != null)
			{
				int num = (int)(cmdId - 100);
				if (num >= 0 && num < _cachedProfiles.Count)
				{
					ServerProfile serverProfile3 = _cachedProfiles[num];
					_settings.LastSelectedProfileId = serverProfile3.Id;
					await _profileManager.NotifyProfileChanged(serverProfile3.Id);
				}
			}
		}
		catch (Exception exception)
		{
			_log.Error(exception, $"Tray menu command {cmdId} failed", "Tray");
		}
	}

	private static void ToggleMonitoringWindow()
	{
		try
		{
			if (!(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime))
			{
				return;
			}
			if (_monitoringWindow != null && _monitoringWindow.IsVisible)
			{
				_monitoringWindow.Hide();
			}
			else if (_monitoringWindow != null)
			{
				_monitoringWindow.Show();
				_monitoringWindow.Activate();
			}
			else if (App.Services?.GetService(typeof(MonitoringViewModel)) is MonitoringViewModel vm)
			{
				_monitoringWindow = new MonitoringWindow(vm);
				_monitoringWindow.Closed += delegate
				{
					_monitoringWindow = null;
				};
				_monitoringWindow.Show();
			}
		}
		catch
		{
		}
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA pData);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern uint RegisterWindowMessage(string lpString);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern ushort RegisterClassW(ref WNDCLASSW lpWndClass);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern nint CreateWindowEx(uint dwExStyle, string lpClassName, string? lpWindowName, uint dwStyle, int X, int Y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

	[DllImport("user32.dll")]
	private static extern nint DefWindowProc(nint hwnd, uint uMsg, nint wParam, nint lParam);

	[DllImport("user32.dll")]
	private static extern nint DestroyWindow(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool DestroyIcon(nint hIcon);

	[DllImport("user32.dll")]
	private static extern nint CreatePopupMenu();

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern bool AppendMenu(nint hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

	[DllImport("user32.dll")]
	private static extern uint TrackPopupMenu(nint hMenu, uint uFlags, int x, int y, int reserved, nint hwnd, nint prcRect);

	[DllImport("user32.dll")]
	private static extern bool DestroyMenu(nint hMenu);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out POINT lpPoint);

	private async Task<ServerProfile?> GetActiveProfileAsync()
	{
		ServerProfile serverProfile = null;
		if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
		{
			serverProfile = await _profileManager.GetProfileAsync(_settings.LastSelectedProfileId);
		}
		if (serverProfile == null)
		{
			serverProfile = await _profileManager.GetDefaultProfileAsync();
		}
		return serverProfile;
	}
}
