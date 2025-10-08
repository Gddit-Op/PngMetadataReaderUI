using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using PngMetadataReaderUI.Models;
using System.Threading.Tasks;

namespace PngMetadataReaderUI.Helpers;

internal static class MessageBoxService
{
    public static Task ShowAsync(Window owner, DialogRequest request)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ContentTitle = request.Title,
            ContentMessage = request.Message,
            ButtonDefinitions = ButtonEnum.Ok,
            Icon = request.Type == DialogType.Error ? Icon.Error : Icon.Info,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        });

        return messageBox.ShowWindowDialogAsync(owner);
    }
}
