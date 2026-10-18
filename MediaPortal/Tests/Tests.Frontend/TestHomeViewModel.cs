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
using Moq;
using NUnit.Framework;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Utilities.Exceptions;

namespace Tests.Frontend
{
  /// <summary>
  /// What the home screen ends up showing, decided by reading a server that is really answering.
  /// </summary>
  [TestFixture]
  public class TestHomeViewModel
  {
    private readonly List<IDisposable> _toDispose = new List<IDisposable>();

    [TearDown]
    public void TearDown()
    {
      InProcessApi.RemoveMediaLibrary();
      foreach (IDisposable disposable in _toDispose)
        disposable.Dispose();
      _toDispose.Clear();
    }

    private HomeViewModel ConnectedOut(int count, out List<IEnumerable<Guid>> counted)
    {
      Mock<IMediaLibrary> library = InProcessApi.CreateLibrary(InProcessApi.CreateAspects(), count, out counted);
      InProcessApi.RegisterMediaLibrary(library.Object);

      InProcessApi api = InProcessApi.Start();
      _toDispose.Add(api);
      var client = new ServerApiClient(api.BaseUrl);
      _toDispose.Add(client);
      return new HomeViewModel(client);
    }

    [Test]
    public async Task ARefreshReadsTheServerTheAspectsAndTheCounts()
    {
      List<IEnumerable<Guid>> counted;
      HomeViewModel home = ConnectedOut(17, out counted);

      await home.RefreshAsync();

      Assert.That(home.ServerDescription, Does.Contain("MediaPortal 2 Server"));
      Assert.That(home.Aspects.Select(aspect => aspect.Name), Is.EqualTo(new[] { "Core", "VideoFile" }));
      Assert.That(home.Aspects[0].Count, Is.EqualTo(17));
      Assert.That(home.Aspects[0].CountText, Is.EqualTo("17"));
      Assert.That(home.IsBusy, Is.False, "the screen is not waiting any more");
      Assert.That(home.Status, Does.Contain("2 aspects"));
      Assert.That(counted.Count, Is.EqualTo(2), "one count request per aspect");
    }

    [Test]
    public async Task TheFirstAspectIsSelectedSoTheScreenHasSomethingToShow()
    {
      List<IEnumerable<Guid>> counted;
      HomeViewModel home = ConnectedOut(1, out counted);

      await home.RefreshAsync();

      Assert.That(home.SelectedAspect, Is.Not.Null);
      Assert.That(home.SelectedAspect!.Name, Is.EqualTo("Core"));
      Assert.That(home.SelectedAspect.IndexedAttributes.Select(attribute => attribute.Name),
          Is.EqualTo(new[] { "Title" }));
    }

    [Test]
    public async Task ASecondRefreshReplacesWhatTheFirstOneRead()
    {
      // The list on screen is the same object across refreshes, so a stale row would stay visible forever if
      // the refresh appended instead of replaced.
      List<IEnumerable<Guid>> counted;
      HomeViewModel home = ConnectedOut(1, out counted);

      await home.RefreshAsync();
      Assert.That(home.Aspects.Count, Is.EqualTo(2));

      await home.RefreshAsync();
      Assert.That(home.Aspects.Count, Is.EqualTo(2), "still two, not four");
    }

    [Test]
    public async Task TwoRefreshesStartedTogetherLeaveOneCleanScreen()
    {
      // What this guards is a crash, not a preference. Starting a second read while the first was still in
      // flight used to dispose the *first* run's cancellation source; the next time that run read `Token` it got
      // ObjectDisposedException, on the dispatcher - an unhandled exception in a running application. It was
      // measured, by the render probe's first run: the window's own read on open and the probe's read overlapped.
      // A double press of a remote's OK button reaches the same line, so this is not a theoretical caller.
      // Since then: the older source is only cancelled (its own run disposes it), and a gate keeps the two runs
      // out of the same collection - a cancelled run carries on until its next await throws.
      List<IEnumerable<Guid>> counted;
      HomeViewModel home = ConnectedOut(4, out counted);

      Task first = home.RefreshAsync();
      Task second = home.RefreshAsync();

      await Task.WhenAll(first, second);

      Assert.That(home.Aspects.Count, Is.EqualTo(2), "one clean list, whatever order the runs finished in");
      Assert.That(home.IsBusy, Is.False, "and the screen is not left waiting on a run that is gone");
    }

