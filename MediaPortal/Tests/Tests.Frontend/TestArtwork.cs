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
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using FrontendAspect = MediaPortal.Frontend.Api.MediaAspect;

namespace Tests.Frontend
{
  /// <summary>
  /// The pictures of titles: who is asked for them, how often, and what happens when a server has none.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The rules being pinned down here are the ones that decide whether a shelf of two thousand films is usable:
  /// one request per title however many times it is looked at, a title without a picture remembered as having
  /// none, and only a few requests on the wire at a moment. None of that is visible in a screenshot - a shelf
  /// looks the same whether it asked the server once or sixty times - so it is visible here or nowhere.
  /// </para>
  /// <para>
  /// These tests do not need a window, so they do not ask for one: they run on an ordinary thread, where an
  /// awaited continuation is served by the thread pool rather than by a dispatcher that would have to be pumped.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestArtwork
  {
    private static readonly Guid FIRST = new Guid("00000001-0001-0001-0001-000000000001");
    private static readonly Guid SECOND = new Guid("00000002-0002-0002-0002-000000000002");

    private static byte[] Picture(string which)
    {
      return Encoding.ASCII.GetBytes("PNG\0" + which);
    }

    /// <summary>
    /// A source that hands out pictures and counts what it was asked for.
    /// </summary>
    /// <remarks>
    /// Thread-safe on purpose: <see cref="Artwork"/> is allowed to answer from whichever thread the answer came
    /// back on, so a test that starts a shelf's worth of reads is a test where this object is called from several
    /// threads at once. A fake that corrupts its own counts would turn a passing run into luck.
    /// </remarks>
    private sealed class Source
    {
      private readonly object _gate = new object();

      private readonly Dictionary<Guid, byte[]> _pictures = new Dictionary<Guid, byte[]>();

      /// <summary>Titles whose answer is being held back by the test.</summary>
      private readonly Dictionary<Guid, TaskCompletionSource<byte[]?>> _held =
          new Dictionary<Guid, TaskCompletionSource<byte[]?>>();

      internal List<Guid> Asked { get; } = new List<Guid>();

      internal int InFlight { get; private set; }

      internal int MostInFlight { get; private set; }

      /// <summary>The titles that fail rather than answer.</summary>
      internal HashSet<Guid> Failing { get; } = new HashSet<Guid>();

      internal void Has(Guid id, byte[] picture)
      {
        _pictures[id] = picture;
      }

      /// <summary>
      /// Holds the answer for one title until the test says <see cref="Release"/>.
      /// </summary>
      internal void Hold(Guid id)
      {
        _held[id] = new TaskCompletionSource<byte[]?>();
      }

      internal void Release(Guid id)
      {
        _held[id].SetResult(_pictures.TryGetValue(id, out byte[]? picture) ? picture : null);
      }

      internal void ReleaseEveryHeld()
      {
        foreach (Guid id in _held.Keys.ToList())
          Release(id);
      }

      internal async Task<byte[]?> FetchAsync(Guid id)
      {
        TaskCompletionSource<byte[]?>? gate;
        bool failing;
        lock (_gate)
        {
          Asked.Add(id);
          InFlight++;
          MostInFlight = Math.Max(MostInFlight, InFlight);
          _held.TryGetValue(id, out gate);
          failing = Failing.Contains(id);
        }

        try
        {
          if (gate != null)
            await gate.Task;
          else if (failing)
            throw new ServerApiException(new Uri("http://127.0.0.1:1/picture"), "the server went away");

          return _pictures.TryGetValue(id, out byte[]? picture) ? picture : null;
        }
        finally
        {
          lock (_gate)
          {
            InFlight--;
          }
        }
      }

      internal int TimesAskedFor(Guid id)
      {
        lock (_gate)
        {
          return Asked.Count(asked => asked == id);
        }
      }
    }

    [Test]
    public async Task APictureIsAskedForOnceHoweverOftenItIsLookedAt()
    {
      Source source = new Source();
      source.Has(FIRST, Picture("a"));
      Artwork artwork = new Artwork(source.FetchAsync);

      byte[]? first = await artwork.LoadAsync(FIRST);
      byte[]? again = await artwork.LoadAsync(FIRST);

      Assert.That(first, Is.EqualTo(Picture("a")));
      Assert.That(again, Is.EqualTo(first), "the row that was drawn once is drawn again from what is held");
      Assert.That(source.TimesAskedFor(FIRST), Is.EqualTo(1),
          "a shelf scrolled back and forth must not read the server again");
    }

    [Test]
    public async Task ATitleWithoutAPictureIsRememberedAsHavingNone()
    {
      Source source = new Source();
      Artwork artwork = new Artwork(source.FetchAsync);

      Assert.That(await artwork.LoadAsync(SECOND), Is.Null);
      Assert.That(await artwork.LoadAsync(SECOND), Is.Null);

      Assert.That(source.TimesAskedFor(SECOND), Is.EqualTo(1),
          "most titles in a real library have no poster, and asking again on every look is the expensive way to "
          + "find that out twice");
      Assert.That(artwork.Remembered, Is.EqualTo(1), "an answered absence is an answer");
    }

    [Test]
    public async Task TwoQuestionsAboutOneTitleAreOneRequest()
    {
      Source source = new Source();
      source.Has(FIRST, Picture("a"));
      source.Hold(FIRST);
      Artwork artwork = new Artwork(source.FetchAsync);

      Task<byte[]?> first = artwork.LoadAsync(FIRST);
      Task<byte[]?> second = artwork.LoadAsync(FIRST);

      source.Release(FIRST);

      Assert.That(await first, Is.EqualTo(Picture("a")));
      Assert.That(await second, Is.EqualTo(Picture("a")));
      Assert.That(source.TimesAskedFor(FIRST), Is.EqualTo(1),
          "a row that is realised and selected at once is one picture, not two");
    }

    [Test]
    public async Task APictureThatCouldNotBeReadIsAskedForAgainNextTime()
    {
      Source source = new Source();
      source.Failing.Add(FIRST);
      Artwork artwork = new Artwork(source.FetchAsync);

      Assert.That(async delegate
      {
        await artwork.LoadAsync(FIRST);
      }, Throws.Exception.TypeOf<ServerApiException>());

      source.Failing.Remove(FIRST);
      source.Has(FIRST, Picture("a"));

      Assert.That(await artwork.LoadAsync(FIRST), Is.EqualTo(Picture("a")),
          "a server that was quiet once is not declared to have no picture");
      Assert.That(source.TimesAskedFor(FIRST), Is.EqualTo(2));
      Assert.That(artwork.Remembered, Is.EqualTo(1));
    }

    [Test]
    public async Task OnlyALittleIsAskedForAtOneMoment()
    {
      Source source = new Source();
      Guid[] ids = Enumerable.Range(0, 30).Select(i => new Guid(i + 1, 0, 0, new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 }))
          .ToArray();
      foreach (Guid id in ids)
      {
        source.Has(id, Picture("x"));
        source.Hold(id);
      }

      Artwork artwork = new Artwork(source.FetchAsync);
      List<Task<byte[]?>> asking = ids.Select(artwork.LoadAsync).ToList();

      Assert.That(artwork.Running, Is.EqualTo(Artwork.MAX_AT_ONCE),
          "a shelf of thirty rows is thirty requests queued, not thirty requests on the wire");

      source.ReleaseEveryHeld();
      await Task.WhenAll(asking);

      Assert.That(source.MostInFlight, Is.LessThanOrEqualTo(Artwork.MAX_AT_ONCE));
      Assert.That(artwork.Running, Is.EqualTo(0));
      Assert.That(asking.All(answer => answer.Result != null), Is.True, "every row got its picture eventually");
    }

