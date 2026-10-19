#region Copyright (C) 2007-2023 Team MediaPortal

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

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The folders screen as XAML, bindings and a visual tree, on a platform with no display.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The view model fixture proves what the screen says; this one proves that the saying arrives. The three things
  /// only a screen can get wrong are all here: the media types arrive from the server after the screen is built, so
  /// their tick boxes exist only later and have to join the arrow ring when they appear; Enter has to mean two
  /// different things depending on where it was pressed; and Back has to leave the library as it was found.
  /// </para>
  /// <para>
  /// None of these tests uses <c>await</c>, for the reason <see cref="TestHomeWindow"/> spells out: on the headless
  /// dispatcher an awaited test waits for a continuation that only the waiting thread could hand over, which hangs
  /// instead of failing. They start the work, run the queue until the screen says it is done, and then look.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestFoldersWindow
  {
    private readonly List<Window> _windows = new List<Window>();
    private readonly List<FoldersViewModel> _models = new List<FoldersViewModel>();
    private readonly List<IDisposable> _clients = new List<IDisposable>();
    private readonly List<InProcessApi> _servers = new List<InProcessApi>();
    private readonly List<string> _leftovers = new List<string>();
    private string _scratch;
    private string _folder;
    private string _other;
    private SharesFakes _server;

    [SetUp]
    public void SetUp()
    {
      string scratch = Path.Combine(Path.GetTempPath(), "mp2-folders-window-" + Guid.NewGuid().ToString("N"));
      _scratch = Directory.CreateDirectory(scratch).FullName;
      _folder = Directory.CreateDirectory(Path.Combine(scratch, "first")).FullName;
      _other = Directory.CreateDirectory(Path.Combine(scratch, "second")).FullName;
      _server = new SharesFakes(_folder);
      InProcessApi.RegisterMediaLibrary(_server.Library.Object);
      ServiceRegistration.Set<ISystemResolver>(_server.Resolver.Object);
      ServiceRegistration.Set<IMediaAccessor>(_server.Accessor.Object);
      ServiceRegistration.Set<IImporterWorker>(_server.Importer.Object);
    }

    [TearDown]
    public void TearDown()
    {
      // Cleaned up before anything is complained about, as in the other screen fixtures: a failure that leaves the
      // server listening and the library registered arrives at the next test as five confusing ones.
      foreach (FoldersViewModel folders in _models)
      {
        // Stops the reading and the queue watching, then gives the screen its turns so that nothing is still in the
        // air when the server is taken away below.
        folders.Dispose();
        if (!PumpWhile(folders, delegate { return folders.IsBusy; }, TimeSpan.FromSeconds(15)))
          _leftovers.Add("a screen was still reading when its test ended; it said: '" + folders.Status + "'");
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

      foreach (InProcessApi server in _servers)
      {
        // The same hazard <see cref="TestHomeWindow"/> measured and wrote down: closing the listener from the
        // display thread deadlocks, so it is closed where no display thread is involved - and said when even that
        // does not finish.
        if (!Task.Run(delegate { server.Dispose(); }).Wait(TimeSpan.FromSeconds(10)))
          _leftovers.Add("a server started by a test did not close within 10 seconds");
      }

      InProcessApi.RemoveMediaLibrary();
      if (ServiceRegistration.IsRegistered<ISystemResolver>())
        ServiceRegistration.Remove<ISystemResolver>();
      if (ServiceRegistration.IsRegistered<IMediaAccessor>())
        ServiceRegistration.Remove<IMediaAccessor>();
      if (ServiceRegistration.IsRegistered<IImporterWorker>())
        ServiceRegistration.Remove<IImporterWorker>();

      try
      {
        if (_scratch != null && Directory.Exists(_scratch))
          Directory.Delete(_scratch, true);
      }
      catch (IOException)
      {
        // A scratch directory left behind is not worth failing a test over
      }

      if (_leftovers.Count > 0)
        Assert.Fail(string.Join("; ", _leftovers.ToArray()));
    }

    /// <summary>
    /// Opens the screen in front of a server that is really running. Nothing presses Refresh.
    /// </summary>
    private FoldersWindow Opened(out FoldersViewModel folders)
    {
      InProcessApi server = InProcessApi.Start();
      _servers.Add(server);
      var client = new ServerApiClient(server.BaseUrl);
      _clients.Add(client);

      folders = new FoldersViewModel(client) { PollDelayMilliseconds = 20 };
      _models.Add(folders);
      var window = new FoldersWindow(folders);
      _windows.Add(window);
      window.Show();
      Pump();
      return window;
    }

    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
    }

    /// <summary>
    /// Pumps while <paramref name="stillRunning"/> holds, and says whether it stopped on its own.
    /// </summary>
    private static bool PumpWhile(FoldersViewModel folders, Func<FoldersViewModel, bool> stillRunning,
        TimeSpan patience)
    {
      DateTimeOffset deadline = DateTimeOffset.UtcNow + patience;
      while (DateTimeOffset.UtcNow < deadline)
      {
        Pump();
        if (!stillRunning(folders))
          return true;
      }
      return false;
    }

    private static void PumpUntil(FoldersViewModel folders, string because, Func<FoldersViewModel, bool> condition)
    {
      if (PumpWhile(folders, model => !condition(model), TimeSpan.FromSeconds(15)))
        return;

      Assert.Fail(string.Format("Timed out after 15 seconds waiting for {0}. The screen said: '{1} / {2}'",
          because, folders.Status, folders.Queue));
    }

    private static ListBox FolderList(Window window)
    {
      return window.GetVisualDescendants().OfType<ListBox>()
          .FirstOrDefault(list => list.Name == "foldersList")
          ?? throw new AssertionException("the screen has no list named \"foldersList\"");
    }

    private static Control Named(Window window, string name)
    {
      return window.GetVisualDescendants().OfType<Control>()
          .FirstOrDefault(control => control.Name == name)
          ?? throw new AssertionException("the screen has no control named '" + name + "'");
    }

    private static IEnumerable<string> VisibleTexts(Window window)
    {
      return window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text ?? string.Empty);
    }

    private static void Press(Control source, Key key)
    {
      source.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = source });
      Pump();
    }

    [AvaloniaTest]
    public void TheScreenListsTheFoldersTheServerHolds()
    {
      FoldersWindow window = Opened(out FoldersViewModel folders);
      PumpUntil(folders, "the two folders to be on screen", delegate { return folders.Shares.Count == 2; });

      Assert.That(FolderList(window).Items.Count, Is.EqualTo(2),
          "the list was given what the view model has, not only what it had when the screen was built");
      Assert.That(VisibleTexts(window), Does.Contain("Films").And.Contain("Alien Earth"));
      // The media types only exist once the server answered, which is after the screen was built.
      PumpUntil(folders, "the media types to arrive", delegate { return folders.Categories.Count == 3; });
      Assert.That(VisibleTexts(window), Does.Contain("Movie"));
    }

    [AvaloniaTest]
    public void TheTickBoxesAreReachedWithTheArrowKeys()
    {
      // The whole point of the ring the screen builds once the categories have arrived: a tick box that cannot be
      // reached from the path field is a folder that cannot be added, because a television has no Tab key.
      FoldersWindow window = Opened(out FoldersViewModel folders);
      PumpUntil(folders, "the media types to arrive", delegate { return folders.Categories.Count == 3; });

      var path = (TextBox) Named(window, "folderPathBox");
      path.Focus();
      Pump();
      Assert.That(path.IsFocused, Is.True, "the field can be given the focus");

      Press(path, Key.Down);

      CheckBox landed = window.GetVisualDescendants().OfType<CheckBox>().SingleOrDefault(box => box.IsFocused);
      Assert.That(landed, Is.Not.Null, "the arrow left the field and reached one of the tick boxes");
      Assert.That(landed.Content, Is.EqualTo("Movie"), "and the first of them, which is the one above it");
    }

    [AvaloniaTest]
    public void EnterInTheFieldAddsTheFolderItHolds()
    {
      // What the field's Enter must not do is add nothing at all: the media types are unticked by default, so the
      // screen is expected to refuse and say why. Ticking one is part of the test, and it is done with the boxes
      // the screen made rather than with the view model behind them.
      FoldersWindow window = Opened(out FoldersViewModel folders);
      PumpUntil(folders, "the media types to arrive", delegate { return folders.Categories.Count == 3; });

      CheckBox movies = window.GetVisualDescendants().OfType<CheckBox>().Single(box => (string) box.Content == "Movie");
      movies.IsChecked = true;
      Pump();

      var path = (TextBox) Named(window, "folderPathBox");
      path.Focus();
      path.Text = _other;
      Pump();
      Press(path, Key.Enter);

      PumpUntil(folders, "the folder to be registered", delegate { return folders.Shares.Count == 3; });
      Assert.That(_server.Registered, Has.Count.EqualTo(1));
      Assert.That(VisibleTexts(window), Does.Contain(Path.GetFileName(_other)),
          "and the new folder is on screen under its own name");
    }

    [AvaloniaTest]
    public void TheBackKeyClosesTheScreen()
    {
      FoldersWindow window = Opened(out FoldersViewModel folders);
      PumpUntil(folders, "the folders to be on screen", delegate { return folders.Shares.Count == 2; });

      bool closed = false;
      window.Closed += delegate { closed = true; };

      Press(FolderList(window), Key.Escape);

      Assert.That(closed, Is.True);
      Assert.That(_server.Shares.Count, Is.EqualTo(2), "and it left the library exactly as it found it");
    }
  }
}
