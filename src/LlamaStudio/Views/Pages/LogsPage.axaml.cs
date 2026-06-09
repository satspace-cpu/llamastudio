using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LlamaStudio.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LlamaStudio.Views.Pages;

public partial class LogsPage : UserControl
{
    LogsViewModel? _viewModel;

    public LogsPage()
    {
        InitializeComponent();
    }

    public LogsPage(LogsViewModel viewModel) : this()
    {
        this.DataContext = viewModel;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is LogsViewModel vm)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModelPropertyChanged;
                _viewModel.LogLines.CollectionChanged -= LogLinesCollectionChanged;
            }
            _viewModel = vm;
            _viewModel.PropertyChanged += ViewModelPropertyChanged;
            _viewModel.LogLines.CollectionChanged += LogLinesCollectionChanged;
        }
    }

    void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LogsViewModel.AutoScroll))
        {
            if (_viewModel?.AutoScroll == true)
            {
                Dispatcher.UIThread.Post(() => LogScrollViewer?.ScrollToEnd());
            }
        }
    }

    void LogLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_viewModel?.AutoScroll == true && (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Reset))
        {
            Dispatcher.UIThread.Post(() => LogScrollViewer?.ScrollToEnd());
        }
    }

    async void CopyBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        var text = _viewModel.GetFullText();
        if (string.IsNullOrEmpty(text)) return;
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(text);
        }
        catch
        {
        }
    }
}
