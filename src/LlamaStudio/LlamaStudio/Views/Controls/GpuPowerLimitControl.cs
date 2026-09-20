using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.Infrastructure.Llama;

namespace LlamaStudio.Views.Controls;

/// <summary>
/// Per-GPU power limit control: check the cards, enter watts, press apply.
/// Runs <c>nvidia-smi -i &lt;id&gt; -pl &lt;watts&gt;</c> (via sudo when the
/// driver requires root). Self-contained — no view model needed.
/// </summary>
public sealed class GpuPowerLimitControl : UserControl
{
	private readonly StackPanel _root = new StackPanel { Spacing = 8 };
	private readonly StackPanel _gpuRows = new StackPanel { Spacing = 4 };
	private readonly TextBox _wattsBox = new TextBox { Width = 90, VerticalAlignment = VerticalAlignment.Center };
	private readonly TextBlock _status = new TextBlock { FontSize = 11 };
	private readonly Button _applyButton = new Button { Content = global::LlamaStudio.NativeLanguage.Text("Применить", "Apply") };
	private readonly IBrush _amber = new SolidColorBrush(Color.Parse("#F59E0B"));
	private readonly IBrush _text = new SolidColorBrush(Color.Parse("#E5E7EB"));
	private readonly IBrush _muted = new SolidColorBrush(Color.Parse("#9CA3AF"));

	private GpuPowerLimitItem[] _items = Array.Empty<GpuPowerLimitItem>();
	private bool _busy;

	// r94: per-profile power limits. The control is self-contained, so the
	// hosting page injects two hooks: resolving the active profile and saving
	// it back after an apply. Without them the control behaves as before.
	public Func<Task<ServerProfile?>>? GetActiveProfileAsync;
	public Func<ServerProfile, Task>? SaveProfileAsync;
	private Dictionary<string, string>? _profileLimits;

	public GpuPowerLimitControl()
	{
		var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		header.Children.Add(new TextBlock { Text = global::LlamaStudio.NativeLanguage.Text("Лимит мощности GPU", "GPU power limit"), FontSize = 14, FontWeight = FontWeight.DemiBold, Foreground = _amber });

		var refreshButton = new Button { Content = global::LlamaStudio.NativeLanguage.Text("Обновить", "Refresh"), FontSize = 11 };
		refreshButton.Click += async (_, _) => await RefreshAsync();
		header.Children.Add(refreshButton);

		_wattsBox.Text = "250";
		_wattsBox.Watermark = global::LlamaStudio.NativeLanguage.Text("Вт", "W");

		var wattsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		wattsRow.Children.Add(new TextBlock { Text = global::LlamaStudio.NativeLanguage.Text("Ватты:", "Watts:"), VerticalAlignment = VerticalAlignment.Center, Foreground = _muted });
		wattsRow.Children.Add(_wattsBox);
		_applyButton.Click += async (_, _) => await ApplyAsync();
		wattsRow.Children.Add(_applyButton);
		wattsRow.Children.Add(new TextBlock { Text = global::LlamaStudio.NativeLanguage.Text("к выбранным", "to selected GPUs"), VerticalAlignment = VerticalAlignment.Center, Foreground = _muted, FontSize = 11 });

		_root.Children.Add(header);
		_root.Children.Add(_gpuRows);
		_root.Children.Add(wattsRow);
		_root.Children.Add(_status);

		Content = new Border
		{
			Background = new SolidColorBrush(Color.Parse("#111827")),
			BorderBrush = new SolidColorBrush(Color.Parse("#374151")),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12),
			Child = _root
		};

