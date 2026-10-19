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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// A feed the viewer can read: its name and the address its headlines are fetched from.
  /// </summary>
  public record NewsFeedBookmark(string Name, string Url);

  /// <summary>
  /// One headline, from whichever feed carried it.
  /// </summary>
  /// <param name="Title">What the feed calls the story.</param>
  /// <param name="FeedName">Which feed carried it, because "who says so" is half of a headline.</param>
  /// <param name="Published">When the feed says it was published, or null when it does not say.</param>
  /// <param name="Summary">The words under the headline, stripped of the markup feeds like to bury them in.</param>
  public record NewsHeadline(string Title, string FeedName, DateTimeOffset? Published, string Summary);

  /// <summary>
  /// The answer of one feed: its headlines, or the reason it could not be read. Never a silence - a feed that
  /// refused is named with its refusal, because "three of four feeds answered" and "nothing to show" are
  /// different situations and the viewer is entitled to know which one is on screen.
  /// </summary>
  public record NewsFeedAnswer(NewsFeedBookmark Feed, IReadOnlyList<NewsHeadline>? Headlines, string? Error);

  /// <summary>
  /// Reads the news feeds the front end shows under "News".
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>The front end asks the internet directly, because that is where the news is.</b> Every other screen of
  /// this application talks to the MP2 server and nothing else, and that separation is deliberate everywhere it
  /// holds - but news was never the server's data even in the old MP2, where the news plugin ran on the
  /// television and fetched the feeds itself. Routing headlines through the media server would give the server a
  /// job it has no endpoint for and this client a dependency on a machine that does not answer for RSS.
  /// </para>
  /// <para>
  /// <b>The feed list is MediaPortal's own.</b> The old plugin downloaded its default feeds from
  /// <c>install.team-mediaportal.com</c> and picked the region for the viewer's language, still the case here:
  /// the same address, the same XML, the same fallback of exact language-plus-region, then language, then
  /// English. What the foundation chooses to put on that list is the foundation's statement, not something a
  /// screen should quietly substitute its own taste for.
  /// </para>
  /// <para>
  /// No icons, no caching, no background timer: the old collector kept a threadpool job refetching every fifteen
  /// minutes for a home screen strip this front end does not have; here the feeds are read when the screen opens
  /// and when the viewer presses refresh, which is the moment the answer is wanted.
  /// </para>
  /// </remarks>
  public sealed class NewsApiClient : IDisposable
  {
    /// <summary>
    /// Where MediaPortal keeps its default feed list, the address the old news plugin read and the list is still
    /// served from.
    /// </summary>
    public const string DEFAULT_FEEDS_URL = "http://install.team-mediaportal.com/MP2/News/DefaultFeeds.xml";

    /// <summary>
    /// How long one feed gets. A news screen is read in seconds, and a feed that has not answered in fifteen is
    /// not going to make the screen worth waiting for.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;

    public NewsApiClient(HttpClient? httpClient = null)
    {
      _ownsHttpClient = httpClient == null;
      _http = httpClient ?? new HttpClient();
      if (_ownsHttpClient)
        _http.Timeout = DefaultTimeout;
      if (string.IsNullOrEmpty(_http.DefaultRequestHeaders.UserAgent.ToString()))
      {
        // Some feeds answer a bare "HttpClient" agent with a refusal; the product's own name is the one the old
        // plugin's web client was served under.
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MediaPortal 2");
      }
    }

    public void Dispose()
    {
      if (_ownsHttpClient)
        _http.Dispose();
    }

    /// <summary>
    /// Downloads MediaPortal's default feed list and keeps the region that fits the given culture.
    /// </summary>
    /// <param name="cultureName">
    /// The viewer's culture in its server spelling, <c>de-AT</c> or <c>en</c>. The pick is exact culture first,
    /// then the two-letter language, then English - the same ladder the old plugin climbed, because a viewer who
    /// set their interface to Austrian German wants the Austrian list and not the generic English one.
    /// </param>
    public async Task<IReadOnlyList<NewsFeedBookmark>> GetDefaultFeedsAsync(string cultureName,
        CancellationToken cancellation = default)
    {
      string xml = await _http.GetStringAsync(DEFAULT_FEEDS_URL, cancellation).ConfigureAwait(false);
      return PickRegion(xml, cultureName);
    }

    /// <summary>
    /// The region of the default feed list that fits a culture, out of the XML the server answers with.
    /// </summary>
    /// <remarks>
    /// Kept separate from the download so that a test can hand it the list it wrote rather than the list the
    /// foundation happens to be serving today. A list without an English region is treated as the failure it
    /// is: the ladder has to end somewhere, and inventing feeds is not somewhere.
    /// </remarks>
    public static IReadOnlyList<NewsFeedBookmark> PickRegion(string feedsXml, string cultureName)
    {
      XDocument document = XDocument.Parse(feedsXml);
      Dictionary<string, List<NewsFeedBookmark>> byRegion = new Dictionary<string, List<NewsFeedBookmark>>(
          StringComparer.OrdinalIgnoreCase);
      foreach (XElement region in document.Descendants("Region"))
      {
        string? code = region.Attribute("Code")?.Value;
        if (string.IsNullOrWhiteSpace(code))
          continue;
        List<NewsFeedBookmark> feeds = new List<NewsFeedBookmark>();
        foreach (XElement feed in region.Descendants("Feed"))
        {
          string? url = feed.Value.Trim();
          if (!string.IsNullOrEmpty(url))
            feeds.Add(new NewsFeedBookmark((feed.Attribute("Name")?.Value ?? url).Trim(), url));
        }
        byRegion[code] = feeds;
      }

      CultureInfo culture = CultureInfo.CurrentCulture;
      try
      {
        culture = CultureInfo.GetCultureInfo(cultureName);
      }
      catch (CultureNotFoundException)
      {
        // A culture the machine does not know is answered like one it has never been told about: two letters,
        // then English.
      }

      List<NewsFeedBookmark>? found;
      if (byRegion.TryGetValue(culture.Name, out found) && found.Count > 0)
        return found;
      if (byRegion.TryGetValue(culture.TwoLetterISOLanguageName, out found) && found.Count > 0)
        return found;
      if (byRegion.TryGetValue("en", out found) && found.Count > 0)
        return found;
      return new List<NewsFeedBookmark>();
    }

    /// <summary>
    /// Reads one feed. Never throws on what the feed itself does - a refusal, a timeout or an unreadable body
    /// come back as an answer naming the feed and the reason, because one dead newspaper is not a reason for
    /// the news screen to show nothing at all.
    /// </summary>
    public async Task<NewsFeedAnswer> ReadFeedAsync(NewsFeedBookmark feed, CancellationToken cancellation = default)
    {
      try
      {
        using (HttpResponseMessage response = await _http.GetAsync(feed.Url, HttpCompletionOption.ResponseHeadersRead,
            cancellation).ConfigureAwait(false))
        {
          if (!response.IsSuccessStatusCode)
            return new NewsFeedAnswer(feed, null, (int) response.StatusCode + " " + response.ReasonPhrase);

          byte[] body = await response.Content.ReadAsByteArrayAsync(cancellation).ConfigureAwait(false);
          using (MemoryStream stream = new MemoryStream(body))
          {
            XmlReaderSettings settings = new XmlReaderSettings();
            // The feeds say what they are in their own XML declaration; what they do not say is whether the
            // address they were fetched from belongs to somebody else's server, so external entities - which an
            // RSS file can name - are not resolved.
            settings.DtdProcessing = DtdProcessing.Prohibit;
            settings.XmlResolver = null;
            using (XmlReader reader = XmlReader.Create(stream, settings))
            {
              SyndicationFeed parsed = SyndicationFeed.Load(reader);
              string feedTitle = string.IsNullOrWhiteSpace(parsed.Title?.Text) ? feed.Name : parsed.Title.Text.Trim();
              List<NewsHeadline> headlines = new List<NewsHeadline>();
              foreach (SyndicationItem item in parsed.Items)
              {
                headlines.Add(new NewsHeadline(
                    PlainText(item.Title?.Text ?? ""),
                    feedTitle,
                    item.PublishDate == default(DateTimeOffset) ? null : item.PublishDate,
                    PlainText(SummaryOf(item))));
              }
              return new NewsFeedAnswer(feed, headlines, null);
            }
          }
        }
      }
      catch (OperationCanceledException)
      {
        throw;
      }
      catch (Exception ex)
      {
        return new NewsFeedAnswer(feed, null, ex.Message);
      }
    }

    /// <summary>
    /// The words under a headline: the full text when the feed carries one, the content when it has that
    /// instead, the summary when that is all there is - the same order the old reader tried, because a feed that
    /// publishes both usually means the first one to be the story.
    /// </summary>
    private static string SummaryOf(SyndicationItem item)
    {
      if (item.Content is TextSyndicationContent content && !string.IsNullOrWhiteSpace(content.Text))
        return content.Text;
      if (item.Summary != null && !string.IsNullOrWhiteSpace(item.Summary.Text))
        return item.Summary.Text;
      return "";
    }

    /// <summary>
    /// What feeds publish as text and what a screen can show: feeds bury their summaries in HTML - paragraphs,
    /// links, sometimes whole pages - and a tile that printed the tags would be a tile of angle brackets.
    /// </summary>
    private static string PlainText(string html)
    {
      if (string.IsNullOrWhiteSpace(html))
        return "";
      string text = Regex.Replace(html, "<[^>]*>", " ");
      text = System.Net.WebUtility.HtmlDecode(text);
      text = Regex.Replace(text, @"\s+", " ");
      return text.Trim();
    }
  }
}
