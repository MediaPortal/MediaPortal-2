/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
    General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Caching;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The channel pictures: which address a channel's picture is read from, which machine it is then asked of, and
  /// what a guide does with the answer.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference matched a channel to a picture out of a picture collection it kept itself, downloaded from a logo
  /// site and updated by a job on the server (<c>SlimTvInterfaces/Settings/SlimTvLogoSettings.cs</c>: a repository
  /// URL, a theme name, an auto-update switch and a job id). A TVHeadend needs none of that - it answers every
  /// channel with the address of the icon it already has - so what is tested here is the narrow version: read the
  /// path the backend gave, ask that backend for it, and draw it beside the row.
  /// </para>
  /// <para>
  /// The narrowness is the point of half of these tests. A front end that fetched the other address a TVHeadend
  /// answers (<c>icon</c>, which is often a machine somewhere else on the internet) would be reporting which
  /// channels a household watches to whoever owns that machine, on a box whose only job is to play television; so
  /// the tests below count requests and complain when a request went to a machine that was not the one written down
  /// in the settings.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTvChannelLogos
  {
    /// <summary>One round hour, so that a programme window can be anchored to something checkable.</summary>
    private static long Anchor()
    {
      long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      return now - now % (TvGuideViewModel.MINUTES_PER_COLUMN * 60L);
    }

    #region The backend, and what it carries

    /// <summary>
    /// Three channels, of which two have a picture and one does not - the ordinary state of a real bouquet, and the
    /// reason a guide has to be able to draw a row without one.
    /// </summary>
    private const string BOUQUET_JSON = "{\"total\":1,\"entries\":[{\"uuid\":\"tv\",\"name\":\"Everything\","
        + "\"services\":[\"s1\",\"s2\",\"s3\"]}]}";

    private const string CHANNELS_JSON = "{\"total\":3,\"entries\":["
        + "{\"number\":3,\"name\":\"Bild\",\"epgchannelid\":\"epg-bild\",\"streamurl\":\"//stream/channelid/3\","
        + "\"tags\":[\"tv\"],\"services\":[\"s1\"],\"icon_public_url\":\"/imagecache/1\"},"
        + "{\"number\":12,\"name\":\"Ton\",\"epgchannelid\":\"epg-ton\",\"streamurl\":\"//stream/channelid/12\","
        + "\"tags\":[\"tv\"],\"services\":[\"s2\"],\"icon_public_url\":\"imagecache/2\"},"
        + "{\"number\":44,\"name\":\"Ohne\",\"epgchannelid\":\"epg-ohne\",\"streamurl\":\"//stream/channelid/44\","
        + "\"tags\":[\"tv\"],\"services\":[\"s3\"]}]}";

    private const string SERVICES_JSON = "{\"total\":3,\"entries\":["
        + "{\"uuid\":\"s1\",\"type\":1},{\"uuid\":\"s2\",\"type\":1},{\"uuid\":\"s3\",\"type\":1}]}";

    /// <summary>
    /// A TVHeadend with three channels and two pictures. It counts the pictures it was asked for, because most of
    /// what is under test here is how often and from where a picture was asked.
    /// </summary>
    private class PictureBackend : HttpMessageHandler
    {
      /// <summary>Every address a picture was asked for, in the order they came in.</summary>
      internal readonly List<string> AskedPictures = new List<string>();

      /// <summary>The host every one of those questions went to.</summary>
      internal readonly List<string> AskedHosts = new List<string>();

      /// <summary>A status to answer every picture with, standing for a machine that has none.</summary>
      internal HttpStatusCode? RefusePictures;

      /// <summary>Set when the pictures are to be answered with something that is not a picture.</summary>
      internal bool PicturesArePages;

      /// <summary>A machine that will not answer about anything at all.</summary>
      internal bool Nowhere;

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        Uri url = request.RequestUri!;
        string path = url.AbsolutePath;

        if (Nowhere)
          throw new HttpRequestException("no route to " + url.Host);

        if (path.Contains("/imagecache/", StringComparison.Ordinal))
        {
          AskedPictures.Add(path);
          AskedHosts.Add(url.Host);

          if (RefusePictures.HasValue)
            return Answer(request, RefusePictures.Value, new StringContent(""));

          if (PicturesArePages)
          {
            return Answer(request, HttpStatusCode.OK,
                new StringContent("<html>a login page</html>", Encoding.UTF8, "text/html"));
          }

          return Answer(request, HttpStatusCode.OK, new ByteArrayContent(Picture)
          {
            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") }
          });
        }

        string body = path.EndsWith("/api/channeltag/list", StringComparison.Ordinal) ? BOUQUET_JSON
            : path.EndsWith("/api/channel/grid", StringComparison.Ordinal) ? CHANNELS_JSON
            : path.EndsWith("/api/service/grid", StringComparison.Ordinal) ? SERVICES_JSON
            : "{\"total\":0,\"entries\":[]}";

        return Answer(request, HttpStatusCode.OK, new StringContent(body, Encoding.UTF8, "application/json"));
      }

      /// <summary>Not a real picture: the headless toolkit answers every bytes with a one-pixel stub anyway.</summary>
      internal static readonly byte[] Picture = Encoding.ASCII.GetBytes("PNG\0a channel's own picture");

      private static Task<HttpResponseMessage> Answer(HttpRequestMessage request, HttpStatusCode code,
          HttpContent content)
      {
        return Task.FromResult(new HttpResponseMessage(code)
        {
          RequestMessage = request,
          Content = content
        });
      }
    }

    private static TvBackendSettings WrittenDown(bool logos = true)
    {
      TvBackendSettings settings = new TvBackendSettings();
      settings.Host = "tvbox";
      settings.Port = 9981;
      settings.ShowChannelLogos = logos;
      return settings;
    }

    private static TvHeadendApiClient Backend(PictureBackend backend, TvBackendSettings settings)
    {
      return new TvHeadendApiClient(settings, new HttpClient(backend));
    }

    private static ChannelLogos Pictures(PictureBackend backend, TvBackendSettings settings)
    {
      return new ChannelLogos(delegate { return settings; },
          delegate (TvBackendSettings s) { return Backend(backend, s); });
    }

    private static TvGuideViewModel Opened(PictureBackend backend, TvBackendSettings settings,
        Action<Action>? marshal = null, ChannelLogos? logos = null)
    {
      TvGuideViewModel guide = new TvGuideViewModel(TvMediaType.Television,
          delegate { return settings; },
          delegate (TvBackendSettings s) { return Backend(backend, s); },
          marshal, logos);
      guide.ReloadAsync().GetAwaiter().GetResult();
      return guide;
    }

    /// <summary>
    /// Waits for the pictures of the page to arrive. They are fetched on the machine's own threads - that is the
    /// whole reason a row can be drawn before its picture is there - so a test that wants to look at a finished row
    /// has to wait for them rather than pretend they came with the row.
    /// </summary>
    private static void PicturesArrived(TvGuideViewModel guide)
    {
      for (int attempt = 0; attempt < 200; attempt++)
      {
        if (guide.Rows.All(delegate (TvGuideRow row)
        {
          return !row.WantsLogo || row.Logo != null;
        }))
          return;

        Thread.Sleep(10);
      }

      Assert.Fail("the pictures did not arrive: " + string.Join(", ", guide.Rows
          .Where(delegate (TvGuideRow r)
          {
            return r.WantsLogo && r.Logo == null;
          })
          .Select(delegate (TvGuideRow r)
          {
            return r.Name;
          })));
    }

    #endregion

    #region Which address a picture is read from

    private static string? PathOf(string json)
    {
      using (JsonDocument document = JsonDocument.Parse(json))
      {
        return TvHeadendApiClient.LogoPathOf(document.RootElement);
      }
    }

    [Test]
    public void ThePictureIsReadFromThePathTheBackendGaveAndFromNowhereElse()
    {
      Assert.That(PathOf("{\"icon_public_url\":\"/imagecache/7\"}"), Is.EqualTo("/imagecache/7"));

      // A path without its leading slash, which is how some of these backends write it.
      Assert.That(PathOf("{\"icon_public_url\":\"imagecache/7\"}"), Is.EqualTo("/imagecache/7"));
    }

    [Test]
    public void AChannelTheBackendSaidNothingAboutHasNoPicture()
    {
      Assert.That(PathOf("{}"), Is.Null);
      Assert.That(PathOf("{\"icon_public_url\":\"\"}"), Is.Null);
      Assert.That(PathOf("{\"icon_public_url\":\"/\"}"), Is.Null);
    }

    /// <summary>
    /// The other field a TVHeadend answers beside <c>icon_public_url</c> is <c>icon</c>, and it is often an address
    /// on a machine that has nothing to do with this household. Nothing here is ever asked of such an address, and
    /// a path that would leave the backend is refused for the same reason.
    /// </summary>
    [Test]
    public void APictureSomewhereElseIsNotAPicture()
    {
      Assert.That(PathOf("{\"icon_public_url\":\"http://logos.example.com/ard.png\"}"), Is.Null);
      Assert.That(PathOf("{\"icon_public_url\":\"https://logos.example.com/ard.png\"}"), Is.Null);
      Assert.That(PathOf("{\"icon_public_url\":\"//logos.example.com/ard.png\"}"), Is.Null);
      Assert.That(PathOf("{\"icon_public_url\":\"../etc/passwd\"}"), Is.Null);
      Assert.That(PathOf("{\"icon_public_url\":\"C:\\\\logos\\\\ard.png\"}"), Is.Null);

      // And the field that names a machine elsewhere is not read at all.
      Assert.That(PathOf("{\"icon\":\"http://logos.example.com/ard.png\"}"), Is.Null);
    }

    [Test]
    public void AChannelIsOnlyAskedForWhenTheBackendNamedAPictureForIt()
    {
      PictureBackend backend = new PictureBackend();
      ChannelLogos pictures = Pictures(backend, WrittenDown());

      TvChannel ohne = new TvChannel(44, "Ohne", "epg-ohne", null, new List<string>(), TvMediaType.Television);
      Assert.That(pictures.Has(ohne), Is.False);
      Assert.That(pictures.PictureOfAsync(ohne).GetAwaiter().GetResult(), Is.Null);

      // The whole point: a guide full of channels without icons asks for no pictures, so a household whose backend
      // carries none is not kept fetching nothing on every redraw of every guide.
      Assert.That(backend.AskedPictures.Count, Is.EqualTo(0));
    }

    [Test]
    public void APictureIsFetchedOnceAndThenRemembered()
    {
      PictureBackend backend = new PictureBackend();
      ChannelLogos pictures = Pictures(backend, WrittenDown());
      TvChannel bild = new TvChannel(3, "Bild", "epg-bild", null, new List<string>(), TvMediaType.Television,
          "/imagecache/1");

      byte[]? first = pictures.PictureOfAsync(bild).GetAwaiter().GetResult();
      byte[]? second = pictures.PictureOfAsync(bild).GetAwaiter().GetResult();

      Assert.That(first, Is.Not.Null);
      Assert.That(second, Is.Not.Null);
      Assert.That(backend.AskedPictures.Count, Is.EqualTo(1),
          "the second look at the same channel's picture is answered out of memory");
      Assert.That(backend.AskedPictures[0], Is.EqualTo("/imagecache/1"));

      byte[]? waiting;
      Assert.That(pictures.TryPeek(bild, out waiting), Is.True);
      Assert.That(waiting, Is.SameAs(first), "and a screen redrawing the row it drew a moment ago asks for nothing");
    }

    /// <summary>
    /// Two channels behind one picture - which is what a bouquet of regional stations with a shared icon is - share
    /// the one fetch, because what is remembered is the picture rather than the channel.
    /// </summary>
    [Test]
    public void TwoChannelsBehindOnePictureAskForItOnce()
    {
      PictureBackend backend = new PictureBackend();
      ChannelLogos pictures = Pictures(backend, WrittenDown());
      TvChannel one = new TvChannel(1, "Nord", "epg-nord", null, new List<string>(), TvMediaType.Television,
          "/imagecache/1");
      TvChannel other = new TvChannel(2, "Sued", "epg-sued", null, new List<string>(), TvMediaType.Television,
          "/imagecache/1");

      Assert.That(pictures.KeyOf(one), Is.EqualTo(pictures.KeyOf(other)));
      pictures.PictureOfAsync(one).GetAwaiter().GetResult();
      pictures.PictureOfAsync(other).GetAwaiter().GetResult();

      Assert.That(backend.AskedPictures.Count, Is.EqualTo(1));
    }

    [Test]
    public void APictureIsOnlyEverAskedOfTheMachineThatWasWrittenDown()
    {
      PictureBackend backend = new PictureBackend();
      ChannelLogos pictures = Pictures(backend, WrittenDown());
      TvChannel bild = new TvChannel(3, "Bild", "epg-bild", null, new List<string>(), TvMediaType.Television,
          "/imagecache/1");

      pictures.PictureOfAsync(bild).GetAwaiter().GetResult();

      Assert.That(backend.AskedHosts, Has.All.EqualTo("tvbox"),
          "a television's pictures are asked for at the television, and at nowhere else");
    }

    [Test]
    public void ABackendWithoutPicturesIsAnAnswerRatherThanAComplaint()
    {
      PictureBackend backend = new PictureBackend();
      backend.RefusePictures = HttpStatusCode.NotFound;
      ChannelLogos pictures = Pictures(backend, WrittenDown());
      TvChannel bild = new TvChannel(3, "Bild", "epg-bild", null, new List<string>(), TvMediaType.Television,
          "/imagecache/1");

      Assert.That(pictures.PictureOfAsync(bild).GetAwaiter().GetResult(), Is.Null,
          "a channel the machine has no picture for is a channel without a picture, said quietly");
    }

    /// <summary>
    /// A machine asked for a picture sometimes answers with a page instead - a login form, a note saying the viewer
    /// may not look - and a page is not a picture however it was addressed.
    /// </summary>
    [Test]
    public void APageIsNotAPicture()
    {
      PictureBackend backend = new PictureBackend();
      backend.PicturesArePages = true;
      ChannelLogos pictures = Pictures(backend, WrittenDown());
      TvChannel bild = new TvChannel(3, "Bild", "epg-bild", null, new List<string>(), TvMediaType.Television,
          "/imagecache/1");

      Assert.That(pictures.PictureOfAsync(bild).GetAwaiter().GetResult(), Is.Null);
    }

    /// <summary>
    /// The one case a picture is allowed to complain about: a machine that is not there at all. Filing that as "this
    /// channel has no picture" would leave a guide without icons until the front end was restarted, so it travels.
    /// </summary>
    [Test]
    public void AMachineThatIsNotThereSaysSoRatherThanHavingNoPictures()
    {
      PictureBackend backend = new PictureBackend();
      backend.Nowhere = true;
      ChannelLogos pictures = Pictures(backend, WrittenDown());
      TvChannel bild = new TvChannel(3, "Bild", "epg-bild", null, new List<string>(), TvMediaType.Television,
          "/imagecache/1");

      Assert.That(async delegate
      {
        await pictures.PictureOfAsync(bild);
      }, Throws.TypeOf<TvBackendException>());
    }

    #endregion

    #region What a guide does with the pictures

    [Test]
    public void TheGuideAsksForThePicturesOfTheChannelsItShows()
    {
      PictureBackend backend = new PictureBackend();
      TvBackendSettings settings = WrittenDown();
      ChannelLogos pictures = Pictures(backend, settings);

      using (TvGuideViewModel guide = Opened(backend, settings, null, pictures))
      {
        Assert.That(guide.ShowChannelLogos, Is.True);
        Assert.That(guide.Rows.Select(delegate (TvGuideRow r)
        {
          return r.WantsLogo;
        }), Is.EqualTo(new[] { true, true, false }),
            "two channels the machine has pictures for and one it has none for");

        PicturesArrived(guide);

        Assert.That(guide.Rows[0].Logo, Is.Not.Null);
        Assert.That(guide.Rows[1].Logo, Is.Not.Null);
        Assert.That(guide.Rows[2].Logo, Is.Null);
        Assert.That(backend.AskedPictures.Count, Is.EqualTo(2),
            "asked once each, and nothing at all for the channel without one");
      }
    }

    [Test]
    public void TheGuideAsksForNoPicturesWhenTheViewerSwitchedThemOff()
    {
      PictureBackend backend = new PictureBackend();
      TvBackendSettings settings = WrittenDown(false);
      ChannelLogos pictures = Pictures(backend, settings);

      using (TvGuideViewModel guide = Opened(backend, settings, null, pictures))
      {
        Assert.That(guide.ShowChannelLogos, Is.False);
        Assert.That(guide.Rows.All(delegate (TvGuideRow r)
        {
          return !r.WantsLogo;
        }), Is.True);
        Assert.That(backend.AskedPictures.Count, Is.EqualTo(0));
      }
    }

    /// <summary>
    /// A guide that was never handed anywhere to get pictures from says so, so that a screen does not leave a column
    /// standing empty for pictures that were never going to come - which is what every test screen and every guide
    /// without a picture shelf is.
    /// </summary>
    [Test]
    public void AGuideWithoutPicturesToAskForDoesNotPretendToHaveThem()
    {
      PictureBackend backend = new PictureBackend();
      TvBackendSettings settings = WrittenDown();

      using (TvGuideViewModel guide = Opened(backend, settings))
      {
        Assert.That(guide.ShowChannelLogos, Is.False);
        Assert.That(guide.Rows.All(delegate (TvGuideRow r)
        {
          return !r.WantsLogo;
        }), Is.True);
      }
    }

    [Test]
    public void ARowSaysWhatItsPictureIsRatherThanShowingOne()
    {
      PictureBackend backend = new PictureBackend();
      TvBackendSettings settings = WrittenDown();

      using (TvGuideViewModel guide = Opened(backend, settings, null, Pictures(backend, settings)))
      {
        PicturesArrived(guide);

        // Bytes, never a picture: a row holding a bitmap could not be built by a test without a window, and the
        // same bytes are what the screen's own converter turns into a picture at whatever size it needs.
        Assert.That(guide.Rows[0].Logo, Is.EqualTo(PictureBackend.Picture));
      }
    }

    #endregion

    #region On the screen

    private static Window Shown(TvGuideViewModel guide)
    {
      Window window = new Window
      {
        Content = new TvGuideView(guide),
        Width = 1280,
        Height = 720
      };
      window.Show();
      Pump();
      return window;
    }

    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
      Dispatcher.UIThread.RunJobs();
    }

    /// <summary>How wide the column of channels is on the screen, read off the hour ruler that shares it.</summary>
    private static double HeaderWidthOf(Window window)
    {
      Grid ruler = window.GetVisualDescendants().OfType<Grid>()
          .First(g => g.ColumnDefinitions.Count == TvGuideViewModel.COLUMNS + 1);
      return ruler.ColumnDefinitions[0].Width.Value;
    }

    private static List<Image> PicturesOn(Window window)
    {
      return window.GetVisualDescendants().OfType<Image>().ToList();
    }

    /// <summary>
    /// The pictures arrive after their rows were laid out, which is the whole arrangement: the grid is on the screen
    /// as soon as the evening is, and a row fills its own picture in when the picture turns up, rather than the grid
    /// redrawing itself under the viewer's hand every time one arrives.
    /// </summary>
    [Test]
    [AvaloniaTest]
    public void TheScreenDrawsThePictureBesideTheRowItBelongsTo()
    {
      PictureBackend backend = new PictureBackend();
      TvBackendSettings settings = WrittenDown();
      TvGuideViewModel guide = Opened(backend, settings,
          delegate (Action action)
          {
            Dispatcher.UIThread.Post(action);
          },
          Pictures(backend, settings));

      Window window = Shown(guide);

      // Nine hundred pixels for the two channels the machine has pictures of, and none for the third: a column is
      // only reserved where a picture is going to be drawn in it.
      Assert.That(HeaderWidthOf(window), Is.EqualTo(90 + 160 + 200).Within(1),
          "the number, the picture and the name, as the reference measured its header");
      Assert.That(PicturesOn(window).Count, Is.EqualTo(2));

      for (int attempt = 0; attempt < 200; attempt++)
      {
        Pump();
        if (PicturesOn(window).All(p => p.Source != null))
          break;
      }

      Assert.That(PicturesOn(window).Count(p => p.Source != null), Is.EqualTo(2),
          "the row that was waiting for a picture has it, without the grid having been built again");
    }

    [Test]
    [AvaloniaTest]
    public void TheScreenLeavesThePicturesOutWhenNobodyAskedForThem()
    {
      PictureBackend backend = new PictureBackend();
      TvBackendSettings settings = WrittenDown(false);
      TvGuideViewModel guide = Opened(backend, settings,
          delegate (Action action)
          {
            Dispatcher.UIThread.Post(action);
          },
          Pictures(backend, settings));

      Window window = Shown(guide);

      Assert.That(PicturesOn(window).Count, Is.EqualTo(0));
      Assert.That(HeaderWidthOf(window), Is.EqualTo(90 + 200).Within(1),
          "the width the pictures would have taken is given to the names instead");
      Assert.That(backend.AskedPictures.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// A page whose channels nobody has pictures for keeps the width it has for names. This is where this front end
    /// and the reference part company deliberately: the reference reserved its hundred and sixty pixels whether the
    /// channels had icons or not, so the commonest real case - a TVHeadend that carries no icons at all - was a
    /// column of nothing beside every row of every guide.
    /// </summary>
    [Test]
    [AvaloniaTest]
    public void APageWithoutPicturesKeepsItsNamesWide()
    {
      PictureBackend backend = new PictureBackend();
      TvBackendSettings settings = WrittenDown();
      settings.VisibleRows = 1;
      TvGuideViewModel guide = Opened(backend, settings,
          delegate (Action action)
          {
            Dispatcher.UIThread.Post(action);
          },
          Pictures(backend, settings));

      Window window = Shown(guide);
      Assert.That(guide.Rows[0].Name, Is.EqualTo("Bild"));
      Assert.That(HeaderWidthOf(window), Is.EqualTo(90 + 160 + 200).Within(1));

      // Walked down to the channel the machine has no picture of, the column goes away and the name gets the room.
      guide.MoveRowDown();
      guide.MoveRowDown();
      Pump();

      Assert.That(guide.Rows.Count, Is.EqualTo(1));
      Assert.That(guide.Rows[0].WantsLogo, Is.False);
      Assert.That(HeaderWidthOf(window), Is.EqualTo(90 + 200).Within(1));
    }

    /// <summary>
    /// A row whose station names no picture is not a hole: the channel's name stands in the picture's place and the
    /// place beside it stays empty, which is what the reference's image and its <c>ChannelLogoFallbackLabel</c>
    /// together did (<c>Skin/Nereus/themes/default/styles/SlimTvStyles.xaml:1432-1478</c>). A page of three channels
    /// of which two have icons therefore reads as two pictures with a name beside each and one name where the third
    /// picture would have been.
    /// </summary>
    [Test]
    [AvaloniaTest]
    public void ARowWithoutAPicturePutsItsNameWhereThePictureWouldStand()
    {
      PictureBackend backend = new PictureBackend();
      TvBackendSettings settings = WrittenDown();
      settings.VisibleRows = 3;
      TvGuideViewModel guide = Opened(backend, settings,
          delegate (Action action)
          {
            Dispatcher.UIThread.Post(action);
          },
          Pictures(backend, settings));

      Window window = Shown(guide);
      for (int attempt = 0; attempt < 200; attempt++)
      {
        Pump();
        if (PicturesOn(window).Count(p => p.Source != null) == 2)
          break;
      }

      // Every name of the page is drawn exactly once. For the two rows with a picture that is the place beside it,
      // and for the third it is the picture's own place - a name drawn twice would be a name the viewer could read
      // twice, and the reference's two labels were bound to be each other's opposite for exactly that reason.
      TvGuideRow without = guide.Rows.First(r => !r.WantsLogo);
      int drawn = window.GetVisualDescendants().OfType<TextBlock>()
          .Count(t => t.IsVisible && t.Text == without.Name);
      Assert.That(drawn, Is.EqualTo(1),
          "the channel without a picture says its name once, in the picture's place");

      // The rows that have a picture still show it, and still show their name beside it.
      Assert.That(PicturesOn(window).Count(p => p.IsVisible && p.Source != null), Is.EqualTo(2));
      TvGuideRow with = guide.Rows.First(r => r.WantsLogo);
      Assert.That(window.GetVisualDescendants().OfType<TextBlock>()
          .Count(t => t.IsVisible && t.Text == with.Name), Is.EqualTo(1));
    }

    #endregion
  }
}
