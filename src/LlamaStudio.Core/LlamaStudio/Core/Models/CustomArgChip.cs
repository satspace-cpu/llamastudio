using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LlamaStudio.Core.Models;

public class CustomArgChip : ObservableObject
{
	private readonly Action<string> _removeAction;

	public string Argument { get; }

	public CustomArgChip(string argument, Action<string> removeAction)
	{
		Argument = argument;
		_removeAction = removeAction;
	}

	[RelayCommand]
	private void Remove()
	{
		_removeAction(Argument);
	}
}
