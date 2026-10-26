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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using NUnit.Framework;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using FrontendAspect = MediaPortal.Frontend.Api.MediaAspect;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The shelf's two shapes: which media wears which tile in which skin, which layout a shelf opens in,
  /// and what the rows know about watched and about pictures. The pictures themselves - the tile templates
  /// painted from the skin's files - are proven by the render gate; what is proven here is the deciding.
  /// </summary>
  /// <remarks>
  /// Every test names its own settings folder through MP2_SKIN_SETTINGS_DIR, so no test reads or writes what
  /// a real viewer on this machine set, and takes the worn skin off again afterwards.
  /// </remarks>
  [TestFixture]
  public class TestMediaBrowserTiles
  {
    private string _previousDir = "";
    private string _ownDir = "";
    private string _previousScreensFile = "";

    [SetUp]
    public void IsolateTheSkinSettingsFolder()
    {
      _previousDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
      _ownDir = Path.Combine(Path.GetTempPath(), "mp2-tiles-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_ownDir);
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", _ownDir);

      // The shapes the screens remembered are just as much the machine's memory as the skin settings are - and
      // inside this fixture the tests write them on purpose, so each test starts with nobody having remembered
      // anything and one test leaving the films in covers cannot decide how the next one opens.
      _previousScreensFile = Environment.GetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE") ?? "";
      Environment.SetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE",
          Path.Combine(_ownDir, "screens.json"));
    }

    [TearDown]
    public void PutTheMachineBack()
    {
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR",
          _previousDir.Length == 0 ? null : _previousDir);
      Environment.SetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE",
          _previousScreensFile.Length == 0 ? null : _previousScreensFile);

      Application? app = Application.Current;
      if (app != null)
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
    }

    private static Application RequireApplication()
    {
      Application? app = Application.Current;
      Assert.That(app, Is.Not.Null, "the shelf needs the application to know what skin it wears");
      return app!;
    }

    private static HomeSection MoviesSection()
    {
      return new HomeSection("Movies",
          new AspectViewModel(new FrontendAspect(MovieAspect.ASPECT_ID, MovieAspect.Metadata.Name, false, false,
              null, Array.Empty<MediaPortal.Frontend.Api.AspectAttribute>())));
    }

    [Test]
    public void TheThreeMediaKnowTheirTileShape()
    {
      NereusSkinSettings settings = new NereusSkinSettings();
      settings.MovieGridViewType = GridViewType.Banner;
      settings.SeriesGridViewType = GridViewType.Thumbnail;

      // The three the reference's settings class holds a knob for, and the collection walks with its films.
      Assert.That(MediaBrowserViewModel.TileKindFor("MovieItem", settings), Is.EqualTo(GridViewType.Banner));
      Assert.That(MediaBrowserViewModel.TileKindFor("MovieCollectionItem", settings),
          Is.EqualTo(GridViewType.Banner));
      Assert.That(MediaBrowserViewModel.TileKindFor("SeriesItem", settings),
          Is.EqualTo(GridViewType.Thumbnail));
      Assert.That(MediaBrowserViewModel.TileKindFor("SeasonItem", settings),
          Is.EqualTo(GridViewType.Poster), "untouched, the reference's default shape");

      // Everything else has no knob and wears the plain poster, as it did in the reference.
      Assert.That(MediaBrowserViewModel.TileKindFor("PictureAlbum", settings), Is.EqualTo(GridViewType.Poster));
      Assert.That(MediaBrowserViewModel.TileKindFor("AudioItem", settings), Is.EqualTo(GridViewType.Poster));
      Assert.That(MediaBrowserViewModel.TileKindFor("", settings), Is.EqualTo(GridViewType.Poster));

      Assert.That(MediaBrowserViewModel.TileKey(GridViewType.Poster), Is.EqualTo("MediaTilePosterTemplate"));
      Assert.That(MediaBrowserViewModel.TileKey(GridViewType.Banner), Is.EqualTo("MediaTileBannerTemplate"));
      Assert.That(MediaBrowserViewModel.TileKey(GridViewType.Thumbnail), Is.EqualTo("MediaTileThumbnailTemplate"));
    }

    [AvaloniaTest]
    public void NereusOpensItsShelfInTilesAndTheButtonTurnsBack()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      MediaBrowserViewModel vm = new MediaBrowserViewModel(null, MoviesSection(), null);

      Assert.That(vm.HasTiles, Is.True, "the worn skin answers the tile keys; the shelf may use them");
      Assert.That(vm.Layout, Is.EqualTo(BrowserLayout.Grid),
          "Nereus is a grid skin; a shelf opening as rows there is wrong at first sight");
      Assert.That(vm.GridTemplateKey, Is.EqualTo("MediaTilePosterTemplate"));
      Assert.That(vm.LayoutLabel, Is.EqualTo("View: Grid"));

      vm.SwitchView();
      Assert.That(vm.Layout, Is.EqualTo(BrowserLayout.Cover),
          "the reference's view mode had a third step besides rows and tiles, and Nereus draws it");
      Assert.That(vm.GridTemplateKey, Is.EqualTo("MediaTileCoverTemplate"));
      Assert.That(vm.LayoutLabel, Is.EqualTo("View: Cover"));

      vm.SwitchView();
      Assert.That(vm.Layout, Is.EqualTo(BrowserLayout.List));
      Assert.That(vm.GridTemplateKey, Is.EqualTo("TitleRowTemplate"),
          "rows ask the skin for the row template, exactly as the shelf always did");
      Assert.That(vm.LayoutLabel, Is.EqualTo("View: List"));

      vm.SwitchView();
      Assert.That(vm.Layout, Is.EqualTo(BrowserLayout.Grid), "the button is a round trip, not a one-way door");
    }

    [AvaloniaTest]
    public void TheSkinsOwnSettingDecidesTheShapeOfFilms()
    {
      Application app = RequireApplication();

      // The viewer chose banners for films in the skin settings screen; the films' shelf has to greet them
      // with banners, without the shelf or the button being asked anything.
      string file = Path.Combine(_ownDir, "skin-nereus.json");
      NereusSkinSettings settings = NereusSkinSettings.Load(file, out _);
      settings.MovieGridViewType = GridViewType.Banner;
      settings.Save(file, out bool saved);
      Assert.That(saved, Is.True);

      FrontendTheme.Apply(app, "nereus", out _);
      MediaBrowserViewModel vm = new MediaBrowserViewModel(null, MoviesSection(), null);

      Assert.That(vm.Layout, Is.EqualTo(BrowserLayout.Grid));
      Assert.That(vm.GridTemplateKey, Is.EqualTo("MediaTileBannerTemplate"));
    }

    [AvaloniaTest]
    public void TheWatchedCornersObeyTheGridSetting()
    {
      Application app = RequireApplication();

      string file = Path.Combine(_ownDir, "skin-nereus.json");
      NereusSkinSettings settings = NereusSkinSettings.Load(file, out _);
      settings.EnableGridWatchedFlags = false;
      settings.Save(file, out _);

      FrontendTheme.Apply(app, "nereus", out _);
      Assert.That(new MediaBrowserViewModel(null, MoviesSection(), null).WatchFlagsShown, Is.False,
          "the reference hid the corners behind exactly this setting");

      settings.EnableGridWatchedFlags = true;
      settings.Save(file, out _);
      Assert.That(new MediaBrowserViewModel(null, MoviesSection(), null).WatchFlagsShown, Is.True);
    }

    [AvaloniaTest]
    public void EveryShelfWalksAllThreeViewsEvenWithoutASkin()
    {
      Application app = RequireApplication();

      // WMC now carries its own shelf tiles, so it opens as a grid and its view button walks all three shapes.
      FrontendTheme.Apply(app, "wmc", out _);
      MediaBrowserViewModel wmc = new MediaBrowserViewModel(null, MoviesSection(), null);
      Assert.That(wmc.HasTiles, Is.True, "WMC learned the shelf tiles, so its films open as tiles");
      Assert.That(wmc.Layout, Is.EqualTo(BrowserLayout.Grid));

      wmc.SwitchView();
      Assert.That(wmc.Layout, Is.EqualTo(BrowserLayout.Cover));
      wmc.SwitchView();
      Assert.That(wmc.Layout, Is.EqualTo(BrowserLayout.List),
          "and the walk closes back onto the rows the reference's view mode started from");
      wmc.SwitchView();
      Assert.That(wmc.Layout, Is.EqualTo(BrowserLayout.Grid));

      // And the front end's own face, which ships no tile template of its own: there the shelf still opens as the
      // rows it was written with and still carries the quiet tick - but the button is no longer a lie, because a
      // shelf walks all three shapes whether a skin draws them or the front end's own frames answer for them. The
      // shapes the WMC walk above remembered are swept away first, so this screen really is one nobody has asked.
      System.IO.File.Delete(Path.Combine(_ownDir, "screens.json"));
      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
      MediaBrowserViewModel own = new MediaBrowserViewModel(null, MoviesSection(), null);
      Assert.That(own.HasTiles, Is.False);
      Assert.That(own.Layout, Is.EqualTo(BrowserLayout.List));
      Assert.That(own.WatchFlagsShown, Is.True, "with no skin to ask, the quiet tick stays");
      own.SwitchView();
      Assert.That(own.Layout, Is.EqualTo(BrowserLayout.Grid),
          "the view button has tiles to switch to even where no skin draws them");
      own.SwitchView();
      Assert.That(own.Layout, Is.EqualTo(BrowserLayout.Cover));
      own.SwitchView();
      Assert.That(own.Layout, Is.EqualTo(BrowserLayout.List));
    }

    [AvaloniaTest]
    public void AShelfRemembersTheShapeItsViewerLeftItIn()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);

      MediaBrowserViewModel films = new MediaBrowserViewModel(null, MoviesSection(), null);
      Assert.That(films.Layout, Is.EqualTo(BrowserLayout.List));

      films.SwitchView();
      films.SwitchView();
      Assert.That(films.Layout, Is.EqualTo(BrowserLayout.Cover));

      // The next time this screen opens - a new view model for the same shelf - it greets the viewer in the
      // shape they chose here, which is what "stored by screen" asks for.
      MediaBrowserViewModel again = new MediaBrowserViewModel(null, MoviesSection(), null);
      Assert.That(again.Layout, Is.EqualTo(BrowserLayout.Cover),
          "the films stay in covers because the films' screen was left in covers");
      Assert.That(again.GridTemplateKey, Is.EqualTo(MediaBrowserViewModel.COVER_TEMPLATE_KEY));

      // And a different screen is a different answer: nobody picked anything for the series' screen.
      HomeSection series = new HomeSection("Series",
          new AspectViewModel(new FrontendAspect(Guid.Parse("22222222-2222-2222-2222-222222222222"),
              "SeriesItem", false, false,
              null, Array.Empty<MediaPortal.Frontend.Api.AspectAttribute>())));
      MediaBrowserViewModel episodes = new MediaBrowserViewModel(null, series, null);
      Assert.That(episodes.Layout, Is.EqualTo(BrowserLayout.List));
    }

    [Test]
    public void ARowKnowsWatchedStartedAndPictureless()
    {
      TitleViewModel finished = new TitleViewModel(
          new MediaTitle(Guid.NewGuid(), "Finished", null, null, null, null, false, PlayCount: 1));
      Assert.That(finished.IsWatched, Is.True);
      Assert.That(finished.IsPartlyWatched, Is.False,
          "a finished title never wears two corners");

      TitleViewModel started = new TitleViewModel(
          new MediaTitle(Guid.NewGuid(), "Started", null, null, null, null, false, Resume: 42));
      Assert.That(started.IsWatched, Is.False);
      Assert.That(started.IsPartlyWatched, Is.True);

      TitleViewModel untouched = new TitleViewModel(
          new MediaTitle(Guid.NewGuid(), "Untouched", null, null, null, null, false));
      Assert.That(untouched.IsWatched, Is.False);
      Assert.That(untouched.IsPartlyWatched, Is.False);

      // The picture: while none has arrived the tile paints the skins fallback, and it stops the moment one does.
      Assert.That(untouched.HasArtwork, Is.False);
      Assert.That(untouched.HasNoArtwork, Is.True);
      untouched.ShowArtwork(new byte[] { 1, 2, 3 });
      Assert.That(untouched.HasArtwork, Is.True);
      Assert.That(untouched.HasNoArtwork, Is.False);
      untouched.ShowArtwork(null);
      Assert.That(untouched.HasNoArtwork, Is.True, "a server that says there is none is believed");

      // The corner flag of the reference's Virtual rows: a title the library knows but has no file behind.
      TitleViewModel virtualTitle = new TitleViewModel(
          new MediaTitle(Guid.NewGuid(), "Virtual", null, null, null, null, true));
      Assert.That(virtualTitle.IsOffline, Is.True, "a stub row wears the flag and its offline mark");
      Assert.That(untouched.IsOffline, Is.False, "an ordinary row wears neither");
    }

    /// <summary>
    /// A server whose typed fanart endpoint answers one title's backdrop with bytes and every other request
    /// with the honest 404 - the shape the real controller gives a title the cache and the media folder have
    /// no picture of that kind for.
    /// </summary>
    private sealed class OneFanartServer : HttpMessageHandler
    {
      private readonly Guid _withPicture;

      public OneFanartServer(Guid withPicture)
      {
        _withPicture = withPicture;
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string url = request.RequestUri!.ToString();
        bool asked = url.EndsWith("/fanart/FanArt", StringComparison.OrdinalIgnoreCase);
        bool known = url.Contains(_withPicture.ToString("D"), StringComparison.OrdinalIgnoreCase);
        if (asked && known)
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
          {
            Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 })
          });
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
      }
    }

    private static void WaitUntil(string because, Func<bool> condition)
    {
      DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);
      while (DateTimeOffset.UtcNow < deadline)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(5);
        if (condition())
          return;
      }
      Assert.Fail("Timed out after 15 seconds waiting for " + because);
    }

    [AvaloniaTest]
    public void TheFanartFollowsTheSelectionWhenTheSkinWantsIt()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      Guid withPicture = Guid.NewGuid();
      ServerApiClient client = new ServerApiClient(
          new Uri("http://127.0.0.1:1/MediaPortal/Server/"), new HttpClient(new OneFanartServer(withPicture)));
      MediaBrowserViewModel vm = new MediaBrowserViewModel(client, MoviesSection(), null);

      Assert.That(vm.FanartShown, Is.True, "Nereus wants fanart behind the shelf by default");
      Assert.That(vm.FanartOverlay, Is.EqualTo(0.85), "the reference's default dimming");

      vm.Selected = new TitleViewModel(new MediaTitle(withPicture, "With Fanart", null, null, null, null, false));
      WaitUntil("the fanart of the selected row to arrive", delegate { return vm.BackgroundArtwork != null; });
      Assert.That(vm.HasBackgroundArtwork, Is.True);

      // Walking to a row the cache has nothing for takes the room's mood away again, rather than leaving the
      // previous title's picture behind it.
      vm.Selected = new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Without", null, null, null, null, false));
      WaitUntil("the fanart to go away with the row that has none",
          delegate { return vm.BackgroundArtwork == null; });

      // And the walk back costs no second request - the same Artwork cache the rows use remembers the absence
      // and the presence alike, so the answer is there before the row is even drawn.
      vm.Selected = new TitleViewModel(new MediaTitle(withPicture, "With Fanart", null, null, null, null, false));
      Assert.That(vm.BackgroundArtwork, Is.Not.Null, "the remembered picture answers at once");
    }

    [AvaloniaTest]
    public void TheSettingOffKeepsTheScreenPlain()
    {
      Application app = RequireApplication();

      string file = Path.Combine(_ownDir, "skin-nereus.json");
      NereusSkinSettings settings = NereusSkinSettings.Load(file, out _);
      settings.EnableFanart = false;
      settings.Save(file, out _);

      FrontendTheme.Apply(app, "nereus", out _);
      Guid withPicture = Guid.NewGuid();
      ServerApiClient client = new ServerApiClient(
          new Uri("http://127.0.0.1:1/MediaPortal/Server/"), new HttpClient(new OneFanartServer(withPicture)));
      MediaBrowserViewModel vm = new MediaBrowserViewModel(client, MoviesSection(), null);

      Assert.That(vm.FanartShown, Is.False);
      vm.Selected = new TitleViewModel(new MediaTitle(withPicture, "With Fanart", null, null, null, null, false));
      Thread.Sleep(100);
      Dispatcher.UIThread.RunJobs();
      Assert.That(vm.BackgroundArtwork, Is.Null,
          "a shelf whose skin says no fanart asks for none and draws none");

      // The front end's own face has no setting to read and no fanart to show, which is what the reference's
      // default skin drew: no background model, no picture.
      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
      Assert.That(new MediaBrowserViewModel(client, MoviesSection(), null).FanartShown, Is.False);
    }

    /// <summary>
    /// A shelf that answers the title list once, hands out one distinct byte pattern per picture kind, and
    /// remembers which kinds it was asked for. The 404 for the thumbnail kind is what a library holding only
    /// posters and banners really answers.
    /// </summary>
    private sealed class KindArtworkServer : HttpMessageHandler
    {
      private readonly Guid _title;
      private readonly Guid _other;

      public KindArtworkServer(Guid title, Guid other)
      {
        _title = title;
        _other = other;
      }

      public List<string> Kinds { get; } = new List<string>();

      /// <summary>
      /// Whether the server was asked for a picture of the title it has nothing for. A test that wants to say
      /// "this row was asked about and stayed empty" waits for this, because an empty row that was never asked
      /// about looks exactly the same.
      /// </summary>
      internal bool AskedAboutTheTitleItHasNothingFor { get; private set; }

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string url = request.RequestUri!.ToString();
        if (url.Contains("/titles?", StringComparison.OrdinalIgnoreCase))
        {
          string json = await Task.FromResult(
              "{\"total\":2,\"offset\":0,\"limit\":60,\"returned\":2,\"sort\":\"title\",\"titles\":["
              + "{\"id\":\"" + _other.ToString("D") + "\",\"title\":\"Other\",\"year\":2001},"
              + "{\"id\":\"" + _title.ToString("D") + "\",\"title\":\"Harbour\",\"year\":2002}"
              + "]}");
          return new HttpResponseMessage(HttpStatusCode.OK)
          {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
          };
        }

        int typed = url.IndexOf("/fanart/", StringComparison.OrdinalIgnoreCase);
        if (typed >= 0)
        {
          string kind = url.Substring(typed + "/fanart/".Length);
          lock (Kinds)
          {
            Kinds.Add(kind);
          }

          if (url.Contains(_other.ToString("D"), StringComparison.OrdinalIgnoreCase))
            AskedAboutTheTitleItHasNothingFor = true;

          if (url.Contains(_title.ToString("D"), StringComparison.OrdinalIgnoreCase))
          {
            if (string.Equals(kind, "Banner", StringComparison.OrdinalIgnoreCase))
              return new HttpResponseMessage(HttpStatusCode.OK)
              {
                Content = new ByteArrayContent(new byte[] { 9, 8, 7, 6, 5 })
              };
            if (string.Equals(kind, "FanArt", StringComparison.OrdinalIgnoreCase))
              return new HttpResponseMessage(HttpStatusCode.OK)
              {
                Content = new ByteArrayContent(new byte[] { 3, 3, 3 })
              };
          }
          return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        if (url.EndsWith("/thumbnail", StringComparison.OrdinalIgnoreCase)
            && url.Contains(_title.ToString("D"), StringComparison.OrdinalIgnoreCase))
          return new HttpResponseMessage(HttpStatusCode.OK)
          {
            Content = new ByteArrayContent(new byte[] { 1, 1, 1, 1 })
          };
        return new HttpResponseMessage(HttpStatusCode.NotFound);
      }
    }

    [AvaloniaTest]
    public void ABannerShelfShowsBannersAndNotResizedPosters()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      string file = Path.Combine(_ownDir, "skin-nereus.json");
      NereusSkinSettings settings = NereusSkinSettings.Load(file, out _);
      settings.MovieGridViewType = GridViewType.Banner;
      settings.Save(file, out _);

      Guid title = Guid.NewGuid();
      KindArtworkServer handler = new KindArtworkServer(title, Guid.NewGuid());
      ServerApiClient client = new ServerApiClient(
          new Uri("http://127.0.0.1:1/MediaPortal/Server/"), new HttpClient(handler));

      MediaBrowserViewModel vm = new MediaBrowserViewModel(client, MoviesSection(), null);
      Assert.That(MediaBrowserViewModel.TileKindFor("MovieItem", settings), Is.EqualTo(GridViewType.Banner));

      vm.OpenCommand.Execute(null);
      WaitUntil("the shelf to fill", delegate { return vm.Titles.Count == 2; });
      TitleViewModel row = vm.Titles[1];
      Assert.That(row.Title.Id, Is.EqualTo(title));

      TestBrowserArtwork.DrawRows(vm, row);
      WaitUntil("the banner to reach the row", delegate { return row.Artwork != null; });
      Assert.That(row.Artwork, Is.EqualTo(new byte[] { 9, 8, 7, 6, 5 }),
          "the row wears the real banner the media folder holds");
      lock (handler.Kinds)
      {
        Assert.That(handler.Kinds, Does.Contain("Banner"), "the banner shelf asks the server for banners");
      }

      // The strip behind the shelf is the backdrop kind, never the banner and never a poster.
      vm.Selected = row;
      WaitUntil("the backdrop kind to be asked for",
          delegate { return vm.BackgroundArtwork != null; });
      Assert.That(row.Artwork, Is.EqualTo(new byte[] { 9, 8, 7, 6, 5 }),
          "the row kept its banner while the background changed");
      lock (handler.Kinds)
      {
        Assert.That(handler.Kinds, Does.Contain("FanArt"), "the background is a real backdrop");
      }

      // And a film the folders have no banner for stays honest: no art on the tile, which draws the
      // reference's placeholder layers - rather than a poster squeezed into the banner frame.
      TitleViewModel other = vm.Titles[0];
      TestBrowserArtwork.DrawRows(vm, other);
      WaitUntil("the banner-less film to have been asked about",
          delegate { return handler.AskedAboutTheTitleItHasNothingFor; });
      Assert.That(other.HasNoArtwork, Is.True,
          "a banner-less film wears the placeholder, not a resized poster");
    }

    [AvaloniaTest]
    public void APosterShelfStillDrawsFromThePosterBlob()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      Guid title = Guid.NewGuid();
      KindArtworkServer handler = new KindArtworkServer(title, Guid.NewGuid());
      ServerApiClient client = new ServerApiClient(
          new Uri("http://127.0.0.1:1/MediaPortal/Server/"), new HttpClient(handler));

      // No settings file was written here, so films keep the reference's default shape: the poster.
      MediaBrowserViewModel vm = new MediaBrowserViewModel(client, MoviesSection(), null);

      vm.OpenCommand.Execute(null);
      WaitUntil("the shelf to fill", delegate { return vm.Titles.Count == 2; });

      TestBrowserArtwork.DrawRows(vm, vm.Titles[1]);
      WaitUntil("the poster to reach the row", delegate { return vm.Titles[1].Artwork != null; });
      Assert.That(vm.Titles[1].Artwork, Is.EqualTo(new byte[] { 1, 1, 1, 1 }),
          "the plain poster shelf still paints from the database's poster blob");
      lock (handler.Kinds)
      {
        Assert.That(handler.Kinds, Has.None.EqualTo("Banner").And.None.EqualTo("Thumbnail"),
            "a poster shelf asks for no kinded tile picture; only the background may ask for its backdrop");
      }
    }

    [AvaloniaTest]
    public void ACoverWearsThePosterEvenWhenTheGridWearsBanners()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      string file = Path.Combine(_ownDir, "skin-nereus.json");
      NereusSkinSettings settings = NereusSkinSettings.Load(file, out _);
      settings.MovieGridViewType = GridViewType.Banner;
      settings.Save(file, out _);

      Guid title = Guid.NewGuid();
      KindArtworkServer handler = new KindArtworkServer(title, Guid.NewGuid());
      ServerApiClient client = new ServerApiClient(
          new Uri("http://127.0.0.1:1/MediaPortal/Server/"), new HttpClient(handler));

      MediaBrowserViewModel vm = new MediaBrowserViewModel(client, MoviesSection(), null);
      vm.OpenCommand.Execute(null);
      WaitUntil("the shelf to fill", delegate { return vm.Titles.Count == 2; });
      TestBrowserArtwork.DrawRows(vm, vm.Titles[1]);
      WaitUntil("the banner to reach the row", delegate { return vm.Titles[1].Artwork != null; });
      Assert.That(vm.Titles[1].Artwork, Is.EqualTo(new byte[] { 9, 8, 7, 6, 5 }));

      // The reference's cover item bound FanArtType="Poster" for films and for series alike - a cover is a
      // poster, whichever shape the grid chose - so walking the shelf into its cover view repaints the rows
      // with the poster blob, and back into the grid they put their banners on again. The screen does that
      // repainting, because it is the screen that knows which rows it has standing in front of it.
      vm.SwitchView();
      Assert.That(vm.Layout, Is.EqualTo(BrowserLayout.Cover));
      TestBrowserArtwork.DrawRows(vm, vm.Titles[1]);
      WaitUntil("the cover to wear the poster", delegate { return vm.Titles[1].Artwork!.SequenceEqual(new byte[] { 1, 1, 1, 1 }); });

      vm.SwitchView();
      Assert.That(vm.Layout, Is.EqualTo(BrowserLayout.List));
      TestBrowserArtwork.DrawRows(vm, vm.Titles[1]);

      vm.SwitchView();
      TestBrowserArtwork.DrawRows(vm, vm.Titles[1]);
      WaitUntil("the grid's banners to come back", delegate { return vm.Titles[1].Artwork!.SequenceEqual(new byte[] { 9, 8, 7, 6, 5 }); });
    }

    [AvaloniaTest]
    public void TheMediaButtonStandsBesideTheViewButtonAndNeitherReplacesTheOther()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      MediaBrowserViewModel vm = new MediaBrowserViewModel(null, MoviesSection(), null);
      Assert.That(vm.MediaLabel, Is.EqualTo("Media: Poster"),
          "the poster is the picture of a shelf nobody ever asked");
      Assert.That(vm.LayoutLabel, Is.EqualTo("View: Grid"),
          "the view question stands right next to the picture question, asked as before");

      // One round over the three pictures: each kind asks the skin for its own frame, and the view it is
      // walked in is untouched by the answer.
      vm.SwitchMedia();
      Assert.That(vm.MediaLabel, Is.EqualTo("Media: Banner"));
      Assert.That(vm.GridTemplateKey, Is.EqualTo("MediaTileBannerTemplate"));
      Assert.That(vm.Layout, Is.EqualTo(BrowserLayout.Grid), "the view stayed where it was");

      vm.SwitchMedia();
      Assert.That(vm.MediaLabel, Is.EqualTo("Media: Thumbnail"));
      Assert.That(vm.GridTemplateKey, Is.EqualTo("MediaTileThumbnailTemplate"));

      vm.SwitchMedia();
      Assert.That(vm.MediaLabel, Is.EqualTo("Media: Poster"));
      Assert.That(vm.GridTemplateKey, Is.EqualTo("MediaTilePosterTemplate"));

      // And the view button is likewise untouched by the picture button's doings.
      vm.SwitchView();
      Assert.That(vm.Layout, Is.EqualTo(BrowserLayout.Cover));
      Assert.That(vm.MediaLabel, Is.EqualTo("Media: Poster"), "the picture stayed where it was");
    }

    [AvaloniaTest]
    public void TheMediaButtonRepaintsWithTheKindsOwnBytesAndIsRememberedByScreen()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      Guid title = Guid.NewGuid();
      KindArtworkServer handler = new KindArtworkServer(title, Guid.NewGuid());
      ServerApiClient client = new ServerApiClient(
          new Uri("http://127.0.0.1:1/MediaPortal/Server/"), new HttpClient(handler));

      MediaBrowserViewModel vm = new MediaBrowserViewModel(client, MoviesSection(), null);
      vm.OpenCommand.Execute(null);
      WaitUntil("the shelf to fill", delegate { return vm.Titles.Count == 2; });
      TestBrowserArtwork.DrawRows(vm, vm.Titles[1]);
      WaitUntil("the poster to reach the row", delegate { return vm.Titles[1].Artwork != null; });
      Assert.That(vm.Titles[1].Artwork, Is.EqualTo(new byte[] { 1, 1, 1, 1 }));

      // The button repaints the rows that are already there, and the banner is the server's own banner
      // bytes - never a poster squeezed into the banner's frame, which is the complaint this answers. The
      // screen asks for those bytes again, because it is the screen that knows which rows to be seen.
      vm.SwitchMedia();
      TestBrowserArtwork.DrawRows(vm, vm.Titles[1]);
      WaitUntil("the banner to reach the row",
          delegate { return vm.Titles[1].Artwork != null && vm.Titles[1].Artwork!.SequenceEqual(new byte[] { 9, 8, 7, 6, 5 }); });
      lock (handler.Kinds)
      {
        Assert.That(handler.Kinds, Does.Contain("Banner"), "the press asks the server for the kind's own bytes");
      }

      vm.SwitchMedia();
      Assert.That(vm.MediaKind, Is.EqualTo(GridViewType.Thumbnail));

      // The screen remembers the answer its viewer gave it, and a second screen is a second answer.
      MediaBrowserViewModel again = new MediaBrowserViewModel(null, MoviesSection(), null);
      Assert.That(again.MediaKind, Is.EqualTo(GridViewType.Thumbnail),
          "the films' screen keeps the picture its viewer picked");
      HomeSection series = new HomeSection("Series",
          new AspectViewModel(new FrontendAspect(Guid.Parse("22222222-2222-2222-2222-222222222222"),
              "SeriesItem", false, false,
              null, Array.Empty<MediaPortal.Frontend.Api.AspectAttribute>())));
      MediaBrowserViewModel episodes = new MediaBrowserViewModel(null, series, null);
      Assert.That(episodes.MediaKind, Is.EqualTo(GridViewType.Poster),
          "nobody answered the picture question for the series' screen");
    }

    [AvaloniaTest]
    public void TheCoversCornersObeyTheCoversOwnSetting()
    {
      Application app = RequireApplication();

      string file = Path.Combine(_ownDir, "skin-nereus.json");
      NereusSkinSettings settings = NereusSkinSettings.Load(file, out _);
      settings.EnableGridWatchedFlags = true;
      settings.EnableCoverWatchedFlags = false;
      settings.Save(file, out _);

      FrontendTheme.Apply(app, "nereus", out _);
      KindArtworkServer handler = new KindArtworkServer(Guid.NewGuid(), Guid.NewGuid());
      ServerApiClient client = new ServerApiClient(
          new Uri("http://127.0.0.1:1/MediaPortal/Server/"), new HttpClient(handler));

      MediaBrowserViewModel vm = new MediaBrowserViewModel(client, MoviesSection(), null);
      vm.OpenCommand.Execute(null);
      WaitUntil("the shelf to fill", delegate { return vm.Titles.Count == 2; });

      Assert.That(vm.WatchFlagsShown, Is.True, "the grid's setting speaks for the grid's corners");
      Assert.That(vm.Titles[0].WatchFlagsShown, Is.True);

      vm.SwitchView();
      Assert.That(vm.Layout, Is.EqualTo(BrowserLayout.Cover));
      Assert.That(vm.WatchFlagsShown, Is.False,
          "the reference gated the cover's corners on its own extEnableCoverWatchFlags, not the grid's");
      Assert.That(vm.Titles[0].WatchFlagsShown, Is.False,
          "and the rows already on screen hear about it, not just the rows read afterwards");

      vm.SwitchView();
      vm.SwitchView();
      Assert.That(vm.Layout, Is.EqualTo(BrowserLayout.Grid));
      Assert.That(vm.Titles[0].WatchFlagsShown, Is.True,
          "back in the grid, the grid's own answer holds again");
    }
  }
}
