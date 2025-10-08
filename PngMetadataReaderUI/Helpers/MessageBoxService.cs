using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PngMetadataReaderUI.Models;
using System.Threading.Tasks;

namespace PngMetadataReaderUI.Helpers;

internal static class MessageBoxService
{
    public static async Task ShowAsync(Window owner, DialogRequest request)
    {
        var messageBlock = new TextBlock
        {
            Text = request.Message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 420
        };

        if (request.Type == DialogType.Error)
        {
            messageBlock.Foreground = Brushes.Red;
        }

        var okButton = new Button
        {
            Content = "OK",
            Width = 96,
            HorizontalAlignment = HorizontalAlignment.Right,
            IsDefault = true
        };

        var dialog = new Window
        {
            Title = request.Title,
            CanResize = false,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 12,
                Children =
                {
                    messageBlock,
                    okButton
                }
            }
        };

        okButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(owner);
    }
}
