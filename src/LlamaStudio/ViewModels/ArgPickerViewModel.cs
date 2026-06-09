using CommunityToolkit.Mvvm.ComponentModel;
using LlamaStudio.Core.Models;
using System.Collections.ObjectModel;

namespace LlamaStudio.ViewModels;

/// <summary>ViewModel for the CLI argument picker dialog</summary>
public partial class ArgPickerViewModel : ObservableObject
{
    [ObservableProperty] string _filterText = string.Empty;
    [ObservableProperty] ObservableCollection<CliFlagEntry> _allFlags = new();
    [ObservableProperty] ObservableCollection<CliFlagEntry> _filteredFlags = new();
    [ObservableProperty] int _selectedCount = 0;

    public void LoadFlags(LlamaHelpInfo? helpInfo)
    {
        AllFlags.Clear();
        if (helpInfo == null) return;

        foreach (var flag in helpInfo.Flags.Values.OrderBy(f => f.Name))
        {
            if (flag.Name.StartsWith(".") || flag.Name.Contains("version", StringComparison.OrdinalIgnoreCase))
                continue;

            var entry = new CliFlagEntry
            {
                Name = flag.Name,
                Description = flag.Description ?? string.Empty,
                DefaultValue = flag.DefaultValue ?? string.Empty,
                RequiresValue = flag.TakesValue,
                UserValue = flag.DefaultValue ?? string.Empty,
            };
            AllFlags.Add(entry);
        }
        ApplyFilter();
    }

    partial void OnFilterTextChanged(string value) => ApplyFilter();

    partial void OnAllFlagsChanged(ObservableCollection<CliFlagEntry> value)
    {
        foreach (var entry in value)
        {
            entry.PropertyChanged += Flag_PropertyChanged;
        }
    }

    void Flag_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CliFlagEntry.IsSelected))
        {
            SelectedCount = AllFlags.Count(f => f.IsSelected);
        }
    }

    void ApplyFilter()
    {
        FilteredFlags.Clear();
        var filter = FilterText.ToLowerInvariant();

        foreach (var flag in AllFlags)
        {
            if (string.IsNullOrEmpty(filter) ||
                flag.Name.Contains(filter) ||
                flag.Description.ToLowerInvariant().Contains(filter))
            {
                FilteredFlags.Add(flag);
            }
        }
    }

    public List<string> GetSelectedArgs()
    {
        var args = new List<string>();
        foreach (var entry in AllFlags.Where(e => e.IsSelected))
        {
            if (entry.RequiresValue && !string.IsNullOrWhiteSpace(entry.UserValue))
                args.Add($"{entry.Name} {entry.UserValue.Trim()}");
            else if (!entry.RequiresValue)
                args.Add(entry.Name);
        }
        return args;
    }
}
