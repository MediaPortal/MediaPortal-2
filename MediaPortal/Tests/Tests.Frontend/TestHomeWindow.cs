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
    private HomeWindow ShowScreenThatReadsAServer(int itemCount, out HomeViewModel home)
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

    private HomeWindow Show(HomeViewModel home)
    {
      var window = new HomeWindow(home);
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
      HomeWindow window = ShowScreenThatReadsAServer(17, out HomeViewModel home);
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
      // The remote's actual journey on the actual screen: focus starts on the button, Right leaves it, and the
      // list - which is next in the ring the screen declared - takes the focus.
      HomeWindow window = ShowScreenThatReadsAServer(1, out HomeViewModel home);
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
      ListBox list = AspectList(window);
      string where = focused == null ? "nobody" : focused.GetType().Name;
      string aboutList = string.Format("list Focusable={0} visible={1} enabled={2} bounds={3} attached={4} items={5}",
          list.Focusable, list.IsVisible, list.IsEnabled, list.Bounds,
          list.GetVisualParent() != null, list.Items.Count);
      Assert.That(Inside(focused, list), Is.True,
          "the focus is in the list, which is next in the ring (focus was on: " + where + "; " + aboutList + ")");
      Assert.That(button.IsFocused, Is.False, "the button is not it");
    }

    [AvaloniaTest]
    public void TheBackKeyClosesTheScreen()
    {
      HomeWindow window = ShowScreenThatReadsAServer(1, out _);
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
      HomeWindow withoutPlayer = Show(new HomeViewModel());
      Assert.That(NowPlayingButton(withoutPlayer).IsVisible, Is.False,
          "a machine without a player is not offered a screen that could not play anything");

      var playback = new NowPlayingViewModel(new FakePlayer());
      _playbacks.Add(playback);
      var playbackScreen = new NowPlayingWindow(playback);
      _windows.Add(playbackScreen);

      var withPlayer = new HomeWindow(new HomeViewModel(), delegate { return playbackScreen; });
      withPlayer.Show();
      _windows.Add(withPlayer);
      Pump();

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

      var playbackScreen = new NowPlayingWindow(playback);
      var home = new HomeWindow(new HomeViewModel(), delegate { return playbackScreen; });
      home.Show();
      _windows.Add(home);
      _windows.Add(playbackScreen);
      Pump();

      Button button = NowPlayingButton(home);
      button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
      Pump();

      Assert.That(playbackScreen.IsVisible, Is.True, "the button opened the playback screen");
      Assert.That(playbackScreen.DataContext, Is.SameAs(playback),
          "and what it shows is the application's playback, not a fresh screen that knows nothing about the film");
      Assert.That(
          playbackScreen.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text ?? "")
              .Any(text => text.Contains("the-film.mkv")),
          Is.True, "including the name of what is playing");

      // A second press must not stack a second screen or lose the playback.
      button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
      Pump();
      Assert.That(playbackScreen.IsVisible, Is.True, "the same screen is still the screen");
    }

    [AvaloniaTest]
    public void AScreenWithoutAServerSaysSoOnItsStatusLine()
    {
      HomeWindow window = Show(new HomeViewModel());
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
      HomeWindow window = Show(new HomeViewModel(client));

      HomeViewModel home = _homes[0];
      WaitUntil(home, "the client to run out of patience", delegate { return !home.IsBusy; });

      Assert.That(home.Status, Does.Contain(dead.ToString()), "and it says which server it could not reach");
      Assert.That(AspectList(window).Items.Count, Is.Zero);
    }
  }
}
