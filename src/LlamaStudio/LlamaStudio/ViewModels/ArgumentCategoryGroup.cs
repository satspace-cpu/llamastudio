using System.Collections.ObjectModel;
using LlamaStudio.Core.Models;

namespace LlamaStudio.ViewModels;

public class ArgumentCategoryGroup
{
	public string CategoryName { get; set; } = string.Empty;

	public ObservableCollection<CliFlagEntry> Items { get; } = new ObservableCollection<CliFlagEntry>();
}
