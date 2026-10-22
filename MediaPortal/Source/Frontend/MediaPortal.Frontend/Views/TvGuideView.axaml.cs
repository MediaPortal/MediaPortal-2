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
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The programme guide as a screen.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The grid is built here rather than declared in the markup, for one reason: a programme has to stand in the
  /// column its own beginning falls in, out of ninety, and "column thirty-seven" is not something a data
  /// template can say. The reference built its grid the same way in code - its <c>EpgGrid</c> computes
  /// <c>colIndex</c> and <c>colSpan</c> for every programme (<c>TV/SlimTvClient/Controls/EpgGrid.cs:610-641</c>) -
  /// and it is the only way the hour ruler and the blocks under it can be guaranteed to agree at any width, since
  /// both are laid out of the same ninety star columns and neither of them knows a pixel.
  /// </para>
  /// <para>
  /// The grid is rebuilt when the model says it changed - a window moved, a bouquet was picked, an answer came in
  /// - and not on a timer. The one thing that moves by itself is the line at the moment it is now, so that line
  /// alone is moved every ten seconds, which is the interval the reference gave it
  /// (<c>EpgGrid.cs:702-737</c>).
  /// </para>
  /// </remarks>
  public partial class TvGuideView : UserControl
  {
    /// <summary>
    /// What the column of a channel's number is worth, which is what the reference allowed it
    /// (<c>Skin/Nereus/screens/SlimTvClient-fullguide.xaml:22-30</c>).
    /// </summary>
    private const double NUMBER_WIDTH = 90;

    /// <summary>What the column of a channel's name is worth, from the same sum in the reference.</summary>
    private const double NAME_WIDTH = 200;

    /// <summary>How tall the strip of hours along the top is.</summary>
    private const double RULER_HEIGHT = 20;

    /// <summary>
    /// What a row is worth before the screen has been measured, which is only ever in a test or the designer - a
    /// guide on a real screen always works its rows out of the room it has been given.
    /// </summary>
    private const double FALLBACK_ROW_HEIGHT = 42;

    /// <summary>
    /// How wide the column of channels is, from the two switches the viewer left on. The reference's own sum was
    /// "ninety pixels for a number, a hundred and sixty for a logo, two hundred for a name", and this front end has
    /// no channel logos to reserve a hundred and sixty pixels for yet - a thing its settings still owe it.
    /// </summary>
    private double _headerWidth = NUMBER_WIDTH + NAME_WIDTH;

    /// <summary>
    /// How tall one channel's row is, out of the room this screen has and the number of rows the viewer asked to
    /// see at once. The reference divided its own height by the same setting (<c>EpgGrid.cs:428</c>:
    /// "ActualHeight / _numberOfRows"), so asking for six rows rather than eighteen makes the six taller rather
    /// than leaving six rows at the top of a screen nobody can read from the sofa.
    /// </summary>
    private double _rowHeight = FALLBACK_ROW_HEIGHT;

    /// <summary>The strip a channel's programmes are drawn on, behind the blocks.</summary>
    private static readonly IBrush TRACK = new SolidColorBrush(Color.Parse("#14FFFFFF"));

    /// <summary>The line at now. The reference's was white, fading out at one side with a gradient.</summary>
    private static readonly IBrush NOW = new SolidColorBrush(Color.Parse("#E6FFFFFF"));

    private readonly TvGuideViewModel? _model;
    private readonly WorkflowManager? _workflow;
    private readonly Dictionary<TvGuideBlock, Border> _boxes =
        new Dictionary<TvGuideBlock, Border>();

    private Border? _standingOut;
    private Border? _nowBar;
    private DispatcherTimer? _creep;

    /// <summary>For the XAML loader and the designer.</summary>
    public TvGuideView()
        : this(new TvGuideViewModel(TvMediaType.Television))
    {
    }

    /// <summary>
    /// Builds the guide over its model.
    /// </summary>
    /// <param name="workflow">
    /// The trail the guide stands on, which it needs for one thing only: "Choose program action..." is a dialog the
    /// guide itself raises over itself (<c>SlimTvGuideModelBase.ShowProgramActions</c> ended in
    /// <c>ShowDialog</c>), so the guide pushes it and pops it again. A guide built without a trail - as every test
    /// builds one - still asks its model the questions, it just has nowhere to put the dialog up.
    /// </param>
    public TvGuideView(TvGuideViewModel model, WorkflowManager? workflow = null)
    {
      _model = model;
      _workflow = workflow;
      DataContext = model;
      InitializeComponent();

      backDayButton.Click += delegate { model.ScrollBackwardDay(); };
      backButton.Click += delegate { model.ScrollBackward(); };
      nowButton.Click += delegate { model.ScrollToNow(); };
      forwardButton.Click += delegate { model.ScrollForward(); };
      forwardDayButton.Click += delegate { model.ScrollForwardDay(); };
      watchButton.Click += delegate { model.Watch(model.SelectedBlock); };

      // The picker would otherwise print a bouquet's whole record; its name is the only part anybody reads.
      groupPicker.ItemTemplate = new FuncDataTemplate<TvChannelGroup>(delegate (TvChannelGroup? group,
          INameScope? scope)
      {
        return new TextBlock { Text = group == null ? "" : group.Name, FontSize = 14 };
      });

      model.PropertyChanged += delegate (object? sender,
          System.ComponentModel.PropertyChangedEventArgs e)
      {
        switch (e.PropertyName)
        {
          case nameof(TvGuideViewModel.Rows):
          case nameof(TvGuideViewModel.Hours):
          case nameof(TvGuideViewModel.Start):
            Rebuild();
            break;
          case nameof(TvGuideViewModel.SelectedBlock):
            StandOut(BoxOf(model.SelectedBlock));
            break;
        }
      };

      // The two halves of the question a programme can be asked. Both are the model's, because what the answers are
      // depends on a question to the backend; both land back here, because putting a dialog on the trail is a
      // screen's work. The dialog is only ever put up while the model still has it open - a viewer who was quick
      // enough to walk off the guide while the entry was being read gets no dialog at all, which is right.
      model.ActionsRequested += delegate
      {
        if (model.ActionsOpen && model.ActionsAbout != null)
          _workflow?.Push(new WorkflowFrame("programactions", model.ActionsTitle, model));
      };
      model.ActionsClosed += delegate { _workflow?.Pop(); };

      _creep = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
      _creep.Tick += delegate { MoveNowLine(); };
      _creep.Start();

      Unloaded += delegate
      {
        if (_creep != null)
        {
          _creep.Stop();
          _creep = null;
        }
      };

      List<Control> ring = new List<Control>();
      ring.Add(backDayButton);
      ring.Add(backButton);
      ring.Add(nowButton);
      ring.Add(forwardButton);
      ring.Add(forwardDayButton);
      ring.Add(watchButton);
      ring.Add(groupPicker);
      ring.Add(gridScroller);
      DpadNavigation.Attach(this, ring);

      // The rows are as tall as the room they have divided by the number of rows the viewer asked for, so a window
      // that is resized - or a guide that is opened on a screen for the first time, whose room is not known until
      // it has been laid out - has to be measured and laid out again.
      gridScroller.SizeChanged += delegate
      {
        if (MeasureRowsAndHeader())
          Rebuild();
      };

      MeasureRowsAndHeader();
      Rebuild();
    }

    /// <summary>
    /// Works out what the column of channels and one row are worth, from the viewer's settings and the room this
    /// screen has been given. Returns whether anything about them changed, because the answer is only interesting
    /// to whoever is about to lay the grid out again.
    /// </summary>
    private bool MeasureRowsAndHeader()
    {
      TvGuideViewModel? model = _model;

      double header = (model == null || model.ShowChannelNumbers ? NUMBER_WIDTH : 0)
          + (model == null || model.ShowChannelNames ? NAME_WIDTH : 0);

      // The hours take their strip off the top of what the scroller shows, and the rows share the rest.
      double room = gridScroller.Bounds.Height - RULER_HEIGHT;
      int rows = model == null ? TvBackendSettings.DEFAULT_VISIBLE_ROWS : model.VisibleRows;
      double height = room > 40 ? Math.Floor(room / Math.Max(1, rows)) : FALLBACK_ROW_HEIGHT;
      height = Math.Min(160, Math.Max(20, height));

      if (Math.Abs(header - _headerWidth) < 0.5 && Math.Abs(height - _rowHeight) < 0.5)
        return false;

      _headerWidth = header;
      _rowHeight = height;
      return true;
    }

    /// <summary>Lays the whole grid out again: the ruler, one row per channel, and the line at now over all.</summary>
    private void Rebuild()
    {
      if (_model == null)
        return;

      // The settings may have moved since the last time this was drawn, and the screen may have been measured
      // since too; either way the grid is about to be built, so it is built out of what is true now.
      MeasureRowsAndHeader();

      gridHost.Children.Clear();
      _boxes.Clear();
      _standingOut = null;
      _nowBar = null;

      gridHost.Children.Add(BuildRuler());

      Grid canvas = new Grid();

      StackPanel rows = new StackPanel { Spacing = 0 };
      foreach (TvGuideRow row in _model.Rows)
        rows.Children.Add(BuildRow(row));
      canvas.Children.Add(rows);

      Control? line = BuildNowLine();
      if (line != null)
        canvas.Children.Add(line);

      gridHost.Children.Add(canvas);
      StandOut(BoxOf(_model.SelectedBlock));
    }

    /// <summary>
    /// The hours along the top, standing in the same columns as the blocks beneath them. A hour is written at
    /// the column it names, which is what lets a viewer read the time a bar stands at off the top of the screen.
    /// </summary>
    private Control BuildRuler()
    {
      Grid ruler = new Grid { Height = RULER_HEIGHT };
      ruler.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(_headerWidth)));
      for (int column = 0; column < TvGuideViewModel.COLUMNS; column++)
        ruler.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

      if (_model != null)
      {
        foreach (TvGuideHour hour in _model.Hours)
        {
          TextBlock label = new TextBlock
          {
            Text = hour.Text,
            FontSize = 13,
            Opacity = 0.55,
            Margin = new Thickness(2, 0, 0, 2)
          };
          Grid.SetColumn(label, hour.Column + 1);
          ruler.Children.Add(label);
        }
      }

      return ruler;
    }

    /// <summary>One channel: its number and name at the left, its programmes across ninety columns.</summary>
    private Control BuildRow(TvGuideRow row)
    {
      Grid line = new Grid { Height = _rowHeight };
      line.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(_headerWidth)));
      for (int column = 0; column < TvGuideViewModel.COLUMNS; column++)
        line.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

      StackPanel head = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        Spacing = 6,
        Margin = new Thickness(0, 0, 8, 0),
        VerticalAlignment = VerticalAlignment.Center
      };

      // Which of the two the viewer wants is a setting, and the reference measured its header by exactly the same
      // question (Skin/Nereus/screens/SlimTvClient-fullguide.xaml:22-30); what is not asked for is not drawn, so
      // the name alone gets the whole of a narrower column rather than being squeezed by something invisible.
      bool numbers = _model == null || _model.ShowChannelNumbers;
      bool names = _model == null || _model.ShowChannelNames;

      if (numbers)
      {
        head.Children.Add(new TextBlock
        {
          Text = row.Number,
          FontSize = 13,
          Opacity = 0.4,
          MinWidth = 26,
          TextAlignment = TextAlignment.Right
        });
      }
      if (names)
      {
        head.Children.Add(new TextBlock
        {
          Text = row.Name,
          FontSize = 14,
          TextTrimming = TextTrimming.CharacterEllipsis,
          VerticalAlignment = VerticalAlignment.Center
        });
      }
      Grid.SetColumn(head, 0);
      line.Children.Add(head);

      Border track = new Border
      {
        Background = TRACK,
        CornerRadius = new CornerRadius(3),
        ClipToBounds = true
      };

      Grid cells = new Grid();
      for (int column = 0; column < TvGuideViewModel.COLUMNS; column++)
        cells.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

      foreach (TvGuideBlock block in row.Blocks)
      {
        Border box = BuildBlock(block);
        _boxes[block] = box;
        cells.Children.Add(box);
      }

      track.Child = cells;
      Grid.SetColumn(track, 1);
      line.Children.Add(track);
      return line;
    }

    /// <summary>
    /// One programme's block, filled with its genre's colour and holding its title with the series line under it -
    /// which is what the reference's block held, the title at eighty per cent opacity, a twenty-five pixel
    /// recording mark, and the series only when the setting allowed it (<c>SlimTvStyles.xaml:1030-1090</c>).
    /// </summary>
    private Border BuildBlock(TvGuideBlock block)
    {
      StackPanel words = new StackPanel { Spacing = 0 };
      words.Children.Add(new TextBlock
      {
        // The hour is written into a running block and only into that one, because it is the block a viewer
        // looks at to ask how much of this is left, and the grid gives no other answer to that question.
        Text = block.IsRunning ? block.StartText + "  " + block.Title : block.Title,
        FontSize = 13,
        TextTrimming = TextTrimming.CharacterEllipsis
      });

      if (block.SeriesLine.Length > 0)
      {
        words.Children.Add(new TextBlock
        {
          Text = block.SeriesLine,
          FontSize = 11,
          Opacity = 0.75,
          TextTrimming = TextTrimming.CharacterEllipsis
        });
      }

      Panel inside = new Panel();
      inside.Children.Add(words);

      if (block.IsRecorded)
      {
        // The reference drew a small camera here (record_single.png, twenty-five pixels across); until a
        // television screen's pictures exist in this skin, a dot in the corner says the same thing.
        inside.Children.Add(new TextBlock
        {
          Text = "●",
          FontSize = 10,
          Opacity = 0.9,
          HorizontalAlignment = HorizontalAlignment.Right,
          VerticalAlignment = VerticalAlignment.Top,
          Margin = new Thickness(0, 1, 3, 0),
          IsHitTestVisible = false
        });
      }

      Border box = new Border
      {
        Background = Paint(block.Fill),
        CornerRadius = new CornerRadius(3),
        Margin = new Thickness(1, 2, 1, 2),
        Padding = new Thickness(5, 2, 5, 2),
        ClipToBounds = true,
        Child = inside,
        Cursor = new Cursor(StandardCursorType.Hand),
        // Which programme this box is, so that the screen can find a block's box again when the model says it is
        // the one being talked about - and so that a test can count what was actually drawn.
        Tag = block
      };

      if (block.IsRunning)
        box.BorderBrush = new SolidColorBrush(Color.Parse("#40FFFFFF"));

      Grid.SetColumn(box, block.Column);
      Grid.SetColumnSpan(box, Math.Max(1, block.ColumnSpan));

      box.PointerPressed += delegate
      {
        if (_model == null)
          return;
        _model.SelectedRow = RowOf(block);
        _model.Press(block);
      };

      return box;
    }

    /// <summary>Which row a block came from, which is what the details' channel line falls back to.</summary>
    private TvGuideRow? RowOf(TvGuideBlock block)
    {
      if (_model == null)
        return null;

      foreach (TvGuideRow row in _model.Rows)
      {
        foreach (TvGuideBlock candidate in row.Blocks)
        {
          if (ReferenceEquals(candidate, block))
            return row;
        }
      }
      return null;
    }

    /// <summary>The box a block was drawn into, or null when that block is not on the screen.</summary>
    private Border? BoxOf(TvGuideBlock? block)
    {
      if (block == null)
        return null;

      Border? box;
      return _boxes.TryGetValue(block, out box) ? box : null;
    }

    /// <summary>
    /// The line at now, drawn once over the whole grid rather than once per row so that it reads as one line, and
    /// hidden when the window holds no now - which is how a window over next Tuesday looks, and is what the
    /// reference did when its indicator fell outside the viewport.
    /// </summary>
    private Control? BuildNowLine()
    {
      if (_model == null || !_model.NowIsInside)
        return null;

      Grid overlay = new Grid
      {
        Margin = new Thickness(_headerWidth, 0, 0, 0),
        IsHitTestVisible = false
      };
      overlay.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
      for (int column = 0; column < TvGuideViewModel.COLUMNS; column++)
        overlay.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

      _nowBar = new Border { Background = NOW, Width = 2, HorizontalAlignment = HorizontalAlignment.Left };
      Grid.SetColumn(_nowBar, _model.NowColumn + 1);
      overlay.Children.Add(_nowBar);
      return overlay;
    }

    /// <summary>
    /// Moves the line at now, ten seconds at a time. Only the line moves: rebuilding the whole grid to advance a
    /// line would throw away the block somebody is standing on, every ten seconds, forever.
    /// </summary>
    private void MoveNowLine()
    {
      if (_model == null || _nowBar == null)
        return;

      int column = _model.NowColumn;
      if (column < 0)
      {
        _nowBar.IsVisible = false;
        return;
      }

      _nowBar.IsVisible = true;
      Grid.SetColumn(_nowBar, column + 1);
    }

    /// <summary>
    /// Puts the white frame on the block being talked about and takes it off whatever wore it before. The
    /// reference's focus visual was a three-pixel white frame with a three-pixel black shadow behind it
    /// (<c>SlimTvStyles.xaml:800-830</c>); the same statement at this screen's scale is two pixels of white.
    /// </summary>
    private void StandOut(Border? box)
    {
      if (_standingOut != null && !ReferenceEquals(_standingOut, box))
      {
        _standingOut.BorderThickness = new Thickness(0);
        _standingOut.BorderBrush = null;
      }

      _standingOut = box;
      if (box != null)
      {
        box.BorderBrush = Brushes.White;
        box.BorderThickness = new Thickness(2);
      }
    }

    private static IBrush Paint(string hex)
    {
      try
      {
        return new SolidColorBrush(Color.Parse(hex));
      }
      catch (FormatException)
      {
        // A colour the screen cannot read is a colour nobody chose; the evening still has to be drawn.
        return TRACK;
      }
    }
  }
}
