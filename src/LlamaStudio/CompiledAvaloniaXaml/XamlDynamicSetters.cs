using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace CompiledAvaloniaXaml;

[CompilerGenerated]
internal class XamlDynamicSetters
{
	public static void _003C_003EXamlDynamicSetter_1(SelectingItemsControl P_0, Binding P_1)
	{
		if (P_1 != null)
		{
			IBinding binding = P_1;
			AvaloniaObjectExtensions.Bind(P_0, SelectingItemsControl.SelectedItemProperty, binding);
		}
		else
		{
			P_0.SelectedItem = P_1;
		}
	}

	public static void _003C_003EXamlDynamicSetter_2(ContentControl P_0, Binding P_1)
	{
		if (P_1 != null)
		{
			IBinding binding = P_1;
			AvaloniaObjectExtensions.Bind(P_0, ContentControl.ContentProperty, binding);
		}
		else
		{
			P_0.Content = P_1;
		}
	}

	public static void _003C_003EXamlDynamicSetter_3(Control P_0, Binding P_1)
	{
		if (P_1 != null)
		{
			IBinding binding = P_1;
			AvaloniaObjectExtensions.Bind(P_0, Control.TagProperty, binding);
		}
		else
		{
			P_0.Tag = P_1;
		}
	}

	public static void _003C_003EXamlDynamicSetter_4(Control P_0, Binding P_1)
	{
		if (P_1 != null)
		{
			IBinding binding = P_1;
			AvaloniaObjectExtensions.Bind(P_0, ToolTip.TipProperty, binding);
		}
		else
		{
			ToolTip.SetTip(P_0, P_1);
		}
	}

	public static void _003C_003EXamlDynamicSetter_5(Button P_0, Binding P_1)
	{
		if (P_1 != null)
		{
			IBinding binding = P_1;
			AvaloniaObjectExtensions.Bind(P_0, Button.CommandParameterProperty, binding);
		}
		else
		{
			P_0.CommandParameter = P_1;
		}
	}

	public static void _003C_003EXamlDynamicSetter_6(ItemsControl P_0, object P_1)
	{
		if (P_1 is UnsetValueType)
		{
			P_0.SetValue(ItemsControl.ItemTemplateProperty, AvaloniaProperty.UnsetValue);
			return;
		}
		if (P_1 is IBinding)
		{
			IBinding binding = (IBinding)P_1;
			AvaloniaObjectExtensions.Bind(P_0, ItemsControl.ItemTemplateProperty, binding);
			return;
		}
		if (P_1 is IDataTemplate)
		{
			P_0.ItemTemplate = (IDataTemplate?)P_1;
			return;
		}
		if (P_1 == null)
		{
			P_0.ItemTemplate = (IDataTemplate?)P_1;
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_7(HeaderedSelectingItemsControl P_0, Binding P_1)
	{
		if (P_1 != null)
		{
			IBinding binding = P_1;
			AvaloniaObjectExtensions.Bind(P_0, HeaderedSelectingItemsControl.HeaderProperty, binding);
		}
		else
		{
			P_0.Header = P_1;
		}
	}

	public static void _003C_003EXamlDynamicSetter_8(HeaderedContentControl P_0, Binding P_1)
	{
		if (P_1 != null)
		{
			IBinding binding = P_1;
			AvaloniaObjectExtensions.Bind(P_0, HeaderedContentControl.HeaderProperty, binding);
		}
		else
		{
			P_0.Header = P_1;
		}
	}
}
