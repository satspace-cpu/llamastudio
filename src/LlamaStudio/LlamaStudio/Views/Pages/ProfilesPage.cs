using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
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

namespace LlamaStudio.Views.Pages;

[CompilerGenerated]
public class ProfilesPage : UserControl
{
	[CompilerGenerated]
	private class XamlClosure_11
	{
		public static object Build_1(IServiceProvider serviceProvider)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<ProfilesPage> context = CreateContext(serviceProvider);
			context.IntermediateRoot = new Border();
			object rootElement = context.IntermediateRoot;
			((ISupportInitialize)rootElement).BeginInit();
			Border cardBorder = (Border)rootElement;
			context.PushParent(cardBorder);
			Border cardBorder2 = cardBorder;
			StyledProperty<IBrush?> backgroundProperty = Border.BackgroundProperty;
			DynamicResourceExtension cardBorderBackgroundResource = new DynamicResourceExtension("ThemeSurface");
			context.ProvideTargetProperty = Border.BackgroundProperty;
			IBinding cardBorderBackgroundBinding = cardBorderBackgroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(cardBorder2, backgroundProperty, cardBorderBackgroundBinding);
			cardBorder2.CornerRadius = new CornerRadius(10.0, 10.0, 10.0, 10.0);
			cardBorder2.Margin = new Thickness(0.0, 0.0, 0.0, 8.0);
			cardBorder2.Padding = new Thickness(16.0, 16.0, 16.0, 16.0);
			StyledProperty<IBrush?> borderBrushProperty = Border.BorderBrushProperty;
			DynamicResourceExtension cardBorderBorderBrushResource = new DynamicResourceExtension("ThemeBorder");
			context.ProvideTargetProperty = Border.BorderBrushProperty;
			IBinding cardBorderBorderBrushBinding = cardBorderBorderBrushResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(cardBorder2, borderBrushProperty, cardBorderBorderBrushBinding);
			cardBorder2.BorderThickness = new Thickness(1.0, 1.0, 1.0, 1.0);
			StackPanel cardPanelNew;
			StackPanel cardPanelNewBase = (cardPanelNew = new StackPanel());
			((ISupportInitialize)cardPanelNewBase).BeginInit();
			cardBorder2.Child = cardPanelNewBase;
			StackPanel stackPanelRef;
			StackPanel cardPanelNewEnd = (stackPanelRef = cardPanelNew);
			context.PushParent(stackPanelRef);
			StackPanel cardPanelNewControl = stackPanelRef;
			cardPanelNewControl.Spacing = 6.0;
			Avalonia.Controls.Controls cardPanelChildren = cardPanelNewControl.Children;
			StackPanel nameRowPanelNew;
			StackPanel nameRowPanelNewBase = (nameRowPanelNew = new StackPanel());
			((ISupportInitialize)nameRowPanelNewBase).BeginInit();
			cardPanelChildren.Add(nameRowPanelNewBase);
			StackPanel nameRowPanelNewEnd = (stackPanelRef = nameRowPanelNew);
			context.PushParent(stackPanelRef);
			StackPanel nameRowPanelNewControl = stackPanelRef;
			nameRowPanelNewControl.Orientation = Orientation.Horizontal;
			nameRowPanelNewControl.Spacing = 12.0;
			Avalonia.Controls.Controls nameRowPanelChildren = nameRowPanelNewControl.Children;
			TextBlock nameLabelNew;
			TextBlock nameLabelNewBase = (nameLabelNew = new TextBlock());
			((ISupportInitialize)nameLabelNewBase).BeginInit();
			nameRowPanelChildren.Add(nameLabelNewBase);
			TextBlock textBlockRef;
			TextBlock nameLabelNewEnd = (textBlockRef = nameLabelNew);
			context.PushParent(textBlockRef);
			TextBlock nameLabelNewControl = textBlockRef;
			StyledProperty<string?> textProperty = TextBlock.TextProperty;
			ReflectionBindingExtension nameLabelTextSource = new ReflectionBindingExtension("Name");
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding nameLabelTextBinding = nameLabelTextSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(nameLabelNewControl, textProperty, nameLabelTextBinding);
			nameLabelNewControl.FontSize = 15.0;
			nameLabelNewControl.FontWeight = FontWeight.DemiBold;
			StyledProperty<IBrush?> foregroundProperty = TextBlock.ForegroundProperty;
			DynamicResourceExtension nameLabelForegroundResource = new DynamicResourceExtension("ThemeTextPrimary");
			context.ProvideTargetProperty = TextBlock.ForegroundProperty;
			IBinding nameLabelForegroundBinding = nameLabelForegroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(nameLabelNewControl, foregroundProperty, nameLabelForegroundBinding);
			context.PopParent();
			((ISupportInitialize)nameLabelNewEnd).EndInit();
			Avalonia.Controls.Controls nameRowPanelChildren2 = nameRowPanelNewControl.Children;
			Border defaultBadgeNew;
			Border defaultBadgeNewBase = (defaultBadgeNew = new Border());
			((ISupportInitialize)defaultBadgeNewBase).BeginInit();
			nameRowPanelChildren2.Add(defaultBadgeNewBase);
			Border defaultBadgeNewEnd = (cardBorder = defaultBadgeNew);
			context.PushParent(cardBorder);
			Border defaultBadgeNewControl = cardBorder;
			StyledProperty<IBrush?> backgroundProperty2 = Border.BackgroundProperty;
			DynamicResourceExtension defaultBadgeBackgroundResource = new DynamicResourceExtension("BadgeReasoning");
			context.ProvideTargetProperty = Border.BackgroundProperty;
			IBinding defaultBadgeBackgroundBinding = defaultBadgeBackgroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(defaultBadgeNewControl, backgroundProperty2, defaultBadgeBackgroundBinding);
			defaultBadgeNewControl.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
			defaultBadgeNewControl.Padding = new Thickness(6.0, 3.0, 6.0, 3.0);
			StyledProperty<bool> isVisibleProperty = Visual.IsVisibleProperty;
			ReflectionBindingExtension defaultBadgeIsVisibleSource = new ReflectionBindingExtension("IsDefault");
			context.ProvideTargetProperty = Visual.IsVisibleProperty;
			Binding defaultBadgeIsVisibleBinding = defaultBadgeIsVisibleSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(defaultBadgeNewControl, isVisibleProperty, defaultBadgeIsVisibleBinding);
			TextBlock badgeLabelNew;
			TextBlock badgeLabelNewBase = (badgeLabelNew = new TextBlock());
			((ISupportInitialize)badgeLabelNewBase).BeginInit();
			defaultBadgeNewControl.Child = badgeLabelNewBase;
			TextBlock badgeLabelNewEnd = (textBlockRef = badgeLabelNew);
			context.PushParent(textBlockRef);
			TextBlock badgeLabelNewControl = textBlockRef;
			StyledProperty<string?> textProperty2 = TextBlock.TextProperty;
			ReflectionBindingExtension defaultBadgeTextSource = new ReflectionBindingExtension("DataContext.DefaultBadge")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding badgeLabelTextBinding = defaultBadgeTextSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(badgeLabelNewControl, textProperty2, badgeLabelTextBinding);
			badgeLabelNewControl.FontSize = 10.0;
			badgeLabelNewControl.FontWeight = FontWeight.DemiBold;
			StyledProperty<IBrush?> foregroundProperty2 = TextBlock.ForegroundProperty;
			DynamicResourceExtension badgeLabelForegroundResource = new DynamicResourceExtension("ThemeTextPrimary");
			context.ProvideTargetProperty = TextBlock.ForegroundProperty;
			IBinding badgeLabelForegroundBinding = badgeLabelForegroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(badgeLabelNewControl, foregroundProperty2, badgeLabelForegroundBinding);
			context.PopParent();
			((ISupportInitialize)badgeLabelNewEnd).EndInit();
			context.PopParent();
			((ISupportInitialize)defaultBadgeNewEnd).EndInit();
			context.PopParent();
			((ISupportInitialize)nameRowPanelNewEnd).EndInit();
			Avalonia.Controls.Controls cardPanelChildren2 = cardPanelNewControl.Children;
			TextBlock descriptionLabelNew;
			TextBlock descriptionLabelNewBase = (descriptionLabelNew = new TextBlock());
			((ISupportInitialize)descriptionLabelNewBase).BeginInit();
			cardPanelChildren2.Add(descriptionLabelNewBase);
			TextBlock descriptionLabelNewEnd = (textBlockRef = descriptionLabelNew);
			context.PushParent(textBlockRef);
			TextBlock descriptionLabelNewControl = textBlockRef;
			StyledProperty<string?> textProperty3 = TextBlock.TextProperty;
			ReflectionBindingExtension descriptionLabelTextSource = new ReflectionBindingExtension("Description");
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding descriptionLabelTextBinding = descriptionLabelTextSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(descriptionLabelNewControl, textProperty3, descriptionLabelTextBinding);
			descriptionLabelNewControl.FontSize = 12.0;
			StyledProperty<IBrush?> foregroundProperty3 = TextBlock.ForegroundProperty;
			DynamicResourceExtension descriptionLabelForegroundResource = new DynamicResourceExtension("ThemeTextSecondary");
			context.ProvideTargetProperty = TextBlock.ForegroundProperty;
			IBinding descriptionLabelForegroundBinding = descriptionLabelForegroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(descriptionLabelNewControl, foregroundProperty3, descriptionLabelForegroundBinding);
			context.PopParent();
			((ISupportInitialize)descriptionLabelNewEnd).EndInit();
			Avalonia.Controls.Controls cardPanelChildren3 = cardPanelNewControl.Children;
			StackPanel statsRowPanelNew;
			StackPanel statsRowPanelNewBase = (statsRowPanelNew = new StackPanel());
			((ISupportInitialize)statsRowPanelNewBase).BeginInit();
			cardPanelChildren3.Add(statsRowPanelNewBase);
			StackPanel statsRowPanelNewEnd = (stackPanelRef = statsRowPanelNew);
			context.PushParent(stackPanelRef);
			StackPanel statsRowPanelNewControl = stackPanelRef;
			statsRowPanelNewControl.Orientation = Orientation.Horizontal;
			statsRowPanelNewControl.Spacing = 16.0;
			Avalonia.Controls.Controls statsRowPanelChildren = statsRowPanelNewControl.Children;
			TextBlock contextSizeLabelNew;
			TextBlock contextSizeLabelNewBase = (contextSizeLabelNew = new TextBlock());
			((ISupportInitialize)contextSizeLabelNewBase).BeginInit();
			statsRowPanelChildren.Add(contextSizeLabelNewBase);
			TextBlock contextSizeLabelNewEnd = (textBlockRef = contextSizeLabelNew);
			context.PushParent(textBlockRef);
			TextBlock contextSizeLabelNewControl = textBlockRef;
			StyledProperty<string?> textProperty4 = TextBlock.TextProperty;
			ReflectionBindingExtension contextSizeTextSource = new ReflectionBindingExtension("ContextSize")
			{
				StringFormat = "ctx: {0}"
			};
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding contextSizeLabelTextBinding = contextSizeTextSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(contextSizeLabelNewControl, textProperty4, contextSizeLabelTextBinding);
			contextSizeLabelNewControl.FontSize = 11.0;
			StyledProperty<IBrush?> foregroundProperty4 = TextBlock.ForegroundProperty;
			DynamicResourceExtension contextSizeLabelForegroundResource = new DynamicResourceExtension("ThemeTextSecondary");
			context.ProvideTargetProperty = TextBlock.ForegroundProperty;
			IBinding contextSizeLabelForegroundBinding = contextSizeLabelForegroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(contextSizeLabelNewControl, foregroundProperty4, contextSizeLabelForegroundBinding);
			context.PopParent();
			((ISupportInitialize)contextSizeLabelNewEnd).EndInit();
			Avalonia.Controls.Controls statsRowPanelChildren2 = statsRowPanelNewControl.Children;
			TextBlock gpuLayersLabelNew;
			TextBlock gpuLayersLabelNewBase = (gpuLayersLabelNew = new TextBlock());
			((ISupportInitialize)gpuLayersLabelNewBase).BeginInit();
			statsRowPanelChildren2.Add(gpuLayersLabelNewBase);
			TextBlock gpuLayersLabelNewEnd = (textBlockRef = gpuLayersLabelNew);
			context.PushParent(textBlockRef);
			TextBlock gpuLayersLabelNewControl = textBlockRef;
			StyledProperty<string?> textProperty5 = TextBlock.TextProperty;
			ReflectionBindingExtension gpuLayersTextSource = new ReflectionBindingExtension("GpuLayers")
			{
				StringFormat = "gpu: {0}"
			};
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding gpuLayersLabelTextBinding = gpuLayersTextSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(gpuLayersLabelNewControl, textProperty5, gpuLayersLabelTextBinding);
			gpuLayersLabelNewControl.FontSize = 11.0;
			StyledProperty<IBrush?> foregroundProperty5 = TextBlock.ForegroundProperty;
			DynamicResourceExtension gpuLayersLabelForegroundResource = new DynamicResourceExtension("ThemeTextSecondary");
			context.ProvideTargetProperty = TextBlock.ForegroundProperty;
			IBinding gpuLayersLabelForegroundBinding = gpuLayersLabelForegroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(gpuLayersLabelNewControl, foregroundProperty5, gpuLayersLabelForegroundBinding);
			context.PopParent();
			((ISupportInitialize)gpuLayersLabelNewEnd).EndInit();
			Avalonia.Controls.Controls statsRowPanelChildren3 = statsRowPanelNewControl.Children;
			TextBlock temperatureLabelNew;
			TextBlock temperatureLabelNewBase = (temperatureLabelNew = new TextBlock());
			((ISupportInitialize)temperatureLabelNewBase).BeginInit();
			statsRowPanelChildren3.Add(temperatureLabelNewBase);
			TextBlock temperatureLabelNewEnd = (textBlockRef = temperatureLabelNew);
			context.PushParent(textBlockRef);
			TextBlock temperatureLabelNewControl = textBlockRef;
			StyledProperty<string?> textProperty6 = TextBlock.TextProperty;
			ReflectionBindingExtension temperatureTextSource = new ReflectionBindingExtension("Temperature")
			{
				StringFormat = "temp: {0:F2}"
			};
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding temperatureLabelTextBinding = temperatureTextSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(temperatureLabelNewControl, textProperty6, temperatureLabelTextBinding);
			temperatureLabelNewControl.FontSize = 11.0;
			StyledProperty<IBrush?> foregroundProperty6 = TextBlock.ForegroundProperty;
			DynamicResourceExtension temperatureLabelForegroundResource = new DynamicResourceExtension("ThemeTextSecondary");
			context.ProvideTargetProperty = TextBlock.ForegroundProperty;
			IBinding temperatureLabelForegroundBinding = temperatureLabelForegroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(temperatureLabelNewControl, foregroundProperty6, temperatureLabelForegroundBinding);
			context.PopParent();
			((ISupportInitialize)temperatureLabelNewEnd).EndInit();
			context.PopParent();
			((ISupportInitialize)statsRowPanelNewEnd).EndInit();
			Avalonia.Controls.Controls cardPanelChildren4 = cardPanelNewControl.Children;
			TextBlock modelPathLabelNew;
			TextBlock modelPathLabelNewBase = (modelPathLabelNew = new TextBlock());
			((ISupportInitialize)modelPathLabelNewBase).BeginInit();
			cardPanelChildren4.Add(modelPathLabelNewBase);
			TextBlock modelPathLabelNewEnd = (textBlockRef = modelPathLabelNew);
			context.PushParent(textBlockRef);
			TextBlock modelPathLabelNewControl = textBlockRef;
			StyledProperty<string?> textProperty7 = TextBlock.TextProperty;
			ReflectionBindingExtension modelPathLabelTextSource = new ReflectionBindingExtension("ModelPath");
			context.ProvideTargetProperty = TextBlock.TextProperty;
			Binding modelPathLabelTextBinding = modelPathLabelTextSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(modelPathLabelNewControl, textProperty7, modelPathLabelTextBinding);
			modelPathLabelNewControl.FontSize = 11.0;
			StyledProperty<IBrush?> foregroundProperty7 = TextBlock.ForegroundProperty;
			DynamicResourceExtension modelPathLabelForegroundResource = new DynamicResourceExtension("GpuMonitorHeader");
			context.ProvideTargetProperty = TextBlock.ForegroundProperty;
			IBinding modelPathLabelForegroundBinding = modelPathLabelForegroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(modelPathLabelNewControl, foregroundProperty7, modelPathLabelForegroundBinding);
			modelPathLabelNewControl.TextTrimming = TextTrimming.CharacterEllipsis;
			context.PopParent();
			((ISupportInitialize)modelPathLabelNewEnd).EndInit();
			Avalonia.Controls.Controls cardPanelChildren5 = cardPanelNewControl.Children;
			StackPanel actionsRowPanelNew;
			StackPanel actionsRowPanelNewBase = (actionsRowPanelNew = new StackPanel());
			((ISupportInitialize)actionsRowPanelNewBase).BeginInit();
			cardPanelChildren5.Add(actionsRowPanelNewBase);
			StackPanel actionsRowPanelNewEnd = (stackPanelRef = actionsRowPanelNew);
			context.PushParent(stackPanelRef);
			StackPanel actionsRowPanelNewControl = stackPanelRef;
			actionsRowPanelNewControl.Orientation = Orientation.Horizontal;
			actionsRowPanelNewControl.Spacing = 8.0;
			Avalonia.Controls.Controls actionsRowPanelChildren = actionsRowPanelNewControl.Children;
			Button setDefaultButtonNew;
			Button setDefaultButtonNewBase = (setDefaultButtonNew = new Button());
			((ISupportInitialize)setDefaultButtonNewBase).BeginInit();
			actionsRowPanelChildren.Add(setDefaultButtonNewBase);
			Button buttonRef;
			Button setDefaultButtonNewEnd = (buttonRef = setDefaultButtonNew);
			context.PushParent(buttonRef);
			Button setDefaultButtonNewControl = buttonRef;
			setDefaultButtonNewControl.AddHandler((RoutedEvent)Button.ClickEvent, (Delegate)new EventHandler<RoutedEventArgs>(context.RootObject.SetDefaultProfile_Click), RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: false);
			ReflectionBindingExtension setDefaultBtnContentSource = new ReflectionBindingExtension("DataContext.SetDefaultBtn")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = ContentControl.ContentProperty;
			Binding setDefaultButtonContentBinding = setDefaultBtnContentSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(setDefaultButtonNewControl, setDefaultButtonContentBinding);
			StyledProperty<IBrush?> backgroundProperty3 = TemplatedControl.BackgroundProperty;
			DynamicResourceExtension setDefaultButtonBackgroundResource = new DynamicResourceExtension("BtnSuccess");
			context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
			IBinding setDefaultButtonBackgroundBinding = setDefaultButtonBackgroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(setDefaultButtonNewControl, backgroundProperty3, setDefaultButtonBackgroundBinding);
			StyledProperty<IBrush?> foregroundProperty8 = TemplatedControl.ForegroundProperty;
			DynamicResourceExtension setDefaultButtonForegroundResource = new DynamicResourceExtension("BadgeText");
			context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
			IBinding setDefaultButtonForegroundBinding = setDefaultButtonForegroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(setDefaultButtonNewControl, foregroundProperty8, setDefaultButtonForegroundBinding);
			setDefaultButtonNewControl.Padding = new Thickness(10.0, 6.0, 10.0, 6.0);
			setDefaultButtonNewControl.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
			StyledProperty<bool> isEnabledProperty = InputElement.IsEnabledProperty;
			ReflectionBindingExtension setDefaultButtonIsEnabledSource2;
			ReflectionBindingExtension setDefaultButtonIsEnabledSource = (setDefaultButtonIsEnabledSource2 = new ReflectionBindingExtension("IsDefault"));
			context.PushParent(setDefaultButtonIsEnabledSource2);
			context.ProvideTargetProperty = CompiledAvaloniaXaml.XamlIlHelpers.Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverter_0021Property();
			object? invertedBoolConverter = new StaticResourceExtension("InvertedBoolConverter").ProvideValue(context);
			context.ProvideTargetProperty = null;
			setDefaultButtonIsEnabledSource2.Converter = (IValueConverter)invertedBoolConverter;
			context.PopParent();
			context.ProvideTargetProperty = InputElement.IsEnabledProperty;
			Binding setDefaultButtonIsEnabledBinding = setDefaultButtonIsEnabledSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(setDefaultButtonNewControl, isEnabledProperty, setDefaultButtonIsEnabledBinding);
			context.PopParent();
			((ISupportInitialize)setDefaultButtonNewEnd).EndInit();
			Avalonia.Controls.Controls actionsRowPanelChildren2 = actionsRowPanelNewControl.Children;
			Button editButtonNew;
			Button editButtonNewBase = (editButtonNew = new Button());
			((ISupportInitialize)editButtonNewBase).BeginInit();
			actionsRowPanelChildren2.Add(editButtonNewBase);
			Button editButtonNewEnd = (buttonRef = editButtonNew);
			context.PushParent(buttonRef);
			Button editButtonNewControl = buttonRef;
			StyledProperty<ICommand?> commandProperty = Button.CommandProperty;
			ReflectionBindingExtension editProfileCommandSource = new ReflectionBindingExtension("DataContext.EditProfileCommand")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = Button.CommandProperty;
			Binding editButtonCommandBinding = editProfileCommandSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(editButtonNewControl, commandProperty, editButtonCommandBinding);
			ReflectionBindingExtension editButtonCommandParameterSource = new ReflectionBindingExtension();
			context.ProvideTargetProperty = Button.CommandParameterProperty;
			Binding editButtonCommandParameterBinding = editButtonCommandParameterSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_5(editButtonNewControl, editButtonCommandParameterBinding);
			ReflectionBindingExtension editBtnContentSource = new ReflectionBindingExtension("DataContext.EditBtn")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = ContentControl.ContentProperty;
			Binding editButtonContentBinding = editBtnContentSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(editButtonNewControl, editButtonContentBinding);
			StyledProperty<IBrush?> backgroundProperty4 = TemplatedControl.BackgroundProperty;
			DynamicResourceExtension editButtonBackgroundResource = new DynamicResourceExtension("BtnPrimary");
			context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
			IBinding editButtonBackgroundBinding = editButtonBackgroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(editButtonNewControl, backgroundProperty4, editButtonBackgroundBinding);
			StyledProperty<IBrush?> foregroundProperty9 = TemplatedControl.ForegroundProperty;
			DynamicResourceExtension editButtonForegroundResource = new DynamicResourceExtension("BadgeText");
			context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
			IBinding editButtonForegroundBinding = editButtonForegroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(editButtonNewControl, foregroundProperty9, editButtonForegroundBinding);
			editButtonNewControl.Padding = new Thickness(10.0, 6.0, 10.0, 6.0);
			editButtonNewControl.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
			context.PopParent();
			((ISupportInitialize)editButtonNewEnd).EndInit();
			Avalonia.Controls.Controls actionsRowPanelChildren3 = actionsRowPanelNewControl.Children;
			Button duplicateButtonNew;
			Button duplicateButtonNewBase = (duplicateButtonNew = new Button());
			((ISupportInitialize)duplicateButtonNewBase).BeginInit();
			actionsRowPanelChildren3.Add(duplicateButtonNewBase);
			Button duplicateButtonNewEnd = (buttonRef = duplicateButtonNew);
			context.PushParent(buttonRef);
			Button duplicateButtonNewControl = buttonRef;
			StyledProperty<ICommand?> commandProperty2 = Button.CommandProperty;
			ReflectionBindingExtension duplicateProfileCommandSource = new ReflectionBindingExtension("DataContext.DuplicateProfileCommand")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = Button.CommandProperty;
			Binding duplicateButtonCommandBinding = duplicateProfileCommandSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(duplicateButtonNewControl, commandProperty2, duplicateButtonCommandBinding);
			ReflectionBindingExtension duplicateButtonCommandParameterSource = new ReflectionBindingExtension();
			context.ProvideTargetProperty = Button.CommandParameterProperty;
			Binding duplicateButtonCommandParameterBinding = duplicateButtonCommandParameterSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_5(duplicateButtonNewControl, duplicateButtonCommandParameterBinding);
			ReflectionBindingExtension duplicateBtnContentSource = new ReflectionBindingExtension("DataContext.DuplicateBtn")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = ContentControl.ContentProperty;
			Binding duplicateButtonContentBinding = duplicateBtnContentSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(duplicateButtonNewControl, duplicateButtonContentBinding);
			StyledProperty<IBrush?> backgroundProperty5 = TemplatedControl.BackgroundProperty;
			DynamicResourceExtension duplicateButtonBackgroundResource = new DynamicResourceExtension("BtnSecondary");
			context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
			IBinding duplicateButtonBackgroundBinding = duplicateButtonBackgroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(duplicateButtonNewControl, backgroundProperty5, duplicateButtonBackgroundBinding);
			StyledProperty<IBrush?> foregroundProperty10 = TemplatedControl.ForegroundProperty;
			DynamicResourceExtension duplicateButtonForegroundResource = new DynamicResourceExtension("ThemeTextPrimary");
			context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
			IBinding duplicateButtonForegroundBinding = duplicateButtonForegroundResource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(duplicateButtonNewControl, foregroundProperty10, duplicateButtonForegroundBinding);
			duplicateButtonNewControl.Padding = new Thickness(10.0, 6.0, 10.0, 6.0);
			duplicateButtonNewControl.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
			context.PopParent();
			((ISupportInitialize)duplicateButtonNewEnd).EndInit();
			Avalonia.Controls.Controls actionsRowPanelChildren4 = actionsRowPanelNewControl.Children;
			Button exportButtonNew;
			Button exportButtonNewBase = (exportButtonNew = new Button());
			((ISupportInitialize)exportButtonNewBase).BeginInit();
			actionsRowPanelChildren4.Add(exportButtonNewBase);
			Button exportButtonNewEnd = (buttonRef = exportButtonNew);
			context.PushParent(buttonRef);
			Button exportButtonNewControl = buttonRef;
			StyledProperty<ICommand?> commandProperty3 = Button.CommandProperty;
			ReflectionBindingExtension exportProfileCommandSource = new ReflectionBindingExtension("DataContext.ExportProfileCommand")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = Button.CommandProperty;
			Binding exportButtonCommandBinding = exportProfileCommandSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(exportButtonNewControl, commandProperty3, exportButtonCommandBinding);
			ReflectionBindingExtension exportButtonCommandParameterSource = new ReflectionBindingExtension();
			context.ProvideTargetProperty = Button.CommandParameterProperty;
			Binding exportButtonCommandParameterBinding = exportButtonCommandParameterSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_5(exportButtonNewControl, exportButtonCommandParameterBinding);
			ReflectionBindingExtension exportBtnContentSource = new ReflectionBindingExtension("DataContext.ExportBtn")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = ContentControl.ContentProperty;
			Binding exportButtonContentBinding = exportBtnContentSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(exportButtonNewControl, exportButtonContentBinding);
			StyledProperty<IBrush?> backgroundProperty6 = TemplatedControl.BackgroundProperty;
			context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
			IBinding exportButtonBackgroundBinding = new DynamicResourceExtension("BtnSecondary").ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(exportButtonNewControl, backgroundProperty6, exportButtonBackgroundBinding);
			StyledProperty<IBrush?> foregroundProperty11 = TemplatedControl.ForegroundProperty;
			context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
			IBinding exportButtonForegroundBinding = new DynamicResourceExtension("ThemeTextPrimary").ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(exportButtonNewControl, foregroundProperty11, exportButtonForegroundBinding);
			exportButtonNewControl.Padding = new Thickness(10.0, 6.0, 10.0, 6.0);
			exportButtonNewControl.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
			context.PopParent();
			((ISupportInitialize)exportButtonNewEnd).EndInit();
			Avalonia.Controls.Controls actionsRowPanelChildren5 = actionsRowPanelNewControl.Children;
			Button deleteButtonNew;
			Button deleteButtonNewBase = (deleteButtonNew = new Button());
			((ISupportInitialize)deleteButtonNewBase).BeginInit();
			actionsRowPanelChildren5.Add(deleteButtonNewBase);
			Button deleteButtonNewEnd = (buttonRef = deleteButtonNew);
			context.PushParent(buttonRef);
			Button deleteButtonNewControl = buttonRef;
			StyledProperty<ICommand?> commandProperty4 = Button.CommandProperty;
			ReflectionBindingExtension deleteProfileCommandSource = new ReflectionBindingExtension("DataContext.DeleteProfileCommand")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = Button.CommandProperty;
			Binding deleteButtonCommandBinding = deleteProfileCommandSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(deleteButtonNewControl, commandProperty4, deleteButtonCommandBinding);
			ReflectionBindingExtension deleteButtonCommandParameterSource = new ReflectionBindingExtension();
			context.ProvideTargetProperty = Button.CommandParameterProperty;
			Binding deleteButtonCommandParameterBinding = deleteButtonCommandParameterSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_5(deleteButtonNewControl, deleteButtonCommandParameterBinding);
			ReflectionBindingExtension deleteBtnContentSource = new ReflectionBindingExtension("DataContext.DeleteBtn")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = ContentControl.ContentProperty;
			Binding deleteButtonContentBinding = deleteBtnContentSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(deleteButtonNewControl, deleteButtonContentBinding);
			StyledProperty<IBrush?> backgroundProperty7 = TemplatedControl.BackgroundProperty;
			context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
			IBinding deleteButtonBackgroundBinding = new DynamicResourceExtension("BtnDanger").ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(deleteButtonNewControl, backgroundProperty7, deleteButtonBackgroundBinding);
			StyledProperty<IBrush?> foregroundProperty12 = TemplatedControl.ForegroundProperty;
			context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
			IBinding deleteButtonForegroundBinding = new DynamicResourceExtension("BadgeText").ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(deleteButtonNewControl, foregroundProperty12, deleteButtonForegroundBinding);
			deleteButtonNewControl.Padding = new Thickness(10.0, 6.0, 10.0, 6.0);
			deleteButtonNewControl.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
			context.PopParent();
			((ISupportInitialize)deleteButtonNewEnd).EndInit();
			context.PopParent();
			((ISupportInitialize)actionsRowPanelNewEnd).EndInit();
			Avalonia.Controls.Controls cardPanelChildren6 = cardPanelNewControl.Children;
			StackPanel bottomRowPanelNew;
			StackPanel bottomRowPanelNewBase = (bottomRowPanelNew = new StackPanel());
			((ISupportInitialize)bottomRowPanelNewBase).BeginInit();
			cardPanelChildren6.Add(bottomRowPanelNewBase);
			StackPanel bottomRowPanelNewEnd = (stackPanelRef = bottomRowPanelNew);
			context.PushParent(stackPanelRef);
			StackPanel bottomRowPanelNewControl = stackPanelRef;
			bottomRowPanelNewControl.Orientation = Orientation.Horizontal;
			bottomRowPanelNewControl.Spacing = 8.0;
			Avalonia.Controls.Controls bottomRowPanelChildren = bottomRowPanelNewControl.Children;
			Button saveSettingsButtonNew;
			Button saveSettingsButtonNewBase = (saveSettingsButtonNew = new Button());
			((ISupportInitialize)saveSettingsButtonNewBase).BeginInit();
			bottomRowPanelChildren.Add(saveSettingsButtonNewBase);
			Button saveSettingsButtonNewEnd = (buttonRef = saveSettingsButtonNew);
			context.PushParent(buttonRef);
			Button saveSettingsButtonNewControl = buttonRef;
			StyledProperty<ICommand?> commandProperty5 = Button.CommandProperty;
			ReflectionBindingExtension saveSettingsCommandSource = new ReflectionBindingExtension("DataContext.SaveSettingsToProfileCommand")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = Button.CommandProperty;
			Binding saveSettingsButtonCommandBinding = saveSettingsCommandSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(saveSettingsButtonNewControl, commandProperty5, saveSettingsButtonCommandBinding);
			ReflectionBindingExtension saveSettingsButtonCommandParameterSource = new ReflectionBindingExtension();
			context.ProvideTargetProperty = Button.CommandParameterProperty;
			Binding saveSettingsButtonCommandParameterBinding = saveSettingsButtonCommandParameterSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_5(saveSettingsButtonNewControl, saveSettingsButtonCommandParameterBinding);
			ReflectionBindingExtension saveSettingsBtnContentSource = new ReflectionBindingExtension("DataContext.SaveSettingsBtn")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = ContentControl.ContentProperty;
			Binding saveSettingsButtonContentBinding = saveSettingsBtnContentSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(saveSettingsButtonNewControl, saveSettingsButtonContentBinding);
			StyledProperty<IBrush?> backgroundProperty8 = TemplatedControl.BackgroundProperty;
			context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
			IBinding saveSettingsButtonBackgroundBinding = new DynamicResourceExtension("BtnPurple").ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(saveSettingsButtonNewControl, backgroundProperty8, saveSettingsButtonBackgroundBinding);
			StyledProperty<IBrush?> foregroundProperty13 = TemplatedControl.ForegroundProperty;
			context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
			IBinding saveSettingsButtonForegroundBinding = new DynamicResourceExtension("BadgeText").ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(saveSettingsButtonNewControl, foregroundProperty13, saveSettingsButtonForegroundBinding);
			saveSettingsButtonNewControl.Padding = new Thickness(10.0, 6.0, 10.0, 6.0);
			saveSettingsButtonNewControl.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
			context.PopParent();
			((ISupportInitialize)saveSettingsButtonNewEnd).EndInit();
			Avalonia.Controls.Controls bottomRowPanelChildren2 = bottomRowPanelNewControl.Children;
			Button startServerButtonNew;
			Button startServerButtonNewBase = (startServerButtonNew = new Button());
			((ISupportInitialize)startServerButtonNewBase).BeginInit();
			bottomRowPanelChildren2.Add(startServerButtonNewBase);
			Button startServerButtonNewEnd = (buttonRef = startServerButtonNew);
			context.PushParent(buttonRef);
			Button startServerButtonNewControl = buttonRef;
			StyledProperty<ICommand?> commandProperty6 = Button.CommandProperty;
			ReflectionBindingExtension startServerCommandSource = new ReflectionBindingExtension("DataContext.StartServerWithProfileCommand")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = Button.CommandProperty;
			Binding startServerButtonCommandBinding = startServerCommandSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(startServerButtonNewControl, commandProperty6, startServerButtonCommandBinding);
			ReflectionBindingExtension startServerButtonCommandParameterSource = new ReflectionBindingExtension();
			context.ProvideTargetProperty = Button.CommandParameterProperty;
			Binding startServerButtonCommandParameterBinding = startServerButtonCommandParameterSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_5(startServerButtonNewControl, startServerButtonCommandParameterBinding);
			ReflectionBindingExtension startServerBtnContentSource = new ReflectionBindingExtension("DataContext.StartServerBtn")
			{
				ElementName = "Root"
			};
			context.ProvideTargetProperty = ContentControl.ContentProperty;
			Binding startServerButtonContentBinding = startServerBtnContentSource.ProvideValue(context);
			context.ProvideTargetProperty = null;
			CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(startServerButtonNewControl, startServerButtonContentBinding);
			StyledProperty<IBrush?> backgroundProperty9 = TemplatedControl.BackgroundProperty;
			context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
			IBinding startServerButtonBackgroundBinding = new DynamicResourceExtension("BtnSuccess").ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(startServerButtonNewControl, backgroundProperty9, startServerButtonBackgroundBinding);
			StyledProperty<IBrush?> foregroundProperty14 = TemplatedControl.ForegroundProperty;
			context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
			IBinding startServerButtonForegroundBinding = new DynamicResourceExtension("BadgeText").ProvideValue(context);
			context.ProvideTargetProperty = null;
			AvaloniaObjectExtensions.Bind(startServerButtonNewControl, foregroundProperty14, startServerButtonForegroundBinding);
			startServerButtonNewControl.Padding = new Thickness(10.0, 6.0, 10.0, 6.0);
			startServerButtonNewControl.CornerRadius = new CornerRadius(6.0, 6.0, 6.0, 6.0);
			context.PopParent();
			((ISupportInitialize)startServerButtonNewEnd).EndInit();
			context.PopParent();
			((ISupportInitialize)bottomRowPanelNewEnd).EndInit();
			context.PopParent();
			((ISupportInitialize)cardPanelNewEnd).EndInit();
			context.PopParent();
			((ISupportInitialize)rootElement).EndInit();
			return rootElement;
		}

