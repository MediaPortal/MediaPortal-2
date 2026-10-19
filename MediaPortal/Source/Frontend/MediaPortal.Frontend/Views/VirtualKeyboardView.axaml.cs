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
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
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
          Grid rowGrid = new Grid { Margin = new Avalonia.Thickness(0, r == 0 ? 0 : 4, 0, 0) };
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
              Content = key.Legend,
              FontSize = 20,
              MinHeight = 44,
              MinWidth = key.Type == VirtualKeyType.Disabled ? 0 : 30,
              Padding = new Avalonia.Thickness(2, 4),
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

      Grid? rows = null;
      foreach (Control child in gridArea.Children)
      {
        if (child is Grid grid && grid.IsVisible)
        {
          rows = grid;
          break;
        }
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
      }
      else
      {
        int toRow = fromRow + (key == Avalonia.Input.Key.Up ? -1 : +1);
        if (toRow < 0 || toRow >= rows.Children.Count || !(rows.Children[toRow] is Grid row))
          return;
        target = NearestKey(row, fromColumn, -1, true) ?? NearestKey(row, fromColumn, +1, true);
      }

      if (target != null && target.Focus())
        args.Handled = true;
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

      // Escape or back ends the edit wherever the focus inside the keyboard happens to stand - and, being a
      // tunnel handler on the window, before the shell hears it as a request to leave the screen.
      _keyedWindow = args.Root as Window;
      if (_keyedWindow != null)
      {
        _windowKeys = delegate (object? sender, Avalonia.Input.KeyEventArgs keys)
        {
          if (keys.Key == Avalonia.Input.Key.Escape || keys.Key == Avalonia.Input.Key.Back)
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
