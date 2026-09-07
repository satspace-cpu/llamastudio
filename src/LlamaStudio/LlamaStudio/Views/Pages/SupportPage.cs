using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using CompiledAvaloniaXaml;
using LlamaStudio;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Pages;

[CompilerGenerated]
public class SupportPage : UserControl
{
	private static Action<object> _0021XamlIlPopulateOverride;

	public SupportPage()
	{
		InitializeComponent();
	}

	public SupportPage(SupportViewModel vm)
	{
		InitializeComponent();
		base.DataContext = vm;
	}

	private void LinkButton_Click(object? sender, RoutedEventArgs e)
	{
		if (sender is Button { Tag: string tag })
		{
			OpenUrl(tag);
		}
	}

	private static void OpenUrl(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true
			});
		}
		catch
		{
		}
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

	private static void _0021XamlIlPopulate(IServiceProvider serviceProvider, SupportPage page)
	{
		CompiledAvaloniaXaml.XamlIlContext.Context<SupportPage> context = new CompiledAvaloniaXaml.XamlIlContext.Context<SupportPage>(serviceProvider, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FViews_002FPages_002FSupportPage_002Eaxaml.Singleton }, "avares://LlamaStudio/Views/Pages/SupportPage.axaml")
		{
			RootObject = page,
			IntermediateRoot = page
		};
		((ISupportInitialize)page).BeginInit();
		context.PushParent(page);
		ScrollViewer rootScrollViewerInstance;
		ScrollViewer rootScrollViewerInstanceBase = (rootScrollViewerInstance = new ScrollViewer());
		((ISupportInitialize)rootScrollViewerInstanceBase).BeginInit();
		page.Content = rootScrollViewerInstanceBase;
		ScrollViewer scrollViewerRef;
		ScrollViewer rootScrollViewerInstanceEnd = (scrollViewerRef = rootScrollViewerInstance);
		context.PushParent(scrollViewerRef);
		scrollViewerRef.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
		StackPanel mainPanelInstance;
		StackPanel mainPanelInstanceBase = (mainPanelInstance = new StackPanel());
		((ISupportInitialize)mainPanelInstanceBase).BeginInit();
		scrollViewerRef.Content = mainPanelInstanceBase;
		StackPanel stackPanelRef;
		StackPanel mainPanelInstanceEnd = (stackPanelRef = mainPanelInstance);
		context.PushParent(stackPanelRef);
		StackPanel mainPanelInstanceControl = stackPanelRef;
		mainPanelInstanceControl.Spacing = 24.0;
		mainPanelInstanceControl.Margin = new Thickness(32.0, 32.0, 32.0, 32.0);
		Avalonia.Controls.Controls titleLabelHostChildren = mainPanelInstanceControl.Children;
		TextBlock titleLabelInstance;
		TextBlock titleLabelInstanceBase = (titleLabelInstance = new TextBlock());
		((ISupportInitialize)titleLabelInstanceBase).BeginInit();
		titleLabelHostChildren.Add(titleLabelInstanceBase);
		TextBlock textBlockRef;
		TextBlock titleLabelInstanceEnd = (textBlockRef = titleLabelInstance);
		context.PushParent(textBlockRef);
		TextBlock titleLabelInstanceControl = textBlockRef;
		StyledProperty<string?> textProperty = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding titleLabelTextBinding = new ReflectionBindingExtension("Title").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(titleLabelInstanceControl, textProperty, titleLabelTextBinding);
		titleLabelInstanceControl.FontSize = 28.0;
		titleLabelInstanceControl.FontWeight = FontWeight.Bold;
		StyledProperty<IBrush?> foregroundProperty = TextBlock.ForegroundProperty;
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding titleLabelForegroundBinding = new DynamicResourceExtension("ThemeTextPrimary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(titleLabelInstanceControl, foregroundProperty, titleLabelForegroundBinding);
		context.PopParent();
		((ISupportInitialize)titleLabelInstanceEnd).EndInit();
		Avalonia.Controls.Controls telegramCardHostChildren = mainPanelInstanceControl.Children;
		Border telegramCardInstance;
		Border telegramCardInstanceBase = (telegramCardInstance = new Border());
		((ISupportInitialize)telegramCardInstanceBase).BeginInit();
		telegramCardHostChildren.Add(telegramCardInstanceBase);
		Border borderRef;
		Border telegramCardInstanceEnd = (borderRef = telegramCardInstance);
		context.PushParent(borderRef);
		Border telegramCardInstanceControl = borderRef;
		StyledProperty<IBrush?> telegramCardBackgroundProperty = Border.BackgroundProperty;
		context.ProvideTargetProperty = Border.BackgroundProperty;
		IBinding telegramCardBackgroundBinding = new DynamicResourceExtension("ThemeBackground").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(telegramCardInstanceControl, telegramCardBackgroundProperty, telegramCardBackgroundBinding);
		StyledProperty<IBrush?> borderBrushProperty = Border.BorderBrushProperty;
		context.ProvideTargetProperty = Border.BorderBrushProperty;
		IBinding telegramCardBorderBrushBinding = new DynamicResourceExtension("ThemeBorder").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(telegramCardInstanceControl, borderBrushProperty, telegramCardBorderBrushBinding);
		telegramCardInstanceControl.BorderThickness = new Thickness(1.0, 1.0, 1.0, 1.0);
		telegramCardInstanceControl.CornerRadius = new CornerRadius(12.0, 12.0, 12.0, 12.0);
		telegramCardInstanceControl.Padding = new Thickness(24.0, 24.0, 24.0, 24.0);
		StackPanel telegramContentPanelInstance;
		StackPanel telegramContentPanelInstanceBase = (telegramContentPanelInstance = new StackPanel());
		((ISupportInitialize)telegramContentPanelInstanceBase).BeginInit();
		telegramCardInstanceControl.Child = telegramContentPanelInstanceBase;
		StackPanel telegramContentPanelInstanceEnd = (stackPanelRef = telegramContentPanelInstance);
		context.PushParent(stackPanelRef);
		StackPanel telegramContentPanelInstanceControl = stackPanelRef;
		telegramContentPanelInstanceControl.Spacing = 16.0;
		Avalonia.Controls.Controls telegramTitleLabelHostChildren = telegramContentPanelInstanceControl.Children;
		TextBlock telegramTitleLabelInstance;
		TextBlock telegramTitleLabelInstanceBase = (telegramTitleLabelInstance = new TextBlock());
		((ISupportInitialize)telegramTitleLabelInstanceBase).BeginInit();
		telegramTitleLabelHostChildren.Add(telegramTitleLabelInstanceBase);
		TextBlock telegramTitleLabelInstanceEnd = (textBlockRef = telegramTitleLabelInstance);
		context.PushParent(textBlockRef);
		TextBlock telegramTitleLabelInstanceControl = textBlockRef;
		StyledProperty<string?> telegramTitleLabelTextProperty = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding telegramTitleLabelTextBinding = new ReflectionBindingExtension("TelegramTitle").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(telegramTitleLabelInstanceControl, telegramTitleLabelTextProperty, telegramTitleLabelTextBinding);
		telegramTitleLabelInstanceControl.FontSize = 18.0;
		telegramTitleLabelInstanceControl.FontWeight = FontWeight.DemiBold;
		StyledProperty<IBrush?> telegramTitleLabelForegroundProperty = TextBlock.ForegroundProperty;
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding telegramTitleLabelForegroundBinding = new DynamicResourceExtension("ThemeTextPrimary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(telegramTitleLabelInstanceControl, telegramTitleLabelForegroundProperty, telegramTitleLabelForegroundBinding);
		context.PopParent();
		((ISupportInitialize)telegramTitleLabelInstanceEnd).EndInit();
		Avalonia.Controls.Controls telegramDescLabelHostChildren = telegramContentPanelInstanceControl.Children;
		TextBlock telegramDescLabelInstance;
		TextBlock telegramDescLabelInstanceBase = (telegramDescLabelInstance = new TextBlock());
		((ISupportInitialize)telegramDescLabelInstanceBase).BeginInit();
		telegramDescLabelHostChildren.Add(telegramDescLabelInstanceBase);
		TextBlock telegramDescLabelInstanceEnd = (textBlockRef = telegramDescLabelInstance);
		context.PushParent(textBlockRef);
		TextBlock telegramDescLabelInstanceControl = textBlockRef;
		StyledProperty<string?> telegramDescLabelTextProperty = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding telegramDescLabelTextBinding = new ReflectionBindingExtension("TelegramDesc").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(telegramDescLabelInstanceControl, telegramDescLabelTextProperty, telegramDescLabelTextBinding);
		StyledProperty<IBrush?> telegramDescLabelForegroundProperty = TextBlock.ForegroundProperty;
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding telegramDescLabelForegroundBinding = new DynamicResourceExtension("ThemeTextSecondary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(telegramDescLabelInstanceControl, telegramDescLabelForegroundProperty, telegramDescLabelForegroundBinding);
		telegramDescLabelInstanceControl.TextWrapping = TextWrapping.Wrap;
		context.PopParent();
		((ISupportInitialize)telegramDescLabelInstanceEnd).EndInit();
		Avalonia.Controls.Controls telegramButtonsPanelHostChildren = telegramContentPanelInstanceControl.Children;
		StackPanel telegramButtonsPanelInstance;
		StackPanel telegramButtonsPanelInstanceBase = (telegramButtonsPanelInstance = new StackPanel());
		((ISupportInitialize)telegramButtonsPanelInstanceBase).BeginInit();
		telegramButtonsPanelHostChildren.Add(telegramButtonsPanelInstanceBase);
		StackPanel telegramButtonsPanelInstanceEnd = (stackPanelRef = telegramButtonsPanelInstance);
		context.PushParent(stackPanelRef);
		StackPanel telegramButtonsPanelInstanceControl = stackPanelRef;
		telegramButtonsPanelInstanceControl.Spacing = 8.0;
		Avalonia.Controls.Controls openChannelButtonHostChildren = telegramButtonsPanelInstanceControl.Children;
		Button openChannelButtonInstance;
		Button openChannelButtonInstanceBase = (openChannelButtonInstance = new Button());
		((ISupportInitialize)openChannelButtonInstanceBase).BeginInit();
		openChannelButtonHostChildren.Add(openChannelButtonInstanceBase);
		Button buttonRef;
		Button openChannelButtonInstanceEnd = (buttonRef = openChannelButtonInstance);
		context.PushParent(buttonRef);
		Button openChannelButtonInstanceControl = buttonRef;
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding openChannelButtonContentBinding = new ReflectionBindingExtension("OpenChannelBtn").ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(openChannelButtonInstanceControl, openChannelButtonContentBinding);
		openChannelButtonInstanceControl.Classes.Add("accent");
		openChannelButtonInstanceControl.HorizontalAlignment = HorizontalAlignment.Left;
		openChannelButtonInstanceControl.Padding = new Thickness(20.0, 12.0, 20.0, 12.0);
		openChannelButtonInstanceControl.CornerRadius = new CornerRadius(8.0, 8.0, 8.0, 8.0);
		openChannelButtonInstanceControl.Tag = "https://t.me/LlamaStudioApp";
		openChannelButtonInstanceControl.AddHandler((RoutedEvent)Button.ClickEvent, (Delegate)new EventHandler<RoutedEventArgs>(context.RootObject.LinkButton_Click), RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: false);
		context.PopParent();
		((ISupportInitialize)openChannelButtonInstanceEnd).EndInit();
		Avalonia.Controls.Controls supportChatButtonHostChildren = telegramButtonsPanelInstanceControl.Children;
		Button supportChatButtonInstance;
		Button supportChatButtonInstanceBase = (supportChatButtonInstance = new Button());
		((ISupportInitialize)supportChatButtonInstanceBase).BeginInit();
		supportChatButtonHostChildren.Add(supportChatButtonInstanceBase);
		Button supportChatButtonInstanceEnd = (buttonRef = supportChatButtonInstance);
		context.PushParent(buttonRef);
		Button supportChatButtonInstanceControl = buttonRef;
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding supportChatButtonContentBinding = new ReflectionBindingExtension("SupportChatBtn").ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(supportChatButtonInstanceControl, supportChatButtonContentBinding);
		supportChatButtonInstanceControl.Classes.Add("accent");
		supportChatButtonInstanceControl.HorizontalAlignment = HorizontalAlignment.Left;
		supportChatButtonInstanceControl.Padding = new Thickness(20.0, 12.0, 20.0, 12.0);
		supportChatButtonInstanceControl.CornerRadius = new CornerRadius(8.0, 8.0, 8.0, 8.0);
		supportChatButtonInstanceControl.Tag = "https://t.me/+6RrP3o2WCCVmMTky";
		supportChatButtonInstanceControl.AddHandler((RoutedEvent)Button.ClickEvent, (Delegate)new EventHandler<RoutedEventArgs>(context.RootObject.LinkButton_Click), RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: false);
		context.PopParent();
		((ISupportInitialize)supportChatButtonInstanceEnd).EndInit();
		Avalonia.Controls.Controls helperBotButtonHostChildren = telegramButtonsPanelInstanceControl.Children;
		Button helperBotButtonInstance;
		Button helperBotButtonInstanceBase = (helperBotButtonInstance = new Button());
		((ISupportInitialize)helperBotButtonInstanceBase).BeginInit();
		helperBotButtonHostChildren.Add(helperBotButtonInstanceBase);
		Button helperBotButtonInstanceEnd = (buttonRef = helperBotButtonInstance);
		context.PushParent(buttonRef);
		Button helperBotButtonInstanceControl = buttonRef;
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding helperBotButtonContentBinding = new ReflectionBindingExtension("HelperBotBtn").ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(helperBotButtonInstanceControl, helperBotButtonContentBinding);
		helperBotButtonInstanceControl.Classes.Add("accent");
		helperBotButtonInstanceControl.HorizontalAlignment = HorizontalAlignment.Left;
		helperBotButtonInstanceControl.Padding = new Thickness(20.0, 12.0, 20.0, 12.0);
		helperBotButtonInstanceControl.CornerRadius = new CornerRadius(8.0, 8.0, 8.0, 8.0);
		helperBotButtonInstanceControl.Tag = "https://t.me/LlamaStudioSupport_bot";
		helperBotButtonInstanceControl.AddHandler((RoutedEvent)Button.ClickEvent, (Delegate)new EventHandler<RoutedEventArgs>(context.RootObject.LinkButton_Click), RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: false);
		context.PopParent();
		((ISupportInitialize)helperBotButtonInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)telegramButtonsPanelInstanceEnd).EndInit();
		Avalonia.Controls.Controls telegramHandlesPanelHostChildren = telegramContentPanelInstanceControl.Children;
		StackPanel telegramHandlesPanel;
		StackPanel telegramHandlesPanelBase = (telegramHandlesPanel = new StackPanel());
		((ISupportInitialize)telegramHandlesPanelBase).BeginInit();
		telegramHandlesPanelHostChildren.Add(telegramHandlesPanelBase);
		telegramHandlesPanel.Spacing = 4.0;
		Avalonia.Controls.Controls channelHandleLabelHostChildren = telegramHandlesPanel.Children;
		TextBlock channelHandleLabel;
		TextBlock channelHandleLabelBase = (channelHandleLabel = new TextBlock());
		((ISupportInitialize)channelHandleLabelBase).BeginInit();
		channelHandleLabelHostChildren.Add(channelHandleLabelBase);
		channelHandleLabel.Text = "t.me/LlamaStudioApp";
		channelHandleLabel.Foreground = new ImmutableSolidColorBrush(4280986606u);
		channelHandleLabel.FontSize = 14.0;
		channelHandleLabel.FontWeight = FontWeight.DemiBold;
		((ISupportInitialize)channelHandleLabel).EndInit();
		Avalonia.Controls.Controls supportBotHandleLabelHostChildren = telegramHandlesPanel.Children;
		TextBlock supportBotHandleLabel;
		TextBlock supportBotHandleLabelBase = (supportBotHandleLabel = new TextBlock());
		((ISupportInitialize)supportBotHandleLabelBase).BeginInit();
		supportBotHandleLabelHostChildren.Add(supportBotHandleLabelBase);
		supportBotHandleLabel.Text = "@LlamaStudioSupport_bot";
		supportBotHandleLabel.Foreground = new ImmutableSolidColorBrush(4280986606u);
		supportBotHandleLabel.FontSize = 12.0;
		((ISupportInitialize)supportBotHandleLabel).EndInit();
		((ISupportInitialize)telegramHandlesPanel).EndInit();
		context.PopParent();
		((ISupportInitialize)telegramContentPanelInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)telegramCardInstanceEnd).EndInit();
		Avalonia.Controls.Controls githubCardHostChildren = mainPanelInstanceControl.Children;
		Border githubCardInstance;
		Border githubCardInstanceBase = (githubCardInstance = new Border());
		((ISupportInitialize)githubCardInstanceBase).BeginInit();
		githubCardHostChildren.Add(githubCardInstanceBase);
		Border githubCardInstanceEnd = (borderRef = githubCardInstance);
		context.PushParent(borderRef);
		Border githubCardInstanceControl = borderRef;
		StyledProperty<IBrush?> githubCardBackgroundProperty = Border.BackgroundProperty;
		context.ProvideTargetProperty = Border.BackgroundProperty;
		IBinding githubCardBackgroundBinding = new DynamicResourceExtension("ThemeBackground").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(githubCardInstanceControl, githubCardBackgroundProperty, githubCardBackgroundBinding);
		StyledProperty<IBrush?> githubCardBorderBrushProperty = Border.BorderBrushProperty;
		context.ProvideTargetProperty = Border.BorderBrushProperty;
		IBinding githubCardBorderBrushBinding = new DynamicResourceExtension("ThemeBorder").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(githubCardInstanceControl, githubCardBorderBrushProperty, githubCardBorderBrushBinding);
		githubCardInstanceControl.BorderThickness = new Thickness(1.0, 1.0, 1.0, 1.0);
		githubCardInstanceControl.CornerRadius = new CornerRadius(12.0, 12.0, 12.0, 12.0);
		githubCardInstanceControl.Padding = new Thickness(24.0, 24.0, 24.0, 24.0);
		StackPanel githubContentPanelInstance;
		StackPanel githubContentPanelInstanceBase = (githubContentPanelInstance = new StackPanel());
		((ISupportInitialize)githubContentPanelInstanceBase).BeginInit();
		githubCardInstanceControl.Child = githubContentPanelInstanceBase;
		StackPanel githubContentPanelInstanceEnd = (stackPanelRef = githubContentPanelInstance);
		context.PushParent(stackPanelRef);
		StackPanel githubContentPanelInstanceControl = stackPanelRef;
		githubContentPanelInstanceControl.Spacing = 16.0;
		Avalonia.Controls.Controls githubTitleLabelHostChildren = githubContentPanelInstanceControl.Children;
		TextBlock githubTitleLabelInstance;
		TextBlock githubTitleLabelInstanceBase = (githubTitleLabelInstance = new TextBlock());
		((ISupportInitialize)githubTitleLabelInstanceBase).BeginInit();
		githubTitleLabelHostChildren.Add(githubTitleLabelInstanceBase);
		TextBlock githubTitleLabelInstanceEnd = (textBlockRef = githubTitleLabelInstance);
		context.PushParent(textBlockRef);
		TextBlock githubTitleLabelInstanceControl = textBlockRef;
		StyledProperty<string?> githubTitleLabelTextProperty = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding githubTitleLabelTextBinding = new ReflectionBindingExtension("GithubTitle").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(githubTitleLabelInstanceControl, githubTitleLabelTextProperty, githubTitleLabelTextBinding);
		githubTitleLabelInstanceControl.FontSize = 18.0;
		githubTitleLabelInstanceControl.FontWeight = FontWeight.DemiBold;
		StyledProperty<IBrush?> githubTitleLabelForegroundProperty = TextBlock.ForegroundProperty;
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding githubTitleLabelForegroundBinding = new DynamicResourceExtension("ThemeTextPrimary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(githubTitleLabelInstanceControl, githubTitleLabelForegroundProperty, githubTitleLabelForegroundBinding);
		context.PopParent();
		((ISupportInitialize)githubTitleLabelInstanceEnd).EndInit();
		Avalonia.Controls.Controls githubDescLabelHostChildren = githubContentPanelInstanceControl.Children;
		TextBlock githubDescLabelInstance;
		TextBlock githubDescLabelInstanceBase = (githubDescLabelInstance = new TextBlock());
		((ISupportInitialize)githubDescLabelInstanceBase).BeginInit();
		githubDescLabelHostChildren.Add(githubDescLabelInstanceBase);
		TextBlock githubDescLabelInstanceEnd = (textBlockRef = githubDescLabelInstance);
		context.PushParent(textBlockRef);
		TextBlock githubDescLabelInstanceControl = textBlockRef;
		StyledProperty<string?> githubDescLabelTextProperty = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding githubDescLabelTextBinding = new ReflectionBindingExtension("GithubDesc").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(githubDescLabelInstanceControl, githubDescLabelTextProperty, githubDescLabelTextBinding);
		StyledProperty<IBrush?> githubDescLabelForegroundProperty = TextBlock.ForegroundProperty;
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding githubDescLabelForegroundBinding = new DynamicResourceExtension("ThemeTextSecondary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(githubDescLabelInstanceControl, githubDescLabelForegroundProperty, githubDescLabelForegroundBinding);
		githubDescLabelInstanceControl.TextWrapping = TextWrapping.Wrap;
		context.PopParent();
		((ISupportInitialize)githubDescLabelInstanceEnd).EndInit();
		Avalonia.Controls.Controls githubButtonsPanelHostChildren = githubContentPanelInstanceControl.Children;
		StackPanel githubButtonsPanelInstance;
		StackPanel githubButtonsPanelInstanceBase = (githubButtonsPanelInstance = new StackPanel());
		((ISupportInitialize)githubButtonsPanelInstanceBase).BeginInit();
		githubButtonsPanelHostChildren.Add(githubButtonsPanelInstanceBase);
		StackPanel githubButtonsPanelInstanceEnd = (stackPanelRef = githubButtonsPanelInstance);
		context.PushParent(stackPanelRef);
		StackPanel githubButtonsPanelInstanceControl = stackPanelRef;
		githubButtonsPanelInstanceControl.Spacing = 8.0;
		Avalonia.Controls.Controls openRepoButtonHostChildren = githubButtonsPanelInstanceControl.Children;
		Button openRepoButtonInstance;
		Button openRepoButtonInstanceBase = (openRepoButtonInstance = new Button());
		((ISupportInitialize)openRepoButtonInstanceBase).BeginInit();
		openRepoButtonHostChildren.Add(openRepoButtonInstanceBase);
		Button openRepoButtonInstanceEnd = (buttonRef = openRepoButtonInstance);
		context.PushParent(buttonRef);
		Button openRepoButtonInstanceControl = buttonRef;
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding openRepoButtonContentBinding = new ReflectionBindingExtension("OpenRepoBtn").ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(openRepoButtonInstanceControl, openRepoButtonContentBinding);
		openRepoButtonInstanceControl.Classes.Add("accent");
		openRepoButtonInstanceControl.HorizontalAlignment = HorizontalAlignment.Left;
		openRepoButtonInstanceControl.Padding = new Thickness(20.0, 12.0, 20.0, 12.0);
		openRepoButtonInstanceControl.CornerRadius = new CornerRadius(8.0, 8.0, 8.0, 8.0);
		openRepoButtonInstanceControl.Tag = "https://github.com/satspace-cpu/llamastudio";
		openRepoButtonInstanceControl.AddHandler((RoutedEvent)Button.ClickEvent, (Delegate)new EventHandler<RoutedEventArgs>(context.RootObject.LinkButton_Click), RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: false);
		context.PopParent();
		((ISupportInitialize)openRepoButtonInstanceEnd).EndInit();
		Avalonia.Controls.Controls latestReleaseButtonHostChildren = githubButtonsPanelInstanceControl.Children;
		Button latestReleaseButtonInstance;
		Button latestReleaseButtonInstanceBase = (latestReleaseButtonInstance = new Button());
		((ISupportInitialize)latestReleaseButtonInstanceBase).BeginInit();
		latestReleaseButtonHostChildren.Add(latestReleaseButtonInstanceBase);
		Button latestReleaseButtonInstanceEnd = (buttonRef = latestReleaseButtonInstance);
		context.PushParent(buttonRef);
		Button latestReleaseButtonInstanceControl = buttonRef;
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding latestReleaseButtonContentBinding = new ReflectionBindingExtension("DownloadLatestBtn").ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(latestReleaseButtonInstanceControl, latestReleaseButtonContentBinding);
		latestReleaseButtonInstanceControl.Classes.Add("accent");
		latestReleaseButtonInstanceControl.HorizontalAlignment = HorizontalAlignment.Left;
		latestReleaseButtonInstanceControl.Padding = new Thickness(20.0, 12.0, 20.0, 12.0);
		latestReleaseButtonInstanceControl.CornerRadius = new CornerRadius(8.0, 8.0, 8.0, 8.0);
		latestReleaseButtonInstanceControl.Tag = "https://github.com/satspace-cpu/llamastudio/releases";
		latestReleaseButtonInstanceControl.AddHandler((RoutedEvent)Button.ClickEvent, (Delegate)new EventHandler<RoutedEventArgs>(context.RootObject.LinkButton_Click), RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: false);
		context.PopParent();
		((ISupportInitialize)latestReleaseButtonInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)githubButtonsPanelInstanceEnd).EndInit();
		Avalonia.Controls.Controls repoUrlLabelHostChildren = githubContentPanelInstanceControl.Children;
		TextBlock repoUrlLabel;
		TextBlock repoUrlLabelBase = (repoUrlLabel = new TextBlock());
		((ISupportInitialize)repoUrlLabelBase).BeginInit();
		repoUrlLabelHostChildren.Add(repoUrlLabelBase);
		repoUrlLabel.Text = "github.com/satspace-cpu/llamastudio";
		repoUrlLabel.Foreground = new ImmutableSolidColorBrush(4280986606u);
		repoUrlLabel.FontSize = 14.0;
		repoUrlLabel.FontWeight = FontWeight.DemiBold;
		((ISupportInitialize)repoUrlLabel).EndInit();
		context.PopParent();
		((ISupportInitialize)githubContentPanelInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)githubCardInstanceEnd).EndInit();
		Avalonia.Controls.Controls whereToFindCardHostChildren = mainPanelInstanceControl.Children;
		Border whereToFindCardInstance;
		Border whereToFindCardInstanceBase = (whereToFindCardInstance = new Border());
		((ISupportInitialize)whereToFindCardInstanceBase).BeginInit();
		whereToFindCardHostChildren.Add(whereToFindCardInstanceBase);
		Border whereToFindCardInstanceEnd = (borderRef = whereToFindCardInstance);
		context.PushParent(borderRef);
		Border whereToFindCardInstanceControl = borderRef;
		StyledProperty<IBrush?> whereToFindCardBackgroundProperty = Border.BackgroundProperty;
		context.ProvideTargetProperty = Border.BackgroundProperty;
		IBinding whereToFindCardBackgroundBinding = new DynamicResourceExtension("ThemeBackground").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(whereToFindCardInstanceControl, whereToFindCardBackgroundProperty, whereToFindCardBackgroundBinding);
		StyledProperty<IBrush?> whereToFindCardBorderBrushProperty = Border.BorderBrushProperty;
		context.ProvideTargetProperty = Border.BorderBrushProperty;
		IBinding whereToFindCardBorderBrushBinding = new DynamicResourceExtension("ThemeBorder").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(whereToFindCardInstanceControl, whereToFindCardBorderBrushProperty, whereToFindCardBorderBrushBinding);
		whereToFindCardInstanceControl.BorderThickness = new Thickness(1.0, 1.0, 1.0, 1.0);
		whereToFindCardInstanceControl.CornerRadius = new CornerRadius(12.0, 12.0, 12.0, 12.0);
		whereToFindCardInstanceControl.Padding = new Thickness(24.0, 24.0, 24.0, 24.0);
		StackPanel whereToFindContentPanelInstance;
		StackPanel whereToFindContentPanelInstanceBase = (whereToFindContentPanelInstance = new StackPanel());
		((ISupportInitialize)whereToFindContentPanelInstanceBase).BeginInit();
		whereToFindCardInstanceControl.Child = whereToFindContentPanelInstanceBase;
		StackPanel whereToFindContentPanelInstanceEnd = (stackPanelRef = whereToFindContentPanelInstance);
		context.PushParent(stackPanelRef);
		StackPanel whereToFindContentPanelInstanceControl = stackPanelRef;
		whereToFindContentPanelInstanceControl.Spacing = 16.0;
		Avalonia.Controls.Controls whereToFindTitleLabelHostChildren = whereToFindContentPanelInstanceControl.Children;
		TextBlock whereToFindTitleLabelInstance;
		TextBlock whereToFindTitleLabelInstanceBase = (whereToFindTitleLabelInstance = new TextBlock());
		((ISupportInitialize)whereToFindTitleLabelInstanceBase).BeginInit();
		whereToFindTitleLabelHostChildren.Add(whereToFindTitleLabelInstanceBase);
		TextBlock whereToFindTitleLabelInstanceEnd = (textBlockRef = whereToFindTitleLabelInstance);
		context.PushParent(textBlockRef);
		TextBlock whereToFindTitleLabelInstanceControl = textBlockRef;
		StyledProperty<string?> whereToFindTitleLabelTextProperty = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding whereToFindTitleLabelTextBinding = new ReflectionBindingExtension("WhereToFind").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(whereToFindTitleLabelInstanceControl, whereToFindTitleLabelTextProperty, whereToFindTitleLabelTextBinding);
		whereToFindTitleLabelInstanceControl.FontSize = 18.0;
		whereToFindTitleLabelInstanceControl.FontWeight = FontWeight.DemiBold;
		StyledProperty<IBrush?> whereToFindTitleLabelForegroundProperty = TextBlock.ForegroundProperty;
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding whereToFindTitleLabelForegroundBinding = new DynamicResourceExtension("ThemeTextPrimary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(whereToFindTitleLabelInstanceControl, whereToFindTitleLabelForegroundProperty, whereToFindTitleLabelForegroundBinding);
		context.PopParent();
		((ISupportInitialize)whereToFindTitleLabelInstanceEnd).EndInit();
		Avalonia.Controls.Controls whereToFindRowsPanelHostChildren = whereToFindContentPanelInstanceControl.Children;
		StackPanel whereToFindRowsPanelInstance;
		StackPanel whereToFindRowsPanelInstanceBase = (whereToFindRowsPanelInstance = new StackPanel());
		((ISupportInitialize)whereToFindRowsPanelInstanceBase).BeginInit();
		whereToFindRowsPanelHostChildren.Add(whereToFindRowsPanelInstanceBase);
		StackPanel whereToFindRowsPanelInstanceEnd = (stackPanelRef = whereToFindRowsPanelInstance);
		context.PushParent(stackPanelRef);
		StackPanel whereToFindRowsPanelInstanceControl = stackPanelRef;
		whereToFindRowsPanelInstanceControl.Spacing = 12.0;
		Avalonia.Controls.Controls newsChannelRowHostChildren = whereToFindRowsPanelInstanceControl.Children;
		StackPanel newsChannelRowInstance;
		StackPanel newsChannelRowInstanceBase = (newsChannelRowInstance = new StackPanel());
		((ISupportInitialize)newsChannelRowInstanceBase).BeginInit();
		newsChannelRowHostChildren.Add(newsChannelRowInstanceBase);
		StackPanel newsChannelRowInstanceEnd = (stackPanelRef = newsChannelRowInstance);
		context.PushParent(stackPanelRef);
		StackPanel newsChannelRowInstanceControl = stackPanelRef;
		newsChannelRowInstanceControl.Orientation = Orientation.Horizontal;
		newsChannelRowInstanceControl.Spacing = 8.0;
		Avalonia.Controls.Controls newsChannelLabelHostChildren = newsChannelRowInstanceControl.Children;
		TextBlock newsChannelLabelInstance;
		TextBlock newsChannelLabelInstanceBase = (newsChannelLabelInstance = new TextBlock());
		((ISupportInitialize)newsChannelLabelInstanceBase).BeginInit();
		newsChannelLabelHostChildren.Add(newsChannelLabelInstanceBase);
		TextBlock newsChannelLabelInstanceEnd = (textBlockRef = newsChannelLabelInstance);
		context.PushParent(textBlockRef);
		TextBlock newsChannelLabelInstanceControl = textBlockRef;
		StyledProperty<string?> newsChannelLabelTextProperty = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding newsChannelLabelTextBinding = new ReflectionBindingExtension("NewsChannel").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(newsChannelLabelInstanceControl, newsChannelLabelTextProperty, newsChannelLabelTextBinding);
		StyledProperty<IBrush?> newsChannelLabelForegroundProperty = TextBlock.ForegroundProperty;
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding newsChannelLabelForegroundBinding = new DynamicResourceExtension("ThemeTextSecondary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(newsChannelLabelInstanceControl, newsChannelLabelForegroundProperty, newsChannelLabelForegroundBinding);
		context.PopParent();
		((ISupportInitialize)newsChannelLabelInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)newsChannelRowInstanceEnd).EndInit();
		Avalonia.Controls.Controls questionsChatRowHostChildren = whereToFindRowsPanelInstanceControl.Children;
		StackPanel questionsChatRowInstance;
		StackPanel questionsChatRowInstanceBase = (questionsChatRowInstance = new StackPanel());
		((ISupportInitialize)questionsChatRowInstanceBase).BeginInit();
		questionsChatRowHostChildren.Add(questionsChatRowInstanceBase);
		StackPanel questionsChatRowInstanceEnd = (stackPanelRef = questionsChatRowInstance);
		context.PushParent(stackPanelRef);
		StackPanel questionsChatRowInstanceControl = stackPanelRef;
		questionsChatRowInstanceControl.Orientation = Orientation.Horizontal;
		questionsChatRowInstanceControl.Spacing = 8.0;
		Avalonia.Controls.Controls questionsChatLabelHostChildren = questionsChatRowInstanceControl.Children;
		TextBlock questionsChatLabelInstance;
		TextBlock questionsChatLabelInstanceBase = (questionsChatLabelInstance = new TextBlock());
		((ISupportInitialize)questionsChatLabelInstanceBase).BeginInit();
		questionsChatLabelHostChildren.Add(questionsChatLabelInstanceBase);
		TextBlock questionsChatLabelInstanceEnd = (textBlockRef = questionsChatLabelInstance);
		context.PushParent(textBlockRef);
		TextBlock questionsChatLabelInstanceControl = textBlockRef;
		StyledProperty<string?> questionsChatLabelTextProperty = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding questionsChatLabelTextBinding = new ReflectionBindingExtension("QuestionsChat").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(questionsChatLabelInstanceControl, questionsChatLabelTextProperty, questionsChatLabelTextBinding);
		StyledProperty<IBrush?> questionsChatLabelForegroundProperty = TextBlock.ForegroundProperty;
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding questionsChatLabelForegroundBinding = new DynamicResourceExtension("ThemeTextSecondary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(questionsChatLabelInstanceControl, questionsChatLabelForegroundProperty, questionsChatLabelForegroundBinding);
		context.PopParent();
		((ISupportInitialize)questionsChatLabelInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)questionsChatRowInstanceEnd).EndInit();
		Avalonia.Controls.Controls quickAnswersRowHostChildren = whereToFindRowsPanelInstanceControl.Children;
		StackPanel quickAnswersRowInstance;
		StackPanel quickAnswersRowInstanceBase = (quickAnswersRowInstance = new StackPanel());
		((ISupportInitialize)quickAnswersRowInstanceBase).BeginInit();
		quickAnswersRowHostChildren.Add(quickAnswersRowInstanceBase);
		StackPanel quickAnswersRowInstanceEnd = (stackPanelRef = quickAnswersRowInstance);
		context.PushParent(stackPanelRef);
		StackPanel quickAnswersRowInstanceControl = stackPanelRef;
		quickAnswersRowInstanceControl.Orientation = Orientation.Horizontal;
		quickAnswersRowInstanceControl.Spacing = 8.0;
		Avalonia.Controls.Controls quickAnswersLabelHostChildren = quickAnswersRowInstanceControl.Children;
		TextBlock quickAnswersLabelInstance;
		TextBlock quickAnswersLabelInstanceBase = (quickAnswersLabelInstance = new TextBlock());
		((ISupportInitialize)quickAnswersLabelInstanceBase).BeginInit();
		quickAnswersLabelHostChildren.Add(quickAnswersLabelInstanceBase);
		TextBlock quickAnswersLabelInstanceEnd = (textBlockRef = quickAnswersLabelInstance);
		context.PushParent(textBlockRef);
		TextBlock quickAnswersLabelInstanceControl = textBlockRef;
		StyledProperty<string?> quickAnswersLabelTextProperty = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding quickAnswersLabelTextBinding = new ReflectionBindingExtension("QuickAnswersBot").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(quickAnswersLabelInstanceControl, quickAnswersLabelTextProperty, quickAnswersLabelTextBinding);
		StyledProperty<IBrush?> quickAnswersLabelForegroundProperty = TextBlock.ForegroundProperty;
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding quickAnswersLabelForegroundBinding = new DynamicResourceExtension("ThemeTextSecondary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(quickAnswersLabelInstanceControl, quickAnswersLabelForegroundProperty, quickAnswersLabelForegroundBinding);
		context.PopParent();
		((ISupportInitialize)quickAnswersLabelInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)quickAnswersRowInstanceEnd).EndInit();
		Avalonia.Controls.Controls reportBugRowHostChildren = whereToFindRowsPanelInstanceControl.Children;
		StackPanel reportBugRowInstance;
		StackPanel reportBugRowInstanceBase = (reportBugRowInstance = new StackPanel());
		((ISupportInitialize)reportBugRowInstanceBase).BeginInit();
		reportBugRowHostChildren.Add(reportBugRowInstanceBase);
		StackPanel reportBugRowInstanceEnd = (stackPanelRef = reportBugRowInstance);
		context.PushParent(stackPanelRef);
		StackPanel reportBugRowInstanceControl = stackPanelRef;
		reportBugRowInstanceControl.Orientation = Orientation.Horizontal;
		reportBugRowInstanceControl.Spacing = 8.0;
		Avalonia.Controls.Controls reportBugLabelHostChildren = reportBugRowInstanceControl.Children;
		TextBlock reportBugLabelInstance;
		TextBlock reportBugLabelInstanceBase = (reportBugLabelInstance = new TextBlock());
		((ISupportInitialize)reportBugLabelInstanceBase).BeginInit();
		reportBugLabelHostChildren.Add(reportBugLabelInstanceBase);
		TextBlock reportBugLabelInstanceEnd = (textBlockRef = reportBugLabelInstance);
		context.PushParent(textBlockRef);
		TextBlock reportBugLabelInstanceControl = textBlockRef;
		StyledProperty<string?> reportBugLabelTextProperty = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding reportBugLabelTextBinding = new ReflectionBindingExtension("ReportBug").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(reportBugLabelInstanceControl, reportBugLabelTextProperty, reportBugLabelTextBinding);
		StyledProperty<IBrush?> reportBugLabelForegroundProperty = TextBlock.ForegroundProperty;
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding reportBugLabelForegroundBinding = new DynamicResourceExtension("ThemeTextSecondary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(reportBugLabelInstanceControl, reportBugLabelForegroundProperty, reportBugLabelForegroundBinding);
		context.PopParent();
		((ISupportInitialize)reportBugLabelInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)reportBugRowInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)whereToFindRowsPanelInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)whereToFindContentPanelInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)whereToFindCardInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)mainPanelInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)rootScrollViewerInstanceEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)page).EndInit();
		if (page is StyledElement styled)
		{
			NameScope.SetNameScope(styled, context.AvaloniaNameScope);
		}
		context.AvaloniaNameScope.Complete();
	}

	private static void _0021XamlIlPopulateTrampoline(SupportPage page)
	{
		if (_0021XamlIlPopulateOverride != null)
		{
			_0021XamlIlPopulateOverride(page);
		}
		else
		{
			_0021XamlIlPopulate(XamlIlRuntimeHelpers.CreateRootServiceProviderV3(null), page);
		}
	}
}
