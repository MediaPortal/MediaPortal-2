/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The on-screen keyboard of the reference, on our screen: the four key grids of the ported layout, shown
  /// one at a time by the reference's own state rule, with its ControlArea column beside them.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The keyboard edits the field that asked for it, at its caret - the reference appends to the text it owns,
  /// but here the field stays the truth so that a viewer with a real keyboard and a viewer poking keys with a
  /// mouse cannot disagree about what the field holds. Everything else follows the reference's control: the
  /// text being edited stands above the keys (masked, the way its <c>PasswordText</c> style masks), a typed
  /// key spends Shift but not Caps Lock, the grids answer <c>(Shift || Caps) &amp;&amp; AltGr</c> and its three
  /// siblings, and the done button ends the edit.
  /// </para>
  /// <para>
  /// The reference shows this as a dialog arranged above or below the edited field. Here it is shown by the
  /// shell window as a layer over the current screen - the same dialog position at the bottom of the picture,
  /// with the field still visible above it, which for every field we have is where the reference would have
  /// put it too.
  /// </para>
  /// </remarks>
  public partial class VirtualKeyboardView : UserControl
  {
    private readonly TextBox? _editor;
    private readonly VirtualKeyboardModel _model;
    private readonly Grid[] _grids = new Grid[4];
    private bool _syncing;
    private EventHandler<Avalonia.AvaloniaPropertyChangedEventArgs>? _followsField;
    private Button[] _side = Array.Empty<Button>();
    private DispatcherTimer? _bounceTimer;

    /// <summary>
    /// How far the keys lean against an edge and spring back.  The dialog's own numbers, the ones the volume
    /// dialog uses (VolumePicker.axaml.cs:39-41), because this is the same kind of furniture leaning on the
    /// same screen.
    /// </summary>
    private const double KEYS_BOUNCE_DIVE = 22;
    private const int KEYS_BOUNCE_DIVE_MS = 90;
    private const int KEYS_BOUNCE_SPRING_MS = 430;
    private EventHandler<Avalonia.Input.KeyEventArgs>? _windowKeys;
    private Window? _keyedWindow;

    /// <summary>The edit is over - whoever showed the keyboard takes it away again.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>For the XAML loader and the designer: an empty English keyboard.</summary>
    public VirtualKeyboardView()
      : this(null, VirtualKeyboardLayout.English)
    {
    }

    public VirtualKeyboardView(TextBox? editor, VirtualKeyboardLayout layout)
    {
      _editor = editor;
      _model = new VirtualKeyboardModel(layout);
      InitializeComponent();

      BuildGrids();

      // The marks at the left hand answer the five rows of the grids one under the other, so the walk can step
      // off the left edge of a row onto the mark of that row.
      _side = new Button[] { altGrButton, shiftButton, capsButton, copyButton, pasteButton };

      // The keys are a grid, so the arrow keys walk them like a grid. This package has no directional tab
      // navigation to ask for that - the mode does not exist - so the walk is done here, one row and one
      // column at a time, skipping the blank fillers the reference's shifted grids leave behind.
      AddHandler(Avalonia.Input.InputElement.KeyDownEvent, OnGridArrowKey,
          Avalonia.Interactivity.RoutingStrategies.Bubble);

      shiftButton.Click += delegate { _model.Shift = shiftButton.IsChecked == true; };
      capsButton.Click += delegate { _model.Caps = capsButton.IsChecked == true; };
      altGrButton.Click += delegate { _model.AltGr = altGrButton.IsChecked == true; };
      copyButton.Click += OnCopy;
      pasteButton.Click += OnPaste;

      _model.Changed += delegate { ReflectState(); };
      _model.Begin(editor?.Text);
      ReflectState();
    }

    /// <summary>The keyboard starts with the editor's text and a clean state, like the reference's Show().</summary>
    public void Begin()
    {
      _model.Begin(_editor?.Text);
      ReflectState();
    }

    /// <summary>
    /// Builds the four grids once, from the layout, and lets the state decide which stands. Each row is a
    /// stretched grid of its own - the reference's column counts make the short rows carry wider keys.
    /// </summary>
    private void BuildGrids()
    {
      for (int gridIndex = 0; gridIndex < 4; gridIndex++)
      {
        bool shifted = (gridIndex & 1) != 0;
        bool altGr = (gridIndex & 2) != 0;
        VirtualKeyboardKey[][] rows = _model.Layout.For(shifted, altGr);

        Grid grid = new Grid();
        for (int r = 0; r < rows.Length; r++)
          grid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition(GridLength.Auto));

        for (int r = 0; r < rows.Length; r++)
        {
          VirtualKeyboardKey[] keys = rows[r];
          Grid rowGrid = new Grid { Margin = new Avalonia.Thickness(0, r == 0 ? 0 : 6, 0, 0), ColumnSpacing = 6 };
          if (r == rows.Length - 1)
          {
            // The reference's last row: a wide space and the done button beside it.
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
          }
          else
          {
            for (int c = 0; c < keys.Length; c++)
              rowGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
          }

          for (int c = 0; c < keys.Length; c++)
          {
            VirtualKeyboardKey key = keys[c];
            Button button = new Button
            {
              Classes = { "keyboard-key" },
              Content = key.Legend,
              FontSize = 26,
              MinWidth = key.Type == VirtualKeyType.Disabled ? 0 : 40,
              Padding = new Avalonia.Thickness(4, 6),
              HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
              HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
              IsEnabled = key.IsEnabled,
              IsTabStop = key.IsEnabled
            };
            Grid.SetColumn(button, c);
            button.Click += delegate { Press(key); };
            rowGrid.Children.Add(button);
          }

          Grid.SetRow(rowGrid, r);
          grid.Children.Add(rowGrid);
        }

        grid.IsVisible = false;
        _grids[gridIndex] = grid;
        gridArea.Children.Add(grid);
      }
    }

    /// <summary>
    /// One arrow key of the remote, spent on the keyboard: the walk changes the row and keeps the column as
    /// close as the row allows - the reference's rows have different key counts, so the key under a viewer's
    /// thumb when they walk down and up again must be the same key. Rows and blanks the reference's shifted
    /// grids leave behind cannot take the focus and are walked past.
    /// </summary>
    private void OnGridArrowKey(object? sender, Avalonia.Input.KeyEventArgs args)
    {
      if (args.Handled)
        return;

      Avalonia.Input.Key key = args.Key;
      if (key != Avalonia.Input.Key.Up && key != Avalonia.Input.Key.Down
          && key != Avalonia.Input.Key.Left && key != Avalonia.Input.Key.Right)
        return;

      Button? stood = FocusedKey();
      if (stood == null)
      {
        // Arrows that arrive while the caret still stands in the field take the first key carrying a character,
        // so that nobody is left pressing arrows against keys which do not answer.
        Button? first = FirstUsableKey();
        if (first != null && first.Focus())
          args.Handled = true;
        return;
      }

      Grid? rows = null;
      foreach (Control child in gridArea.Children)
      {
        if (child is Grid grid && grid.IsVisible)
        {
          rows = grid;
          break;
        }
      }

      // The marks at the left hand belong to the keyboard and are not furniture beside it: they answer the very
      // rows the grids have, five under five, so the walk steps off the left edge of a row onto the mark of that
      // row and back.  Asked for, because with a keyboard no hand could reach them at all.
      int side = SideIndexOf(stood);
      if (side >= 0)
      {
        if (key == Avalonia.Input.Key.Right)
        {
          Button? into = rows == null ? null : FirstKeyInRow(rows, side);
          if (into != null && into.Focus())
            args.Handled = true;
          else
            Bounce(+1, 0);
          return;
        }

        if (key == Avalonia.Input.Key.Left)
        {
          Bounce(-1, 0);
          args.Handled = true;
          return;
        }

        Button? mark = SideAt(side + (key == Avalonia.Input.Key.Up ? -1 : +1));
        if (mark != null && mark.Focusable && mark.IsEnabled && mark.Focus())
          args.Handled = true;
        else
          Bounce(0, key == Avalonia.Input.Key.Up ? -1 : +1);
        return;
      }

      if (rows == null)
        return;

      int fromRow = -1;
      int fromColumn = -1;
      for (int r = 0; r < rows.Children.Count; r++)
      {
        if (!(rows.Children[r] is Grid rowGrid))
          continue;
        foreach (Control cell in rowGrid.Children)
        {
          if (cell is Button button && button.IsFocused)
          {
            fromRow = r;
            fromColumn = Grid.GetColumn(button);
          }
        }
      }

      if (fromRow < 0)
        return;

      Button? target;
      if (key == Avalonia.Input.Key.Left || key == Avalonia.Input.Key.Right)
      {
        if (!(rows.Children[fromRow] is Grid sameRow))
          return;
        target = NearestKey(sameRow, fromColumn, key == Avalonia.Input.Key.Left ? -1 : +1, false);
        if (target == null)
        {
          // Off the edge of the keys: the left hand lands on the mark of this row, and where even that is gone -
          // and at the right hand always - the whole keyboard leans into the edge and springs back, which is how
          // this face says "here is the end" everywhere else.
          Button? mark = key == Avalonia.Input.Key.Left ? SideAt(fromRow) : null;
          if (mark != null && mark.Focusable && mark.IsEnabled && mark.Focus())
            args.Handled = true;
          else
            Bounce(key == Avalonia.Input.Key.Left ? -1 : +1, 0);
          return;
        }
      }
      else
      {
        int toRow = fromRow + (key == Avalonia.Input.Key.Up ? -1 : +1);
        if (toRow < 0 || toRow >= rows.Children.Count || !(rows.Children[toRow] is Grid row))
        {
          Bounce(0, key == Avalonia.Input.Key.Up ? -1 : +1);
          args.Handled = true;
          return;
        }
        target = NearestKey(row, fromColumn, -1, true) ?? NearestKey(row, fromColumn, +1, true);
        if (target == null)
        {
          Bounce(0, key == Avalonia.Input.Key.Up ? -1 : +1);
          args.Handled = true;
          return;
        }
      }

      if (target.Focus())
        args.Handled = true;
    }

    /// <summary>The key of this keyboard the caret stands on, or none when it stands elsewhere.</summary>
    private Button? FocusedKey()
    {
      foreach (Button tile in this.GetVisualDescendants().OfType<Button>())
      {
        if (tile.IsFocused)
          return tile;
      }

      return null;
    }

    /// <summary>The left-hand mark of a row, counted from the head: Alt Gr, Shift, Caps Lock, Copy, Paste.</summary>
    private Button? SideAt(int row)
    {
      return row >= 0 && row < _side.Length ? _side[row] : null;
    }

    /// <summary>Which row a left-hand mark answers, or -1 for a key of the grids.</summary>
    private int SideIndexOf(Control? key)
    {
      for (int r = 0; r < _side.Length; r++)
      {
        if (ReferenceEquals(_side[r], key))
          return r;
      }

      return -1;
    }

    /// <summary>The left-most key carrying a character in one row of the standing grid.</summary>
    private static Button? FirstKeyInRow(Grid rows, int row)
    {
      if (row < 0 || row >= rows.Children.Count || !(rows.Children[row] is Grid rowGrid))
        return null;

      Button? best = null;
      foreach (Control cell in rowGrid.Children)
      {
        if (cell is Button button && button.Focusable && button.IsEnabled
            && (best == null || Grid.GetColumn(button) < Grid.GetColumn(best)))
          best = button;
      }

      return best;
    }

    /// <summary>Whether the keys are leaning against an edge right now (asked by the tests).</summary>
    internal bool KeysNodding => _bounceTimer != null && _bounceTimer.IsEnabled;

    /// <summary>
    /// Lean the whole keyboard toward an edge of the screen and spring it back - the volume dialog's nod
    /// (VolumePicker.axaml.cs:213) and the rail's (MediaBrowserView), so an edge reads the same everywhere.
    /// </summary>
    /// <param name="directionX">+1 off the right edge, -1 off the left, 0 for a vertical nod.</param>
    /// <param name="directionY">+1 off the bottom edge, -1 off the top, 0 for a sideways nod.</param>
    private void Bounce(int directionX, int directionY)
    {
      // One lean at a time; a key pressed while the keys are still travelling starts a fresh one from wherever
      // they stand, so a viewer leaning on the arrow sees the keyboard lean with every press.
      _bounceTimer?.Stop();

      Avalonia.Media.TranslateTransform nod = slab.RenderTransform as Avalonia.Media.TranslateTransform;
      if (nod == null)
      {
        nod = new Avalonia.Media.TranslateTransform();
        slab.RenderTransform = nod;
      }

      double diveX = directionX * KEYS_BOUNCE_DIVE;
      double diveY = directionY * KEYS_BOUNCE_DIVE;
      double fromX = nod.X;
      double fromY = nod.Y;
      long started = Environment.TickCount64;

      DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render)
      {
        Interval = TimeSpan.FromMilliseconds(16)
      };
      _bounceTimer = timer;

      timer.Tick += delegate
      {
        if (!ReferenceEquals(_bounceTimer, timer))
          return; // a newer lean drives the keys from here on

        long elapsed = Environment.TickCount64 - started;
        if (elapsed >= KEYS_BOUNCE_DIVE_MS + KEYS_BOUNCE_SPRING_MS)
        {
          timer.Stop();
          nod.X = 0;
          nod.Y = 0;
          return;
        }

        if (elapsed <= KEYS_BOUNCE_DIVE_MS)
        {
          double u = elapsed / (double)KEYS_BOUNCE_DIVE_MS;
          double lean = 1 - (1 - u) * (1 - u);
          nod.X = fromX + (diveX - fromX) * lean;
          nod.Y = fromY + (diveY - fromY) * lean;
          return;
        }

        double v = (elapsed - KEYS_BOUNCE_DIVE_MS) / (double)KEYS_BOUNCE_SPRING_MS;
        const double c1 = 1.70158;
        const double c3 = c1 + 1;
        double back = 1 + c3 * Math.Pow(v - 1, 3) + c1 * Math.Pow(v - 1, 2);
        nod.X = diveX * (1 - back);
        nod.Y = diveY * (1 - back);
      };
      timer.Start();
    }

    /// <summary>
    /// The focusable key of a row whose column stands nearest to the given one in the given direction.
    /// Within a row a different column is demanded - the viewer would stay put; into a new row the very same
    /// column is answered by its own key, and its neighbour stands in when the rows count differently.
    /// </summary>
    private static Button? NearestKey(Grid rowGrid, int column, int direction, bool allowSameColumn)
    {
      Button? best = null;
      int bestDistance = int.MaxValue;
      foreach (Control cell in rowGrid.Children)
      {
        if (!(cell is Button button) || !button.Focusable || !button.IsEnabled)
          continue;
        int distance = (Grid.GetColumn(button) - column) * direction;
        if (distance < 0 || (distance == 0 && !allowSameColumn))
          continue;
        if (distance < bestDistance)
        {
          best = button;
          bestDistance = distance;
        }
      }

      return best;
    }

    /// <summary>
    /// Presses one key. The model decides what the modifiers do (and when the edit is done); the field takes
    /// the change at its caret.
    /// </summary>
    private void Press(VirtualKeyboardKey key)
    {
      bool finished = _model.Press(key);
      if (finished)
      {
        CloseRequested?.Invoke(this, EventArgs.Empty);
        return;
      }

      switch (key.Type)
      {
        case VirtualKeyType.Text:
          InsertAtCaret(key.Insert);
          break;
        case VirtualKeyType.Space:
          InsertAtCaret(" ");
          break;
        case VirtualKeyType.Backspace:
          BackspaceAtCaret();
          break;
      }
    }

    private void InsertAtCaret(string text)
    {
      if (_editor == null || text.Length == 0)
        return;
      // Assigning SelectedText replaces whatever is selected and leaves the caret behind the new text -
      // the one write that keeps the caret of a moving edit where the viewer expects it.
      _editor.SelectedText = text;
    }

    private void BackspaceAtCaret()
    {
      if (_editor == null)
        return;
      if (_editor.SelectionStart != _editor.SelectionEnd)
      {
        _editor.SelectedText = "";
        return;
      }
      int caret = _editor.CaretIndex;
      if (caret > 0)
      {
        _editor.SelectionStart = caret - 1;
        _editor.SelectionEnd = caret;
        _editor.SelectedText = "";
      }
    }

    private void ReflectState()
    {
      bool shift = _model.Shift;
      bool caps = _model.Caps;
      bool altGr = _model.AltGr;

      // The reference's visibility expressions, one grid only.
      _grids[0].IsVisible = !shift && !caps && !altGr;
      _grids[1].IsVisible = (shift || caps) && !altGr;
      _grids[2].IsVisible = !shift && !caps && altGr;
      _grids[3].IsVisible = (shift || caps) && altGr;

      _syncing = true;
      shiftButton.IsChecked = shift;
      capsButton.IsChecked = caps;
      altGrButton.IsChecked = altGr;
      _syncing = false;

      editText.Text = DisplayText();
    }

    /// <summary>
    /// What stands above the keys: the field's text, or its asterisks when the field hides its characters -
    /// the reference's <c>VisibleText</c> over a <c>PasswordText</c> field.
    /// </summary>
    private string DisplayText()
    {
      if (_editor == null)
        return _model.Text;
      string text = _editor.Text ?? "";
      bool masked = _editor.PasswordChar != '\0';
      return masked ? new string('*', text.Length) : text;
    }

    private async void OnCopy(object? sender, RoutedEventArgs e)
    {
      IClipboard? clipboard = GetClipboard();
      if (clipboard == null)
        return;
      try
      {
        await clipboard.SetTextAsync(_editor?.Text ?? "");
      }
      catch (Exception)
      {
        // A clipboard that refuses is a clipboard that is not there; the reference's copy failed too then.
      }
    }

    private async void OnPaste(object? sender, RoutedEventArgs e)
    {
      IClipboard? clipboard = GetClipboard();
      if (clipboard == null)
        return;
      try
      {
        string? text = await clipboard.TryGetTextAsync();
        if (!string.IsNullOrEmpty(text))
          InsertAtCaret(text!);
      }
      catch (Exception)
      {
        // As above: a refusing clipboard behaves like no clipboard.
      }
    }

    private IClipboard? GetClipboard()
    {
      TopLevel? top = TopLevel.GetTopLevel(this);
      return top?.Clipboard;
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs args)
    {
      base.OnAttachedToVisualTree(args);

      // The line above the keys shows what the field holds, so it has to follow the FIELD.  A press asks the
      // model first, and the model redraws the line before the character has reached the field - the line then
      // stood one press behind, and typing with a real keyboard did not move it at all.  Reading the field on
      // its own change is the answer: whatever the field holds is what stands over the keys, at once.
      if (_editor != null)
      {
        _followsField = delegate (object? sender, Avalonia.AvaloniaPropertyChangedEventArgs changed)
        {
          if (changed.Property == TextBox.TextProperty)
            editText.Text = DisplayText();
        };
        _editor.PropertyChanged += _followsField;
      }

      // Escape or back ends the edit wherever the focus inside the keyboard happens to stand - and, being a
      // tunnel handler on the window, before the shell hears it as a request to leave the screen.
      //
      // Asked of the top level rather than of args.Root: Avalonia hands the root VISUAL there now, and the layer
      // the keys hang in is not that visual - reading only args.Root left the keys without their handler, so the
      // back key fell through to the screen and threw the viewer out of it instead of putting the keys away.
      _keyedWindow = TopLevel.GetTopLevel(this) as Window ?? args.Root as Window;
      if (_keyedWindow != null)
      {
        _windowKeys = delegate (object? sender, Avalonia.Input.KeyEventArgs keys)
        {
          if (MediaPortal.Frontend.Settings.KeyBindings.Current.Is(
                  MediaPortal.Frontend.Settings.KeyAction.Back, keys.Key))
          {
            keys.Handled = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
          }
        };
        _keyedWindow.AddHandler(KeyDownEvent, _windowKeys, RoutingStrategies.Tunnel);
      }

      // The reference's Show() ends in TrySetFocus(true): the keyboard takes the focus, and the viewer is
      // among the keys from the first arrow press on.
      if (shiftButton.Focusable)
      {
        Avalonia.Threading.Dispatcher.UIThread.Post(delegate
        {
          Button? first = FirstUsableKey();
          first?.Focus();
        }, Avalonia.Threading.DispatcherPriority.Loaded);
      }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs args)
    {
      base.OnDetachedFromVisualTree(args);
      if (_editor != null && _followsField != null)
      {
        _editor.PropertyChanged -= _followsField;
        _followsField = null;
      }
      if (_keyedWindow != null && _windowKeys != null)
      {
        _keyedWindow.RemoveHandler(KeyDownEvent, _windowKeys);
        _windowKeys = null;
        _keyedWindow = null;
      }
    }

    private Button? FirstUsableKey()
    {
      foreach (Control child in gridArea.Children)
      {
        if (child is not Grid grid || !grid.IsVisible)
          continue;
        foreach (Control row in grid.Children)
        {
          if (row is not Grid rowGrid)
            continue;
          foreach (Control cell in rowGrid.Children)
          {
            if (cell is Button button && button.Focusable)
              return button;
          }
        }
      }
      return null;
    }
  }
}
