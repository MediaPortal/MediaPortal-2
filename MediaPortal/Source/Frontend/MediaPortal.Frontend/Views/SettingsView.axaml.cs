using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The settings hub: the wall of places a household configures, the narrowed list of them once one is open,
  /// and the screen of the one it picked.
  /// </summary>
  /// <remarks>
  /// Both shapes of the hub are on the screen at once and only one of them is ever visible, so the d-pad ring
  /// names both - a stop the viewer cannot see is stepped over rather than stopped at, which is what
  /// <see cref="DpadNavigation"/> does with an invisible control. Back is a key of this screen as well as the
  /// shell's: out of a screen into the wall, out of a folder into the level above, and only then out of the
  /// settings - the order the reference's configuration states kept.
  /// </remarks>
  public partial class SettingsView : UserControl, IWorkflowScreen
  {
    private readonly SettingsViewModel _model;

    private readonly List<Control> _ring = new List<Control>();

    /// <summary>For the XAML loader: an empty hub, which the designer shows as an empty list.</summary>
    public SettingsView()
        : this(new SettingsViewModel())
    {
    }

    public SettingsView(SettingsViewModel sections)
    {
      DataContext = sections;
      _model = sections;
      InitializeComponent();

      _ring.Add(wallList);
      _ring.Add(sectionList);
      _ring.Add(backButton);
      DpadNavigation.Attach(this, _ring);

      // Both lists show the level and only one of them is ever looked at, and a press leaves a number behind in
      // whichever held it - the number of the plate last pressed, which the control walks back to on the next
      // press. A number belonging to a level that is no longer on screen is not a wrong selection but an
      // ArgumentException out of Avalonia's selection model, unhandled, and that is what closed the client. So
      // both lists are made to forget theirs the moment the hub hands over another level.
      sections.LevelChanged += delegate { ForgetPlaces(); };

      // Nothing opens itself on arrival. The reference's configuration screen showed a wall of plates until a
      // plate was picked, and a wall is not an empty screen - opening the first section instead would be the
      // hub deciding what a viewer came for.
    }

    /// <summary>
    /// Dropping the places the two lists remember: the selected one, and the anchor a single-select list walks
    /// back to when the next plate is pressed.
    /// </summary>
    private void ForgetPlaces()
    {
      Forget(wallList);
      Forget(sectionList);
    }

    private static void Forget(ListBox list)
    {
      list.SelectedIndex = -1;
      list.Selection.AnchorIndex = -1;
    }

    /// <summary>Runs every time the workflow shows this screen. See IWorkflowScreen.</summary>
    public void OnScreenActivated()
    {
      Target().Focus();
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
      if (_model.GoBack())
        Target().Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
      // The back key, for a viewer on a keyboard. The remote's Back reaches the same answer through the shell;
      // this is the hub's own half of it, which stops at the first level and hands the key back from there.
      if ((e.Key == Key.Escape || e.Key == Key.Back) && _model.GoBack())
      {
        e.Handled = true;
        Target().Focus();
        return;
      }

      base.OnKeyDown(e);
    }

    /// <summary>The list the viewer is actually looking at, so that a press after going back walks the level
    /// they landed on rather than the one they left.</summary>
    private Control Target()
    {
      return _model.IsWall ? wallList : (Control) sectionList;
    }
  }
}