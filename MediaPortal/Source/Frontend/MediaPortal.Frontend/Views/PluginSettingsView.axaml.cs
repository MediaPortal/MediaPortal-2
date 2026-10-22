using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The plugin settings row of the settings list. Which sections there are, and what the opened one holds,
  /// is read from the server by the view model; this file only carries a press on a row across to it.
  /// </summary>
  public partial class PluginSettingsView : UserControl
  {
    /// <summary>For the XAML loader and the designer.</summary>
    public PluginSettingsView()
    {
      this.InitializeComponent();
    }

    /// <summary>
    /// A row of the list: the click lands on the section that row names, and opening it is the screen's
    /// work - reading its fields is a call, and a row is not the thing that should be making one.
    /// </summary>
    private void OnOpenPress(object? sender, RoutedEventArgs e)
    {
      if (sender is Button { DataContext: SettingSectionSummary summary }
          && DataContext is PluginSettingsViewModel model)
        model.Open(summary);
    }
  }
}
