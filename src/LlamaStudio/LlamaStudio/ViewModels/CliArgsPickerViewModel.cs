using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.ViewModels;

public class CliArgsPickerViewModel : ObservableObject
{
	[ObservableProperty]
	private string _filterText = "";

	[ObservableProperty]
	private ObservableCollection<CliArgCategoryGroup> _groupedArguments = new ObservableCollection<CliArgCategoryGroup>();

	[ObservableProperty]
	private int _selectedCount;

	[ObservableProperty]
	private bool _hasArguments = true;

	private readonly List<CliArgDefinition> _allItems = new List<CliArgDefinition>();

	private readonly ILocalizationService _loc;

	private string _language = "ru";

	public string SelectedCountText => string.Format(SelectedCountFmt, SelectedCount);

	public string SelectedCountFmt { get; private set; } = "Selected: {0}";

	public string Title { get; private set; } = "CLI Args Picker";

	public string FilterWatermark { get; private set; } = "Filter flags...";

	public string AddBtnText { get; private set; } = "Add";

	public string CancelBtnText { get; private set; } = "Cancel";

	public string NoArgsText { get; private set; } = "Failed to load arguments. Check llama-server.exe path";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string FilterText
	{
		get
		{
			return _filterText;
		}
		[MemberNotNull("_filterText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_filterText, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.FilterText);
				_filterText = value;
				OnFilterTextChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.FilterText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<CliArgCategoryGroup> GroupedArguments
	{
		get
		{
			return _groupedArguments;
		}
		[MemberNotNull("_groupedArguments")]
		set
		{
			if (!EqualityComparer<ObservableCollection<CliArgCategoryGroup>>.Default.Equals(_groupedArguments, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.GroupedArguments);
				_groupedArguments = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.GroupedArguments);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int SelectedCount
	{
		get
		{
			return _selectedCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_selectedCount, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedCount);
				_selectedCount = value;
				OnSelectedCountChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedCount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HasArguments
	{
		get
		{
			return _hasArguments;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hasArguments, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HasArguments);
				_hasArguments = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HasArguments);
			}
		}
	}

	public CliArgsPickerViewModel(ILocalizationService loc)
	{
		_loc = loc;
		_loc.OnLanguageChanged += OnLanguageChanged;
	}

	private void OnLanguageChanged(object? sender, string language)
	{
		_language = language;
		SetLocalizedStrings(language);
		ApplyFilter();
	}

	public void LoadDefinitions(List<CliArgDefinition> definitions, string language = "ru")
	{
		_language = language;
		SetLocalizedStrings(language);
		foreach (CliArgDefinition def in definitions)
		{
			if (!_allItems.Any((CliArgDefinition d) => d.PrimaryFlag == def.PrimaryFlag))
			{
				_allItems.Add(def);
			}
		}
		foreach (CliArgDefinition allItem in _allItems)
		{
			allItem.PropertyChanged += Item_PropertyChanged;
		}
		HasArguments = _allItems.Count > 0;
		ApplyFilter();
	}

	private void SetLocalizedStrings(string lang)
	{
		Title = _loc.T("cli_args.title");
		FilterWatermark = _loc.T("cli_args.filter");
		AddBtnText = _loc.T("cli_args.add_btn");
		CancelBtnText = _loc.T("cli_args.cancel_btn");
		SelectedCountFmt = _loc.T("cli_args.selected_count_fmt");
		NoArgsText = _loc.T("cli_args.no_args");
	}

	private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "IsSelected")
		{
			SelectedCount = _allItems.Count((CliArgDefinition d) => d.IsSelected);
		}
	}

	private void ApplyFilter()
	{
		string filter = FilterText.ToLowerInvariant().Trim();
		GroupedArguments.Clear();
		IEnumerable<CliArgDefinition> source;
		if (!string.IsNullOrEmpty(filter))
		{
			source = _allItems.Where((CliArgDefinition d) => d.PrimaryFlag.Contains(filter) || d.DescriptionEn.ToLowerInvariant().Contains(filter) || d.DescriptionRu.ToLowerInvariant().Contains(filter));
		}
		else
		{
			IEnumerable<CliArgDefinition> allItems = _allItems;
			source = allItems;
		}
		foreach (IGrouping<string, CliArgDefinition> categoryGroup in from d in source
			group d by d.Category into g
			orderby g.Key
			select g)
		{
			string categoryDisplayName = GetCategoryDisplayName(categoryGroup.Key);
			GroupedArguments.Add(new CliArgCategoryGroup
			{
				CategoryName = categoryDisplayName,
				Items = categoryGroup.ToList()
			});
		}
	}

	private string GetCategoryDisplayName(string key)
	{
		string categoryText = _loc.T("cli_args.cat." + key);
		if (!categoryText.StartsWith("["))
		{
			return categoryText;
		}
		return key switch
		{
			"common" => "Common", 
			"sampling" => "Sampling", 
			"speculative" => "Speculative Decoding", 
			"server" => "Server", 
			"other" => "Other", 
			_ => key, 
		};
	}

	public List<string> ConsumeSelectedArgs()
	{
		List<string> selectedArgs = new List<string>();
		foreach (CliArgDefinition argDef in _allItems.Where((CliArgDefinition d) => d.IsSelected))
		{
			string argValue = null;
			if (!string.IsNullOrWhiteSpace(argDef.UserValue) && argDef.UserValue != argDef.DefaultValue)
			{
				argValue = argDef.UserValue.Trim();
			}
			if (argDef.InferredArgType == ArgType.Flag && !string.IsNullOrEmpty(argValue))
			{
				argDef.IsSelected = false;
				continue;
			}
			if (argDef.InferredArgType != ArgType.Flag && string.IsNullOrEmpty(argValue))
			{
				string defaultValue = argDef.DefaultValue.Trim();
				if (!string.IsNullOrEmpty(defaultValue) && !IsDefaultLikeFlag(defaultValue))
				{
					argValue = defaultValue;
				}
				else if (argDef.InferredArgType.HasValue && argDef.InferredArgType != ArgType.String)
				{
					argDef.IsSelected = false;
					continue;
				}
			}
			if (!string.IsNullOrEmpty(argValue) && !ValidateValue(argDef.InferredArgType, argValue))
			{
				argDef.IsSelected = false;
				continue;
			}
			string argString = (string.IsNullOrEmpty(argValue) ? argDef.PrimaryFlag : (argDef.PrimaryFlag + " " + argValue));
			selectedArgs.Add(argString);
			argDef.IsSelected = false;
		}
		return selectedArgs;
	}

	private static bool ValidateValue(ArgType? argType, string value)
	{
		switch (argType)
		{
		case ArgType.Int:
		{
			int parsedInt;
			return int.TryParse(value, out parsedInt);
		}
		case ArgType.Float:
		{
			float parsedFloat;
			return float.TryParse(value, out parsedFloat);
		}
		case ArgType.BoolOnOff:
		{
			string lowerText = value.ToLowerInvariant();
			if (lowerText == "on" || lowerText == "off")
			{
				return true;
			}
			return false;
		}
		case ArgType.Flag:
			return true;
		default:
			return true;
		}
	}

	private static bool IsDefaultLikeFlag(string value)
	{
		switch (value.ToLowerInvariant())
		{
		case "enabled":
		case "disabled":
		case "on":
		case "off":
		case "true":
		case "false":
			return true;
		default:
			return false;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnFilterTextChanged(string value)
	{
		ApplyFilter();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedCountChanged(int value)
	{
		OnPropertyChanged("SelectedCountText");
	}
}
