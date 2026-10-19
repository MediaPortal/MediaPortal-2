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
using System.IO;
using System.Linq;
using System.Net;
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
using MediaPortal.Frontend.Discovery;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// Choosing the server from the television: the view model that decides what may be saved, the screen that is
  /// really loaded from its XAML, and the address the next start reads back out of the file.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The tests that need a server use the real API hosted in this process (<see cref="InProcessApi"/>), because the
  /// promise this screen makes is "somebody answered over there as a MediaPortal server". A hand-written double could
  /// not make that promise, and it is the only reason saving is allowed at all.
  /// </para>
  /// <para>
  /// Every save writes into a folder of its own under <see cref="Path.GetTempPath"/> and deletes it afterwards. A
  /// suite that wrote the real settings file would change which server the developer's own front end talks to, which
  /// is the sort of thing that is very hard to find later.
  /// </para>
  /// <para>
  /// No test here uses <c>await</c>, for the reason the other screen tests state at length: the runner waits with the
  /// dispatcher thread blocked, so an awaited test would wait for a continuation that only that thread could hand
  /// over. They start the work, pump the queue, and then look at the result.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestConnectionScreen
  {
    private readonly List<Window> _windows = new List<Window>();
    private readonly List<IDisposable> _clients = new List<IDisposable>();
    private readonly List<InProcessApi> _servers = new List<InProcessApi>();
      private readonly List<FakeUpnpServer> _devices = new List<FakeUpnpServer>();
      private readonly List<HomeViewModel> _homes = new List<HomeViewModel>();
    private readonly List<string> _leftovers = new List<string>();
    private string _root = "";

    [SetUp]
    public void SetUp()
    {
      _root = Path.Combine(Path.GetTempPath(), "mp2-connection-tests-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
      // Everything is cleaned up before anything is complained about, as in the other screen fixtures: asserting in
      // the middle would leave the server listening and the library registered for the next test.
      foreach (HomeViewModel home in _homes)
      {
        if (!WaitForHome(home, home2 => !home2.IsBusy, TimeSpan.FromSeconds(15)))
          _leftovers.Add("a library screen was still reading when its test ended; it said: '" + home.Status + "'");
      }

      foreach (Window window in _windows)
      {
        try
        {
          window.Close();
        }
        catch (ObjectDisposedException)
        {
          // Closed by the test itself, which is what the back-key test does on purpose.
        }
      }

      foreach (IDisposable client in _clients)
        client.Dispose();

      foreach (FakeUpnpServer device in _devices)
      {
        device.Dispose();
      }

      foreach (InProcessApi server in _servers)
      {
        // Closing the listener from the display thread deadlocked once with a request still in flight, so it is
        // closed elsewhere and reported when even that does not finish.
        if (!Task.Run(delegate { server.Dispose(); }).Wait(TimeSpan.FromSeconds(10)))
          _leftovers.Add("a server started by a test did not close within 10 seconds");
      }

      try
      {
        Directory.Delete(_root, true);
      }
      catch (IOException)
      {
        // A file still being written when the folder goes is not worth failing the run over; the folder is named
        // after a GUID and lives in the temp directory.
      }

      InProcessApi.RemoveMediaLibrary();
      _windows.Clear();
      _clients.Clear();
      _servers.Clear();
      _devices.Clear();
      _homes.Clear();

      if (_leftovers.Count > 0)
        Assert.Fail(string.Join("; ", _leftovers.ToArray()));
    }

    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
    }

    /// <summary>
    /// A view model for a screen test, with the search turned off.
    /// </summary>
    /// <remarks>
    /// A screen shown by a test must not ask the network what is out there: the answer would depend on whether a
    /// neighbour's laptop, a printer or Windows' own SSDP service happens to be awake, and a test that passes
    /// because of that is not testing anything. The search itself is tested against a device the test started on a
    /// port of its own, and <see cref="TheScreenLooksForServersAsSoonAsItOpens"/> shows that opening the screen is
    /// what starts it.
    /// </remarks>
    private static ConnectionViewModel Quietly(ConnectionViewModel model)
    {
      model.AutoScan = false;
      return model;
    }

    private static bool WaitFor<T>(T underTest, string because, Func<T, bool> condition, Func<T, string> whenStuck)
    {
      DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);
      while (DateTimeOffset.UtcNow < deadline)
      {
        Pump();
        if (condition(underTest))
          return true;
      }

      Assert.Fail(string.Format("Timed out after 15 seconds waiting for {0}. The screen said: '{1}'",
          because, whenStuck(underTest)));
      return false;
    }

    private static string StatusOf(ConnectionViewModel model)
    {
      return model.Status;
    }

    private static bool WaitForHome(HomeViewModel home, Func<HomeViewModel, bool> condition, TimeSpan patience)
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
    /// A settings file of this test's own, inside a folder that is deleted with the test.
    /// </summary>
    private string SettingsFileCalled(string name)
    {
      return Path.Combine(_root, name, "frontend.json");
    }

    /// <summary>
    /// A view model over a server that is really listening, with nothing typed into it yet.
    /// </summary>
    private ConnectionViewModel ScreenInFrontOfAServer(out Uri serverUrl)
    {
      List<IEnumerable<Guid>> ignored;
      InProcessApi.RegisterMediaLibrary(
          InProcessApi.CreateLibrary(InProcessApi.CreateAspects(), 3, out ignored).Object);

      InProcessApi server = InProcessApi.Start();
      _servers.Add(server);
      serverUrl = server.BaseUrl;
      return Quietly(new ConnectionViewModel(null, SettingsFileCalled("settings")));
    }

    private ShellWindow Show(ConnectionViewModel model)
    {
      var window = new ShellWindow(new ConnectionView(model));
      window.Show();
      _windows.Add(window);
      Pump();
      return window;
    }

    /// <summary>
    /// A view model whose search asks one fake device on a port of this test's own, and whose announced server is
    /// really listening in this process. The screen finds it exactly the way it finds a real one: a datagram out,
    /// an answer back, and the announced machine asked over its HTTP API.
    /// </summary>
    /// <param name="serverUrl">The address the found server is standing at.</param>
    /// <param name="deviceAnswers">
    /// <c>false</c> for a network with something on it but no MediaPortal server, which is the state the screen has
    /// to be able to talk about.
    /// </param>
    private ConnectionViewModel ScreenSearchingAServer(out Uri serverUrl, bool deviceAnswers = true)
    {
      List<IEnumerable<Guid>> ignored;
      InProcessApi.RegisterMediaLibrary(
          InProcessApi.CreateLibrary(InProcessApi.CreateAspects(), 3, out ignored).Object);
      InProcessApi api = InProcessApi.Start();
      _servers.Add(api);
      serverUrl = api.BaseUrl;

      FakeUpnpServer device = FakeUpnpServer.Announcing(
          "http://" + api.BaseUrl.Host + ":" + api.BaseUrl.Port + "/MediaPortal/UPnPServer_1/description.xml",
          deviceAnswers);
      _devices.Add(device);

      return new ConnectionViewModel(null, SettingsFileCalled("searched"), null,
          new SsdpSearchClient(device.Port), new ServerLocator(),
          new SsdpSearchOptions(SsdpAnnouncement.ServerSearchTarget, 1, TimeSpan.FromMilliseconds(400), false,
              new IPEndPoint[] { device.Endpoint }));
    }

    private static ListBox ServerList(Window window)
    {
      return window.GetVisualDescendants().OfType<ListBox>().SingleOrDefault(list => list.Name == "serversList")
          ?? throw new AssertionException("the screen has no list named \"serversList\"");
    }

    private static TextBox AddressField(Window window)
    {
      return window.GetVisualDescendants().OfType<TextBox>().SingleOrDefault(box => box.Name == "serverUrlBox")
          ?? throw new AssertionException("the screen has no field named \"serverUrlBox\"");
    }

    private static Button ButtonCalled(Window window, string name)
    {
      return window.GetVisualDescendants().OfType<Button>().SingleOrDefault(button => button.Name == name)
          ?? throw new AssertionException(string.Format("the screen has no button named \"{0}\"", name));
    }

    private static void PressKey(Control control, Key key)
    {
      control.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = key,
        Source = control
      });
    }

    private static IEnumerable<string> ShownTexts(Window window)
    {
      return window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text ?? string.Empty);
    }

    [AvaloniaTest]
    public void ANewScreenStartsByAskingForTheAddress()
    {
      var model = Quietly(new ConnectionViewModel(null, SettingsFileCalled("fresh")));

      Assert.That(model.ServerUrlText, Is.Empty, "nothing was saved, so nothing is typed in");
      Assert.That(model.Status, Does.Contain("address"));
      Assert.That(model.SaveCommand.CanExecute(null), Is.False,
          "an address that was never tested must not be savable");
    }

    [AvaloniaTest]
    public void ASavedAddressIsOfferedOnTheScreenAgain()
    {
      const string SAVED = "http://192.168.7.9:55555/MediaPortal/Server";
      var model = Quietly(new ConnectionViewModel(new ServerSettings { ServerUrl = SAVED }, SettingsFileCalled("saved")));

      Assert.That(model.ServerUrlText, Is.EqualTo(SAVED),
          "the viewer should not have to type the same address twice because the front end forgot it");
      Assert.That(model.SaveCommand.CanExecute(null), Is.False,
          "what was saved in the past still has to be tested before it is saved again");
    }

    [AvaloniaTest]
    public void OnlyAnHttpAddressCanBeTested()
    {
      var model = new ConnectionViewModel(null, SettingsFileCalled("validation"));

      Assert.That(model.TestCommand.CanExecute(null), Is.False, "an empty field cannot be asked");
      model.ServerUrlText = "192.168.1.5:55555";
      Assert.That(model.TestCommand.CanExecute(null), Is.False, "no scheme is not an address");
      model.ServerUrlText = "file:///C:/servers.txt";
      Assert.That(model.TestCommand.CanExecute(null), Is.False,
          "a file is not a server; the front end does not read media off its own disk");
      model.ServerUrlText = "https://192.168.1.5/MediaPortal/Server";
      Assert.That(model.TestCommand.CanExecute(null), Is.True, "https is a server just like http is");
    }

    [AvaloniaTest]
    public void TestingAnAddressThatAnswersSaysWhatItIs()
    {
      ConnectionViewModel model = ScreenInFrontOfAServer(out Uri url);
      model.ServerUrlText = url.ToString();

      model.TestCommand.Execute(null);
      WaitFor(model, "the server to have answered", delegate (ConnectionViewModel m) { return m.Tested; }, StatusOf);

      Assert.That(model.Status, Does.Contain("MediaPortal"),
          "the answer should be the server saying who it is, not just \"ok\": " + model.Status);
      Assert.That(model.SaveCommand.CanExecute(null), Is.True, "a tested address may be saved");
    }

    [AvaloniaTest]
    public void AnAddressThatDoesNotAnswerIsReportedAndStaysUnsaved()
    {
      string file = SettingsFileCalled("dead");
      var model = new ConnectionViewModel(null, file);
      model.ServerUrlText = string.Format("http://127.0.0.1:{0}/MediaPortal/Server/", InProcessApi.GetFreePort());

      model.TestCommand.Execute(null);
      Assert.That(model.IsBusy, Is.True, "the attempt has to say so while it is going on");
      WaitFor(model, "the attempt to be over", delegate (ConnectionViewModel m) { return !m.IsBusy; }, StatusOf);

      Assert.That(model.Tested, Is.False, "nothing was listening on that port");
      Assert.That(model.Status, Is.Not.Empty, "the viewer has to be told why nothing happened");
      Assert.That(model.SaveCommand.CanExecute(null), Is.False);
      Assert.That(File.Exists(file), Is.False, "an address that is not there must not become the saved one");
    }

    [AvaloniaTest]
    public void TheTestedAddressIsWhatTheNextStartReads()
    {
      ConnectionViewModel model = ScreenInFrontOfAServer(out Uri url);
      string file = model.SettingsFile;
      model.ServerUrlText = "  " + url + "  "; // Typed with spaces around it, as happens on a television keyboard.

      string? chosen = null;
      model.Saved += delegate (string address) { chosen = address; };

      model.TestCommand.Execute(null);
      WaitFor(model, "the server to have answered", delegate (ConnectionViewModel m) { return m.Tested; }, StatusOf);

      model.SaveCommand.Execute(null);
      Pump();

      Assert.That(chosen, Is.EqualTo(url.ToString()), "the spaces are not part of the address");
      Assert.That(File.Exists(file), Is.True, "the file the screen named is the file that was written");

      ServerSettings readBack = ServerSettings.Load(file, out string? problem);
      Assert.That(problem, Is.Null, "what the screen wrote has to be readable: " + problem);
      Assert.That(readBack.ServerUrl, Is.EqualTo(url.ToString()));
      // Every input of the rule is stated here, including an environment with nothing in it. The container image
      // exports MP2_SERVER for its own render step, and a version of this line that left the environment out
      // asked the image where it would go instead of asking the rule - which is how it came to fail on Linux
      // while meaning the opposite of what it said on both platforms.
      Assert.That(ServerApiClient.ResolveBaseUrl(null, null, readBack.ServerUrl), Is.EqualTo(url),
          "a start with no argument and nothing in the environment must end up at the address that was just chosen");
    }

    [AvaloniaTest]
    public void TypingAfterATestTakesThePermissionToSaveAway()
    {
      ConnectionViewModel model = ScreenInFrontOfAServer(out Uri url);
      model.ServerUrlText = url.ToString();
      model.TestCommand.Execute(null);
      WaitFor(model, "the server to have answered", delegate (ConnectionViewModel m) { return m.Tested; }, StatusOf);

      model.ServerUrlText = url.ToString().Replace("127.0.0.1", "localhost");
      Assert.That(model.Tested, Is.False,
          "the answer belonged to the address before the edit, not to the one in the field now");
      Assert.That(model.SaveCommand.CanExecute(null), Is.False);
    }

    [AvaloniaTest]
    public void SavingWithoutTestingRefusesAndWritesNothing()
    {
      string file = SettingsFileCalled("untested");
      var model = new ConnectionViewModel(null, file);
      model.ServerUrlText = "http://somewhere.example/MediaPortal/Server";

      model.Save(); // Called directly: the button is disabled, but the rule belongs to the view model.

      Assert.That(model.Status, Does.Contain("Test the address first"));
      Assert.That(File.Exists(file), Is.False);
    }

    [AvaloniaTest]
    public void TheScreenLoadsFromItsXamlWithTheFieldReadyToTypeIn()
    {
      const string SAVED = "http://192.168.7.9:55555/MediaPortal/Server";
      var model = Quietly(new ConnectionViewModel(new ServerSettings { ServerUrl = SAVED }, SettingsFileCalled("xaml")));
      ShellWindow window = Show(model);

      Assert.That(AddressField(window).Text, Is.EqualTo(SAVED),
          "the binding has to reach the field, not only exist next to it");
      Assert.That(AddressField(window).IsFocused, Is.True,
          "a viewer with a keyboard must be able to type as soon as the screen appears");
      Assert.That(ButtonCalled(window, "saveButton").IsEnabled, Is.False,
          "the screen shows that saving is not yet possible");
      Assert.That(ButtonCalled(window, "closeButton").IsEnabled, Is.True,
          "getting out of the screen must never depend on having chosen a server");
    }

    [AvaloniaTest]
    public void WhatIsTypedInTheFieldIsWhatTheScreenTests()
    {
      ConnectionViewModel model = ScreenInFrontOfAServer(out Uri url);
      ShellWindow window = Show(model);

      AddressField(window).Text = url.ToString();
      Pump();
      Assert.That(model.ServerUrlText, Is.EqualTo(url.ToString()),
          "the field writes into the view model, so the test asks the address the viewer typed");

      Button testButton = ButtonCalled(window, "testButton");
      Assert.That(testButton.IsEnabled, Is.True, "an address that could be a server can be tested");
      Assert.That(testButton.Command, Is.Not.Null, "the button on the screen is wired to the testing");

      // The button is activated the way a remote activates it: it takes the focus, and OK is pressed. Synthesising
      // Avalonia's Click event does not do this, because that event is what a button raises when it has already
      // decided to act - a test that raised it would prove nothing about the command behind the button.
      testButton.Focus();
      PressKey(testButton, Key.Enter);
      WaitFor(model, "the server to have answered", delegate (ConnectionViewModel m) { return m.Tested; }, StatusOf);
      Pump();

      Assert.That(ButtonCalled(window, "saveButton").IsEnabled, Is.True,
          "the screen lights the way to save as soon as the answer is in");
    }

    [AvaloniaTest]
    public void EnterInTheFieldAsksTheServer()
    {
      ConnectionViewModel model = ScreenInFrontOfAServer(out Uri url);
      ShellWindow window = Show(model);
      AddressField(window).Text = url.ToString();
      Pump();

      PressKey(AddressField(window), Key.Enter);
      WaitFor(model, "the server to have answered", delegate (ConnectionViewModel m) { return m.Tested; }, StatusOf);

      Assert.That(model.Tested, Is.True,
          "after typing an address, the remote's OK button has to be the thing that tries it");
    }

    [AvaloniaTest]
    public void TheRingWalksOverTheAreasTheScreenIsOffering()
    {
      // Nothing is typed yet, so neither the test nor the save can be done, and neither can be walked into. The
      // search and the close button are the areas that are there, and reaching close is the difference between "I
      // can leave this screen" and being trapped in a text field with a remote in hand.
      ShellWindow window = Show(Quietly(new ConnectionViewModel(null, SettingsFileCalled("caret"))));
      TextBox field = AddressField(window);

      PressKey(field, Key.Right);
      Pump();
      Assert.That(ButtonCalled(window, "searchButton").IsFocused, Is.True,
          "the first stop from the field is the button that is always offered");

      PressKey(ButtonCalled(window, "searchButton"), Key.Right);
      Pump();
      Assert.That(ButtonCalled(window, "closeButton").IsFocused, Is.True,
          "the areas that are not offered are walked over rather than walked into");

      PressKey(ButtonCalled(window, "closeButton"), Key.Right);
      Pump();
      Assert.That(window.GetVisualDescendants().OfType<ListBox>().Single().IsFocused, Is.True,
          "and the ring closes at the end, on the list the screen is really about");
    }

    [AvaloniaTest]
    public void AnAddressMakesTheTestButtonPartOfTheRing()
    {
      ConnectionViewModel model = ScreenInFrontOfAServer(out Uri url);
      ShellWindow window = Show(model);
      TextBox field = AddressField(window);
      field.Text = url.ToString();
      Pump();

      PressKey(field, Key.Down);
      Pump();
      Assert.That(ButtonCalled(window, "searchButton").IsFocused, Is.True,
          "down from the field is the row of buttons, and the first one there is the search");

      PressKey(ButtonCalled(window, "searchButton"), Key.Right);
      Pump();
      Button testButton = ButtonCalled(window, "testButton");
      Assert.That(testButton.IsFocused, Is.True,
          "an address that could be a server can be tested, so the way to that button is open");

      // Not tested yet, so saving is still not something the screen offers, and the ring walks past it.
      PressKey(testButton, Key.Right);
      Pump();
      Assert.That(ButtonCalled(window, "saveButton").IsFocused, Is.False);
      Assert.That(ButtonCalled(window, "closeButton").IsFocused, Is.True);
    }

    [AvaloniaTest]
    public void OnceTheServerAnsweredTheWayToSaveIsWalkableToo()
    {
      ConnectionViewModel model = ScreenInFrontOfAServer(out Uri url);
      ShellWindow window = Show(model);
      AddressField(window).Text = url.ToString();
      Pump();

      model.TestCommand.Execute(null);
      WaitFor(model, "the server to have answered", delegate (ConnectionViewModel m) { return m.Tested; }, StatusOf);
      Pump();

      Button testButton = ButtonCalled(window, "testButton");
      testButton.Focus();
      PressKey(testButton, Key.Right);
      Pump();

      Assert.That(ButtonCalled(window, "saveButton").IsFocused, Is.True,
          "the button the screen just enabled is the one the next key lands on");
    }

    [AvaloniaTest]
    public void BackLeavesTheScreenAndChangesNothing()
    {
      string file = SettingsFileCalled("back");
      var model = Quietly(new ConnectionViewModel(null, file));
      ShellWindow window = Show(model);
      AddressField(window).Text = "http://somewhere.example/MediaPortal/Server";
      Pump();

      PressKey(window, Key.Escape);
      Pump();

      Assert.That(window.IsVisible, Is.False);
      Assert.That(File.Exists(file), Is.False, "leaving the screen is not a decision to save anything");
    }

    [AvaloniaTest]
    public void TheScreenSaysWhereItSavesTo()
    {
      string file = SettingsFileCalled("shown");
      ShellWindow window = Show(Quietly(new ConnectionViewModel(null, file)));

      Assert.That(ShownTexts(window).Any(text => text.Contains(file)), Is.True,
          "the file is named on the screen, so \"it did not stick after a reboot\" has somewhere to look");
    }

    [AvaloniaTest]
    public void ChoosingAServerMovesTheLibraryScreenToIt()
    {
      // The other half of the screen: what the application makes of the chosen address. Dropping the old library and
      // naming the new server is the view model's job, and a screen that kept showing the previous server's aspects
      // under a new address would be worse than no screen at all.
      List<IEnumerable<Guid>> ignored;
      InProcessApi.RegisterMediaLibrary(
          InProcessApi.CreateLibrary(InProcessApi.CreateAspects(), 5, out ignored).Object);
      InProcessApi server = InProcessApi.Start();
      _servers.Add(server);

      var client = new ServerApiClient(server.BaseUrl);
      _clients.Add(client);
      var home = new HomeViewModel(client);
      _homes.Add(home);
      home.RefreshCommand.Execute(null);
      WaitForHome(home, delegate { return home.Aspects.Count == 2; }, TimeSpan.FromSeconds(15));
      Assert.That(home.Aspects.Count, Is.EqualTo(2), "the first server had a library to show");

      Uri other = new Uri(string.Format("http://127.0.0.1:{0}/MediaPortal/Server", InProcessApi.GetFreePort()));
      var second = new ServerApiClient(other);
      _clients.Add(second);
      home.UseServer(second);

      Assert.That(home.Aspects.Count, Is.EqualTo(0), "the old server's library went with its address");
      Assert.That(home.ServerUrl, Does.StartWith(other.ToString()),
          "the screen has to say which server it is looking at now");
      Assert.That(home.Status, Does.Contain("Server address set to"));
    }

    [AvaloniaTest]
    public void TheScreenLooksForServersAsSoonAsItOpens()
    {
      // The whole point of the screen: a viewer who has never typed an address and does not want to. Opening it asks
      // the network, and one key press later the library is being read from the server that answered.
      ConnectionViewModel model = ScreenSearchingAServer(out Uri url);
      ShellWindow window = Show(model);

      WaitFor(model, "the screen to have searched by itself",
          delegate (ConnectionViewModel m) { return m.Servers.Count == 1; }, StatusOf);

      Assert.That(ServerList(window).Items.Count, Is.EqualTo(1), "what the network answered is on the screen");
      Assert.That(ServerList(window).Items[0], Is.InstanceOf<DetectedServer>());
      Assert.That(ShownTexts(window).Any(text => text.Contains("Select one")), Is.True,
          "and the screen says what the viewer is meant to do next");
      Assert.That(AddressField(window).Text, Is.EqualTo(url.ToString().TrimEnd('/')),
          "the only server found is picked for them");
      Assert.That(ButtonCalled(window, "saveButton").IsEnabled, Is.True,
          "and saving it is offered straight away");
    }

    [AvaloniaTest]
    public void PressingOkOnAListedServerTakesIt()
    {
      Uri url;
      ConnectionViewModel model = ScreenSearchingAServer(out url);
      string file = model.SettingsFile;
      string? chosen = null;
      model.Saved += delegate (string address) { chosen = address; };

      ShellWindow window = Show(model);
      WaitFor(model, "the search to have found the server",
          delegate (ConnectionViewModel m) { return m.Servers.Count == 1; }, StatusOf);

      ListBox list = ServerList(window);
      list.Focus();
      PressKey(list, Key.Enter);
      Pump();

      Assert.That(chosen, Is.EqualTo(url.ToString().TrimEnd('/')),
          "the remote's OK button on a name in the list is the whole decision");
      Assert.That(File.Exists(file), Is.True, "and it is written down, so the next start does not have to ask again");
    }

    [AvaloniaTest]
    public void TypingAnAddressIsNotOverwrittenByWhatTheNetworkSays()
    {
      // Somebody who knows the address and has started typing it is further than the network is, and a screen that
      // replaced the text under the caret the moment a laptop two rooms away woke up would lose their work.
      Uri url;
      ConnectionViewModel model = ScreenSearchingAServer(out url);
      ShellWindow window = Show(model);
      TextBox field = AddressField(window);
      field.Text = "http://192.168.9.9:55555/MediaPortal/Server";
      Pump();

      WaitFor(model, "the search to finish",
          delegate (ConnectionViewModel m) { return m.Servers.Count == 1; }, StatusOf);

      Assert.That(model.ServerUrlText, Is.EqualTo("http://192.168.9.9:55555/MediaPortal/Server"),
          "what was typed is still what is typed");
      Assert.That(model.SelectedServer, Is.Null);
      Assert.That(field.IsFocused, Is.True, "and the caret did not move out from under the viewer");
      Assert.That(model.SaveCommand.CanExecute(null), Is.False, "but it has not been asked yet, so it cannot be saved");
    }

    [AvaloniaTest]
    public void ANetworkWithNoServerOnItIsSaidRatherThanShownAsAnEmptyBox()
    {
      Uri ignored;
      ConnectionViewModel model = ScreenSearchingAServer(out ignored, false);
      ShellWindow window = Show(model);

      WaitFor(model, "the search to be over",
          delegate (ConnectionViewModel m) { return !m.IsScanning; }, StatusOf);

      Assert.That(ServerList(window).Items.Count, Is.EqualTo(0));
      Assert.That(ShownTexts(window).Any(text => text.Contains("Nothing on this network answered")), Is.True,
          "\"no server\" and \"nobody asked\" have to look different");
      Assert.That(ButtonCalled(window, "searchButton").IsEnabled, Is.True, "asking again stays offered");

      AddressField(window).Text = "http://192.168.1.44:55555/MediaPortal/Server";
      Pump();
      Assert.That(ButtonCalled(window, "testButton").IsEnabled, Is.True,
          "and the keyboard is still a way in, which is the whole reason the field is there");
    }
  }
}
