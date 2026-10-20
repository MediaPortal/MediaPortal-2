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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using MediaPortal.Frontend.Input;
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
      };

      // The arrangement on screen is the worn face's own answer: each rescued skin ships its home screen
      // with a layout of its own, and changing the face moves the same three areas somewhere else. The
      // view model says which arrangement is worn and says again whenever the face changes.
      home.PropertyChanged += delegate (object? sender, PropertyChangedEventArgs e)
      {
        if (e.PropertyName == nameof(HomeViewModel.HomeLayout))
          ApplyHomeLayout();
        else if (e.PropertyName == nameof(HomeViewModel.SelectedEntry))
          RebuildCross();
      };
      // The menu arriving, arriving again from a refresh, or keeping its place through a rebuild - the
      // cross arms show the menu, so every time the menu speaks, WMC's model lays itself out from it again
      // and the arms are redrawn from the model.
      _home.MenuRebuilt += delegate
      {
        _wmc.Rebuild(_home.SelectedEntry);
        RebuildCross();
      };
      ApplyHomeLayout();

      // A button that opens nothing is worse than no button, so the playback button is only offered - and only
      // walks with the d-pad - when the application can actually build the screen.
      nowPlayingButton.IsVisible = _nowPlayingScreen != null;
      connectButton.IsVisible = _connectScreen != null;

      // The ring is in the order the areas are arranged on the screen: the menu on the left, its content to the
      // right of it, the server's aspect list on the far side, and the buttons underneath. Left/right walk it;
      // each list keeps its own up/down over its items, because it handles those keys before they reach the ring.
      // The menu first: it is where MP2's home screen starts, and where a viewer with a remote expects to land.
      var ring = new List<Control>();
      if (_sectionScreen != null)
        ring.Add(menuList);
      ring.Add(contentList);
      ring.Add(aspectsList);
      ring.Add(refreshButton);
      if (_nowPlayingScreen != null)
        ring.Add(nowPlayingButton);
      if (_connectScreen != null)
        ring.Add(connectButton);
      DpadNavigation.Attach(this, ring);

      // The same screen with a mouse on a desk: double-clicking a tile, a title or a watched row sends the
      // Enter that a remote would have sent on that same list, and this screen's own handler below - which
      // already knows which lists open what and which rows merely explain something - decides the rest.
      MouseRemote.Attach(this, menuList, contentList);

      // Clicking a home tile: with both of Nereus' two switches off the reference executed the clicked tile
      // outright - HomeMenuModel.SelectItem's "if touch display is not enabled just execute the item's
      // command" - while with either on a click only selects and only pressing the tile already standing on
      // works it, which is exactly the road the double-click already walks. So one click presses here when
      // the worn face says nobody needs to stand on the tile first.
      menuList.AddHandler(PointerPressedEvent,
          delegate (object? sender, PointerPressedEventArgs args)
          {
            if (args.ClickCount != 1 || HomeViewModel.WaitsForAPress)
              return;

            // Posted for the same reason MouseRemote posts: the click has to finish selecting its tile
            // before the Enter asks which tile it meant.
            Dispatcher.UIThread.Post(delegate
            {
              if (!menuList.Focus())
                return;
              RaiseEvent(new KeyEventArgs
              {
                  RoutedEvent = KeyDownEvent,
                  Key = Key.Enter,
                  Source = menuList
              });
            });
          },
          RoutingStrategies.Tunnel);

      // The user buttons beside the sign-in boxes: names arrive from the server while the screen stands
      // there, so the row is rebuilt when they do. Pressing one says the name and steps into the password
      // box, which is what picking a name at a door means.
      _home.SignInNames.CollectionChanged += delegate { RebuildSignInNames(); };
      RebuildSignInNames();

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
            if (args.Key != Key.Left && args.Key != Key.Right)
              return;
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

              HomeSection? section = _home.Open(entry);
              if (section != null)
                OpenSection(section);
              else if (entry.ScreenKey != null)
                OpenScreen(entry.ScreenKey);
              args.Handled = true;
            }
            else if (contentList.IsKeyboardFocusWithin && contentList.SelectedItem is HomeRow row
                && row.Title != null)
            {
              _home.ResumeCommand.Execute(row.Title);
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
          // Nereus' MainLayoutPanel carries negative margins - its content goes edge to edge - and its
          // HomeContentGrid starts 40 pixels in from the rail, at the top. The top of the screen belongs
          // to the chrome and to the first gradient, so the areas start below the header band.
          homeLayout.Margin = new Thickness(16, 110, 16, 10);
          homeAreas.ColumnDefinitions = ColumnDefinitions.Parse("400,*,0");
          homeAreas.ColumnSpacing = 30;
          menuList.Margin = new Thickness(0);
          contentPane.Margin = new Thickness(40, 0, 0, 0);
          contentPane.MaxWidth = double.PositiveInfinity;
          contentPane.HorizontalAlignment = HorizontalAlignment.Stretch;
          contentPane.VerticalAlignment = VerticalAlignment.Stretch;
          // The reference's pane is a wall: covers stand side by side and the row wraps, with a heading's
          // over-wide label starting each new band. A vertical list of covers would be a shelf, not a wall.
          contentList.ItemsPanel = new FuncTemplate<Panel?>(() => new WrapPanel());
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
          contentPane.Margin = new Thickness(0, -100, 120, 0);
          contentPane.MaxWidth = 1900;
          contentPane.HorizontalAlignment = HorizontalAlignment.Right;
          contentPane.VerticalAlignment = VerticalAlignment.Top;
          contentList.ItemsPanel = PlainItemsPanel();
          break;

        default:
          homeLayout.Margin = new Thickness(48, 30);
          homeAreas.ColumnDefinitions = ColumnDefinitions.Parse("330,*,270");
          homeAreas.ColumnSpacing = 34;
          menuList.Margin = new Thickness(0);
          contentPane.Margin = new Thickness(0);
          contentPane.MaxWidth = double.PositiveInfinity;
          contentPane.HorizontalAlignment = HorizontalAlignment.Stretch;
          contentPane.VerticalAlignment = VerticalAlignment.Stretch;
          contentList.ItemsPanel = PlainItemsPanel();
          break;
      }

      // The cross is what a WMC home is made of and nothing else's; where no WMC is worn the arms step
      // aside and the menu list puts its own face back on. RebuildCross runs on every arrangement
      // because walking onto the cross, off it, or onto another entry all pass through here or through
      // the view model's selection, and an arm showing yesterday's entry is worse than an empty arm.
      bool wearsCross = _home.HomeLayout == "wmc";
      menuList.Opacity = wearsCross ? 0 : 1;
      wmcCross.IsVisible = wearsCross;
      // The three layers the face wears, in the reference's order: Background.png opaque under the fanart -
      // the reference's BackgroundBrush, the ground every WMC picture stands on - and Background_Home.png
      // translucent over it, which is the one place the window's own copy of that picture (behind this
      // screen, and so behind the fanart too) cannot be. Both only while WMC wears them: other faces dim
      // their fanart with their own gradients, and a second copy of a background under them would only
      // double what they paint. Asked from the worn face by name, so each WMC theme (default, Dark-WMC,
      // Turquoise) puts its own pair of pictures into the same two slots, and the wash falls back to the
      // theme's declared window background where a face carries no picture of that name.
      wmcHomeBase.IsVisible = wearsCross;
      wmcHomeBase.Source = wearsCross ? CrossArt("Background") : null;
      wmcFanartWash.IsVisible = wearsCross;
      wmcWashImage.Source = wearsCross
          ? CrossArt("Background_Home") ?? this.FindResource("SkinBackgroundImage") as Avalonia.Media.IImage
          : null;
      RebuildCross();
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
      crossAbove.Children.Clear();
      crossBelow.Children.Clear();
      crossLeft.Children.Clear();
      crossRight.Children.Clear();

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
        crossCenterLabel.Text = "";
        crossName.Text = "";
        crossFrame.Source = null;
        crossFocusArt.Source = null;
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
          crossAbove.Children.Add(CrossLabel(groups[standingGroup - d].Label, dim, d));
      for (int d = 1; d <= 3; d++)
        if (standingGroup + d < groups.Count)
          crossBelow.Children.Add(CrossLabel(groups[standingGroup + d].Label, dim, d));

      for (int d = 2; d >= 1; d--)
        if (standingSub - d >= 0)
          crossLeft.Children.Add(CrossNeighbour(subItems[standingSub - d], dim));
      for (int d = 1; d <= 2; d++)
        if (standingSub + d < subItems.Count)
          crossRight.Children.Add(CrossNeighbour(subItems[standingSub + d], dim));

      MainMenuEntry? stood = standingSub >= 0 ? subItems[standingSub] : null;
      crossCenterLabel.Text = _wmc.SelectedGroup?.Label ?? "";
      crossName.Text = stood?.Label ?? "";
      crossFrame.Source = CrossArt("Focus.Frame");
      crossFocusArt.Source = stood == null || string.IsNullOrEmpty(stood.HomeImageId)
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

    /// <summary>A picture of the worn skin by name, or null - asked the same way a template's converter asks.</summary>
    private static Avalonia.Media.Imaging.Bitmap? CrossArt(string name)
    {
      WornSkin? worn = SkinImageConverter.WornPictures();
      return worn == null ? null : worn.Images.Load(name);
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
