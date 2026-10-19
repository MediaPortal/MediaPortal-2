using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The settings hub: the list of places a household configures, and the screen of the one it picked.
  /// </summary>
  /// <remarks>
  /// The d-pad ring is the list alone - the embedded screens on the right are the same controls they have
  /// always been, reachable by Tab and mouse exactly as before, while the remote walks the list. Back is the
  /// shell's key: it leaves the settings hub the way it leaves any other screen.
  /// </remarks>
  public partial class SettingsView : UserControl, IWorkflowScreen
  {
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>For the XAML loader: an empty hub, which the designer shows as an empty list.</summary>
    public SettingsView()
        : this(new SettingsViewModel())
    {
    }

    public SettingsView(SettingsViewModel sections)
    {
      DataContext = sections;
      InitializeComponent();

      _ring.Add(sectionList);
      DpadNavigation.Attach(this, _ring);

      // The first section opens itself, so the right of the screen is never empty on arrival - a hub that
      // shows nothing until the viewer picks is a hub that looks broken from the couch.
      if (sections.Sections.Count > 0)
        sections.Selected = sections.Sections[0];
    }

    /// <summary>Runs every time the workflow shows this screen. See IWorkflowScreen.</summary>
    public void OnScreenActivated()
    {
      sectionList.Focus();
    }
  }
}