		public static CompiledAvaloniaXaml.XamlIlContext.Context<ProfilesPage> CreateContext(IServiceProvider serviceProvider)
		{
			CompiledAvaloniaXaml.XamlIlContext.Context<ProfilesPage> context = new CompiledAvaloniaXaml.XamlIlContext.Context<ProfilesPage>(serviceProvider, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FViews_002FPages_002FProfilesPage_002Eaxaml.Singleton }, "avares://LlamaStudio/Views/Pages/ProfilesPage.axaml");
			if (serviceProvider != null)
			{
				object service = serviceProvider.GetService(typeof(IRootObjectProvider));
				if (service != null)
				{
					service = ((IRootObjectProvider)service).RootObject;
					context.RootObject = (ProfilesPage)service;
				}
			}
			return context;
		}
	}

	[GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.1.0")]
	internal UserControl Root;

	private static Action<object> _0021XamlIlPopulateOverride;

	public ProfilesPage()
	{
		InitializeComponent();
	}

	public ProfilesPage(ProfilesViewModel viewModel)
		: this()
	{
		base.DataContext = viewModel;
	}

	private void InitializeComponent()
	{
		_0021XamlIlPopulateTrampoline(this);
	}

	private async void SetDefaultProfile_Click(object? sender, RoutedEventArgs e)
	{
		if (sender is Button { DataContext: ServerProfile dataContext } && base.DataContext is ProfilesViewModel profilesViewModel)
		{
			await profilesViewModel.SetDefault(dataContext);
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
		Root = this.FindNameScope()?.Find<UserControl>("Root");
	}

	private unsafe static void _0021XamlIlPopulate(IServiceProvider serviceProvider, ProfilesPage page)
	{
		CompiledAvaloniaXaml.XamlIlContext.Context<ProfilesPage> context = new CompiledAvaloniaXaml.XamlIlContext.Context<ProfilesPage>(serviceProvider, new object[1] { _0021AvaloniaResources.NamespaceInfo_003A_002FViews_002FPages_002FProfilesPage_002Eaxaml.Singleton }, "avares://LlamaStudio/Views/Pages/ProfilesPage.axaml")
		{
			RootObject = page,
			IntermediateRoot = page
		};
		((ISupportInitialize)page).BeginInit();
		context.PushParent(page);
		page.Name = "Root";
		context.AvaloniaNameScope.Register("Root", page);
		DockPanel rootPanelNew;
		DockPanel rootPanelNewBase = (rootPanelNew = new DockPanel());
		((ISupportInitialize)rootPanelNewBase).BeginInit();
		page.Content = rootPanelNewBase;
		DockPanel dockPanelRef;
		DockPanel rootPanelNewEnd = (dockPanelRef = rootPanelNew);
		context.PushParent(dockPanelRef);
		Avalonia.Controls.Controls rootPanelChildren = dockPanelRef.Children;
		Border headerBorderNew;
		Border headerBorderNewBase = (headerBorderNew = new Border());
		((ISupportInitialize)headerBorderNewBase).BeginInit();
		rootPanelChildren.Add(headerBorderNewBase);
		Border borderRef;
		Border headerBorderNewEnd = (borderRef = headerBorderNew);
		context.PushParent(borderRef);
		DockPanel.SetDock(borderRef, Dock.Top);
		StyledProperty<IBrush?> backgroundProperty = Border.BackgroundProperty;
		DynamicResourceExtension headerBorderBackgroundResource = new DynamicResourceExtension("ThemeSurface");
		context.ProvideTargetProperty = Border.BackgroundProperty;
		IBinding headerBorderBackgroundBinding = headerBorderBackgroundResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(borderRef, backgroundProperty, headerBorderBackgroundBinding);
		borderRef.Padding = new Thickness(16.0, 16.0, 16.0, 16.0);
		StyledProperty<IBrush?> borderBrushProperty = Border.BorderBrushProperty;
		DynamicResourceExtension headerBorderBorderBrushResource = new DynamicResourceExtension("ThemeBorder");
		context.ProvideTargetProperty = Border.BorderBrushProperty;
		IBinding headerBorderBorderBrushBinding = headerBorderBorderBrushResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(borderRef, borderBrushProperty, headerBorderBorderBrushBinding);
		borderRef.BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0);
		borderRef.Margin = new Thickness(0.0, 0.0, 0.0, 8.0);
		StackPanel headerPanelNew;
		StackPanel headerPanelNewBase = (headerPanelNew = new StackPanel());
		((ISupportInitialize)headerPanelNewBase).BeginInit();
		borderRef.Child = headerPanelNewBase;
		StackPanel stackPanelRef;
		StackPanel headerPanelNewEnd = (stackPanelRef = headerPanelNew);
		context.PushParent(stackPanelRef);
		StackPanel headerPanelNewControl = stackPanelRef;
		headerPanelNewControl.Spacing = 12.0;
		Avalonia.Controls.Controls headerPanelChildren = headerPanelNewControl.Children;
		StackPanel headerRowPanelNew;
		StackPanel headerRowPanelNewBase = (headerRowPanelNew = new StackPanel());
		((ISupportInitialize)headerRowPanelNewBase).BeginInit();
		headerPanelChildren.Add(headerRowPanelNewBase);
		StackPanel headerRowPanelNewEnd = (stackPanelRef = headerRowPanelNew);
		context.PushParent(stackPanelRef);
		StackPanel headerRowPanelNewControl = stackPanelRef;
		headerRowPanelNewControl.Orientation = Orientation.Horizontal;
		headerRowPanelNewControl.Spacing = 12.0;
		Avalonia.Controls.Controls headerRowPanelChildren = headerRowPanelNewControl.Children;
		TextBlock titleLabelNew;
		TextBlock titleLabelNewBase = (titleLabelNew = new TextBlock());
		((ISupportInitialize)titleLabelNewBase).BeginInit();
		headerRowPanelChildren.Add(titleLabelNewBase);
		TextBlock textBlockRef;
		TextBlock titleLabelNewEnd = (textBlockRef = titleLabelNew);
		context.PushParent(textBlockRef);
		TextBlock titleLabelNewControl = textBlockRef;
		StyledProperty<string?> textProperty = TextBlock.TextProperty;
		ReflectionBindingExtension titleLabelTextSource = new ReflectionBindingExtension("Title");
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding titleLabelTextBinding = titleLabelTextSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(titleLabelNewControl, textProperty, titleLabelTextBinding);
		titleLabelNewControl.FontSize = 24.0;
		titleLabelNewControl.FontWeight = FontWeight.Bold;
		StyledProperty<IBrush?> foregroundProperty = TextBlock.ForegroundProperty;
		DynamicResourceExtension titleLabelForegroundResource = new DynamicResourceExtension("ThemeTextPrimary");
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding titleLabelForegroundBinding = titleLabelForegroundResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(titleLabelNewControl, foregroundProperty, titleLabelForegroundBinding);
		titleLabelNewControl.VerticalAlignment = VerticalAlignment.Center;
		context.PopParent();
		((ISupportInitialize)titleLabelNewEnd).EndInit();
		Avalonia.Controls.Controls headerRowPanelChildren2 = headerRowPanelNewControl.Children;
		Button newProfileButtonNew;
		Button newProfileButtonNewBase = (newProfileButtonNew = new Button());
		((ISupportInitialize)newProfileButtonNewBase).BeginInit();
		headerRowPanelChildren2.Add(newProfileButtonNewBase);
		Button buttonRef;
		Button newProfileButtonNewEnd = (buttonRef = newProfileButtonNew);
		context.PushParent(buttonRef);
		Button newProfileButtonNewControl = buttonRef;
		StyledProperty<ICommand?> commandProperty = Button.CommandProperty;
		ReflectionBindingExtension newProfileButtonCommandSource = new ReflectionBindingExtension("CreateProfileCommand");
		context.ProvideTargetProperty = Button.CommandProperty;
		Binding newProfileButtonCommandBinding = newProfileButtonCommandSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(newProfileButtonNewControl, commandProperty, newProfileButtonCommandBinding);
		ReflectionBindingExtension newProfileButtonContentSource = new ReflectionBindingExtension("NewProfileBtn");
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding newProfileButtonContentBinding = newProfileButtonContentSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(newProfileButtonNewControl, newProfileButtonContentBinding);
		StyledProperty<IBrush?> backgroundProperty2 = TemplatedControl.BackgroundProperty;
		DynamicResourceExtension newProfileButtonBackgroundResource = new DynamicResourceExtension("BtnPrimary");
		context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
		IBinding newProfileButtonBackgroundBinding = newProfileButtonBackgroundResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(newProfileButtonNewControl, backgroundProperty2, newProfileButtonBackgroundBinding);
		StyledProperty<IBrush?> foregroundProperty2 = TemplatedControl.ForegroundProperty;
		DynamicResourceExtension newProfileButtonForegroundResource = new DynamicResourceExtension("BadgeText");
		context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
		IBinding newProfileButtonForegroundBinding = newProfileButtonForegroundResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(newProfileButtonNewControl, foregroundProperty2, newProfileButtonForegroundBinding);
		newProfileButtonNewControl.Padding = new Thickness(16.0, 8.0, 16.0, 8.0);
		newProfileButtonNewControl.CornerRadius = new CornerRadius(8.0, 8.0, 8.0, 8.0);
		context.PopParent();
		((ISupportInitialize)newProfileButtonNewEnd).EndInit();
		Avalonia.Controls.Controls headerRowPanelChildren3 = headerRowPanelNewControl.Children;
		Button importProfileButtonNew;
		Button importProfileButtonNewBase = (importProfileButtonNew = new Button());
		((ISupportInitialize)importProfileButtonNewBase).BeginInit();
		headerRowPanelChildren3.Add(importProfileButtonNewBase);
		Button importProfileButtonNewEnd = (buttonRef = importProfileButtonNew);
		context.PushParent(buttonRef);
		Button importProfileButtonNewControl = buttonRef;
		StyledProperty<ICommand?> commandProperty2 = Button.CommandProperty;
		ReflectionBindingExtension importProfileButtonCommandSource = new ReflectionBindingExtension("ImportProfileCommand");
		context.ProvideTargetProperty = Button.CommandProperty;
		Binding importProfileButtonCommandBinding = importProfileButtonCommandSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(importProfileButtonNewControl, commandProperty2, importProfileButtonCommandBinding);
		ReflectionBindingExtension importProfileButtonContentSource = new ReflectionBindingExtension("ImportJsonBtn");
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding importProfileButtonContentBinding = importProfileButtonContentSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(importProfileButtonNewControl, importProfileButtonContentBinding);
		StyledProperty<IBrush?> backgroundProperty3 = TemplatedControl.BackgroundProperty;
		DynamicResourceExtension importProfileButtonBackgroundResource = new DynamicResourceExtension("BtnSecondary");
		context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
		IBinding importProfileButtonBackgroundBinding = importProfileButtonBackgroundResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(importProfileButtonNewControl, backgroundProperty3, importProfileButtonBackgroundBinding);
		StyledProperty<IBrush?> foregroundProperty3 = TemplatedControl.ForegroundProperty;
		DynamicResourceExtension importProfileButtonForegroundResource = new DynamicResourceExtension("ThemeTextPrimary");
		context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
		IBinding importProfileButtonForegroundBinding = importProfileButtonForegroundResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(importProfileButtonNewControl, foregroundProperty3, importProfileButtonForegroundBinding);
		importProfileButtonNewControl.Padding = new Thickness(12.0, 8.0, 12.0, 8.0);
		importProfileButtonNewControl.CornerRadius = new CornerRadius(8.0, 8.0, 8.0, 8.0);
		context.PopParent();
		((ISupportInitialize)importProfileButtonNewEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)headerRowPanelNewEnd).EndInit();
		Avalonia.Controls.Controls headerPanelChildren2 = headerPanelNewControl.Children;
		StackPanel profileRowPanelNew;
		StackPanel profileRowPanelNewBase = (profileRowPanelNew = new StackPanel());
		((ISupportInitialize)profileRowPanelNewBase).BeginInit();
		headerPanelChildren2.Add(profileRowPanelNewBase);
		StackPanel profileRowPanelNewEnd = (stackPanelRef = profileRowPanelNew);
		context.PushParent(stackPanelRef);
		StackPanel profileRowPanelNewControl = stackPanelRef;
		profileRowPanelNewControl.Orientation = Orientation.Horizontal;
		profileRowPanelNewControl.Spacing = 8.0;
		Avalonia.Controls.Controls profileRowPanelChildren = profileRowPanelNewControl.Children;
		TextBox profileNameBoxNew;
		TextBox profileNameBoxNewBase = (profileNameBoxNew = new TextBox());
		((ISupportInitialize)profileNameBoxNewBase).BeginInit();
		profileRowPanelChildren.Add(profileNameBoxNewBase);
		TextBox textBoxRef;
		TextBox profileNameBoxNewEnd = (textBoxRef = profileNameBoxNew);
		context.PushParent(textBoxRef);
		TextBox profileNameBoxNewControl = textBoxRef;
		StyledProperty<string?> textProperty2 = TextBox.TextProperty;
		ReflectionBindingExtension profileNameTextSource = new ReflectionBindingExtension("ProfileName")
		{
			Mode = BindingMode.TwoWay
		};
		context.ProvideTargetProperty = TextBox.TextProperty;
		Binding profileNameBoxTextBinding = profileNameTextSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(profileNameBoxNewControl, textProperty2, profileNameBoxTextBinding);
		StyledProperty<string?> watermarkProperty = TextBox.WatermarkProperty;
		ReflectionBindingExtension profileNameBoxWatermarkSource = new ReflectionBindingExtension("NamePlaceholder");
		context.ProvideTargetProperty = TextBox.WatermarkProperty;
		Binding profileNameBoxWatermarkBinding = profileNameBoxWatermarkSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(profileNameBoxNewControl, watermarkProperty, profileNameBoxWatermarkBinding);
		StyledProperty<IBrush?> backgroundProperty4 = TemplatedControl.BackgroundProperty;
		DynamicResourceExtension profileNameBoxBackgroundResource = new DynamicResourceExtension("ThemeCard");
		context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
		IBinding profileNameBoxBackgroundBinding = profileNameBoxBackgroundResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(profileNameBoxNewControl, backgroundProperty4, profileNameBoxBackgroundBinding);
		StyledProperty<IBrush?> foregroundProperty4 = TemplatedControl.ForegroundProperty;
		DynamicResourceExtension profileNameBoxForegroundResource = new DynamicResourceExtension("ThemeTextPrimary");
		context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
		IBinding profileNameBoxForegroundBinding = profileNameBoxForegroundResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(profileNameBoxNewControl, foregroundProperty4, profileNameBoxForegroundBinding);
		StyledProperty<IBrush?> borderBrushProperty2 = TemplatedControl.BorderBrushProperty;
		DynamicResourceExtension profileNameBoxBorderBrushResource = new DynamicResourceExtension("ThemeBorder");
		context.ProvideTargetProperty = TemplatedControl.BorderBrushProperty;
		IBinding profileNameBoxBorderBrushBinding = profileNameBoxBorderBrushResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(profileNameBoxNewControl, borderBrushProperty2, profileNameBoxBorderBrushBinding);
		profileNameBoxNewControl.Padding = new Thickness(10.0, 8.0, 10.0, 8.0);
		profileNameBoxNewControl.CornerRadius = new CornerRadius(8.0, 8.0, 8.0, 8.0);
		profileNameBoxNewControl.MinWidth = 200.0;
		context.PopParent();
		((ISupportInitialize)profileNameBoxNewEnd).EndInit();
		Avalonia.Controls.Controls profileRowPanelChildren2 = profileRowPanelNewControl.Children;
		TextBox profileDescriptionBoxNew;
		TextBox profileDescriptionBoxNewBase = (profileDescriptionBoxNew = new TextBox());
		((ISupportInitialize)profileDescriptionBoxNewBase).BeginInit();
		profileRowPanelChildren2.Add(profileDescriptionBoxNewBase);
		TextBox profileDescriptionBoxNewEnd = (textBoxRef = profileDescriptionBoxNew);
		context.PushParent(textBoxRef);
		TextBox profileDescriptionBoxNewControl = textBoxRef;
		StyledProperty<string?> textProperty3 = TextBox.TextProperty;
		ReflectionBindingExtension profileDescriptionTextSource = new ReflectionBindingExtension("ProfileDescription")
		{
			Mode = BindingMode.TwoWay
		};
		context.ProvideTargetProperty = TextBox.TextProperty;
		Binding profileDescriptionBoxTextBinding = profileDescriptionTextSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(profileDescriptionBoxNewControl, textProperty3, profileDescriptionBoxTextBinding);
		StyledProperty<string?> watermarkProperty2 = TextBox.WatermarkProperty;
		ReflectionBindingExtension profileDescriptionBoxWatermarkSource = new ReflectionBindingExtension("DescPlaceholder");
		context.ProvideTargetProperty = TextBox.WatermarkProperty;
		Binding profileDescriptionBoxWatermarkBinding = profileDescriptionBoxWatermarkSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(profileDescriptionBoxNewControl, watermarkProperty2, profileDescriptionBoxWatermarkBinding);
		StyledProperty<IBrush?> backgroundProperty5 = TemplatedControl.BackgroundProperty;
		DynamicResourceExtension profileDescriptionBoxBackgroundResource = new DynamicResourceExtension("ThemeCard");
		context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
		IBinding profileDescriptionBoxBackgroundBinding = profileDescriptionBoxBackgroundResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(profileDescriptionBoxNewControl, backgroundProperty5, profileDescriptionBoxBackgroundBinding);
		StyledProperty<IBrush?> foregroundProperty5 = TemplatedControl.ForegroundProperty;
		DynamicResourceExtension profileDescriptionBoxForegroundResource = new DynamicResourceExtension("ThemeTextPrimary");
		context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
		IBinding profileDescriptionBoxForegroundBinding = profileDescriptionBoxForegroundResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(profileDescriptionBoxNewControl, foregroundProperty5, profileDescriptionBoxForegroundBinding);
		StyledProperty<IBrush?> borderBrushProperty3 = TemplatedControl.BorderBrushProperty;
		DynamicResourceExtension profileDescriptionBoxBorderBrushResource = new DynamicResourceExtension("ThemeBorder");
		context.ProvideTargetProperty = TemplatedControl.BorderBrushProperty;
		IBinding profileDescriptionBoxBorderBrushBinding = profileDescriptionBoxBorderBrushResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(profileDescriptionBoxNewControl, borderBrushProperty3, profileDescriptionBoxBorderBrushBinding);
		profileDescriptionBoxNewControl.Padding = new Thickness(10.0, 8.0, 10.0, 8.0);
		profileDescriptionBoxNewControl.CornerRadius = new CornerRadius(8.0, 8.0, 8.0, 8.0);
		profileDescriptionBoxNewControl.MinWidth = 300.0;
		context.PopParent();
		((ISupportInitialize)profileDescriptionBoxNewEnd).EndInit();
		Avalonia.Controls.Controls profileRowPanelChildren3 = profileRowPanelNewControl.Children;
		Button selectModelButtonNew;
		Button selectModelButtonNewBase = (selectModelButtonNew = new Button());
		((ISupportInitialize)selectModelButtonNewBase).BeginInit();
		profileRowPanelChildren3.Add(selectModelButtonNewBase);
		Button selectModelButtonNewEnd = (buttonRef = selectModelButtonNew);
		context.PushParent(buttonRef);
		Button selectModelButtonNewControl = buttonRef;
		StyledProperty<ICommand?> commandProperty3 = Button.CommandProperty;
		ReflectionBindingExtension selectModelButtonCommandSource = new ReflectionBindingExtension("SelectModelCommand");
		context.ProvideTargetProperty = Button.CommandProperty;
		Binding selectModelButtonCommandBinding = selectModelButtonCommandSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(selectModelButtonNewControl, commandProperty3, selectModelButtonCommandBinding);
		ReflectionBindingExtension selectModelButtonContentSource = new ReflectionBindingExtension("SelectModelPlaceholder");
		context.ProvideTargetProperty = ContentControl.ContentProperty;
		Binding selectModelButtonContentBinding = selectModelButtonContentSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_2(selectModelButtonNewControl, selectModelButtonContentBinding);
		StyledProperty<IBrush?> backgroundProperty6 = TemplatedControl.BackgroundProperty;
		DynamicResourceExtension selectModelButtonBackgroundResource = new DynamicResourceExtension("BtnPurple");
		context.ProvideTargetProperty = TemplatedControl.BackgroundProperty;
		IBinding selectModelButtonBackgroundBinding = selectModelButtonBackgroundResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(selectModelButtonNewControl, backgroundProperty6, selectModelButtonBackgroundBinding);
		StyledProperty<IBrush?> foregroundProperty6 = TemplatedControl.ForegroundProperty;
		DynamicResourceExtension selectModelButtonForegroundResource = new DynamicResourceExtension("BadgeText");
		context.ProvideTargetProperty = TemplatedControl.ForegroundProperty;
		IBinding selectModelButtonForegroundBinding = selectModelButtonForegroundResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(selectModelButtonNewControl, foregroundProperty6, selectModelButtonForegroundBinding);
		selectModelButtonNewControl.Padding = new Thickness(12.0, 8.0, 12.0, 8.0);
		selectModelButtonNewControl.CornerRadius = new CornerRadius(8.0, 8.0, 8.0, 8.0);
		context.PopParent();
		((ISupportInitialize)selectModelButtonNewEnd).EndInit();
		Avalonia.Controls.Controls profileRowPanelChildren4 = profileRowPanelNewControl.Children;
		TextBlock selectedModelPathLabelNew;
		TextBlock selectedModelPathLabelNewBase = (selectedModelPathLabelNew = new TextBlock());
		((ISupportInitialize)selectedModelPathLabelNewBase).BeginInit();
		profileRowPanelChildren4.Add(selectedModelPathLabelNewBase);
		TextBlock selectedModelPathLabelNewEnd = (textBlockRef = selectedModelPathLabelNew);
		context.PushParent(textBlockRef);
		TextBlock selectedModelPathLabelNewControl = textBlockRef;
		StyledProperty<string?> textProperty4 = TextBlock.TextProperty;
		ReflectionBindingExtension selectedModelPathLabelTextSource = new ReflectionBindingExtension("SelectedModelPath");
		context.ProvideTargetProperty = TextBlock.TextProperty;
		Binding selectedModelPathLabelTextBinding = selectedModelPathLabelTextSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(selectedModelPathLabelNewControl, textProperty4, selectedModelPathLabelTextBinding);
		StyledProperty<IBrush?> foregroundProperty7 = TextBlock.ForegroundProperty;
		DynamicResourceExtension selectedModelPathLabelForegroundResource = new DynamicResourceExtension("ThemeTextSecondary");
		context.ProvideTargetProperty = TextBlock.ForegroundProperty;
		IBinding selectedModelPathLabelForegroundBinding = selectedModelPathLabelForegroundResource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(selectedModelPathLabelNewControl, foregroundProperty7, selectedModelPathLabelForegroundBinding);
		selectedModelPathLabelNewControl.FontSize = 12.0;
		selectedModelPathLabelNewControl.VerticalAlignment = VerticalAlignment.Center;
		selectedModelPathLabelNewControl.TextTrimming = TextTrimming.CharacterEllipsis;
		context.PopParent();
		((ISupportInitialize)selectedModelPathLabelNewEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)profileRowPanelNewEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)headerPanelNewEnd).EndInit();
		context.PopParent();
		((ISupportInitialize)headerBorderNewEnd).EndInit();
		Avalonia.Controls.Controls rootPanelChildren2 = dockPanelRef.Children;
		ListBox profilesListNew;
		ListBox profilesList = (profilesListNew = new ListBox());
		((ISupportInitialize)profilesList).BeginInit();
		rootPanelChildren2.Add(profilesList);
		ListBox profilesListRef;
		ListBox profilesListEnd = (profilesListRef = profilesListNew);
		context.PushParent(profilesListRef);
		StyledProperty<IEnumerable?> itemsSourceProperty = ItemsControl.ItemsSourceProperty;
		ReflectionBindingExtension profilesSource = new ReflectionBindingExtension("Profiles");
		context.ProvideTargetProperty = ItemsControl.ItemsSourceProperty;
		Binding profilesListItemsSourceBinding = profilesSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		AvaloniaObjectExtensions.Bind(profilesListRef, itemsSourceProperty, profilesListItemsSourceBinding);
		ReflectionBindingExtension selectedProfileSource = new ReflectionBindingExtension("SelectedProfile");
		context.ProvideTargetProperty = SelectingItemsControl.SelectedItemProperty;
		Binding profilesListSelectedItemBinding = selectedProfileSource.ProvideValue(context);
		context.ProvideTargetProperty = null;
		CompiledAvaloniaXaml.XamlDynamicSetters._003C_003EXamlDynamicSetter_1(profilesListRef, profilesListSelectedItemBinding);
		profilesListRef.Background = new ImmutableSolidColorBrush(16777215u);
		profilesListRef.BorderThickness = new Thickness(0.0, 0.0, 0.0, 0.0);
		DataTemplate itemTemplateNew;
		DataTemplate itemTemplate = (itemTemplateNew = new DataTemplate());
		context.PushParent(itemTemplateNew);
		itemTemplateNew.Content = XamlIlRuntimeHelpers.DeferredTransformationFactoryV3<Control>((nint)(delegate*<IServiceProvider, object>)(&XamlClosure_11.Build_1), context);
		context.PopParent();
		profilesListRef.ItemTemplate = itemTemplate;
		context.PopParent();
		((ISupportInitialize)profilesListEnd).EndInit();
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

	private static void _0021XamlIlPopulateTrampoline(ProfilesPage page)
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
