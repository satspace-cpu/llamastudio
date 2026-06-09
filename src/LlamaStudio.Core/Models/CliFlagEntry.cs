using CommunityToolkit.Mvvm.ComponentModel;

namespace LlamaStudio.Core.Models;

/// <summary>Single CLI flag entry for the argument picker dialog</summary>
public partial class CliFlagEntry : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public bool RequiresValue { get; set; } = false;

    [ObservableProperty] bool _isSelected;
    [ObservableProperty] string _userValue = string.Empty;
}
