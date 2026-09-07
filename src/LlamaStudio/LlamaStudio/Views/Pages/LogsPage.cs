using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Threading;
using CompiledAvaloniaXaml;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views.Pages;

[CompilerGenerated]
public class LogsPage : UserControl
{
	[CompilerGenerated]
	private class XamlClosure_8
	{
		public static object Build_1(IServiceProvider serviceProvider)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<LogsPage> context = CreateContext(serviceProvider);
			context.IntermediateRoot = new VirtualizingStackPanel();
			object obj = context.IntermediateRoot;
			((ISupportInitialize)obj).BeginInit();
			((ISupportInitialize)obj).EndInit();
			return obj;
		}

		public static CompiledAvaloniaXaml.XamlIlContext.Context<LogsPage> CreateContext(IServiceProvider serviceProvider)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<LogsPage> context = new CompiledAvaloniaXaml.XamlIlContext.Context<LogsPage>(serviceProvider, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FViews_002FPages_002FLogsPage_002Eaxaml.Singleton }, "avares://LlamaStudio/Views/Pages/LogsPage.axaml");
			if (serviceProvider != null)
			{
				object service = serviceProvider.GetService(typeof(IRootObjectProvider));
				if (service != null)
				{
					service = ((IRootObjectProvider)service).RootObject;
					context.RootObject = (LogsPage)service;
				}
			}
			return context;
		}

		public static object Build_2(IServiceProvider serviceProvider)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<LogsPage> context = CreateContext(serviceProvider);
			context.IntermediateRoot = new TextBlock();
			object obj = context.IntermediateRoot;
			((ISupportInitialize)obj).BeginInit();
			TextBlock logLineText = (TextBlock)obj;
			context.PushParent(logLineText);
			StyledProperty<string?> textProperty = TextBlock.TextProperty;
			ReflectionBindingExtension reflectionBindingExtension = new ReflectionBindingExtension();
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding logLineTextBinding = reflectionBindingExtension.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(logLineText, textProperty, logLineTextBinding);
			logLineText.FontFamily = new FontFamily(((IUriContext)context).BaseUri, "Consolas");
			logLineText.FontSize = 12.0;
			StyledProperty<IBrush?> foregroundProperty = TextBlock.ForegroundProperty;
			DynamicResourceExtension dynamicResourceExtension = new DynamicResourceExtension("ThemeTextPrimary");
			context.ProvideTargetProperty = TextBlock.ForegroundProperty;
			IBinding foregroundBinding = dynamicResourceExtension.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(logLineText, foregroundProperty, foregroundBinding);
			logLineText.TextWrapping = TextWrapping.NoWrap;
			logLineText.Margin = new Thickness(8.0, 0.0, 8.0, 0.0);
			context.PopParent();
			((ISupportInitialize)obj).EndInit();
			return obj;
		}
	}

	private LogsViewModel? _viewModel;

	[GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.1.0")]
	internal Button CopyBtn;

	[GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.1.0")]
	internal ScrollViewer LogScrollViewer;

	private static Action<object> _0021XamlIlPopulateOverride;

	public LogsPage()
	{
		InitializeComponent();
	}

	public LogsPage(LogsViewModel viewModel)
		: this()
	{
		base.DataContext = viewModel;
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);
		if (base.DataContext is LogsViewModel viewModel)
		{
			if (_viewModel != null)
			{
				_viewModel.PropertyChanged -= ViewModelPropertyChanged;
				_viewModel.LogLines.CollectionChanged -= LogLinesCollectionChanged;
			}
			_viewModel = viewModel;
			_viewModel.PropertyChanged += ViewModelPropertyChanged;
			_viewModel.LogLines.CollectionChanged += LogLinesCollectionChanged;
		}
	}

	private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (!(e.PropertyName == "AutoScroll"))
		{
			return;
		}
		LogsViewModel? logsViewModel = _viewModel;
		if (logsViewModel != null && logsViewModel.AutoScroll)
		{
			Dispatcher.UIThread.Post(delegate
			{
				LogScrollViewer?.ScrollToEnd();
			});
		}
	}

	private void LogLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		LogsViewModel? logsViewModel = _viewModel;
		if (logsViewModel != null && logsViewModel.AutoScroll && (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Reset))
		{
			Dispatcher.UIThread.Post(delegate
			{
				LogScrollViewer?.ScrollToEnd();
			});
		}
	}

	private async void CopyBtn_Click(object? sender, RoutedEventArgs e)
	{
		if (_viewModel == null)
		{
			return;
		}
		string fullText = _viewModel.GetFullText();
		if (string.IsNullOrEmpty(fullText))
		{
			return;
		}
		try
		{
			IClipboard clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
			if (clipboard != null)
			{
				await clipboard.SetTextAsync(fullText);
			}
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
		INameScope nameScope = this.FindNameScope();
		CopyBtn = nameScope?.Find<Button>("CopyBtn");
		LogScrollViewer = nameScope?.Find<ScrollViewer>("LogScrollViewer");
	}

	private unsafe static void _0021XamlIlPopulate(IServiceProvider serviceProvider, LogsPage page)
	{
		CompiledAvaloniaXaml.XamlIlContext.Context<LogsPage> context = new CompiledAvaloniaXaml.XamlIlContext.Context<LogsPage>(serviceProvider, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FViews_002FPages_002FLogsPage_002Eaxaml.Singleton }, "avares://LlamaStudio/Views/Pages/LogsPage.axaml")
		{
			RootObject = page,
			IntermediateRoot = page
		};
		((ISupportInitialize)page).BeginInit();
		context.PushParent(page);
		DockPanel rootPanelNew;
		DockPanel rootPanelNewBase = (rootPanelNew = new DockPanel());
		((ISupportInitialize)rootPanelNewBase).BeginInit();
		page.Content = rootPanelNewBase;
		DockPanel dockPanelRef;
		DockPanel rootPanelNewEnd = (dockPanelRef = rootPanelNew);
		context.PushParent(dockPanelRef);
		Avalonia.Controls.Controls rootChildren = dockPanelRef.Children;
		Border toolbarBorderNew;
		Border toolbarBorderNewBase = (toolbarBorderNew = new Border());
		((ISupportInitialize)toolbarBorderNewBase).BeginInit();
		rootChildren.Add(toolbarBorderNewBase);
		Border borderRef;
		Border toolbarBorderNewEnd = (borderRef = toolbarBorderNew);
		context.PushParent(borderRef);
		Border toolbarBorderNewControl = borderRef;
		DockPanel.SetDock(toolbarBorderNewControl, Dock.Top);
		StyledProperty<IBrush?> backgroundProperty = Border.BackgroundProperty;
		DynamicResourceExtension dynamicResourceExtension = new DynamicResourceExtension("ThemeSurface");
		context.ProvideTargetProperty = Border.BackgroundProperty;
		IBinding toolbarBackgroundBinding = dynamicResourceExtension.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(toolbarBorderNewControl, backgroundProperty, toolbarBackgroundBinding);
		toolbarBorderNewControl.Padding = new Thickness(8.0, 6.0, 8.0, 6.0);
		StyledProperty<IBrush?> borderBrushProperty = Border.BorderBrushProperty;
		context.ProvideTargetProperty = Border.BorderBrushProperty;
		IBinding borderBrushBinding = new DynamicResourceExtension("ThemeBorder").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(toolbarBorderNewControl, borderBrushProperty, borderBrushBinding);
		toolbarBorderNewControl.BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0);
		StackPanel toolbarPanelNew;
		StackPanel toolbarPanelNewBase = (toolbarPanelNew = new StackPanel());
		((ISupportInitialize)toolbarPanelNewBase).BeginInit();
		toolbarBorderNewControl.Child = toolbarPanelNewBase;
		StackPanel stackPanelRef;
		StackPanel toolbarPanelNewEnd = (stackPanelRef = toolbarPanelNew);
		context.PushParent(stackPanelRef);
		StackPanel toolbarPanelNewControl = stackPanelRef;
		toolbarPanelNewControl.Orientation = Orientation.Horizontal;
		toolbarPanelNewControl.Spacing = 8.0;
		Avalonia.Controls.Controls toolbarChildren = toolbarPanelNewControl.Children;
		TextBlock titleLabelNew;
		TextBlock titleLabelNewBase = (titleLabelNew = new TextBlock());
		((ISupportInitialize)titleLabelNewBase).BeginInit();
		toolbarChildren.Add(titleLabelNewBase);
		TextBlock textBlockRef;
		TextBlock titleLabelNewEnd = (textBlockRef = titleLabelNew);
		context.PushParent(textBlockRef);
		TextBlock titleLabelNewControl = textBlockRef;
		StyledProperty<string?> textProperty = TextBlock.TextProperty;
		ReflectionBindingExtension reflectionBindingExtension = new ReflectionBindingExtension("Title");
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding titleBinding = reflectionBindingExtension.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(titleLabelNewControl, textProperty, titleBinding);
		titleLabelNewControl.FontSize = 14.0;
		titleLabelNewControl.FontWeight = FontWeight.DemiBold;
		StyledProperty<IBrush?> foregroundProperty = TextBlock.ForegroundProperty;
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding titleForegroundBinding = new DynamicResourceExtension("ThemeTextPrimary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(titleLabelNewControl, foregroundProperty, titleForegroundBinding);
		titleLabelNewControl.VerticalAlignment = VerticalAlignment.Center;
		context.PopParent();
		((ISupportInitialize)titleLabelNewEnd).EndInit();
		Avalonia.Controls.Controls toolbarChildrenAutoScroll = toolbarPanelNewControl.Children;
		StackPanel autoScrollPanelNew;
		StackPanel autoScrollPanelNewBase = (autoScrollPanelNew = new StackPanel());
		((ISupportInitialize)autoScrollPanelNewBase).BeginInit();
		toolbarChildrenAutoScroll.Add(autoScrollPanelNewBase);
		StackPanel autoScrollPanelNewEnd = (stackPanelRef = autoScrollPanelNew);
		context.PushParent(stackPanelRef);
		StackPanel autoScrollPanelNewControl = stackPanelRef;
		autoScrollPanelNewControl.Orientation = Orientation.Horizontal;
		autoScrollPanelNewControl.Spacing = 8.0;
		autoScrollPanelNewControl.Margin = new Thickness(40.0, 0.0, 0.0, 0.0);
		Avalonia.Controls.Controls autoScrollChildren = autoScrollPanelNewControl.Children;
		ToggleSwitch autoScrollToggleNew;
		ToggleSwitch autoScrollToggleMain = (autoScrollToggleNew = new ToggleSwitch());
		((ISupportInitialize)autoScrollToggleMain).BeginInit();
		autoScrollChildren.Add(autoScrollToggleMain);
		ToggleSwitch autoScrollToggle;
		ToggleSwitch autoScrollToggleInit = (autoScrollToggle = autoScrollToggleNew);
		context.PushParent(autoScrollToggle);
		StyledProperty<bool?> isCheckedProperty = ToggleButton.IsCheckedProperty;
		context.ProvideTargetProperty = ToggleButton.IsCheckedProperty;
		Binding autoScrollBinding = new ReflectionBindingExtension("AutoScroll").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(autoScrollToggle, isCheckedProperty, autoScrollBinding);
		context.PopParent();
		((ISupportInitialize)autoScrollToggleInit).EndInit();
		Avalonia.Controls.Controls autoScrollChildrenLabel = autoScrollPanelNewControl.Children;
		TextBlock autoScrollLabelNew;
		TextBlock autoScrollLabelNewBase = (autoScrollLabelNew = new TextBlock());
		((ISupportInitialize)autoScrollLabelNewBase).BeginInit();
		autoScrollChildrenLabel.Add(autoScrollLabelNewBase);
		TextBlock autoScrollLabelNewEnd = (textBlockRef = autoScrollLabelNew);
		context.PushParent(textBlockRef);
		TextBlock autoScrollLabelNewControl = textBlockRef;
		StyledProperty<string?> textProperty2 = TextBlock.TextProperty;
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding autoScrollLabelBinding = new ReflectionBindingExtension("AutoScrollLabel").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(autoScrollLabelNewControl, textProperty2, autoScrollLabelBinding);
		autoScrollLabelNewControl.FontSize = 12.0;
		StyledProperty<IBrush?> foregroundProperty2 = TextBlock.ForegroundProperty;
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding labelForegroundBinding = new DynamicResourceExtension("SectionLabel").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(autoScrollLabelNewControl, foregroundProperty2, labelForegroundBinding);
		autoScrollLabelNewControl.VerticalAlignment = VerticalAlignment.Center;
		context.PopParent();
		((ISupportInitialize)autoScrollLabelNewEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)autoScrollPanelNewEnd).EndInit();
		Avalonia.Controls.Controls toolbarChildrenClear = toolbarPanelNewControl.Children;
		Button clearLogButtonNew;
		Button clearLogButtonNewBase = (clearLogButtonNew = new Button());
		((ISupportInitialize)clearLogButtonNewBase).BeginInit();
		toolbarChildrenClear.Add(clearLogButtonNewBase);
		Button buttonRef;
		Button clearLogButtonNewEnd = (buttonRef = clearLogButtonNew);
		context.PushParent(buttonRef);
		Button clearLogButtonNewControl = buttonRef;
		StyledProperty<ICommand?> commandProperty = Button.CommandProperty;
		context.ProvideTargetProperty = Button.CommandProperty;
		Binding clearLogCommandBinding = new ReflectionBindingExtension("ClearLogCommand").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(clearLogButtonNewControl, commandProperty, clearLogCommandBinding);
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding clearButtonContentBinding = new ReflectionBindingExtension("ClearBtn").ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(clearLogButtonNewControl, clearButtonContentBinding);
		StyledProperty<IBrush?> backgroundProperty2 = TemplatedControl.BackgroundProperty;
		context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
		IBinding clearButtonBackgroundBinding = new DynamicResourceExtension("BtnSecondary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(clearLogButtonNewControl, backgroundProperty2, clearButtonBackgroundBinding);
		StyledProperty<IBrush?> foregroundProperty3 = TemplatedControl.ForegroundProperty;
		context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
		IBinding clearButtonForegroundBinding = new DynamicResourceExtension("ThemeTextPrimary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(clearLogButtonNewControl, foregroundProperty3, clearButtonForegroundBinding);
		clearLogButtonNewControl.Padding = new Thickness(10.0, 4.0, 10.0, 4.0);
		clearLogButtonNewControl.CornerRadius = new CornerRadius(4.0, 4.0, 4.0, 4.0);
		clearLogButtonNewControl.VerticalAlignment = VerticalAlignment.Center;
		clearLogButtonNewControl.FontSize = 12.0;
		clearLogButtonNewControl.Margin = new Thickness(40.0, 0.0, 0.0, 0.0);
		context.PopParent();
		((ISupportInitialize)clearLogButtonNewEnd).EndInit();
		Avalonia.Controls.Controls toolbarChildrenCopy = toolbarPanelNewControl.Children;
		Button copyButtonNew;
		Button copyButtonNewBase = (copyButtonNew = new Button());
		((ISupportInitialize)copyButtonNewBase).BeginInit();
		toolbarChildrenCopy.Add(copyButtonNewBase);
		Button copyButtonNewEnd = (buttonRef = copyButtonNew);
		context.PushParent(buttonRef);
		Button copyButtonNewControl = buttonRef;
		copyButtonNewControl.Name = "CopyBtn";
		object nameScopeElement = copyButtonNewControl;
		context.AvaloniaNameScope.Register("CopyBtn", nameScopeElement);
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding copyButtonContentBinding = new ReflectionBindingExtension("CopyBtn").ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(copyButtonNewControl, copyButtonContentBinding);
		StyledProperty<IBrush?> backgroundProperty3 = TemplatedControl.BackgroundProperty;
		context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
		IBinding copyButtonBackgroundBinding = new DynamicResourceExtension("BtnSecondary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(copyButtonNewControl, backgroundProperty3, copyButtonBackgroundBinding);
		StyledProperty<IBrush?> foregroundProperty4 = TemplatedControl.ForegroundProperty;
		context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
		IBinding copyButtonForegroundBinding = new DynamicResourceExtension("ThemeTextPrimary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(copyButtonNewControl, foregroundProperty4, copyButtonForegroundBinding);
		copyButtonNewControl.Padding = new Thickness(10.0, 4.0, 10.0, 4.0);
		copyButtonNewControl.CornerRadius = new CornerRadius(4.0, 4.0, 4.0, 4.0);
		copyButtonNewControl.VerticalAlignment = VerticalAlignment.Center;
		copyButtonNewControl.FontSize = 12.0;
		copyButtonNewControl.AddHandler((RoutedEvent)Button.ClickEvent, (Delegate)new EventHandler<RoutedEventArgs>(context.RootObject.CopyBtn_Click), RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: false);
		context.PopParent();
		((ISupportInitialize)copyButtonNewEnd).EndInit();
		Avalonia.Controls.Controls toolbarChildrenSave = toolbarPanelNewControl.Children;
		Button saveLogButtonNew;
		Button saveLogButtonNewBase = (saveLogButtonNew = new Button());
		((ISupportInitialize)saveLogButtonNewBase).BeginInit();
		toolbarChildrenSave.Add(saveLogButtonNewBase);
		Button saveLogButtonNewEnd = (buttonRef = saveLogButtonNew);
		context.PushParent(buttonRef);
		Button saveLogButtonNewControl = buttonRef;
		StyledProperty<ICommand?> commandProperty2 = Button.CommandProperty;
		context.ProvideTargetProperty = Button.CommandProperty;
		Binding saveLogCommandBinding = new ReflectionBindingExtension("SaveLogCommand").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(saveLogButtonNewControl, commandProperty2, saveLogCommandBinding);
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding saveButtonContentBinding = new ReflectionBindingExtension("SaveBtn").ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(saveLogButtonNewControl, saveButtonContentBinding);
		StyledProperty<IBrush?> backgroundProperty4 = TemplatedControl.BackgroundProperty;
		context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
		IBinding saveButtonBackgroundBinding = new DynamicResourceExtension("BtnSecondary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(saveLogButtonNewControl, backgroundProperty4, saveButtonBackgroundBinding);
		StyledProperty<IBrush?> foregroundProperty5 = TemplatedControl.ForegroundProperty;
		context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
		IBinding saveButtonForegroundBinding = new DynamicResourceExtension("ThemeTextPrimary").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(saveLogButtonNewControl, foregroundProperty5, saveButtonForegroundBinding);
		saveLogButtonNewControl.Padding = new Thickness(10.0, 4.0, 10.0, 4.0);
		saveLogButtonNewControl.CornerRadius = new CornerRadius(4.0, 4.0, 4.0, 4.0);
		saveLogButtonNewControl.VerticalAlignment = VerticalAlignment.Center;
		saveLogButtonNewControl.FontSize = 12.0;
		context.PopParent();
		((ISupportInitialize)saveLogButtonNewEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)toolbarPanelNewEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)toolbarBorderNewEnd).EndInit();
		Avalonia.Controls.Controls rootChildrenLog = dockPanelRef.Children;
		Border logBorderNew;
		Border logBorderNewBase = (logBorderNew = new Border());
		((ISupportInitialize)logBorderNewBase).BeginInit();
		rootChildrenLog.Add(logBorderNewBase);
		Border logBorderNewEnd = (borderRef = logBorderNew);
		context.PushParent(borderRef);
		Border logBorderNewControl = borderRef;
		StyledProperty<IBrush?> backgroundProperty5 = Border.BackgroundProperty;
		context.ProvideTargetProperty = Border.BackgroundProperty;
		IBinding logBorderBackgroundBinding = new DynamicResourceExtension("ThemeCard").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(logBorderNewControl, backgroundProperty5, logBorderBackgroundBinding);
		StyledProperty<IBrush?> borderBrushProperty2 = Border.BorderBrushProperty;
		context.ProvideTargetProperty = Border.BorderBrushProperty;
		IBinding logBorderBrushBinding = new DynamicResourceExtension("ThemeBorder").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(logBorderNewControl, borderBrushProperty2, logBorderBrushBinding);
		logBorderNewControl.BorderThickness = new Thickness(1.0, 1.0, 1.0, 1.0);
		ScrollViewer logScrollViewerNew;
		ScrollViewer logScrollViewerNewBase = (logScrollViewerNew = new ScrollViewer());
		((ISupportInitialize)logScrollViewerNewBase).BeginInit();
		logBorderNewControl.Child = logScrollViewerNewBase;
		ScrollViewer scrollViewerRef;
		ScrollViewer logScrollViewerNewEnd = (scrollViewerRef = logScrollViewerNew);
		context.PushParent(scrollViewerRef);
		scrollViewerRef.Name = "LogScrollViewer";
		nameScopeElement = scrollViewerRef;
		context.AvaloniaNameScope.Register("LogScrollViewer", nameScopeElement);
		scrollViewerRef.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
		scrollViewerRef.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
		StyledProperty<IBrush?> backgroundProperty6 = TemplatedControl.BackgroundProperty;
		context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
		IBinding logScrollBackgroundBinding = new DynamicResourceExtension("ThemeCard").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(scrollViewerRef, backgroundProperty6, logScrollBackgroundBinding);
		ItemsControl logListNew;
		ItemsControl logListMain = (logListNew = new ItemsControl());
		((ISupportInitialize)logListMain).BeginInit();
		scrollViewerRef.Content = logListMain;
		ItemsControl logList;
		ItemsControl logListInit = (logList = logListNew);
		context.PushParent(logList);
		StyledProperty<IEnumerable?> itemsSourceProperty = ItemsControl.ItemsSourceProperty;
		context.ProvideTargetProperty = ItemsControl.ItemsSourceProperty;
		Binding logLinesBinding = new ReflectionBindingExtension("LogLines").ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(logList, itemsSourceProperty, logLinesBinding);
		logList.Margin = new Thickness(0.0, 0.0, 0.0, 16.0);
		logList.ItemsPanel = new ItemsPanelTemplate
		{
			Content = XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<Control>((nint)(delegate*<IServiceProvider, object>)(&XamlClosure_8.Build_1), context)
		};
		DataTemplate logLineTemplateParent;
		DataTemplate logLineTemplate = (logLineTemplateParent = new DataTemplate());
		context.PushParent(logLineTemplateParent);
		logLineTemplateParent.Content = XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<Control>((nint)(delegate*<IServiceProvider, object>)(&XamlClosure_8.Build_2), context);
		context.PopParent();
		logList.ItemTemplate = logLineTemplate;
		context.PopParent();
		((ISupportInitialize)logListInit).EndInit();
		context.PopParent();
		((ISupportInitialize)logScrollViewerNewEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)logBorderNewEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)rootPanelNewEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)page).EndInit();
		if (page is StyledElement styled)
		{
			NameScope.SetNameScope(styled, context.AvaloniaNameScope);
		}
		context.AvaloniaNameScope.Complete();
	}

	private static void _0021XamlIlPopulateTrampoline(LogsPage page)
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
