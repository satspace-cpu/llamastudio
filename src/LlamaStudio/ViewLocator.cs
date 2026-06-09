  using Avalonia.Controls;
using Avalonia.Controls.Templates;
using LlamaStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace LlamaStudio;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var fullName = param.GetType().FullName;
        if (fullName == null)
            return new TextBlock { Text = "Not Found: null" };

        // Try multiple naming conventions:
        // 1. ViewModel → Page (e.g., DashboardViewModel → DashboardPage)
        // 2. ViewModel → View (legacy)
        var candidates = new[]
        {
            fullName.Replace(".ViewModels.", ".Views.Pages.").Replace("ViewModel", "Page"),
            fullName.Replace("ViewModel", "View"),
        };

        Control? control = null;
        foreach (var name in candidates)
        {
            var type = Type.GetType(name);
            if (type == null) continue;

            var ctor = type.GetConstructor(new[] { param.GetType() });
            if (ctor != null)
                return (Control)ctor.Invoke(new[] { param })!;

            control = (Control)Activator.CreateInstance(type)!;
            control.DataContext = param;
            return control;
        }

        return new TextBlock { Text = "Not Found: " + param.GetType().FullName };
    }

    public bool Match(object? data)
    {
        if (data == null) return false;
        // Only match ViewModel types, not Controls or primitives
        return data.GetType().FullName?.EndsWith("ViewModel") ?? false;
    }
}