    /// <summary>
    /// Twenty-four titles, each asked for twice, by twenty-four threads that are released together.
    /// </summary>
    /// <remarks>
    /// This is the shape the pictures of a shelf actually arrive in: the reads go out from the screen's thread and
    /// the answers come back on whatever the thread pool had free, so the tables behind <see cref="Artwork"/> are
    /// written from several threads at once. An early version of that cache was written for one thread only and
    /// answered this test by corrupting its own dictionary - which is the kind of failure that shows up on a
    /// television after twenty minutes of browsing rather than in a review pass, so it is asked for here on
    /// purpose and repeatedly.
    /// </remarks>
    [Test]
    public async Task PicturesAskedForFromSeveralThreadsAtOnceAreFetchedOnceEach()
    {
      const int TITLES = 24;

      Source source = new Source();
      Guid[] ids = Enumerable.Range(0, TITLES)
          .Select(i => new Guid(i + 1, 0, 0, new byte[] { 2, 2, 2, 2, 2, 2, 2, 2 }))
          .ToArray();
      foreach (Guid id in ids)
        source.Has(id, Picture(id.ToString("N")));

      Artwork artwork = new Artwork(source.FetchAsync);

      using ManualResetEventSlim go = new ManualResetEventSlim(false);
      List<Task<byte[]?>> asking = new List<Task<byte[]?>>();
      foreach (Guid id in ids.Concat(ids))
      {
        asking.Add(Task.Run(async delegate
        {
          go.Wait();
          return await artwork.LoadAsync(id);
        }));
      }

      go.Set();
      byte[]?[] answers = await Task.WhenAll(asking);

      Assert.That(answers.All(answer => answer != null), Is.True, "every question got an answer");
      Assert.That(artwork.Remembered, Is.EqualTo(TITLES), "twenty-four titles are twenty-four answers, not forty-eight");
      foreach (Guid id in ids)
      {
        Assert.That(source.TimesAskedFor(id), Is.EqualTo(1),
            "two threads asking about one title at once is one request to the server");
      }

      Assert.That(artwork.Running, Is.EqualTo(0));
    }

