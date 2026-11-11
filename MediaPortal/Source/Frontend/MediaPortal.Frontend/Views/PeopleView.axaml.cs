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

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;
using System;
using System.ComponentModel;
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
    private bool _railWired;
    private bool _readOnce;
    private bool _menuUp;
    private DispatcherTimer? _hideRail;

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

      peopleList.SelectionChanged += delegate
      {
        // A caret walking into the last quarter of the names that arrived is the moment to ask for more, which is
        // the only wall a screen has that a hand is on it: a wall read by the thousandth row cannot wait for a
        // "load more" plate nobody can see.
        if (_wall != null)
          _ = _wall.KeepReadingAsync();
      };

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

        if (args.Key == Key.Left && !_menuUp && !mediaMenuList.IsKeyboardFocusWithin)
        {
          // The wall is one column of names, so a caret standing anywhere on it stands on its leading edge, and
          // the rail is the only other thing on this screen that can be holding the caret.  Asked that way rather
          // than "when the wall has focus", because a screen the workflow has just shown has not been handed the
          // focus yet by every host - and a viewer who points left from a wall they can see means the rail.
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
      Dispatcher.UIThread.Post(PutTheCaretOnTheWall, DispatcherPriority.Loaded);
    }

    /// <summary>Puts the caret on the wall, or on the row the wall says the caret stood on.</summary>
    private void PutTheCaretOnTheWall()
    {
      if (!peopleList.Focus())
        return;

      if (_wall?.Selected is PersonTile stood)
        peopleList.ScrollIntoView(stood);
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
        case nameof(PeopleViewModel.Selected):
          if (_wall?.Selected is PersonTile stood)
            peopleList.ScrollIntoView(stood);
          break;
      }
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

      object? key = asRows ? "PersonWallRow" : "PersonWallTile";
      if (this.TryFindResource(key, out object? template) && template is IDataTemplate worn)
        peopleList.ItemTemplate = worn;

      if (asRows)
        peopleList.ClearValue(ItemsControl.ItemsPanelProperty);
      else
        peopleList.ItemsPanel = TILE_PANEL;
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
      // The lean toward the wall the viewer pointed at and the return.  The room carries it rather than the rail,
      // because it is the room that has the transition on it - the rail travelling with its own room would be a
      // rail moving twice.
      RoomSlide.X = diveX;
      Dispatcher.UIThread.Post(delegate
      {
        if (_menuUp)
          RoomSlide.X = 0;
      }, DispatcherPriority.Loaded);
    }

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
