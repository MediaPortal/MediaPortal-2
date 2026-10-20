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
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Linq;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Skins.Nereus;
using MediaPortal.Frontend.ViewModels;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// A band of home content takes sideways only the room its covers cover. The skin's mosaic tables were drawn
  /// for a band the server filled, and a band that came back short would otherwise keep the places of the covers
  /// that never came standing empty - which the wall beside it reads as a gap, the one thing a wall of covers may
  /// not have. Vertically a band keeps the height the skin drew it in.
  /// </summary>
  [TestFixture]
  public class TestHomeBandTakesItsRoom
  {
    private const double DESIGN = 900;
    private const double SHADOW = 20;    private static readonly string[] MOSAICS = { "poster1", "poster2", "poster3", "square1", "square2",
        "thumbnail1", "thumbnail2" };

    private static HomeContentGroup Band(string layout, int covers, double width = DESIGN)
    {
      HomeContentGroup band = new HomeContentGroup("Latest added", layout, width, covers);
      for (int i = 0; i < covers; i++)
        band.AddTitle(new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Title " + (i + 1), null, 2020 - i, null,
            null, false)));
      return band;
    }

    [Test]
    public void FullBandKeepsTheSkinMosaic()
    {
      HomeContentGroup band = Band("poster1", 6);

      Assert.That(band.OccupiedWidth, Is.EqualTo(DESIGN),
        "a band the server filled stands in the width the skin drew it in");
      Assert.That(band.Tiles[0].Width, Is.GreaterThan(band.Tiles[1].Width),
        "with the first cover the big one, as the skin's table puts it");
    }

    [Test]
    public void ShortBandKeepsNoRoomStandingEmpty()
    {
      foreach (string layout in MOSAICS)
      {
        HomeContentGroup band = Band(layout, 2);
        double widest = band.Tiles.Max(tile => tile.X + tile.Width);

        Assert.That(band.OccupiedWidth, Is.LessThan(DESIGN),
            $"two covers in the {layout} band may not keep a column of six warm: the band occupies "
            + $"{band.OccupiedWidth} of {DESIGN}");
        Assert.That(band.OccupiedWidth - widest, Is.LessThanOrEqualTo(12),
            $"and it keeps nothing beside its widest cover but its own margin: {band.OccupiedWidth} against "
            + $"{widest}");

        HomeContentTile first = band.Tiles[0];
        HomeContentTile second = band.Tiles[1];
        bool beside = Math.Abs(second.X - (first.X + first.Width + 10)) < 0.5
            && Math.Abs(second.Y - first.Y) < 0.5;
        bool below = Math.Abs(second.X - first.X) < 0.5 && second.Y > first.Y;
        Assert.That(beside || below, Is.True,
            $"the {layout} band's second cover stands beside or below the first, never in a column the missing "
            + "covers would have filled");
      }
    }

    [Test]
    public void NoCoverStandsBeyondTheRoomItsBandTakes()
    {
      foreach (string layout in MOSAICS)
      {
        for (int covers = 1; covers <= 8; covers++)
        {
          HomeContentGroup band = Band(layout, covers);
          Assert.That(band.Tiles.Count, Is.EqualTo(covers), "every cover that arrived stands on the band");

          foreach (HomeContentTile tile in band.Tiles)
          {
            Assert.That(tile.X + tile.Width, Is.LessThanOrEqualTo(band.OccupiedWidth),
                $"the {layout} band with {covers} covers keeps its cover at {tile.X},{tile.Y} inside the width it "
                + $"takes: {tile.X + tile.Width} of {band.OccupiedWidth}");
            Assert.That(tile.Y + tile.Height + SHADOW, Is.LessThanOrEqualTo(band.BoxHeight),
                $"and its shadow - four down and sixteen blurred - still falls inside the band's box: {tile.Y + tile.Height} "
                + $"+{SHADOW} of {band.BoxHeight}");
          }
        }
      }
    }

    [Test]
    public void BigCoverIsKeptOnlyWhileTheCoversBesideItArrive()
    {
      HomeContentGroup threeInSquare = Band("square1", 3);
      Assert.That(threeInSquare.Tiles[0].Width, Is.GreaterThan(threeInSquare.Tiles[1].Width),
        "three covers in a three-column band fill the cells beside the big one, so the big cover stands");

      HomeContentGroup twoInPoster = Band("poster1", 2);
      Assert.That(twoInPoster.Tiles[0].Width, Is.EqualTo(twoInPoster.Tiles[1].Width),
        "two covers in a four-column band could not fill the cells beside a big one, so nothing stands empty "
        + "beside it: both covers are one cell wide");
    }

    [Test]
    public void ShortcutRailAndPluginsBandTakeOnlyTheirColumn()
    {
      HomeContentGroup rail = new HomeContentGroup("Shortcuts", "shortcuts", 175, 5);
      HomeContentGroup plugins = new HomeContentGroup("Plugins", "plugins", 1500, 5);
      for (int i = 0; i < 5; i++)
      {
        rail.AddShortcut(new NereusHomeShortcut("Search " + (i + 1), "Search", null, null));
        plugins.AddShortcut(new NereusHomeShortcut("Plugin " + (i + 1), "Plugin", null, null));
      }

      Assert.That(rail.OccupiedWidth, Is.EqualTo(175), "the shortcut rail keeps the column the skin drew");
      Assert.That(plugins.OccupiedWidth, Is.LessThan(plugins.Width / 2),
          $"a plugins band drawn across the whole screen still takes only the column its entries stand in: it "
          + $"occupies {plugins.OccupiedWidth} of {plugins.Width}");
      Assert.That(rail.Height, Is.EqualTo(880), "and every band keeps the height the skin drew it in");

      HomeContentTile lowest = rail.Tiles[rail.Tiles.Count - 1];
      Assert.That(lowest.Y + lowest.Height + SHADOW, Is.LessThanOrEqualTo(rail.BoxHeight),
          "the lowest shortcut's shadow falls inside the rail's box, so the wall does not cut it off: "
          + (lowest.Y + lowest.Height) + " plus its shadow against " + rail.BoxHeight);
    }
  }
}
