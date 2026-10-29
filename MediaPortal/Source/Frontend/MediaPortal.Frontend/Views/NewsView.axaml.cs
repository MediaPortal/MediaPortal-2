/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the license, or
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
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The news screen: the papers as squares, their headlines as a grid of tiles or a list of rows, and the
  /// story itself to read - the mockup's arrangement, standing in the library screen's room, walked the way
  /// the library screen is walked.
  /// </summary>
  /// <remarks>
  /// What only a screen can add, beyond what the mockup drew:
  /// <para>
  /// <b>The feeds are read when it opens</b> rather than when somebody presses a button - a screen called
  /// "News" that shows nothing until asked is a screen that says the news is over.
  /// </para>
  /// <para>
  /// <b>Nothing scrolls by bar and nothing fades unconditionally.</b> Every column wears the shelf's edge
  /// rule: an end fades exactly as deeply as that end has content out of the picture, and an end that has
  /// lost nothing shows everything. The wheel glides instead of jumping, which the mockup's smooth scrolling
  /// asked for and which Avalonia 12 does not give a ScrollViewer on its own.
  /// </para>
  /// <para>
  /// And the ways in that no pointer needs: the papers, the headlines and the reading column walk as one
  /// ring, Enter steps from a paper onto its headlines, and the screen's own menu - the four commands the
  /// mockup's rail carried - stands in the two hundred points off the screen's left edge exactly as the
  /// library's menu does, sliding into view when the caret asks for it from the leading edge of the papers.
  /// </para>
  /// </remarks>
  public partial class NewsView : UserControl, IWorkflowScreen
  {
    private readonly NewsViewModel _news;
    private readonly WorkflowManager? _workflow;
    private readonly Func<string, WorkflowFrame>? _screenFactory;

    /// <summary>Which shape the headlines column wears. The menu's second row turns it over.</summary>
    private bool _gridView = true;

    /// <summary>Whether the room is standing open for the menu right now.</summary>
    private bool _menuShown;

    /// <summary>The window this screen is standing in, for as long as it stands there (see the header stow).</summary>
    private ShellWindow? _shell;

    /// <summary>What the mockup's tile carries on each side - the space the two tiles keep between them.
    /// Five a side, so two neighbours stand ten apart, the very gap the feed squares keep below them.</summary>
    private const double TILE_EDGE_MARGIN = 5;

    /// <summary>
    /// The width a headline tile of the grid stands at, the mockup's own. The column no longer splits itself
    /// into two fixed halves: it asks how many tiles of THIS width fit across it, stands that many columns,
    /// and shares any leftover width out so the row fills the column edge to edge - the home wall's law, where
    /// a tile is as wide as a tile rather than as wide as a fraction of the window. A wide screen therefore
    /// carries three or four tiles side by side; a narrow one carries one.
    /// </summary>
    private const double TILE_PREFERRED = 360;

    /// <summary>
    /// What each cell keeps back from its exact share of the column. The panel that lays the tiles counts the
    /// columns by dividing the column through a measured cell, and a cell of EXACTLY the column's share falls
    /// under that division by a hair of rounding - four columns became three in front of the viewer and the
    /// row stood centred with a hand of empty glass on either side. Ten points off each cell covers that hair
    /// (and the bar a scroller reserves), which is the same allowance the two-column law carried.
    /// </summary>
    private const double TILE_ALLOWANCE = 10;

    /// <summary>
    /// How wide one headline tile of the grid stands, margins and all taken off. The panel that lays the tiles
    /// asks ONE tile for its width and derives the columns from the column's width, so the screen has to tell
    /// the tiles how wide they are rather than let a stretched tile answer "as wide as my words happen to be".
    /// See <see cref="TILE_PREFERRED"/> for how many of them the column stands.
    /// </summary>
    public static readonly StyledProperty<double> GridTileWidthProperty =
        AvaloniaProperty.Register<NewsView, double>(nameof(GridTileWidth), TILE_PREFERRED);

    /// <summary>See <see cref="GridTileWidthProperty"/>.</summary>
    public double GridTileWidth
    {
      get => GetValue(GridTileWidthProperty);
      set => SetValue(GridTileWidthProperty, value);
    }

    /// <summary>
    /// For the XAML loader and the designer: the screen a front end with nowhere to read from shows.
    /// </summary>
    public NewsView()
        : this(new NewsViewModel())
    {
    }

    /// <param name="news">What to show: the feeds, their headlines, the story of the one that is picked.</param>
    /// <param name="workflow">
    /// Where the rail's setup tile pushes its screen, or null for a screen that cannot leave its place - the
    /// designer's and any caller that has no workflow to walk.
    /// </param>
    /// <param name="screenFactory">
    /// The builder of named screens, the same one the home's tiles ask through; without it the setup tile
    /// does not sit on the rail at all, because a button that opens nothing is worse than no button.
    /// </param>
    public NewsView(NewsViewModel news, WorkflowManager? workflow = null,
        Func<string, WorkflowFrame>? screenFactory = null)
    {
      DataContext = news;
      _news = news;
      _workflow = workflow;
      _screenFactory = screenFactory;
      InitializeComponent();

      // A row of the menu that a pointer rests on and leaves is a row nobody pressed; a row the pointer
      // pressed once is the command. So the menu acts on selection only from a pointer, and the keyboard
      // acts on Enter - the same split the library rail answers with.
      newsMenuList.AddHandler(PointerPressedEvent,
          delegate (object? sender, PointerPressedEventArgs args)
          {
            if (args.GetCurrentPoint(newsMenuList).Properties.IsLeftButtonPressed
                && newsMenuList.SelectedItem is NewsMenuEntry pressed)
            {
              args.Handled = true;
              RunMenuEntry(pressed);
            }
          },
          RoutingStrategies.Tunnel);

      // The columns are NOT a Dpad ring. The ring answers every direction that a column's own list lets slip
      // at its ends - so an UP over the first headline teleported the caret into another column, and a viewer
      // who reached the bottom of one list woke up in the reading column. The columns answer their own UP and
      // DOWN always, and the hand-written answers below own Left, Right and Enter: menu, papers, headlines,
      // story - each door named where it leads.

      // The interval clock ticks only while the news is actually in the window, and the scrollers are wired
      // once it is: their ScrollVisuals only exist once the lists have been measured.
      AttachedToVisualTree += (_, _) =>
      {
        _news.ScreenOpened();
        WireScrollers();
        // The mockup's news room has no head line over it - no word, no marks, no day, no clock. The screen
        // asks the window to put that line away for the length of its visit and to bring it back on the way
        // out, the same way the photograph screen hands the television back.
        (_shell = TopLevel.GetTopLevel(this) as ShellWindow)?.SetChromeHeaderStowed(true);
      };
      DetachedFromVisualTree += (_, _) =>
      {
        _shell?.SetChromeHeaderStowed(false);
        _shell = null;
        _news.ScreenClosed();
      };

      // A double-click on a paper means the Enter that walks to its headlines; a double-click on a headline
      // fills the reading column, which is what standing on it already did.
      MouseRemote.Attach(this, feedsList, headlinesList);

      // The column follows its picked row by the shelf's law (see GlideColumnToStoodRow): the pick moves,
      // the column asks where the stood row now sits and - if it left the band - writes the offset itself,
      // which the scroller's transition travels over four hundred milliseconds.
      feedsList.SelectionChanged += delegate { GlideColumnToStoodRow(feedsList); };
      headlinesList.SelectionChanged += delegate { GlideColumnToStoodRow(headlinesList); };

      // Which column the keyboard last stood in - remembered so the shell's net (ShellWindow's answer to a
      // window standing on no focus at all) returns the keyboard to where the viewer was, not to the first
      // focusable control in tree order, which on this screen is the menu rail hidden behind its left edge.
      // The same moment dresses the row: exactly one row in the whole screen wears the caret's frame, and the
      // view puts that mark on itself rather than trusting a pseudo-class - measured live, the frame simply
      // stayed away from the papers while the accessibility tree swore the row held the keyboard.
      foreach (Control walked in new Control[] { feedsList, headlinesList, storyScroller })
      {
        Control column = walked;
        column.AddHandler(GotFocusEvent,
            delegate (object? s, FocusChangedEventArgs e)
            {
              _caretColumn = column;
              SweepCaretMarks();
              DressCaretMark();
            },
            RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);

        // While the rail stands open it owns the one frame of the screen; the columns lay their marks down.
        newsMenuList.AddHandler(GotFocusEvent,
            delegate { SweepCaretMarks(); },
            RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);

        // A recycled row re-wears its container: the mark must not ride along onto a stranger's tile. The
        // mark belongs to the keyboard, so after a scroll the single marked row is checked against the row
        // that really holds the keyboard - asked cheaply, because a walk that swept the whole tree on every
        // frame of a glide made the glide itself stutter.
        column.AddHandler(ScrollViewer.ScrollChangedEvent,
            delegate { DressCaretMark(); },
            RoutingStrategies.Bubble, handledEventsToo: true);
      }

      // The tile keeps the mockup's width and the column decides how many tiles stand beside one another -
      // the same law the home wall lays its posters by. Whatever width the whole columns leave over is
      // shared between them, so the tiles fill the column from edge to edge at every window width.
      headlinesList.SizeChanged += (_, args) =>
      {
        if (args.NewSize.Width <= 0)
          return;

        double pitch = TILE_PREFERRED + 2 * TILE_EDGE_MARGIN + TILE_ALLOWANCE;
        int columns = Math.Max(1, (int)(args.NewSize.Width / pitch));
        GridTileWidth = args.NewSize.Width / columns - 2 * TILE_EDGE_MARGIN - TILE_ALLOWANCE;
      };

      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            // A bare list that holds the keyboard - dropped there by a click on its empty ground or by the
            // window handing its keys back - answers an arrow key by moving the SELECTION alone: the marks
            // slide and no row ever wears the frame, so the viewer walks blind. The arrow is therefore
            // preceded by the step onto the row; the list then walks from a caret anybody can see.
            if (args.Key == Key.Up || args.Key == Key.Down || args.Key == Key.Left || args.Key == Key.Right)
            {
              Control? standing = TopLevel.GetTopLevel(this)?.FocusManager.GetFocusedElement() as Control;
              if (ReferenceEquals(standing, feedsList))
                StandOnItsRow(feedsList);
              else if (ReferenceEquals(standing, headlinesList))
                StandOnItsRow(headlinesList);
            }

            if (newsMenuList.IsKeyboardFocusWithin)
            {
              // The menu has its own answers while it is open: Enter runs the row the caret stands on,
              // Right walks the caret back onto the papers and slides the menu away, and Left - which has
              // nothing to the left of a rail - is swallowed rather than handed to the ring, which would
              // otherwise pull the caret off the menu onto a column and leave the rail standing open with
              // nobody on it.
              if (args.Key == Key.Enter || args.Key == Key.Return)
              {
                if (newsMenuList.SelectedItem is NewsMenuEntry chosen)
                {
                  args.Handled = true;
                  RunMenuEntry(chosen);
                }
              }
              else if (args.Key == Key.Right)
              {
                args.Handled = true;
                HideNewsMenu();
                StandCaretOn(feedsList);
              }
              else if (args.Key == Key.Left)
              {
                args.Handled = true;
              }
              else if (args.Key == Key.Escape)
              {
                // The reference's own answer: the escape of a whole screen first puts away a menu that
                // stands open on it (master_mediamenu.xaml:402); only an already closed menu lets the
                // escape leave the screen.
                args.Handled = true;
                HideNewsMenu();
                feedsList.Focus();
                StandCaretOn(feedsList);
              }
              else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.MediaMenu, args.Key))
              {
                args.Handled = true;
                HideNewsMenu();
                StandCaretOn(feedsList);
              }

              return;
            }

            if (Settings.KeyBindings.Current.Is(Settings.KeyAction.MediaMenu, args.Key))
            {
              // The reference's other half: the remote's info key (Apps, or the letter I) toggles the
              // screen's menu from wherever the caret stands (master_mediamenu.xaml:401). On the library
              // shelf this key asks the title's dialog; the news screen has no title dialog, so here the
              // key keeps the reference's own answer.
              args.Handled = true;
              if (_menuShown)
              {
                HideNewsMenu();
                StandCaretOn(feedsList);
              }
              else
              {
                ShowNewsMenu();
              }
            }
            else if (!_menuShown && (args.Key == Key.Enter || args.Key == Key.Return))
            {
              // Enter on a paper is the wish to read it, which is the headlines column across - and Enter on
              // a headline opens its story in the reading column, which standing on it already did; the key
              // only carries the caret there, the same door the reference's shelf gives every poster.
              if (feedsList.IsKeyboardFocusWithin)
              {
                StandCaretOn(headlinesList);
                if (headlinesList.IsKeyboardFocusWithin)
                  args.Handled = true;
              }
              else if (headlinesList.IsKeyboardFocusWithin)
              {
                StandCaretOn(storyScroller);
                if (storyScroller.IsKeyboardFocusWithin)
                  args.Handled = true;
              }
            }
            else if (args.Key == Key.Left && !_menuShown && feedsList.IsKeyboardFocusWithin)
            {
              // The papers are one column, so every paper touches their leading edge, and Left there is the
              // wish to see the screen's own commands - the same door the library's list gives every one of
              // its rows (MediaBrowserView: "in a list, whose rows are one column wide, every row touches it").
              args.Handled = true;
              ShowNewsMenu();
            }
            else if (!_menuShown && (args.Key == Key.Right || args.Key == Key.Left))
            {
              if (headlinesList.IsKeyboardFocusWithin)
              {
                // The headlines are a GRID of two tile columns, and inside a grid every arrow belongs to the
                // grid: Right steps to the tile beside this one and Left back, the same walk the shelf's wall
                // gives its posters. A column door only opens at a true edge of the grid - Left on the left
                // column's tile walks out onto the papers, Right on the right column's tile onto the story -
                // and in the list shape, whose rows are one column wide, every row touches both edges.
                int standing = headlinesList.SelectedIndex;
                if (standing < 0)
                  return; // the bare column's own first arrow picks a row; nobody else may have it
                VirtualizingTilePanel? grid = headlinesList.GetVisualDescendants()
                    .OfType<VirtualizingTilePanel>().FirstOrDefault();
                int columns = grid?.Columns ?? 1;
                int column = standing % columns;
                bool touches_edge = args.Key == Key.Left ? column == 0 : column == columns - 1;
                if (!touches_edge)
                  return;
                Control out_of_grid = args.Key == Key.Left ? (Control)feedsList : storyScroller;
                StandCaretOn(out_of_grid);
                if (out_of_grid.IsKeyboardFocusWithin)
                  args.Handled = true;
                return;
              }

              Control? neighbour = args.Key == Key.Right
                  ? (feedsList.IsKeyboardFocusWithin ? headlinesList
                      : storyScroller.IsKeyboardFocusWithin ? (Control?) feedsList
                      : (Control?) null)
                  : (storyScroller.IsKeyboardFocusWithin ? (Control?) headlinesList
                      : (Control?) null);
              if (neighbour != null)
              {
                StandCaretOn(neighbour);
                if (neighbour.IsKeyboardFocusWithin)
                  args.Handled = true;
              }
            }
          },
          RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Hands the caret to the row a column has picked rather than to the bare column. A gallery whose caret
    /// stands on the control itself answers the first arrow key twice - measured live as every Down stepping
    /// exactly two headlines - while a caret that stands ON a row walks one row at a time, which is what the
    /// library's rail and shelf have always made of it. Rows only exist as containers where the panel could
    /// be drawing, so the row is asked for by index and the bare column stays the fallback for the moment
    /// before any row exists.
    /// </summary>
    private void StandCaretOn(Control column)
    {
      _caretColumn = column;
      StandCaretOn(column, CARET_STEP_TRIES);
    }

    private const int CARET_STEP_TRIES = 6;

    /// <summary>
    /// The column the caret was last walked into. The shell's net asks where this screen's keys start when it
    /// notices the window standing on none - after a reload pulled the focused row out from under the keyboard,
    /// for example - and the answer is the column the viewer was last standing in, on the row it had picked.
    /// Without that answer the net would take the FIRST focusable control of the room, which is the menu rail
    /// standing behind the screen's left edge: the keyboard would vanish into an invisible list and every key
    /// the viewer pressed from then on would walk nothing they could see.
    /// </summary>
    private Control? _caretColumn;

    public Control? KeysStartAt()
    {
      Control column = _caretColumn ?? feedsList;
      if (column is ListBox list && list.SelectedIndex >= 0
          && list.ContainerFromIndex(list.SelectedIndex) is Control row)
        return row;
      return column;
    }

    /// <summary>
    /// The one row that currently wears the caret's frame, if any. Tracking the marked row by name rather
    /// than sweeping the realized rows of both columns costs nothing on a scroll frame - and a sweep that
    /// ran on every frame re-dressed the row under the caret every frame, which the viewer felt as the
    /// glide stuttering into jumps.
    /// </summary>
    private ListBoxItem? _caretMarked;

    /// <summary>
    /// Takes the caret's frame off the row that wore it. One row in the whole screen wears it, so before
    /// anybody puts it on, the wearer takes it off.
    /// </summary>
    private void SweepCaretMarks()
    {
      if (_caretMarked != null)
      {
        _caretMarked.Classes.Remove("news-caret");
        _caretMarked = null;
      }
    }

    /// <summary>
    /// Puts the caret's frame on the row that actually holds the keyboard, and takes it off any other.
    /// A keyboard standing on no row - on the bare column, on the rail, anywhere else - wears no frame.
    /// </summary>
    private void DressCaretMark()
    {
      ListBoxItem? standing = TopLevel.GetTopLevel(this)?.FocusManager.GetFocusedElement() as ListBoxItem;
      if (standing != null
          && (ReferenceEquals(standing.Parent, feedsList) || ReferenceEquals(standing.Parent, headlinesList)))
      {
        if (!ReferenceEquals(standing, _caretMarked))
        {
          SweepCaretMarks();
          _caretMarked = standing;
          standing.Classes.Add("news-caret");
        }
      }
      else
      {
        SweepCaretMarks();
      }
    }

    private static void StandCaretOn(Control column, int tries)
    {
      if (StandOnItsRow(column))
        return;

      // The move can outrun the column: a caret walked into a column whose items are still arriving - the
      // papers load over the network - finds no rows to stand on, and the bare column would answer the first
      // arrow key with a page rather than a row. Measured live, the bare list even kept the keyboard after
      // the rows arrived: the arrows moved the SELECTION while no row ever wore the frame, which is what the
      // viewer called "der Fokus kann nicht von der Feed zur News-Liste wandern". The caret therefore asks
      // again every layout pass - a bounded few times, and only while the column has kept the keyboard.
      column.Focus();
      if (tries <= 0 || column is not ListBox list)
        return;
      Dispatcher.UIThread.Post(
          delegate
          {
            if (column.IsKeyboardFocusWithin)
              StandCaretOn(column, tries - 1);
          },
          DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Whether the caret stands ON a row of the column now: the row the column has picked, if its container
    /// could be drawn and takes the focus. A column without a picked row picks its first, so entering a
    /// column always ends on a row a key can walk from.
    /// </summary>
    private static bool StandOnItsRow(Control column)
    {
      if (column is ListBox list)
      {
        if (list.SelectedIndex < 0 && list.Items.Count > 0)
          list.SelectedIndex = 0;
        return list.SelectedIndex >= 0
            && list.ContainerFromIndex(list.SelectedIndex) is Control row
            && row.Focus();
      }
      return false;
    }

    /// <summary>
    /// Runs every time the workflow shows this screen: the first showing loads, every showing takes
    /// the focus.  See IWorkflowScreen.
    /// </summary>
    public void OnScreenActivated()
    {
      // The same command the button-less screen would have; opening and pressing cannot drift apart.
      _news.RefreshCommand.Execute(null);

      // The papers take the focus so the first Enter of the visit means "read this paper" rather than
      // nothing at all; see HomeWindow for the general reason.
      feedsList.Focus();

      // The read may have finished before anybody looked: when it has, the caret moves onto the paper it
      // picked, which is the row the first arrow key of the visit walks from.
      Dispatcher.UIThread.Post(delegate
      {
        if (feedsList.IsKeyboardFocusWithin)
          StandCaretOn(feedsList);
      }, DispatcherPriority.Input);
    }

    /// <summary>
    /// Runs once, when the frame leaves the workflow for good.  See IWorkflowScreen.
    /// </summary>
    public void OnScreenClosed()
    {
      _news.Dispose();
    }

    /// <summary>
    /// Builds the rows this screen has to offer and slides the room to make room for them - the menu does
    /// not appear, the room makes room for it, which is the whole of the reference's opening animation. The
    /// rows are built anew every showing, because the shape the headlines column wears may have turned
    /// between two showings and the tile has to name the OTHER shape, the one a press would bring.
    /// </summary>
    private void ShowNewsMenu(int caretRow = 0)
    {
      List<NewsMenuEntry> entries = new List<NewsMenuEntry>
      {
        // The white bar of the row that is the screen's answer already: the papers are the column the news
        // screen opens on, as the layout row is the library's answer to "which shape".
        new NewsMenuEntry("library_books", "Feeds", true, NewsMenuCommand.ShowFeeds),
        new NewsMenuEntry(_gridView ? "view_list" : "grid_view", _gridView ? "List" : "Grid", false,
            NewsMenuCommand.ToggleView),
      };
      if (_workflow != null && _screenFactory != null)
        entries.Add(new NewsMenuEntry("setup", "Setup", false, NewsMenuCommand.OpenSetup));
      entries.Add(new NewsMenuEntry("refresh", "Refresh", false, NewsMenuCommand.Refresh));

      newsMenuList.ItemsSource = entries;
      newsMenuList.SelectedIndex = Math.Clamp(caretRow, 0, entries.Count - 1);
      _menuShown = true;
      ((TranslateTransform)room.RenderTransform).X = 200;
      StandCaretOn(newsMenuList);
    }

    /// <summary>Slides the menu back off the edge of the screen, where it stands until the next Left.</summary>
    private void HideNewsMenu()
    {
      _menuShown = false;
      ((TranslateTransform)room.RenderTransform).X = 0;
      // A closed rail must never keep the keyboard. A row of it can still hold the focus when the rail left
      // by a side door - a click elsewhere, a rebuilt row - and a focus inside a hidden menu is a caret
      // behind a wall: the viewer sees no mark anywhere, and every arrow walks an invisible list.
      // Measured live as the complaint "die Feeds haben keinen Fokusrahmen": the arrows moved, no column
      // answered. Closing the rail therefore carries the caret onto the papers it came from.
      if (newsMenuList.IsKeyboardFocusWithin)
        StandCaretOn(feedsList);
    }

    /// <summary>
    /// Runs the command of the row that was pressed. Only the view turn keeps the menu open - the same
    /// answer the library gives while its wall changes shape under an open menu - and its row says the new
    /// offer right away. Everything else leaves the caret where the command put it: on the papers, or on
    /// the settings screen the room walked to.
    /// </summary>
    private void RunMenuEntry(NewsMenuEntry entry)
    {
      switch (entry.Command)
      {
        case NewsMenuCommand.ToggleView:
          ToggleView();
          ShowNewsMenu(1);
          return;

        case NewsMenuCommand.OpenSetup:
          HideNewsMenu();
          WorkflowFrame? frame = _screenFactory?.Invoke("settings");
          if (frame != null)
            _workflow?.Push(frame);
          return;

        case NewsMenuCommand.Refresh:
          HideNewsMenu();
          _news.RefreshCommand.Execute(null);
          StandCaretOn(feedsList);
          return;

        default:
          HideNewsMenu();
          StandCaretOn(feedsList);
          return;
      }
    }

    /// <summary>
    /// The headlines column turns from the grid of tiles to the list of rows and back. The list keeps its
    /// selection through the turn - the same model's rows are re-dressed, never re-picked - and the caret
    /// stays where the viewer was standing.
    /// </summary>
    private void ToggleView()
    {
      _gridView = !_gridView;
      headlinesList.Classes.Remove(_gridView ? "news-list" : "news-grid");
      headlinesList.Classes.Add(_gridView ? "news-grid" : "news-list");
      StandCaretOn(headlinesList);
    }

    /// <summary>
    /// Finds the three scrollers and gives each its edge fade and its gliding wheel. The scroller of a list
    /// lives inside the list's template and only exists once the list has been measured, so a missing answer
    /// is asked for again on the next pass for a few passes rather than given up on, and each column keeps
    /// exactly one pair - a screen that is shown twice wires nothing twice.
    /// </summary>
    private void WireScrollers()
    {
      Dispatcher.UIThread.Post(delegate
      {
        ScrollViewer? feeds = feedsList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        ScrollViewer? headlines = headlinesList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

        if (feeds != null)
        {
          _feedsScroller = feeds;
          _feedsFade ??= new ColumnFade(feeds);
        }
        if (headlines != null)
        {
          _headlinesScroller = headlines;
          _headlinesFade ??= new ColumnFade(headlines);
        }

        // The reading column is a scroller of its own, not a list's tenant, so it is wired where it stands.
        _storyFade ??= new ColumnFade(storyScroller);

        // The glide lives on the scroller as a local value, exactly as the shelf installs its own
        // (InstallShelfGlide): measured live, a column whose transition only rides the style still answered
        // the caret with a cut, because the platform's own bring-into-view writes the offset during a
        // layout pass, where a styled transition stays cold. A locally owned transition answers the same
        // writes the way the shelf answers them - with a glide.
        InstallColumnGlide(_feedsScroller);
        InstallColumnGlide(_headlinesScroller);
        InstallColumnGlide(storyScroller);

        if (feeds == null || headlines == null)
        {
          _wireTries++;
          if (_wireTries < 10)
            WireScrollers();
        }
      }, DispatcherPriority.Loaded);
    }

    private static void InstallColumnGlide(ScrollViewer? scroller)
    {
      if (scroller == null || scroller.Transitions?.Any(t => t is VectorTransition) == true)
        return;

      Transitions glide = new Transitions();
      glide.Add(new VectorTransition
      {
        Property = ScrollViewer.OffsetProperty,
        Duration = TimeSpan.FromMilliseconds(400)
      });
      scroller.Transitions = glide;
    }

    private ColumnFade? _feedsFade;
    private ColumnFade? _headlinesFade;
    private ColumnFade? _storyFade;
    private ScrollViewer? _feedsScroller;
    private ScrollViewer? _headlinesScroller;
    private int _wireTries;

    /// <summary>
    /// How far inside the window a stood row should be held: the grid shape's own ScrollMargin of four
    /// hundred points (MediaStyles.xaml), the same number the library wall glides by. A band this deep
    /// holds the stood row at the window's middle and keeps every other row inside the picture, which
    /// also leaves the platform's bring-into-view nothing to cut - the column's own glide is then the
    /// only hand that moves it.
    /// </summary>
    private const double COLUMN_BAND = 400.0;

    /// <summary>
    /// Stands the column so the picked row sits inside the band, by the shelf's law: a row already inside
    /// the band leaves the column still, a row taller than the band is stood at its middle, and the move
    /// is one local write to the offset - which is what the scroller's transition turns into a glide. The
    /// platform's bring-into-view alone is not enough: measured live it walks one row per key press with a
    /// cut, and a television viewer calls that a stutter.
    /// </summary>
    private void GlideColumnToStoodRow(ListBox list, int tries = 2)
    {
      ScrollViewer? scroller = ReferenceEquals(list, feedsList) ? _feedsScroller : _headlinesScroller;
      Control? stood = list.SelectedIndex >= 0 ? list.ContainerFromIndex(list.SelectedIndex) as Control : null;

      if (scroller == null || stood == null)
      {
        // A row that is not realized yet has no place to stand at, and the caret outruns the panel more
        // often than not - the same retry the shelf's glide asks for.
        if (tries > 0)
        {
          Dispatcher.UIThread.Post(delegate { GlideColumnToStoodRow(list, tries - 1); },
              DispatcherPriority.Loaded);
        }

        return;
      }

      Point? at = stood.TranslatePoint(new Point(), scroller);
      if (at == null)
        return;

      // Absolute, not relative: the offset is mid-travel while a glide runs, and a target built from a
      // moving start drifts with it - measured live as the column bouncing twice over one standing row.
      // The row's place inside the whole column is the only honest start for the arithmetic.
      double view = scroller.Viewport.Height;
      double extent = scroller.Extent.Height;
      double abs = at.Value.Y + scroller.Offset.Y;
      double row = stood.Bounds.Height;
      double low = Math.Min(COLUMN_BAND, view / 2);
      double high = Math.Max(low, Math.Max(view - COLUMN_BAND, view / 2));
      double wanted = scroller.Offset.Y;

      if (row > high - low)
        wanted = abs + row / 2 - (low + high) / 2;
      else if (abs - wanted < low)
        wanted = abs - low;
      else if (abs - wanted + row > high)
        wanted = abs + row - high;
      else
        return; // The row stands inside the band already: the column stays still, as the shelf's does.

      wanted = Math.Clamp(wanted, 0, Math.Max(0, extent - view));
      if (Math.Abs(wanted - scroller.Offset.Y) < 1.0)
        return;

      scroller.Offset = new Vector(scroller.Offset.X, wanted);
    }

    // The wheels used to be turned by a hand of the screen's own - a timer walking the offset towards a wish
    // a notch had moved. That hand is gone: the wheel and the drag travel on the scroller's own transition
    // (installed as a local value in WireScrollers, measured to be the difference between a glide and a
    // cut), and only the caret gets a hand of the screen's own, because the platform brings a row into view
    // by cutting. The fades below are the shelf's: an end fades only as far as its content stands out of
    // the picture.
    /// <summary>
    /// One column's edge fade: the shelf's rule, written once for a column that only runs downwards. The
    /// end of a column fades exactly as deeply as that end has rows out of the picture - nothing out is no
    /// fade at all and no mask is worn, one fade zone's worth out is the full fade - and the reading is
    /// taken off a ladder of eight steps, so a column travelling through its middle rewrites nothing.
    /// </summary>
    private sealed class ColumnFade
    {
      private readonly ScrollViewer _scroller;
      private LinearGradientBrush? _brush;
      private double _before = UNREAD;
      private double _after = UNREAD;

      public ColumnFade(ScrollViewer scroller)
      {
        _scroller = scroller;
        scroller.AddHandler(ScrollViewer.ScrollChangedEvent, delegate { Update(); });
        Update();
      }

      /// <summary>How far in from an end of the column its rows dissolve once a whole zone has gone out.</summary>
      private const double ZONE = 0.12;

      /// <summary>How finely the depth an end fades is read: eight steps between none and all.</summary>
      private const double STEPS = 8;

      /// <summary>A depth no end has been asked about yet, so that the first reading is always written.</summary>
      private const double UNREAD = -1;

      private void Update()
      {
        double view = _scroller.Viewport.Height;
        if (view <= 0)
          return;

        double extent = _scroller.Extent.Height;
        double offset = _scroller.Offset.Y;
        double zone = ZONE * view;
        double before = Depth(offset, zone);
        double after = Depth(extent - view - offset, zone);
        if (before == _before && after == _after)
          return;

        _before = before;
        _after = after;

        // A column that has left nothing out of the picture at either end wears no mask at all, which is both
        // the honest answer and the cheap one.
        if (before == 0 && after == 0)
        {
          _scroller.OpacityMask = null;
          return;
        }

        LinearGradientBrush fade = _brush ??= NewFade();
        fade.GradientStops[0].Color = Colour(before);
        fade.GradientStops[3].Color = Colour(after);
        _scroller.OpacityMask = fade;
      }

      private static double Depth(double hidden, double zone)
      {
        if (hidden <= 0 || zone <= 0)
          return 0;

        double gone = Math.Min(1.0, hidden / zone);
        return Math.Round(gone * STEPS) / STEPS;
      }

      /// <summary>A mask hides by its own transparency: nothing hidden is opaque, a zone hidden is clear.</summary>
      private static Color Colour(double depth)
        => Color.FromArgb((byte)Math.Round((1.0 - depth) * 255.0), 0xFF, 0xFF, 0xFF);

      private static LinearGradientBrush NewFade()
      {
        LinearGradientBrush fade = new LinearGradientBrush
        {
          StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
          EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative)
        };
        fade.GradientStops.Add(new GradientStop(Colors.White, 0));
        fade.GradientStops.Add(new GradientStop(Colors.White, ZONE));
        fade.GradientStops.Add(new GradientStop(Colors.White, 1.0 - ZONE));
        fade.GradientStops.Add(new GradientStop(Colors.White, 1));
        return fade;
      }
    }

  }

  /// <summary>
  /// One row of the news screen's own menu: the mark out of the skin's vectors, the name on the glass, the
  /// white bar of the screen's standing answer, and the command a press runs.
  /// </summary>
  public sealed class NewsMenuEntry
  {
    public NewsMenuEntry(string icon, string label, bool isCurrent, NewsMenuCommand command)
    {
      Icon = icon;
      Label = label;
      IsCurrent = isCurrent;
      Command = command;
    }

    /// <summary>Name of the glyph under MediaMenu/, worn the way the library rail wears its commands.</summary>
    public string Icon { get; }

    public string Label { get; }

    public bool IsCurrent { get; }

    internal NewsMenuCommand Command { get; }
  }

  /// <summary>What a row of the news menu does when it is pressed.</summary>
  public enum NewsMenuCommand
  {
    /// <summary>The caret walks back onto the papers.</summary>
    ShowFeeds,

    /// <summary>The headlines column turns from tiles to rows and back; the menu stays open.</summary>
    ToggleView,

    /// <summary>The settings, pushed as their own stop.</summary>
    OpenSetup,

    /// <summary>The papers are asked again, right now.</summary>
    Refresh,
  }
}