using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LlamaStudio.Core.Models;

public class CliFlagEntry : ObservableObject
{
	[ObservableProperty]
	private bool _isSelected;

	[ObservableProperty]
	private string _userValue = string.Empty;

	public bool IsSelected
	{
		get => _isSelected;
		set => SetProperty(ref _isSelected, value);
	}

	public string UserValue
	{
		get => _userValue;
		set => SetProperty(ref _userValue, value);
	}

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public string DefaultValue { get; set; } = string.Empty;

	public bool RequiresValue { get; set; }

	public string Category { get; set; } = string.Empty;

	public string[]? AllowedValues { get; set; }

	public string? AllowedValuesDisplay
	{
		get
		{
			string[]? allowedValues = AllowedValues;
			if (allowedValues == null || allowedValues.Length == 0)
			{
				return null;
			}
			return "[" + string.Join(", ", AllowedValues) + "]";
		}
	}

	public bool HasEnumValues
	{
		get
		{
			if (AllowedValues != null)
			{
				return AllowedValues.Length != 0;
			}
			return false;
		}
	}

	public bool NeedsTextInput
	{
		get
		{
			if (RequiresValue)
			{
				return !HasEnumValues;
			}
			return false;
		}
	}

	public bool IsNumeric
	{
		get
		{
			if (!HasEnumValues && !string.IsNullOrWhiteSpace(DefaultValue))
			{
				int result2;
				if (!double.TryParse(DefaultValue, out var _))
				{
					return int.TryParse(DefaultValue, out result2);
				}
				return true;
			}
			return false;
		}
	}
}
