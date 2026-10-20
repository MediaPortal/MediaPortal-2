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
          homeAreas.ColumnDefinitions = ColumnDefinitions.Parse("260,*,0");
          homeAreas.ColumnSpacing = 24;
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
          // WMC's menu stands a quarter of the frame in from the left, and its HomeContentGrid is
          // right-aligned and top-aligned with a cap - the content is a panel beside the menu, not a
          // flood of it. The negative top margin is the reference's own -150, doubled: the pane slips
          // under the header band, which is where its title stands.
          homeLayout.Margin = new Thickness(300, 100, 100, 20);
          homeAreas.ColumnDefinitions = ColumnDefinitions.Parse("430,*,0");
          homeAreas.ColumnSpacing = 30;
          menuList.Margin = new Thickness(0, 60, 0, 0);
          contentPane.Margin = new Thickness(0, -100, 100, 0);
          contentPane.MaxWidth = 1500;
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
      menuList.Focus();
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
