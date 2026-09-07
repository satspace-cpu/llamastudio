using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using CompiledAvaloniaXaml;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.Services;
using LlamaStudio.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaStudio.Views.Pages;

[CompilerGenerated]
public class CliArgsPickerWindow : Window
{
	[CompilerGenerated]
	private class XamlClosure_5
	{
		public unsafe static object Build_1(IServiceProvider P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<CliArgsPickerWindow> context = CreateContext(P_0);
			context.IntermediateRoot = new StackPanel();
			object obj = context.IntermediateRoot;
			((ISupportInitialize)obj).BeginInit();
			StackPanel groupPanel = (StackPanel)obj;
			context.PushParent(groupPanel);
			groupPanel.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
			Avalonia.Controls.Controls children = groupPanel.Children;
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
			textBlockRef.FontSize = 15.0;
			textBlockRef.Foreground = new ImmutableSolidColorBrush(4287931320u);
			textBlockRef.Margin = new Thickness(0.0, 6.0, 0.0, 4.0);
			context.PopParent();
			((ISupportInitialize)categoryNameLabelEnd).EndInit();
			Avalonia.Controls.Controls children2 = groupPanel.Children;
			ItemsControl groupItemsList2;
			ItemsControl groupItemsList = (groupItemsList2 = new ItemsControl());
			((ISupportInitialize)groupItemsList).BeginInit();
			children2.Add(groupItemsList);
			ItemsControl groupItemsList4;
			ItemsControl groupItemsList3 = (groupItemsList4 = groupItemsList2);
			context.PushParent(groupItemsList4);
			StyledProperty<IEnumerable?> itemsSourceProperty = ItemsControl.ItemsSourceProperty;
			ReflectionBindingExtension reflectionBindingExtension2 = new ReflectionBindingExtension("Items");
			context.ProvideTargetProperty = ItemsControl.ItemsSourceProperty;
			Binding binding2 = reflectionBindingExtension2.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(groupItemsList4, itemsSourceProperty, binding2);
			DataTemplate dataTemplate;
			DataTemplate itemTemplate = (dataTemplate = new DataTemplate());
			context.PushParent(dataTemplate);
			dataTemplate.Content = XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<Control>((nint)(delegate*<IServiceProvider, object>)(&Build_2), context);
			context.PopParent();
			groupItemsList4.ItemTemplate = itemTemplate;
			context.PopParent();
			((ISupportInitialize)groupItemsList3).EndInit();
			context.PopParent();
			((ISupportInitialize)obj).EndInit();
			return obj;
		}

		public static CompiledAvaloniaXaml.XamlIlContext.Context<CliArgsPickerWindow> CreateContext(IServiceProvider P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<CliArgsPickerWindow> context = new CompiledAvaloniaXaml.XamlIlContext.Context<CliArgsPickerWindow>(P_0, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FViews_002FPages_002FCliArgsPickerWindow_002Eaxaml.Singleton }, "avares://LlamaStudio/Views/Pages/CliArgsPickerWindow.axaml");
			if (P_0 != null)
			{
				object service = P_0.GetService(typeof(IRootObjectProvider));
				if (service != null)
				{
					service = ((IRootObjectProvider)service).RootObject;
					context.RootObject = (CliArgsPickerWindow)service;
				}
			}
			return context;
		}

