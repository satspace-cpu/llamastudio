using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace LlamaStudio;

public class ViewLocator : IDataTemplate, ITemplate<object?, Control?>
{
	public Control? Build(object? param)
	{
		if (param == null)
		{
			return null;
		}
		string fullName = param.GetType().FullName;
		if (fullName == null)
		{
			return new TextBlock
			{
				Text = "Not Found: null"
			};
		}
		string[] array = new string[2]
		{
			fullName.Replace(".ViewModels.", ".Views.Pages.").Replace("ViewModel", "Page"),
			fullName.Replace("ViewModel", "View")
		};
		for (int i = 0; i < array.Length; i++)
		{
			Type type = Type.GetType(array[i]);
			if (!(type == null))
			{
				ConstructorInfo constructor = type.GetConstructor(new Type[1] { param.GetType() });
				if (constructor != null)
				{
					return (Control)constructor.Invoke(new object[1] { param });
				}
				Control obj = (Control)Activator.CreateInstance(type);
				obj.DataContext = param;
				return obj;
			}
		}
		return new TextBlock
		{
			Text = "Not Found: " + param.GetType().FullName
		};
	}

	public bool Match(object? data)
	{
		if (data == null)
		{
			return false;
		}
		return data.GetType().FullName?.EndsWith("ViewModel") ?? false;
	}
}
