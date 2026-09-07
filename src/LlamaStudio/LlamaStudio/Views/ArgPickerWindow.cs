using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using CompiledAvaloniaXaml;
using LlamaStudio.Core.Models;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Views;

[CompilerGenerated]
public class ArgPickerWindow : Window
{
	[CompilerGenerated]
	private class XamlClosure_2
	{
		public unsafe static object Build_1(IServiceProvider serviceProvider)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<ArgPickerWindow> context = CreateContext(serviceProvider);
			context.IntermediateRoot = new StackPanel();
			object obj = context.IntermediateRoot;
			((ISupportInitialize)obj).BeginInit();
			StackPanel categoryGroupPanel = (StackPanel)obj;
			context.PushParent(categoryGroupPanel);
			categoryGroupPanel.Margin = new Thickness(0.0, 0.0, 0.0, 8.0);
			Avalonia.Controls.Controls children = categoryGroupPanel.Children;
			TextBlock categoryNameLabel;
			TextBlock categoryNameLabelBase = (categoryNameLabel = new TextBlock());
			((ISupportInitialize)categoryNameLabelBase).BeginInit();
			children.Add(categoryNameLabelBase);
			TextBlock textBlockRef;
			TextBlock categoryNameLabelEnd = (textBlockRef = categoryNameLabel);
			context.PushParent(textBlockRef);
			StyledProperty<string?> textProperty = TextBlock.TextProperty;
			ReflectionBindingExtension reflectionBindingExtension = new ReflectionBindingExtension("CategoryName");
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding binding = reflectionBindingExtension.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(textBlockRef, textProperty, binding);
			textBlockRef.FontWeight = FontWeight.Bold;
			textBlockRef.FontSize = 14.0;
			textBlockRef.Foreground = new ImmutableSolidColorBrush(4289170426u);
			textBlockRef.Margin = new Thickness(0.0, 4.0, 0.0, 4.0);
			context.PopParent();
			((ISupportInitialize)categoryNameLabelEnd).EndInit();
			Avalonia.Controls.Controls children2 = categoryGroupPanel.Children;
			ItemsControl flagList;
			ItemsControl itemsControl = (flagList = new ItemsControl());
			((ISupportInitialize)itemsControl).BeginInit();
			children2.Add(itemsControl);
			ItemsControl itemsControl4;
			ItemsControl itemsControl3 = (itemsControl4 = flagList);
			context.PushParent(itemsControl4);
			StyledProperty<IEnumerable?> itemsSourceProperty = ItemsControl.ItemsSourceProperty;
			ReflectionBindingExtension reflectionBindingExtension2 = new ReflectionBindingExtension("Items");
			context.ProvideTargetProperty = ItemsControl.ItemsSourceProperty;
			Binding binding2 = reflectionBindingExtension2.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(itemsControl4, itemsSourceProperty, binding2);
			DataTemplates dataTemplates = itemsControl4.DataTemplates;
			DataTemplate dataTemplate;
			DataTemplate flagRowTemplate = (dataTemplate = new DataTemplate());
			context.PushParent(dataTemplate);
			dataTemplate.DataType = typeof(CliFlagEntry);
			dataTemplate.Content = XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<Control>((nint)(delegate*<IServiceProvider, object>)(&Build_2), context);
			context.PopParent();
			dataTemplates.Add(flagRowTemplate);
			context.PopParent();
			((ISupportInitialize)itemsControl3).EndInit();
			context.PopParent();
			((ISupportInitialize)obj).EndInit();
			return obj;
		}