		public static object Build_2(IServiceProvider P_0)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<CliArgsPickerWindow> context = CreateContext(P_0);
			context.IntermediateRoot = new Border();
			object obj = context.IntermediateRoot;
			((ISupportInitialize)obj).BeginInit();
			Border rowBorder = (Border)obj;
			context.PushParent(rowBorder);
			rowBorder.Background = new ImmutableSolidColorBrush(4280166715u);
			rowBorder.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
			rowBorder.Padding = new Thickness(12.0, 8.0, 12.0, 8.0);
			rowBorder.Margin = new Thickness(0.0, 2.0, 0.0, 2.0);
			Grid rowGrid;
			Grid rowGridBase = (rowGrid = new Grid());
			((ISupportInitialize)rowGridBase).BeginInit();
			rowBorder.Child = rowGridBase;
			Grid gridRef;
			Grid rowGridEnd = (gridRef = rowGrid);
			context.PushParent(gridRef);
			Grid rowGridControl = gridRef;
			ColumnDefinitions columnDefinitions = new ColumnDefinitions();
			columnDefinitions.Capacity = 4;
			columnDefinitions.Add(new ColumnDefinition(new GridLength(0.0, GridUnitType.Auto)));
			columnDefinitions.Add(new ColumnDefinition(new GridLength(180.0, GridUnitType.Pixel)));
			columnDefinitions.Add(new ColumnDefinition(new GridLength(1.0, GridUnitType.Star)));
			columnDefinitions.Add(new ColumnDefinition(new GridLength(150.0, GridUnitType.Pixel)));
			rowGridControl.ColumnDefinitions = columnDefinitions;
			RowDefinitions rowDefinitions = new RowDefinitions();
			rowDefinitions.Capacity = 2;
			rowDefinitions.Add(new RowDefinition(new GridLength(0.0, GridUnitType.Auto)));
			rowDefinitions.Add(new RowDefinition(new GridLength(0.0, GridUnitType.Auto)));
			rowGridControl.RowDefinitions = rowDefinitions;
			Avalonia.Controls.Controls children = rowGridControl.Children;
			CheckBox isSelectedCheck;
			CheckBox isSelectedCheckBase = (isSelectedCheck = new CheckBox());
			((ISupportInitialize)isSelectedCheckBase).BeginInit();
			children.Add(isSelectedCheckBase);
			CheckBox checkBoxRef;
			CheckBox isSelectedCheckEnd = (checkBoxRef = isSelectedCheck);
			context.PushParent(checkBoxRef);
			Grid.SetRow(checkBoxRef, 0);
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
			Avalonia.Controls.Controls children2 = rowGridControl.Children;
			TextBlock primaryFlagLabel;
			TextBlock primaryFlagLabelBase = (primaryFlagLabel = new TextBlock());
			((ISupportInitialize)primaryFlagLabelBase).BeginInit();
			children2.Add(primaryFlagLabelBase);
			TextBlock textBlockRef;
			TextBlock primaryFlagLabelEnd = (textBlockRef = primaryFlagLabel);
			context.PushParent(textBlockRef);
			TextBlock primaryFlagLabelControl = textBlockRef;
			Grid.SetRow(primaryFlagLabelControl, 0);
			Grid.SetColumn(primaryFlagLabelControl, 1);
			StyledProperty<string?> textProperty = TextBlock.TextProperty;
			ReflectionBindingExtension reflectionBindingExtension2 = new ReflectionBindingExtension("PrimaryFlag");
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding binding2 = reflectionBindingExtension2.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(primaryFlagLabelControl, textProperty, binding2);
			primaryFlagLabelControl.Margin = new Thickness(8.0, 2.0, 0.0, 0.0);
			primaryFlagLabelControl.FontFamily = new FontFamily(((IUriContext)context).BaseUri, "Consolas,Courier New,monospace");
			primaryFlagLabelControl.FontSize = 13.0;
			primaryFlagLabelControl.FontWeight = FontWeight.DemiBold;
			primaryFlagLabelControl.Foreground = new ImmutableSolidColorBrush(4286680312u);
			context.PopParent();
			((ISupportInitialize)primaryFlagLabelEnd).EndInit();
			Avalonia.Controls.Controls children3 = rowGridControl.Children;
			StackPanel detailsPanel;
			StackPanel detailsPanelBase = (detailsPanel = new StackPanel());
			((ISupportInitialize)detailsPanelBase).BeginInit();
			children3.Add(detailsPanelBase);
			StackPanel stackPanelRef;
			StackPanel detailsPanelEnd = (stackPanelRef = detailsPanel);
			context.PushParent(stackPanelRef);
			Grid.SetRow(stackPanelRef, 0);
			Grid.SetColumn(stackPanelRef, 2);
			stackPanelRef.Margin = new Thickness(8.0, 2.0, 0.0, 0.0);
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
			descriptionLabelControl.FontSize = 13.0;
			descriptionLabelControl.Foreground = new ImmutableSolidColorBrush(4287931320u);
			descriptionLabelControl.TextWrapping = TextWrapping.Wrap;
			context.PopParent();
			((ISupportInitialize)descriptionLabelEnd).EndInit();
			Avalonia.Controls.Controls children5 = stackPanelRef.Children;
			TextBlock allowedValuesLabel;
			TextBlock allowedValuesLabelBase = (allowedValuesLabel = new TextBlock());
			((ISupportInitialize)allowedValuesLabelBase).BeginInit();
			children5.Add(allowedValuesLabelBase);
			TextBlock allowedValuesLabelEnd = (textBlockRef = allowedValuesLabel);
			context.PushParent(textBlockRef);
			TextBlock allowedValuesLabelControl = textBlockRef;
			StyledProperty<string?> textProperty3 = TextBlock.TextProperty;
			ReflectionBindingExtension reflectionBindingExtension4 = new ReflectionBindingExtension("AllowedValuesText");
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding binding4 = reflectionBindingExtension4.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(allowedValuesLabelControl, textProperty3, binding4);
			allowedValuesLabelControl.FontFamily = new FontFamily(((IUriContext)context).BaseUri, "Consolas,Courier New,monospace");
			allowedValuesLabelControl.FontSize = 12.0;
			allowedValuesLabelControl.Foreground = new ImmutableSolidColorBrush(4284773515u);
			allowedValuesLabelControl.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
			StyledProperty<bool> isVisibleProperty = Visual.IsVisibleProperty;
			ReflectionBindingExtension reflectionBindingExtension5 = new ReflectionBindingExtension("HasAllowedValues");
			context.ProvideTargetProperty = Visual.IsVisibleProperty;
			Binding binding5 = reflectionBindingExtension5.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(allowedValuesLabelControl, isVisibleProperty, binding5);
			context.PopParent();
			((ISupportInitialize)allowedValuesLabelEnd).EndInit();
			context.PopParent();
			((ISupportInitialize)detailsPanelEnd).EndInit();
			Avalonia.Controls.Controls children6 = rowGridControl.Children;
			Grid valueGrid;
			Grid valueGridBase = (valueGrid = new Grid());
			((ISupportInitialize)valueGridBase).BeginInit();
			children6.Add(valueGridBase);
			Grid valueGridEnd = (gridRef = valueGrid);
			context.PushParent(gridRef);
			Grid valueGridControl = gridRef;
			Grid.SetRow(valueGridControl, 0);
			Grid.SetColumn(valueGridControl, 3);
			Avalonia.Controls.Controls children7 = valueGridControl.Children;
			ComboBox valueCombo;
			ComboBox valueComboBase = (valueCombo = new ComboBox());
			((ISupportInitialize)valueComboBase).BeginInit();
			children7.Add(valueComboBase);
			ComboBox comboBoxRef;
			ComboBox valueComboEnd = (comboBoxRef = valueCombo);
			context.PushParent(comboBoxRef);
			comboBoxRef.Name = "ValueCombo";
			object element = comboBoxRef;
			context.AvaloniaNameScope.Register("ValueCombo", element);
			StyledProperty<IEnumerable?> itemsSourceProperty = ItemsControl.ItemsSourceProperty;
			ReflectionBindingExtension reflectionBindingExtension6 = new ReflectionBindingExtension("AllowedValues");
			context.ProvideTargetProperty = ItemsControl.ItemsSourceProperty;
			Binding binding6 = reflectionBindingExtension6.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(comboBoxRef, itemsSourceProperty, binding6);
			ReflectionBindingExtension reflectionBindingExtension7 = new ReflectionBindingExtension("UserValue");
			context.ProvideTargetProperty = SelectingItemsControl.SelectedItemProperty;
			Binding binding7 = reflectionBindingExtension7.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_1(comboBoxRef, binding7);
			comboBoxRef.FontSize = 12.0;
			comboBoxRef.Padding = new Thickness(8.0, 4.0, 8.0, 4.0);
			comboBoxRef.CornerRadius = new CornerRadius(4.0, 4.0, 4.0, 4.0);
			comboBoxRef.Background = new ImmutableSolidColorBrush(4279179050u);
			comboBoxRef.Foreground = new ImmutableSolidColorBrush(4293060848u);
			comboBoxRef.BorderBrush = new ImmutableSolidColorBrush(4281549141u);
			StyledProperty<bool> isVisibleProperty2 = Visual.IsVisibleProperty;
			ReflectionBindingExtension reflectionBindingExtension8 = new ReflectionBindingExtension("HasAllowedValues");
			context.ProvideTargetProperty = Visual.IsVisibleProperty;
			Binding binding8 = reflectionBindingExtension8.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(comboBoxRef, isVisibleProperty2, binding8);
			context.PopParent();
			((ISupportInitialize)valueComboEnd).EndInit();
			Avalonia.Controls.Controls children8 = valueGridControl.Children;
			TextBox valueBox;
			TextBox valueBoxBase = (valueBox = new TextBox());
			((ISupportInitialize)valueBoxBase).BeginInit();
			children8.Add(valueBoxBase);
			TextBox textBoxRef;
			TextBox valueBoxEnd = (textBoxRef = valueBox);
			context.PushParent(textBoxRef);
			textBoxRef.Name = "ValueBox";
			element = textBoxRef;
			context.AvaloniaNameScope.Register("ValueBox", element);
			StyledProperty<string?> textProperty4 = TextBox.TextProperty;
			ReflectionBindingExtension obj2 = new ReflectionBindingExtension("UserValue")
			{
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			};
			context.ProvideTargetProperty = TextBox.TextProperty;
			Binding binding9 = obj2.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(textBoxRef, textProperty4, binding9);
			StyledProperty<string?> watermarkProperty = TextBox.WatermarkProperty;
			ReflectionBindingExtension reflectionBindingExtension9 = new ReflectionBindingExtension("DefaultValue");
			context.ProvideTargetProperty = TextBox.WatermarkProperty;
			Binding binding10 = reflectionBindingExtension9.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(textBoxRef, watermarkProperty, binding10);
			textBoxRef.FontFamily = new FontFamily(((IUriContext)context).BaseUri, "Consolas,Courier New,monospace");
			textBoxRef.FontSize = 12.0;
			textBoxRef.Padding = new Thickness(8.0, 4.0, 8.0, 4.0);
			textBoxRef.CornerRadius = new CornerRadius(4.0, 4.0, 4.0, 4.0);
			textBoxRef.Background = new ImmutableSolidColorBrush(4279179050u);
			textBoxRef.Foreground = new ImmutableSolidColorBrush(4293060848u);
			textBoxRef.BorderBrush = new ImmutableSolidColorBrush(4281549141u);
			StyledProperty<bool> isVisibleProperty3 = Visual.IsVisibleProperty;
			ReflectionBindingExtension reflectionBindingExtension11;
			ReflectionBindingExtension reflectionBindingExtension10 = (reflectionBindingExtension11 = new ReflectionBindingExtension("HasAllowedValues"));
			context.PushParent(reflectionBindingExtension11);
			StaticResourceExtension staticResourceExtension = new StaticResourceExtension("InvertedBoolConverter");
			context.ProvideTargetProperty = CompiledAvaloniaXaml.XamlIlHelpers.Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverter_0021Property();
			object? obj3 = staticResourceExtension.ProvideValue(context);
			context.ProvideTargetProperty = null;
			reflectionBindingExtension11.Converter = (IValueConverter)obj3;
			context.PopParent();
			context.ProvideTargetProperty = Visual.IsVisibleProperty;
			Binding binding11 = reflectionBindingExtension10.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(textBoxRef, isVisibleProperty3, binding11);
			context.PopParent();
			((ISupportInitialize)valueBoxEnd).EndInit();
			context.PopParent();
			((ISupportInitialize)valueGridEnd).EndInit();
			context.PopParent();
			((ISupportInitialize)rowGridEnd).EndInit();
			context.PopParent();
			((ISupportInitialize)obj).EndInit();
			return obj;
		}
	}

	private readonly CliArgsPickerViewModel _viewModel;

	private List<string> _cachedSelectedFlags = new List<string>();

	[GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.1.0")]
	internal Window RootWindow;

	private static Action<object> _0021XamlIlPopulateOverride;

	public bool IsConfirmed { get; private set; }

	public List<string> SelectedFlags => _cachedSelectedFlags;

	public CliArgsPickerWindow(string? executablePath = null, string? customArguments = null, string language = "ru")
	{
		InitializeComponent();
		ILocalizationService requiredService = App.Services.GetRequiredService<ILocalizationService>();
		_viewModel = new CliArgsPickerViewModel(requiredService);
		base.DataContext = _viewModel;
		CliArgDefinition.SetCurrentLanguage(language);
		LoadArguments(_viewModel, executablePath, customArguments, language);
		base.Title = _viewModel.Title;
	}

	private static void LoadArguments(CliArgsPickerViewModel vm, string? executablePath, string? customArguments, string language)
	{
		List<CliArgDefinition> all = (string.IsNullOrWhiteSpace(executablePath) ? new List<CliArgDefinition>() : GetFromHelpSync(executablePath));
		List<CliArgDefinition> definitions = HelpParserService.FilterMissingFlags(all, customArguments);
		vm.LoadDefinitions(definitions, language);
	}

	private static List<CliArgDefinition> GetFromHelpSync(string executablePath)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
			{
				return new List<CliArgDefinition>();
			}
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = executablePath,
				Arguments = "--help",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8
			};
			using Process process = new Process
			{
				StartInfo = startInfo
			};
			process.Start();
			Task<string> task = process.StandardOutput.ReadToEndAsync();
			Task<string> task2 = process.StandardError.ReadToEndAsync();
			if (!process.WaitForExit(8000))
			{
				try
				{
					process.Kill();
				}
				catch
				{
				}
				return new List<CliArgDefinition>();
			}
			string text = task.Result + "\n" + task2.Result;
			if (string.IsNullOrWhiteSpace(text))
			{
				return new List<CliArgDefinition>();
			}
			return HelpParserService.ParseArgumentDescriptions(text);
		}
		catch
		{
			return new List<CliArgDefinition>();
		}
	}

	private void AddClick(object? sender, RoutedEventArgs e)
	{
		_cachedSelectedFlags = _viewModel.ConsumeSelectedArgs();
		IsConfirmed = true;
		Close();
	}

	private void CancelClick(object? sender, RoutedEventArgs e)
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
		RootWindow = this.FindNameScope()?.Find<Window>("RootWindow");
	}

	private unsafe static void _0021XamlIlPopulate(IServiceProvider P_0, CliArgsPickerWindow P_1)
	{
		CompiledAvaloniaXaml.XamlIlContext.Context<CliArgsPickerWindow> context = new CompiledAvaloniaXaml.XamlIlContext.Context<CliArgsPickerWindow>(P_0, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FViews_002FPages_002FCliArgsPickerWindow_002Eaxaml.Singleton }, "avares://LlamaStudio/Views/Pages/CliArgsPickerWindow.axaml")
		{
			RootObject = P_1,
			IntermediateRoot = P_1
		};
		((ISupportInitialize)P_1).BeginInit();
		context.PushParent(P_1);
		P_1.Name = "RootWindow";
		context.AvaloniaNameScope.Register("RootWindow", P_1);
		P_1.Width = 1050.0;
		P_1.Height = 650.0;
		P_1.MinWidth = 800.0;
		P_1.MinHeight = 500.0;
		P_1.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Border rootBorder;
		Border rootBorderBase = (rootBorder = new Border());
		((ISupportInitialize)rootBorderBase).BeginInit();
		P_1.Content = rootBorderBase;
		Border borderRef;
		Border rootBorderEnd = (borderRef = rootBorder);
		context.PushParent(borderRef);
		borderRef.Background = new ImmutableSolidColorBrush(4279903026u);
		Grid mainGrid;
		Grid mainGridBase = (mainGrid = new Grid());
		((ISupportInitialize)mainGridBase).BeginInit();
		borderRef.Child = mainGridBase;
		Grid gridRef;
		Grid mainGridEnd = (gridRef = mainGrid);
		context.PushParent(gridRef);
		Grid mainGridControl = gridRef;
		RowDefinitions rowDefinitions = new RowDefinitions();
		rowDefinitions.Capacity = 3;
		rowDefinitions.Add(new RowDefinition(new GridLength(0.0, GridUnitType.Auto)));
		rowDefinitions.Add(new RowDefinition(new GridLength(1.0, GridUnitType.Star)));
		rowDefinitions.Add(new RowDefinition(new GridLength(0.0, GridUnitType.Auto)));
		mainGridControl.RowDefinitions = rowDefinitions;
		mainGridControl.Margin = new Thickness(12.0, 12.0, 12.0, 12.0);
		Avalonia.Controls.Controls children = mainGridControl.Children;
		TextBox filterBox;
		TextBox filterBoxBase = (filterBox = new TextBox());
		((ISupportInitialize)filterBoxBase).BeginInit();
		children.Add(filterBoxBase);
		TextBox textBoxRef;
		TextBox filterBoxEnd = (textBoxRef = filterBox);
		context.PushParent(textBoxRef);
		Grid.SetRow(textBoxRef, 0);
		StyledProperty<string?> textProperty = TextBox.TextProperty;
		ReflectionBindingExtension obj = new ReflectionBindingExtension("FilterText")
		{
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		};
		context.ProvideTargetProperty = TextBox.TextProperty;
		Binding binding = obj.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(textBoxRef, textProperty, binding);
		StyledProperty<string?> watermarkProperty = TextBox.WatermarkProperty;
		ReflectionBindingExtension reflectionBindingExtension = new ReflectionBindingExtension("FilterWatermark");
		context.ProvideTargetProperty = TextBox.WatermarkProperty;
		Binding binding2 = reflectionBindingExtension.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(textBoxRef, watermarkProperty, binding2);
		textBoxRef.FontSize = 14.0;
		textBoxRef.Padding = new Thickness(12.0, 8.0, 12.0, 8.0);
		textBoxRef.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
		textBoxRef.Background = new ImmutableSolidColorBrush(4280166715u);
		textBoxRef.Foreground = new ImmutableSolidColorBrush(4293060848u);
		textBoxRef.BorderBrush = new ImmutableSolidColorBrush(4281549141u);
		textBoxRef.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
		context.PopParent();
		((ISupportInitialize)filterBoxEnd).EndInit();
		Avalonia.Controls.Controls children2 = mainGridControl.Children;
		ScrollViewer mainScrollViewer;
		ScrollViewer mainScrollViewerBase = (mainScrollViewer = new ScrollViewer());
		((ISupportInitialize)mainScrollViewerBase).BeginInit();
		children2.Add(mainScrollViewerBase);
		ScrollViewer scrollViewerRef;
		ScrollViewer mainScrollViewerEnd = (scrollViewerRef = mainScrollViewer);
		context.PushParent(scrollViewerRef);
		Grid.SetRow(scrollViewerRef, 1);
		Grid contentGrid;
		Grid contentGridBase = (contentGrid = new Grid());
		((ISupportInitialize)contentGridBase).BeginInit();
		scrollViewerRef.Content = contentGridBase;
		Grid contentGridEnd = (gridRef = contentGrid);
		context.PushParent(gridRef);
		Grid contentGridControl = gridRef;
		Avalonia.Controls.Controls children3 = contentGridControl.Children;
		TextBlock noArgsLabel;
		TextBlock noArgsLabelBase = (noArgsLabel = new TextBlock());
		((ISupportInitialize)noArgsLabelBase).BeginInit();
		children3.Add(noArgsLabelBase);
		TextBlock textBlockRef;
		TextBlock noArgsLabelEnd = (textBlockRef = noArgsLabel);
		context.PushParent(textBlockRef);
		TextBlock noArgsLabelControl = textBlockRef;
		StyledProperty<string?> textProperty2 = TextBlock.TextProperty;
		ReflectionBindingExtension reflectionBindingExtension2 = new ReflectionBindingExtension("NoArgsText");
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding binding3 = reflectionBindingExtension2.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(noArgsLabelControl, textProperty2, binding3);
		noArgsLabelControl.FontSize = 14.0;
		noArgsLabelControl.Foreground = new ImmutableSolidColorBrush(4293870660u);
		noArgsLabelControl.HorizontalAlignment = HorizontalAlignment.Center;
		noArgsLabelControl.VerticalAlignment = VerticalAlignment.Center;
		StyledProperty<bool> isVisibleProperty = Visual.IsVisibleProperty;
		ReflectionBindingExtension reflectionBindingExtension4;
		ReflectionBindingExtension reflectionBindingExtension3 = (reflectionBindingExtension4 = new ReflectionBindingExtension("HasArguments"));
		context.PushParent(reflectionBindingExtension4);
		StaticResourceExtension staticResourceExtension = new StaticResourceExtension("InvertedBoolConverter");
		context.ProvideTargetProperty = CompiledAvaloniaXaml.XamlIlHelpers.Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverter_0021Property();
		object? obj2 = staticResourceExtension.ProvideValue(context);
		context.ProvideTargetProperty = null;
		reflectionBindingExtension4.Converter = (IValueConverter)obj2;
		context.PopParent();
		context.ProvideTargetProperty = Visual.IsVisibleProperty;
		Binding binding4 = reflectionBindingExtension3.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(noArgsLabelControl, isVisibleProperty, binding4);
		context.PopParent();
		((ISupportInitialize)noArgsLabelEnd).EndInit();
		Avalonia.Controls.Controls children4 = contentGridControl.Children;
		ItemsControl groupedArgumentsList2;
		ItemsControl groupedArgumentsList = (groupedArgumentsList2 = new ItemsControl());
		((ISupportInitialize)groupedArgumentsList).BeginInit();
		children4.Add(groupedArgumentsList);
		ItemsControl groupedArgumentsList4;
		ItemsControl groupedArgumentsList3 = (groupedArgumentsList4 = groupedArgumentsList2);
		context.PushParent(groupedArgumentsList4);
		StyledProperty<IEnumerable?> itemsSourceProperty = ItemsControl.ItemsSourceProperty;
		ReflectionBindingExtension reflectionBindingExtension5 = new ReflectionBindingExtension("GroupedArguments");
		context.ProvideTargetProperty = ItemsControl.ItemsSourceProperty;
		Binding binding5 = reflectionBindingExtension5.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(groupedArgumentsList4, itemsSourceProperty, binding5);
		StyledProperty<bool> isVisibleProperty2 = Visual.IsVisibleProperty;
		ReflectionBindingExtension reflectionBindingExtension6 = new ReflectionBindingExtension("HasArguments");
		context.ProvideTargetProperty = Visual.IsVisibleProperty;
		Binding binding6 = reflectionBindingExtension6.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(groupedArgumentsList4, isVisibleProperty2, binding6);
		DataTemplate dataTemplate;
		DataTemplate itemTemplate = (dataTemplate = new DataTemplate());
		context.PushParent(dataTemplate);
		dataTemplate.Content = XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<Control>((nint)(delegate*<IServiceProvider, object>)(&XamlClosure_5.Build_1), context);
		context.PopParent();
		groupedArgumentsList4.ItemTemplate = itemTemplate;
		context.PopParent();
		((ISupportInitialize)groupedArgumentsList3).EndInit();
		context.PopParent();
		((ISupportInitialize)contentGridEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)mainScrollViewerEnd).EndInit();
		Avalonia.Controls.Controls children5 = mainGridControl.Children;
		DockPanel footerPanel;
		DockPanel footerPanelBase = (footerPanel = new DockPanel());
		((ISupportInitialize)footerPanelBase).BeginInit();
		children5.Add(footerPanelBase);
		DockPanel dockPanelRef;
		DockPanel footerPanelEnd = (dockPanelRef = footerPanel);
		context.PushParent(dockPanelRef);
		Grid.SetRow(dockPanelRef, 2);
		dockPanelRef.Margin = new Thickness(0.0, 10.0, 0.0, 0.0);
		Avalonia.Controls.Controls children6 = dockPanelRef.Children;
		TextBlock selectedCountLabel;
		TextBlock selectedCountLabelBase = (selectedCountLabel = new TextBlock());
		((ISupportInitialize)selectedCountLabelBase).BeginInit();
		children6.Add(selectedCountLabelBase);
		TextBlock selectedCountLabelEnd = (textBlockRef = selectedCountLabel);
		context.PushParent(textBlockRef);
		TextBlock selectedCountLabelControl = textBlockRef;
		StyledProperty<string?> textProperty3 = TextBlock.TextProperty;
		ReflectionBindingExtension reflectionBindingExtension7 = new ReflectionBindingExtension("SelectedCountText");
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding binding7 = reflectionBindingExtension7.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(selectedCountLabelControl, textProperty3, binding7);
		selectedCountLabelControl.FontSize = 14.0;
		selectedCountLabelControl.Foreground = new ImmutableSolidColorBrush(4287931320u);
		selectedCountLabelControl.VerticalAlignment = VerticalAlignment.Center;
		context.PopParent();
		((ISupportInitialize)selectedCountLabelEnd).EndInit();
		Avalonia.Controls.Controls children7 = dockPanelRef.Children;
		StackPanel buttonRow;
		StackPanel buttonRowBase = (buttonRow = new StackPanel());
		((ISupportInitialize)buttonRowBase).BeginInit();
		children7.Add(buttonRowBase);
		StackPanel stackPanelRef;
		StackPanel buttonRowEnd = (stackPanelRef = buttonRow);
		context.PushParent(stackPanelRef);
		DockPanel.SetDock(stackPanelRef, Dock.Right);
		stackPanelRef.Orientation = Orientation.Horizontal;
		stackPanelRef.Spacing = 8.0;
		Avalonia.Controls.Controls children8 = stackPanelRef.Children;
		Button addButton;
		Button addButtonBase = (addButton = new Button());
		((ISupportInitialize)addButtonBase).BeginInit();
		children8.Add(addButtonBase);
		Button buttonRef;
		Button addButtonEnd = (buttonRef = addButton);
		context.PushParent(buttonRef);
		Button addButtonControl = buttonRef;
		ReflectionBindingExtension reflectionBindingExtension8 = new ReflectionBindingExtension("AddBtnText");
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding binding8 = reflectionBindingExtension8.ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(addButtonControl, binding8);
		addButtonControl.AddHandler((RoutedEvent)Button.ClickEvent, (Delegate)new EventHandler<RoutedEventArgs>(context.RootObject.AddClick), RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: false);
		addButtonControl.MinWidth = 100.0;
		addButtonControl.IsDefault = true;
		addButtonControl.Background = new ImmutableSolidColorBrush(4284704497u);
		addButtonControl.Foreground = new ImmutableSolidColorBrush(uint.MaxValue);
		addButtonControl.Padding = new Thickness(24.0, 10.0, 24.0, 10.0);
		addButtonControl.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
		context.PopParent();
		((ISupportInitialize)addButtonEnd).EndInit();
		Avalonia.Controls.Controls children9 = stackPanelRef.Children;
		Button cancelButton;
		Button cancelButtonBase = (cancelButton = new Button());
		((ISupportInitialize)cancelButtonBase).BeginInit();
		children9.Add(cancelButtonBase);
		Button cancelButtonEnd = (buttonRef = cancelButton);
		context.PushParent(buttonRef);
		Button cancelButtonControl = buttonRef;
		ReflectionBindingExtension reflectionBindingExtension9 = new ReflectionBindingExtension("CancelBtnText");
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding binding9 = reflectionBindingExtension9.ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(cancelButtonControl, binding9);
		cancelButtonControl.AddHandler((RoutedEvent)Button.ClickEvent, (Delegate)new EventHandler<RoutedEventArgs>(context.RootObject.CancelClick), RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: false);
		cancelButtonControl.MinWidth = 90.0;
		cancelButtonControl.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
		cancelButtonControl.IsCancel = true;
		cancelButtonControl.Background = new ImmutableSolidColorBrush(4282865001u);
		cancelButtonControl.Foreground = new ImmutableSolidColorBrush(uint.MaxValue);
		cancelButtonControl.Padding = new Thickness(24.0, 10.0, 24.0, 10.0);
		cancelButtonControl.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
		context.PopParent();
		((ISupportInitialize)cancelButtonEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)buttonRowEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)footerPanelEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)mainGridEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)rootBorderEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)P_1).EndInit();
		if (P_1 is StyledElement styled)
		{
			NameScope.SetNameScope(styled, context.AvaloniaNameScope);
		}
		context.AvaloniaNameScope.Complete();
	}

	private static void _0021XamlIlPopulateTrampoline(CliArgsPickerWindow P_0)
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
