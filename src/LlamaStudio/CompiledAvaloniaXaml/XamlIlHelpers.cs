using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;

namespace CompiledAvaloniaXaml;

[CompilerGenerated]
internal class XamlIlHelpers
{
	private static IPropertyInfo Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Field;

	private static IPropertyInfo Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverter_0021Field;

	private static IPropertyInfo Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EConverter_0021Field;

	private static IPropertyInfo Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EBindings_0021Field;

	private static IPropertyInfo Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverterParameter_0021Field;

	private static object Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Getter(object P_0)
	{
		return ((Setter)P_0).Value;
	}

	private static void Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Setter(object P_0, object P_1)
	{
		((Setter)P_0).Value = P_1;
	}

	public static IPropertyInfo Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Property()
	{
		if (Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Field != null)
		{
			return Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Field;
		}
		Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Field = new ClrPropertyInfo("Value", (object P_0) => ((Setter)P_0).Value, delegate(object P_0, object P_1)
		{
			((Setter)P_0).Value = P_1;
		}, typeof(object));
		return Avalonia_002EStyling_002ESetter_002CAvalonia_002EBase_002EValue_0021Field;
	}

	private static object Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverter_0021Getter(object P_0)
	{
		return ((ReflectionBindingExtension)P_0).Converter;
	}

	private static void Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverter_0021Setter(object P_0, object P_1)
	{
		((ReflectionBindingExtension)P_0).Converter = (IValueConverter)P_1;
	}

	public static IPropertyInfo Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverter_0021Property()
	{
		if (Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverter_0021Field != null)
		{
			return Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverter_0021Field;
		}
		Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverter_0021Field = new ClrPropertyInfo("Converter", (object P_0) => ((ReflectionBindingExtension)P_0).Converter, delegate(object P_0, object P_1)
		{
			((ReflectionBindingExtension)P_0).Converter = (IValueConverter)P_1;
		}, typeof(IValueConverter));
		return Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverter_0021Field;
	}

	private static object Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EConverter_0021Getter(object P_0)
	{
		return ((MultiBinding)P_0).Converter;
	}

	private static void Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EConverter_0021Setter(object P_0, object P_1)
	{
		((MultiBinding)P_0).Converter = (IMultiValueConverter)P_1;
	}

	public static IPropertyInfo Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EConverter_0021Property()
	{
		if (Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EConverter_0021Field != null)
		{
			return Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EConverter_0021Field;
		}
		Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EConverter_0021Field = new ClrPropertyInfo("Converter", (object P_0) => ((MultiBinding)P_0).Converter, delegate(object P_0, object P_1)
		{
			((MultiBinding)P_0).Converter = (IMultiValueConverter)P_1;
		}, typeof(IMultiValueConverter));
		return Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EConverter_0021Field;
	}

	private static object Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EBindings_0021Getter(object P_0)
	{
		return ((MultiBinding)P_0).Bindings;
	}

	private static void Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EBindings_0021Setter(object P_0, object P_1)
	{
		((MultiBinding)P_0).Bindings = (IList<IBinding>)P_1;
	}

	public static IPropertyInfo Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EBindings_0021Property()
	{
		if (Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EBindings_0021Field != null)
		{
			return Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EBindings_0021Field;
		}
		Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EBindings_0021Field = new ClrPropertyInfo("Bindings", (object P_0) => ((MultiBinding)P_0).Bindings, delegate(object P_0, object P_1)
		{
			((MultiBinding)P_0).Bindings = (IList<IBinding>)P_1;
		}, typeof(IList<IBinding>));
		return Avalonia_002EData_002EMultiBinding_002CAvalonia_002EMarkup_002EBindings_0021Field;
	}

	private static object Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverterParameter_0021Getter(object P_0)
	{
		return ((ReflectionBindingExtension)P_0).ConverterParameter;
	}

	private static void Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverterParameter_0021Setter(object P_0, object P_1)
	{
		((ReflectionBindingExtension)P_0).ConverterParameter = P_1;
	}

	public static IPropertyInfo Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverterParameter_0021Property()
	{
		if (Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverterParameter_0021Field != null)
		{
			return Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverterParameter_0021Field;
		}
		Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverterParameter_0021Field = new ClrPropertyInfo("ConverterParameter", (object P_0) => ((ReflectionBindingExtension)P_0).ConverterParameter, delegate(object P_0, object P_1)
		{
			((ReflectionBindingExtension)P_0).ConverterParameter = P_1;
		}, typeof(object));
		return Avalonia_002EMarkup_002EXaml_002EMarkupExtensions_002EReflectionBindingExtension_002CAvalonia_002EMarkup_002EXaml_002EConverterParameter_0021Field;
	}
}
