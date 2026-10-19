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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// The real screen, loaded from its XAML, bound to a view model that read a server that is really running, on
  /// a platform with no display.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A build that compiles says the XAML parsed. It does not say the resource was reachable at runtime, that the
  /// bindings resolved against the view model, that the screen fills itself in when it appears, or that the
  /// visual tree Avalonia built is the one the layout describes. Those are exactly the failures a skin engine has
  /// always produced, which is why this fixture exists instead of stopping at the view-model tests.
  /// </para>
  /// <para>
  /// <c>Avalonia.Headless.NUnit</c> runs the test on the dispatcher's thread with an
  /// <c>AvaloniaSynchronizationContext</c> installed. The code after an <c>await</c> is therefore queued for that
  /// dispatcher, and a test runner waiting for an awaited test does it with that thread blocked - so an awaited
  /// test here waits for a continuation that only the blocked thread could hand over. That is a hang, not a
  /// failure, and it is the reason none of the tests below uses <c>await</c>: they start the work, run the queue
  /// until the screen says it is done, and only then look at it. Which is also closer to what a viewer does.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestHomeWindow
  {
    private readonly List<Window> _windows = new List<Window>();
    private readonly List<HomeViewModel> _homes = new List<HomeViewModel>();
    private readonly List<IDisposable> _clients = new List<IDisposable>();
    private readonly List<InProcessApi> _servers = new List<InProcessApi>();
    private readonly List<NowPlayingViewModel> _playbacks = new List<NowPlayingViewModel>();
    private readonly List<string> _leftovers = new List<string>();

    [TearDown]
    public void TearDown()
    {
      // Everything is cleaned up before anything is complained about. Asserting in the middle would leave the
      // screen open, the server listening and the library registered for the next test, and one honest failure
      // would arrive as five confusing ones.
      foreach (HomeViewModel home in _homes)
      {
        if (!WaitFor(home, delegate { return !home.IsBusy; }, TimeSpan.FromSeconds(15)))
          _leftovers.Add("a screen was still reading when its test ended; it said: '" + home.Status + "'");
      }

      foreach (Window window in _windows)
      {
        try
        {
          window.Close();
        }
        catch (ObjectDisposedException)
        {
          // Already closed by the test itself, which is what the back-key test does on purpose.
        }
      }

      foreach (IDisposable client in _clients)
        client.Dispose();

      foreach (NowPlayingViewModel playback in _playbacks)
        playback.Dispose();
      _playbacks.Clear();

      foreach (InProcessApi server in _servers)
      {
        // Closing the listener from this thread deadlocked once (measured, with a request still in flight), so
        // it is closed where no display thread is involved and reported when even that does not finish.
        if (!Task.Run(delegate { server.Dispose(); }).Wait(TimeSpan.FromSeconds(10)))
          _leftovers.Add("a server started by a test did not close within 10 seconds");
      }

      InProcessApi.RemoveMediaLibrary();
      _windows.Clear();
      _homes.Clear();
      _clients.Clear();
      _servers.Clear();

      if (_leftovers.Count > 0)
        Assert.Fail(string.Join("; ", _leftovers.ToArray()));
    }

    /// <summary>
    /// Runs the message queue once and gives the thread pool a moment. This is how a test on a platform with no
    /// display does what a viewer does by waiting: the work that continues after a request came back is handed to
    /// the dispatcher, and the dispatcher only runs when somebody asks it to.
    /// </summary>
    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
    }

    private static bool WaitFor(HomeViewModel home, Func<HomeViewModel, bool> condition, TimeSpan patience)
    {
      DateTimeOffset deadline = DateTimeOffset.UtcNow + patience;
      while (DateTimeOffset.UtcNow < deadline)
      {
        Pump();
        if (condition(home))
          return true;
      }
      return false;
    }

    /// <summary>
    /// Pumps until the screen is in the state the test wants and fails when it does not get there. A gate that
    /// hangs is worse than a gate that fails: it hides which of the two happened.
    /// </summary>
    private static void WaitUntil(HomeViewModel home, string because, Func<HomeViewModel, bool> condition)
    {
      if (!WaitFor(home, condition, TimeSpan.FromSeconds(15)))
      {
        Assert.Fail(string.Format("Timed out after 15 seconds waiting for {0}. The screen said: '{1}'",
            because, home.Status));
      }
    }

    /// <summary>
    /// Shows the real screen in front of a server that is really running. Nothing presses Refresh: the screen is
    /// expected to fill itself in when it appears, and the tests read the result of that.
    /// </summary>
    private ShellWindow ShowScreenThatReadsAServer(int itemCount, out HomeViewModel home)
    {
      List<IEnumerable<Guid>> ignored;
      InProcessApi.RegisterMediaLibrary(
          InProcessApi.CreateLibrary(InProcessApi.CreateAspects(), itemCount, out ignored).Object);

      InProcessApi server = InProcessApi.Start();
      _servers.Add(server);
      var client = new ServerApiClient(server.BaseUrl);
      _clients.Add(client);

      home = new HomeViewModel(client);
      return Show(home);
    }

    private ShellWindow Show(HomeViewModel home)
    {
      return ShowScreen(home, null, null, null);
    }

    /// <summary>
    /// The screen in the position the application puts it in: the bottom frame of the workflow, shown by the one
    /// window a viewer meets.  The doors into the other screens are the ones the test names; the shell builds a
    /// frame's view from the control the frame's model carries, so a test can watch what a key press opens.
    /// </summary>
    private ShellWindow ShowScreen(HomeViewModel home, Func<WorkflowFrame> nowPlayingScreen,
        Func<HomeSection, WorkflowFrame> sectionScreen, Func<string, WorkflowFrame> screenFactory)
    {
      var workflow = new WorkflowManager();
      var window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        if (frame.StateId == "home")
          return new HomeView(home, workflow, nowPlayingScreen, null, sectionScreen, screenFactory);
        if (frame.StateId == "nowplaying")
          return new NowPlayingView((NowPlayingViewModel)frame.ViewModel);
        return frame.ViewModel as Control;
      });
      workflow.Push(new WorkflowFrame("home", "Home", home));
      window.Show();
      _homes.Add(home);
      _windows.Add(window);
      Pump();
      return window;
    }

    private static ListBox AspectList(Window window)
    {
      return ListBoxNamed(window, "aspectsList");
    }

    /// <summary>
    /// The pane that shows the lists of the highlighted menu entry - MP2's home content.
    /// </summary>
    private static ListBox ContentList(Window window)
    {
      return ListBoxNamed(window, "contentList");
    }

    /// <summary>
    /// The row of tiles a viewer opens a part of the library from, when the screen was given a way to open one.
    /// </summary>
    private static ListBox TilesList(Window window)
    {
      return ListBoxNamed(window, "sectionsList");
    }

    /// <summary>
    /// One of the screen's lists, by the name the screen gives it.
    /// </summary>
    /// <remarks>
    /// Found by name and not by "the only list on the screen": since the home screen shows the library as tiles
    /// above the aspect list, there are two, and a test that grabs "the list" would be testing whichever one the
    /// visual tree happened to yield.
    /// </remarks>
    private static ListBox ListBoxNamed(Window window, string name)
    {
      return window.GetVisualDescendants().OfType<ListBox>()
          .FirstOrDefault(list => string.Equals(list.Name, name, System.StringComparison.Ordinal))
          ?? throw new AssertionException("the screen has no list named '" + name + "'");
    }

    /// <summary>
    /// The Refresh button, found by the name the screen gives it. Found by name rather than by "the only button
    /// on the screen", because the screen has more than one button since it grew a way to the playback screen -
    /// and a test that grabs "the single button" starts failing for reasons that have nothing to do with what it
    /// is checking.
    /// </summary>
    private static Button RefreshButton(Window window)
    {
      return window.GetVisualDescendants().OfType<Button>().SingleOrDefault(button => button.Name == "refreshButton")
          ?? throw new AssertionException("the screen has no button named \"refreshButton\"");
    }

    private static Button NowPlayingButton(Window window)
    {
      return window.GetVisualDescendants().OfType<Button>().SingleOrDefault(button => button.Name == "nowPlayingButton")
          ?? throw new AssertionException("the screen has no button named \"nowPlayingButton\"");
    }

    private static IEnumerable<string> VisibleTexts(Window window)
    {
      return window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text ?? string.Empty);
    }

    /// <summary>
    /// Whether <paramref name="node"/> is the <paramref name="area"/> itself or one of the controls inside it,
    /// walked up the visual tree Avalonia built.
    /// </summary>
    private static bool Inside(Visual? node, Visual area)
    {
      while (node != null)
      {
        if (ReferenceEquals(node, area))
          return true;
        node = node.GetVisualParent();
      }
      return false;
    }

    [AvaloniaTest]
    public void TheScreenFillsItselfInFromWhatTheServerSent()
    {
      ShellWindow window = ShowScreenThatReadsAServer(17, out HomeViewModel home);
      WaitUntil(home, "two aspects to be on screen", delegate { return home.Aspects.Count == 2; });

      // The count of an aspect is its own request after the aspects arrived, so a screen that already lists both
      // aspects does not necessarily know their numbers yet. Measured on the first Linux container run of this
      // test: the frame and the view model said "17" on Windows and "?" on Linux, with the same code and the same
      // in-process server, because the count was still on the wire. Waiting for the number is the same claim
      // made at a moment when it can be true; asserting it without waiting is a race that Windows happens to win.
      WaitUntil(home, "the count of the selected aspect to arrive",
          delegate { return home.SelectedAspect != null && home.SelectedAspect.Count.HasValue; });

      ListBox list = AspectList(window);
      Assert.That(list.Items.Count, Is.EqualTo(2), "two aspects were read, two rows are shown");
      Assert.That(list.SelectedItem, Is.InstanceOf<AspectViewModel>(), "the first row is selected for the viewer");
      Assert.That(list.Items.Cast<AspectViewModel>().Select(aspect => aspect.Name),
          Is.EqualTo(new[] { "Core", "VideoFile" }));

      // The texts a viewer would actually read, taken from the visual tree rather than from the view model: a
      // binding that resolves in code but is not wired in the XAML leaves an empty screen.
      Assert.That(VisibleTexts(window), Does.Contain("17"), "the count of the selected aspect");
      Assert.That(VisibleTexts(window).Any(text => text.Contains("MediaPortal 2 Server")), Is.True,
          "the header names the server it read");
      Assert.That(home.Status, Does.Contain("2 aspects"), "the status line adds up what it read");
    }

    [AvaloniaTest]
    public void TheRightKeyWalksFromTheButtonIntoTheList()
    {
      // The remote's actual journey on the actual screen: focus starts on the button, Right leaves it, and the ring
      // the screen declared takes the focus. Since the home screen was arranged like MP2's - menu on the left, its
      // content to the right, the server's aspect list on the far side, buttons underneath - the buttons are the
      // last area, so Right from a button wraps the ring and lands on the first one: the content pane, which is
      // where the viewer's eye is when they leave the button row.
      ShellWindow window = ShowScreenThatReadsAServer(1, out HomeViewModel home);
      WaitUntil(home, "the screen to be done reading before its keys are pressed",
          delegate { return !home.IsBusy; });

      Button button = RefreshButton(window);
      button.Focus();
      Pump();
      Assert.That(button.IsFocused, Is.True, "the button can be given the focus");

      var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right, Source = button };
      button.RaiseEvent(args);
      Pump();

      Assert.That(args.Handled, Is.True, "the ring used the key");

      // Where the focus actually landed. A list in Avalonia hands the focus to the row inside it rather than
      // keeping it on the list itself, so "the focus is in the list" is the claim - asking the list whether it
      // personally holds the focus would be a question about an implementation detail, and a wrong one.
      Visual? focused = window.FocusManager.GetFocusedElement() as Visual;
      ListBox list = ContentList(window);
      string where = focused == null ? "nobody" : focused.GetType().Name;
      string aboutList = string.Format("list Focusable={0} visible={1} enabled={2} bounds={3} attached={4} items={5}",
          list.Focusable, list.IsVisible, list.IsEnabled, list.Bounds,
          list.GetVisualParent() != null, list.Items.Count);
      Assert.That(Inside(focused, list), Is.True,
          "the focus is in the content pane, which is first in the ring (focus was on: " + where + "; " + aboutList + ")");
      Assert.That(button.IsFocused, Is.False, "the button is not it");
    }

    [AvaloniaTest]
    public void TheRingWalksTheAreasInTheOrderTheyAreOnTheScreen()
    {
      // The claim the arrangement of the screen makes to the viewer: with a remote in hand, Right moves to the area
      // to the right. MP2's home screen puts the main menu on the left and the highlighted entry's lists beside it,
      // so this front end does the same and the ring has to follow - menu, content, aspect list - rather than the
      // order the XAML happens to declare them in.
      // The screen is shown with a section screen so that the menu is part of the walk at all: without a way to
      // open a section the menu is deliberately not focusable (see HomeView), because a menu that leads nowhere
      // should not be where the arrow keys start.
      var home = new HomeViewModel(null);
      var window = ShowScreen(home, null, delegate (HomeSection section) { return null!; }, null);
      Pump();

      ListBox menu = ListBoxNamed(window, "menuList");
      ListBox content = ContentList(window);
      ListBox aspects = AspectList(window);

      menu.Focus();
      Pump();
      Press(window, menu, Key.Right);
      Visual? afterRight = window.FocusManager.GetFocusedElement() as Visual;
      string where = afterRight == null ? "nobody" : afterRight.GetType().Name;
      string aboutContent = string.Format("content Focusable={0} visible={1} enabled={2} bounds={3} items={4}",
          content.Focusable, content.IsVisible, content.IsEnabled, content.Bounds, content.Items.Count);
      Assert.That(Inside(afterRight, content), Is.True,
          "Right from the menu lands in the content pane next to it (focus was on: " + where + "; "
          + aboutContent + "; menu items=" + menu.Items.Count + ")");

      content.Focus();
      Pump();
      Press(window, content, Key.Right);
      Assert.That(Inside(window.FocusManager.GetFocusedElement() as Visual, aspects), Is.True,
          "Right from the content pane lands in the aspect list on the far side");

      aspects.Focus();
      Pump();
      Press(window, aspects, Key.Left);
      Assert.That(Inside(window.FocusManager.GetFocusedElement() as Visual, content), Is.True,
          "and Left walks back the same way");
    }

    /// <summary>
    /// Presses one key on a control the way the remote does - a <see cref="KeyDownEvent"/> routed at it - and pumps
    /// the screen so the focus change has happened by the time the caller looks.
    /// </summary>
    private static void Press(Window window, Control control, Key key)
    {
      control.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = key,
        Source = control
      });
      Pump();
    }

    [AvaloniaTest]
    public void TheBackKeyClosesTheScreen()
    {
      ShellWindow window = ShowScreenThatReadsAServer(1, out _);
      bool closed = false;
      window.Closed += delegate { closed = true; };

      RefreshButton(window).RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Escape,
        Source = window
      });

      Assert.That(closed, Is.True);
      _windows.Remove(window);
    }

    /// <summary>
    /// The way from the library to what is playing, measured on the real home screen: the button is only there
    /// when the application has a player to show, and pressing it opens the application's own playback screen
    /// rather than a new empty one.
    /// </summary>
    [AvaloniaTest]
    public void TheWayToThePlaybackScreenIsOfferedOnlyWhenThereIsAPlayer()
    {
      ShellWindow withoutPlayer = Show(new HomeViewModel());
      Assert.That(NowPlayingButton(withoutPlayer).IsVisible, Is.False,
          "a machine without a player is not offered a screen that could not play anything");

      var playback = new NowPlayingViewModel(new FakePlayer());
      _playbacks.Add(playback);

      ShellWindow withPlayer = ShowScreen(new HomeViewModel(),
          delegate { return new WorkflowFrame("nowplaying", "Now playing", playback); }, null, null);

      Assert.That(NowPlayingButton(withPlayer).IsVisible, Is.True, "with a player, the way there appears");
    }

    [AvaloniaTest]
    public void ThePlaybackButtonOpensTheApplicationOwnPlaybackScreen()
    {
      var player = new FakePlayer();
      var playback = new NowPlayingViewModel(player);
      _playbacks.Add(playback);
      Assert.That(playback.Play(MediaLocation.FromUrl("http://mediaportal.example/the-film.mkv")),
          Is.True, playback.StatusLine);

      ShellWindow home = ShowScreen(new HomeViewModel(),
          delegate { return new WorkflowFrame("nowplaying", "Now playing", playback); }, null, null);
      Pump();

      Button button = NowPlayingButton(home);
      button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
      Pump();

      Assert.That(home.Workflow.Current.StateId, Is.EqualTo("nowplaying"), "the button opened the playback screen");
      Assert.That(home.Screen, Is.InstanceOf<NowPlayingView>(), "and what appeared is the playback screen");
      Assert.That(home.Screen.DataContext, Is.SameAs(playback),
          "and what it shows is the application's playback, not a fresh screen that knows nothing about the film");
      (home.Screen as NowPlayingView)!.SetOsdVisible(true);
      Pump();
      Assert.That(
          home.Screen.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text ?? "")
              .Any(text => text.Contains("the-film.mkv")),
          Is.True, "including the name of what is playing");

      // A second press must not stack a second copy of the screen over itself.
      button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
      Pump();
      Assert.That(home.Workflow.Stack.Count, Is.EqualTo(2),
          "the home screen with the playback over it - not three deep");
    }

    /// <summary>
    /// The first thing a screen owes the keys pressed on it: something focused when it opens. Every action here
    /// is Enter asking who holds the focus, so a screen that opens with nothing focused answers no key at all -
    /// which is exactly how it felt on the machine, with a viewer pressing Enter against a screen that never
    /// replied.
    /// </summary>
    [AvaloniaTest]
    public void TheScreenTakesTheFocusWhenItOpensSoTheFirstKeyLandsSomewhere()
    {
      ShellWindow window = ShowScreenThatReadsAServer(1, out _);

      // The menu, or - because a list often hands its focus to the row it stands on - a row inside the menu.
      // Either answer lets the first Enter find something; "nothing" is the one answer that must not happen.
      object? focused = window.FocusManager.GetFocusedElement();
      ListBox menu = ListBoxNamed(window, "menuList");
      Assert.That(focused, Is.Not.Null, "the screen opened with nothing focused, so the first Enter went nowhere");
      Assert.That(ReferenceEquals(focused, menu) || Inside(focused as Visual, menu), Is.True,
          "and it is the menu that holds it, where MP2's home screen starts");
    }

    /// <summary>
    /// The mouse on a desk: a double-click on a menu entry means the Enter a remote would have sent on that
    /// list, so it opens the screen the entry names - through the same view model and factory a key press uses,
    /// not a second path that could drift from it.
    /// </summary>
    [AvaloniaTest]
    public void ADoubleClickOnAMenuEntryOpensTheScreenItNames()
    {
      string? asked = null;
      var marker = new ContentControl();
      var home = new HomeViewModel();
      var window = ShowScreen(home, null, null,
          delegate (string screenKey)
          {
            asked = screenKey;
            return new WorkflowFrame("screen", screenKey, marker);
          });
      ListBox menu = ListBoxNamed(window, "menuList");
      MainMenuEntry settings = MenuEntryAfterSettling(menu, "themes");

      Assert.That(settings, Is.Not.Null, "the menu carries the settings entry the double-click is meant to open");

      // The clicks are injected as raw mouse input on the row itself, at the point the row covers - the same
      // two clicks a hand on the desk makes. Not a synthesised key: the point of this test is that the mouse
      // reaches the screen at all, and an event raised by hand would skip the hit testing that has to agree.
      DoubleClickOn(window, menu, settings);

      Assert.That(asked, Is.EqualTo("themes"), "the double-click asked for the screen the entry names");
      Assert.That(ReferenceEquals(window.Screen, marker), Is.True, "and that screen is the one that appeared");
    }

    /// <summary>
    /// Double-clicks the row that shows <paramref name="item"/>, wherever on screen that row ended up: the two
    /// clicks are sent where the viewer would aim, through the headless window's mouse, and the queue runs
    /// between them so the first click finishes selecting the row before the second one becomes a double-click.
    /// </summary>
    private static void DoubleClickOn(Window window, ListBox list, object item)
    {
      list.ScrollIntoView(item);
      Pump();
      Control row = list.ContainerFromItem(item) as Control;
      Assert.That(row, Is.Not.Null, "the row to click is on the screen");
      Assert.That(row.Bounds.Width, Is.GreaterThan(0.0), "and it has a place on the screen to click");

      Point? at = row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), window);
      Assert.That(at, Is.Not.Null);
      window.MouseDown(at.Value, MouseButton.Left);
      window.MouseUp(at.Value, MouseButton.Left);
      Pump();
      window.MouseDown(at.Value, MouseButton.Left);
      window.MouseUp(at.Value, MouseButton.Left);
      Pump();
    }

    /// <summary>
    /// The menu entry for <paramref name="screenKey"/>, once the home screen has stopped rebuilding the menu.
    /// Every rebuild makes every entry a new object, so a test that grabs one during the churn is holding a
    /// stale row that is no longer on the screen; settled here means the same object answering twice in a row.
    /// </summary>
    private static MainMenuEntry MenuEntryAfterSettling(ListBox menu, string screenKey)
    {
      MainMenuEntry previous = null;
      for (int attempt = 0; attempt < 40; attempt++)
      {
        Pump();
        MainMenuEntry current = null;
        foreach (object item in menu.Items)
        {
          MainMenuEntry entry = item as MainMenuEntry;
          if (entry != null && entry.ScreenKey == screenKey)
            current = entry;
        }

        if (previous != null && ReferenceEquals(previous, current))
          return current;
        previous = current;
      }

      Assert.That(previous, Is.Not.Null, "the menu carries no entry for the screen '" + screenKey + "'");
      Assert.Fail("the menu kept rebuilding itself, so nothing could hold on to one of its entries");
      return null;
    }

    /// <summary>
    /// Walking the menu with the arrow keys must not cost the viewer the Enter: whichever element the list ends
    /// up holding the focus on, the key presses what the walk selected. A viewer who can move the highlight but
    /// not open it is no further than one who could do neither.
    /// </summary>
    [AvaloniaTest]
    public void EnterStillOpensAfterTheArrowKeysHaveWalkedTheMenu()
    {
      string? asked = null;
      var marker = new ContentControl();
      var home = new HomeViewModel();
      var window = ShowScreen(home, null, null,
          delegate (string screenKey)
          {
            asked = screenKey;
            return new WorkflowFrame("screen", screenKey, marker);
          });
      ListBox menu = ListBoxNamed(window, "menuList");
      MainMenuEntry settings = MenuEntryAfterSettling(menu, "themes");

      Assert.That(settings, Is.Not.Null);

      // Walk with the arrow keys the way a viewer does - one key at a time, stopping where the menu stops - until
      // the highlight stands on the settings entry. Not a direct selection: the point of this test is the state
      // the walk leaves behind, which in Avalonia is a focused row inside the list rather than the list itself.
      for (int step = 0; step < 15 && (menu.SelectedItem as MainMenuEntry)?.ScreenKey != "themes"; step++)
        Press(window, menu, Key.Down);
      Assert.That((menu.SelectedItem as MainMenuEntry)?.ScreenKey, Is.EqualTo("themes"),
          "walking down with the arrow keys stops on the settings entry, as far as this test goes");

      // The key is raised where Avalonia says the focus is - a real keyboard never aims at the window, it aims
      // at whatever holds the focus, and the window's handler only ever sees it on its way past.
      Control target = window.FocusManager.GetFocusedElement() as Control ?? menu;
      target.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Enter,
        Source = target
      });
      Pump();

      Assert.That(asked, Is.EqualTo("themes"),
          "the screen walked to was the screen the key was meant for, and it opened exactly that one");
    }

    [AvaloniaTest]
    public void AScreenWithoutAServerSaysSoOnItsStatusLine()
    {
      ShellWindow window = Show(new HomeViewModel());
      Pump();

      Assert.That(VisibleTexts(window), Does.Contain("No server configured"));
      Assert.That(AspectList(window).Items.Count, Is.Zero);
    }

    [AvaloniaTest]
    public void AServerThatNeverAnswersIsNamedRatherThanLeavingTheScreenWaiting()
    {
      // Nobody is listening on this port. What is checked is that the screen gives up and says which server it
      // could not reach; a status line that stays at "Reading ..." forever is the one failure a viewer can do
      // nothing with. The two seconds are the client's patience for this test, not the product's - a port that
      // was just measured to be free is not guaranteed to refuse the connection quickly, because the operating
      // system may still be holding it.
      Uri dead = new Uri(string.Format("http://127.0.0.1:{0}/MediaPortal/Server/", InProcessApi.GetFreePort()));
      var client = new ServerApiClient(dead, null, TimeSpan.FromSeconds(2));
      _clients.Add(client);
      ShellWindow window = Show(new HomeViewModel(client));

      HomeViewModel home = _homes[0];
      WaitUntil(home, "the client to run out of patience", delegate { return !home.IsBusy; });

      Assert.That(home.Status, Does.Contain(dead.ToString()), "and it says which server it could not reach");
      Assert.That(AspectList(window).Items.Count, Is.Zero);
    }
  }
}
