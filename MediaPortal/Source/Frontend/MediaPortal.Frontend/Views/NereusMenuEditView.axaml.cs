using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Skins.Nereus;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// Which of the editor's four stops a <see cref="NereusMenuEditView"/> is showing. The reference opened four
  /// dialogs; here they are four stops of one workflow, so that the back key steps off one level at a time and
  /// the header over the screen names where the viewer is.
  /// </summary>
  public enum NereusMenuEditScreen
  {
    /// <summary>The tiles the viewer arranged, with the buttons that move them.</summary>
    Tiles,

    /// <summary>The tiles that are not on the menu yet.</summary>
    PickTiles,

    /// <summary>The lists one tile reads its previews from.</summary>
    Lists,

    /// <summary>The lists that tile is not showing.</summary>
    PickLists
  }

  /// <summary>
  /// One stop of Nereus' home menu editor: the tiles, or one tile's media lists, either of them being arranged or
  /// being picked from.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The face is the reference's: the name of the line, then its up, down, remove and configure buttons
  /// (<c>EditMenuStyles.xaml</c>), the plus above the list that opens the picker (<c>DialogEditMenu.xaml</c>), and
  /// the picker itself a plain menu of names (<c>simple_dialog_menu.xaml</c>). What the buttons do is decided in
  /// <see cref="NereusMenuEditModel"/>, which is the reference's model; this class only says which stop is showing,
  /// pushes the next one, and hands the focus back to the button the viewer was pressing.
  /// </para>
  /// <para>
  /// The focus matters here more than anywhere else in the front end: moving a tile is done by holding one button,
  /// and a list that rebuilt itself under the viewer's finger has to give that button back or every press after
  /// the first goes somewhere else.
  /// </para>
  /// </remarks>
  public partial class NereusMenuEditView : UserControl, IWorkflowScreen, IDialogScreen
  {
    private readonly NereusMenuEditViewModel? _editor;
    private readonly NereusMenuEditScreen _screen;
    private readonly WorkflowManager? _workflow;
    private readonly Func<string, WorkflowFrame>? _frames;

    public NereusMenuEditView()
    {
      InitializeComponent();
      _screen = NereusMenuEditScreen.Tiles;
    }

    public NereusMenuEditView(NereusMenuEditViewModel editor, NereusMenuEditScreen screen,
        WorkflowManager? workflow, Func<string, WorkflowFrame>? frames)
    {
      _editor = editor;
      _screen = screen;
      _workflow = workflow;
      _frames = frames;
      DataContext = editor;
      InitializeComponent();

      Arrange();
      editor.Model.PropertyChanged += (_, _) => Arrange();
      editor.Model.FocusRequested += OnFocusWanted;
      editor.Model.MediaListEditRequested += delegate
      {
        // Only the menu's own stop follows the gear. The other three stops never offer it: a media list line's
        // gear is disabled, exactly as the reference left it.
        if (_screen == NereusMenuEditScreen.Tiles)
          OpenMediaLists();
      };
      // The reference's picker was a menu of buttons: one press on an entry was the answer, not a selection to be
      // confirmed. The list here looks like a list, so the press is caught on the entry's container instead of on a
      // button of its own, and the entry the viewer aimed at is the entry that is taken.
      AddHandler(PointerPressedEvent, OnPickPressed, handledEventsToo: true);
      AddHandler(KeyDownEvent, OnKeyDown);
    }

    /// <summary>
    /// Where the focus starts on each of the four stops, asked twice: once here and once when the layout has
    /// settled.
    /// </summary>
    /// <remarks>
    /// The lists are empty at the moment the shell hands the stop over - their rows arrive with the layout, not with
    /// the <c>ItemsSource</c> assignment - and an empty list is exactly the answer that sends this dialog's keys to
    /// the OK button instead of to the rows. A dialog whose whole point is moving a line then starts with the
    /// keyboard standing on its way out, which is what the viewer met: the arrows moved nothing they meant. Asking
    /// again once the frame has been laid out is the same ask, and every branch of it is written to be asked twice.
    /// </remarks>
    public void OnScreenActivated()
    {
      void Ask()
      {
        if (_editor == null)
          return;

        NereusMenuEditModel model = _editor.Model;
        if (_screen == NereusMenuEditScreen.Tiles && model.StartActionId.HasValue)
        {
          // The reference opened its dialog on the tile the viewer was standing on, so that nobody had to walk to
          // the tile they meant. Coming back from a sub-screen, the selection the viewer left is kept instead.
          NereusMenuEditRow? start = model.Rows.FirstOrDefault(row => row.ActionId == model.StartActionId);
          if (start != null && editList.SelectedItem == null)
          {
            editList.SelectedItem = start;
            editList.ScrollIntoView(start);
          }
        }

        if (_screen == NereusMenuEditScreen.PickTiles || _screen == NereusMenuEditScreen.PickLists)
        {
          if (!pickList.Items.OfType<object>().Any())
          {
            // Nothing left to add: there is nothing to stand on, so the screen says so by being where it is.
            return;
          }

          if (pickList.SelectedItem == null)
            pickList.SelectedIndex = 0;
          pickList.Focus();
          return;
        }

        if (editList.Items.OfType<object>().Any())
        {
          if (editList.SelectedItem == null)
            editList.SelectedIndex = 0;
          editList.Focus();
        }
        else if (okButton.IsVisible)
        {
          okButton.Focus();
        }
      }

      Ask();
      Avalonia.Threading.Dispatcher.UIThread.Post(Ask, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    private void Arrange()
    {
      if (_editor == null)
        return;

      NereusMenuEditModel model = _editor.Model;
      bool lists = _screen == NereusMenuEditScreen.Lists || _screen == NereusMenuEditScreen.PickLists;
      bool picking = _screen == NereusMenuEditScreen.PickTiles || _screen == NereusMenuEditScreen.PickLists;

      // The frame's head band carries the heading, the way the reference's dialog master carried
      // Dialog_Header_Text; the sub-line below it only names the tile whose lists are being arranged.
      _editor.Heading = _screen switch
      {
        NereusMenuEditScreen.Tiles => "Edit menu",
        NereusMenuEditScreen.PickTiles => "Add menu item",
        NereusMenuEditScreen.Lists => "Edit home content",
        _ => "Add home content"
      };

      // The lists belong to one tile, and the viewer should not have to remember which one was opened.
      subText.Text = lists ? model.SelectedLabel ?? "" : "";
      subText.IsVisible = subText.Text.Length > 0;

      plusButton.IsVisible = !picking && (lists ? model.HasOtherMediaListRows : model.HasOtherRows);
      okButton.IsVisible = !picking;

      if (picking)
      {
        editList.IsVisible = false;
        pickList.IsVisible = true;
        pickList.ItemsSource = lists ? model.OtherMediaListRows : model.OtherRows;
        hintLine.Text = lists
            ? "A press puts the list at the end of the tile's lists · Back returns to the tile"
            : "A press puts the tile at the end of the menu · Back returns to the menu";
        return;
      }

      pickList.IsVisible = false;
      editList.IsVisible = true;
      // The gear has no work on this stop - the reference's home content dialog carries no Config_Command and its
      // model builds every line with IsConfigButtonEnabled false - so the skin is told to leave it out here, while
      // the rows themselves keep the reference's shape everywhere else.
      if (lists)
        editList.Classes.Add("nereus-editor-lists");
      else
        editList.Classes.Remove("nereus-editor-lists");
      editList.ItemsSource = lists ? model.MediaListRows : model.Rows;
      hintLine.Text = lists
          ? "The arrows order the lists, the bin takes one out · OK keeps the choice, Back throws it away"
          : "The arrows move a tile, the bin takes it out of the menu, the gear says which lists it may show · "
            + "OK keeps the menu, Back leaves it as it was";
    }

    private void OnAddPress(object? sender, RoutedEventArgs args)
    {
      if (_editor == null || _workflow == null || _frames == null)
        return;

      bool lists = _screen == NereusMenuEditScreen.Lists;
      _workflow.Push(_frames(lists ? "menulistadd" : "menuadd"));
    }

    private void OnOkPress(object? sender, RoutedEventArgs args)
    {
      if (_editor == null)
        return;

      if (_screen == NereusMenuEditScreen.Lists)
      {
        // The list dialog's OK stores the choice in the editor and writes nothing, exactly as the reference's
        // DialogEditMediaListMenu did; the menu's own OK is the one that produces a file.
        _editor.SaveMediaListEdit();
      }
      else if (_screen == NereusMenuEditScreen.Tiles)
      {
        _editor.Save();
      }

      _workflow?.Pop();
    }

    private void OnKeyDown(object? sender, KeyEventArgs args)
    {
      if (args.Key != Key.Enter && args.Key != Key.Return)
        return;

      if (pickList.IsVisible && pickList.IsKeyboardFocusWithin)
      {
        PickSelected();
        args.Handled = true;
      }
    }

    /// <summary>
    /// A press on one of the offered entries takes that entry, wherever in its row the viewer pressed. The picker
    /// is a question with a one-press answer - "which tile do you want next" - and making the viewer press the
    /// name twice, or press it and then hunt for the Enter key, turns a screen with one job into a screen that
    /// appears to answer nothing at all.
    /// </summary>
    private void OnPickPressed(object? sender, PointerPressedEventArgs args)
    {
      if (!pickList.IsVisible || !(args.Source is Control source))
        return;

      // Whatever was hit - the name, the line around it, or the entry itself - the entry it belongs to is asked
      // for. The walk stops at the list, so a press on the ground below the entries asks for nothing.
      NereusMenuEditRow? row = null;
      foreach (Control node in new[] { source }.Concat(source.GetVisualAncestors().OfType<Control>()))
      {
        if (node.DataContext is NereusMenuEditRow candidate)
        {
          row = candidate;
          break;
        }

        if (ReferenceEquals(node, pickList))
          break;
      }

      if (row == null)
        return;

      // The line is lit before it is taken so that the press is visible even where it also closes the screen.
      pickList.SelectedItem = row;
      PickSelected();
      args.Handled = true;
    }

    /// <summary>
    /// Puts the picked line where the reference put it - at the end of its list - and steps back off the picker,
    /// which is what closing that dialog by pressing an entry did.
    /// </summary>
    private void PickSelected()
    {
      if (_editor == null || !(pickList.SelectedItem is NereusMenuEditRow row))
        return;

      _editor.Model.Add(row);
      _workflow?.Pop();
    }

    /// <summary>
    /// Hands the focus back to the button that was just pressed. The reference bound its buttons to a focus flag
    /// on the line and its GUI engine polled that flag; here somebody has to ask the button for the focus once,
    /// by name, after the list has moved.
    /// </summary>
    private void OnFocusWanted(object? sender, NereusMenuEditFocusEventArgs args)
    {
      if (!editList.IsVisible)
        return;

      int index = editList.Items.IndexOf(args.Row);
      if (index < 0)
        return;

      string wanted = args.Button switch
      {
        NereusMenuEditButton.Up => "nereus-editor-up",
        NereusMenuEditButton.Down => "nereus-editor-down",
        NereusMenuEditButton.Remove => "nereus-editor-remove",
        _ => "nereus-editor-config"
      };

      Control? container = editList.ContainerFromIndex(index) as Control;
      Button? button = container?.GetLogicalDescendants().OfType<Button>()
          .FirstOrDefault(candidate => candidate.Classes.Contains(wanted));
      Control target = (Control?)button ?? editList;
      target.Focus();
    }

    /// <summary>Asks the workflow for the stop that edits the open tile's media lists.</summary>
    public void OpenMediaLists()
    {
      if (_workflow != null && _frames != null)
        _workflow.Push(_frames("menulists"));
    }
  }
}
