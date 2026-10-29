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
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The news screen: the feed list MediaPortal publishes for the viewer's language, the headlines the feeds
  /// answer with, and what happens when one of them refuses.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Nothing here talks to the internet. The feed list is the shape the real one has - regions of feeds under
  /// <c>RegionalFeeds</c>, downloaded from the address the old plugin used - but served from a handler inside
  /// this file, and the feeds are an RSS 2.0 document, an Atom document and a refusal, all written here. That
  /// keeps what is under test in this front end: the culture ladder, the merging, the dates, the stripping of
  /// markup, and the naming of a feed that did not answer. Whether the foundation's server answers today with
  /// what it answered when this was written is the foundation's business, and the screen says a refusal out loud
  /// when it happens.
  /// </para>
  /// <para>
  /// No test awaits, for the reason spelled out in <c>TestMediaBrowser</c>; the view-model tests pump the queue
  /// until the screen says it is done.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestNewsScreen
  {
    #region Fixtures

    /// <summary>
    /// The default feed list in the shape MediaPortal publishes it - three regions so that the ladder has
    /// something to climb: an exact language-plus-region, a bare language, and the English everybody ends at.
    /// </summary>
    private const string DEFAULT_FEEDS_XML = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RegionalFeeds>
  <Region Code=""en""><Feeds>
    <Feed Name=""TV Weekly"">http://127.0.0.1:1/feeds/tv.rss</Feed>
    <Feed Name=""Tech Wire"">http://127.0.0.1:1/feeds/tech.atom</Feed>
    <Feed Name=""Dead Paper"">http://127.0.0.1:1/feeds/dead</Feed>
  </Feeds></Region>
  <Region Code=""de""><Feeds>
    <Feed Name=""Spiegel"">http://127.0.0.1:1/feeds/spiegel.rss</Feed>
  </Feeds></Region>
  <Region Code=""de-AT""><Feeds>
    <Feed Name=""ORF"">http://127.0.0.1:1/feeds/orf.rss</Feed>
  </Feeds></Region>
  <Region Code=""big""><Feeds>
    <Feed Name=""Big Paper"">http://127.0.0.1:1/feeds/big.rss</Feed>
  </Feeds></Region>
</RegionalFeeds>";

    /// <summary>
    /// An RSS 2.0 feed. The first item buries its summary in markup the way feeds do, and the second one has no
    /// date at all - feeds leave undated items out of nobody's ordering, so the screen has to have an opinion.
    /// </summary>
    private const string TV_RSS = @"<?xml version=""1.0"" encoding=""utf-8""?>
<rss version=""2.0""><channel>
<title>TV Weekly</title><link>http://127.0.0.1:1/</link><description>What is on tonight</description>
<item><title>Series returns</title><pubDate>Fri, 02 Jan 2026 08:00:00 GMT</pubDate>
<description><![CDATA[<p>Big <b>news</b> &amp; more</p>]]></description></item>
<item><title>Notes without a date</title><description>An item the feed forgot to date</description></item>
</channel></rss>";

    /// <summary>
    /// An Atom feed carrying both a summary and full content, because the reader has to have an opinion about
    /// which of the two is the story - it takes the longer one, as the old reader did.
    /// </summary>
    private const string TECH_ATOM = @"<?xml version=""1.0"" encoding=""utf-8""?>
<feed xmlns=""http://www.w3.org/2005/Atom"">
<title>Tech Wire</title><updated>2026-01-03T10:00:00Z</updated>
<entry>
  <title>New player ships</title>
  <id>urn:uuid:6d67e0f1-3a1d-4c2e-9a11-0f9e8d7c6b5a</id>
  <published>2026-01-03T10:00:00Z</published>
  <updated>2026-01-03T11:00:00Z</updated>
  <summary>Just the summary</summary>
  <content type=""text"">The full story of the player</content>
</entry>
</feed>";

    private const string ORF_RSS = @"<?xml version=""1.0"" encoding=""utf-8""?>
<rss version=""2.0""><channel>
<title>ORF</title><link>http://127.0.0.1:1/</link><description>Austrian news</description>
<item><title>Wiener Ball</title><pubDate>Sat, 03 Jan 2026 18:00:00 GMT</pubDate>
<description>Probe in Wien</description></item>
</channel></rss>";

    /// <summary>
    /// A paper with hundreds of headlines, because that is what a real one carries - and the number that decides
    /// whether the headlines column builds a container for every story or only for the ones it could be showing.
    /// </summary>
    private static string BigFeed(int items)
    {
      StringBuilder feed = new StringBuilder(
          @"<?xml version=""1.0"" encoding=""utf-8""?><rss version=""2.0""><channel><title>Big Paper</title>");
      for (int i = 0; i < items; i++)
        feed.Append("<item><title>Story number ").Append(i)
            .Append("</title><pubDate>Sat, 03 Jan 2026 12:00:0").Append(i % 10)
            .Append(@" GMT</pubDate><description>The words of story ").Append(i)
            .Append("</description></item>");
      feed.Append("</channel></rss>");
      return feed.ToString();
    }

    /// <summary>
    /// The internet, scripted: the feed list under the address the client asks for, two feeds that answer, one
    /// that refuses with a 500, and a memory of every address it was asked for.
    /// </summary>
    private class NewsServer : HttpMessageHandler
    {
      internal readonly List<string> RequestedPaths = new List<string>();

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        RequestedPaths.Add(path);

        string body;
        if (path.EndsWith("/DefaultFeeds.xml", StringComparison.Ordinal))
          body = DEFAULT_FEEDS_XML;
        else if (path.EndsWith("/big.rss", StringComparison.Ordinal))
          body = BigFeed(260);
        else if (path.EndsWith("/tv.rss", StringComparison.Ordinal))
          body = TV_RSS;
        else if (path.EndsWith("/tech.atom", StringComparison.Ordinal))
          body = TECH_ATOM;
        else if (path.EndsWith("/orf.rss", StringComparison.Ordinal))
          body = ORF_RSS;
        else if (path.EndsWith("/dead", StringComparison.Ordinal))
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
          {
            RequestMessage = request,
            Content = new StringContent("gone", Encoding.UTF8, "text/plain")
          });
        else
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/rss+xml")
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

    private static NewsViewModel ScreenOn(NewsServer server, string cultureName)
    {
      return new NewsViewModel(new NewsApiClient(new HttpClient(server)), cultureName);
    }

    /// <summary>Opens the screen the way the window does - by pressing refresh - and waits until it has stopped.</summary>
    private static void OpenAndWait(NewsViewModel screen, int expectedFeeds)
    {
      screen.RefreshCommand.Execute(null);
      WaitUntil("the feeds to be read",
          () => screen.Feeds.Count == expectedFeeds && !screen.IsBusy && !screen.Status.StartsWith("Not read"));
    }

    #endregion

    #region The feed list

    [Test]
    public void TheCultureClimbsFromItsLanguageToEnglish()
    {
      IReadOnlyList<NewsFeedBookmark> vienna = NewsApiClient.PickRegion(DEFAULT_FEEDS_XML, "de-AT");
      Assert.That(vienna.Select(feed => feed.Name), Is.EqualTo(new[] { "ORF" }),
          "a viewer whose interface language is Austrian German gets the Austrian list, not the generic German "
          + "one - the exact culture is the first rung of the ladder the old plugin climbed");

      IReadOnlyList<NewsFeedBookmark> germany = NewsApiClient.PickRegion(DEFAULT_FEEDS_XML, "de-DE");
      Assert.That(germany.Select(feed => feed.Name), Is.EqualTo(new[] { "Spiegel" }),
          "a culture the list has no region for falls to its two-letter language - the list has 'de', and "
          + "somebody from Germany is not left in English just because nobody wrote 'de-DE'");

      IReadOnlyList<NewsFeedBookmark> france = NewsApiClient.PickRegion(DEFAULT_FEEDS_XML, "fr");
      Assert.That(france.Select(feed => feed.Name),
          Is.EqualTo(new[] { "TV Weekly", "Tech Wire", "Dead Paper" }),
          "and a language the list carries nowhere at all ends at English, because the ladder has to end "
          + "somewhere and no feeds is not somewhere a news screen can end at");

      Assert.That(NewsApiClient.PickRegion(DEFAULT_FEEDS_XML, "fr").First().Url,
          Is.EqualTo("http://127.0.0.1:1/feeds/tv.rss"),
          "the address travels with the name - the XML's text is where the headlines get fetched from");
    }

    [Test]
    [AvaloniaTest]
    public void TheScreenAsksForTheViewersOwnLanguage()
    {
      NewsServer internet = new NewsServer();
      NewsViewModel screen = ScreenOn(internet, "de-AT");
      OpenAndWait(screen, 2);

      Assert.That(screen.Feeds.Select(feed => feed.Name), Is.EqualTo(new[] { "All headlines", "ORF" }),
          "the culture the screen was built with is the culture the list is picked with - a viewer in Vienna "
          + "reads the Austrian feeds without having to configure anything");
      Assert.That(screen.Headlines.Select(row => row.Title), Is.EqualTo(new[] { "Wiener Ball" }));
    }

    #endregion

    #region The headlines

    [Test]
    [AvaloniaTest]
    public void TheScreenReadsEveryFeedTheMomentItOpens()
    {
      NewsServer internet = new NewsServer();
      NewsViewModel screen = ScreenOn(internet, "en");
      OpenAndWait(screen, 4);

      Assert.That(internet.RequestedPaths.Count(path => path.EndsWith("/DefaultFeeds.xml", StringComparison.Ordinal)),
          Is.EqualTo(1), "the feed list is asked for exactly once per refresh, not once per feed");
      Assert.That(internet.RequestedPaths.Count(path => path.EndsWith("/tv.rss", StringComparison.Ordinal)),
          Is.EqualTo(1), "opening the screen is itself the request - a screen called News that waits for a "
          + "button is a screen that says the news is over");
      Assert.That(internet.RequestedPaths.Count(path => path.EndsWith("/tech.atom", StringComparison.Ordinal)),
          Is.EqualTo(1));
      Assert.That(internet.RequestedPaths.Count(path => path.EndsWith("/dead", StringComparison.Ordinal)),
          Is.EqualTo(1), "the feed that always refuses is asked too, because refusing is an answer and naming it "
          + "is the screen's job");

      Assert.That(screen.Feeds.Select(feed => feed.Name),
          Is.EqualTo(new[] { "All headlines", "TV Weekly", "Tech Wire", "Dead Paper" }),
          "the merged list first, then one row per feed on MediaPortal's list, in the list's own order");
      Assert.That(screen.Feeds[1].State, Is.EqualTo("2 headlines"),
          "the row carries its own count, so the viewer can see which papers spoke before selecting any of them");
    }

    [Test]
    [AvaloniaTest]
    public void ARefusedFeedIsNamedWhileTheOthersStillSpeak()
    {
      NewsServer internet = new NewsServer();
      NewsViewModel screen = ScreenOn(internet, "en");
      OpenAndWait(screen, 4);

      NewsFeedRow dead = screen.Feeds.Single(feed => feed.Name == "Dead Paper");
      Assert.That(dead.State, Does.Contain("500"),
          "the refusal sits in the feed's own row rather than vanishing; \"0 headlines\" would be a lie - the "
          + "paper did not come out empty, it did not come out");

      Assert.That(screen.Status, Does.Contain("Dead Paper"),
          "the status line names the paper that refused, beside the count of what did arrive");
      Assert.That(screen.Headlines.Count, Is.EqualTo(3),
          "and the refusal costs exactly nothing of what the other two feeds delivered - one dead newspaper is "
          + "not a reason for a blank screen");
    }

    [Test]
    [AvaloniaTest]
    public void TheStoryIsTheWordsAndNotTheMarkup()
    {
      NewsServer internet = new NewsServer();
      NewsViewModel screen = ScreenOn(internet, "en");
      OpenAndWait(screen, 4);

      NewsHeadlineRow series = screen.Headlines.Single(row => row.Title == "Series returns");
      Assert.That(series.Summary, Is.EqualTo("Big news & more"),
          "feeds bury their summaries in HTML, and a story panel that printed the tags would be a panel of angle "
          + "brackets; the words are what a viewer came for, entities included");

      NewsHeadlineRow player = screen.Headlines.Single(row => row.Title == "New player ships");
      Assert.That(player.Summary, Is.EqualTo("The full story of the player"),
          "when a feed carries both a summary and the full content, the content is the story - the same order "
          + "the old reader tried, because a feed that publishes both means the second one");
    }

    [Test]
    [AvaloniaTest]
    public void TheMergedHeadlinesAreNewestFirstAndTheUndatedLast()
    {
      NewsServer internet = new NewsServer();
      NewsViewModel screen = ScreenOn(internet, "en");
      OpenAndWait(screen, 4);

      Assert.That(screen.Headlines.Select(row => row.Title),
          Is.EqualTo(new[] { "New player ships", "Series returns", "Notes without a date" }),
          "the merged list is one newspaper made of three: newest first across feeds - the Atom feed's date is "
          + "later than the RSS feed's, so it leads although its feed is second in the list - and the undated "
          + "item last, because a feed that gives no date is not claiming to be the newest story anywhere");

      Assert.That(screen.Headlines.Single(row => row.Title == "Notes without a date").DateText,
          Is.EqualTo("no date"),
          "and the screen says that out loud instead of printing a placeholder date");

      Assert.That(screen.SelectedHeadline, Is.SameAs(screen.Headlines[0]),
          "a story is selected from the first frame, so the words panel is not empty when the screen opens");

      Assert.That(screen.Status, Does.Contain("3 headlines from 2 of 3 feeds"),
          "the arithmetic of what arrived, in one line");
    }

    [Test]
    [AvaloniaTest]
    public void APaperShowsOnlyItsOwnHeadlines()
    {
      NewsServer internet = new NewsServer();
      NewsViewModel screen = ScreenOn(internet, "en");
      OpenAndWait(screen, 4);

      screen.SelectedFeed = screen.Feeds.Single(feed => feed.Name == "TV Weekly");
      Pump();

      Assert.That(screen.Headlines.Select(row => row.Title),
          Is.EqualTo(new[] { "Series returns", "Notes without a date" }),
          "selecting a paper narrows the middle column to that paper; nothing is fetched again on the way - the "
          + "feeds were read when the screen opened and walking the column only walks what arrived");
      Assert.That(screen.Headlines.Select(row => row.FeedName).Distinct().ToArray(), Is.EqualTo(new[] { "TV Weekly" }),
          "every row still carries the name of its paper, because a one-paper column reads like the merged list "
          + "otherwise");

      screen.SelectedFeed = screen.Feeds[0];
      Pump();
      Assert.That(screen.Headlines.Count, Is.EqualTo(3),
          "and \"All headlines\" is the way back to the merged list");
    }

    #endregion

    #region The viewer's own feeds

    [Test]
    [AvaloniaTest]
    public void TheViewersOwnFeedsReadOnTopOfMediasList()
    {
      NewsServer server = new NewsServer();
      NewsFeedsSettings own = new NewsFeedsSettings
      {
        IntervalMinutes = 15,
        ExtraFeeds =
        {
          new NewsFeedEntry { Name = "Mein Blog", Url = "http://127.0.0.1:1/feeds/orf.rss" },
          // A name with no address is a half an answer; it must not become a row that blames the network.
          new NewsFeedEntry { Name = "Half-written", Url = "  " }
        }
      };

      NewsViewModel screen = new NewsViewModel(new NewsApiClient(new HttpClient(server)), "en", own);
      OpenAndWait(screen, 5);

      Assert.That(screen.Feeds.Select(row => row.Name),
          Is.EqualTo(new[] { "All headlines", "TV Weekly", "Tech Wire", "Dead Paper", "Mein Blog" }),
          "MediaPortal's list first, the viewer's own on top of it, and the half-written one is not a feed yet");
      Assert.That(screen.Feeds[4].State, Is.EqualTo("1 headline"),
          "the viewer's feed is read like any other and answers with its own count");
      Assert.That(screen.Headlines.Select(row => row.Title), Does.Contain("Wiener Ball"),
          "and its headlines are in the merged list from the first read, not only after a second press");

      Assert.That(server.RequestedPaths.Any(path => path.Contains("Half", StringComparison.OrdinalIgnoreCase)),
          Is.False, "nothing was ever asked of a feed that has no address");
      screen.Dispose();
    }

    [Test]
    public void TheFeedsFileSurvivesBeingWrittenAndReadAgain()
    {
      string file = Path.Combine(Path.GetTempPath(),
          "news-feeds-" + Guid.NewGuid().ToString("N") + ".json");
      try
      {
        NewsFeedsSettings written = new NewsFeedsSettings { IntervalMinutes = 30 };
        written.ExtraFeeds.Add(new NewsFeedEntry { Name = "Mein Blog", Url = "http://example.org/feed" });
        string message = written.Save(file, out bool saved);
        Assert.That(saved, Is.True, message);

        NewsFeedsSettings read = NewsFeedsSettings.Load(file, out string? problem);
        Assert.That(problem, Is.Null);
        Assert.That(read.IntervalMinutes, Is.EqualTo(30));
        Assert.That(read.ExtraBookmarks().Select(mark => mark.Name + "=" + mark.Url),
          Is.EqualTo(new[] { "Mein Blog=http://example.org/feed" }));

        Assert.That(NewsFeedsSettings.Combine(read.ExtraBookmarks(), read.ExtraBookmarks()).Count, Is.EqualTo(2),
            "combining is appending, and an empty addition leaves the list standing alone");
        Assert.That(NewsFeedsSettings.Combine(Array.Empty<NewsFeedBookmark>(), read.ExtraBookmarks()).Count,
            Is.EqualTo(1),
            "and an official list with nothing on it still carries the viewer's own feeds");
      }
      finally
      {
        File.Delete(file);
      }
    }

    #endregion

    #region The tile

    [Test]
    [AvaloniaTest]
    public void TheNewsTileIsOpenableWithoutAServer()
    {
      HomeViewModel home = new HomeViewModel();
      MainMenuEntry news = home.Menu.Single(entry => entry.Label == "News");

      Assert.That(news.ScreenKey, Is.EqualTo("news"),
          "the tile leads somewhere real: the home screen routes this key to the news screen");
      Assert.That(news.Missing, Is.Null,
          "the news was never the server's data - the old plugin read the feeds from the television too - so the "
          + "tile has no reason to apologise: it opens with a server, without one, and without media");
      Assert.That(news.Openable, Is.True);
    }

    #endregion

    #region Walking the screen

    private static void SendKey(Control source, Key key)
    {
      source.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = key,
        Source = source
      });
      Pump();
    }

    private static T Found<T>(Window window, string name) where T : Control
    {
      return window.GetVisualDescendants().OfType<T>().First(control => control.Name == name);
    }

    /// <summary>
    /// The three columns are one ring: Right walks the caret from a paper onto its headlines and on to the
    /// story, Left walks it back, Enter steps from a paper onto its headlines as the reference's Select did,
    /// and Left at the leading edge of the papers slides the screen's own menu in - from where the opposite
    /// key walks the caret back onto the papers.  None of this may depend on the mouse: the screen lives in a
    /// room with a remote.
    /// </summary>
    [AvaloniaTest]
    public void TheCaretWalksFromThePapersThroughTheHeadlinesToTheStory()
    {
      NewsServer internet = new NewsServer();
      NewsViewModel news = ScreenOn(internet, "en");
      OpenAndWait(news, 4);

      ShellWindow window = new ShellWindow(new NewsView(news));
      window.Show();
      ListBox feeds = Found<ListBox>(window, "feedsList");
      ListBox headlines = Found<ListBox>(window, "headlinesList");
      ScrollViewer story = Found<ScrollViewer>(window, "storyScroller");
      WaitUntil("the papers to stand on the screen", () => feeds.Items.Count == 4);

      feeds.Focus();
      feeds.SelectedIndex = 0;
      Pump();

      SendKey(feeds, Key.Right);
      Assert.That(headlines.IsKeyboardFocusWithin, Is.True,
          "Right from a paper never reached the headlines - the middle column cannot be operated at all");

      SendKey(headlines, Key.Right);
      Assert.That(story.IsKeyboardFocusWithin, Is.True,
          "Right from the headlines never reached the reading column");

      SendKey(story, Key.Right);
      Assert.That(feeds.IsKeyboardFocusWithin, Is.True,
          "the ring does not wrap, so the caret stands on the story with no way back but Left");

      SendKey(feeds, Key.Enter);
      Assert.That(headlines.IsKeyboardFocusWithin, Is.True,
          "Enter on a paper does not step onto its headlines");

      SendKey(headlines, Key.Left);
      Assert.That(feeds.IsKeyboardFocusWithin, Is.True,
          "Left from the headlines does not walk back onto the papers");

      // The papers' leading edge is the menu's door, and the menu is not the ring's.
      feeds.SelectedIndex = 0;
      SendKey(feeds, Key.Left);
      ListBox menu = Found<ListBox>(window, "newsMenuList");
      Assert.That(menu.IsKeyboardFocusWithin, Is.True,
          "Left at the first paper does not slide the screen's menu in");
      SendKey(menu, Key.Right);
      Assert.That(feeds.IsKeyboardFocusWithin, Is.True,
          "Right does not walk the caret out of the menu back onto the papers");

      // A window left standing here goes on being laid out by every later pump, and a skin put on or off in a
      // later test re-dresses its columns - a stranger's screen paying rent in somebody else's test.
      window.Close();
      Pump();
    }

    /// <summary>
    /// The screen's menu stands with all the rows it promises, not with a solitary "Feeds" and the rest
    /// vanished between the list and its panel - the disappearance a viewer kept seeing. The guard counts
    /// the entries, walks every one of them to a realized container, and demands the row's name to stand
    /// on the glass in a visible TextBlock, because a row the panel never built is exactly the bug.
    /// </summary>
    [AvaloniaTest]
    public void TheScreensMenuStandsWithEveryRowItPromises()
    {
      NewsServer internet = new NewsServer();
      NewsViewModel news = ScreenOn(internet, "en");
      OpenAndWait(news, 4);

      ShellWindow window = new ShellWindow(new NewsView(news));
      window.Show();
      ListBox feeds = Found<ListBox>(window, "feedsList");
      WaitUntil("the papers to stand on the screen", () => feeds.Items.Count == 4);

      feeds.Focus();
      feeds.SelectedIndex = 0;
      SendKey(feeds, Key.Left);
      ListBox menu = Found<ListBox>(window, "newsMenuList");
      WaitUntil("the menu to hold its rows", () => menu.Items.Count >= 3);

      // Built means seen: every entry wears a container the panel laid out with a real height, and the
      // row's name reaches a visible caption. One row short of this and the rail offers commands
      // without names - which is what the viewer called "nur ein Eintrag".
      Assert.That(menu.Items.Count, Is.EqualTo(3),
          "the rail carries the papers, the shape of the headlines and the refresh - no more without a frame to open the settings");
      string stood = string.Join(" | ", menu.GetVisualDescendants().OfType<ListBoxItem>()
          .Select(i => $"{i.Bounds} {(i.DataContext as NewsMenuEntry)?.Label ?? "?"}"));
      for (int row = 0; row < menu.Items.Count; row++)
      {
        object entry = menu.Items[row]!;
        ListBoxItem? container = menu.GetVisualDescendants().OfType<ListBoxItem>()
            .FirstOrDefault(item => ReferenceEquals(item.DataContext, entry));
        Assert.That(container, Is.Not.Null,
            $"row {row} never got a container - the rail stands with: {stood}");
        Assert.That(container!.Bounds.Height, Is.GreaterThan(50),
            $"row {row} was built but stands with no height");
      }

      List<string?> names = menu.GetVisualDescendants().OfType<TextBlock>()
          .Where(t => t.IsVisible && !string.IsNullOrEmpty(t.Text))
          .Select(t => t.Text)
          .ToList();
      Assert.That(names, Does.Contain("Feeds"), "the papers' row wears no name");
      Assert.That(names, Does.Contain("List"), "the headlines' shape row wears no name");
      Assert.That(names, Does.Contain("Refresh"), "the refresh row wears no name");

      window.Close();
      Pump();
    }

    /// <summary>
    /// The frame paints on the papers exactly as it paints on the headlines. Measured live as "bei den Feeds
    /// fehlt der Focus-Rahmen, wenn ein Feed Fokus hat" while the accessibility tree proved a feed row held
    /// the keyboard - a caret nobody can see is no caret at all. The row the keyboard stands on wears the
    /// frame, and when the keyboard walks away the frame walks with it: one frame in the whole screen, which
    /// is what answers the question "welche Liste hat den Fokus".
    /// </summary>
    [AvaloniaTest]
    public void ThePaperUnderTheKeyboardWearsTheFrameAndLaysItDownWhenTheKeyboardLeaves()
    {
      NewsServer internet = new NewsServer();
      NewsViewModel news = ScreenOn(internet, "en");
      OpenAndWait(news, 4);

      ShellWindow window = new ShellWindow(new NewsView(news));
      window.Show();
      ListBox feeds = Found<ListBox>(window, "feedsList");
      ListBox headlines = Found<ListBox>(window, "headlinesList");
      WaitUntil("the papers to stand on the screen", () => feeds.Items.Count == 4);

      feeds.Focus();
      feeds.SelectedIndex = 0;
      SendKey(feeds, Key.Down);

      ListBoxItem? caret = feeds.GetLogicalDescendants().OfType<ListBoxItem>()
          .FirstOrDefault(row => row.IsFocused);
      Assert.That(caret, Is.Not.Null, "the keyboard must stand ON a paper, not on the bare column");
      Border? edge = caret!.GetVisualDescendants().OfType<Border>()
          .FirstOrDefault(b => b.Classes.Contains("home-tile-edge"));
      Assert.That(edge, Is.Not.Null, "the paper's tile carries no edge to paint the frame on");
      Assert.That(edge!.BorderBrush, Is.Not.Null,
          "the paper the keyboard stands on must wear the frame - live it wore nothing");

      // Right hands the caret to the headlines; the frame must leave the paper and stand on the headline.
      SendKey(feeds, Key.Right);
      WaitUntil("the headlines to take the keyboard", () => headlines.IsKeyboardFocusWithin);

      object left_behind = caret.DataContext!;
      IBrush? kept = feeds.GetLogicalDescendants().OfType<ListBoxItem>()
          .Where(row => ReferenceEquals(row.DataContext, left_behind))
          .SelectMany(row => row.GetVisualDescendants().OfType<Border>()
              .Where(b => b.Classes.Contains("home-tile-edge")))
          .Select(b => (IBrush?)b.BorderBrush)
          .FirstOrDefault();
      Assert.That(kept, Is.Null,
          "a paper that lost the keyboard wears the frame no longer - two frames ask nothing");

      ListBoxItem? headline = headlines.GetLogicalDescendants().OfType<ListBoxItem>()
          .FirstOrDefault(row => row.IsFocused);
      Assert.That(headline, Is.Not.Null, "the caret never stood on a headline");
      Border? theirs = headline!.GetVisualDescendants().OfType<Border>()
          .FirstOrDefault(b => b.Classes.Contains("home-tile-edge"));
      Assert.That(theirs!.BorderBrush, Is.Not.Null, "the headline the keyboard stands on wears no frame");

      window.Close();
      Pump();
    }

    /// <summary>
    /// A wide screen lays the headlines as the mockup's grid of fixed-width tiles, in as many columns as the
    /// area fills, and inside that grid Left and Right belong to the GRID: Right steps from a tile to its
    /// right neighbour and Left back, the walk the shelf's wall gives its posters. Only a true edge of the
    /// grid opens a column door - Right on the last column's tile walks onto the story, Left on the first
    /// column's tile onto the papers. The narrow window's one-column shape is the same law with every row on
    /// both edges, which is what the walk test above walks.
    /// </summary>
    [AvaloniaTest]
    public void TheWideGridWalksTileToTileAndOpensItsDoorsOnlyAtItsEdges()
    {
      NewsServer internet = new NewsServer();
      // The wide column stands five tiles abreast, so the walking needs a paper with more than one row of
      // headlines under it - the "en" fixture's three stories would run out mid-row. The big paper's two
      // hundred and sixty are what a real one carries anyway.
      NewsViewModel news = ScreenOn(internet, "big");
      OpenAndWait(news, 2);

      ShellWindow window = new ShellWindow(new NewsView(news)) { Width = 3440, Height = 1440 };
      window.Show();
      ListBox feeds = Found<ListBox>(window, "feedsList");
      ListBox headlines = Found<ListBox>(window, "headlinesList");
      ScrollViewer story = Found<ScrollViewer>(window, "storyScroller");
      WaitUntil("the headlines to stand on the screen", () => headlines.Items.Count == 260);
      VirtualizingTilePanel grid = headlines.GetVisualDescendants()
          .OfType<VirtualizingTilePanel>().FirstOrDefault();
      Assert.That(grid, Is.Not.Null, "the headlines are the grid shape, not the list shape");
      // A wide column does not split in half any more: it fills with tiles of the mockup's fixed width, and
      // a 3440-wide window must carry at least three of them side by side.
      WaitUntil("the wide column to fill with at least three tile columns", () => grid.Columns >= 3);
      int columns = grid.Columns;

      feeds.Focus();
      feeds.SelectedIndex = 0;
      SendKey(feeds, Key.Right);
      Pump();
      Assert.That(headlines.IsKeyboardFocusWithin, Is.True, "Right from a paper must reach the headlines");
      if (headlines.SelectedIndex < 0)
        headlines.SelectedIndex = 0;

      // Right on a tile that has a neighbour to its right steps sideways within the row; it may not leave.
      SendKey(headlines, Key.Right);
      Pump();
      Assert.That(headlines.IsKeyboardFocusWithin, Is.True,
          "Right inside a row of tiles jumped out of the column - the viewer cannot walk the grid");
      Assert.That(headlines.SelectedIndex, Is.EqualTo(1),
          "Right on the first tile did not step onto the tile beside it");

      // Right steps tile to tile along the row - every step is the grid's own - until the last column's
      // tile, where the next Right is the door to the story.
      for (int step = 2; step <= columns - 1; step++)
      {
        SendKey(headlines, Key.Right);
        Pump();
        Assert.That(headlines.IsKeyboardFocusWithin, Is.True,
            $"Right stepped out of the grid before reaching its right edge (at tile {step})");
        Assert.That(headlines.SelectedIndex, Is.EqualTo(step),
            $"Right did not step one tile sideways (wanted tile {step}; grid says {grid.Columns} columns, "
            + $"standing {grid.StandingCount} containers from {grid.FirstStandingIndex})");
      }

      SendKey(headlines, Key.Right);
      Pump();
      Assert.That(story.IsKeyboardFocusWithin, Is.True,
          "Right on the grid's right edge does not open the door to the reading column "
          + $"(columns={grid.Columns}, selected={headlines.SelectedIndex})");
      SendKey(story, Key.Left);
      Pump();
      Assert.That(headlines.IsKeyboardFocusWithin, Is.True,
          "Left from the story does not walk back onto the headlines");

      // Left walks one tile at a time back to the first column - every press belongs to the grid - and the
      // press on the FIRST column's tile is the door to the papers.
      for (int step = columns - 2; step >= 0; step--)
      {
        SendKey(headlines, Key.Left);
        Pump();
        Assert.That(headlines.IsKeyboardFocusWithin, Is.True,
            $"Left stepped out of the grid before reaching its left edge (at tile {step + 1})");
        Assert.That(headlines.SelectedIndex, Is.EqualTo(step),
            $"Left did not step one tile back (wanted tile {step})");
      }

      SendKey(headlines, Key.Left);
      Pump();
      Assert.That(feeds.IsKeyboardFocusWithin, Is.True,
          "Left on the grid's left edge does not open the door to the papers");

      window.Close();
      Pump();
    }

    /// <summary>
    /// A real paper carries hundreds of headlines, and the column may only dress the ones the screen could be
    /// drawing: every tile of this grid wears a photograph and a shadow, and when a whole paper's containers
    /// stood at once the live screen froze the UI thread for seconds at a stretch - Windows filed the frozen
    /// window as AppHangTransient and the viewer could not use the page at all. Walking to the last story is
    /// the same handful of containers all the way, and it must stay walkable with the keyboard alone.
    /// </summary>
    [AvaloniaTest]
    public void AHundredPapersWorthOfHeadlinesOnlyBuildsTheTilesTheScreenCouldShow()
    {
      NewsServer internet = new NewsServer();
      NewsViewModel news = ScreenOn(internet, "big");
      OpenAndWait(news, 2);
      Assert.That(news.Headlines.Count, Is.EqualTo(260),
          "the fixture paper must carry hundreds of headlines, or nothing here is testing the hundreds");

      ShellWindow window = new ShellWindow(new NewsView(news)) { Width = 1920, Height = 1080 };
      window.Show();
      ListBox headlines = Found<ListBox>(window, "headlinesList");
      WaitUntil("the headlines to stand", () => headlines.Items.Count == 260);

      VirtualizingTilePanel panel = headlines.GetVisualDescendants()
          .OfType<VirtualizingTilePanel>().FirstOrDefault();
      Assert.That(panel, Is.Not.Null,
          "the headlines grid lays its tiles on the shelf's virtualizing panel, not on a plain grid");
      WaitUntil("a band of tiles to be realized", () => panel.StandingCount > 0);
      int standingAtFirst = panel.StandingCount;
      Assert.That(standingAtFirst, Is.LessThan(100),
          "a column that builds all 260 tiles freezes the screen; only the band may stand");

      // Down is the column's own key and stays the list's; forty presses must walk it through rows the panel
      // only builds as they come near, without ever letting the whole paper stand at once.
      headlines.Focus();
      headlines.SelectedIndex = 0;
      for (int press = 0; press < 40; press++)
        SendKey(headlines, Key.Down);
      // The column carries two of the mockup's fixed tiles at this window width, so the list's own geometric
      // walk takes the caret one whole row per press - straight down to the tile under the one it stood on,
      // which is two of the flat list's rows. Forty presses land on the eightieth headline.
      Assert.That(headlines.SelectedIndex, Is.EqualTo(80),
          "Down walks the grid one row at a time; it stopped at index " + headlines.SelectedIndex);
      Assert.That(panel.StandingCount, Is.LessThan(100),
          "walking the grid woke a hundred containers - the band is not being kept");
      Assert.That(panel.FirstStandingIndex, Is.GreaterThan(0),
          "the band never followed the caret down");
      // What the glide makes unavoidable is that rows STOOD along the way - the caret travels, it does not
      // teleport, and every station of a travelling caret realises its band once. The law that still holds,
      // and the one that freezes screens when it breaks, is that never more than a band stands at a time
      // (asserted above) and that the rows walked past went back into the pool rather than standing: the
      // build count may therefore read the whole journey (here two tiles over forty rows), but it must stay
      // clearly under the paper's 260, and the pool must visibly catch the handovers.
      Assert.That(panel.ContainersBuilt, Is.LessThan(200),
          "every row walked past built its own container instead of taking one from the pool");
      Assert.That(panel.ContainersReused, Is.GreaterThan(50),
          "the pool caught nothing - containers were thrown away rather than handed to the next band");

      // And the list shape keeps its rows virtual too - the menu's second row turns the column over, and a
      // stack panel would build all 260 rows of it just the same.
      headlines.Classes.Remove("news-grid");
      headlines.Classes.Add("news-list");
      Pump();
      Assert.That(headlines.GetVisualDescendants().Any(p => p is VirtualizingStackPanel), Is.True,
          "the list shape lays its rows on something that does not build two hundred and sixty at once");
      Assert.That(headlines.GetVisualDescendants().OfType<VirtualizingTilePanel>().Any(), Is.False,
          "the grid's panel outlived the shape that asked for it");

      window.Close();
    }

    #endregion

  }
}

