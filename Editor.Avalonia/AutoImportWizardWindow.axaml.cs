using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tools.FontImport;

namespace Editor.Avalonia;

public partial class AutoImportWizardWindow : Window
{
    private AutoImportWizardViewModel? _vm;
    private CancellationTokenSource? _cts;

    public AutoImportWizardWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public void Initialize(AutoImportWizardViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
    }

    private async void OnStart(object? sender, RoutedEventArgs e)
    {
        if (_vm == null) return;

        _cts = new CancellationTokenSource();
        
        var startBtn = this.FindControl<Button>("StartButton");
        var cancelBtn = this.FindControl<Button>("CancelButton");
        
        if (startBtn != null) startBtn.IsEnabled = false;
        if (cancelBtn != null) cancelBtn.Content = "Cancel";

        try
        {
            await _vm.StartImportAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            _vm.Status = "Import cancelled";
        }
        finally
        {
            if (startBtn != null) startBtn.IsEnabled = true;
            if (cancelBtn != null) cancelBtn.Content = "Close";
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
        else
        {
            Close();
        }
    }
}
