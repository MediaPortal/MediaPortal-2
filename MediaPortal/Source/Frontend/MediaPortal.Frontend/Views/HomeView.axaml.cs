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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MediaPortal.Frontend.Input;
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

      // A button that opens nothing is worse than no button, so the playback button is only offered - and only
      // walks with the d-pad - when the application can actually build the screen.
      nowPlayingButton.IsVisible = _nowPlayingScreen != null;
      connectButton.IsVisible = _connectScreen != null;

      // A list of films that cannot be started is worse than no list, so on a machine with no player the strip
      // stays out of the way entirely; the view model still read it, and the aspects below still say what the
      // library holds.
      if (_nowPlayingScreen == null)
        unfinishedPanel.IsVisible = false;

      // The ring is in the order the areas are arranged on the screen: the menu on the left, its content to the
      // right of it, the server's aspect list on the far side, and the buttons underneath. Left/right walk it;
      // each list keeps its own up/down over its items, because it handles those keys before they reach the ring.
      // The menu first: it is where MP2's home screen starts, and where a viewer with a remote expects to land.
      var ring = new List<Control>();
      if (_sectionScreen != null)
        ring.Add(menuList);
      ring.Add(contentList);
      if (_nowPlayingScreen != null)
        ring.Add(unfinishedList);
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
      MouseRemote.Attach(this, menuList, unfinishedList, contentList);

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
                && !aspectsList.IsKeyboardFocusWithin && !unfinishedList.IsKeyboardFocusWithin)
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
              HomeSection? section = _home.Open(entry);
              if (section != null)
                OpenSection(section);
              else if (entry.ScreenKey != null)
                OpenScreen(entry.ScreenKey);
              args.Handled = true;
            }
            else if (unfinishedList.IsKeyboardFocusWithin
                && unfinishedList.SelectedItem is TitleViewModel unfinished)
            {
              _home.ResumeCommand.Execute(unfinished);
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
