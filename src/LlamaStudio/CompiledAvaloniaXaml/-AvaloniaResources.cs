using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Markup.Xaml.XamlIl.Runtime;

namespace CompiledAvaloniaXaml;

[EditorBrowsable(EditorBrowsableState.Never)]
[CompilerGenerated]
public class _0021AvaloniaResources
{
	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FApp_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(5)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"local",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio", null) }
				},
				{
					"converters",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.Converters", null) }
				},
				{
					"materialIcons",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("Material.Icons.Avalonia", "Material.Icons.Avalonia") }
				}
			};
		}

		static NamespaceInfo_003A_002FApp_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FApp_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FControls_002FTpsGaugeControl_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(3)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				}
			};
		}

		static NamespaceInfo_003A_002FControls_002FTpsGaugeControl_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FControls_002FTpsGaugeControl_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FArgPickerWindow_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(4)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				},
				{
					"models",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.Core.Models", null) }
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FArgPickerWindow_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FArgPickerWindow_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FMainWindow_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(7)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				},
				{
					"views",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.Views.Pages", null) }
				},
				{
					"materialIcons",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("Material.Icons.Avalonia", "Material.Icons.Avalonia") }
				},
				{
					"d",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"mc",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FMainWindow_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FMainWindow_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FMonitoringWindow_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(5)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				},
				{
					"controls",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.Controls", null) }
				},
				{
					"converters",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.Converters", null) }
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FMonitoringWindow_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FMonitoringWindow_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FPages_002FApiTestPage_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(5)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				},
				{
					"d",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"mc",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FPages_002FApiTestPage_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FPages_002FApiTestPage_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FPages_002FChatPage_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(8)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				},
				{
					"models",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.Core.Models", null) }
				},
				{
					"materialIcons",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("Material.Icons.Avalonia", "Material.Icons.Avalonia") }
				},
				{
					"d",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"mc",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"md",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("Markdown.Avalonia.Full", "Markdown.Avalonia.Full") }
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FPages_002FChatPage_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FPages_002FChatPage_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FPages_002FCliArgsPickerWindow_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(5)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"d",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"mc",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FPages_002FCliArgsPickerWindow_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FPages_002FCliArgsPickerWindow_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FPages_002FDashboardPage_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(6)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				},
				{
					"core",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.Core.Models", null) }
				},
				{
					"d",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"mc",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FPages_002FDashboardPage_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FPages_002FDashboardPage_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FPages_002FLlamaReleasesPage_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(3)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FPages_002FLlamaReleasesPage_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FPages_002FLlamaReleasesPage_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FPages_002FLogsPage_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(5)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				},
				{
					"d",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"mc",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FPages_002FLogsPage_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FPages_002FLogsPage_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FPages_002FModelsPage_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(5)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				},
				{
					"d",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"mc",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FPages_002FModelsPage_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FPages_002FModelsPage_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FPages_002FMonitoringPage_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(4)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				},
				{
					"converters",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.Converters", null) }
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FPages_002FMonitoringPage_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FPages_002FMonitoringPage_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FPages_002FProfilesPage_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(5)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				},
				{
					"d",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"mc",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FPages_002FProfilesPage_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FPages_002FProfilesPage_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FPages_002FServerPage_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(6)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				},
				{
					"core",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.Core.Models", null) }
				},
				{
					"d",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"mc",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FPages_002FServerPage_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FPages_002FServerPage_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FPages_002FSettingsPage_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(5)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				},
				{
					"d",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"mc",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FPages_002FSettingsPage_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FPages_002FSettingsPage_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FPages_002FSupportPage_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(3)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				},
				{
					"vm",
					new AvaloniaXamlIlXmlNamespaceInfo[1] { CreateNamespaceInfo("LlamaStudio.ViewModels", null) }
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FPages_002FSupportPage_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FPages_002FSupportPage_002Eaxaml();
		}
	}

	[CompilerGenerated]
	internal class NamespaceInfo_003A_002FViews_002FTipWindow_002Eaxaml : IAvaloniaXamlIlXmlNamespaceInfoProvider
	{
		private IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> _xmlNamespaces;

		public static IAvaloniaXamlIlXmlNamespaceInfoProvider Singleton;

		public virtual IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces
		{
			get
			{
				if (_xmlNamespaces == null)
				{
					_xmlNamespaces = CreateNamespaces();
				}
				return _xmlNamespaces;
			}
		}

		private static AvaloniaXamlIlXmlNamespaceInfo CreateNamespaceInfo(string P_0, string P_1)
		{
			return new AvaloniaXamlIlXmlNamespaceInfo
			{
				ClrNamespace = P_0,
				ClrAssemblyName = P_1
			};
		}

		private static IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> CreateNamespaces()
		{
			return new Dictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>>(2)
			{
				{
					"",
					new AvaloniaXamlIlXmlNamespaceInfo[41]
					{
						CreateNamespaceInfo("Avalonia", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Animation.Easings", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Data.Converters", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.GestureRecognizers", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Input.TextInput", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Layout", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.LogicalTree", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Imaging", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Media.Transformation", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Styling", "Avalonia.Base"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.ColorPicker"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Collections", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls.DataGrid"),
						CreateNamespaceInfo("Avalonia", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Automation", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Embedding", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Presenters", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Primitives", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Shapes", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Templates", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Notifications", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Chrome", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Controls.Documents", "Avalonia.Controls"),
						CreateNamespaceInfo("Avalonia.Fonts.Inter", "Avalonia.Fonts.Inter"),
						CreateNamespaceInfo("Avalonia.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Data", "Avalonia.Markup"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.MarkupExtensions", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Styling", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Markup.Xaml.Templates", "Avalonia.Markup.Xaml"),
						CreateNamespaceInfo("Avalonia.Svg", "Avalonia.Svg"),
						CreateNamespaceInfo("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent"),
						CreateNamespaceInfo("Avalonia.Themes.Simple", "Avalonia.Themes.Simple")
					}
				},
				{
					"x",
					new AvaloniaXamlIlXmlNamespaceInfo[0]
				}
			};
		}

		static NamespaceInfo_003A_002FViews_002FTipWindow_002Eaxaml()
		{
			Singleton = new NamespaceInfo_003A_002FViews_002FTipWindow_002Eaxaml();
		}
	}
}
