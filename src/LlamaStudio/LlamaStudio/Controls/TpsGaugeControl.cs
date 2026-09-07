using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using CompiledAvaloniaXaml;

namespace LlamaStudio.Controls;

[CompilerGenerated]
public class TpsGaugeControl : UserControl
{
	private static Action<object> _0021XamlIlPopulateOverride;

	public TpsGaugeControl()
	{
		InitializeComponent();
	}

	[GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.1.0")]
	[ExcludeFromCodeCoverage]
	public void InitializeComponent(bool loadXaml = true)
	{
		if (loadXaml)
		{
			_0021XamlIlPopulateTrampoline(this);
		}
	}

	private static void _0021XamlIlPopulate(IServiceProvider P_0, TpsGaugeControl P_1)
	{
		CompiledAvaloniaXaml.XamlIlContext.Context<TpsGaugeControl> context = new CompiledAvaloniaXaml.XamlIlContext.Context<TpsGaugeControl>(P_0, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FControls_002FTpsGaugeControl_002Eaxaml.Singleton }, "avares://LlamaStudio/Controls/TpsGaugeControl.axaml")
		{
			RootObject = P_1,
			IntermediateRoot = P_1
		};
		((ISupportInitialize)P_1).BeginInit();
		context.PushParent(P_1);
		Border border2;
		Border border = (border2 = new Border());
		((ISupportInitialize)border).BeginInit();
		P_1.Content = border;
		Border border4;
		Border border3 = (border4 = border2);
		context.PushParent(border4);
		border4.Background = new ImmutableSolidColorBrush(4279900718u);
		border4.CornerRadius = new CornerRadius(12.0, 12.0, 12.0, 12.0);
		border4.Padding = new Thickness(16.0, 12.0, 16.0, 12.0);
		StackPanel stackPanel2;
		StackPanel stackPanel = (stackPanel2 = new StackPanel());
		((ISupportInitialize)stackPanel).BeginInit();
		border4.Child = stackPanel;
		StackPanel stackPanel4;
		StackPanel stackPanel3 = (stackPanel4 = stackPanel2);
		context.PushParent(stackPanel4);
		StackPanel stackPanel5 = stackPanel4;
		stackPanel5.Spacing = 8.0;
		stackPanel5.HorizontalAlignment = HorizontalAlignment.Center;
		Avalonia.Controls.Controls children = stackPanel5.Children;
		Grid grid2;
		Grid grid = (grid2 = new Grid());
		((ISupportInitialize)grid).BeginInit();
		children.Add(grid);
		Grid grid4;
		Grid grid3 = (grid4 = grid2);
		context.PushParent(grid4);
		ColumnDefinitions columnDefinitions = new ColumnDefinitions();
		columnDefinitions.Capacity = 3;
		columnDefinitions.Add(new ColumnDefinition(new GridLength(1.0, GridUnitType.Star)));
		columnDefinitions.Add(new ColumnDefinition(new GridLength(0.0, GridUnitType.Auto)));
		columnDefinitions.Add(new ColumnDefinition(new GridLength(1.0, GridUnitType.Star)));
		grid4.ColumnDefinitions = columnDefinitions;
		grid4.HorizontalAlignment = HorizontalAlignment.Stretch;
		Avalonia.Controls.Controls children2 = grid4.Children;
		StackPanel stackPanel7;
		StackPanel stackPanel6 = (stackPanel7 = new StackPanel());
		((ISupportInitialize)stackPanel6).BeginInit();
		children2.Add(stackPanel6);
		StackPanel stackPanel8 = (stackPanel4 = stackPanel7);
		context.PushParent(stackPanel4);
		StackPanel stackPanel9 = stackPanel4;
		Grid.SetColumn(stackPanel9, 0);
		stackPanel9.Spacing = 2.0;
		stackPanel9.HorizontalAlignment = HorizontalAlignment.Right;
		Avalonia.Controls.Controls children3 = stackPanel9.Children;
		TextBlock textBlock2;
		TextBlock textBlock = (textBlock2 = new TextBlock());
		((ISupportInitialize)textBlock).BeginInit();
		children3.Add(textBlock);
		TextBlock textBlock4;
		TextBlock textBlock3 = (textBlock4 = textBlock2);
		context.PushParent(textBlock4);
		TextBlock textBlock5 = textBlock4;
		StyledProperty<string?> textProperty = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding binding = new ReflectionBindingExtension("GenLabel").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(textBlock5, textProperty, binding);
		textBlock5.FontSize = 10.0;
		textBlock5.Foreground = new ImmutableSolidColorBrush(4294688548u);
		textBlock5.HorizontalAlignment = HorizontalAlignment.Center;
		context.PopParent();
		((ISupportInitialize)textBlock3).EndInit();
		Avalonia.Controls.Controls children4 = stackPanel9.Children;
		StackPanel stackPanel11;
		StackPanel stackPanel10 = (stackPanel11 = new StackPanel());
		((ISupportInitialize)stackPanel10).BeginInit();
		children4.Add(stackPanel10);
		StackPanel stackPanel12 = (stackPanel4 = stackPanel11);
		context.PushParent(stackPanel4);
		StackPanel stackPanel13 = stackPanel4;
		stackPanel13.Orientation = Orientation.Horizontal;
		stackPanel13.Spacing = 4.0;
		stackPanel13.HorizontalAlignment = HorizontalAlignment.Center;
		Avalonia.Controls.Controls children5 = stackPanel13.Children;
		TextBlock textBlock7;
		TextBlock textBlock6 = (textBlock7 = new TextBlock());
		((ISupportInitialize)textBlock6).BeginInit();
		children5.Add(textBlock6);
		TextBlock textBlock8 = (textBlock4 = textBlock7);
		context.PushParent(textBlock4);
		TextBlock textBlock9 = textBlock4;
		StyledProperty<string?> textProperty2 = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding binding2 = new ReflectionBindingExtension("TpsText").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(textBlock9, textProperty2, binding2);
		textBlock9.FontSize = 32.0;
		textBlock9.FontWeight = FontWeight.Bold;
		textBlock9.Foreground = new ImmutableSolidColorBrush(4294688548u);
		textBlock9.VerticalAlignment = VerticalAlignment.Center;
		context.PopParent();
		((ISupportInitialize)textBlock8).EndInit();
		Avalonia.Controls.Controls children6 = stackPanel13.Children;
		TextBlock textBlock11;
		TextBlock textBlock10 = (textBlock11 = new TextBlock());
		((ISupportInitialize)textBlock10).BeginInit();
		children6.Add(textBlock10);
		textBlock11.Text = "tok/s";
		textBlock11.FontSize = 12.0;
		textBlock11.Foreground = new ImmutableSolidColorBrush(4287931320u);
		textBlock11.VerticalAlignment = VerticalAlignment.Bottom;
		textBlock11.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
		((ISupportInitialize)textBlock11).EndInit();
		context.PopParent();
		((ISupportInitialize)stackPanel12).EndInit();
		context.PopParent();
		((ISupportInitialize)stackPanel8).EndInit();
		Avalonia.Controls.Controls children7 = grid4.Children;
		Border border6;
		Border border5 = (border6 = new Border());
		((ISupportInitialize)border5).BeginInit();
		children7.Add(border5);
		Grid.SetColumn(border6, 1);
		border6.Width = 1.0;
		border6.Background = new ImmutableSolidColorBrush(4281549141u);
		border6.Margin = new Thickness(12.0, 4.0, 12.0, 4.0);
		((ISupportInitialize)border6).EndInit();
		Avalonia.Controls.Controls children8 = grid4.Children;
		StackPanel stackPanel15;
		StackPanel stackPanel14 = (stackPanel15 = new StackPanel());
		((ISupportInitialize)stackPanel14).BeginInit();
		children8.Add(stackPanel14);
		StackPanel stackPanel16 = (stackPanel4 = stackPanel15);
		context.PushParent(stackPanel4);
		StackPanel stackPanel17 = stackPanel4;
		Grid.SetColumn(stackPanel17, 2);
		stackPanel17.Spacing = 2.0;
		stackPanel17.HorizontalAlignment = HorizontalAlignment.Left;
		Avalonia.Controls.Controls children9 = stackPanel17.Children;
		TextBlock textBlock13;
		TextBlock textBlock12 = (textBlock13 = new TextBlock());
		((ISupportInitialize)textBlock12).BeginInit();
		children9.Add(textBlock12);
		TextBlock textBlock14 = (textBlock4 = textBlock13);
		context.PushParent(textBlock4);
		TextBlock textBlock15 = textBlock4;
		StyledProperty<string?> textProperty3 = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding binding3 = new ReflectionBindingExtension("TpsPromptLabel").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(textBlock15, textProperty3, binding3);
		textBlock15.FontSize = 10.0;
		textBlock15.Foreground = new ImmutableSolidColorBrush(4287323382u);
		textBlock15.HorizontalAlignment = HorizontalAlignment.Center;
		context.PopParent();
		((ISupportInitialize)textBlock14).EndInit();
		Avalonia.Controls.Controls children10 = stackPanel17.Children;
		StackPanel stackPanel19;
		StackPanel stackPanel18 = (stackPanel19 = new StackPanel());
		((ISupportInitialize)stackPanel18).BeginInit();
		children10.Add(stackPanel18);
		StackPanel stackPanel20 = (stackPanel4 = stackPanel19);
		context.PushParent(stackPanel4);
		StackPanel stackPanel21 = stackPanel4;
		stackPanel21.Orientation = Orientation.Horizontal;
		stackPanel21.Spacing = 4.0;
		stackPanel21.HorizontalAlignment = HorizontalAlignment.Center;
		Avalonia.Controls.Controls children11 = stackPanel21.Children;
		TextBlock textBlock17;
		TextBlock textBlock16 = (textBlock17 = new TextBlock());
		((ISupportInitialize)textBlock16).BeginInit();
		children11.Add(textBlock16);
		TextBlock textBlock18 = (textBlock4 = textBlock17);
		context.PushParent(textBlock4);
		TextBlock textBlock19 = textBlock4;
		StyledProperty<string?> textProperty4 = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding binding4 = new ReflectionBindingExtension("PromptTpsText").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(textBlock19, textProperty4, binding4);
		textBlock19.FontSize = 32.0;
		textBlock19.FontWeight = FontWeight.Bold;
		textBlock19.Foreground = new ImmutableSolidColorBrush(4287323382u);
		textBlock19.VerticalAlignment = VerticalAlignment.Center;
		context.PopParent();
		((ISupportInitialize)textBlock18).EndInit();
		Avalonia.Controls.Controls children12 = stackPanel21.Children;
		TextBlock textBlock21;
		TextBlock textBlock20 = (textBlock21 = new TextBlock());
		((ISupportInitialize)textBlock20).BeginInit();
		children12.Add(textBlock20);
		textBlock21.Text = "tok/s";
		textBlock21.FontSize = 12.0;
		textBlock21.Foreground = new ImmutableSolidColorBrush(4287931320u);
		textBlock21.VerticalAlignment = VerticalAlignment.Bottom;
		textBlock21.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
		((ISupportInitialize)textBlock21).EndInit();
		context.PopParent();
		((ISupportInitialize)stackPanel20).EndInit();
		context.PopParent();
		((ISupportInitialize)stackPanel16).EndInit();
		context.PopParent();
		((ISupportInitialize)grid3).EndInit();
		Avalonia.Controls.Controls children13 = stackPanel5.Children;
		ProgressBar progressBar2;
		ProgressBar progressBar = (progressBar2 = new ProgressBar());
		((ISupportInitialize)progressBar).BeginInit();
		children13.Add(progressBar);
		ProgressBar progressBar4;
		ProgressBar progressBar3 = (progressBar4 = progressBar2);
		context.PushParent(progressBar4);
		StyledProperty<double> valueProperty = RangeBase.ValueProperty;
		context.ProvideTargetProperty = RangeBase.ValueProperty;
		Binding binding5 = new ReflectionBindingExtension("TpsPercent").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(progressBar4, valueProperty, binding5);
		progressBar4.Maximum = 100.0;
		progressBar4.Height = 8.0;
		progressBar4.CornerRadius = new CornerRadius(4.0, 4.0, 4.0, 4.0);
		progressBar4.Background = new ImmutableSolidColorBrush(4281146217u);
		progressBar4.Foreground = new ImmutableSolidColorBrush(4294688548u);
		context.PopParent();
		((ISupportInitialize)progressBar3).EndInit();
		Avalonia.Controls.Controls children14 = stackPanel5.Children;
		Grid grid6;
		Grid grid5 = (grid6 = new Grid());
		((ISupportInitialize)grid5).BeginInit();
		children14.Add(grid5);
		ColumnDefinitions columnDefinitions2 = new ColumnDefinitions();
		columnDefinitions2.Capacity = 3;
		columnDefinitions2.Add(new ColumnDefinition(new GridLength(0.0, GridUnitType.Auto)));
		columnDefinitions2.Add(new ColumnDefinition(new GridLength(1.0, GridUnitType.Star)));
		columnDefinitions2.Add(new ColumnDefinition(new GridLength(0.0, GridUnitType.Auto)));
		grid6.ColumnDefinitions = columnDefinitions2;
		Avalonia.Controls.Controls children15 = grid6.Children;
		TextBlock textBlock23;
		TextBlock textBlock22 = (textBlock23 = new TextBlock());
		((ISupportInitialize)textBlock22).BeginInit();
		children15.Add(textBlock22);
		Grid.SetColumn(textBlock23, 0);
		textBlock23.Text = "0";
		textBlock23.FontSize = 10.0;
		textBlock23.Foreground = new ImmutableSolidColorBrush(4284773515u);
		((ISupportInitialize)textBlock23).EndInit();
		Avalonia.Controls.Controls children16 = grid6.Children;
		TextBlock textBlock25;
		TextBlock textBlock24 = (textBlock25 = new TextBlock());
		((ISupportInitialize)textBlock24).BeginInit();
		children16.Add(textBlock24);
		Grid.SetColumn(textBlock25, 1);
		textBlock25.Text = "150";
		textBlock25.FontSize = 10.0;
		textBlock25.Foreground = new ImmutableSolidColorBrush(4284773515u);
		textBlock25.HorizontalAlignment = HorizontalAlignment.Center;
		((ISupportInitialize)textBlock25).EndInit();
		Avalonia.Controls.Controls children17 = grid6.Children;
		TextBlock textBlock27;
		TextBlock textBlock26 = (textBlock27 = new TextBlock());
		((ISupportInitialize)textBlock26).BeginInit();
		children17.Add(textBlock26);
		Grid.SetColumn(textBlock27, 2);
		textBlock27.Text = "300+";
		textBlock27.FontSize = 10.0;
		textBlock27.Foreground = new ImmutableSolidColorBrush(4284773515u);
		((ISupportInitialize)textBlock27).EndInit();
		((ISupportInitialize)grid6).EndInit();
		context.PopParent();
		((ISupportInitialize)stackPanel3).EndInit();
		context.PopParent();
		((ISupportInitialize)border3).EndInit();
		context.PopParent();
		((ISupportInitialize)P_1).EndInit();
		if (P_1 is StyledElement styled)
		{
			NameScope.SetNameScope(styled, context.AvaloniaNameScope);
		}
		context.AvaloniaNameScope.Complete();
	}

	private static void _0021XamlIlPopulateTrampoline(TpsGaugeControl P_0)
	{
		if (_0021XamlIlPopulateOverride != null)
		{
			_0021XamlIlPopulateOverride(P_0);
		}
		else
		{
			_0021XamlIlPopulate(XamlIlRuntimeHelpers.CreateRootServiceProviderV3(null), P_0);
		}
	}
}
