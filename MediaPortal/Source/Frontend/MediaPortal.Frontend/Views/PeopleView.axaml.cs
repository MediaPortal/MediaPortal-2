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

using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// A wall of people - the names a shelf was made by - worn like a media screen wears its shelf.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The screen brings nothing of its own to the look of a media screen and means that as a promise: the rail is
  /// <see cref="ShelfRail"/>, the styles and the menu plates are <c>ShelfChrome.axaml</c> and
  /// <c>ShelfChromeStyles.axaml</c>, the plate over a group of names is <see cref="ShelfFrameTemplate"/>, and the
  /// fade at the ends of the rail is the one <see cref="ShelfRail"/> puts on itself.  A viewer who learned on a
  /// shelf of films where "View" stands finds it standing there on a wall of names.
  /// </para>
  /// <para>
  /// What the screen does add is the walk: the arrow keys hand the caret from the wall to the rail and back, Enter
  /// on a person asks the application for that person's works, and the room slides the two hundred points that
  /// carry the rail into view - the same move the shelf makes, for the same reason that a rail which covered the
  /// wall it belongs to would be a rail hiding the thing it is about.
  /// </para>
  /// </remarks>
  public partial class PeopleView : UserControl, IWorkflowScreen, IShelfRailHost
  {
    /// <summary>How far the room stands off the rail, and how far the rail stands off the screen.</summary>
    private const double MENU_ROOM = 200;

    /// <summary>How long the room takes to travel, which is the shelf's own three hundred and fifty milliseconds.</summary>
    private static readonly TimeSpan SLIDE_TIME = TimeSpan.FromMilliseconds(350);

    /// <summary>The panel a wall of tiles is laid in: the same one the shelf's grid and covers lay their tiles in.</summary>
    private static readonly FuncTemplate<Panel?> TILE_PANEL =
        new FuncTemplate<Panel?>(() => new VirtualizingTilePanel());

    private readonly PeopleViewModel? _wall;
    private readonly WorkflowManager? _workflow;
    private readonly Func<PersonTile, Task<WorkflowFrame?>>? _works;

    private ShelfRail? _rail;
    private ShellWindow? _chromeShell;

    /// <summary>What the column of words was last told to be, so that a resize that changes nothing changes nothing.</summary>
    private double? _detailsColumnWidth;
    private bool _railWired;
    private bool _readOnce;
    private bool _menuUp;
    private DispatcherTimer? _hideRail;

    /// <summary>The list's own scroller, once it has been found: what the fade is laid over and what the glide
    /// is worn by.  The markup never names it, because a list builds it out of its template.</summary>
    private ScrollViewer? _wallScroller;
    private BandEndFade? _wallFade;

    /// <summary>For the XAML loader: the wall a front end with nothing to read shows.</summary>
    public PeopleView()
        : this(new PeopleViewModel(null, PeopleShelf.FilmActors), null, null)
    {
    }

    /// <param name="wall">One of the three walls: film actors, series actors or artists.</param>
    /// <param name="workflow">The workflow this screen stands in, for the frame a pressed person opens.</param>
    /// <param name="works">
    /// Builds the stop that shows one person's works, which only the application can do - it is the application
    /// that knows how a shelf of titles is opened and what its heading is.  Null on a screen with nowhere to go.
    /// </param>
    public PeopleView(PeopleViewModel wall, WorkflowManager? workflow,
        Func<PersonTile, Task<WorkflowFrame?>>? works)
    {
      DataContext = wall;
      _wall = wall;
      _workflow = workflow;
      _works = works;
      InitializeComponent();

      WearTheShape();
      DpadNavigation.Attach(this, new System.Collections.Generic.List<Control> { peopleList });

      // The rail is the shelf's rail, wearing the shelf's ground: it asks this screen whose menu it is, where the
      // caret is asked for and how its ground nods, and knows everything else for itself.
      _rail = new ShelfRail(mediaMenuList, sideMenu, false, this);

      // THE HEAD LINE OF THE WINDOW, asked for exactly as a shelf of titles asks for it.  The reference laid its
      // media masters in three bands - a hundred points of head, the client area, two hundred and sixty of floor -
      // and hung the picture over ALL THREE, so its clock and its screen name stood on the picture and the
      // picture went to the very edge of the television.  A header kept in a row of its own above the screen does
      // the opposite: a black bar lies over the head of the wall.  So this screen asks for the header to float
      // over its own row and to be put away while it stands here - its word is the word the viewer already read
      // on the tile they walked in through - and lays it back to rest on the way out, for a screen that plans its
      // space under a header row would have its top cut off otherwise.
      AttachedToVisualTree += delegate
      {
        _chromeShell = this.GetVisualAncestors().OfType<ShellWindow>().FirstOrDefault();
        _chromeShell?.SetChromeHeaderOverlay(true);
        _chromeShell?.SetChromeHeaderStowed(true);
        ApplyColumns();
      };
      DetachedFromVisualTree += delegate
      {
        _chromeShell?.SetChromeHeaderOverlay(false);
        _chromeShell?.SetChromeHeaderStowed(false);
        _chromeShell = null;
      };

      // The wall and its column of words divide the room between them, and the division is a number of points
      // rather than a proportion, so it is made again whenever the room changes size or the words go away.
      SizeChanged += delegate
      {
        ApplyColumns();
        FitWordsIntoThePanel();
      };

      peopleList.SelectionChanged += delegate
      {
        // A caret walking into the last quarter of the names that arrived is the moment to ask for more, which is
        // the only wall a screen has that a hand is on it: a wall read by the thousandth row cannot wait for a
        // "load more" plate nobody can see.
        if (_wall != null)
          _ = _wall.KeepReadingAsync();
      };

      // A face belongs to the tile that stands in it, and only this screen knows which tiles stand where the viewer
      // can see them - so the asking goes out from here, per prepared tile, exactly as a shelf of titles asks for
      // the posters of its rows (MediaBrowserView.ContainerPrepared/ScrollChanged → ScheduleRowPainting).  Asked
      // from the view model in one walk down the whole wall, the faces arrive one per round trip and the viewer
      // watches them pop into tiles that were already standing.
      peopleList.ContainerPrepared += delegate
      {
        ScheduleFacePainting();
      };

      peopleList.AddHandler(ScrollViewer.ScrollChangedEvent, delegate { ScheduleFacePainting(); });

      if (_wall != null)
        _wall.PropertyChanged += OnWallChanged;

      AddHandler(KeyDownEvent, delegate (object? sender, KeyEventArgs args)
      {
        if (args.Key == Key.Enter || args.Key == Key.Return)
        {
          if (peopleList.IsKeyboardFocusWithin && peopleList.SelectedItem is PersonTile person)
          {
            // A person is not opened, their works are: pressing a name is the question "which of these?", which
            // is a shelf cut to one person and therefore a shelf like any other.
            args.Handled = true;
            _ = OpenWorksAsync(person);
          }
          return;
        }

        if (args.Key == Key.Escape && _menuUp)
        {
          // Back from a page of the rail is the page above it, and back from the commands is the rail going away -
          // which is what the rail's own back does when the viewer climbed down, so only the top page is asked
          // about here.
          args.Handled = true;
          ShowRail(false);
          return;
        }

        if (MediaPortal.Frontend.Settings.KeyBindings.Current.Is(
                MediaPortal.Frontend.Settings.KeyAction.Details, args))
        {
          // The same letter the shelf of titles answers, asking the same question the rail's view page asks: whether
          // the words beside the wall stand.  The wall itself is untouched by the answer - it is a wall either way.
          args.Handled = true;
          _wall?.ToggleDetailsCommand.Execute(null);
          return;
        }

        if (args.Key == Key.Left && !_menuUp && !mediaMenuList.IsKeyboardFocusWithin
            && StandingOnTheLeadingEdge())
        {
          // Only the wall's leading edge points at the rail, and the leading edge is the WHOLE first column - the
          // shelf's law (MediaBrowserView.StandingOnTheLeadingEdge) and its reason: a caret standing in the middle
          // of a row of tiles is pointing at the tile beside it, and a rail that jumped out on every Left would be
          // a rail nobody can walk past.  Laid as rows the wall IS one column of names, so there the key keeps the
          // answer it always had.
          args.Handled = true;
          ShowRail(true);
          return;
        }

        if (args.Key == Key.Right && _menuUp && mediaMenuList.IsKeyboardFocusWithin)
        {
          args.Handled = true;
          ShowRail(false);
        }
      }, RoutingStrategies.Tunnel);

      // The Menu key and a right press are the two roads to a screen's own menu on every screen of this front end.
      AddHandler(ContextRequestedEvent, delegate (object? sender, RoutedEventArgs args)
      {
        args.Handled = true;
        ShowRail(!_menuUp);
      }, RoutingStrategies.Tunnel);
    }

    /// <summary>Every time the workflow shows this wall: read it if it was never read, and take the caret.</summary>
    public void OnScreenActivated()
    {
      if (_wall != null && !_readOnce)
      {
        _readOnce = true;
        _ = ReadWallAsync();
      }

      PutTheCaretOnTheWall();
    }

    /// <summary>The wall, never the rail: a rail shut behind the edge of the screen is not where keys start.</summary>
    public Control? KeysStartAt()
    {
      return peopleList;
    }

    /// <summary>Runs when the frame leaves the workflow: the wall is put down with it.</summary>
    public void OnScreenClosed()
    {
      if (_wall != null)
        _wall.PropertyChanged -= OnWallChanged;

      _hideRail?.Stop();
    }

    private async Task ReadWallAsync()
    {
      if (_wall == null)
        return;

      await _wall.ReadAsync(true);
      // The names arrived, so the plates and the shapes have to be laid under them; the caret is asked for again
      // because the row it stood on was not there when it was first asked for.
      Dispatcher.UIThread.Post(delegate
      {
        PutTheCaretOnTheWall();
        WearTheWallChrome();
        ScheduleFacePainting();
        ApplyColumns();
        FitWordsIntoThePanel();
      }, DispatcherPriority.Loaded);
    }

    /// <summary>How many frames the wall asks a control for its caret before it gives up on it.</summary>
    private const int CARET_TRIES = 12;

    private DispatcherTimer? _caretTimer;

    /// <summary>
    /// Puts the caret on the wall - on the name the wall says the caret stood on, or on the head of the wall when
    /// nobody is stood on it yet - and keeps asking for the wall until it can hold the caret.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two laws, both the shelf's.  The first is what a screen that opens with nobody selected does: it stands the
    /// caret on its first card, because a wall that opens unselected lights no tile, grows no picture, says nothing
    /// about anybody at its foot and behind its column, and hands its first arrow key to the rail standing two
    /// hundred points off the left edge - which is the same wall the viewer finds when they walk off the front of
    /// it.  <c>MediaBrowserView.FocusShelf</c> selects index nought for the same reason: the described name and the
    /// stood-on name have to be one name, not two answers to one question.
    /// </para>
    /// <para>
    /// The second is how the caret is asked for.  A list that has not been measured cannot hold a caret and answers
    /// <c>Focus()</c> with a failure and nothing else, which is the state this wall is in every time it is asked -
    /// the names came off the server one frame ago, the wall is still sliding in with the room.  Asked once the
    /// caret therefore stayed wherever it was, which measured as a wall of names with no frame on any of them.
    /// Asked once per frame for a fifth of a second it lands the moment the wall can hold it, and a field somebody
    /// is typing into always wins.
    /// </para>
    /// </remarks>
    private void PutTheCaretOnTheWall()
    {
      StandCaret(peopleList, delegate
      {
        if (_wall != null && _wall.Selected == null
            && peopleList.Items.OfType<PersonTile>().FirstOrDefault() is PersonTile head)
        {
          //  Stands the caret on the head of the wall through the wall's own answer, so that the column, the room's
          //  picture and the line at the foot all change their subject with it.
          _wall.Selected = head;
          return;
        }

        if (_wall?.Selected is PersonTile stood)
        {
          peopleList.ScrollIntoView(stood);
          StandTheCaret();
        }
      });
    }

    /// <summary>
    /// Gives the caret to a control of this screen and keeps asking for it until it is there - the shelf's
    /// <c>StandCaret</c>, for the shelf's reason (see <see cref="PutTheCaretOnTheWall"/>).
    /// </summary>
    private void StandCaret(Control target, Action? whenStood)
    {
      _caretTimer?.Stop();

      target.Focus();
      if (target.IsKeyboardFocusWithin)
      {
        whenStood?.Invoke();
        return;
      }

      DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render)
      {
        Interval = TimeSpan.FromMilliseconds(16)
      };
      _caretTimer = timer;
      int left = CARET_TRIES;

      timer.Tick += delegate
      {
        if (target.IsKeyboardFocusWithin)
        {
          timer.Stop();
          whenStood?.Invoke();
          return;
        }

        if ((TopLevel.GetTopLevel(this) as Window)?.FocusManager.GetFocusedElement() is TextBox || left-- <= 0)
        {
          timer.Stop();
          return;
        }

        target.Focus();
      };

      timer.Start();
    }

    private void OnWallChanged(object? sender, PropertyChangedEventArgs args)
    {
      switch (args.PropertyName)
      {
        case nameof(PeopleViewModel.MediaMenuVisible):
          ShowRail(_wall?.MediaMenuVisible == true);
          break;
        case nameof(PeopleViewModel.Layout):
          WearTheShape();
          break;
        case nameof(PeopleViewModel.ShowsSideDetails):
        case nameof(PeopleViewModel.ShowsDetailsWords):
          // The words came or went, and with them the points of room they stand in.
          ApplyColumns();
          RefitWords();
          break;
        case nameof(PeopleViewModel.Selected):
          RefitWords();
          if (_wall?.Selected is PersonTile stood)
          {
            peopleList.ScrollIntoView(stood);
            StandTheCaret();
          }
          break;
        case nameof(PeopleViewModel.DetailsBiography):
          // A life begins at its first word, so a new name is read from its beginning rather than from the line the
          // previous name's life happened to be scrolled to.
          biographyScroller.ScrollToHome();
          break;
      }
    }
    /// <summary>
    /// The two things a wall of the skin is asked to wear and a list of names is not: the mask that dissolves its
    /// rows into the top and bottom edges, and the glide that carries the wall to the name the caret stands on.
    /// Both live on the list's own scroller, which the markup never names - a list builds it from its template -
    /// so it is asked for once the list has been laid, which is the moment the shelf asks for its own
    /// (<c>MediaBrowserView.ApplyShelfFade</c>).
    /// </summary>
    /// <remarks>
    /// The fade is <see cref="BandEndFade"/>, the one reading every band of this front end dresses itself with,
    /// turned upright.  The glide is worn permanently rather than for one travel, for the shelf's own reason: a
    /// scroller that only animates when the program asks it to answers a mouse wheel with hard jumps, and the
    /// viewer's verdict on the shelf put smoothness above the crispness of a bare scroller.
    /// </remarks>
    private void WearTheWallChrome()
    {
      Dispatcher.UIThread.Post(delegate
      {
        ScrollViewer? scroller =
            peopleList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scroller == null)
          return;

        if (!ReferenceEquals(scroller, _wallScroller))
        {
          _wallScroller = scroller;
          _wallFade = new BandEndFade(scroller, false);
          scroller.Transitions = new Transitions
          {
            new VectorTransition
            {
              Property = ScrollViewer.OffsetProperty,
              Duration = StandsAsRows() ? ROW_GLIDE : TILE_GLIDE
            }
          };
        }

        // Names arrive into a wall that has already been laid, and a wall whose floor has grown since it was last
        // asked says nothing about the rows below its edge until it is asked.
        _wallFade?.Update();
      }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Stands the caret on the container the wall recycled for the selected name, which is where a shelf stands its
    /// caret and not an only-looking-fine detail: every rule of this skin that lights a tile's ring, grows its
    /// picture, whitens a row's sheet or doubles a row's height is written against <c>ListBoxItem:focus</c>.  A
    /// wall that kept the caret on the list itself would be a wall that never lights any of them, which is exactly
    /// what was measured before this method stood here.
    /// </summary>
    /// <param name="tries">
    /// A name the wall has not built a container for yet - a row of a wall of a thousand is only laid when it comes
    /// near the picture - is asked for once more after the layout has had its turn, as the shelf re-asks for the
    /// container it wants to travel to.
    /// </param>
    private void StandTheCaret(int tries = 2)
    {
      Control? stood = StoodContainer();
      if (stood == null)
      {
        if (tries > 0)
        {
          Dispatcher.UIThread.Post(delegate { StandTheCaret(tries - 1); }, DispatcherPriority.Loaded);
        }
        return;
      }

      stood.Focus();
      GlideWallToStoodName();
    }

    /// <summary>The container the wall recycled for the name being stood on, or nothing while it is not laid.</summary>
    private Control? StoodContainer()
    {
      int at = peopleList.SelectedIndex;
      return at < 0 ? null : peopleList.ContainerFromIndex(at) as Control;
    }

    /// <summary>
    /// Carries the wall so that the stood name sits inside the band its shape walks by, and lets the scroller's own
    /// transition carry it there.  The band, not the middle: a name already inside it is left where it stands, so a
    /// wall stays still for the first steps of a walk and starts moving when the stood row reaches the band's edge
    /// - the shelf's behaviour, and the reason its wall never crawls row by row under a held key.  Where a band
    /// cannot hold what stands inside it (a grid's four hundred points either side of a window eight hundred tall
    /// leaves thirty points of band for a tile two hundred tall), the name is stood at the middle instead, which is
    /// the shelf's own answer to that case.
    /// </summary>
    private void GlideWallToStoodName()
    {
      ScrollViewer? scroller = _wallScroller;
      Control? stood = StoodContainer();
      if (scroller == null || stood == null)
        return;

      double view = scroller.Viewport.Height;
      if (view <= 0)
        return;

      Point at = stood.TranslatePoint(new Point(0, 0), scroller) ?? new Point(0, 0);
      double top = scroller.Offset.Y + at.Y;
      double high = stood.Bounds.Height;
      bool rows = StandsAsRows();
      double before = rows ? ROW_BAND : TILE_BAND;
      double after = rows ? ROW_BAND : TILE_BAND;

      double wanted = scroller.Offset.Y;
      if (view - before - after >= high)
      {
        if (top < before)
          wanted = top;
        else if (top + high > view - after)
          wanted = top + high - (view - after);
      }
      else
      {
        wanted = top + high / 2.0 - view / 2.0;
      }

      double floor = Math.Max(0.0, scroller.Extent.Height - view);
      wanted = Math.Max(0.0, Math.Min(floor, wanted));
      if (Math.Abs(wanted - scroller.Offset.Y) < 0.5)
        return;

      scroller.Offset = new Vector(scroller.Offset.X, wanted);
    }

    /// <summary>
    /// Asks for the faces of the names the screen can see, once the layout pass that laid them out is over - the
    /// shelf's <c>ScheduleRowPainting</c>, for the shelf's reason: a tile is noticed as prepared before anything was
    /// measured, so the pass is posted to the moment when a container's bounds say something about standing inside
    /// the viewport.
    /// </summary>
    private void ScheduleFacePainting()
    {
      if (_facesScheduled)
        return;

      _facesScheduled = true;
      Dispatcher.UIThread.Post(delegate
      {
        _facesScheduled = false;
        PaintFacesTheWallCanSee();
      }, DispatcherPriority.Loaded);
    }

    private bool _facesScheduled;

    /// <summary>
    /// Asks the wall for the face of every name it has laid out where the television can see it, nearest the caret
    /// first and a batch at most.
    /// </summary>
    /// <remarks>
    /// The order is the whole point, and it is the shelf's (see <c>MediaBrowserView.PaintRowsTheScreenCanSee</c>):
    /// the picture fetcher keeps four errands on the wire and queues the rest, so a wall walked to the right that
    /// asked in the order its panel laid things spends its whole queue on the names the viewer has already left.
    /// Walking the sighted names by their distance from the caret is what makes the faces arrive where a viewer is
    /// looking instead of behind them, and taking only the nearest of them keeps a panel that pre-heated a band
    /// beyond the screen from buying pictures nobody can see.
    /// </remarks>
    private void PaintFacesTheWallCanSee()
    {
      if (_wall == null)
        return;

      int caret = peopleList.SelectedIndex;
      List<(int distance, PersonTile tile)> in_sight = new List<(int distance, PersonTile tile)>();

      foreach (Control? container in peopleList.GetRealizedContainers())
      {
        if (container?.DataContext is not PersonTile tile)
          continue;

        int at = peopleList.IndexFromContainer(container);
        if (at < 0)
          continue;

        in_sight.Add((caret < 0 ? 0 : Math.Abs(at - caret), tile));
      }

      Services.HomeTrace.Log("paintfaces", "sighted=" + in_sight.Count + " asking="
          + Math.Min(in_sight.Count, MAX_TILES_PAINTED_AT_ONCE) + " caret=" + caret);

      foreach ((int _, PersonTile tile) pair in
          in_sight.OrderBy(stand => stand.Item1).Take(MAX_TILES_PAINTED_AT_ONCE))
      {
        _wall.PaintFace(pair.tile);
      }
    }

    /// <summary>
    /// How many names are asked for their face in one pass: about what a television fills with tiles, twice over,
    /// so that the names coming into view are already on their way when the viewer reaches them.  The shelf's
    /// number for the same question.
    /// </summary>
    private const int MAX_TILES_PAINTED_AT_ONCE = 48;

    /// <summary>Whether the wall stands as rows of words rather than as a wrap of squares.</summary>
    private bool StandsAsRows()
    {
      return _wall != null
          && string.Equals(_wall.Layout, PeopleViewModel.LAYOUT_LIST, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether the caret stands on the wall's leading edge, which is the whole first column of names and not the
    /// first name alone: the shelf reads its edge that way (MediaBrowserView.StandingOnTheLeadingEdge) because a
    /// viewer who walked one tile to the right must find the same Left they found before, and one who stands in the
    /// middle of a row means the tile beside them rather than the rail.  A wall laid as rows is one column, so
    /// every row stands on the edge; a wall with no caret on it has no tile to step to, and the rail is the only
    /// thing Left can mean there.
    /// </summary>
    private bool StandingOnTheLeadingEdge()
    {
      int standing = peopleList.SelectedIndex;
      if (standing < 0 || StandsAsRows())
        return true;

      return standing % WallColumns() == 0;
    }

    /// <summary>
    /// How many names the wall lays across, taken from the panel that lays them - the only one who knows, since it
    /// is the tile's own width against the wall's that decides.  Asked rather than remembered, because the shape
    /// changes with the room and with the shape the viewer picked.
    /// </summary>
    private int WallColumns()
      => peopleList.GetVisualDescendants().OfType<VirtualizingTilePanel>().FirstOrDefault()?.Columns ?? 1;

    /// <summary>How far the wall travels ahead of and behind the stood name, and how long the travel takes.</summary>
    private const double TILE_BAND = 400.0;
    private const double ROW_BAND = 130.0;
    private static readonly TimeSpan TILE_GLIDE = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ROW_GLIDE = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Divides the room between the wall and its column of words by the measure a shelf of titles divides its room
    /// by (<see cref="ShelfDetailsColumn"/>), so that the same window width hands a wall and a shelf the same
    /// number of points to the words and the two screens stand their tiles at the same x.  With the words away the
    /// wall has the room to itself, which is what the shelf does when a viewer turns them off.
    /// </summary>
    private void ApplyColumns()
    {
      if (_wall == null)
        return;

      double room = Bounds.Width > 0 ? Bounds.Width : 1920;
      double details = ShelfDetailsColumn.WidthFor(room, _wall.ShowsSideDetails);
      if (_detailsColumnWidth.HasValue && Math.Abs(_detailsColumnWidth.Value - details) < 0.5)
        return;

      _detailsColumnWidth = details;
      contentGrid.ColumnDefinitions = new ColumnDefinitions
      {
        new ColumnDefinition(GridLength.Star),
        new ColumnDefinition(new GridLength(details))
      };
    }

    /// <summary>
    /// Measures the column of words again once the layout has had its say, which is the only moment its own
    /// position is known.  Measuring it the instant a name is stood on hands the fitter a point the panel has not
    /// been laid at yet, and a height read off a box that was not laid out is a height that cuts the wrong lines.
    /// </summary>
    private void RefitWords()
    {
      Dispatcher.UIThread.Post(FitWordsIntoThePanel, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Hands the column of words the height it stands in, which is everything between the line its first word
    /// begins at and the floor the viewer drew under the shelf's words (<see cref="ShelfDetailsColumn"/>).  A box
    /// only keeps to a height it is handed, and this one is handed none by the markup: a life of eight thousand
    /// characters asks for eight thousand lines, which is what ran the last lines of a reading off the bottom of
    /// the screen and cut them through the middle of a glyph.
    /// </summary>
    private void FitWordsIntoThePanel()
    {
      double height = Bounds.Height;
      if (height <= 0 || !detailsPanel.IsEffectivelyVisible)
        return;

      Point stood = detailsWords.TranslatePoint(new Point(0, 0), this) ?? new Point(0, -1);
      double room = ShelfDetailsColumn.WordsHeightFor(height, stood.Y);
      if (double.IsNaN(room) || (!double.IsNaN(detailsWords.Height) && Math.Abs(detailsWords.Height - room) < 0.5))
        return;

      detailsWords.Height = room;
    }

    /// <summary>
    /// Puts the wall into the shape its viewer picked: the square a portrait is drawn in, or the strip of rows a
    /// list is.  The panel travels with the template, because a wrap of squares and a strip of rows are two ways of
    /// laying things out rather than two ways of drawing one thing.
    /// </summary>
    private void WearTheShape()
    {
      bool asRows = _wall != null
          && string.Equals(_wall.Layout, PeopleViewModel.LAYOUT_LIST, StringComparison.OrdinalIgnoreCase);

      // THE SKIN'S OWN NAMES for what the wall is, which is how a shelf of titles is dressed: the view puts these
      // two class names on its list and the skin answers with the row's white sheet, the tile's shadow and ring,
      // and the resting and stood states of a row (MediaBrowserView.ApplyLayout).  A name of our own inventing kept
      // the skin silent about the wall and left this screen to re-invent a look it already had.
      peopleList.Classes.Set("nereus-shelf-rows", asRows);
      peopleList.Classes.Set("nereus-shelf-tiles", !asRows);

      object? key = asRows ? "PersonWallRow" : "PersonWallTile";
      if (this.TryFindResource(key, out object? template) && template is IDataTemplate worn)
        peopleList.ItemTemplate = worn;

      if (asRows)
        peopleList.ClearValue(ItemsControl.ItemsPanelProperty);
      else
        peopleList.ItemsPanel = TILE_PANEL;

      WearTheWallChrome();

      // The shape changed under tiles that stayed standing, so the tiles the viewer can see are exactly the tiles
      // that still have to be offered their faces - a template swap prepares no new container for them.
      ScheduleFacePainting();
    }

    /// <summary>
    /// The transform the room travels on, read off the room itself: naming a transform in the markup hands the code
    /// a field for a thing that is not a control, and asking the control what it is wearing answers the same
    /// question without that detour.
    /// </summary>
    private TranslateTransform RoomSlide
    {
      get { return (TranslateTransform)room.RenderTransform; }
    }

    /// <summary>
    /// Slides the room out of the rail's way, or back over it.  The rail is put into the tree when the room begins
    /// to move away and out of it again when the room has arrived, because Avalonia measures and paints a control
    /// nobody can see not at all - a rail left in the tree while the room stands over it is a rail a wall carries
    /// off the picture for the rest of its life.
    /// </summary>
    private void ShowRail(bool open)
    {
      if (_wall == null || _menuUp == open)
        return;

      _menuUp = open;
      _hideRail?.Stop();

      // The model is told, because it is the model that says what the foot line under the head reads and what the
      // plates are standing for; the word comes back here as a property change, which this method answers by
      // noticing it already stands where it was asked to stand.
      if (open)
        _wall.ShowMediaMenu();
      else
        _wall.HideMediaMenu();

      if (open)
      {
        sideMenu.IsVisible = true;

        // The rail has to be wired before its first row is asked for, and only once: the handlers it puts on its
        // list are put on the list, and a list asked twice answers every key twice.
        if (!_railWired)
        {
          _railWired = true;
          _rail?.Wire();
        }

        RoomSlide.X = MENU_ROOM;
        Dispatcher.UIThread.Post(delegate
        {
          _rail?.Dress();
          if (mediaMenuList.Focus())
            _rail?.StandOnRow(0);
        }, DispatcherPriority.Loaded);
        return;
      }

      RoomSlide.X = 0;
      if (_hideRail == null)
      {
        _hideRail = new DispatcherTimer { Interval = SLIDE_TIME };
        _hideRail.Tick += delegate
        {
          _hideRail!.Stop();
          if (!_menuUp)
            sideMenu.IsVisible = false;
        };
      }
      _hideRail.Start();
      PutTheCaretOnTheWall();
    }

    /// <summary>Asks the application for one person's works and pushes the shelf it answers with.</summary>
    private async Task OpenWorksAsync(PersonTile person)
    {
      Func<PersonTile, Task<WorkflowFrame?>>? ask = _works;
      if (ask == null || _workflow == null)
        return;

      WorkflowFrame? frame = await ask(person).ConfigureAwait(true);
      if (frame != null)
        _workflow.Push(frame);
    }

    // What the rail asks of this screen; see IShelfRailHost.
    /// <inheritdoc />
    IMenuRailOwner? IShelfRailHost.RailOwner => _wall;

    /// <inheritdoc />
    void IShelfRailHost.RailCaret(Control target, Action? whenStood)
    {
      // A row of the rail that was replaced a moment ago is not realised yet, so the caret is asked for again
      // until the row is there - the same patience the shelf's own caret uses, for the same reason.
      if (target.IsFocused || target.Focus())
      {
        whenStood?.Invoke();
        return;
      }

      Dispatcher.UIThread.Post(delegate
      {
        if (target.Focus())
          whenStood?.Invoke();
      }, DispatcherPriority.Loaded);
    }

    /// <inheritdoc />
    void IShelfRailHost.RailNod(Control ground, double diveX, double diveY)
    {
      _nods.Nod(ground, diveX, diveY);
    }

    /// <summary>
    /// The nod this screen's rail answers a pointing key with - the shelf's own nodder, so the lean and the spring
    /// feel exactly like the lean and the spring of a shelf of titles.
    /// </summary>
    private readonly ShelfNod _nods = new ShelfNod();

    /// <inheritdoc cref="ShelfNod.BounceX" />
    internal double RailBounceX => _nods.BounceX;

    /// <inheritdoc cref="RailBounceX" />
    internal double RailBounceY => _nods.BounceY;

    /// <inheritdoc cref="ShelfNod.LastNodX" />
    internal double LastNodX => _nods.LastNodX;

    /// <inheritdoc cref="RailBounceY" />
    internal double LastNodY => _nods.LastNodY;

    /// <inheritdoc />
    void IShelfRailHost.RailClimbedDown(int row)
    {
      // Remembered so that the way back can stand the caret on the command the viewer climbed down from.  The
      // rail itself does the standing; this screen only keeps the number.
      ClimbedFrom = row;
    }

    /// <summary>Which row of the commands the viewer climbed down from, or -1 while they never did.</summary>
    private int ClimbedFrom
    {
      get;
      set;
    } = -1;
  }
}
