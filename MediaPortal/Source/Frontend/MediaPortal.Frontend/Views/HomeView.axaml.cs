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
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.Skins.Nereus;
using MediaPortal.Frontend.Skins.WMC;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The screen that shows the server and its media aspects - the root of the workflow, and the first screen the
  /// shell hosts.
  /// </summary>
  public partial class HomeView : UserControl, IWorkflowScreen
  {
    private readonly WorkflowManager _workflow;
    private readonly Func<WorkflowFrame>? _nowPlayingScreen;
    private readonly Func<WorkflowFrame>? _connectScreen;
    private readonly Func<HomeSection, WorkflowFrame>? _sectionScreen;
    private readonly Func<string, WorkflowFrame>? _screenFactory;

    /// <summary>
    /// Whether the person watching may reach the screen a corner button names - the application's viewer
    /// state answered for one key. Null on a machine where nothing is kept from anybody. The buttons read it
    /// for their face, and the press reads it again, so a name that was hidden a moment ago cannot be opened
    /// by a highlight that walked onto it a moment before.
    /// </summary>
    private readonly Func<string, bool>? _opensScreen;
    private readonly HomeViewModel _home;

    /// <summary>
    /// WMC's own home menu model - the groups and the two cursors its cross walks. It belongs to the skin (see
    /// the class), and the view is the seam where a worn face's model is hung on the shared menu: the base front
    /// end knows a flat menu and a selection, and only this view reads WMC's grouping out of them. Nereus wears
    /// its own model in its own way, and neither reading touches the other.
    /// </summary>
    private readonly WmcHomeMenuModel _wmc;

    /// <summary>
    /// How long the standing wall takes to leave the light when its entry changes. The way back up is longer and
    /// lives with the wall's own curve in <c>HomeView.axaml</c> (0.45), because a picture arriving is worth more
    /// watching than a picture leaving. These two are the only knobs the change of a home menu entry has: zero on
    /// both is the hard cut this screen had before any of it.
    /// </summary>
    private const int WALL_SINK_MS = 180;

    /// <summary>
    /// The beat the screen is given on top of the curve's own length, because a curve is played out by the screen
    /// rather than by the clock and the last frame wants to have been drawn before its successor is written.
    /// </summary>
    private const int WALL_SINK_ROOM_MS = 40;

    /// <summary>
    /// Whether the standing wall has been sent down and not yet brought back, and since when. The first decides
    /// who pays for the curve (see <see cref="SinkStandingWallAsync"/>), the second how much of it is left.
    /// </summary>
    private bool _wallSinking;
    private readonly System.Diagnostics.Stopwatch _wallSink = new System.Diagnostics.Stopwatch();

    /// <summary>
    /// Whether the beat that answers a wall's arrival has been asked for already. A wall is put on band by band, so
    /// without this one answer every band folds, raises and measures the wall afresh.
    /// </summary>
    private bool _wallArrivalQueued;

    /// <summary>
    /// The number the wall's transform carries, so that a shelf arriving on the same number re-lays nothing out.
    /// Zero means no transform stands.
    /// </summary>
    private double _wallScale;

    /// <summary>
    /// The shape of a refusal, as shares of the distance the thing stood on is thrown: out toward the edge in two
    /// beats, back in five, so that the last beat is a settling and not a snap. The numbers are the shape; a beat
    /// is <see cref="LEAN_STEP_MS"/> long.
    /// </summary>
    private static readonly double[] LEAN_SHAPE = { 0.62, 1.0, 0.82, 0.6, 0.42, 0.26, 0.12 };
    private const int LEAN_STEP_MS = 30;

    /// <summary>
    /// How far the content pane is thrown when an arrow asks the wall for a row below its last one - down the
    /// television, so a positive number - and how far the corner row when an arrow asks above the head of the
    /// screen - up, so a negative one. The wall's right end throws its content back into the screen, so a negative
    /// number sideways: the pane is thrown off the edge the viewer asked for rather than after it. The menu's left
    /// end is that answer mirrored - the rail is the leftmost thing the face wears, and it too is thrown back into
    /// the screen, so a positive number.
    /// The four numbers are the skin's promise about how strongly it refuses, and the fixture holds the face to
    /// them: how far a lean actually travels depends on how many frames a screen paints, which is not something a
    /// test can lean on, whereas the depth a refusal was built to reach is.
    /// </summary>
    public const double WALL_LEAN_DOWN = 16.0;
    public const double CORNER_LEAN_UP = -10.0;
    public const double WALL_LEAN_RIGHT = -18.0;
    public const double MENU_LEAN_LEFT = 18.0;

    /// <summary>
    /// The refusals this screen knows, one per edge, kept apart so that a wall mid-lean does not silence the
    /// corner row and the other way round.
    /// </summary>
    private readonly LeanAtTheEnd _wallRefusal = new LeanAtTheEnd();
    private readonly LeanAtTheEnd _cornerRefusal = new LeanAtTheEnd();
    private readonly LeanAtTheEnd _wallEndRefusal = new LeanAtTheEnd();
    private readonly LeanAtTheEnd _menuRefusal = new LeanAtTheEnd();

    /// <summary>
    /// The name the worn face gives the row of corner buttons in its own markup, which is the thing that leans
    /// when an arrow asks for one more row above it.
    /// </summary>
    private const string TOP_MENU_ROW = "topMenuRow";

    /// <summary>
    /// The class that puts the shorter curve on the wall for the length of a change of entries. Its curve is the
    /// one the styles block keeps under that name in <c>HomeView.axaml</c>, and its length is <see cref="WALL_SINK_MS"/>.
    /// </summary>
    private const string WALL_SINKING_CLASS = "wall-sinking";

    /// <summary>
    /// The window this home is standing in, while it is standing in one. The plain test windows that host
    /// this view in the test runs have no shell chrome at all, which is exactly the case where nothing is
    /// asked of it - hence a nullable, not an assumption.
    /// </summary>
    private ShellWindow? _chromeShell;

    /// <summary>
    /// For the XAML loader and the designer: a screen with a view model that has no server, which is what it
    /// says on screen.
    /// </summary>
    public HomeView()
        : this(new HomeViewModel(), new WorkflowManager())
    {
    }

    /// <param name="home">What the screen shows.</param>
    /// <param name="workflow">
    /// The navigation this screen opens its sub-screens through. Nothing here calls <c>Show()</c> anymore: the
    /// shell has one window and the stack says where the viewer is, so opening means pushing a frame and the back
    /// key pops it - which is the arrangement the old client had and this front end is getting back.
    /// </param>
    /// <param name="nowPlayingScreen">
    /// How to build the playback screen's frame, or <c>null</c> on a machine with no player. The screen is built
    /// by the application because the playback belongs to the application, not to this view: popping back from
    /// the playback screen leaves the film running.
    /// </param>
    /// <param name="connectScreen">
    /// How to build the frame of the screen that picks the server, or <c>null</c> when the operator has taken
    /// that decision away by starting the front end with an address of its own.
    /// </param>
    /// <param name="screenFactory">
    /// How to build the frame a menu entry names - <c>"folders"</c>, <c>"themes"</c>, the media screens - or
    /// <c>null</c> when this build has no screens behind the menu at all. A name rather than a frame per entry,
    /// because the menu is assembled from what the server reports and the application is the one that knows what
    /// it can build.
    /// </param>
    public HomeView(HomeViewModel home, WorkflowManager workflow, Func<WorkflowFrame>? nowPlayingScreen = null,
        Func<WorkflowFrame>? connectScreen = null, Func<HomeSection, WorkflowFrame>? sectionScreen = null,
        Func<string, WorkflowFrame>? screenFactory = null, Func<string, bool>? screenGate = null)
    {
        DataContext = home;
      _home = home;
      _wmc = new WmcHomeMenuModel(() => _home.Menu, () => _home.SelectedEntry, entry => _home.SelectedEntry = entry);
      _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
      _nowPlayingScreen = nowPlayingScreen;
      _connectScreen = connectScreen;
      _sectionScreen = sectionScreen;
      _screenFactory = screenFactory;
      _opensScreen = screenGate;
      InitializeComponent();

      // One wall on the screen, and the change of entries is this wall going out and its successor coming up. The
      // model reads a wall where the screen cannot see it and then asks this view for the way between the two:
      // SinkStandingWallAsync below, and RaiseWall where the finished wall arrives. The wall starts in the dark so
      // that even the first wall of a session comes up on its curve rather than appearing.
      nereusWallStanding.Opacity = 0;
      _home.WallHandover = SinkStandingWallAsync;
      _home.WallLeaving = BeginWallSink;

      home.ContentGroups.CollectionChanged += delegate
      {
        // A wall is put on band by band, and every band is a notification of its own. Answered on the spot, one
        // change of entries folded the pane away, raised it on one band, raised it again on two and again on three,
        // and asked for the wall's measurement four times over - a wall watched being built, which is the one thing
        // this exchange exists to prevent, and three layout passes of a tree that is only finished at the last
        // band. So the beat is asked for once and runs when the swapping is through: what it looks at then is the
        // wall the viewer is meant to see, and it answers it once.
        if (_wallArrivalQueued)
          return;

        _wallArrivalQueued = true;
        Dispatcher.UIThread.Post(PutFinishedWallInLight);
      };

      // The labels of the main menu survive any skin change, by fiat: the moment this view is built it owns
      // its own tile tree, and any moment afterwards when no skin answers the home menu's key, the view's
      // own tiles go back onto the list - whoever swapped them away and whatever its bookkeeping did. A
      // home menu without labels after a skin switch is not a detail to argue about; it is the one thing
      // this screen may never do.
      IDataTemplate? ownHomeTiles = menuList.ItemTemplate;
      SkinEngine.Changed += delegate
      {
        if (ownHomeTiles != null
            && SkinEngine.TryTileTemplate("HomeMenuTileTemplate") == null
            && !ReferenceEquals(menuList.ItemTemplate, ownHomeTiles))
        {
          menuList.ItemTemplate = ownHomeTiles;
        }
      };
      // and on the way back onto screen, where a swap's hand-back happens without any event at all
      AttachedToVisualTree += delegate
      {
        if (ownHomeTiles != null
            && SkinEngine.TryTileTemplate("HomeMenuTileTemplate") == null
            && !ReferenceEquals(menuList.ItemTemplate, ownHomeTiles))
        {
          menuList.ItemTemplate = ownHomeTiles;
        }

        // The window is only knowable once the view hangs in a tree, and the header's standing depends on
        // the window: find it on the way in, lay the floating header to rest on the way out.
        _chromeShell = this.GetVisualAncestors().OfType<ShellWindow>().FirstOrDefault();
        _chromeShell?.SetChromeHeaderOverlay(_home.HomeLayout == "nereus");
      };
      DetachedFromVisualTree += delegate
      {
        // Walking into a deeper screen hands the header back: it was floating because this home asked it
        // to, and a screen that plans its space under a header row would find its top cut off otherwise.
        _chromeShell?.SetChromeHeaderOverlay(false);
        _chromeShell = null;
      };

      // The arrangement on screen is the worn face's own answer: each rescued skin ships its home screen
      // with a layout of its own, and changing the face moves the same three areas somewhere else. The
      // view model says which arrangement is worn and says again whenever the face changes.
      home.PropertyChanged += delegate (object? sender, PropertyChangedEventArgs e)
      {
        if (e.PropertyName == nameof(HomeViewModel.HomeLayout))
          ApplyHomeLayout();
        else if (e.PropertyName == nameof(HomeViewModel.SelectedEntry))
        {
          RebuildCross();
          ScrollSelectedMenuItem();
        }
      };
      // The menu arriving, arriving again from a refresh, or keeping its place through a rebuild - the
      // cross arms show the menu, so every time the menu speaks, WMC's model lays itself out from it again
      // and the arms are redrawn from the model.
      _home.MenuRebuilt += delegate
      {
        _wmc.Rebuild(_home.SelectedEntry);
        RebuildCross();
        ScrollSelectedMenuItem();
        TakeTheMenuBackIfTheRebuildThrewItOut();
      };
      // The face's own home comes and goes with the face. Hang what its home.xaml answers on the three hosts
      // now - a view built after a skin went on never sees the event that put the skin on - and again whenever
      // the skin changes under a running front end.
      SkinEngine.Changed += WearSkinHome;
      WearSkinHome(null, EventArgs.Empty);

      // The bottom row's buttons belong to ApplyHomeLayout alone, and it is called just below: setting their
      // visibility here as well re-showed the NowPlaying and server buttons that the Nereus case had just
      // hidden, and the row then held the menu column short of the bottom of the screen.
      ApplyHomeLayout();

      // The wall is scaled to the panel it stands on, and a screen only knows its own size after its first
      // layout - so a panel that changes (a window dragged between televisions, a first measure of zero)
      // asks the worn face for its share again rather than keeping a transform computed on nothing. The shelf
      // arrives later still, from the server, and until it has there is no drawn height to scale from. The shelf's
      // own arrival is answered where a wall's arrival is answered - in PutFinishedWallInLight, once for a wall
      // rather than once for every band of it.
      SizeChanged += delegate { ScaleNereusWall(); SizeNereusMenu(); };

      // A wall that was already standing when this view was built comes into the light here. The wake reads the
      // library and the opening wall behind its ring and only then hands the window over, so on the way in the
      // model is regularly finished reading while this view does not exist yet - and the light a wall arrives in
      // is written by its arrival, which nothing heard. Left at that, the home opened on a wall standing in the
      // dark it rises out of, empty until the first step along the menu sent a second wall in. Asking the beat a
      // wall's arrival is answered in is the whole fix: it scales the standing wall and brings it, or folds it
      // away when it turned out to hold nothing.
      if (_home.ContentGroups.Count > 0)
        PutFinishedWallInLight();

      // The ring is in the order the areas are arranged on the screen: the menu on the left, its content to the
      // right of it, the server's aspect list on the far side, and the buttons underneath. Left/right walk it;
      // each list keeps its own up/down over its items, because it handles those keys before they reach the ring.
      // The menu first: it is where MP2's home screen starts, and where a viewer with a remote expects to land.
      var ring = new List<Control>();
      if (_sectionScreen != null)
        ring.Add(menuList);
      ring.Add(_home.IsNereusHome ? nereusContentPane : contentList);
      ring.Add(aspectsList);
      ring.Add(refreshButton);
      if (_nowPlayingScreen != null)
        ring.Add(nowPlayingButton);
      if (_connectScreen != null)
        ring.Add(connectButton);
      DpadNavigation.Attach(this, ring);

      menuList.AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (!_home.IsNereusHome || args.Key != Key.Right)
              return;

            // The reference's own courtesy on the way back: the menu's Right key does not start the viewer at
            // the top of the wall again, it returns them to the square they last stood on. That is what
            // NereusHomeListView's RestoreFocusItem was for, seen from the other side. The one place it does not
            // apply is the head of the menu, where the row of buttons - the power, the person, the cog and the
            // search glass - is the thing that actually stands beside the viewer.
            Visual? pressed = args.Source as Visual;
            while (pressed != null && pressed != menuList && !(pressed is ListBoxItem))
              pressed = pressed.GetVisualParent();
            ListBoxItem? stoodOn = pressed as ListBoxItem;
            Control? goingTo = BesideTheRow(stoodOn)
                ?? (Control?) LastWallStand()
                ?? GetFocusableNereusTiles().FirstOrDefault();
            if (goingTo == null)
              return;

            if (goingTo is Button tile)
              FocusNereusTile(tile);
            else if (!goingTo.Focus())
              return;
            else
              goingTo.BringIntoView();

            args.Handled = true;
          },
          RoutingStrategies.Tunnel,
          handledEventsToo: true);

      // A list that is merely focused is not a list that is stood on: with the ring or the wall handing the rail
      // the focus, the viewer sees no frame and an arrow key that does nothing. Step onto an entry instead -
      // the one the wall is showing, or the first - which is the same thing the reference's list does when its
      // items are realized and its selection stands somewhere. Stepped after the focus call has returned, not
      // inside it: a control that hands the focus on while it is being given it reports the giving as a failure,
      // and whatever asked for the move (the ring among them) reads that as nothing having happened.
      menuList.AddHandler(GotFocusEvent,
          delegate (object? sender, FocusChangedEventArgs args)
          {
            if (!_home.IsNereusHome || !ReferenceEquals(args.Source, menuList))
              return;

            Dispatcher.UIThread.Post(delegate
            {
              if (menuList.IsFocused)
                StandOnMenuEntry();
            });
          },
          handledEventsToo: true);

      // The rail's own memory, for the walk to hand back: which entry the viewer was last stood on. The entry is
      // remembered rather than the container, because a list realizes and throws away its containers as it is
      // scrolled, and because the menu rebuilds them whenever the menu itself changes.
      menuList.AddHandler(GotFocusEvent,
          delegate (object? sender, FocusChangedEventArgs args)
          {
            if (args.Source is ListBoxItem item && item.DataContext is MainMenuEntry entry)
              _lastRailStand = entry;
          },
          RoutingStrategies.Bubble,
          handledEventsToo: true);

      nereusContentPane.AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (!_home.IsNereusHome || args.Handled
                || (args.Key != Key.Left && args.Key != Key.Right
                    && args.Key != Key.Up && args.Key != Key.Down))
              return;

            Button[] tiles = GetFocusableNereusTiles();
            Button? focusedTile = tiles.FirstOrDefault(tile => tile.IsKeyboardFocusWithin);
            if (focusedTile == null)
            {
              if (args.Key == Key.Left || args.Key == Key.Up)
              {
                if (StandOnMenuEntry())
                  args.Handled = true;
              }
              else if (tiles.FirstOrDefault() is Button firstTile)
              {
                FocusNereusTile(firstTile);
                args.Handled = true;
              }
              return;
            }

            _wallStand = focusedTile;
            Control? nextTile = NextOnWall(focusedTile, tiles, args.Key);
            if (nextTile is Button tile)
            {
              FocusNereusTile(tile);
              args.Handled = true;
            }
            else if (args.Key == Key.Down)
            {
              // Nothing of the wall lies under this square. What does lie below the wall is a tile of the menu -
              // another place, one the viewer reaches with Left, and not the thing the arrow pointed at - so the
              // key is answered where it was pressed instead: the wall leans off its own end and the viewer keeps
              // the square they were standing on.
              LeanWallOffTheBottom();
              args.Handled = true;
            }
            else if (args.Key == Key.Right)
            {
              // The same answer at the wall's other end, and for the same reason. Past the last square there is
              // nothing of the wall to the right, and what the measurement still finds somewhere off that edge is
              // an entry of the menu - a different place, reached with Left. Before, that key threw the viewer out
              // of the wall and onto the menu; now the wall leans off its right end, which is the answer to
              // "one more than the wall has", and the viewer keeps the square they stand on.
              LeanWallOffTheRight();
              args.Handled = true;
            }
            else if (nextTile is Control entry && entry.Focus())
            {
              // The menu rail lies in that direction, and it is reached the way every other control on the
              // screen is: by the same measurement, not by a special case for the Left key.
              entry.BringIntoView();
              args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel,
          handledEventsToo: true);

      // The same screen with a mouse on a desk: double-clicking a tile, a title or a watched row sends the
      // Enter that a remote would have sent on that same list, and this screen's own handler below - which
      // already knows which lists open what and which rows merely explain something - decides the rest.
      MouseRemote.Attach(this, menuList, contentList);

      // Clicking a home tile selects it and opens nothing: one click or one touch stands on a tile, and
      // only the second click of a double-click (already routed through Enter by MouseRemote just above)
      // walks through the door. That is Avalonia's own press behaviour, so there is deliberately no
      // handler here any more - the one that used to post an Enter on a single click is what this removes.
      // Keyboard and remote keep walking the old road: a tile stood on is opened by pressing it.
      menuList.FocusAdorner = null; // no blue frame, from nowhere, ever.

      // Nereus' own home menu editor, opened the way the reference opened it: from the menu itself, with the
      // context key. MP2 hung that on the menu list's <c>Context_Menu_Command</c>, which a remote's context key
      // answers and a mouse's right button answers; on a screen with no keys at all, a finger that rests on a
      // tile is the same intention. All three belong to Nereus alone - the other face's menu is not a rail of
      // tiles the viewer can rearrange, it is invisible markup behind the cross.
      menuList.AddHandler(PointerPressedEvent,
          delegate (object? sender, PointerPressedEventArgs args)
          {
            if (!_home.IsNereusHome)
              return;

            if (args.Properties.IsRightButtonPressed)
            {
              OpenMenuEditor();
              args.Handled = true;
              return;
            }

            if (args.Pointer.Type != PointerType.Mouse)
              BeginRestingPress(args.GetPosition(menuList));
          },
          RoutingStrategies.Tunnel);

      // A tile picked with the mouse is the reference's explicitly chosen tile and is answered at once; the settling
      // stays on the walk, where it was always meant to keep the pane from being refilled for every tile passed
      // over. Bubble, because Avalonia selects on the press: by the time this arrives the picked tile is the
      // selected one, which is the tile the model has to answer for.
      menuList.AddHandler(PointerPressedEvent,
          delegate (object? sender, PointerPressedEventArgs args)
          {
            if (!_home.IsNereusHome
                || args.Pointer.Type != PointerType.Mouse
                || !args.Properties.IsLeftButtonPressed)
              return;

            _home.ChooseEntryNow();
          },
          // Handled events too: the tile's own ListBoxItem marks the press handled once it has selected itself,
          // and a bubble that stops there never reaches the rail.
          RoutingStrategies.Bubble,
          handledEventsToo: true);
      menuList.AddHandler(PointerMovedEvent,
          delegate (object? sender, PointerEventArgs args)
          {
            // A finger that travels is scrolling through the menu, not asking it for anything.
            Point at = args.GetPosition(menuList);
            if (_restingPress != null
                && (Math.Abs(at.X - _restingAt.X) > 12 || Math.Abs(at.Y - _restingAt.Y) > 12))
              StopRestingPress();
          },
          RoutingStrategies.Tunnel);
      menuList.AddHandler(PointerReleasedEvent,
          delegate { StopRestingPress(); }, RoutingStrategies.Tunnel);
      menuList.AddHandler(PointerCaptureLostEvent,
          delegate { StopRestingPress(); }, RoutingStrategies.Tunnel);

      // The remote's context key, which is the key the reference's menu list answered. The virtual keyboard
      // works the very same key over its text fields, so a key that arrives from a box being typed in is not a
      // request to rearrange the menu behind it.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Source is TextBox
                || !_home.IsNereusHome
                || !Settings.KeyBindings.Current.Is(Settings.KeyAction.Menu, args.Key))
              return;

            OpenMenuEditor();
            args.Handled = true;
          });

      // The user buttons beside the sign-in boxes: names arrive from the server while the screen stands
      // there, so the row is rebuilt when they do. Pressing one says the name and steps into the password
      // box, which is what picking a name at a door means.
      _home.SignInNames.CollectionChanged += delegate { RebuildSignInNames(); };
      RebuildSignInNames();

      // The four corner buttons of master_home.xaml's WindowButtonsPanel walk through the door this front end has
      // for what the reference's dialog did: the power menu is this build's close screen, the person button asks
      // who is watching, the cog is the settings list, and the search glass opens the shelves. They are the face's
      // own markup - hung by its home.xaml, carrying no name this file may hold - and what they open they say in
      // their Tag: "close", "viewer", "settings", "browse". The host answers every one of them here, and each is
      // an ordinary stop on the workflow, so Back lands the viewer right back on the home they came from. A face
      // that hangs no buttons never reaches this handler.
      homeOverlays.AddHandler(Button.ClickEvent,
          delegate (object? sender, RoutedEventArgs e)
          {
            if (e.Source is Button { Tag: string key } && key.Length > 0)
            {
              // A door the person watching is not allowed through does not open - which is also how the
              // reference answered it, by never hanging the handle there at all. The buttons already hide
              // themselves (ApplyScreenGates); this is the same rule read a second time at the press, so a
              // highlight that walked onto a still-visible button from before the last switch cannot get
              // further than the key it stands on.
              if (_opensScreen != null && !_opensScreen(key))
                return;

              OpenScreen(key);
            }
          },
          RoutingStrategies.Bubble);

      // Standing on that row with a remote is the same thing as standing on it with a mouse, so the row takes
      // part in the one measurement the screen makes for every arrow: it is a candidate of the walk while the
      // viewer is on the wall - which is what makes up from the topmost shortcut and right from the topmost
      // menu entry land on the button that really lies that way - and while the viewer stands on one of the
      // buttons themselves, the same measurement walks them along the row or back down onto the wall. A key
      // with nothing in its direction is left alone, so a face that hangs no row, or a direction that points
      // off the television, costs the viewer nothing.
      homeOverlays.AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (!_home.IsNereusHome || args.Handled
                || (args.Key != Key.Left && args.Key != Key.Right
                    && args.Key != Key.Up && args.Key != Key.Down))
              return;

            // The button the key was pressed on, rather than a search for whatever holds the focus: the key
            // arrived through this button, and that is the same thing the viewer is standing on.
            Button? stoodOn = args.Source as Button;
            if (stoodOn == null || !(stoodOn.Tag is string name) || name.Length == 0
                || !TopMenuRow().Any(button => ReferenceEquals(button, stoodOn)))
              return;

            // Up from the head of the screen pointed off the television. What the measurement answers with is a
            // menu entry standing somewhere above and to the side, and standing the viewer on that used to drag
            // the rail up under their feet - a step to another place, taken by a key that meant "one row higher".
            // The row answers for itself instead: it leans up out of its place and settles back, and the viewer
            // stays on the button they were standing on.
            if (args.Key == Key.Up)
            {
              LeanCornerRowOffTheTop();
              args.Handled = true;
              return;
            }

            Control? goingTo = NextStanding(stoodOn, args.Key);
            if (goingTo == null || ReferenceEquals(goingTo, stoodOn))
              return;

            if (goingTo is Button square && square.Tag is HomeContentTile)
              FocusNereusTile(square);
            else if (goingTo.Focus())
              goingTo.BringIntoView();
            else
              return;

            args.Handled = true;
          },
          RoutingStrategies.Tunnel,
          handledEventsToo: true);

      // On the cross, the four arrows are the cross itself, exactly as the reference's HomeMenuModel takes them:
      // up and down walk the <i>groups</i> - the main menus of the vertical arm - and left and right walk the
      // <i>submenus</i> of the group stood on, the horizontal arm. Neither wraps; both clamp at the ends like the
      // reference. Claimed before the ring handler below ever sees a key, and claimed wherever the keyboard stands
      // while WMC is worn - on the invisible menu, on a corner button, or on nowhere at all - because the menu the
      // cross paints is grouped while the invisible list underneath is flat, and its native up and down would walk
      // that flat list and disagree with the arms. Only the two lists beside the pane keep their ring (left walks
      // back out of them), and a text box being typed in - the sign-in's boxes live on this screen too - keeps its
      // cursor keys.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            bool vertical = args.Key == Key.Up || args.Key == Key.Down;
            bool horizontal = args.Key == Key.Left || args.Key == Key.Right;
            if (!vertical && !horizontal)
              return;
            if (_home.HomeLayout != "wmc"
                || contentList.IsKeyboardFocusWithin || aspectsList.IsKeyboardFocusWithin
                || args.Source is TextBox)
              return;

            // The cross owns the arrows, so the menu that wears it takes the keyboard back - Enter still opens
            // what the standing place stands on, and that answer lives in the list.
            menuList.Focus();

            if (vertical)
              _wmc.MoveGroup(args.Key == Key.Up ? -1 : 1);
            else
              _wmc.MoveSubItem(args.Key == Key.Left ? -1 : 1);
            args.Handled = true;
          },
          RoutingStrategies.Tunnel);

      // A vertical list takes left and right for its own selection - Avalonia moves the highlight with every
      // arrow key, not just up and down - so a viewer standing on the menu and pressing Right would only walk
      // their highlight one entry further and the ring would never see the key. Claimed on the screen while the
      // key tunnels in, exactly where the connection screen had to learn the same lesson: left and right are how
      // a viewer leaves one of these lists, while up and down stay its own.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Handled || (args.Key != Key.Left && args.Key != Key.Right))
              return;
            // Not the Nereus pane's: its tiles are walked by the geometric navigation on the pane itself,
            // which answers every direction by where the tiles stand - including the Left back to the menu
            // and the Right onto the first tile when the pane rather than a tile holds the focus. Handing
            // this pane to the ring instead would jump the viewer out of the content wall on every Right
            // and never let them walk from the shortcut column into the titles beside it.
            if (!menuList.IsKeyboardFocusWithin && !contentList.IsKeyboardFocusWithin
                && !aspectsList.IsKeyboardFocusWithin)
              return;

            // The rail is the leftmost thing the face wears, so a viewer standing on a menu entry and pointing
            // left pointed at nothing that exists. The ring measures where things stand and still answered with
            // one of the buttons at the foot of the television, which threw the viewer out of the entry whose
            // wall they were reading. The rail answers for itself, as the wall does at its ends: it leans off the
            // edge that was asked for and the viewer keeps the tile they stand on.
            if (_home.IsNereusHome && menuList.IsKeyboardFocusWithin && args.Key == Key.Left)
            {
              LeanMenuOffTheLeft();
              args.Handled = true;
              return;
            }

            Control? next = DpadNavigation.NextReachableInRing(ring, args.Key);
            if (next != null && next.Focus())
              args.Handled = true;
          },
          RoutingStrategies.Tunnel);

      // Enter asks who has the focus, and the view model decides what the press means: pressing a tile opens
      // that part of the library, pressing a menu entry that cannot open shows the reason on screen instead of
      // clicking against nothing, and pressing a row - half way watched or in the lists beside it - resumes
      // through the view model, which tells the server who is watching.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key != Key.Enter && args.Key != Key.Return)
              return;

            if (menuList.IsKeyboardFocusWithin && menuList.SelectedItem is MainMenuEntry entry)
            {
              // Pressing a tile is the answer Nereus' switches wait for: it moves the content pane when the
              // pane was told not to follow the walk, and it is the press the reference remembered.
              _home.CommitEntry();
              OpenEntry(entry);
              args.Handled = true;
            }
            else if (contentList.IsKeyboardFocusWithin && contentList.SelectedItem is HomeRow row
                && row.Title != null)
            {
              _home.ResumeCommand.Execute(row.Title);
              args.Handled = true;
            }
            else if (contentList.IsKeyboardFocusWithin && contentList.SelectedItem is HomeRow menuRow
                && menuRow.MenuEntry is MainMenuEntry otherEntry)
            {
              OpenEntry(otherEntry);
              args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel);

      // The screen fills itself in the first time it appears: a home screen that waits for someone to press
      // "Refresh" shows an empty list in front of a server that is answering perfectly. Returning from a
      // sub-screen does not re-read - the viewer left a screen they had just seen, not a stranger.
      _firstActivation = true;

      // The key that used to close the window is the shell's now: it pops the workflow, and at the root of the
      // stack that is the application's decision, not this screen's.
    }

    /// <summary>
    /// How far the shadow a tile lies on reaches past that tile's own edge: the skin writes it as an offset of
    /// three and four with a blur of sixteen, and the blur fades out a little further still than its own number.
    /// </summary>
    private const double MENU_SHADOW_ROOM = 24;

    /// <summary>
    /// The row of name buttons, rebuilt whenever the server's list arrives: one button per person, pressing
    /// one fills the name box and moves the highlight to the password. A picked name is still a name to
    /// prove - the buttons say WHO, the password box still asks whether.
    /// </summary>
    /// <summary>
    /// Moves the three areas of this screen into the arrangement the worn face asks for.
    /// </summary>
    /// <remarks>
    /// A skin is not only its pictures: Nereus and WMC each ship their home screen of
    /// <c>master_homemenu.xaml</c>, and the two arrange the very same things differently - Nereus with the
    /// tile rail flush at the screen edge, the content filling everything beside it under two black
    /// gradients (see the scrim overlays in the XAML), and WMC standing its menu well in from the edge
    /// (margin 300,50 of its 1280x720 frame) with the content pane hung top-right, right-aligned and
    /// capped at 1000x615. The reference's numbers are doubled for a 1440-high panel. The lists themselves,
    /// their entries, and the ring the remote walks never change - only where the areas stand.
    /// </remarks>
    private void ApplyHomeLayout()
    {
      switch (_home.HomeLayout)
      {
        case "nereus":
          // The reference's picture fills the whole television and its header floats over that picture,
          // so nothing of this layout may claim a strip of its own: the arrangement starts at the very
          // top of the screen, the menu rail runs the full height (master_homemenu.xaml spans its menu
          // column over the entire client), and the content column begins where the reference begins it -
          // 40px to the right of the rail (master_home.xaml's HomeContentGrid Margin="40,0,0,0"). Its
          // header row of 100 claims nothing here, because the header floats over the picture and the
          // wall is scaled to a share of the whole panel and stands centred in it - see ScaleNereusWall.
          homeLayout.Margin = new Thickness(0);
          homeAreas.ColumnDefinitions =
              ColumnDefinitions.Parse(FormattableString.Invariant($"{Math.Round(_home.MenuRailWidth)},*,0"));
          homeAreas.ColumnSpacing = 0;
          // The list is the one thing on this screen that cuts: its viewport is exactly as wide as the tiles it
          // holds, while the shadow the tiles lie on leans past their edge - measured on the running screen,
          // that was a straight line along the right side of every tile in the rail. The margin hands the
          // viewport the shadow's reach on both sides and Stretch makes the list own that widened place instead
          // of shrinking back to its content; the tiles themselves do not move, because a tile centres itself
          // in the wider item.
          menuList.Margin = new Thickness(-MENU_SHADOW_ROOM, 0, -MENU_SHADOW_ROOM, 0);
          // The reference lets its list stand overscanned (Height 1200 of 1080) so the item area always
          // fills the column top to bottom, half items bleeding into the viewport's fade at both edges.
          // On this taller panel a fixed 1200 would leave the rail's bottom third empty while its top was
          // full - the lopsided column the viewer sees as "compare the top with the bottom". Stretch makes
          // the list own the whole column height on every screen, which is what the overscan really means.
          menuList.Height = double.NaN;
          menuList.VerticalAlignment = VerticalAlignment.Stretch;
          menuList.HorizontalAlignment = HorizontalAlignment.Stretch;
          contentPane.Margin = new Thickness(0);
          // The reference's content begins forty to the right of its menu, and that forty is kept - but as the
          // wall's own margin now rather than as the pane's place, so that the pane itself starts where the menu
          // ends. The margin goes on the control the wall's scale rides on and not on the wall, because a margin
          // inside that scale would be forty times the scale and would leave the standing wall dimmed behind a
          // mask that is measured in the screen's own numbers. Nothing on the screen moved: the covers still
          // stand their forty from the menu. What changed is where a cover that walked off the wall's left ends:
          // it used to end on a straight line beside the menu column, and it now travels through the forty and
          // dissolves in it, so that nothing still wearing a cut arrives at the menu's edge. The same gap is kept
          // at the wall's far end, because the screen cannot be asked to cut the wall short of a section's own
          // margin: without it the last section could never stand before the right edge the way every section
          // before it does.
          nereusContentWall.Margin = new Thickness(
              HomeViewModel.CONTENT_GAP, 0, HomeViewModel.CONTENT_GAP, 0);
          nereusContentPane.OpacityMask = WALL_EDGE_FADE;
          contentPane.MaxWidth = double.PositiveInfinity;
          contentPane.HorizontalAlignment = HorizontalAlignment.Stretch;
          contentPane.VerticalAlignment = VerticalAlignment.Stretch;
          // The reference's pane is a wall: covers stand side by side and the row wraps, with a heading's
          // over-wide label starting each new band. A vertical list of covers would be a shelf, not a wall.
          contentList.ItemsPanel = new FuncTemplate<Panel?>(() => new WrapPanel());
          contentList.IsVisible = false;
          // A wall with nothing on it is not shown at all (AbstractHomeContent only ever handed the
          // ContentControl a wrapper when the wrapper HadItems).
          nereusContentPane.IsVisible = _home.ContentGroups.Count > 0;
          // The buttons of this row are not the reference's: master_home.xaml shows a player's footer and
          // the four corner buttons of power, user, settings and browse - no refresh, no now-playing, no
          // server. A TV screen refreshes itself on its own terms; the viewer leaves and walks back in.
          // (The row itself is emptied by the single rule at the bottom of this method.)
          break;

        case "wmc":
          // WMC stands its home on no columns at all: master_homemenu.xaml hangs a cross. The vertical arm
          // is the menu written in Huge text, the horizontal arm the same menu walked as pictures, and both
          // meet at the entry being stood on - which is what wmcCross above paints, filled by RebuildCross.
          // The menu list itself turns invisible here but stays exactly where it is: it is still the thing
          // the keyboard walks, still holds the selection, and still opens what Enter opens. The content
          // pane hangs where the reference hangs its HomeContentGrid - right-aligned, capped (1000x615
          // grown to this panel), slipping up under the header band with its own -150 doubled.
          homeLayout.Margin = new Thickness(0, 100, 60, 20);
          homeAreas.ColumnDefinitions = ColumnDefinitions.Parse("0,*,0");
          homeAreas.ColumnSpacing = 0;
          menuList.Margin = new Thickness(0);
          menuList.Height = double.NaN;
          menuList.VerticalAlignment = VerticalAlignment.Stretch;
          menuList.HorizontalAlignment = HorizontalAlignment.Stretch;
          contentPane.Margin = new Thickness(0, -100, 120, 0);
          contentPane.MaxWidth = 1900;
          contentPane.HorizontalAlignment = HorizontalAlignment.Right;
          contentPane.VerticalAlignment = VerticalAlignment.Top;
          contentList.ItemsPanel = PlainItemsPanel();
          contentList.IsVisible = true;
          nereusContentPane.IsVisible = false;
          break;

        default:
          homeLayout.Margin = new Thickness(48, 30);
          homeAreas.ColumnDefinitions = ColumnDefinitions.Parse("330,*,270");
          homeAreas.ColumnSpacing = 34;
          menuList.Margin = new Thickness(0);
          menuList.Height = double.NaN;
          menuList.VerticalAlignment = VerticalAlignment.Stretch;
          menuList.HorizontalAlignment = HorizontalAlignment.Stretch;
          contentPane.Margin = new Thickness(0);
          contentPane.MaxWidth = double.PositiveInfinity;
          contentPane.HorizontalAlignment = HorizontalAlignment.Stretch;
          contentPane.VerticalAlignment = VerticalAlignment.Stretch;
          contentList.ItemsPanel = PlainItemsPanel();
          contentList.IsVisible = true;
          nereusContentPane.IsVisible = false;
          break;
      }

      // The bottom row is the skin's to decide. Only master_home.xaml is silent about these buttons, and its
      // silent version of this screen has its own ways in (the corner buttons and the player's footer); under
      // every other face the buttons are offered as they always were - a button that opens nothing being
      // worse than no button, so each only while its screen can be built.
      bool nereusFace = _home.HomeLayout == "nereus";
      refreshButton.IsVisible = !nereusFace;
      nowPlayingButton.IsVisible = !nereusFace && _nowPlayingScreen != null;
      connectButton.IsVisible = !nereusFace && _connectScreen != null;
      // And with the row emptied there must be no gap left where it was: the reference's menu column runs to
      // the very bottom of the screen, so an empty row may not even reserve its margin.
      bottomRow.Margin = nereusFace ? new Thickness(0) : new Thickness(0, 16, 0, 0);

      // What the focused title whispers at the bottom of the screen starts where the content starts, not at
      // the corner of the television: master_homemenu.xaml hangs its MultiDetailsHome.inc inside the
      // content column (Grid.Column 1, fifty in), so the words begin beside the rail and never lie across
      // it. The other arrangements always had the line clear of their well-inset menus.
      contentInfoLine.Margin = nereusFace
          ? new Thickness(_home.HomeContentLeft, 0, 0, 34)
          : new Thickness(46, 0, 0, 34);

      // And the header: over a Nereus home it floats, so the fanart of this screen reaches the very top
      // of the television behind it, exactly as it does behind the empty HeaderPanel of master_home.xaml.
      _chromeShell?.SetChromeHeaderOverlay(nereusFace);

      // What a face paints for itself steps in front of what this screen offers as a fallback: a face that
      // answers the home's background or overlay keys in its own home.xaml has said what it wants there, and
      // the general fanart and the general line about the standing title have no business painting under it.
      plainBackground.IsVisible = homeBackground.Template == null;
      plainOverlays.IsVisible = homeOverlays.Template == null;

      // The cross is what a WMC home is made of and nothing else's - and it is WMC's own markup, hanging on
      // the menu-face host. Where no such face is worn the menu list puts its own face back on. RebuildCross
      // runs on every arrangement because walking onto the cross, off it, or onto another entry all pass
      // through here or through the view model's selection, and an arm showing yesterday's entry is worse
      // than an empty arm.
      menuList.Opacity = _home.HomeLayout == "wmc" ? 0 : 1;
      RebuildCross();
      ScaleNereusWall();
      SizeNereusMenu();
    }

    /// <summary>
    /// The way one wall leaves and its successor arrives: this wall is walked down out of the light, the model puts
    /// the next entry's wall - which it has read through before asking - on the dark screen, and the styles bring it
    /// back up. See <see cref="HomeViewModel.WallHandover"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only one wall is ever on the screen, and that is what makes the change cheap. Two walls dissolving through
    /// each other are the softer picture and the dearer one: every glass tile, every shadow and every cover is
    /// drawn twice for as long as the two lie on each other, which on this screen was a switch that stumbled.
    /// </para>
    /// <para>
    /// The wait below is the fade itself, not a pause added to it. A fade written and replaced in the same beat is
    /// never drawn at all, which is the cut every attempt here before this one looked like. It used to be paid
    /// twice over: the step sent its answer off half a second later and only then started the curve, so the viewer
    /// watched a wall standing, then a fade, then its successor. Now the step itself sends the wall down (see
    /// <see cref="BeginWallSink"/>) and this answer waits only for whatever the screen has not shown yet - which,
    /// the way down being shorter than the settling, is nothing. The way down and the reading of a successor run
    /// through the same half second, and the wall that leaves is already gone when its successor is written.
    /// </para>
    /// </remarks>
    private async Task SinkStandingWallAsync()
    {
      // Normally nothing happens here but the waiting: the step onto the next rail tile already sent this wall on
      // its way (BeginWallSink), and the reading of its successor ran while the curve was playing. What is paid is
      // what the screen has not shown yet, never the whole curve again.
      if (!_wallSinking)
      {
        // Nobody asked the wall down, which means the wall standing here is this entry's own - a re-read of the
        // entry that is already worn, or a wall arriving behind an empty area. Then the way down is this answer's
        // business and its whole length, because a successor put up in the same beat is a cut.
        BeginWallSink();
        if (!_wallSinking)
          return;
      }

      long left = WALL_SINK_MS + WALL_SINK_ROOM_MS - _wallSink.ElapsedMilliseconds;
      if (left > 0)
        await Task.Delay((int)left);
    }

    /// <summary>
    /// Starts the standing wall on its way down and remembers when, so that whoever picks it up afterwards waits
    /// for the rest of the curve rather than the whole of it.
    /// </summary>
    private void BeginWallSink()
    {
      // Already on its way down: a step further along the rail cannot take the same wall down twice, and the
      // clock of the first step is the one that stands.
      if (_wallSinking)
        return;

      // Nothing standing, nothing to take down: an empty area, or the first wall of a session. Its successor comes
      // up out of the fold on RaiseWall, and waiting for a fade with no picture to fade would only delay it.
      if (!_home.IsNereusHome || !nereusContentPane.IsVisible || _home.ContentGroups.Count == 0)
        return;

      // The way down is the shorter curve the styles block keeps under this class name; it is put on for the walk
      // down and taken off again on the way up, which leaves the wall wearing its rise curve at every other time.
      _wallSinking = true;
      _wallSink.Restart();
      nereusWallStanding.Classes.Add(WALL_SINKING_CLASS);
      nereusWallStanding.Opacity = 0;
    }

    /// <summary>
    /// The beat a wall's arrival is answered in: measured once for the whole shelf, then folded away or brought
    /// into the light. The model swaps a wall band by band and every band is a notification, so this runs at the end
    /// of the beat that did the swapping and looks at the wall the viewer is meant to see rather than at however
    /// many walls were standing on the way to it.
    /// </summary>
    private void PutFinishedWallInLight()
    {
      _wallArrivalQueued = false;

      // The share of the panel the wall owns is asked for once a wall, not once a band: writing the transform is
      // what tells the screen to measure the whole shelf again, and a band that left the number where it was has
      // no business sending the wall through that.
      ScaleNereusWall();

      if (!_home.IsNereusHome)
        return;

      if (_home.ContentGroups.Count == 0)
      {
        // Nothing to show is nothing to leave standing: the reference's home content is a ContentControl whose
        // content simply was not there when no list had items (AbstractHomeContent adds a list only when HasItems),
        // so the whole area took itself off and the fanart showed through. An invisible pane also drops out of the
        // remote's ring - DpadNavigation walks over what cannot take focus.
        FoldWall();
        return;
      }

      // Bands went on the wall: it comes into the light. A wall that is already standing in it stays as it is,
      // because writing the light something is already in starts no curve at all - so this is also the beat a
      // refreshed wall takes without flickering.
      RaiseWall();
    }

    /// <summary>
    /// Brings the wall into the light on the styles' own curve, and with it the area it stands in - which is also
    /// how the first wall of a session appears, since the fold and the view's own first beat leave it dark.
    /// </summary>
    private void RaiseWall()
    {
      // Visible first: a curve started behind an invisible pane is played out unseen and the wall then merely
      // appears, which is the one thing this exchange exists to prevent. The shorter curve comes off before the
      // light is written, so that the walk up is the wall's own and not the way down played backwards.
      nereusContentPane.IsVisible = true;
      nereusWallStanding.Classes.Remove(WALL_SINKING_CLASS);
      nereusWallStanding.Opacity = 1;

      // Standing in the light again, so the next step down is a new way down and not the tail of the old one.
      _wallSinking = false;
    }

    /// <summary>
    /// Takes the content area off the screen and leaves its wall in the dark it rises out of, so that whatever comes
    /// after an empty entry comes up on its curve instead of appearing at full light. The pane's own opacity is
    /// never written: it is the wall that fades, not the frame around it.
    /// </summary>
    private void FoldWall()
    {
      nereusContentPane.IsVisible = false;
      nereusContentPane.Offset = new Vector(0, nereusContentPane.Offset.Y);
      nereusWallStanding.Classes.Remove(WALL_SINKING_CLASS);
      nereusWallStanding.Opacity = 0;
    }

    /// <summary>
    /// Answers the arrow that pointed past the bottom of the wall with the wall itself: the whole content pane
    /// leans toward the edge that was asked for and finds its way back. Nobody loses the square they stand on and
    /// nothing steps out of the wall, because the answer to that key is "there is nothing below this" and not a
    /// step to somewhere else.
    /// </summary>
    /// <remarks>
    /// A transform on the pane rather than its scroll offset, which is what the pane's other curve moves: the
    /// content wall never scrolls vertically (<c>VerticalScrollBarVisibility="Disabled"</c>), so an offset there
    /// would be clamped to nothing. Moving the pane moves its clip with it, which makes the lean the wall
    /// pressing against the edge of its own area instead of being cut by it, and leaves every glass tile, shadow
    /// and cover where the face put them.
    /// </remarks>
    private void LeanWallOffTheBottom()
    {
      _wallRefusal.Lean(nereusContentPane, WALL_LEAN_DOWN);
    }

    /// <summary>
    /// The same answer at the other edge of the screen: an arrow that pointed above the row of corner buttons
    /// pointed off the television, so the row leans up out of its place and back rather than taking the viewer
    /// somewhere - which, before, it did by dragging the menu rail up an entry.
    /// </summary>
    private void LeanCornerRowOffTheTop()
    {
      Control? row = FacePart<StackPanel>(homeOverlays, TOP_MENU_ROW);
      if (row != null)
        _cornerRefusal.Lean(row, CORNER_LEAN_UP);
    }

    /// <summary>
    /// The answer at the wall's right end, where the wall runs out and the menu does not begin: the content is
    /// thrown off that end - which, being the right one, means thrown back into the screen - and settles. The
    /// viewer keeps their square, and the key says what it is true to say: there is nothing more to the right.
    /// </summary>
    private void LeanWallOffTheRight()
    {
      _wallEndRefusal.Lean(nereusContentPane, WALL_LEAN_RIGHT, sideways: true);
    }

    /// <summary>
    /// The answer at the menu's left end, where nothing lies at all: the rail is thrown off that end - which,
    /// being the left one, means thrown back into the screen - and settles. The entry the viewer stands on stays
    /// the entry they stand on, and the menu does not hand its entries out to a key that pointed off the
    /// television.
    /// </summary>
    private void LeanMenuOffTheLeft()
    {
      _menuRefusal.Lean(menuList, MENU_LEAN_LEFT, sideways: true);
    }

    /// <summary>
    /// The gesture that answers an arrow which asked for one more place than the screen has: the thing the viewer
    /// stands on is thrown toward that edge and settles back on its own, while the focus stays exactly where it
    /// was. One of these per edge, because each owns its transform and its beat.
    /// </summary>
    /// <remarks>
    /// The transform is put on once and its number moved afterwards, which is how the wake screen has nudged its
    /// PIN card from the beginning: a fresh transform per refusal asks the compositor for a layer over a gesture
    /// that lasts a quarter of a second. The beats are the dispatcher's timers, the same clock that shake runs
    /// on, and the first line of the lean is written on the beat the key arrived - the answer begins when the
    /// viewer pressed, not one beat later.
    /// </remarks>
    private sealed class LeanAtTheEnd
    {
      private readonly Avalonia.Media.TranslateTransform _lean = new Avalonia.Media.TranslateTransform();
      private DispatcherTimer? _beating;

      /// <summary>
      /// Throws <paramref name="thing"/> by <paramref name="howFar"/> lines down the television (a negative
      /// number throws it up) and lets it settle back. A thing already leaning leans no further: a key held down
      /// repeats, and a fresh throw on every repeat is a shiver rather than an answer. Pass
      /// <paramref name="sideways"/> to throw it across the television instead, which is what the edges at the
      /// sides of the screen ask for.
      /// </summary>
      public void Lean(Control thing, double howFar, bool sideways = false)
      {
        if (_beating != null)
          return;

        thing.RenderTransform = _lean;

        int step = 0;
        DispatcherTimer beating = new DispatcherTimer
        {
          Interval = TimeSpan.FromMilliseconds(LEAN_STEP_MS)
        };
        _beating = beating;
        beating.Tick += delegate { Step(); };
        Step();
        beating.Start();

        void Step()
        {
          double howMuch = step < LEAN_SHAPE.Length ? howFar * LEAN_SHAPE[step] : 0;
          if (sideways)
            _lean.X = howMuch;
          else
            _lean.Y = howMuch;

          if (step >= LEAN_SHAPE.Length)
          {
            beating.Stop();
            _beating = null;
          }

          step++;
        }
      }
    }

    /// <summary>
    /// Grows or shrinks the home's content wall to the share of the panel the worn skin asks for, and lets it
    /// stand centred there rather than hanging under a header row.
    /// </summary>
    /// <remarks>
    /// The shelf is measured once, in the reference's own numbers - its content grid was 880 of the reference's
    /// 1080 lines - and a panel that is taller cannot be filled by those numbers without the wall standing in
    /// the upper half of the screen with a third of the television empty under it. So the wall carries one
    /// transform instead of every tile being re-measured: headings, tiles, their gaps and their shadows keep
    /// exactly the proportion the face drew them in, and the share of the height the wall should own is the
    /// face's own number (<c>HomeWallHeightRatio</c>, eight tenths under Nereus) rather than a second one kept
    /// here. A wall with nothing on it or a panel whose size is not known yet is left alone; the size arrives
    /// with <see cref="Control.SizeChanged"/>, which asks for the same number again.
    /// </remarks>
    private void ScaleNereusWall()
    {
      if (_home.HomeLayout != "nereus")
      {
        _wallScale = 0;
        nereusContentWall.LayoutTransform = null;
        return;
      }

      double drawn = _home.ContentGroups.Count > 0
          ? _home.ContentGroups.Max(delegate (HomeContentGroup group) { return group.Height; })
          : 0;
      double panel = Bounds.Height;
      if (drawn <= 0 || panel <= 0)
        return;

      double share = SkinEngine.WornNumber("HomeWallHeightRatio") ?? 0.8;
      double scale = Math.Clamp(share * panel / drawn, 0.35, 2.5);

      // The number standing is remembered because a new transform object is a new answer to the screen: writing
      // one sends the whole shelf through a measure and an arrange, and every band of a wall being put on writes
      // the same number. Half a thousandth of a scale is nothing a television can show.
      if (_wallScale > 0 && Math.Abs(scale - _wallScale) < 0.0005)
        return;

      _wallScale = scale;
      nereusContentWall.LayoutTransform = new Avalonia.Media.ScaleTransform(scale, scale);
    }

    /// <summary>
    /// Sizes the main menu's rail to the panel it stands on, from the shares the worn face keeps in its metrics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A menu tile is square, so one number sizes it, and the column behind it has to be as wide as the tiles the
    /// rail carries - otherwise the ground the face paints under its menu stops short of the tiles it holds. Both
    /// numbers are read off the panel's HEIGHT and not its width: the rail is bounded by the lines standing under
    /// it, and a wide panel is not a tall one. <c>HomeMenuTileHeightRatio</c> and <c>HomeMenuRailWidthRatio</c>
    /// are the face's own measurements, exactly like the wall's share; a face that writes neither keeps the sizes
    /// it drew, which is what the plain arrangement and every face without a rail of its own show.
    /// </para>
    /// <para>
    /// Growing the tiles grows the whole rail, and eight entries at some panel sizes then stand over the bottom
    /// of the screen. The reference did the same on purpose - its list was 1200 lines of a 1080 television - and
    /// the rail answers it the same way it always did: it walks to whatever is stood on, and its viewport fades
    /// what leaves the screen instead of cutting it off.
    /// </para>
    /// </remarks>
    private void SizeNereusMenu()
    {
      double tile = 0;
      double rail = 0;
      if (_home.HomeLayout == "nereus" && Bounds.Height > 0)
      {
        tile = (SkinEngine.WornNumber("HomeMenuTileHeightRatio") ?? 0) * Bounds.Height;
        rail = (SkinEngine.WornNumber("HomeMenuRailWidthRatio") ?? 0) * Bounds.Height;
      }

      if (tile <= 0 || rail <= 0)
      {
        tile = HomeViewModel.MENU_TILE_DRAWN;
        rail = HomeViewModel.MENU_RAIL_DRAWN;
      }

      double sized = Math.Clamp(tile, 40, 900);
      double widened = Math.Clamp(rail, sized, 1000);
      if (sized == _home.MenuTileSize && widened == _home.MenuRailWidth)
        return;

      _home.MenuTileSize = sized;
      _home.MenuRailWidth = widened;
      if (homeAreas.ColumnDefinitions.Count == 3)
      {
        homeAreas.ColumnDefinitions[0].Width =
            new GridLength(Math.Round(widened), GridUnitType.Pixel);
      }
    }

    /// <summary>
    /// Hangs the worn face's own home on the three hosts this screen offers: the layers it stands on, the face
    /// its menu wears, and what it floats over the finished screen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing here knows what a Nereus or a WMC home looks like - that is written in <c>home.xaml</c> under
    /// each face's own roof, in its own folder, and read by <see cref="SkinEngine"/> the way every other file
    /// of a skin is. A face answers the keys it cares about and stays silent about the rest; a face that
    /// answers none leaves the general arrangement standing, hosts empty and fallbacks painted.
    /// </para>
    /// <para>
    /// This is the same road the window's chrome takes (<c>ShellWindow.WearSkinChrome</c>): a host whose
    /// template comes from the worn skin, re-hung whenever the skin changes, so swapping a face on a running
    /// front end repaints the home without a screen of C# being touched.
    /// </para>
    /// </remarks>
    private void WearSkinHome(object? sender, EventArgs args)
    {
      homeBackground.Template = SkinEngine.WornTemplate("HomeBackgroundTemplate");
      homeMenuFace.Template = SkinEngine.WornTemplate("HomeMenuFaceTemplate");
      homeOverlays.Template = SkinEngine.WornTemplate("HomeOverlaysTemplate");

      // A template that was just hung is not yet a visual tree, and the arms of a cross are filled from the
      // tree - so build it now and, because a host only realises what it is given once it stands on screen,
      // say so again when the layout has had its turn.
      homeMenuFace.ApplyTemplate();
      RebuildCross();
      Dispatcher.UIThread.Post(delegate
      {
        homeMenuFace.ApplyTemplate();
        RebuildCross();
      }, DispatcherPriority.Loaded);

      // The fallbacks step aside for whatever the face paints, and the general line about the standing title
      // comes back when a face says nothing about overlays.
      plainBackground.IsVisible = homeBackground.Template == null;
      plainOverlays.IsVisible = homeOverlays.Template == null;
      ApplyScreenGates();
      Dispatcher.UIThread.Post(ApplyScreenGates, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Takes the corner buttons of the worn face away from the screens the person watching may not reach -
    /// which is the reference's own answer to a restricted user's settings entry: it was never offered, not
    /// refused. A face that hangs no buttons has nothing to take away, and a house without restrictions
    /// leaves every button where it stands.
    /// </summary>
    private void ApplyScreenGates()
    {
      if (_opensScreen == null)
        return;

      homeOverlays.ApplyTemplate();
      // The buttons sit under the panel the face's markup roots its overlays in, so the walk has to go down
      // rather than only one level - a face is free to lay its corner out however it likes.
      foreach (Avalonia.Controls.Button button in homeOverlays.GetVisualDescendants()
          .OfType<Avalonia.Controls.Button>())
      {
        if (button.Tag is string key)
          button.IsVisible = _opensScreen(key);
      }
    }

    /// <summary>
    /// Says the gates over again - which is what the application does the moment the person watching moves,
    /// so the home the child walks into hides the cog without a word and the home the grown-up switches to
    /// has it back.
    /// </summary>
    public void ReapplyScreenGates()
    {
      ApplyScreenGates();
    }

    /// <summary>
    /// One of the parts the worn face hung on a host, found by the name its own markup gave it - a cross arm,
    /// the frame around the entry being stood on. Null while no face is worn, while the face's markup names
    /// nothing of that kind, or before the host has built what it was given.
    /// </summary>
    /// <remarks>
    /// Names rather than fields because a template built at run time belongs to no compiled view: the face
    /// decides what its parts are called, and this screen only ever asks for them by the name the face chose.
    /// </remarks>
    private static T? FacePart<T>(ContentControl host, string name) where T : Control
    {
      return host.GetVisualDescendants().OfType<T>().FirstOrDefault(control => control.Name == name);
    }

    private void ScrollSelectedMenuItem()
    {
      MainMenuEntry? selected = _home.SelectedEntry;
      if (!_home.IsNereusHome || selected == null)
        return;

      Dispatcher.UIThread.Post(delegate
      {
        ScrollViewer? scrollViewer = menuList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        Control? item = menuList.ContainerFromItem(selected) as Control;
        if (scrollViewer == null || item == null)
          return;

        Point? itemTop = item.TranslatePoint(new Point(0, 0), scrollViewer);
        if (itemTop == null)
          return;

        double maxOffset = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        double centeredOffset = scrollViewer.Offset.Y + itemTop.Value.Y
            + item.Bounds.Height / 2 - scrollViewer.Viewport.Height / 2;
        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, Math.Clamp(centeredOffset, 0, maxOffset));
      }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// The cross's arms refilled from WMC's own menu model: the <i>groups</i> written in Huge text above and below
    /// the standing place - the main menus, fading by distance the way the reference's dimmed menu does - the
    /// standing group's other <i>submenus</i> as their unfocused art left and right, and the standing submenu
    /// itself in the middle: Focus art in the Focus frame, its group's name Huge behind the frame where the
    /// reference writes the selected group, and its own name under the frame where the reference's selected
    /// subitem says itself. A submenu the skin has no art for still stands in the middle wearing its name; the
    /// reference's own selected state was only ever text.
    /// </summary>
    /// <remarks>
    /// The reference keeps this centring by scrolling both of its lists until the selection sits at their shared
    /// centre (<c>MyMainMenuListViewStyle</c> and <c>SubMenuListViewStyle</c>' auto-centring scroll viewers). With
    /// a window of a few arms' worth of entries there is nothing to scroll: the arms are rebuilt whole on every
    /// step, which is also what the reference's model does in its two-hundred millisecond animations - only at
    /// once, and so needing no timer to lose.
    /// </remarks>
    private void RebuildCross()
    {
      // The arms are the worn face's markup rather than this screen's: they hang on the menu-face host under
      // the names WMC's own home.xaml gave them, and where no face hangs a cross there is nothing to fill.
      StackPanel? above = FacePart<StackPanel>(homeMenuFace, "crossAbove");
      StackPanel? below = FacePart<StackPanel>(homeMenuFace, "crossBelow");
      StackPanel? left = FacePart<StackPanel>(homeMenuFace, "crossLeft");
      StackPanel? right = FacePart<StackPanel>(homeMenuFace, "crossRight");
      TextBlock? centerLabel = FacePart<TextBlock>(homeMenuFace, "crossCenterLabel");
      TextBlock? stoodName = FacePart<TextBlock>(homeMenuFace, "crossName");
      Image? frame = FacePart<Image>(homeMenuFace, "crossFrame");
      Image? focusArt = FacePart<Image>(homeMenuFace, "crossFocusArt");
      if (above == null || below == null || left == null || right == null)
        return;

      above.Children.Clear();
      below.Children.Clear();
      left.Children.Clear();
      right.Children.Clear();

      if (_home.HomeLayout != "wmc")
        return;

      if (_home.Menu.Count > 0 && _wmc.Groups.Count == 0)
      {
        // A menu that filled itself in without the cross worn - booted offline, where the rebuild answer never
        // arrives - still has to find its way onto the arms the moment WMC does.
        _wmc.Rebuild(_home.SelectedEntry);
      }

      if (_home.SelectedEntry == null && _home.Menu.Count > 0)
      {
        // A cross without a standing place is not a cross. The reference's HomeMenuModel never had this
        // moment - it restored its remembered action on every rebuild and always stood somewhere. This
        // front end remembers only where a skin's settings can keep it, so where nothing was remembered
        // (or the remembered one is gone from today's menu) the cross takes the first entry, the same
        // place the menu's own highlight falls into when a viewer presses an arrow first. Setting the
        // selection, rather than drawing from the first entry here, is deliberate: the selection is the
        // one answer the invisible list, the content pane and the arms all read from, and it refills the
        // pane exactly like a key press would - which ends up calling this method again, now standing.
        _home.SelectedEntry = _home.Menu[0];
        return;
      }

      if (_wmc.SelectedGroup == null)
      {
        if (centerLabel != null)
          centerLabel.Text = "";
        if (stoodName != null)
          stoodName.Text = "";
        if (frame != null)
          frame.Source = null;
        if (focusArt != null)
          focusArt.Source = null;
        return;
      }

      _wmc.AlignToSelection();
      IReadOnlyList<WmcHomeMenuGroup> groups = _wmc.Groups;
      int standingGroup = _wmc.SelectedGroup == null ? -1 : IndexOf(groups, _wmc.SelectedGroup);
      IReadOnlyList<MainMenuEntry> subItems = _wmc.SubItems;
      int standingSub = _home.SelectedEntry == null ? -1 : IndexOf(subItems, _home.SelectedEntry);

      var dim = this.FindResource("AccentBrush") as Avalonia.Media.IBrush;

      for (int d = 3; d >= 1; d--)
        if (standingGroup - d >= 0)
          above.Children.Add(CrossLabel(groups[standingGroup - d].Label, dim, d));
      for (int d = 1; d <= 3; d++)
        if (standingGroup + d < groups.Count)
          below.Children.Add(CrossLabel(groups[standingGroup + d].Label, dim, d));

      for (int d = 2; d >= 1; d--)
        if (standingSub - d >= 0)
          left.Children.Add(CrossNeighbour(subItems[standingSub - d], dim));
      for (int d = 1; d <= 2; d++)
        if (standingSub + d < subItems.Count)
          right.Children.Add(CrossNeighbour(subItems[standingSub + d], dim));

      MainMenuEntry? stood = standingSub >= 0 ? subItems[standingSub] : null;
      if (centerLabel != null)
        centerLabel.Text = _wmc.SelectedGroup?.Label ?? "";
      if (stoodName != null)
        stoodName.Text = stood?.Label ?? "";
      if (frame != null)
        frame.Source = CrossArt("Focus.Frame");
      if (focusArt != null)
        focusArt.Source = stood == null || string.IsNullOrEmpty(stood.HomeImageId)
            ? null
            : CrossArt($"HomeMenu/{stood.HomeImageId}Focus");
    }

    /// <summary>
    /// One name of the vertical arm, dimmed by how far it stands from the middle - the reference paints
    /// every unselected entry in the skin's quiet blue (MenuTextBackground) and only the stood-on one in
    /// white; the distance fade is this port's telling of which of the quiet ones the eye reads first.
    /// </summary>
    private static TextBlock CrossLabel(string? text, Avalonia.Media.IBrush? brush, int distance)
    {
      return new TextBlock
      {
        Text = text,
        FontSize = 110,
        Foreground = brush,
        TextAlignment = Avalonia.Media.TextAlignment.Center,
        Opacity = distance == 1 ? 0.9 : distance == 2 ? 0.55 : 0.3,
      };
    }

    /// <summary>
    /// One neighbour of the horizontal arm: the entry's NoFocus art with its name under it, which is the
    /// reference's unfocused subitem to the picture - and where the skin carries no art for the entry,
    /// its badge letter stands in the picture's place rather than a hole.
    /// </summary>
    private Control CrossNeighbour(MainMenuEntry entry, Avalonia.Media.IBrush? brush)
    {
      var tile = new StackPanel { Spacing = 6, Width = 490 };

      Avalonia.Media.Imaging.Bitmap? art = string.IsNullOrEmpty(entry.HomeImageId)
          ? null
          : CrossArt($"HomeMenu/{entry.HomeImageId}NoFocus");
      if (art != null)
      {
        tile.Children.Add(new Image
        {
          Source = art,
          Width = 490,
          Stretch = Avalonia.Media.Stretch.Uniform,
          Opacity = 0.85,
        });
      }
      else
      {
        tile.Children.Add(new TextBlock
        {
          Text = entry.Badge,
          FontSize = 60,
          Foreground = brush,
          Opacity = 0.7,
          HorizontalAlignment = HorizontalAlignment.Center,
          Height = 170,
          TextAlignment = Avalonia.Media.TextAlignment.Center,
          VerticalAlignment = VerticalAlignment.Bottom,
        });
      }

      tile.Children.Add(new TextBlock
      {
        Text = entry.Label,
        FontSize = 34,
        Foreground = brush,
        Opacity = 0.75,
        TextAlignment = Avalonia.Media.TextAlignment.Center,
        TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
      });
      return tile;
    }

    /// <summary>
    /// A picture of the worn skin by name, or null - read the same way the engine reads it, but as the control's
    /// <i>own</i> bitmap rather than the engine's cached one. The engine keeps its pictures only while the face
    /// is worn and frees every one of them when it comes off; a frame the view painted with the shared bitmap
    /// then stands in the visual tree over a released one, and the next render pass dies with an
    /// ObjectDisposedException no dispatcher trick can arrive early enough to prevent. Re-reading the file costs
    /// one small PNG and buys a bitmap nothing but this view ever releases.
    /// </summary>
    private static Avalonia.Media.Imaging.Bitmap? CrossArt(string name)
    {
      WornSkin? worn = SkinImageConverter.WornPictures();
      if (worn == null || !worn.Images.TryGetLocation(name, out string? location) || location == null)
        return null;
      try
      {
        return (Avalonia.Media.Imaging.Bitmap) SkinEngine.ReadImage(location);
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>Where an item stands in a read-only list - these lists hold an armful, and a walk is all the search they need.</summary>
    private static int IndexOf<T>(IReadOnlyList<T> items, T item)
    {
      for (int i = 0; i < items.Count; i++)
      {
        if (ReferenceEquals(items[i], item))
          return i;
      }
      return -1;
    }

    /// <summary>
    /// The pane's ordinary vertical arrangement, as a template.
    /// </summary>
    /// <remarks>
    /// An items presenter reads its panel template when it is applied and asks no question about it - handing it
    /// a null one after it has built itself once is how the screen used to die on its very first measure. So
    /// leaving the poster wall hands the list the vertical panel it would have had anyway, by name, rather than
    /// handing it nothing.
    /// </remarks>
    private static readonly ITemplate<Panel?> VerticalItemsPanel =
        new FuncTemplate<Panel?>(() => new StackPanel());

    private static ITemplate<Panel?> PlainItemsPanel()
    {
      return VerticalItemsPanel;
    }

    private void RebuildSignInNames()
    {
      signInNames.Children.Clear();
      foreach (string name in _home.SignInNames)
      {
        var button = new Button
        {
          Content = name,
          FontSize = 15,
          Padding = new Avalonia.Thickness(12, 4),
          Focusable = true
        };
        string picked = name;
        button.Click += delegate
        {
          _home.PickSignInName(picked);
          signInPassword.Focus();
        };
        signInNames.Children.Add(button);
      }
    }

    private bool _firstActivation;

    /// <summary>
    /// The menu takes the focus every time this screen is shown. Every action here is Enter asking who holds the
    /// focus, and a screen that appears without focus answers no key at all - on the way back from a sub-screen
    /// included, because the control comes back with the focus it gave away.
    /// </summary>
    public void OnScreenActivated()
    {
      if (_firstActivation)
      {
        _firstActivation = false;
        _home.ResumeRequested += OnResumeRequested;
        // The wake reads the library under its animation, so a screen opened under an answer that was read for
        // the person standing here has nothing left to ask - and asking would throw those answers out again
        // along with the menu containers the rebuild takes away.
        if (DataContext is HomeViewModel viewModel
            && !viewModel.IsBusy && !viewModel.ReadUnderThisViewer)
          viewModel.RefreshCommand.Execute(null);
      }
      // Asked before the screen is joined to the window, a focus request answers nothing at all - and on
      // the way in here it usually is asked that early, while the menu list cannot take it yet. The posted
      // second ask runs once the layout has settled, which is the moment the request starts to stick;
      // asking twice costs nothing, since the menu owns this screen's keyboard either way.
      // Asked before the screen is joined to the window, a focus request answers nothing at all - and on
      // the way in here it usually is asked that early, while the menu list cannot take it yet. The posted
      // second ask runs once the layout has settled, which is the moment the request starts to stick;
      // asking twice costs nothing, since the menu owns this screen's keyboard either way.
      menuList.Focus();
      Dispatcher.UIThread.Post(delegate { menuList.Focus(); }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// A reading of the server empties the menu and fills it again, and the container that stood under the
    /// focus goes with the list it stood in. Avalonia answers that by handing the focus to the window itself,
    /// which is what a viewer feels as the one key press that does nothing: their arrow is spent walking back
    /// into the window instead of along the menu. So the screen takes its menu back whenever a reading left
    /// nothing but the window standing on the screen.
    /// </summary>
    private void TakeTheMenuBackIfTheRebuildThrewItOut()
    {
      Window? window = TopLevel.GetTopLevel(this) as Window;
      if (window == null)
        return;
      object? standing = window.FocusManager.GetFocusedElement();
      if (standing != null && !ReferenceEquals(standing, window))
        return; // Somebody stands on a thing of their own; the menu is not owed the keys away from that.
      Dispatcher.UIThread.Post(delegate { menuList.Focus(); }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Only reached if the workflow ever replaces the root; the subscription is the one thing this screen holds
    /// that must not outlive it.
    /// </summary>
    public void OnScreenClosed()
    {
      _home.ResumeRequested -= OnResumeRequested;
    }

    /// <summary>
    /// Opens one tile: the titles of that part of the library, one stop deeper in the way.
    /// </summary>
    private void OpenSection(HomeSection section)
    {
      if (_sectionScreen == null)
        return;

      WorkflowFrame? frame = _sectionScreen(section);
      if (frame != null)
        _workflow.Push(frame);
    }

    private void OpenEntry(MainMenuEntry entry)
    {
      HomeSection? section = _home.Open(entry);
      if (section != null)
        OpenSection(section);
      else if (entry.ScreenKey != null)
        OpenScreen(entry.ScreenKey);
    }

    private void OnNereusTileClick(object? sender, RoutedEventArgs args)
    {
      if (sender is not Button button || button.Tag is not HomeContentTile tile)
        return;

      if (tile.Title != null)
      {
        _home.ResumeCommand.Execute(tile.Title);
        return;
      }

      if (tile.MenuEntry != null)
      {
        OpenEntry(tile.MenuEntry);
        return;
      }

      if (tile.Shortcut is not NereusHomeShortcut shortcut)
        return;

      if (shortcut.BrowseBy != null && _home.SelectedEntry?.Section is HomeSection section)
      {
        BrowseCategory? category = BrowseViewModel.CATEGORIES.FirstOrDefault(candidate =>
            string.Equals(candidate.Word, shortcut.BrowseBy, StringComparison.OrdinalIgnoreCase));
        if (category != null)
        {
          BrowseViewModel browse = new BrowseViewModel(_home.Client, new[] { section })
          {
            SelectedCategory = category
          };
          _workflow.Push(new WorkflowFrame("browse", "Browse " + shortcut.Label, browse));
          return;
        }
      }

      if (shortcut.ScreenKey != null && OPENABLE_SHORTCUT_SCREENS.Contains(shortcut.ScreenKey))
      {
        OpenScreen(shortcut.ScreenKey);
        return;
      }

      _home.Status = "The " + shortcut.Label + " shortcut is not available in this build.";
    }

    /// <summary>
    /// The screens a shortcut on the arm of Nereus' cross may open.
    /// </summary>
    /// <remarks>
    /// Named here rather than asked of the application, because the application answers a screen it cannot build
    /// with an exception - a tile naming a screen nobody builds is a bug in the front end, not something a viewer
    /// can be shown - and the tiles that have nothing behind them yet, the programme search among them, are a
    /// viewer's ordinary question. Those keep the sentence that says so.
    /// </remarks>
    private static readonly HashSet<string> OPENABLE_SHORTCUT_SCREENS = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "news", "weather", "tv", "radio", "tvwatch", "radiowatch", "schedules", "recordings"
    };

    private void OnNereusTileGotFocus(object? sender, RoutedEventArgs args)
    {
      if (sender is not Button button || button.Tag is not HomeContentTile tile)
        return;

      _wallStand = button;
      if (tile.Group != null)
        _lastTileInGroup[tile.Group] = button;

      if (tile.Title != null)
      {
        _home.SelectedContent = _home.ContentRows.FirstOrDefault(row => ReferenceEquals(row.Title, tile.Title));
      }

      // A wall that is going out of the screen is not stood anywhere; only the wall the viewer is reading is.
      if (tile.Group == null || !nereusWallStanding.GetVisualDescendants().Contains(button))
        return;

      // The wall is scrolled by section and not by square. Where the wall stands is decided by the section the
      // viewer is reading - "Last added", "Unwatched" - and never by the square under their feet, so that walking
      // along a section leaves the wall where it stands and a section is always seen whole, from its heading to
      // its last cover, instead of drifting a cover at a time until the section the viewer is reading is split
      // across the screen.
      // A wall is still being measured while its covers are arriving, and a section the wall has not measured yet
      // has no place to be stood in. Such a section is stood again on the beat the layout is answered in, because
      // the fallback below stands a wall by the square under the viewer's feet alone - which is exactly how a last
      // cover once came to be pressed flat against the edge of the television.
      HomeContentGroup band = tile.Group;
      if (StandBandAtTheEdge(band, button))
        return;

      button.BringIntoView();
      Button stoodOn = button;
      Dispatcher.UIThread.Post(
          delegate
          {
            if (ReferenceEquals(LastWallStand(), stoodOn))
              StandBandAtTheEdge(band, stoodOn);
          },
          DispatcherPriority.Loaded);
    }

    private Button[] GetFocusableNereusTiles()
    {
      return nereusContentPane.GetVisualDescendants()
          .OfType<Button>()
          // IsEffectivelyVisible rather than IsVisible: a tile's template carries the surface it is not wearing
          // (a cover keeps its glass branch and a shortcut its cover branch) folded away, and the folded one is
          // still 'visible' on itself while being hidden by its parent. Standing on it from the menu would aim at
          // a button that cannot be stood on and leave the viewer where they were.
          .Where(button => button.Focusable && button.IsEffectivelyVisible && button.IsEnabled)
          .ToArray();
    }

    /// <summary>
    /// Where an arrow key goes from a square of the wall: the reference's own measurement over everything the
    /// viewer could land on, which is the wall's squares and the menu rail's entries together - one screen, one
    /// walk, the way MP2 answers its arrows rather than one rule for the wall and a favour for the menu.
    /// </summary>
    /// <remarks>
    /// Measuring rects instead of centres is the whole of the fix. The shortcut squares are a third of the size
    /// of the covers beside them, so their centres sit nearer the middle of the screen than the centres of the
    /// covers a row above or below: pressing up from the top row of covers used to leave the wall for a shortcut,
    /// and down from the bottom row did the same. Under the reference's test a square standing beside the one
    /// the viewer is on is not above it, however near its centre comes.
    /// </remarks>
    private Control? NextOnWall(Button from, Button[] tiles, Key direction)
    {
      Rect here = ScreenRect(from);
      if (here.Width <= 0 || here.Height <= 0)
        return null;

      Control? next = NextStanding(from, direction, tiles);

      // The reference's <c>RestoreFocus</c>: a list a viewer has already walked hands back the item they last
      // stood on when the focus re-enters it, and only when that item really does lie in the direction they
      // asked for - the remembered element is offered to the walk as a candidate, never bolted on afterwards.
      // Here the "list" is a band of the wall, because this wall holds several bands where MP2's home had
      // several lists: leaving the shortcuts for the covers and coming back lands on the shortcut that was
      // stood on before, not on the first one.
      if (next is Button square && square.Tag is HomeContentTile tile && tile.Group != null
          && !ReferenceEquals(tile.Group, GroupOf(from))
          && _lastTileInGroup.TryGetValue(tile.Group, out Button? remembered)
          && remembered != null
          && !ReferenceEquals(remembered, next)
          && tiles.Contains(remembered)
          && SkinFocusWalk.LiesIn(direction, ScreenRect(remembered), here, out _))
        return remembered;

      // The menu is one place with one standing entry, not a column of squares to be picked by height. The
      // measurement still answers whether the arrow leads into the menu at all - only the entry it names is
      // asked again, and asked of the rail: the entry the viewer last stood on is what the menu hands back, and
      // before they ever stood on it, that is the entry whose wall they are looking at. Landing on whichever
      // entry happens to hang beside the square the key came from threw both away, and scrolled the rail to it.
      if (next is ListBoxItem)
      {
        ListBoxItem? standing = StandingRailEntry(direction, here);
        if (standing != null)
          return standing;
      }

      return next;
    }

    /// <summary>
    /// The entry of the menu rail that an arrow walking into the rail stands on: the one the viewer last stood
    /// on, or while they never have, the entry the rail is showing. <c>null</c> when neither can be had - the
    /// menu changed under the viewer, or the rail will not realize that entry - in which case the answer the
    /// measurement gave stands, which is at least an entry of the menu.
    /// </summary>
    /// <remarks>
    /// The entry is brought into view before its place is measured, because an entry the rail has scrolled away
    /// has no place to compare: reaching the menu scrolls its list anyway, so this asks for nothing the answer
    /// would not have done a moment later. The direction is still asked about, so that an arrow which the walk
    /// sent into the menu does not get answered by an entry that stands back where the viewer came from.
    /// </remarks>
    private ListBoxItem? StandingRailEntry(Key direction, Rect from)
    {
      int index = -1;
      if (_lastRailStand != null)
      {
        for (int at = 0; at < menuList.Items.Count; at++)
        {
          if (ReferenceEquals(menuList.Items[at], _lastRailStand))
          {
            index = at;
            break;
          }
        }
      }

      if (index < 0)
        index = menuList.SelectedIndex;
      if (index < 0 || index >= menuList.Items.Count)
        return null;

      menuList.ScrollIntoView(index);
      if (menuList.ContainerFromIndex(index) is not ListBoxItem entry)
        return null;

      Rect standing = ScreenRect(entry);
      return standing.Width > 0 && standing.Height > 0
          && SkinFocusWalk.LiesIn(direction, standing, from, out _)
          ? entry
          : null;
    }

    /// <summary>
    /// What the given arrow points at from wherever the viewer stands, measured over the whole screen at once:
    /// the wall's squares, the menu rail's entries and the face's row of corner buttons are candidates of one
    /// and the same walk, as they are in the reference, which measures every focusable element of the screen
    /// together rather than one list against the next.
    /// </summary>
    private Control? NextStanding(Control from, Key direction, Button[]? tiles = null)
    {
      Rect here = ScreenRect(from);
      if (here.Width <= 0 || here.Height <= 0)
        return null;

      List<Control> candidates = new List<Control>(tiles ?? GetFocusableNereusTiles());
      candidates.AddRange(MenuRailEntries());
      candidates.AddRange(TopMenuRow());
      return SkinFocusWalk.Next(candidates, ScreenRect, here, direction);
    }

    /// <summary>
    /// The row of buttons the worn face hangs at the top of the screen - the power, the person, the cog and the
    /// search glass - as what they are to a remote: places a viewer stands on. They carry no name, only their
    /// Tag, which is also how the press reads them.
    /// </summary>
    private IEnumerable<Button> TopMenuRow()
    {
      if (!homeOverlays.IsEffectivelyVisible)
        yield break;

      foreach (Button button in homeOverlays.GetVisualDescendants().OfType<Button>())
        if (button.Tag is string key && key.Length > 0 && button.Focusable
            && button.IsEffectivelyVisible && button.IsEnabled)
          yield return button;
    }

    /// <summary>
    /// The button of the top row that stands beside the entry the viewer is on, when one does: the row hangs at
    /// the head of the screen, so only the head of the menu has it beside it, and every lower entry keeps its
    /// Right key for the way back to the wall.
    /// </summary>
    private Button? BesideTheRow(ListBoxItem? entry)
    {
      if (entry == null)
        return null;

      Rect here = ScreenRect(entry);
      if (here.Width <= 0 || here.Height <= 0)
        return null;

      return TopMenuRow()
          .Where(button =>
          {
            Rect standing = ScreenRect(button);
            return standing.Width > 0 && standing.Right > here.Right
                && standing.Top < here.Bottom && standing.Bottom > here.Top;
          })
          .OrderBy(button => ScreenRect(button).X)
          .FirstOrDefault();
    }

    /// <summary>The band a square of the wall stands in, or <c>null</c> for a control that is not one.</summary>
    private static HomeContentGroup? GroupOf(Control? control)
    {
      return control is Button button && button.Tag is HomeContentTile tile ? tile.Group : null;
    }

    /// <summary>A square's bounds in this screen's own coordinates, the wall's scale and all.</summary>
    private Rect ScreenRect(Control control)
    {
      Point? topLeft = control.TranslatePoint(new Point(), this);
      Point? bottomRight = control.TranslatePoint(
          new Point(control.Bounds.Width, control.Bounds.Height), this);
      if (topLeft == null || bottomRight == null)
        return default;
      return new Rect(topLeft.Value, bottomRight.Value);
    }

    /// <summary>The menu rail's entries, as what the reference treats them as: controls on the same screen.</summary>
    private IEnumerable<ListBoxItem> MenuRailEntries()
    {
      if (!menuList.IsVisible)
        yield break;

      foreach (ListBoxItem entry in menuList.GetVisualDescendants().OfType<ListBoxItem>())
        if (entry.Focusable && entry.IsEffectivelyVisible && entry.IsEnabled)
          yield return entry;
    }

    /// <summary>
    /// Puts the focus onto an entry of the menu rail rather than onto the bare list, which shows the viewer a
    /// frame they can see and gives the arrow keys something to move. The remembered entry is the one the rail
    /// is already showing; a rail that never chose anything starts at its first.
    /// </summary>
    private bool StandOnMenuEntry()
    {
      if (!menuList.IsVisible || menuList.Items.Count == 0)
        return false;

      // Not `SelectedIndex = 0` when nothing is selected: the rail's selection is the standing entry, and
      // stepping onto the menu by accident would else switch the whole wall. The frame goes where the arrow
      // points; only pressing changes what the screen is showing.
      int index = menuList.SelectedIndex >= 0 ? menuList.SelectedIndex : 0;
      menuList.ScrollIntoView(index);

      return (menuList.ContainerFromIndex(index) as Control)?.Focus() ?? menuList.Focus();
    }

    /// <summary>The square the viewer last stood on, as long as it is still on the wall.</summary>
    private Button? LastWallStand()
    {
      Button? stood = _wallStand;
      return stood != null && nereusContentPane.GetVisualDescendants().Contains(stood) ? stood : null;
    }

    private Button? _wallStand;
    private readonly Dictionary<HomeContentGroup, Button> _lastTileInGroup =
        new Dictionary<HomeContentGroup, Button>();

    /// <summary>
    /// The entry of the menu rail the viewer last stood on. Kept as the menu entry rather than as the container
    /// that was stood on, because the rail throws its containers away whenever the menu is rebuilt and a viewer
    /// returning to the menu means the entry, not the piece of chrome that happened to show it.
    /// </summary>
    private MainMenuEntry? _lastRailStand;

    /// <summary>
    /// The wall's left edge: the content gap's width of screen in which a cover that has walked off the wall
    /// dissolves, so that the wall gives its covers back to the picture behind them rather than cutting them at
    /// a straight line beside the menu. A mask and not a veil of black, because what lies behind the wall is the
    /// entry's own artwork, and that is what a departing cover should sink into.
    /// </summary>
    private static readonly Avalonia.Media.LinearGradientBrush WALL_EDGE_FADE =
        new Avalonia.Media.LinearGradientBrush
        {
          StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Absolute),
          EndPoint = new Avalonia.RelativePoint(
              HomeViewModel.CONTENT_GAP, 0, Avalonia.RelativeUnit.Absolute),
          GradientStops =
          {
            new Avalonia.Media.GradientStop(Avalonia.Media.Colors.Transparent, 0),
            new Avalonia.Media.GradientStop(Avalonia.Media.Color.Parse("#40FFFFFF"), 0.45),
            new Avalonia.Media.GradientStop(Avalonia.Media.Colors.White, 1)
          }
        };

    private void FocusNereusTile(Button tile)
    {
      // Where the wall stands is not said here: the wall is stood by the square taking the focus, which is the one
      // moment every way onto the wall - an arrow, a finger, the focus coming back - passes through.
      tile.Focus();
    }

    /// <summary>
    /// Stands the section the viewer is reading with its own edge against the right of the screen, which is where
    /// a section is read: whole, with the gap the face writes between two sections breathing after it, and only
    /// what comes after that gap cut by the edge.
    /// </summary>
    /// <returns>
    /// Whether the section was stood. A section whose place is unknown cannot be stood, and a section too wide for
    /// the screen is stood by the square the viewer stands on instead - which still keeps the gap, but is no longer
    /// the section standing whole.
    /// </returns>
    /// <remarks>
    /// Measured on the section's own container, because a container already carries that gap as its trailing
    /// margin - the band's Grid in <c>HomeView.axaml</c> - so bringing the container's right edge to the
    /// viewport's leaves exactly the margin a section keeps to its neighbour, and nobody has to hold the number in
    /// two places. The gap itself is read off the difference between that container and the box inside it, so that
    /// it is the wall's own number in the screen's own numbers rather than one kept here. The section stood is the
    /// one the viewer is reading, not whatever already leans over the edge: a section that had merely begun to show
    /// would tear the reader's own off the screen. The pane's offset is what moves, which is the same number the
    /// wall's own easing curve runs on.
    /// </remarks>
    private bool StandBandAtTheEdge(HomeContentGroup band, Control? stood)
    {
      double viewport = nereusContentPane.Viewport.Width;
      if (!nereusContentPane.IsVisible || viewport <= 0)
        return false;

      // Measured inside the wall and not inside the pane. A corner handed to the pane comes back at the place the
      // pane has reached so far, and a wall that is still gliding to a section has reached somewhere else than the
      // place it was asked for; a corner handed to the wall is the same number however far the glide has got.
      // The wall's own left margin is where its numbers end and the pane's begin, and the wall carries its scale,
      // so what a control calls its own width is not the width it stands on the television with - which is how a
      // wall scaled away from one once stood its last cover flat against the edge of the screen.
      double wallLeft = Math.Max(0, nereusContentWall.Margin.Left);

      foreach (Control? container in nereusWallStanding.GetRealizedContainers())
      {
        if (container == null || !ReferenceEquals(container.DataContext, band))
          continue;

        double? end = RightOnWall(container);
        double? beginning = LeftOnWall(container);
        if (end == null || beginning == null)
          return false;

        double width = end.Value - beginning.Value;
        double right = wallLeft + end.Value;

        // The gap the section keeps to whatever stands beside it, in the same numbers: the difference between the
        // container and the box the face drew inside it, which is the band's own trailing margin.
        Control? drawn = container.GetVisualDescendants().OfType<Panel>()
            .FirstOrDefault(panel => ReferenceEquals(panel.DataContext, band));
        double? boxEnd = drawn == null ? null : RightOnWall(drawn);
        double gap = boxEnd == null ? 0 : Math.Max(0, end.Value - boxEnd.Value);

        if (width > viewport)
        {
          // A section too wide for the screen cannot be shown whole at all, so the square the viewer stands on is
          // stood - and stood with the same gap, because the edge of a television is not a place to press a cover
          // flat against.
          double? stoodEnd = stood == null ? null : RightOnWall(stood);
          if (stoodEnd != null)
            right = Math.Min(right, wallLeft + stoodEnd.Value + gap);
        }

        // No gap is added here: the container's own edge already carries it, so standing that edge at the edge of
        // the screen is what leaves the room the face writes between two sections breathing after the last cover.
        double wanted = right - viewport;
        double furthest = Math.Max(0, nereusContentPane.Extent.Width - viewport);
        nereusContentPane.Offset = new Vector(Math.Clamp(wanted, 0, furthest), nereusContentPane.Offset.Y);
        return true;
      }

      return false;

      // The two corners of a control, counted by the wall it stands on. A control's Bounds are counted from its
      // parent's corner, so handing Bounds.Right to a translate would count its place on the wall a second time.
      double? RightOnWall(Control control)
      {
        Point? corner = control.TranslatePoint(
            new Point(control.Bounds.Width, control.Bounds.Height), nereusContentWall);
        return corner?.X;
      }

      double? LeftOnWall(Control control)
      {
        Point? corner = control.TranslatePoint(new Point(), nereusContentWall);
        return corner?.X;
      }
    }

    private DispatcherTimer? _restingPress;
    private Point _restingAt;

    /// <summary>
    /// Avalonia has no long press, so this is one: the press is timed where it landed, a finger that leaves early
    /// or strays off the tile cancels it, and only a finger that rests there the whole while says what the
    /// reference's context key said.
    /// </summary>
    private void BeginRestingPress(Point at)
    {
      StopRestingPress();
      _restingAt = at;

      DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) };
      timer.Tick += delegate
      {
        timer.Stop();
        _restingPress = null;
        OpenMenuEditor();
      };
      _restingPress = timer;
      timer.Start();
    }

    private void StopRestingPress()
    {
      DispatcherTimer? timer = _restingPress;
      _restingPress = null;
      timer?.Stop();
    }

    /// <summary>
    /// Asks for Nereus' own home menu editor, standing where the viewer stands: the reference passed the tile the
    /// viewer was on to the editor as its starting point (<c>HomeMenuModel.BeginMenuEdit</c>), and the application
    /// builds the same thing behind the <c>menuedit</c> screen key. An ordinary stop on the workflow, so Back
    /// steps off it and the header over the screen names it.
    /// </summary>
    private void OpenMenuEditor()
    {
      OpenScreen("menuedit");
    }

    /// <summary>
    /// Opens one of the screens the menu names, "folders" and "themes" among them.
    /// </summary>
    /// <remarks>
    /// Pushed onto the workflow rather than shown beside this screen: pressing Back has to leave the viewer
    /// where they were, and the header over the screen says what they are standing on.
    /// </remarks>
    private void OpenScreen(string screenKey)
    {
      if (_screenFactory == null)
        return;

      WorkflowFrame? frame = _screenFactory(screenKey);
      if (frame != null)
        _workflow.Push(frame);
    }

    /// <summary>
    /// A row of the "left half way" strip or of the content wall was pressed. The application is the one that
    /// starts the film - it owns the player - so this screen only gets out of the way and shows the playback,
    /// the same screen the "Now playing" button shows. A title that holds others is the one exception: the
    /// application opens what stands inside it instead of playing, and a player screen pushed beside an
    /// album that just opened is a screen about a film that was never started.
    /// </summary>
    private void OnResumeRequested(object? sender, TitleViewModel title)
    {
      if (title.HoldsTitles)
        return;

      PushNowPlaying();
    }

    /// <summary>
    /// Opens the playback screen. The frame shown is the application's, so a viewer who goes back to the library
    /// and returns finds the same film at the same position.
    /// </summary>
    private void OnNowPlaying(object? sender, RoutedEventArgs e)
    {
      PushNowPlaying();
    }

    /// <summary>
    /// Shows the playback screen over this one.  The application hands out one frame for the playback - the film
    /// belongs to the application, not to the screen that opened it - so a screen already standing on the
    /// playback does not push a second copy of it.
    /// </summary>
    private void PushNowPlaying()
    {
      if (_nowPlayingScreen == null)
        return;

      WorkflowFrame? frame = _nowPlayingScreen();
      if (frame == null)
        return;

      if (_workflow.Current != null && _workflow.Current.StateId == frame.StateId)
        return;

      _workflow.Push(frame);
    }

    /// <summary>
    /// Opens the screen that picks the server. One stop deeper rather than instead of: a viewer who changes their
    /// mind and presses Back has to be left where they were, with the server they had not replaced still filling
    /// the screen.
    /// </summary>
    private void OnConnect(object? sender, RoutedEventArgs e)
    {
      if (_connectScreen == null)
        return;

      WorkflowFrame? frame = _connectScreen();
      if (frame != null)
        _workflow.Push(frame);
    }
  }
}
