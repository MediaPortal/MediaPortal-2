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
using System.IO;
using Avalonia;
using Avalonia.Headless.NUnit;
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

    [SetUp]
    public void IsolateTheSkinSettingsFolder()
    {
      _previousDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
      _ownDir = Path.Combine(Path.GetTempPath(), "mp2-tiles-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_ownDir);
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", _ownDir);
    }

    [TearDown]
    public void PutTheMachineBack()
    {
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR",
          _previousDir.Length == 0 ? null : _previousDir);

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
    public void WmcAndTheOwnFaceKeepTheirShelvesAsRows()
    {
      Application app = RequireApplication();

      // WMC ships no media tiles in this front end; offering a "View: Grid" button there would be a lie.
      FrontendTheme.Apply(app, "wmc", out _);
      MediaBrowserViewModel wmc = new MediaBrowserViewModel(null, MoviesSection(), null);
      Assert.That(wmc.HasTiles, Is.False);
      Assert.That(wmc.Layout, Is.EqualTo(BrowserLayout.List));

      wmc.SwitchView();
      Assert.That(wmc.Layout, Is.EqualTo(BrowserLayout.List),
          "where there are no tiles, the switch has nothing to switch to");

      // And the front end's own face: the same.
      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
      MediaBrowserViewModel own = new MediaBrowserViewModel(null, MoviesSection(), null);
      Assert.That(own.HasTiles, Is.False);
      Assert.That(own.Layout, Is.EqualTo(BrowserLayout.List));
      Assert.That(own.WatchFlagsShown, Is.True, "with no skin to ask, the quiet tick stays");
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
    }
  }
}
