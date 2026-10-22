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
        Func<string, WorkflowFrame>? screenFactory = null)
    {
      DataContext = home;
      _home = home;
      _wmc = new WmcHomeMenuModel(() => _home.Menu, () => _home.SelectedEntry, entry => _home.SelectedEntry = entry);
      _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
      _nowPlayingScreen = nowPlayingScreen;
      _connectScreen = connectScreen;
      _sectionScreen = sectionScreen;
      _screenFactory = screenFactory;
      InitializeComponent();

      // One wall on the screen, and the change of entries is this wall going out and its successor coming up. The
      // model reads a wall where the screen cannot see it and then asks this view for the way between the two:
      // SinkStandingWallAsync below, and RaiseWall where the finished wall arrives. The wall starts in the dark so
      // that even the first wall of a session comes up on its curve rather than appearing.
      nereusWallStanding.Opacity = 0;
      _home.WallHandover = SinkStandingWallAsync;

      home.ContentGroups.CollectionChanged += delegate
      {
        if (!_home.IsNereusHome)
          return;

        if (home.ContentGroups.Count == 0)
        {
          // Nothing to show is nothing to leave standing: the reference's home content is a ContentControl
          // whose content simply was not there when no list had items (AbstractHomeContent adds a list only
          // when HasItems), so the whole area took itself off and the fanart showed through. An invisible
          // pane also drops out of the remote's ring - DpadNavigation walks over what cannot take focus.
          FoldWall();
          return;
        }

        // Bands went on the wall: it comes into the light. A wall that is already standing in it stays as it is,
        // because writing the light something is already in starts no curve at all - so this is also the beat a
        // refreshed wall takes without flickering.
        RaiseWall();
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
      // arrives later still, from the server, and until it has there is no drawn height to scale from.
      SizeChanged += delegate { ScaleNereusWall(); SizeNereusMenu(); };
      _home.ContentGroups.CollectionChanged +=
          delegate { ScaleNereusWall(); };

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

            if (GetFocusableNereusTiles().FirstOrDefault() is Button firstTile)
            {
              FocusNereusTile(firstTile);
              args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel,
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
              if (args.Key == Key.Left)
              {
                menuList.Focus();
                args.Handled = true;
              }
              else if (tiles.FirstOrDefault() is Button firstTile)
              {
                FocusNereusTile(firstTile);
                args.Handled = true;
              }
              return;
            }

            Button? nextTile = FindDirectionalNereusTile(focusedTile, tiles, args.Key);
            if (nextTile != null)
            {
              FocusNereusTile(nextTile);
              args.Handled = true;
            }
            else if (args.Key == Key.Left)
            {
              menuList.Focus();
              args.Handled = true;
            }
            else if (args.Key == Key.Right && refreshButton.IsVisible && refreshButton.Focus())
            {
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
              OpenScreen(key);
          },
          RoutingStrategies.Bubble);

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
          menuList.Margin = new Thickness(0);
          // The reference lets its list stand overscanned (Height 1200 of 1080) so the item area always
          // fills the column top to bottom, half items bleeding into the viewport's fade at both edges.
          // On this taller panel a fixed 1200 would leave the rail's bottom third empty while its top was
          // full - the lopsided column the viewer sees as "compare the top with the bottom". Stretch makes
          // the list own the whole column height on every screen, which is what the overscan really means.
          menuList.Height = double.NaN;
          menuList.VerticalAlignment = VerticalAlignment.Stretch;
          menuList.HorizontalAlignment = HorizontalAlignment.Center;
          contentPane.Margin = new Thickness(40, 0, 0, 0);
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
    /// The wait below is the fade itself, not a pause added to it. The write starts the curve and going back to the
    /// dispatcher lets it be drawn, so by the time this answer comes back the viewer has watched the wall go out
    /// and its successor has a dark screen to come up on. A fade written and replaced in the same beat is never
    /// drawn at all, which is the cut every attempt here before this one looked like.
    /// </para>
    /// </remarks>
    private async Task SinkStandingWallAsync()
    {
      // Nothing standing, nothing to take down: an empty area, or the first wall of a session. Its successor comes
      // up out of the fold on RaiseWall, and waiting for a fade with no picture to fade would only delay it.
      if (!_home.IsNereusHome || !nereusContentPane.IsVisible || _home.ContentGroups.Count == 0)
        return;

      // The way down is the shorter curve the styles block keeps under this class name; it is put on for the walk
      // down and taken off again on the way up, which leaves the wall wearing its rise curve at every other time.
      nereusWallStanding.Classes.Add(WALL_SINKING_CLASS);
      nereusWallStanding.Opacity = 0;

      await Task.Delay(WALL_SINK_MS + 40);
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
        if (DataContext is HomeViewModel viewModel)
          viewModel.RefreshCommand.Execute(null);
      }
      // Asked before the screen is joined to the window, a focus request answers nothing at all - and on
      // the way in here it usually is asked that early, while the menu list cannot take it yet. The posted
      // second ask runs once the layout has settled, which is the moment the request starts to stick;
      // asking twice costs nothing, since the menu owns this screen's keyboard either way.
      menuList.Focus();
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

      if (shortcut.ScreenKey == "news" || shortcut.ScreenKey == "weather")
      {
        OpenScreen(shortcut.ScreenKey);
        return;
      }

      _home.Status = "The " + shortcut.Label + " shortcut is not available in this build.";
    }

    private void OnNereusTileGotFocus(object? sender, RoutedEventArgs args)
    {
      if (sender is Button button && button.Tag is HomeContentTile tile && tile.Title != null)
      {
        _home.SelectedContent = _home.ContentRows.FirstOrDefault(row => ReferenceEquals(row.Title, tile.Title));
      }
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

    private Button? FindDirectionalNereusTile(Button current, IEnumerable<Button> tiles, Key direction)
    {
      Point? currentCenter = current.TranslatePoint(
          new Point(current.Bounds.Width / 2, current.Bounds.Height / 2), this);
      if (currentCenter == null)
        return null;

      Button? nearest = null;
      double bestScore = double.MaxValue;
      foreach (Button candidate in tiles)
      {
        if (ReferenceEquals(candidate, current))
          continue;

        Point? candidateCenter = candidate.TranslatePoint(
            new Point(candidate.Bounds.Width / 2, candidate.Bounds.Height / 2), this);
        if (candidateCenter == null)
          continue;

        double dx = candidateCenter.Value.X - currentCenter.Value.X;
        double dy = candidateCenter.Value.Y - currentCenter.Value.Y;
        double primary;
        double secondary;
        switch (direction)
        {
          case Key.Left:
            primary = -dx;
            secondary = Math.Abs(dy);
            break;
          case Key.Right:
            primary = dx;
            secondary = Math.Abs(dy);
            break;
          case Key.Up:
            primary = -dy;
            secondary = Math.Abs(dx);
            break;
          case Key.Down:
            primary = dy;
            secondary = Math.Abs(dx);
            break;
          default:
            return null;
        }

        if (primary <= 0)
          continue;

        double score = primary + secondary * 1.5;
        if (score < bestScore)
        {
          nearest = candidate;
          bestScore = score;
        }
      }

      return nearest;
    }

    private void FocusNereusTile(Button tile)
    {
      if (tile.Focus())
        tile.BringIntoView();
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
    /// steps off it and the trail above the screen names it.
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
    /// where they were, and the trail above the screen shows exactly how deep that is.
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
    /// A row of the "left half way" strip was pressed. The application is the one that starts the film - it owns
    /// the player - so this screen only gets out of the way and shows the playback, the same screen the
    /// "Now playing" button shows.
    /// </summary>
    private void OnResumeRequested(object? sender, TitleViewModel title)
    {
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
