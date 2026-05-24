using System.Windows;
using System.Windows.Media;

namespace TreeManager.App.L0.Fixtures;

public sealed class ApplicationResourceFixture : IDisposable
{
    public ApplicationResourceFixture()
    {
        if (Application.Current == null)
        {
            _ = new Application();
        }

        var resources = Application.Current.Resources;
        resources["AddModeBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3E5F5"));
        resources["EditTreeModeBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F7FA"));
        resources["EditDraftModeBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9FBE7"));
    }

    public void Dispose() { /* Application is process-singleton; let it live */ }
}
