using System;
using System.Collections.Generic;
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

      // The frame's answer row under the contents plate only presses; what a press means - keep the choice or
      // keep the menu, then step off this stop - is what the screen knows.
      editor.ConfirmAndLeave = ConfirmAndLeave;
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
      // The arrows' way into the row's buttons, before the list can spend Left/Right on its own selection.
      AddHandler(KeyDownEvent, OnRowKeysTunnel, RoutingStrategies.Tunnel);
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
        else
        {
          // Nothing to stand on: the keyboard goes to the frame's answer instead, which stands under the plate
          // of the rows - the same fallback the OK button inside the body used to be, now reached through the
          // frame that draws it.
          Button? answer = this.GetVisualDescendants().OfType<Button>()
              .FirstOrDefault(b => b.Name == "dialogAnswerOk" && b.IsEffectivelyVisible);
          answer?.Focus();
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
      // The frame's answer row under the plate: the two picking stops answer with one press on a line, exactly
      // as the reference's pickers did, so they speak no keeping word and the row leaves the dialog.
      _editor.DialogOkWord = picking ? "" : "OK";

      if (picking)
      {
        editList.IsVisible = false;
        pickList.IsVisible = true;
        pickList.ItemsSource = lists ? model.OtherMediaListRows : model.OtherRows;
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
    }

    private void OnAddPress(object? sender, RoutedEventArgs args)
    {
      if (_editor == null || _workflow == null || _frames == null)
        return;

      bool lists = _screen == NereusMenuEditScreen.Lists;
      _workflow.Push(_frames(lists ? "menulistadd" : "menuadd"));
    }

    private void ConfirmAndLeave()
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

    /// <summary>The row's buttons in the order they stand, same names the focus request from the model uses.</summary>
    private static readonly string[] ROW_GLYPHS = { "nereus-editor-up", "nereus-editor-down", "nereus-editor-remove", "nereus-editor-config" };

    private Control? FocusedControl()
    {
        return (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement()) as Control;
    }

    private static bool IsRowGlyph(Control control)
    {
      return control is Button && ROW_GLYPHS.Any(glyph => control.Classes.Contains(glyph));
    }

    /// <summary>The row's buttons as realized in <paramref name="container"/>, in their screen order.</summary>
    private static List<Button> RowButtons(Control container)
    {
      return container.GetLogicalDescendants().OfType<Button>()
          .Where(button => ROW_GLYPHS.Any(glyph => button.Classes.Contains(glyph)))
          .OrderBy(button => System.Array.FindIndex(ROW_GLYPHS, glyph => button.Classes.Contains(glyph)))
          .ToList();
    }

    /// <summary>
    /// The arrows' path through the arranged list, in the language the skin already speaks on its other screens:
    /// the list keeps up and down for its lines, and left and right - which a vertical list has no use for - are
    /// the way in and out of one line's buttons. On a button, left and right walk its row and up and down step to
    /// the same button of the row above or below, because moving a tile is a walk, not a hunt. Above the first
    /// row stands the plus, which is otherwise a mouse's private button.
    /// </summary>
    /// <remarks>
    /// From the tunnel, exactly as the connection screen takes the arrows off its list: the list would otherwise
    /// spend left and right on moving the selection, which is where every key went and no button ever lit.
    /// </remarks>
    private void OnRowKeysTunnel(object? sender, KeyEventArgs args)
    {
      if (_editor == null || !editList.IsVisible || editList.Items.Count == 0)
        return;

      Control? focused = FocusedControl();
      if (focused == null)
        return;

      if (ReferenceEquals(focused, plusButton))
      {
        // Down walks back off the plus into the lines, onto the line the viewer left.
        if (args.Key == Key.Down)
        {
          StepBackIntoList();
          args.Handled = true;
        }
        return;
      }

      if (focused is Button keeping && keeping.Name == "dialogAnswerOk")
      {
        // Up walks back off the frame's keeping answer into the lines, as from the plus.
        if (args.Key == Key.Up)
        {
          StepBackIntoList();
          args.Handled = true;
        }
        return;
      }

      if (IsRowGlyph(focused))
      {
        ListBoxItem? item = focused.GetVisualAncestors().OfType<ListBoxItem>().FirstOrDefault();
        if (item == null)
          return;

        int index = editList.Items.IndexOf(item.DataContext!);
        if (index < 0)
          return;

        switch (args.Key)
        {
          case Key.Left:
          case Key.Right:
          {
            List<Button> reachable = RowButtons(item).Where(button => button.IsEnabled).ToList();
            int at = reachable.IndexOf((Button)focused);
            if (reachable.Count == 0 || at < 0)
              return;
            Button next = reachable[(at + (args.Key == Key.Right ? 1 : reachable.Count - 1)) % reachable.Count];
            if (next.Focus())
              args.Handled = true;
            return;
          }
          case Key.Up:
          case Key.Down:
          {
            bool upward = args.Key == Key.Up;
            if (upward && index == 0)
            {
              if (plusButton.IsVisible)
              {
                StepOutOfListInto(plusButton);
                args.Handled = true;
              }
              return;
            }

            int neighbour = index + (upward ? -1 : 1);
            if (neighbour >= editList.Items.Count)
            {
              // Below the last line's buttons stands the frame's keeping answer.
              Button? answer = FrameAnswer();
              if (answer != null)
              {
                StepOutOfListInto(answer);
                args.Handled = true;
              }
              return;
            }
            if (neighbour < 0)
              return;

            string wanted = System.Array.Find(ROW_GLYPHS, glyph => focused.Classes.Contains(glyph));
            Control? container = editList.ContainerFromIndex(neighbour) as Control;
            editList.SelectedItem = editList.Items[neighbour];
            editList.ScrollIntoView(editList.SelectedItem);
            Button? same = container == null
                ? null
                : RowButtons(container).FirstOrDefault(button => button.Classes.Contains(wanted) && button.IsEnabled);
            // The same button of the next line if it can be stood on - the first row's up and the last row's
            // down cannot - and the line itself otherwise, which keeps the arrow's promise either way.
            if ((same != null && same.Focus()) || editList.Focus())
              args.Handled = true;
            return;
          }
        }
        return;
      }

      if (!editList.IsKeyboardFocusWithin)
        return;

      switch (args.Key)
      {
        case Key.Left:
        case Key.Right:
        {
          if (!(editList.SelectedItem is NereusMenuEditRow))
            return;
          Control? container = editList.ContainerFromIndex(editList.Items.IndexOf(editList.SelectedItem!)) as Control;
          if (container == null)
            return;
          List<Button> reachable = RowButtons(container).Where(button => button.IsEnabled).ToList();
          if (reachable.Count == 0)
            return;
          // Right enters the row at its first button and Left at its last, so the key points where it walks.
          Button want = args.Key == Key.Right ? reachable[0] : reachable[reachable.Count - 1];
          if (want.Focus())
            args.Handled = true;
          return;
        }
        case Key.Up:
        {
          // The plus is reached the way it stands: above the first line.
          if (editList.SelectedIndex == 0 && plusButton.IsVisible)
          {
            StepOutOfListInto(plusButton);
            args.Handled = true;
          }
          return;
        }
        case Key.Down:
        {
          // The keeping answer is reached the way it stands: below the last line. Away from the last line, the
          // key belongs to the answer and not to the list, which has nowhere further down to go anyway.
          if (editList.SelectedIndex == editList.Items.Count - 1)
          {
            Button? answer = FrameAnswer();
            if (answer != null)
            {
              StepOutOfListInto(answer);
              args.Handled = true;
            }
          }
          return;
        }
      }
    }

    /// <summary>The line the caret stood on when the keyboard walked out of the list, kept to be given back.</summary>
    private NereusMenuEditRow? _caretLeftBehind;

    /// <summary>The frame's keeping answer, when this stop of the editor speaks one.</summary>
    private Button? FrameAnswer()
    {
      return this.GetVisualDescendants().OfType<Button>()
          .FirstOrDefault(b => b.Name == "dialogAnswerOk" && b.IsEffectivelyVisible && b.IsEffectivelyEnabled);
    }

    /// <summary>
    /// Walking the keyboard out of the list - to the plus above, to the keeping answer below - hands the white
    /// sheet of its line back too: a line standing marked while the key is somewhere else is a caret the viewer
    /// does not have. The line it stood on is kept, and stepped back onto when the key returns.
    /// </summary>
    private void StepOutOfListInto(Control target)
    {
      _caretLeftBehind = editList.SelectedItem as NereusMenuEditRow;
      editList.SelectedItem = null;
      target.Focus();
    }

    private void StepBackIntoList()
    {
      NereusMenuEditRow? keep = _caretLeftBehind;
      _caretLeftBehind = null;
      if (keep != null && editList.Items.Contains(keep))
        editList.SelectedItem = keep;
      else if (editList.SelectedItem == null && editList.Items.Count > 0)
        editList.SelectedIndex = 0;
      editList.Focus();
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
