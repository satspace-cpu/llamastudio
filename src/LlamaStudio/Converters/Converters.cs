using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Controls;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Models;
using System.Globalization;

namespace LlamaStudio.Converters;

public class EqualConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        if (value.Equals(parameter)) return true;

        var valueStr = value.ToString();
        var paramStr = parameter.ToString();

        if (string.IsNullOrEmpty(valueStr) || string.IsNullOrEmpty(paramStr)) return false;

        if (valueStr == paramStr) return true;

        try
        {
            return object.Equals(
                System.Convert.ChangeType(value, typeof(double)),
                System.Convert.ChangeType(parameter, typeof(double)));
        }
        catch { return false; }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return parameter;
    }
}

public class BoolToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parts = parameter?.ToString()?.Split('|');
        if (value is bool b)
        {
            if (b)
                return (parts != null && parts.Length >= 1) ? parts[0] : "True";
            return (parts != null && parts.Length >= 2) ? parts[1] : "False";
        }
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return false;
    }
}

public class GreaterThanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IComparable comparable && parameter != null)
            return comparable.CompareTo(System.Convert.ChangeType(parameter, value.GetType())) > 0;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class NotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null && !string.IsNullOrEmpty(value.ToString());
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class InvertedBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b && !b;
   }
}

public class ChatRoleToBrushConverter : IValueConverter
{
    static readonly Dictionary<ChatRole, SolidColorBrush> _brushes = new()
    {
        { ChatRole.System, new SolidColorBrush(Color.Parse("#64748B")) },
        { ChatRole.User, new SolidColorBrush(Color.Parse("#4338CA")) },
        { ChatRole.Assistant, new SolidColorBrush(Color.Parse("#059669")) },
        { ChatRole.Tool, new SolidColorBrush(Color.Parse("#7C3AED")) },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChatRole role && _brushes.TryGetValue(role, out var brush))
            return brush;
        return _brushes[ChatRole.Assistant];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class ChatRoleToInitialConverter : IValueConverter
{
    static readonly Dictionary<ChatRole, string> _initials = new()
    {
        { ChatRole.System, "S" },
        { ChatRole.User, "U" },
        { ChatRole.Assistant, "A" },
        { ChatRole.Tool, "T" },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChatRole role && _initials.TryGetValue(role, out var initial))
            return initial;
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class ChatRoleToNameConverter : IValueConverter
{
    static readonly Dictionary<ChatRole, string> _names = new()
    {
        { ChatRole.System, "System" },
        { ChatRole.User, "You" },
        { ChatRole.Assistant, "Assistant" },
        { ChatRole.Tool, "Tool" },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChatRole role && _names.TryGetValue(role, out var name))
            return name;
        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class ChatRoleTextBrushConverter : IValueConverter
{
    static readonly Dictionary<ChatRole, SolidColorBrush> _brushes = new()
    {
        { ChatRole.System, new SolidColorBrush(Color.Parse("#94A3B8")) },
        { ChatRole.User, new SolidColorBrush(Color.Parse("#E2E8F0")) },
        { ChatRole.Assistant, new SolidColorBrush(Color.Parse("#E2E8F0")) },
        { ChatRole.Tool, new SolidColorBrush(Color.Parse("#A78BFA")) },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChatRole role && _brushes.TryGetValue(role, out var brush))
            return brush;
        return _brushes[ChatRole.Assistant];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class NullVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}


    public class NotEqualConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null && parameter == null) return false;
            if (value == null || parameter == null) return true;
            if (value.Equals(parameter)) return false;

            var valueStr = value.ToString();
            var paramStr = parameter.ToString();
            if (string.IsNullOrEmpty(valueStr) || string.IsNullOrEmpty(paramStr))
                return string.IsNullOrEmpty(valueStr) != string.IsNullOrEmpty(paramStr);

            if (valueStr == paramStr) return false;

            try
            {
                return !object.Equals(
                    System.Convert.ChangeType(value, typeof(double)),
                    System.Convert.ChangeType(parameter, typeof(double)));
            }
            catch { return true; }
        }
  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

/// <summary>Chevron text for reasoning toggle: collapsed→▶, expanded→▼</summary>
public class ReasoningChevronConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool collapsed)
            return collapsed ? "▶" : "▼";
        return "▶";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}


