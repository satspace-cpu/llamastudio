using Avalonia;
using Avalonia.Media;

namespace LlamaStudio;

/// <summary>
/// Resolves semantic theme brushes for controls that are created in code.
/// </summary>
public static class ThemeBrushes
{
	public static IBrush Get(string resourceKey, string fallback)
	{
		if (Application.Current?.Resources.TryGetValue(resourceKey, out object? value) == true && value is IBrush brush)
		{
			return brush;
		}

		return new SolidColorBrush(Color.Parse(fallback));
	}
}
