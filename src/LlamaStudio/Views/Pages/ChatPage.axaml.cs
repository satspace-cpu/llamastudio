   using System.Collections.Specialized;
  using LlamaStudio.Services;
      using Avalonia;
      using Avalonia.Controls;
      using Avalonia.Input;
       using Avalonia.Interactivity;
      using Avalonia.Markup.Xaml;
      using Avalonia.Platform.Storage;
      using Avalonia.Threading;
      using Avalonia.Media.Imaging;
      using LlamaStudio.Core.Models;
      using LlamaStudio.ViewModels;
      using System.IO;
      using System.Diagnostics;
      using System.Linq;

namespace LlamaStudio.Views.Pages;

public partial class ChatPage : UserControl
    {
        ChatViewModel? _viewModel;
        INotifyCollectionChanged? _messagesCollection;
        bool _autoScroll = true;
        ChatSession? _draggedSession;
        StackPanel? _dragSourcePanel;
        bool _isDragging = false;

    public ChatPage()
        {
            InitializeComponent();
            Loaded += ChatPage_Loaded;
        }

    public async Task CopyCodeToClipboard(string code)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(code);
        }
        catch { }
    }

    async void CopyCodeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string code)
            await CopyCodeToClipboard(code);
    }

        void ChatPage_Loaded(object? sender, System.EventArgs e)
        {
            InputTextBox?.AddHandler(InputElement.KeyDownEvent, InputTextBoxPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

        void InputTextBoxPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                e.Handled = true;
                _viewModel?.SendMessageCommand?.Execute(null);
                return;
            }

            if (e.Key == Key.V && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                if (ClipboardImageHelper.TryGetImageFromClipboard(out byte[]? imageBytes))
                {
                    if (imageBytes != null && imageBytes.Length > 0 && _viewModel != null)
                    {
                        e.Handled = true;
                        var base64 = Convert.ToBase64String(imageBytes);
                        _viewModel.AttachedImageBase64.Add(base64);
                        _viewModel.NotifyImagesChanged();
                    }
                }
            }
        }

    public ChatPage(ChatViewModel viewModel) : this()
    {
        this.DataContext = viewModel;
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel != null)
        {
            UnsubscribeMessages();
            ((System.ComponentModel.INotifyPropertyChanged)_viewModel).PropertyChanged -= ViewModelPropertyChanged;
        }

        if (DataContext is ChatViewModel vm)
        {
            _viewModel = vm;
            SubscribeMessages();
            ((System.ComponentModel.INotifyPropertyChanged)_viewModel).PropertyChanged += ViewModelPropertyChanged;
        }
    }

    void SubscribeMessages()
    {
        UnsubscribeMessages();
        if (_viewModel?.Messages is INotifyCollectionChanged coll)
        {
            _messagesCollection = coll;
            coll.CollectionChanged += MessagesCollectionChanged;
        }
    }

    void UnsubscribeMessages()
    {
        if (_messagesCollection != null)
        {
            _messagesCollection.CollectionChanged -= MessagesCollectionChanged;
            _messagesCollection = null;
        }
    }

    void ViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatViewModel.Messages))
        {
            SubscribeMessages(); // Re-subscribe to new collection instance
            if (_autoScroll)
                Dispatcher.UIThread.Post(() => MessagesScroll?.ScrollToEnd());
        }
        else if (e.PropertyName == nameof(ChatViewModel.IsGenerating))
        {
            if (_autoScroll)
                Dispatcher.UIThread.Post(() => MessagesScroll?.ScrollToEnd());
        }
    }

    void MessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_autoScroll)
        {
            Dispatcher.UIThread.Post(() =>
            {
                MessagesScroll?.ScrollToEnd();
            });
        }
    }

    void MessagesScrollPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _autoScroll = e.Delta.Y < 0;
    }

    void InputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                e.Handled = true;
                return;
            }
            e.Handled = true;
            _viewModel?.SendMessageCommand?.Execute(null);
        }
    }

    void ContextMenuCut_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (InputTextBox != null && _viewModel != null)
        {
            var text = InputTextBox.SelectedText;
            if (!string.IsNullOrEmpty(text))
            {
                var start = InputTextBox.SelectionStart;
                var current = _viewModel.InputText;
                _viewModel.InputText = current.Remove(start, text.Length);
                InputTextBox.CaretIndex = start;
                _ = TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text);
            }
        }
    }

    void ContextMenuCopy_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (InputTextBox != null && !string.IsNullOrEmpty(InputTextBox.SelectedText))
            _ = TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(InputTextBox.SelectedText);
    }

    async void ContextMenuPaste_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (InputTextBox != null && _viewModel != null)
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                var text = await clipboard.GetTextAsync();
                if (!string.IsNullOrEmpty(text))
                {
                    var caret = InputTextBox.CaretIndex;
                    var current = _viewModel.InputText;
                    _viewModel.InputText = current.Insert(caret, text);
                    InputTextBox.CaretIndex = caret + text.Length;
                }
            }
        }
    }

    void ContextMenuSelectAll_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (InputTextBox != null)
            InputTextBox.SelectAll();
    }

    void SessionItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is StackPanel panel && panel.DataContext is ChatSession session)
        {
            _viewModel?.StartRenameSessionCommand?.Execute(session);
        }
    }

    void SessionItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is StackPanel panel && panel.DataContext is ChatSession session)
        {
            _draggedSession = session;
            _dragSourcePanel = panel;
            _isDragging = false;

            var cursor = e.GetCurrentPoint(panel);
            if (cursor.Properties.IsLeftButtonPressed)
            {
                panel.AddHandler(InputElement.PointerMovedEvent, SessionItemPointerMoved, Avalonia.Interactivity.RoutingStrategies.Tunnel);
                panel.AddHandler(InputElement.PointerReleasedEvent, SessionItemPointerReleased, Avalonia.Interactivity.RoutingStrategies.Bubble);
            }
        }
    }

    void SessionItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedSession == null)
            return;

        var point = e.GetPosition(null);
        if (Math.Abs(point.X) + Math.Abs(point.Y) > 5)
        {
            _isDragging = true;
            if (_dragSourcePanel != null)
            {
                _dragSourcePanel.Opacity = 0.4;
            }
        }
    }

    void SessionItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragSourcePanel != null)
        {
            _dragSourcePanel.RemoveHandler(InputElement.PointerMovedEvent, SessionItemPointerMoved);
            _dragSourcePanel.RemoveHandler(InputElement.PointerReleasedEvent, SessionItemPointerReleased);
        }

        if (!_isDragging || _draggedSession == null || _viewModel == null)
        {
            if (_dragSourcePanel != null)
                _dragSourcePanel.Opacity = 1.0;
            _draggedSession = null;
            _dragSourcePanel = null;
            _isDragging = false;
            return;
        }

        if (_dragSourcePanel != null)
            _dragSourcePanel.Opacity = 1.0;

        var targetSession = GetSessionUnderPointer(e);
        if (targetSession == null)
        {
            _draggedSession = null;
            _dragSourcePanel = null;
            _isDragging = false;
            return;
        }

        var sessions = _viewModel.Sessions.ToList();
        var dragIndex = sessions.IndexOf(_draggedSession);
        var targetIndex = sessions.IndexOf(targetSession);

        if (dragIndex >= 0 && targetIndex >= 0 && dragIndex != targetIndex)
        {
            sessions.RemoveAt(dragIndex);
            sessions.Insert(targetIndex, _draggedSession);

            foreach (var s in _viewModel.Sessions)
                _viewModel.Sessions.Remove(s);
            foreach (var s in sessions)
                _viewModel.Sessions.Add(s);

            _viewModel.ReorderSessionsCommand?.Execute(null);
        }

        _draggedSession = null;
        _dragSourcePanel = null;
        _isDragging = false;
    }

    ChatSession? GetSessionUnderPointer(PointerReleasedEventArgs e)
    {
        var root = this;
        var point = e.GetPosition(root);
        var target = root.InputHitTest(point);

        while (target != null)
        {
            if (target is StackPanel panel && panel.DataContext is ChatSession session)
                return session;
            if (target is ListBox)
                break;
            target = GetVisualParent(target) as IInputElement;
        }

        return null;
    }

    static object? GetVisualParent(object visual)
    {
        var parentProp = visual.GetType().GetProperty("Parent");
        return parentProp?.GetValue(visual);
    }

    void RenameTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            _viewModel?.CommitRenameSessionCommand?.Execute(null);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            _viewModel?.CancelRenameSessionCommand?.Execute(null);
        }
    }

    async void AttachBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await PickFilesAsync(new[] { "*.*", "*.txt", "*.md", "*.csv", "*.json", "*.xml", "*.log", "*.cs", "*.py", "*.js", "*.ts" });
    }

    async void ImageBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await PickImagesAsync(new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp", "*.svg" });
    }

    async Task PickImagesAsync(string[] patterns)
    {
        try
        {
            var top = Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var window = top?.MainWindow;
            if (window == null)
                return;

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выбрать изображения",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Images") { Patterns = patterns },
                }
            });

            if (files.Count > 0 && _viewModel != null)
            {
                foreach (var file in files)
                {
                    var path = file.Path.LocalPath;
                    try
                    {
                        var bytes = File.ReadAllBytes(path);
                        var base64 = Convert.ToBase64String(bytes);
                        _viewModel.AttachedImageBase64.Add(base64);
                    }
                    catch { }
                }
                _viewModel.NotifyImagesChanged();
            }
        }
        catch { }
    }

    async Task PickFilesAsync(string[] patterns)
    {
        try
        {
            var top = Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var window = top?.MainWindow;
            if (window == null) return;

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выбрать файлы",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Files") { Patterns = patterns },
                }
            });

            if (files.Count > 0 && _viewModel != null)
            {
                var current = _viewModel.InputText;
                var paths = string.Join("\n", files.Select(f => f.Path.LocalPath));
                _viewModel.InputText = string.IsNullOrWhiteSpace(current)
                    ? paths
                    : current + "\n" + paths;
            }
        }
        catch { }
    }

    void ToolHeaderTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Avalonia.Controls.Panel panel && panel.DataContext is ChatMessage msg)
        {
            msg.IsToolCollapsed = !msg.IsToolCollapsed;
        }
    }

 }