    [Test]
    public async Task TheOldestPicturesGoFirstWhenTheMemoryIsFull()
    {
      Source source = new Source();
      for (int i = 0; i < Artwork.MAX_REMEMBERED + 1; i++)
        source.Has(IdOf(i), Picture(i.ToString(System.Globalization.CultureInfo.InvariantCulture)));

      Artwork artwork = new Artwork(source.FetchAsync);
      for (int i = 0; i < Artwork.MAX_REMEMBERED + 1; i++)
        await artwork.LoadAsync(IdOf(i));

      Assert.That(artwork.Remembered, Is.EqualTo(Artwork.MAX_REMEMBERED),
          "a media centre runs for weeks at a time, so the pictures it remembers have to stop growing somewhere");
      Assert.That(source.TimesAskedFor(IdOf(0)), Is.EqualTo(1));

      await artwork.LoadAsync(IdOf(0));
      Assert.That(source.TimesAskedFor(IdOf(0)), Is.EqualTo(2),
          "the title that was dropped is asked for again, which is what bounded memory costs and why it is "
          + "bounded at three or four shelves rather than at one");
      Assert.That(artwork.Remembered, Is.EqualTo(Artwork.MAX_REMEMBERED));

      static Guid IdOf(int index)
      {
        return new Guid(index + 1, 0, 0, new byte[] { 2, 2, 2, 2, 2, 2, 2, 2 });
      }
    }

