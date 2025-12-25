using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Threading.Tasks;

namespace Editor.Consolonia;

/// <summary>
/// Helper for creating inline dialogs in Consolonia (no nested Window objects)
/// </summary>
public static class InlineDialogHelper
{
    /// <summary>
    /// Show a text input dialog as overlay
    /// </summary>
    public static Task<string?> ShowTextInputAsync(
        Window owner,
        string title,
        string prompt,
        string? defaultValue = null,
        string? watermark = null)
    {
        var tcs = new TaskCompletionSource<string?>();

        // Create overlay
        var overlay = new Panel
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        // Create dialog content
        var dialog = new Border
        {
            Background = Brushes.Black,
            BorderBrush = Brushes.Cyan,
            BorderThickness = new Thickness(2),
            Padding = new Thickness(2),
            Width = 60,
            Height = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var stack = new StackPanel { Spacing = 1 };

        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.Cyan
        });

        stack.Children.Add(new TextBlock { Text = prompt });

        var input = new TextBox
        {
            Text = defaultValue,
            Watermark = watermark,
            Margin = new Thickness(0, 1)
        };
        stack.Children.Add(input);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 1,
            Margin = new Thickness(0, 1, 0, 0)
        };

        var okBtn = new Button { Content = "OK", Width = 10 };
        var cancelBtn = new Button { Content = "Cancel", Width = 10 };
        buttons.Children.Add(okBtn);
        buttons.Children.Add(cancelBtn);
        stack.Children.Add(buttons);

        dialog.Child = stack;
        overlay.Children.Add(dialog);

        // Event handlers
        void Close(string? result)
        {
            if (owner.Content is Panel mainPanel)
            {
                mainPanel.Children.Remove(overlay);
            }
            tcs.TrySetResult(result);
        }

        okBtn.Click += (s, e) => Close(input.Text?.Trim());
        cancelBtn.Click += (s, e) => Close(null);

        input.KeyDown += (s, e) =>
        {
            if (e.Key == global::Avalonia.Input.Key.Enter)
            {
                Close(input.Text?.Trim());
                e.Handled = true;
            }
            else if (e.Key == global::Avalonia.Input.Key.Escape)
            {
                Close(null);
                e.Handled = true;
            }
        };

        // Add to owner
        if (owner.Content is Panel panel)
        {
            panel.Children.Add(overlay);
            input.Focus();
        }
        else
        {
            tcs.TrySetResult(null);
        }

        return tcs.Task;
    }
}