public class HasItemsVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Collections.IEnumerable enumerable)
        {
            foreach (var _ in enumerable)
                return true;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class BuildTypeToBrushConverter : IValueConverter
{
    static readonly Dictionary<BuildType, SolidColorBrush> _brushes = new()
    {
        { BuildType.Cuda12x, new SolidColorBrush(Color.Parse("#F59E0B")) },
        { BuildType.Cuda13x, new SolidColorBrush(Color.Parse("#A855F7")) },
        { BuildType.Vulkan, new SolidColorBrush(Color.Parse("#3B82F6")) },
        { BuildType.OpenVino, new SolidColorBrush(Color.Parse("#10B981")) },
        { BuildType.Cpu, new SolidColorBrush(Color.Parse("#64748B")) },
        { BuildType.Unknown, new SolidColorBrush(Color.Parse("#64748B")) },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BuildType bt && _brushes.TryGetValue(bt, out var brush))
            return brush;
        return _brushes[BuildType.Unknown];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class StringEqualBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter == null) return null;
            var isActive = value?.ToString() == parameter.ToString();
            if (!isActive) return null;

            // Detect current theme to return appropriate active color
            var isLight = IsLightTheme();
            return new Avalonia.Media.SolidColorBrush(
                Avalonia.Media.Color.Parse(isLight ? "#D4D4D4" : "#27272A"));
        }

        static bool IsLightTheme()
        {
            try
            {
                var app = Avalonia.Application.Current;
                if (app != null)
                {
                    var variant = app.ActualThemeVariant;
                    return variant == Avalonia.Styling.ThemeVariant.Light;
                }
            }
            catch { }
            return false;
        }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class SizeFormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return bytes switch
            {
                >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GiB",
                >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MiB",
                >= 1_024 => $"{bytes / 1_024.0:F1} KiB",
                _ => $"{bytes} B"
            };
        }
        return value?.ToString() ?? "0 B";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class LogLevelBrushConverter : IValueConverter
{
    static readonly Dictionary<string, Avalonia.Media.IBrush> _brushes = new()
    {
        { "Debug", new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#64748B")) },
        { "Information", new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#38BDF8")) },
        { "Warning", new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FBBF24")) },
        { "Error", new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F87171")) },
        { "Fatal", new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF0000")) },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var level = value?.ToString();
        return _brushes.TryGetValue(level ?? string.Empty, out var brush) ? brush : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class ServerStateBrushConverter : IValueConverter
{
    static readonly Dictionary<string, Avalonia.Media.IBrush> _brushes = new()
    {
        { "Stopped", new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#64748B")) },
        { "Starting", new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FBBF24")) },
        { "Running", new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#34D399")) },
        { "Stopping", new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FBBF24")) },
        { "Error", new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F87171")) },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var state = value?.ToString();
        return _brushes.TryGetValue(state ?? string.Empty, out var brush) ? brush : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

  public class ContainsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.ToString()?.Contains(parameter.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class NotContainsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return true;
        return value.ToString()?.Contains(parameter.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase) != true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class ServerConnectionStatusConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return "Disconnected";

        var state = values[0]?.ToString();
        var modelName = values[1]?.ToString();

        return state == "Running"
            ? (!string.IsNullOrWhiteSpace(modelName) ? modelName : "Connected")
            : "Disconnected";
    }
}

 public class Base64ToImageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string base64 && !string.IsNullOrEmpty(base64))
            {
                try
                {
                    var bytes = System.Convert.FromBase64String(base64);
                    using var stream = new MemoryStream(bytes);
                    return new Avalonia.Media.Imaging.Bitmap(stream);
                }
                catch { }
            }
            return null;
        }
   public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

/// <summary>MCP chip background: active=green, inactive=gray</summary>
public class McpChipBgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool enabled && enabled)
            return "#064E3B"; // dark green bg
        return "#374151"; // gray bg
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

/// <summary>MCP chip foreground: active=green, inactive=gray</summary>
public class McpChipFgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool enabled && enabled)
            return "#10B981"; // green text
        return "#9CA3AF"; // gray text
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

/// <summary>MCP chip opacity: connected=1.0, disconnected=0.5</summary>
public class McpChipOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool connected && connected)
            return 1.0;
        return 0.5;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

/// <summary>MCP toggle button background</summary>
public class McpToggleBgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool enabled && enabled)
            return "#6366F1";
        return "#374151";
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

/// <summary>MCP toggle button foreground</summary>
public class McpToggleFgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool enabled && enabled)
            return "#FFFFFF";
        return "#9CA3AF";
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

/// <summary>Session selected background — value=SelectedSession, parameter=current session Id</summary>
public class SessionSelectedBgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChatSession selected && parameter is string id && selected.Id == id)
            return new SolidColorBrush(Avalonia.Media.Color.Parse("#2D2D3D"));
        return Avalonia.Media.Brushes.Transparent;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

/// <summary>Context progress bar color: green → yellow → red</summary>
public class ContextProgressBarColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double pct)
        {
            if (pct < 70.0) return "#10B981"; // green
            if (pct < 90.0) return "#F59E0B"; // amber
            return "#EF4444"; // red
        }
        return "#10B981";
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}


public class BookmarkBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool bookmarked = value is true;
        return new SolidColorBrush(Color.Parse(bookmarked ? "#FBBF24" : "#6B7280"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class BookmarkTooltipConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool bookmarked = value is true;
        if (parameter is string param && param.Contains('|'))
        {
            var parts = param.Split('|');
            return bookmarked ? parts[1] : parts[0];
        }
        return bookmarked ? "Remove bookmark" : "Bookmark";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class McpTransportToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is McpTransportType transport && parameter is string param)
        {
            return transport.ToString() == param;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class McpConnectedToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool connected)
        {
            return connected
                ? new SolidColorBrush(Color.Parse("#34D399"))
                : new SolidColorBrush(Color.Parse("#64748B"));
        }
        return new SolidColorBrush(Color.Parse("#64748B"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

    public class StateToStringConverter : IValueConverter
{
    static readonly Dictionary<string, string> _states = new()
    {
        { "Stopped", "⏹ Stopped" },
        { "Starting", "⏳ Starting..." },
        { "Running", "▶ Running" },
        { "Stopping", "⏳ Stopping..." },
        { "Error", "⚠ Error" },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var state = value?.ToString();
        return _states.TryGetValue(state ?? string.Empty, out var text) ? text : state;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class ChatRoleToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChatRole role && parameter is string roleName)
        {
            // Support "NotTool" to hide Tool messages
            if (roleName == "NotTool")
                return role != ChatRole.Tool;
            return role.ToString().Equals(roleName, StringComparison.OrdinalIgnoreCase);
        }
        return true; // default visible
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

/// <summary>Shows reasoning block only for Assistant messages that have reasoning content</summary>
public class ReasoningVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Core.Models.ChatMessage msg)
            return !string.IsNullOrEmpty(msg.Reasoning) && msg.Role == Core.Models.ChatRole.Assistant;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class PercentToAngleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percent)
            return Math.Min(percent / 100.0 * 360, 360);
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class PercentToDashConverter : IValueConverter
{
    // ConverterParameter: radius of the ellipse (default 36 for page, 23 for window)
    const double DefaultRadius = 36;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double radius = DefaultRadius;
        if (parameter is string p && double.TryParse(p, System.Globalization.CultureInfo.InvariantCulture, out double pr))
            radius = pr;
        else if (parameter is double rd)
            radius = rd;

        double center = radius;

        if (value is double percent && percent > 0)
        {
            double angle = (percent / 100.0) * 360.0;
            if (angle > 360) angle = 360;
            
            // Start from top (-90 degrees)
            double startAngleRad = -90 * Math.PI / 180.0;
            double endAngleRad = (startAngleRad + angle * Math.PI / 180.0);
            
            double endX = center + radius * Math.Cos(endAngleRad);
            double endY = center + radius * Math.Sin(endAngleRad);
            
            bool isLarge = angle > 180;
            
            // SVG path: Move to start, Arc to end (InvariantCulture for '.' decimal separator)
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string path = System.String.Format(inv, "M {0:F2},{1:F2} A {2:F2},{2:F2} 0 {3},1 {4:F2},{5:F2}", center, center - radius, radius, isLarge ? 1 : 0, endX, endY);
            return Avalonia.Media.StreamGeometry.Parse(path);
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class BoolToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var colors = parameter?.ToString()?.Split('|');
        if (value is bool b && colors != null && colors.Length >= 2)
            return new SolidColorBrush(Color.Parse(b ? colors[0] : colors[1]));
        return new SolidColorBrush(Color.Parse("#64748B"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class BoolToDoubleConverter : IValueConverter
{
    // Converts bool to double: true→0 (auto height), false→0 (collapsed). Used with MaxHeight.
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? 10000.0 : 0.0; // visible→large max, hidden→zero
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class BoolToGridLengthConverter : IValueConverter
{
    // Converts bool to GridLength: true→Auto, false→0 (collapsed row/column)
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? GridLength.Auto : GridLength.Parse("0");
        return GridLength.Parse("0");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}


/// <summary>Hides assistant messages that have ToolCalls but no meaningful content (intermediate tool-call-only messages)</summary>
public class MessageVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Core.Models.ChatMessage msg)
        {
            // Hide assistant messages that have tool calls but empty/whitespace-only content
            if (msg.Role == Core.Models.ChatRole.Assistant && msg.ToolCalls.Count > 0)
            {
                return string.IsNullOrWhiteSpace(msg.Content) == false;
            }
            // Always show other messages
            return true;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}


/// <summary>Shows character count for tool results (e.g. "12.4 KB")</summary>
public class ToolResultLengthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            int len = s.Length;
            return len switch
            {
                >= 1024 => $"{len / 1024.0:F1} KB",
                _ => $"{len} chars"
            };
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

/// <summary>Subtracts parameter from value (for MaxWidth calculations)</summary>
public class SubtractConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double v && parameter is string p && double.TryParse(p, out double sub))
            return v - sub;
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

