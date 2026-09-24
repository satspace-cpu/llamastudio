using System;
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

public class ArgPickerViewModel : ObservableObject
{
	private readonly ILocalizationService? _loc;

	[ObservableProperty]
	private string _filterText = string.Empty;

	[ObservableProperty]
	private ObservableCollection<CliFlagEntry> _allFlags = new ObservableCollection<CliFlagEntry>();

	[ObservableProperty]
	private ObservableCollection<ArgumentCategoryGroup> _groupedFlags = new ObservableCollection<ArgumentCategoryGroup>();

	[ObservableProperty]
	private int _selectedCount;

	public string ErrorTitle => _loc?.T("dialog.error_title") ?? "Error";

	public string Title => _loc?.T("cli_args.title") ?? "CLI Args Picker";

	public string FilterWatermark => _loc?.T("cli_args.filter") ?? "Filter flags...";

	public string AddBtnText => _loc?.T("cli_args.add_btn") ?? "Add";

	public string CancelBtnText => _loc?.T("cli_args.cancel_btn") ?? "Cancel";

	public string SelectedCountText => string.Format(_loc?.T("cli_args.selected_count_fmt") ?? "Selected: {0}", SelectedCount);

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
	public ObservableCollection<CliFlagEntry> AllFlags
	{
		get
		{
			return _allFlags;
		}
		[MemberNotNull("_allFlags")]
		set
		{
			if (!EqualityComparer<ObservableCollection<CliFlagEntry>>.Default.Equals(_allFlags, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AllFlags);
				_allFlags = value;
				OnAllFlagsChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AllFlags);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<ArgumentCategoryGroup> GroupedFlags
	{
		get
		{
			return _groupedFlags;
		}
		[MemberNotNull("_groupedFlags")]
		set
		{
			if (!EqualityComparer<ObservableCollection<ArgumentCategoryGroup>>.Default.Equals(_groupedFlags, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.GroupedFlags);
				_groupedFlags = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.GroupedFlags);
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

	public ArgPickerViewModel(ILocalizationService? loc = null)
	{
		_loc = loc;
		if (_loc != null)
		{
			_loc.OnLanguageChanged += OnLanguageChanged;
		}
	}

	private void OnLanguageChanged(object? sender, string language)
	{
		string[] propertyNames = new string[6] { "ErrorTitle", "Title", "FilterWatermark", "AddBtnText", "CancelBtnText", "SelectedCountText" };
		foreach (string propertyName in propertyNames)
		{
			OnPropertyChanged(propertyName);
		}
	}

	private static bool IsFlagExcluded(CliFlagInfo flag, HashSet<string> excludedFlags)
	{
		HashSet<string> flagVariants = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			flag.Name,
			flag.Name.TrimStart('-')
		};
		if (!string.IsNullOrWhiteSpace(flag.ShortName))
		{
			flagVariants.Add(flag.ShortName);
			flagVariants.Add(flag.ShortName.TrimStart('-'));
		}
		foreach (string variant in flagVariants)
		{
			if (excludedFlags.Contains(variant))
			{
				return true;
			}
		}
		return false;
	}

	public void LoadFlags(LlamaHelpInfo? helpInfo, HashSet<string>? excludedFlags = null, string? existingArgs = null)
	{
		AllFlags.Clear();
		if (helpInfo == null)
		{
			return;
		}
		Dictionary<string, string> existingArgsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (!string.IsNullOrWhiteSpace(existingArgs))
		{
			string[] lines = existingArgs.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			for (int i = 0; i < lines.Length; i++)
			{
				string[] parts = lines[i].Trim().Split(new char[2] { ' ', '\t' }, 2, StringSplitOptions.TrimEntries);
				if (parts.Length != 0)
				{
					existingArgsMap[parts[0]] = ((parts.Length > 1) ? parts[1].Trim('"') : string.Empty);
				}
			}
		}
		foreach (CliFlagInfo flagInfo in helpInfo.Flags.Values.OrderBy((CliFlagInfo f) => f.Name))
		{
			if (flagInfo.Name.StartsWith(".") || flagInfo.Name.Contains("version", StringComparison.OrdinalIgnoreCase) || (excludedFlags != null && IsFlagExcluded(flagInfo, excludedFlags)))
			{
				continue;
			}
			LlamaArgumentRegistry.ArgDefinition definition = LlamaArgumentRegistry.GetDefinition(flagInfo.Name);
			LlamaArgumentRegistry.Category category = definition?.Category ?? LlamaArgumentRegistry.Category.Server;
			string description = definition?.GetLocalizedDescription() ?? flagInfo.Description ?? string.Empty;
			string defaultValue = flagInfo.DefaultValue ?? definition?.DefaultValue ?? string.Empty;
			string key = (flagInfo.Name.StartsWith("--") ? flagInfo.Name.Substring(2) : flagInfo.Name);
			bool isSelected = existingArgsMap.ContainsKey(flagInfo.Name) || existingArgsMap.ContainsKey(key);
			string userValue = string.Empty;
			if (isSelected)
			{
				string argValue2;
				if (existingArgsMap.TryGetValue(flagInfo.Name, out var argValue))
				{
					userValue = argValue;
				}
				else if (existingArgsMap.TryGetValue(key, out argValue2))
				{
					userValue = argValue2;
				}
				else if (flagInfo.TakesValue || (definition != null && definition.TakesValue))
				{
					userValue = defaultValue;
				}
			}
			else if (flagInfo.TakesValue || (definition != null && definition.TakesValue))
			{
				userValue = defaultValue;
			}
			CliFlagEntry entry = new CliFlagEntry
			{
				Name = flagInfo.Name,
				Description = description,
				DefaultValue = defaultValue,
				RequiresValue = (flagInfo.TakesValue || (definition?.TakesValue ?? false)),
				Category = category.ToString(),
				UserValue = userValue,
				IsSelected = isSelected,
				AllowedValues = definition?.AllowedValues
			};
			AllFlags.Add(entry);
		}
		ApplyFilter();
		SelectedCount = AllFlags.Count((CliFlagEntry f) => f.IsSelected);
	}

	private void ApplyFilter()
	{
		GroupedFlags.Clear();
		string filter = FilterText.ToLowerInvariant();
		IEnumerable<CliFlagEntry> source = AllFlags.Where((CliFlagEntry flag) => string.IsNullOrEmpty(filter) || flag.Name.Contains(filter) || flag.Description.ToLowerInvariant().Contains(filter));
		LlamaArgumentRegistry.Category[] categories = new LlamaArgumentRegistry.Category[4]
		{
			LlamaArgumentRegistry.Category.Common,
			LlamaArgumentRegistry.Category.Sampling,
			LlamaArgumentRegistry.Category.Speculative,
			LlamaArgumentRegistry.Category.Server
		};
		for (int i = 0; i < categories.Length; i++)
		{
			LlamaArgumentRegistry.Category cat = categories[i];
			string categoryDisplayName = LlamaArgumentRegistry.GetCategoryDisplayName(cat);
			List<CliFlagEntry> categoryFlags = (from f in source
				where f.Category == cat.ToString()
				orderby f.Name
				select f).ToList();
			if (categoryFlags.Count <= 0)
			{
				continue;
			}
			ArgumentCategoryGroup group = new ArgumentCategoryGroup
			{
				CategoryName = categoryDisplayName
			};
			foreach (CliFlagEntry entry in categoryFlags)
			{
				group.Items.Add(entry);
			}
			GroupedFlags.Add(group);
		}
	}

	private void Flag_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "IsSelected")
		{
			SelectedCount = AllFlags.Count((CliFlagEntry f) => f.IsSelected);
		}
	}

	public (List<string> Args, string? Error) GetSelectedArgs()
	{
		List<string> args = new List<string>();
		foreach (CliFlagEntry entry in AllFlags.Where((CliFlagEntry e) => e.IsSelected))
		{
			if (entry.RequiresValue)
			{
				string flagValue = (string.IsNullOrWhiteSpace(entry.UserValue) ? entry.DefaultValue : entry.UserValue.Trim());
				if (entry.AllowedValues != null && entry.AllowedValues.Length != 0)
				{
					string allowedValuesDisplay = string.Join(", ", entry.AllowedValues);
					if (string.IsNullOrWhiteSpace(flagValue))
					{
						return (Args: args, Error: (_loc != null) ? string.Format(_loc.T("argpicker.requires_value_enum"), entry.Name, allowedValuesDisplay) : global::LlamaStudio.NativeLanguage.Text($"Аргумент {entry.Name} требует значение: [{allowedValuesDisplay}]", $"Argument {entry.Name} requires a value: [{allowedValuesDisplay}]"));
					}
					if (!entry.AllowedValues.Contains<string>(flagValue, StringComparer.OrdinalIgnoreCase))
					{
						return (Args: args, Error: (_loc != null) ? string.Format(_loc.T("argpicker.invalid_value_enum"), entry.Name, flagValue, allowedValuesDisplay) : global::LlamaStudio.NativeLanguage.Text($"Недопустимое значение для {entry.Name}: \"{flagValue}\". Допустимо: [{allowedValuesDisplay}]", $"Invalid value for {entry.Name}: \"{flagValue}\". Allowed: [{allowedValuesDisplay}]"));
					}
				}
				else if (entry.IsNumeric)
				{
					if (string.IsNullOrWhiteSpace(flagValue))
					{
						return (Args: args, Error: (_loc != null) ? string.Format(_loc.T("argpicker.requires_numeric"), entry.Name) : global::LlamaStudio.NativeLanguage.Text("Аргумент " + entry.Name + " требует числовое значение", "Argument " + entry.Name + " requires a numeric value"));
					}
					if (!double.TryParse(flagValue, out var _))
					{
						return (Args: args, Error: (_loc != null) ? string.Format(_loc.T("argpicker.invalid_numeric"), entry.Name, flagValue) : global::LlamaStudio.NativeLanguage.Text($"Недопустимое значение для {entry.Name}: \"{flagValue}\". Требуется число.", $"Invalid value for {entry.Name}: \"{flagValue}\". A number is required."));
					}
				}
				else if (string.IsNullOrWhiteSpace(flagValue))
				{
					return (Args: args, Error: (_loc != null) ? string.Format(_loc.T("argpicker.requires_value"), entry.Name) : global::LlamaStudio.NativeLanguage.Text("Аргумент " + entry.Name + " требует значение", "Argument " + entry.Name + " requires a value"));
				}
				if (!string.IsNullOrWhiteSpace(flagValue))
				{
					args.Add(entry.Name + " " + flagValue);
				}
			}
			else
			{
				args.Add(entry.Name);
			}
		}
		return (Args: args, Error: null);
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnFilterTextChanged(string value)
	{
		ApplyFilter();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnAllFlagsChanged(ObservableCollection<CliFlagEntry> value)
	{
		foreach (CliFlagEntry entry in value)
		{
			entry.PropertyChanged += Flag_PropertyChanged;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedCountChanged(int value)
	{
		OnPropertyChanged("SelectedCountText");
	}
}
