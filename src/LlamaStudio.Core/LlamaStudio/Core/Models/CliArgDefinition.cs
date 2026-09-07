using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LlamaStudio.Core.Models;

public class CliArgDefinition : ObservableObject
{
	[ObservableProperty]
	private bool _isSelected;

	[ObservableProperty]
	private string _userValue = "";

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

	private static string _currentLanguage = "ru";

	public string PrimaryFlag { get; set; } = "";

	public List<string> Aliases { get; set; } = new List<string>();

	public string DescriptionEn { get; set; } = "";

	public string DescriptionRu { get; set; } = "";

	public string DefaultValue { get; set; } = "";

	public List<string> AllowedValues { get; set; } = new List<string>();

	public string Category { get; set; } = "other";

	public ArgType? InferredArgType { get; set; }

	public string Description
	{
		get
		{
			if (!(_currentLanguage == "en"))
			{
				if (string.IsNullOrEmpty(DescriptionRu))
				{
					return DescriptionEn;
				}
				return DescriptionRu;
			}
			if (!string.IsNullOrEmpty(DescriptionEn))
			{
				return DescriptionEn;
			}
			return DescriptionRu;
		}
	}

	public string AllowedValuesText
	{
		get
		{
			if (AllowedValues == null || AllowedValues.Count == 0)
			{
				return "";
			}
			return "[" + string.Join(", ", AllowedValues) + "]";
		}
	}

	public bool HasAllowedValues
	{
		get
		{
			if (AllowedValues != null)
			{
				return AllowedValues.Count > 0;
			}
			return false;
		}
	}

	public static void SetCurrentLanguage(string lang)
	{
		_currentLanguage = lang ?? "ru";
	}

	public void ResetValue()
	{
		UserValue = DefaultValue;
	}
}