    [Test]
    public async Task APictureAlreadyHeldIsAnsweredWithoutAsking()
    {
      Source source = new Source();
      source.Has(FIRST, Picture("a"));
      Artwork artwork = new Artwork(source.FetchAsync);
      await artwork.LoadAsync(FIRST);

      bool known = artwork.TryPeek(FIRST, out byte[]? held);
      bool unknown = artwork.TryPeek(SECOND, out byte[]? nothing);

      Assert.That(known, Is.True);
      Assert.That(held, Is.EqualTo(Picture("a")));
      Assert.That(unknown, Is.False, "a title nobody asked about is not known to have no picture");
      Assert.That(nothing, Is.Null);
    }
  }

  /// <summary>
  /// The pictures a shelf shows: which row carries which picture, and what a server without artwork does to a list.
  /// </summary>
  [TestFixture]
  public class TestBrowserArtwork
  {
    private static readonly Uri SERVER_URL = new Uri("http://127.0.0.1:1/MediaPortal/Server/");
    private static readonly Guid WITH_A_POSTER = new Guid("00000001-0001-0001-0001-000000000001");
    private static readonly Guid WITH_NOTHING = new Guid("00000002-0002-0002-0002-000000000002");
    private static readonly Guid WITH_A_FRAME = new Guid("00000003-0003-0003-0003-000000000003");

    private static byte[] Poster
    {
      get { return Encoding.ASCII.GetBytes("PNG\0poster"); }
    }

    private static byte[] Frame
    {
      get { return Encoding.ASCII.GetBytes("PNG\0frame"); }
    }

    private static HomeSection Shelf(string aspectName)
    {
      return new HomeSection("Movies",
          new AspectViewModel(new FrontendAspect(Guid.Empty, aspectName, false, false, null,
              Array.Empty<AspectAttribute>())));
    }

    /// <summary>
    /// A server with three titles, of which two have a picture and one does not - the ordinary state of a library.
    /// </summary>
    private class PictureServer : HttpMessageHandler
    {
      /// <summary>The two titles that have art, and what the server hands out for them.</summary>
      private readonly Dictionary<Guid, byte[]> _pictures = new Dictionary<Guid, byte[]>
      {
          { WITH_A_POSTER, Poster },
          { WITH_A_FRAME, Frame }
      };

      private readonly Dictionary<Guid, int> _asks = new Dictionary<Guid, int>();

      /// <summary>Set when the server should not answer about pictures at all.</summary>
      internal bool NoPicturesAtAll { get; set; }

      internal int AskedAbout(Guid id)
      {
        return _asks.TryGetValue(id, out int times) ? times : 0;
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;

        if (path.EndsWith("/thumbnail", StringComparison.Ordinal))
        {
          // The id the client put in the address, which is the thirty-six characters before the last slash.
          Guid id = Guid.Parse(path.Substring(path.LastIndexOf('/') - 36, 36));
          _asks[id] = AskedAbout(id) + 1;

          if (!NoPicturesAtAll && _pictures.TryGetValue(id, out byte[]? picture))
          {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
              RequestMessage = request,
              Content = new ByteArrayContent(picture)
            });
          }

          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
        }

        StringBuilder body = new StringBuilder(
            "{\"total\":3,\"offset\":0,\"limit\":60,\"returned\":3,\"sort\":\"title\",\"titles\":[");
        body.Append(Row(WITH_A_POSTER, "Alien Earth"));
        body.Append(',').Append(Row(WITH_NOTHING, "A title with no art"));
        body.Append(',').Append(Row(WITH_A_FRAME, "Nimmerland"));
        body.Append("]}");

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
        });
      }

      private static string Row(Guid id, string title)
      {
        return "{\"id\":\"" + id.ToString("D") + "\",\"title\":\"" + title + "\",\"year\":2025}";
      }
    }

    /// <summary>
    /// Says what a screen says when it lays rows out: that they are on screen, and may have their picture.
    /// </summary>
    /// <remarks>
    /// A test holds a view model and no television, so nobody lays anything out. Naming the rows here is the
    /// honest way of playing the screen's part - the shelf asks no row for a picture on its own, which is what
    /// keeps a library of a thousand titles from fetching a thousand posters nobody looks at.
    /// </remarks>
    public static void DrawRows(MediaBrowserViewModel shelf, params TitleViewModel[] rows)
    {
      foreach (TitleViewModel row in rows)
        shelf.PaintRow(row);
    }

    private static void Pump()
    {
      Avalonia.Threading.Dispatcher.UIThread.RunJobs();
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

    [AvaloniaTest]
    public void EveryRowCarriesItsOwnPicture()
    {
      PictureServer server = new PictureServer();
      using (var client = new ServerApiClient(SERVER_URL, new HttpClient(server)))
      {
        MediaBrowserViewModel films = new MediaBrowserViewModel(client, Shelf("MovieItem"));
        films.OpenCommand.Execute(null);
        // The three rows the television would be showing. The shelf itself asks no row for a picture.
        DrawRows(films, films.Titles[0], films.Titles[1], films.Titles[2]);
        WaitUntil("every picture the server has to arrive",
            delegate { return films.Titles[0].Artwork != null && films.Titles[2].Artwork != null; });

        Assert.That(films.Titles[0].Artwork, Is.EqualTo(Poster));
        Assert.That(films.Titles[1].Artwork, Is.Null,
            "the server said it has no picture for this title, and the row shows the box behind the image");
        Assert.That(films.Titles[2].Artwork, Is.EqualTo(Frame),
            "the third row carries the third title's picture, not the first one that arrived");
      }
    }

    [AvaloniaTest]
    public void TheDetailsPictureIsTheTitlesOwnAndNotTheShelves()
    {
      PictureServer server = new PictureServer();
      using (var client = new ServerApiClient(SERVER_URL, new HttpClient(server)))
      {
        MediaBrowserViewModel films = new MediaBrowserViewModel(client, Shelf("MovieItem"));
        films.OpenCommand.Execute(null);
        WaitUntil("the rows to arrive", delegate { return films.Titles.Count == 3; });

        films.Selected = films.Titles[2];
        WaitUntil("the details picture of the selected title", delegate { return films.DetailsArtwork != null; });

        Assert.That(films.DetailsArtwork, Is.EqualTo(Frame),
            "a poster above the wrong plot is worse than no poster at all");
      }
    }

    [AvaloniaTest]
    public void ATitleOnTheScreenIsAskedForItsPictureOnce()
    {
      PictureServer server = new PictureServer();
      using (var client = new ServerApiClient(SERVER_URL, new HttpClient(server)))
      {
        MediaBrowserViewModel films = new MediaBrowserViewModel(client, Shelf("MovieItem"));
        films.OpenCommand.Execute(null);
        WaitUntil("the rows to arrive", delegate { return films.Titles.Count == 3; });

        films.Selected = films.Titles[0];
        WaitUntil("the details to be read", delegate { return films.DetailsVisible; });
        films.ToggleDetailsCommand.Execute(null);
        films.ToggleDetailsCommand.Execute(null);
        Pump();

        Assert.That(server.AskedAbout(WITH_A_POSTER), Is.EqualTo(1),
            "the shelf asked for it and the details panel joined that question rather than asking its own");
      }
    }

    [AvaloniaTest]
    public void AServerWithNoPicturesAtAllStillShowsItsTitles()
    {
      PictureServer server = new PictureServer { NoPicturesAtAll = true };
      using (var client = new ServerApiClient(SERVER_URL, new HttpClient(server)))
      {
        MediaBrowserViewModel films = new MediaBrowserViewModel(client, Shelf("MovieItem"));
        films.OpenCommand.Execute(null);
        WaitUntil("the list to be read", delegate { return films.Titles.Count == 3; });
        // Drawn, so that the rows really do ask - and really do get told there is no picture.
        DrawRows(films, films.Titles[0], films.Titles[1], films.Titles[2]);
        Pump();

        Assert.That(films.Titles.All(row => row.Artwork == null), Is.True);
        Assert.That(films.Status, Is.EqualTo("3 of 3 movies, sorted by title"),
            "a picture the server does not have is not a complaint about the list");
        Assert.That(films.IsBusy, Is.False);
      }
    }
  }

  /// <summary>
  /// Turning the bytes a server sent into a picture a screen can draw, and what is remembered while that happens.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>What these tests can and cannot show.</b> The test toolkit draws headless, and the headless build answers
  /// every array of bytes handed to it with a one-pixel picture - measured here, not assumed: a real PNG, the words
  /// "this used to be a poster" and an empty array all three came back as 1x1. So this fixture cannot prove that a
  /// picture was decoded (nothing fails to decode here) and it cannot prove that a corrupt file is survived. What
  /// it does prove is the converter's own decisions - that bytes become a picture object, that the same bytes are
  /// decoded once rather than once per layout, and that nothing is handed out for no bytes. Whether Skia turned
  /// the server's poster into the pixels on the shelf, and whether a corrupt file leaves the shelf standing, is
  /// what the render gate looks at on a platform with a real decoder, where it can measure the colour of the
  /// square the row's picture was drawn into.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestArtworkConverter
  {
    /// <summary>
    /// A PNG one pixel across, written out rather than generated: the test is about what the converter does with
    /// bytes, and a generator would need the very toolkit under test to make them.
    /// </summary>
    private const string ONE_PIXEL_PNG =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==";

    private static byte[] Picture()
    {
      return Convert.FromBase64String(ONE_PIXEL_PNG);
    }

    [AvaloniaTest]
    public void TheBytesOfAPictureBecomeAPicture()
    {
      ArtworkConverter converter = new ArtworkConverter();

      object? picture = converter.Convert(Picture(), typeof(Bitmap), null!, System.Globalization.CultureInfo.InvariantCulture);

      Assert.That(picture, Is.InstanceOf<Bitmap>(), "the screen is handed a picture, not the bytes it came in");
      Assert.That(converter.Held, Is.EqualTo(1));
    }

    [AvaloniaTest]
    public void NoBytesIsNoPicture()
    {
      ArtworkConverter converter = new ArtworkConverter();

      Assert.That(converter.Convert(null, typeof(Bitmap), null!, System.Globalization.CultureInfo.InvariantCulture),
          Is.Null, "a title the server has no picture for gets the placeholder, not an empty image");
      Assert.That(converter.Convert(new byte[0], typeof(Bitmap), null!, System.Globalization.CultureInfo.InvariantCulture),
          Is.Null);
      Assert.That(converter.Held, Is.EqualTo(0), "nothing was decoded, so nothing is being remembered");
    }

    [AvaloniaTest]
    public void TheSameBytesAreDecodedOnce()
    {
      ArtworkConverter converter = new ArtworkConverter();
      byte[] bytes = Picture();

      object? first = converter.Convert(bytes, typeof(Bitmap), null!, System.Globalization.CultureInfo.InvariantCulture);
      object? second = converter.Convert(bytes, typeof(Bitmap), null!, System.Globalization.CultureInfo.InvariantCulture);

      Assert.That(second, Is.SameAs(first),
          "a row that is laid out twice must not decode its poster twice, or scrolling is a slideshow");
      Assert.That(converter.Held, Is.EqualTo(1));
    }

    /// <summary>
    /// How wide a tile of the pictures screen is, in logical pixels. The picture inside it is drawn two hundred and
    /// six of those across; the tile's own width is the number a reader can check in the markup.
    /// </summary>
    private const int TILE_LOGICAL_WIDTH = 216;

    [AvaloniaTest]
    public void ATemplateThatSaysTileGetsATileSizedDecode()
    {
      Assert.That(ArtworkConverter.DecodeWidthFor(ArtworkConverter.TILE_PARAMETER),
          Is.EqualTo(ArtworkConverter.TILE_DECODE_WIDTH),
          "the pictures screen asks for its pictures by that word, so the word has to mean a size");
    }

    [Test]
    public void TheWordForTileIsUnderstoodHoweverItIsCapitalised()
    {
      // XAML is written by people who capitalise, and a parameter that silently means "the whole picture" would
      // undo the whole point of asking.
      Assert.That(ArtworkConverter.DecodeWidthFor("TILE"), Is.EqualTo(ArtworkConverter.TILE_DECODE_WIDTH));
      Assert.That(ArtworkConverter.DecodeWidthFor("Tile"), Is.EqualTo(ArtworkConverter.TILE_DECODE_WIDTH));
    }

    [Test]
    public void ATileSizedDecodeIsWideEnoughForATelevisionAndNarrowEnoughToMatter()
    {
      // Lower bound: a television driven at 200% scaling paints that tile twice as many device pixels across, so
      // anything less than double the tile's width is a blurred tile on the set that has the most pixels.
      Assert.That(ArtworkConverter.TILE_DECODE_WIDTH, Is.GreaterThanOrEqualTo(2 * TILE_LOGICAL_WIDTH),
          "a tile decoded narrower than twice its logical width is a soft picture on a 4K panel");

      // Upper bound: the reason for asking at all is the decoded-picture budget, which holds eight million pixels.
      // Four times the tile's width is where twenty-four tiles of a folder stop fitting in it.
      Assert.That(ArtworkConverter.TILE_DECODE_WIDTH, Is.LessThanOrEqualTo(4 * TILE_LOGICAL_WIDTH),
          "decoded tiles have to leave room in the budget for all of a folder's tiles at once");
    }

    [Test]
    public void ATemplateMayAskForItsOwnWidth()
    {
      Assert.That(ArtworkConverter.DecodeWidthFor(97), Is.EqualTo(97),
          "a template whose tiles are a different size should not have to take the pictures screen's number");
    }

    [Test]
    public void AnythingThatIsNotASizeIsTheWholePicture()
    {
      // Every existing caller - the poster on a details screen, the raised photograph, the shelf's covers - passes
      // nothing and must keep getting the whole picture it was given before.
      Assert.That(ArtworkConverter.DecodeWidthFor(null!), Is.EqualTo(0));
      Assert.That(ArtworkConverter.DecodeWidthFor("poster"), Is.EqualTo(0));
      Assert.That(ArtworkConverter.DecodeWidthFor(0), Is.EqualTo(0));
      Assert.That(ArtworkConverter.DecodeWidthFor(-4), Is.EqualTo(0));
    }

    [AvaloniaTest]
    public void TheSameBytesAtTwoSizesAreTwoPictures()
    {
      ArtworkConverter converter = new ArtworkConverter();
      byte[] bytes = Picture();

      object? whole = converter.Convert(bytes, typeof(Bitmap), null!, System.Globalization.CultureInfo.InvariantCulture);
      object? tile = converter.Convert(bytes, typeof(Bitmap), ArtworkConverter.TILE_PARAMETER,
          System.Globalization.CultureInfo.InvariantCulture);

      // The photograph a viewer raises and the tile it came from are on screen together, from the same array of
      // bytes. If the size were not part of what the converter remembers, one of the two would be handed the
      // other's decoding - a raised photograph drawn from a tile, or a grid of twelve-megapixel pictures.
      Assert.That(tile, Is.Not.SameAs(whole));
      Assert.That(converter.Held, Is.EqualTo(2));
    }

    [AvaloniaTest]
    public void TheSameBytesAtTheSameSizeAreStillOnePicture()
    {
      ArtworkConverter converter = new ArtworkConverter();
      byte[] bytes = Picture();

      object? first = converter.Convert(bytes, typeof(Bitmap), ArtworkConverter.TILE_PARAMETER,
          System.Globalization.CultureInfo.InvariantCulture);
      object? again = converter.Convert(bytes, typeof(Bitmap), ArtworkConverter.TILE_PARAMETER,
          System.Globalization.CultureInfo.InvariantCulture);

      Assert.That(again, Is.SameAs(first), "asking for the same tile picture twice is one decode, not two");
      Assert.That(converter.Held, Is.EqualTo(1));
    }

    [AvaloniaTest]
    public void AnEvictedPictureIsLetGoAndNeverKilled()
    {
      // The crash this pins out: the home wall decodes a fanart, the next picture arriving evicts the oldest
      // from the budget, and an Image control on screen is still painting with it - the bitmap the cache
      // handed out and the bitmap the visual holds are one and the same object. Disposing the evicted one
      // took the whole front end down with an ObjectDisposedException inside Image.Render.
      //
      // The budget is asked small here rather than the picture made large: the headless toolkit answers every
      // picture with a one-pixel stub and saves nothing, so a nine-megapixel stand-in cannot be decoded in
      // this fixture - while the eviction rule this guards does not care about sizes at all.
      ArtworkConverter converter = new ArtworkConverter();
      converter.HeldBudget = 1;

      object? poster = converter.Convert(Picture(), typeof(Bitmap), null!,
          System.Globalization.CultureInfo.InvariantCulture);
      Assert.That(poster, Is.InstanceOf<Bitmap>(), "the first picture is held despite the whole budget");

      converter.Convert(Picture(), typeof(Bitmap), null!, System.Globalization.CultureInfo.InvariantCulture);

      Assert.That(converter.Held, Is.EqualTo(1),
          "the second picture pushed the first out of the budget, oldest first, as the cache does");
      Assert.DoesNotThrow(() => System.GC.KeepAlive(((Bitmap) poster!).PixelSize),
          "the evicted picture is dropped, never disposed - whatever screen still paints with it has to be "
          + "allowed to finish; the garbage collector reclaims it once nothing stands on it any more");
    }
  }
}