    [Test]
    public async Task PressingTheRefreshButtonTwiceDoesNotThrowOnTheDispatcher()
    {
      // Same crash, but through the path a user takes. `RefreshCommand.Execute` is not a thin wrapper around the
      // method: the toolkit's Execute() hands the task to an `async void` continuation that rethrows anything the
      // command lets escape, on whatever context the press happened on - on the UI thread that is an unhandled
      // exception, which is how the ObjectDisposedException above killed the process. The render probe found it,
      // this test is what keeps it found: an awaited test would never have seen it.
      List<IEnumerable<Guid>> counted;
      HomeViewModel home = ConnectedOut(3, out counted);

      home.RefreshCommand.Execute(null);
      Task firstRun = home.RefreshCommand.ExecutionTask!;
      home.RefreshCommand.Execute(null);
      Task secondRun = home.RefreshCommand.ExecutionTask!;

      Assert.That(firstRun, Is.Not.SameAs(secondRun), "two presses, two runs");

      // Nothing should reach the rethrow path, so awaiting the runs here stays quiet.
      await Task.WhenAll(firstRun, secondRun);

      Assert.That(home.Aspects.Count, Is.EqualTo(2));
      Assert.That(home.IsBusy, Is.False);
      Assert.That(home.Status, Does.Contain("aspects"));
    }

    [Test]
    public async Task WhenNobodyAnswersTheScreenSaysSoRatherThanShowingAnEmptyLibrary()
    {
      Uri dead = new Uri(string.Format("http://127.0.0.1:{0}/MediaPortal/Server/", InProcessApi.GetFreePort()));
      var client = new ServerApiClient(dead, null, TimeSpan.FromSeconds(2));
      _toDispose.Add(client);
      HomeViewModel home = new HomeViewModel(client);

      await home.RefreshAsync();

      Assert.That(home.Aspects, Is.Empty);
      // The port was measured free a moment ago. Whether it refuses the connection outright or says nothing at
      // all within the client's patience is the operating system's choice, and the two are worded differently;
      // what is being claimed here is the part that holds either way - the screen names the server it could not
      // read, and does not present an empty library as if that were the answer.
      Assert.That(home.Status, Does.Contain(dead.ToString()), "it says which server it could not reach");
      Assert.That(home.Status, Does.Not.Contain("aspects on"), "and does not dress an absence up as a result");
      Assert.That(home.IsBusy, Is.False);
    }

    [Test]
    public async Task ALibraryThatIsStillStartingIsReportedAsNotReady()
    {
      // The state a request that arrives while the server boots lands in. The view model's job here is the
      // distinction: a start-up window is a reason to ask again, an error is something to read.
      InProcessApi.RegisterMediaLibrary(InProcessApi.CreateStartingLibrary().Object);

      InProcessApi api = InProcessApi.Start();
      _toDispose.Add(api);
      var client = new ServerApiClient(api.BaseUrl);
      _toDispose.Add(client);
      HomeViewModel home = new HomeViewModel(client);

      await home.RefreshAsync();

      Assert.That(home.Status, Does.StartWith("The server is not ready yet"),
          "a start-up window is a reason to ask again, not an error to read");
      Assert.That(home.Aspects, Is.Empty);
    }

    [Test]
    public async Task WithoutAClientTheViewModelSaysSoAndDoesNotPretend()
    {
      HomeViewModel home = new HomeViewModel();
      Assert.That(home.Status, Is.EqualTo("No server configured"));
      Assert.That(home.ServerUrl, Is.EqualTo("(none)"));

      await home.RefreshAsync();
      Assert.That(home.Status, Is.EqualTo("No server configured"));
    }

    [Test]
    public void AnAspectWithoutACountIsShownAsUnknownAndNotAsZero()
    {
      AspectViewModel aspect = new AspectViewModel(
          new MediaAspect(InProcessApi.CORE_ASPECT_ID, "Core", true, false, null, new List<AspectAttribute>()));

      Assert.That(aspect.CountText, Is.EqualTo("?"));
      aspect.Count = 0;
      Assert.That(aspect.CountText, Is.EqualTo("0"), "a real zero is not the same as not knowing");
    }
  }
}
