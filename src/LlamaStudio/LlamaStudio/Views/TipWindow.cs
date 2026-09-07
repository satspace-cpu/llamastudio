using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using CompiledAvaloniaXaml;

namespace LlamaStudio.Views;

[CompilerGenerated]
public class TipWindow : Window
{
	[GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.1.0")]
	internal Window Root;

	[GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.1.0")]
	internal TextBlock TipText;

	private static Action<object> _0021XamlIlPopulateOverride;

	public TipWindow()
	{
		InitializeComponent();
	}

	public void SetTip(string tip, string? title = null)
	{
		TextBlock tipLabel = this.FindControl<TextBlock>("TipText");
		if (tipLabel != null)
		{
			tipLabel.Text = tip;
		}
		if (!string.IsNullOrEmpty(title))
		{
			base.Title = title;
		}
	}

	[GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.1.0")]
	[ExcludeFromCodeCoverage]
	public void InitializeComponent(bool loadXaml = true, bool attachDevTools = true)
	{
		if (loadXaml)
		{
			_0021XamlIlPopulateTrampoline(this);
		}
		INameScope nameScope = this.FindNameScope();
		Root = nameScope?.Find<Window>("Root");
		TipText = nameScope?.Find<TextBlock>("TipText");
	}

	private static void _0021XamlIlPopulate(IServiceProvider serviceProvider, TipWindow window)
	{
		CompiledAvaloniaXaml.XamlIlContext.Context<TipWindow> context = new CompiledAvaloniaXaml.XamlIlContext.Context<TipWindow>(serviceProvider, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FViews_002FTipWindow_002Eaxaml.Singleton }, "avares://LlamaStudio/Views/TipWindow.axaml")
		{
			RootObject = window,
			IntermediateRoot = window
		};
		((ISupportInitialize)window).BeginInit();
		window.Name = "Root";
		object elementToRegister = window;
		context.AvaloniaNameScope.Register("Root", elementToRegister);
		window.Title = "Tip";
		window.Width = 480.0;
		window.Height = 350.0;
		window.MaxHeight = 520.0;
		window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		window.CanResize = false;
		window.SystemDecorations = SystemDecorations.None;
		window.Background = new ImmutableSolidColorBrush(4279179050u);
		Border contentBorder;
		Border contentBorderBase = (contentBorder = new Border());
		((ISupportInitialize)contentBorderBase).BeginInit();
		window.Content = contentBorderBase;
		contentBorder.Background = new ImmutableSolidColorBrush(4279179050u);
		contentBorder.BorderBrush = new ImmutableSolidColorBrush(4284704497u);
		contentBorder.BorderThickness = new Thickness(1.5, 1.5, 1.5, 1.5);
		contentBorder.CornerRadius = new CornerRadius(8.0, 8.0, 8.0, 8.0);
		contentBorder.Margin = new Thickness(4.0, 4.0, 4.0, 4.0);
		ScrollViewer tipScrollViewer;
		ScrollViewer tipScrollViewerBase = (tipScrollViewer = new ScrollViewer());
		((ISupportInitialize)tipScrollViewerBase).BeginInit();
		contentBorder.Child = tipScrollViewerBase;
		tipScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
		tipScrollViewer.Padding = new Thickness(16.0, 12.0, 16.0, 12.0);
		TextBlock tipTextBlock;
		TextBlock tipTextBlockBase = (tipTextBlock = new TextBlock());
		((ISupportInitialize)tipTextBlockBase).BeginInit();
		tipScrollViewer.Content = tipTextBlockBase;
		tipTextBlock.Name = "TipText";
		elementToRegister = tipTextBlock;
		context.AvaloniaNameScope.Register("TipText", elementToRegister);
		tipTextBlock.TextWrapping = TextWrapping.Wrap;
		tipTextBlock.Foreground = new ImmutableSolidColorBrush(4293060848u);
		tipTextBlock.FontSize = 13.0;
		tipTextBlock.LineHeight = 19.0;
		((ISupportInitialize)tipTextBlock).EndInit();
		((ISupportInitialize)tipScrollViewer).EndInit();
		((ISupportInitialize)contentBorder).EndInit();
		((ISupportInitialize)window).EndInit();
		if (window is StyledElement styled)
		{
			NameScope.SetNameScope(styled, context.AvaloniaNameScope);
		}
		context.AvaloniaNameScope.Complete();
	}

	private static void _0021XamlIlPopulateTrampoline(TipWindow window)
	{
		if (_0021XamlIlPopulateOverride != null)
		{
			_0021XamlIlPopulateOverride(window);
		}
		else
		{
			_0021XamlIlPopulate(XamlIlRuntimeHelpers.CreateRootServiceProviderV3(null), window);
		}
	}
}
