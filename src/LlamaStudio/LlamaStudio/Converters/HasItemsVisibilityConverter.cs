using System;
using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LlamaStudio.Converters;

public class HasItemsVisibilityConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is IEnumerable enumerable)
		{
			{
				IEnumerator enumerator = enumerable.GetEnumerator();
				try
				{
					if (enumerator.MoveNext())
					{
						_ = enumerator.Current;
						return true;
					}
				}
				finally
				{
					IDisposable disposable = enumerator as IDisposable;
					if (disposable != null)
					{
						disposable.Dispose();
					}
				}
			}
		}
		return false;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
