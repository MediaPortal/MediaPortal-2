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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;
using FrontendAspect = MediaPortal.Frontend.Api.MediaAspect;

namespace Tests.Frontend
{
  /// <summary>
  /// Browsing: the screen that asks the server what genres, years and people its titles carry, and the step from
  /// one of those answers to the titles wearing it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The counting itself is proven over in the server's own tests - there it is a GROUP BY the query compiler has
  /// to emit. What is proven here is the front end's half: that the two halves of a filter travel together or not
  /// at all, that the server's order and the server's numbers survive JSON, that a picked value opens the
  /// ordinary titles list rather than a second kind of list, and that an empty or refused answer is said rather
  /// than shown as a blank column.
  /// </para>
  /// <para>
  /// No test awaits, for the reason spelled out in <c>TestMediaBrowser</c>; the view-model tests pump the queue
  /// until the screen says it is done.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestBrowseScreen
  {
    #region Fixtures

    private static readonly Uri SIGN_IN_URL = new Uri("http://127.0.0.1:1/MediaPortal/Server/");

    private static readonly HomeSection MOVIES = new HomeSection("Movies",
        new AspectViewModel(new FrontendAspect(new Guid("11111111-1111-1111-1111-111111111111"), "MovieItem",
            false, false, null, Array.Empty<AspectAttribute>())));

    /// <summary>
    /// A genre answer with a hole in it: the empty value a database answers with for "no genre written" is in
    /// there, and so is a row order that is deliberately not alphabetical - the server sorts biggest first, and
    /// the screen has to keep that.
    /// </summary>
    private const string GROUPS = "{\"aspect\":\"MovieItem\",\"by\":\"genre\",\"groups\":["
        + "{\"value\":\"Drama\",\"count\":4},{\"value\":\"Action\",\"count\":2},"
        + "{\"value\":\"\",\"count\":9}]}";

    private const string TITLES = "{\"total\":2,\"offset\":0,\"limit\":60,\"returned\":2,\"sort\":\"title\","
        + "\"by\":\"genre\",\"value\":\"Drama\",\"titles\":["
        + "{\"id\":\"00000001-0000-0000-0000-000000000000\",\"title\":\"Alien\",\"year\":1979},"
        + "{\"id\":\"00000002-0000-0000-0000-000000000000\",\"title\":\"Brand X\",\"year\":1973}]}";

    /// <summary>
    /// A server that answers the groups URL with whatever it was handed and every titles URL with a shelf; it
    /// remembers both URLs so the tests can read what left the screen.
    /// </summary>
    private class BrowseServer : HttpMessageHandler
    {
      private readonly string _groupsBody;

      internal string LastGroupsUrl { get; private set; }

      internal string LastTitlesUrl { get; private set; }

      internal BrowseServer(string groupsBody = GROUPS)
      {
        _groupsBody = groupsBody;
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        if (path.EndsWith("/thumbnail", StringComparison.Ordinal))
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });

        string body;
        if (path.EndsWith("/groups", StringComparison.Ordinal))
        {
          LastGroupsUrl = request.RequestUri.PathAndQuery;
          body = _groupsBody;
        }
        else
        {
          // The screen's details read for the selected row arrives here too; it is answered from the shelf
          // body, but it is not the titles question and does not overwrite the URL it was asked with.
          if (!path.Contains("/items/", StringComparison.Ordinal))
            LastTitlesUrl = request.RequestUri.PathAndQuery;
          body = TITLES;
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
      }
    }

    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
    }

    private static void WaitUntil(string because, Func<bool> condition)
    {
      DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);
      while (DateTimeOffset.UtcNow < deadline)
      {
        Pump();
        if (condition())
          return;
      }
      Assert.Fail("Timed out after 15 seconds waiting for " + because);
    }

    private static BrowseViewModel ScreenOn(BrowseServer server)
    {
      return new BrowseViewModel(new ServerApiClient(SIGN_IN_URL, new HttpClient(server)),
          new List<HomeSection> { MOVIES });
    }

    #endregion

    #region The two endpoints

    [Test]
    public void TheTwoHalvesOfAGroupingTravelTogetherOrNotAtAll()
    {
      BrowseServer server = new BrowseServer();
      using (var client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server)))
      {
        Task.Run(delegate
        {
          return client.GetTitlesAsync("MovieItem", null, false, 60, 0, null, null, null, "genre", null,
              CancellationToken.None);
        }).GetAwaiter().GetResult();

        Assert.That(server.LastTitlesUrl, Does.Not.Contain("by="),
            "a category whose value went missing is not sent; the server would be right to refuse it, and a "
            + "screen that reads the whole shelf under a heading naming one genre is the bug the refusal exists "
            + "to prevent");
        Assert.That(server.LastTitlesUrl, Does.Not.Contain("value="));

        Task.Run(delegate
        {
          return client.GetTitlesAsync("MovieItem", null, false, 60, 0, null, null, null, "genre",
              "Science Fiction", CancellationToken.None);
        }).GetAwaiter().GetResult();

        Assert.That(server.LastTitlesUrl, Does.Contain("by=genre&value=Science%20Fiction"),
            "both halves, escaped - a genre with a space in its name is the same genre the library stores");
      }
    }

    [Test]
    public void TheCountsComeBackInTheServersOrderWithTheHolesTakenOut()
    {
      BrowseServer server = new BrowseServer();
      using (var client = new ServerApiClient(SIGN_IN_URL, new HttpClient(server)))
      {
        MediaGroups answer = Task.Run(delegate
        {
          return client.GetGroupsAsync("MovieItem", "genre", CancellationToken.None);
        }).GetAwaiter().GetResult();

        Assert.That(server.LastGroupsUrl, Does.Contain("aspect=MovieItem").And.Contain("by=genre"));
        Assert.That(answer.Values.Select(group => group.Value), Is.EqualTo(new[] { "Drama", "Action" }),
            "biggest first as the database counted, not re-sorted here - the order is part of the answer");
        Assert.That(answer.Values.Select(group => group.Count), Is.EqualTo(new[] { 4, 2 }));
      }
    }

    #endregion

    #region The screen

    [AvaloniaTest]
    public void TheScreenCountsTheShelfTheMomentItOpens()
    {
      BrowseServer server = new BrowseServer();
      BrowseViewModel browse = ScreenOn(server);
      browse.RefreshCommand.Execute(null);
      WaitUntil("the genres to arrive", delegate { return browse.Values.Count == 2; });

      Assert.That(browse.Values.Select(value => value.Value), Is.EqualTo(new[] { "Drama", "Action" }),
          "the empty group the database answers with for \"nobody wrote a genre\" is not a row a viewer can pick");
      Assert.That(browse.SelectedValue, Is.Not.Null,
          "the first value is already picked, because the arrows arrive before any decision does");
      Assert.That(browse.Status, Does.Contain("2 genres among Movies"),
          "plural by hand: \"2 genre\" is not a sentence");
    }

    [AvaloniaTest]
    public void PickingAnotherGroupingAsksTheServerAgain()
    {
      BrowseServer server = new BrowseServer();
      BrowseViewModel browse = ScreenOn(server);
      browse.RefreshCommand.Execute(null);
      WaitUntil("the first count to arrive", delegate { return browse.Values.Count == 2; });

      browse.SelectedCategory = BrowseViewModel.CATEGORIES.First(category => category.Word == "writers");
      WaitUntil("the second count to arrive", delegate
      {
        return server.LastGroupsUrl != null && server.LastGroupsUrl.Contains("by=writers");
      });

      Assert.That(server.LastGroupsUrl, Does.Contain("aspect=MovieItem"));
    }

    [AvaloniaTest]
    public void APressedValueHandsOverTheShelfWearingTheFilter()
    {
      BrowseServer server = new BrowseServer();
      BrowseViewModel browse = ScreenOn(server);
      HomeSection opened = null;
      browse.ValueChosen += delegate (object? sender, HomeSection section) { opened = section; };

      browse.RefreshCommand.Execute(null);
      WaitUntil("the genres to arrive", delegate { return browse.Values.Count == 2; });

      browse.SelectedValue = browse.Values.First(value => value.Value == "Action");
      Assert.That(browse.OpenCommand.CanExecute(null), Is.True);
      browse.OpenCommand.Execute(null);

      Assert.That(opened, Is.Not.Null, "pressing a value is a request to see its titles");
      Assert.That(opened.Label, Is.EqualTo("Movies"),
          "the screen that opens is the shelf's own, so the viewer knows where these titles came from");
      Assert.That(opened.Aspect.Name, Is.EqualTo("MovieItem"));
      Assert.That(opened.By, Is.EqualTo("genre"));
      Assert.That(opened.Value, Is.EqualTo("Action"));
    }

    [AvaloniaTest]
    public void AShelfNobodyTaggedSaysSoRatherThanShowingAnEmptyColumn()
    {
      BrowseServer server = new BrowseServer(
          "{\"aspect\":\"MovieItem\",\"by\":\"genre\",\"groups\":[]}");
      BrowseViewModel browse = ScreenOn(server);
      browse.RefreshCommand.Execute(null);
      WaitUntil("the answer to be read", delegate { return browse.Status.StartsWith("None of the Movies"); });

      Assert.That(browse.Values, Is.Empty);
      Assert.That(browse.Status, Does.Contain("carries a genre the importer wrote"),
          "the two possible nothings - no titles, or titles nobody tagged - name which one it is");
    }

    [AvaloniaTest]
    public void ARefusalFromTheServerIsSaidAndNotSwallowed()
    {
      BrowseServer server = new BrowseServer(
          "{\"aspect\":\"MovieItem\",\"by\":\"genre\",\"error\":\"this shelf has no genre\"}");
      BrowseViewModel browse = ScreenOn(server);
      browse.RefreshCommand.Execute(null);
      WaitUntil("the refusal to reach the status line",
          delegate { return browse.Status == "this shelf has no genre"; });

      Assert.That(browse.Values, Is.Empty, "a refusal is not an empty shelf, and the screen does not dress it as one");
    }

    [AvaloniaTest]
    public void TheTitlesScreenCarriesThePickedFilterAndSaysSo()
    {
      BrowseServer server = new BrowseServer();
      MediaBrowserViewModel browser = new MediaBrowserViewModel(
          new ServerApiClient(SIGN_IN_URL, new HttpClient(server)),
          new HomeSection(MOVIES.Label, MOVIES.Aspect, "genre", "Drama"));
      browser.OpenCommand.Execute(null);
      WaitUntil("the filtered titles to arrive", delegate { return browser.Titles.Count == 2; });

      Assert.That(server.LastTitlesUrl, Does.Contain("by=genre&value=Drama"),
          "the picked value travels into the ordinary titles query - browsing is a way into the list, not a "
          + "second list with its own rules");
      Assert.That(browser.Status, Does.Contain("under Drama"),
          "the count says what it is a count of, so the filtered shelf is not mistaken for the whole one");
    }

    [Test]
    public void TheBrowseTileOpensTheBrowseScreen()
    {
      HomeViewModel home = new HomeViewModel();
      MainMenuEntry browse = home.Menu.First(entry => entry.Label == "Browse media");

      Assert.That(browse.Missing, Is.Null,
          "the tile used to explain that the HTTP API could not group titles; it can now - the server counts a "
          + "shelf by genre, year, actors, directors and writers in one query each - so the tile opens the "
          + "browse screen instead of apologising");
      Assert.That(browse.ScreenKey, Is.EqualTo("browse"));
    }

    #endregion
  }
}