		Loaded += async (_, _) => await RefreshAsync();
	}

	private async Task RefreshAsync()
	{
		try
		{
			// r94: remember what THIS profile last applied, to preselect below.
			if (GetActiveProfileAsync != null)
			{
				ServerProfile? activeProfile = await GetActiveProfileAsync();
				_profileLimits = activeProfile?.GpuPowerLimits;
			}
			_items = (await GpuPowerLimitManager.GetItemsAsync()).ToArray();
			Dispatcher.UIThread.Invoke(BuildRows);
		}
		catch (Exception ex)
		{
			_status.Text = global::LlamaStudio.NativeLanguage.Text("Ошибка чтения nvidia-smi: ", "Failed to read nvidia-smi: ") + ex.Message;
			_status.Foreground = new SolidColorBrush(Color.Parse("#EF4444"));
		}
	}

	private void BuildRows()
	{
		_gpuRows.Children.Clear();
		// r94: prefill the watts box with the profile's last applied value.
		if (_profileLimits is { Count: > 0 })
		{
			string firstWatts = _profileLimits.Values.First();
			if (double.TryParse(firstWatts.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
			{
				_wattsBox.Text = firstWatts;
			}
		}
		foreach (GpuPowerLimitItem item in _items)
		{
			var gpuRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
			// r94: preselect the cards this profile has limits for.
			bool profileHasLimit = _profileLimits != null && _profileLimits.ContainsKey(item.Index.ToString(System.Globalization.CultureInfo.InvariantCulture));
			var includeCheck = new CheckBox { IsChecked = profileHasLimit, VerticalAlignment = VerticalAlignment.Center, Tag = item.Index, Foreground = _text };
			var gpuNameLabel = new TextBlock
			{
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = _text,
				Text = $"GPU {item.Index} · {item.Name}"
			};
			// The SET limit is the value the user cares about — make it stand out.
			var limitLabel = new TextBlock
			{
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = _amber,
				FontWeight = FontWeight.SemiBold,
				Text = global::LlamaStudio.NativeLanguage.Text($"лимит {item.LimitDisplay}", $"limit {item.LimitDisplay}")
			};
			var currentLabel = new TextBlock
			{
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = _muted,
				Text = global::LlamaStudio.NativeLanguage.Text($"сейчас {item.CurrentDisplay} · диапазон {item.RangeDisplay}", $"current {item.CurrentDisplay} · range {item.RangeDisplay}")
			};
			gpuRow.Children.Add(includeCheck);
			gpuRow.Children.Add(gpuNameLabel);
			gpuRow.Children.Add(limitLabel);
			gpuRow.Children.Add(currentLabel);
			_gpuRows.Children.Add(gpuRow);
		}
		if (_items.Length == 0)
		{
			_gpuRows.Children.Add(new TextBlock { Text = global::LlamaStudio.NativeLanguage.Text("GPU не найдены", "No GPUs found"), Foreground = _muted });
		}
	}

	private static IEnumerable<Control> EnumerateDescendants(Control root)
	{
		if (root is Panel panel)
		{
			foreach (Control child in panel.Children)
			{
				yield return child;
				foreach (Control sub in EnumerateDescendants(child))
				{
					yield return sub;
				}
			}
		}
	}

	private async Task ApplyAsync()
	{
		if (_busy)
		{
			return;
		}
		string raw = _wattsBox.Text.Replace(',', '.').Trim();
		if (!double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double watts) || watts <= 0.0)
		{
			_status.Text = global::LlamaStudio.NativeLanguage.Text("Введите число ватт, например 250", "Enter a watt value, for example 250");
			_status.Foreground = new SolidColorBrush(Color.Parse("#EF4444"));
			return;
		}
		CheckBox[] checks = EnumerateDescendants(_gpuRows).OfType<CheckBox>().ToArray();
		if (checks.Length == 0)
		{
			_status.Text = global::LlamaStudio.NativeLanguage.Text("Сначала загрузите список GPU", "Load the GPU list first");
			_status.Foreground = new SolidColorBrush(Color.Parse("#EF4444"));
			return;
		}
		_busy = true;
		_applyButton.IsEnabled = false;
		_status.Text = global::LlamaStudio.NativeLanguage.Text("Применяю...", "Applying...");
		_status.Foreground = _muted;
		List<string> messages = new List<string>();
		bool anyFailed = false;
		try
		{
			foreach (CheckBox check in checks.Where(c => c.IsChecked == true))
			{
				int index = check.Tag is int i ? i : -1;
				if (index < 0)
				{
					continue;
				}
				// r89: pass the queried range for a client-side pre-check.
				GpuPowerLimitItem? item = _items.FirstOrDefault(x => x.Index == index);
				(bool ok, string message) = await GpuPowerLimitManager.SetPowerLimitAsync(index, watts, item?.MinWatts, item?.MaxWatts);
				if (!ok)
				{
					anyFailed = true;
				}
				messages.Add(message);
			}
			_status.Text = messages.Count > 0 ? string.Join("  |  ", messages) : global::LlamaStudio.NativeLanguage.Text("Нет выбранных GPU", "No GPUs selected");
			_status.Foreground = anyFailed
				? new SolidColorBrush(Color.Parse("#EF4444"))
				: new SolidColorBrush(Color.Parse("#10B981"));
			// r94: remember the applied limits in the active profile so a
			// different profile can keep a different limit for the same cards.
			if (!anyFailed && GetActiveProfileAsync != null && SaveProfileAsync != null)
			{
				try
				{
					ServerProfile? activeProfile = await GetActiveProfileAsync();
					if (activeProfile != null)
					{
						Dictionary<string, string> applied = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
						foreach (CheckBox check in checks.Where(c => c.IsChecked == true))
						{
							if (check.Tag is int appliedIndex && appliedIndex >= 0)
							{
								applied[appliedIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)] = watts.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
							}
						}
						activeProfile.GpuPowerLimits = applied;
						await SaveProfileAsync(activeProfile);
					}
				}
				catch
				{
					// best effort — the driver state is already applied
				}
			}
			await RefreshAsync();
		}
		catch (Exception ex)
		{
			_status.Text = global::LlamaStudio.NativeLanguage.Text("Ошибка: ", "Error: ") + ex.Message;
			_status.Foreground = new SolidColorBrush(Color.Parse("#EF4444"));
		}
		finally
		{
			_busy = false;
			_applyButton.IsEnabled = true;
		}
	}
}
