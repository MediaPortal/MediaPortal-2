using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
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

      // The wall is walked with the caret of the keyboard, not with the list's selection: a picked plate IS
      // an opened section here, so a list that answered the arrow keys with its own selection would open a
      // section on every key - the hub deciding twice per breath what the viewer came for. This handler
      // therefore takes the arrow keys off the list while a plate stands under the caret, moves the caret
      // from plate to plate, and answers Enter as the press of the plate stood on. A direction nobody stands
      // under - no plate has the caret, or the wall ends in that direction - is left for the ring above.
      wallList.AddHandler(KeyDownEvent, OnWallKeysTunnel, RoutingStrategies.Tunnel);

      // Both lists show the level and only one of them is ever looked at, and a press leaves a number behind in
      // whichever held it - the number of the plate last pressed, which the control walks back to on the next
      // press. A number belonging to a level that is no longer on screen is not a wrong selection but an
      // ArgumentException out of Avalonia's selection model, unhandled, and that is what closed the client. So
      // both lists are made to forget theirs the moment the hub hands over another level - and the caret walks
      // onto the first plate of the new level, so the wall a viewer lands on always has a plate stood on.
      sections.LevelChanged += delegate
      {
        ForgetPlaces();
        ApplyHeaderTitle();
        StandCaretOn(Target(), CARET_TRIES);
      };

      // No SECTION opens itself on arrival: opening the first one instead would be the hub deciding what a
      // viewer came for. A PLATE standing under the caret is not an opening - it is where the first arrow key
      // of the visit walks from, and what the edge of the wall says "this one" about.

      // The same ask the home makes (HomeView): take the header out of its row and let it float over the
      // screen, so the skin's ground reaches the top edge of the television and the clock stands over the
      // ground and its darkening rather than over a band of black. The bands this screen paints are the
      // home's own - see the overlays in the markup - and a screen that planned its space under a header
      // row would find its top cut off by the floating strip, so the ask is made on the way in and handed
      // back on the way out.
      AttachedToVisualTree += delegate
      {
        _chromeShell = this.GetVisualAncestors().OfType<ShellWindow>().FirstOrDefault();
        _chromeShell?.SetChromeHeaderOverlay(true);
        ApplyHeaderTitle();
      };
      DetachedFromVisualTree += delegate
      {
        _chromeShell?.SetChromeHeaderOverlay(false);
        _chromeShell = null;
      };
    }

    private ShellWindow? _chromeShell;

    /// <summary>
    /// Tells the shell header which word to show for this screen. The header names itself after the workflow
    /// frame, and the whole hub lives on one frame labelled "Settings" - so walking into "General" or deeper
    /// would otherwise leave the header reading "Settings" over a wall that is plainly something else. The hub
    /// knows its own trail, so it hands the shell the door it last walked through; back on the first wall that
    /// word is just "Settings" again. See <see cref="ShellWindow.SetChromeTitle"/>.
    /// </summary>
    private void ApplyHeaderTitle()
    {
      _chromeShell?.SetChromeTitle(_model.HeaderTitle);
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
      // A frame pushed over the hub and left again makes the shell name the header from the frame's label,
      // which is "Settings" however deep the viewer had walked inside the hub before the dialog came. The
      // word is spoken again here because this call is the hub's notice that it is back on screen.
      ApplyHeaderTitle();

      Control target = Target();
      target.Focus();
      StandCaretOn(target, CARET_TRIES);
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
      if (_model.GoBack())
      {
        Target().Focus();
        StandCaretOn(Target(), CARET_TRIES);
      }
    }

    /// <summary>
    /// How often a caret asks again when the plates it wants are not drawn yet - the same bounded asking
    /// the news column does (NewsView), because a list whose rows are still arriving answers the first
    /// arrow key with a page rather than a row.
    /// </summary>
    private const int CARET_TRIES = 6;

    /// <summary>
    /// Stands the caret ON a plate: the first plate of a wall - never picked, only focused, because a picked
    /// plate here is an opened section - or the row of the narrowed list that shows the open screen, which
    /// is the row the list has already picked. Asks again every layout pass while the list has kept the
    /// keyboard, because the containers may not be drawn the moment the screen is activated.
    /// </summary>
    private static void StandCaretOn(Control target, int tries)
    {
      if (target is not ListBox list)
        return;

      int index = list.SelectedIndex >= 0 ? list.SelectedIndex : 0;
      if (list.Items.Count > 0
          && list.ContainerFromIndex(index) is Control plate && plate.IsEffectivelyVisible && plate.Focus())
        return;

      if (tries <= 0 || list.Items.Count == 0)
        return;

      list.Focus();
      Dispatcher.UIThread.Post(
          delegate
          {
            if (list.IsKeyboardFocusWithin)
              StandCaretOn(list, tries - 1);
          },
          DispatcherPriority.Loaded);
    }

    /// <summary>The number of the plate the caret stands on, or -1 while no plate of the wall has it.</summary>
    private int StoodPlate()
    {
      for (int i = 0; i < wallList.Items.Count; i++)
      {
        if (wallList.ContainerFromIndex(i) is ListBoxItem { IsKeyboardFocusWithin: true })
          return i;
      }

      return -1;
    }

    /// <summary>
    /// The plate one row above (-1) or below (+1) the stood one: the wall wraps, so a row is what the
    /// containers' own places say, and the neighbour of a plate is the plate of the next row that stands
    /// nearest under it. -1 when the wall has no such row - the top has nothing above it.
    /// </summary>
    private int RowNeighbour(int index, int down)
    {
      if (wallList.ContainerFromIndex(index) is not Control stood)
        return -1;

      double stoodTop = stood.Bounds.Top;
      double rowTop = double.NaN;
      for (int i = 0; i < wallList.Items.Count; i++)
      {
        if (i == index || wallList.ContainerFromIndex(i) is not Control plate || !plate.IsEffectivelyVisible)
          continue;

        double top = plate.Bounds.Top;
        bool beyond = down > 0 ? top > stoodTop + 10 : top < stoodTop - 10;
        if (beyond && (double.IsNaN(rowTop) || (down > 0 ? top < rowTop : top > rowTop)))
          rowTop = top;
      }

      if (double.IsNaN(rowTop))
        return -1;

      double stoodCenter = stood.Bounds.Center.X;
      int best = -1;
      double bestDistance = double.MaxValue;
      for (int i = 0; i < wallList.Items.Count; i++)
      {
        if (wallList.ContainerFromIndex(i) is not Control plate || !plate.IsEffectivelyVisible)
          continue;
        if (Math.Abs(plate.Bounds.Top - rowTop) > 10)
          continue;

        double distance = Math.Abs(plate.Bounds.Center.X - stoodCenter);
        if (distance < bestDistance)
        {
          bestDistance = distance;
          best = i;
        }
      }

      return best;
    }

    private void OnWallKeysTunnel(object? sender, KeyEventArgs e)
    {
      if (!_model.IsWall)
        return;

      int stood = StoodPlate();
      if (stood < 0)
        return;

      if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Space)
      {
        // The press of the plate the viewer stands on. Selection stays out of it: the press goes to the hub
        // as the pointer's press of the same plate goes, and the caret alone does not pick.
        if (wallList.Items[stood] is SettingsSection plate)
        {
          e.Handled = true;
          _ = _model.OpenAsync(plate);
        }

        return;
      }

      int to = e.Key switch
      {
        Key.Left => stood - 1,
        Key.Right => stood + 1,
        Key.Up => RowNeighbour(stood, -1),
        Key.Down => RowNeighbour(stood, 1),
        _ => -2
      };

      if (to == -2 || to < 0 || to >= wallList.Items.Count)
        return; // not an arrow, or the wall ends that way - the ring above takes the key from here

      if (wallList.ContainerFromIndex(to) is Control next && next.IsEffectivelyVisible)
      {
        e.Handled = true;
        next.Focus();
        return;
      }

      // A plate the virtualized wall has not drawn yet is asked for by number: the scroll realizes it and
      // the caret steps onto it a layout later. The key is kept meanwhile - letting it out here would hand
      // the list a selection to commit, and a committed selection is an opened section.
      e.Handled = true;
      wallList.ScrollIntoView(to);
      StandOnRealizedPlate(to, CARET_TRIES);
    }

    private void StandOnRealizedPlate(int index, int tries)
    {
      if (wallList.ContainerFromIndex(index) is Control plate && plate.IsEffectivelyVisible)
      {
        plate.Focus();
        return;
      }

      if (tries <= 0)
        return;

      Dispatcher.UIThread.Post(
          delegate
          {
            if (wallList.IsKeyboardFocusWithin && _model.IsWall)
              StandOnRealizedPlate(index, tries - 1);
          },
          DispatcherPriority.Loaded);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
      // The back key, for a viewer on a keyboard. The remote's Back reaches the same answer through the shell;
      // this is the hub's own half of it, which stops at the first level and hands the key back from there.
      if ((e.Key == Key.Escape || e.Key == Key.Back) && _model.GoBack())
      {
        e.Handled = true;
        Target().Focus();
        StandCaretOn(Target(), CARET_TRIES);
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