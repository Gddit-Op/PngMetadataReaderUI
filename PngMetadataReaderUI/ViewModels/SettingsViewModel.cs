using CommunityToolkit.Mvvm.ComponentModel;
using PngMetadataReaderUI.Models;

namespace PngMetadataReaderUI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _ipAddress = "127.0.0.1";

    [ObservableProperty]
    private int _port = 1234;

    [ObservableProperty]
    private double _temperature = 0.7;

    [ObservableProperty]
    private int _maxTokens = 600;

    [ObservableProperty]
    private string _modelId = "lfm2-vl-1.6b";

    public void Apply(UserSettings settings)
    {
        IpAddress = settings.IpAddress;
        Port = settings.Port;
        Temperature = settings.Temperature;
        MaxTokens = settings.MaxTokens;
        ModelId = settings.ModelId;
    }

    public UserSettings ToSettings()
    {
        return new UserSettings
        {
            IpAddress = IpAddress,
            Port = Port,
            Temperature = Temperature,
            MaxTokens = MaxTokens,
            ModelId = ModelId
        };
    }
}
