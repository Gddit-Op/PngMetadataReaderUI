using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using PngMetadataReaderUI.ViewModels;
using PngMetadataReaderUI.Views;

namespace PngMetadataReaderUI
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Use reflection to access internal Avalonia BindingPlugins and remove the DataAnnotationsValidationPlugin instances.
            var bindingPluginsType = System.Type.GetType("Avalonia.Data.Core.BindingPlugins, Avalonia");
            if (bindingPluginsType == null)
                return;

            var prop = bindingPluginsType.GetProperty("DataValidators", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (prop == null)
                return;

            var dataValidators = prop.GetValue(null) as System.Collections.IList;
            if (dataValidators == null)
                return;

            // Collect plugins to remove (identify by type name to avoid internal type visibility issues)
            var toRemove = new System.Collections.Generic.List<object>();
            foreach (var plugin in dataValidators)
            {
                var pluginType = plugin?.GetType();
                if (pluginType != null && pluginType.Name == "DataAnnotationsValidationPlugin" && pluginType.Namespace == "Avalonia.Data.Core")
                {
                    toRemove.Add(plugin);
                }
            }

            // Remove the identified plugins
            foreach (var plugin in toRemove)
            {
                dataValidators.Remove(plugin);
            }
        }
    }
}
