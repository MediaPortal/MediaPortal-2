using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MediaPortal.Frontend.Views
{
    /// <summary>
    /// The server-settings row of the settings list. The view model loads the values
    /// itself when the row opens; the view only draws them.
    /// </summary>
    public partial class ServerSettingsView : UserControl
    {
        public ServerSettingsView()
        {
            this.InitializeComponent();
        }
    }
}
