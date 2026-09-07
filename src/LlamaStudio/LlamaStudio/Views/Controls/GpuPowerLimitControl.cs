using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
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
	private readonly Button _applyButton = new Button { Content = "Применить" };
	private readonly IBrush _amber = new SolidColorBrush(Color.Parse("#F59E0B"));
	private readonly IBrush _text = new SolidColorBrush(Color.Parse("#E5E7EB"));
	private readonly IBrush _muted = new SolidColorBrush(Color.Parse("#9CA3AF"));

	private GpuPowerLimitItem[] _items = Array.Empty<GpuPowerLimitItem>();
	private bool _busy;

	public GpuPowerLimitControl()
	{
		var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		header.Children.Add(new TextBlock { Text = "Лимит мощности GPU", FontSize = 14, FontWeight = FontWeight.DemiBold, Foreground = _amber });

		var refreshButton = new Button { Content = "Обновить", FontSize = 11 };
		refreshButton.Click += async (_, _) => await RefreshAsync();
		header.Children.Add(refreshButton);

		_wattsBox.Text = "250";
		_wattsBox.Watermark = "Вт";

		var wattsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		wattsRow.Children.Add(new TextBlock { Text = "Ватты:", VerticalAlignment = VerticalAlignment.Center, Foreground = _muted });
		wattsRow.Children.Add(_wattsBox);
		_applyButton.Click += async (_, _) => await ApplyAsync();
		wattsRow.Children.Add(_applyButton);
		wattsRow.Children.Add(new TextBlock { Text = "к выбранным", VerticalAlignment = VerticalAlignment.Center, Foreground = _muted, FontSize = 11 });

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
			_items = (await GpuPowerLimitManager.GetItemsAsync()).ToArray();
			Dispatcher.UIThread.Invoke(BuildRows);
		}
		catch (Exception ex)
		{
			_status.Text = "Ошибка чтения nvidia-smi: " + ex.Message;
			_status.Foreground = new SolidColorBrush(Color.Parse("#EF4444"));
		}
	}

	private void BuildRows()
	{
		_gpuRows.Children.Clear();
		foreach (GpuPowerLimitItem item in _items)
		{
			var gpuRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
			var includeCheck = new CheckBox { IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Tag = item.Index, Foreground = _text };
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
				Text = $"лимит {item.LimitDisplay}"
			};
			var currentLabel = new TextBlock
			{
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = _muted,
				Text = $"сейчас {item.CurrentDisplay} · диапазон {item.RangeDisplay}"
			};
			gpuRow.Children.Add(includeCheck);
			gpuRow.Children.Add(gpuNameLabel);
			gpuRow.Children.Add(limitLabel);
			gpuRow.Children.Add(currentLabel);
			_gpuRows.Children.Add(gpuRow);
		}
		if (_items.Length == 0)
		{
			_gpuRows.Children.Add(new TextBlock { Text = "GPU не найдены", Foreground = _muted });
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
			_status.Text = "Введите число ватт, например 250";
			_status.Foreground = new SolidColorBrush(Color.Parse("#EF4444"));
			return;
		}
		CheckBox[] checks = EnumerateDescendants(_gpuRows).OfType<CheckBox>().ToArray();
		if (checks.Length == 0)
		{
			_status.Text = "Сначала загрузите список GPU";
			_status.Foreground = new SolidColorBrush(Color.Parse("#EF4444"));
			return;
		}
		_busy = true;
		_applyButton.IsEnabled = false;
		_status.Text = "Применяю...";
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
				(bool ok, string message) = await GpuPowerLimitManager.SetPowerLimitAsync(index, watts);
				if (!ok)
				{
					anyFailed = true;
				}
				messages.Add(message);
			}
			_status.Text = messages.Count > 0 ? string.Join("  |  ", messages) : "Нет выбранных GPU";
			_status.Foreground = anyFailed
				? new SolidColorBrush(Color.Parse("#EF4444"))
				: new SolidColorBrush(Color.Parse("#10B981"));
			await RefreshAsync();
		}
		catch (Exception ex)
		{
			_status.Text = "Ошибка: " + ex.Message;
			_status.Foreground = new SolidColorBrush(Color.Parse("#EF4444"));
		}
		finally
		{
			_busy = false;
			_applyButton.IsEnabled = true;
		}
	}
}