		public static CompiledAvaloniaXaml.XamlIlContext.Context<ArgPickerWindow> CreateContext(IServiceProvider serviceProvider)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<ArgPickerWindow> context = new CompiledAvaloniaXaml.XamlIlContext.Context<ArgPickerWindow>(serviceProvider, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FViews_002FArgPickerWindow_002Eaxaml.Singleton }, "avares://LlamaStudio/Views/ArgPickerWindow.axaml");
			if (serviceProvider != null)
			{
				object service = serviceProvider.GetService(typeof(IRootObjectProvider));
				if (service != null)
				{
					service = ((IRootObjectProvider)service).RootObject;
					context.RootObject = (ArgPickerWindow)service;
				}
			}
			return context;
		}

		public static object Build_2(IServiceProvider serviceProvider)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<ArgPickerWindow> context = CreateContext(serviceProvider);
			context.IntermediateRoot = new Grid();
			object obj = context.IntermediateRoot;
			((ISupportInitialize)obj).BeginInit();
			Grid flagRowGrid = (Grid)obj;
			context.PushParent(flagRowGrid);
			ColumnDefinitions columnDefinitions = new ColumnDefinitions();
			columnDefinitions.Capacity = 4;
			columnDefinitions.Add(new ColumnDefinition(new GridLength(0.0, GridUnitType.Auto)));
			columnDefinitions.Add(new ColumnDefinition(new GridLength(170.0, GridUnitType.Pixel)));
			columnDefinitions.Add(new ColumnDefinition(new GridLength(1.0, GridUnitType.Star)));
			columnDefinitions.Add(new ColumnDefinition(new GridLength(120.0, GridUnitType.Pixel)));
			flagRowGrid.ColumnDefinitions = columnDefinitions;
			flagRowGrid.Margin = new Thickness(4.0, 2.0, 4.0, 2.0);
			flagRowGrid.Background = new ImmutableSolidColorBrush(16777215u);
			Avalonia.Controls.Controls children = flagRowGrid.Children;
			CheckBox isSelectedCheck;
			CheckBox isSelectedCheckBase = (isSelectedCheck = new CheckBox());
			((ISupportInitialize)isSelectedCheckBase).BeginInit();
			children.Add(isSelectedCheckBase);
			CheckBox checkBoxRef;
			CheckBox isSelectedCheckEnd = (checkBoxRef = isSelectedCheck);
			context.PushParent(checkBoxRef);
			Grid.SetColumn(checkBoxRef, 0);
			StyledProperty<bool?> isCheckedProperty = ToggleButton.IsCheckedProperty;
			ReflectionBindingExtension reflectionBindingExtension = new ReflectionBindingExtension("IsSelected");
			context.ProvideTargetProperty = ToggleButton.IsCheckedProperty;
			Binding binding = reflectionBindingExtension.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(checkBoxRef, isCheckedProperty, binding);
			checkBoxRef.VerticalAlignment = VerticalAlignment.Center;
			context.PopParent();
			((ISupportInitialize)isSelectedCheckEnd).EndInit();
			Avalonia.Controls.Controls children2 = flagRowGrid.Children;
			TextBlock flagNameLabel;
			TextBlock flagNameLabelBase = (flagNameLabel = new TextBlock());
			((ISupportInitialize)flagNameLabelBase).BeginInit();
			children2.Add(flagNameLabelBase);
			TextBlock textBlockRef;
			TextBlock flagNameLabelEnd = (textBlockRef = flagNameLabel);
			context.PushParent(textBlockRef);
			TextBlock flagNameLabelControl = textBlockRef;
			Grid.SetColumn(flagNameLabelControl, 1);
			StyledProperty<string?> textProperty = TextBlock.TextProperty;
			ReflectionBindingExtension reflectionBindingExtension2 = new ReflectionBindingExtension("Name");
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding binding2 = reflectionBindingExtension2.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(flagNameLabelControl, textProperty, binding2);
			flagNameLabelControl.VerticalAlignment = VerticalAlignment.Center;
			flagNameLabelControl.TextTrimming = TextTrimming.CharacterEllipsis;
			flagNameLabelControl.FontFamily = new FontFamily(((IUriContext)context).BaseUri, "Consolas, Courier New, monospace");
			flagNameLabelControl.Foreground = new ImmutableSolidColorBrush(4286436348u);
			flagNameLabelControl.Margin = new Thickness(5.0, 0.0, 5.0, 0.0);
			context.PopParent();
			((ISupportInitialize)flagNameLabelEnd).EndInit();
			Avalonia.Controls.Controls children3 = flagRowGrid.Children;
			StackPanel valueEditorPanel;
			StackPanel valueEditorPanelBase = (valueEditorPanel = new StackPanel());
			((ISupportInitialize)valueEditorPanelBase).BeginInit();
			children3.Add(valueEditorPanelBase);
			StackPanel stackPanelRef;
			StackPanel valueEditorPanelEnd = (stackPanelRef = valueEditorPanel);
			context.PushParent(stackPanelRef);
			Grid.SetColumn(stackPanelRef, 2);
			stackPanelRef.Margin = new Thickness(5.0, 0.0, 5.0, 0.0);
			stackPanelRef.VerticalAlignment = VerticalAlignment.Center;
			stackPanelRef.Spacing = 2.0;
			Avalonia.Controls.Controls children4 = stackPanelRef.Children;
			TextBlock descriptionLabel;
			TextBlock descriptionLabelBase = (descriptionLabel = new TextBlock());
			((ISupportInitialize)descriptionLabelBase).BeginInit();
			children4.Add(descriptionLabelBase);
			TextBlock descriptionLabelEnd = (textBlockRef = descriptionLabel);
			context.PushParent(textBlockRef);
			TextBlock descriptionLabelControl = textBlockRef;
			StyledProperty<string?> textProperty2 = TextBlock.TextProperty;
			ReflectionBindingExtension reflectionBindingExtension3 = new ReflectionBindingExtension("Description");
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding binding3 = reflectionBindingExtension3.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(descriptionLabelControl, textProperty2, binding3);
			descriptionLabelControl.TextWrapping = TextWrapping.Wrap;
			descriptionLabelControl.FontSize = 12.0;
			descriptionLabelControl.Foreground = new ImmutableSolidColorBrush(4291548641u);
			context.PopParent();
			((ISupportInitialize)descriptionLabelEnd).EndInit();
			Avalonia.Controls.Controls children5 = stackPanelRef.Children;
			ComboBox enumValueBox;
			ComboBox enumValueBoxBase = (enumValueBox = new ComboBox());
			((ISupportInitialize)enumValueBoxBase).BeginInit();
			children5.Add(enumValueBoxBase);
			ComboBox comboBoxRef;
			ComboBox enumValueBoxEnd = (comboBoxRef = enumValueBox);
			context.PushParent(comboBoxRef);
			StyledProperty<IEnumerable?> itemsSourceProperty = ItemsControl.ItemsSourceProperty;
			ReflectionBindingExtension reflectionBindingExtension4 = new ReflectionBindingExtension("AllowedValues");
			context.ProvideTargetProperty = ItemsControl.ItemsSourceProperty;
			Binding binding4 = reflectionBindingExtension4.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(comboBoxRef, itemsSourceProperty, binding4);
			ReflectionBindingExtension reflectionBindingExtension5 = new ReflectionBindingExtension("UserValue");
			context.ProvideTargetProperty = SelectingItemsControl.SelectedItemProperty;
			Binding binding5 = reflectionBindingExtension5.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_1(comboBoxRef, binding5);
			comboBoxRef.FontSize = 11.0;
			comboBoxRef.Padding = new Thickness(6.0, 3.0, 6.0, 3.0);
			comboBoxRef.CornerRadius = new CornerRadius(3.0, 3.0, 3.0, 3.0);
			comboBoxRef.Background = new ImmutableSolidColorBrush(4280166715u);
			comboBoxRef.Foreground = new ImmutableSolidColorBrush(4293060848u);
			comboBoxRef.BorderBrush = new ImmutableSolidColorBrush(4281549141u);
			comboBoxRef.FontFamily = new FontFamily(((IUriContext)context).BaseUri, "Consolas, Courier New, monospace");
			StyledProperty<bool> isVisibleProperty = Visual.IsVisibleProperty;
			ReflectionBindingExtension reflectionBindingExtension6 = new ReflectionBindingExtension("HasEnumValues");
			context.ProvideTargetProperty = Visual.IsVisibleProperty;
			Binding binding6 = reflectionBindingExtension6.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(comboBoxRef, isVisibleProperty, binding6);
			comboBoxRef.Width = 200.0;
			comboBoxRef.HorizontalAlignment = HorizontalAlignment.Left;
			context.PopParent();
			((ISupportInitialize)enumValueBoxEnd).EndInit();
			Avalonia.Controls.Controls children6 = stackPanelRef.Children;
			TextBox userValueBox;
			TextBox userValueBoxBase = (userValueBox = new TextBox());
			((ISupportInitialize)userValueBoxBase).BeginInit();
			children6.Add(userValueBoxBase);
			TextBox textBoxRef;
			TextBox userValueBoxEnd = (textBoxRef = userValueBox);
			context.PushParent(textBoxRef);
			StyledProperty<string?> textProperty3 = TextBox.TextProperty;
			ReflectionBindingExtension reflectionBindingExtension7 = new ReflectionBindingExtension("UserValue");
			context.ProvideTargetProperty = TextBox.TextProperty;
			Binding binding7 = reflectionBindingExtension7.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(textBoxRef, textProperty3, binding7);
			textBoxRef.FontSize = 11.0;
			textBoxRef.Padding = new Thickness(6.0, 3.0, 6.0, 3.0);
			textBoxRef.CornerRadius = new CornerRadius(3.0, 3.0, 3.0, 3.0);
			textBoxRef.Background = new ImmutableSolidColorBrush(4280166715u);
			textBoxRef.Foreground = new ImmutableSolidColorBrush(4293060848u);
			textBoxRef.BorderBrush = new ImmutableSolidColorBrush(4281549141u);
			textBoxRef.FontFamily = new FontFamily(((IUriContext)context).BaseUri, "Consolas, Courier New, monospace");
			StyledProperty<bool> isVisibleProperty2 = Visual.IsVisibleProperty;
			context.ProvideTargetProperty = Visual.IsVisibleProperty;
			Binding binding8 = new ReflectionBindingExtension("NeedsTextInput").ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(textBoxRef, isVisibleProperty2, binding8);
			textBoxRef.Width = 200.0;
			textBoxRef.HorizontalAlignment = HorizontalAlignment.Left;
			context.PopParent();
			((ISupportInitialize)userValueBoxEnd).EndInit();
			context.PopParent();
			((ISupportInitialize)valueEditorPanelEnd).EndInit();
			Avalonia.Controls.Controls children7 = flagRowGrid.Children;
			TextBlock defaultValueLabel;
			TextBlock defaultValueLabelBase = (defaultValueLabel = new TextBlock());
			((ISupportInitialize)defaultValueLabelBase).BeginInit();
			children7.Add(defaultValueLabelBase);
			TextBlock defaultValueLabelEnd = (textBlockRef = defaultValueLabel);
			context.PushParent(textBlockRef);
			TextBlock defaultValueLabelControl = textBlockRef;
			Grid.SetColumn(defaultValueLabelControl, 3);
			StyledProperty<string?> textProperty4 = TextBlock.TextProperty;
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding binding9 = new ReflectionBindingExtension("DefaultValue").ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(defaultValueLabelControl, textProperty4, binding9);
			defaultValueLabelControl.VerticalAlignment = VerticalAlignment.Center;
			defaultValueLabelControl.TextTrimming = TextTrimming.CharacterEllipsis;
			defaultValueLabelControl.FontFamily = new FontFamily(((IUriContext)context).BaseUri, "Consolas, Courier New, monospace");
			defaultValueLabelControl.FontSize = 11.0;
			defaultValueLabelControl.Foreground = new ImmutableSolidColorBrush(4284773515u);
			defaultValueLabelControl.Margin = new Thickness(5.0, 0.0, 5.0, 0.0);
			context.PopParent();
			((ISupportInitialize)defaultValueLabelEnd).EndInit();
			context.PopParent();
			((ISupportInitialize)obj).EndInit();
			return obj;
		}
	}

	[GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.1.0")]
	internal Button AddBtn;

	[GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.1.0")]
	internal Button CancelBtn;

	private static Action<object> _0021XamlIlPopulateOverride;

	public bool IsConfirmed { get; private set; }

	public ArgPickerViewModel ViewModel => (ArgPickerViewModel)base.DataContext;

	public ArgPickerWindow()
	{
		InitializeComponent();
	}

	private void AddButton_Click(object? sender, RoutedEventArgs e)
	{
		string errorMessage = ViewModel.GetSelectedArgs().Error;
		if (errorMessage != null)
		{
			Window errorWin = new Window
			{
				Title = ViewModel.ErrorTitle,
				Width = 500.0,
				Height = 120.0,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			StackPanel errorPanel = new StackPanel
			{
				Margin = new Thickness(16.0)
			};
			errorPanel.Children.Add(new TextBlock
			{
				Text = errorMessage,
				TextWrapping = TextWrapping.Wrap,
				Foreground = new SolidColorBrush(Color.Parse("#F59E0B"))
			});
			errorPanel.Children.Add(new Button
			{
				Content = "OK",
				HorizontalAlignment = HorizontalAlignment.Right,
				Margin = new Thickness(0.0, 12.0, 0.0, 0.0),
				Background = new SolidColorBrush(Color.Parse("#374151")),
				Foreground = Brushes.White,
				Padding = new Thickness(20.0, 6.0),
				CornerRadius = new CornerRadius(6.0)
			});
			((Button)errorPanel.Children[1]).Click += delegate
			{
				errorWin.Close();
			};
			errorWin.Content = errorPanel;
			errorWin.ShowDialog(this);
		}
		else
		{
			IsConfirmed = true;
			Close();
		}
	}

	private void CancelButton_Click(object? sender, RoutedEventArgs e)
	{
		IsConfirmed = false;
		Close();
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
		AddBtn = nameScope?.Find<Button>("AddBtn");
		CancelBtn = nameScope?.Find<Button>("CancelBtn");
	}

	private unsafe static void _0021XamlIlPopulate(IServiceProvider serviceProvider, ArgPickerWindow window)
	{
		CompiledAvaloniaXaml.XamlIlContext.Context<ArgPickerWindow> context = new CompiledAvaloniaXaml.XamlIlContext.Context<ArgPickerWindow>(serviceProvider, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FViews_002FArgPickerWindow_002Eaxaml.Singleton }, "avares://LlamaStudio/Views/ArgPickerWindow.axaml")
		{
			RootObject = window,
			IntermediateRoot = window
		};
		((ISupportInitialize)window).BeginInit();
		context.PushParent(window);
		StyledProperty<string?> titleProperty = Window.TitleProperty;
		ReflectionBindingExtension reflectionBindingExtension = new ReflectionBindingExtension("Title");
		context.ProvideTargetProperty = Window.TitleProperty;
		Binding binding = reflectionBindingExtension.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(window, titleProperty, binding);
		window.Width = 950.0;
		window.Height = 600.0;
		window.MinWidth = 600.0;
		window.MinHeight = 400.0;
		window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Grid mainGrid;
		Grid mainGridBase = (mainGrid = new Grid());
		((ISupportInitialize)mainGridBase).BeginInit();
		window.Content = mainGridBase;
		Grid gridRef;
		Grid mainGridEnd = (gridRef = mainGrid);
		context.PushParent(gridRef);
		RowDefinitions rowDefinitions = new RowDefinitions();
		rowDefinitions.Capacity = 3;
		rowDefinitions.Add(new RowDefinition(new GridLength(0.0, GridUnitType.Auto)));
		rowDefinitions.Add(new RowDefinition(new GridLength(1.0, GridUnitType.Star)));
		rowDefinitions.Add(new RowDefinition(new GridLength(0.0, GridUnitType.Auto)));
		gridRef.RowDefinitions = rowDefinitions;
		gridRef.Background = new ImmutableSolidColorBrush(4279179050u);
		Avalonia.Controls.Controls children = gridRef.Children;
		TextBox filterBox;
		TextBox filterBoxBase = (filterBox = new TextBox());
		((ISupportInitialize)filterBoxBase).BeginInit();
		children.Add(filterBoxBase);
		TextBox textBoxRef;
		TextBox filterBoxEnd = (textBoxRef = filterBox);
		context.PushParent(textBoxRef);
		Grid.SetRow(textBoxRef, 0);
		StyledProperty<string?> textProperty = TextBox.TextProperty;
		ReflectionBindingExtension reflectionBindingExtension2 = new ReflectionBindingExtension("FilterText");
		context.ProvideTargetProperty = TextBox.TextProperty;
		Binding binding2 = reflectionBindingExtension2.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(textBoxRef, textProperty, binding2);
		StyledProperty<string?> watermarkProperty = TextBox.WatermarkProperty;
		ReflectionBindingExtension reflectionBindingExtension3 = new ReflectionBindingExtension("FilterWatermark");
		context.ProvideTargetProperty = TextBox.WatermarkProperty;
		Binding binding3 = reflectionBindingExtension3.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(textBoxRef, watermarkProperty, binding3);
		textBoxRef.Margin = new Thickness(12.0, 8.0, 12.0, 8.0);
		textBoxRef.Padding = new Thickness(10.0, 8.0, 10.0, 8.0);
		textBoxRef.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
		textBoxRef.Background = new ImmutableSolidColorBrush(4280166715u);
		textBoxRef.Foreground = new ImmutableSolidColorBrush(4293060848u);
		textBoxRef.BorderBrush = new ImmutableSolidColorBrush(4281549141u);
		context.PopParent();
		((ISupportInitialize)filterBoxEnd).EndInit();
		Avalonia.Controls.Controls children2 = gridRef.Children;
		ScrollViewer flagListViewer;
		ScrollViewer flagListViewerBase = (flagListViewer = new ScrollViewer());
		((ISupportInitialize)flagListViewerBase).BeginInit();
		children2.Add(flagListViewerBase);
		ScrollViewer scrollViewerRef;
		ScrollViewer flagListViewerEnd = (scrollViewerRef = flagListViewer);
		context.PushParent(scrollViewerRef);
		Grid.SetRow(scrollViewerRef, 1);
		scrollViewerRef.Margin = new Thickness(12.0, 0.0, 12.0, 0.0);
		scrollViewerRef.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
		ItemsControl groupedFlagsList;
		ItemsControl itemsControl = (groupedFlagsList = new ItemsControl());
		((ISupportInitialize)itemsControl).BeginInit();
		scrollViewerRef.Content = itemsControl;
		ItemsControl itemsControl4;
		ItemsControl itemsControl3 = (itemsControl4 = groupedFlagsList);
		context.PushParent(itemsControl4);
		StyledProperty<IEnumerable?> itemsSourceProperty = ItemsControl.ItemsSourceProperty;
		ReflectionBindingExtension reflectionBindingExtension4 = new ReflectionBindingExtension("GroupedFlags");
		context.ProvideTargetProperty = ItemsControl.ItemsSourceProperty;
		Binding binding4 = reflectionBindingExtension4.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(itemsControl4, itemsSourceProperty, binding4);
		DataTemplates dataTemplates = itemsControl4.DataTemplates;
		DataTemplate dataTemplate;
		DataTemplate categoryGroupTemplate = (dataTemplate = new DataTemplate());
		context.PushParent(dataTemplate);
		dataTemplate.DataType = typeof(ArgumentCategoryGroup);
		dataTemplate.Content = XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<Control>((nint)(delegate*<IServiceProvider, object>)(&XamlClosure_2.Build_1), context);
		context.PopParent();
		dataTemplates.Add(categoryGroupTemplate);
		context.PopParent();
		((ISupportInitialize)itemsControl3).EndInit();
		context.PopParent();
		((ISupportInitialize)flagListViewerEnd).EndInit();
		Avalonia.Controls.Controls children3 = gridRef.Children;
		StackPanel buttonBar;
		StackPanel buttonBarBase = (buttonBar = new StackPanel());
		((ISupportInitialize)buttonBarBase).BeginInit();
		children3.Add(buttonBarBase);
		StackPanel stackPanelRef;
		StackPanel buttonBarEnd = (stackPanelRef = buttonBar);
		context.PushParent(stackPanelRef);
		Grid.SetRow(stackPanelRef, 2);
		stackPanelRef.Orientation = Orientation.Horizontal;
		stackPanelRef.Spacing = 10.0;
		stackPanelRef.Margin = new Thickness(12.0, 10.0, 12.0, 10.0);
		stackPanelRef.HorizontalAlignment = HorizontalAlignment.Right;
		Avalonia.Controls.Controls children4 = stackPanelRef.Children;
		TextBlock selectedCountLabel;
		TextBlock selectedCountLabelBase = (selectedCountLabel = new TextBlock());
		((ISupportInitialize)selectedCountLabelBase).BeginInit();
		children4.Add(selectedCountLabelBase);
		TextBlock textBlockRef;
		TextBlock selectedCountLabelEnd = (textBlockRef = selectedCountLabel);
		context.PushParent(textBlockRef);
		StyledProperty<string?> textProperty2 = TextBlock.TextProperty;
		ReflectionBindingExtension reflectionBindingExtension5 = new ReflectionBindingExtension("SelectedCountText");
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding binding5 = reflectionBindingExtension5.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(textBlockRef, textProperty2, binding5);
		textBlockRef.FontSize = 12.0;
		textBlockRef.Foreground = new ImmutableSolidColorBrush(4287931320u);
		textBlockRef.VerticalAlignment = VerticalAlignment.Center;
		context.PopParent();
		((ISupportInitialize)selectedCountLabelEnd).EndInit();
		Avalonia.Controls.Controls children5 = stackPanelRef.Children;
		Grid spacerGrid;
		Grid spacerGridBase = (spacerGrid = new Grid());
		((ISupportInitialize)spacerGridBase).BeginInit();
		children5.Add(spacerGridBase);
		spacerGrid.Width = 20.0;
		((ISupportInitialize)spacerGrid).EndInit();
		Avalonia.Controls.Controls children6 = stackPanelRef.Children;
		Button addButton;
		Button addButtonBase = (addButton = new Button());
		((ISupportInitialize)addButtonBase).BeginInit();
		children6.Add(addButtonBase);
		Button buttonRef;
		Button addButtonEnd = (buttonRef = addButton);
		context.PushParent(buttonRef);
		Button addButtonControl = buttonRef;
		addButtonControl.Name = "AddBtn";
		object namedElement = addButtonControl;
		context.AvaloniaNameScope.Register("AddBtn", namedElement);
		ReflectionBindingExtension reflectionBindingExtension6 = new ReflectionBindingExtension("AddBtnText");
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding binding6 = reflectionBindingExtension6.ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(addButtonControl, binding6);
		addButtonControl.AddHandler((RoutedEvent)Button.ClickEvent, (Delegate)new EventHandler<RoutedEventArgs>(context.RootObject.AddButton_Click), RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: false);
		addButtonControl.Background = new ImmutableSolidColorBrush(4282595530u);
		addButtonControl.Foreground = new ImmutableSolidColorBrush(uint.MaxValue);
		addButtonControl.Padding = new Thickness(18.0, 7.0, 18.0, 7.0);
		addButtonControl.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
		context.PopParent();
		((ISupportInitialize)addButtonEnd).EndInit();
		Avalonia.Controls.Controls children7 = stackPanelRef.Children;
		Button cancelButton;
		Button cancelButtonBase = (cancelButton = new Button());
		((ISupportInitialize)cancelButtonBase).BeginInit();
		children7.Add(cancelButtonBase);
		Button cancelButtonEnd = (buttonRef = cancelButton);
		context.PushParent(buttonRef);
		Button cancelButtonControl = buttonRef;
		cancelButtonControl.Name = "CancelBtn";
		namedElement = cancelButtonControl;
		context.AvaloniaNameScope.Register("CancelBtn", namedElement);
		ReflectionBindingExtension reflectionBindingExtension7 = new ReflectionBindingExtension("CancelBtnText");
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding binding7 = reflectionBindingExtension7.ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(cancelButtonControl, binding7);
		cancelButtonControl.AddHandler((RoutedEvent)Button.ClickEvent, (Delegate)new EventHandler<RoutedEventArgs>(context.RootObject.CancelButton_Click), RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: false);
		cancelButtonControl.Background = new ImmutableSolidColorBrush(4281811281u);
		cancelButtonControl.Foreground = new ImmutableSolidColorBrush(4293060848u);
		cancelButtonControl.Padding = new Thickness(18.0, 7.0, 18.0, 7.0);
		cancelButtonControl.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
		context.PopParent();
		((ISupportInitialize)cancelButtonEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)buttonBarEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)mainGridEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)window).EndInit();
		if (window is StyledElement styled)
		{
			NameScope.SetNameScope(styled, context.AvaloniaNameScope);
		}
		context.AvaloniaNameScope.Complete();
	}

	private static void _0021XamlIlPopulateTrampoline(ArgPickerWindow window)
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
