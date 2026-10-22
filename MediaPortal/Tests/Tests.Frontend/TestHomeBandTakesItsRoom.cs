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
    private const double SHADOW = 20;

    private static readonly string[] MOSAICS = { "poster1", "poster2", "poster3", "square1", "square2",
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

    /// <summary>
    /// Every cover that arrived stands on the band, inside the room the band takes - until the band is full. A
    /// mosaic has exactly as many places as the face drew squares, and a cover beyond the last of them is left off
    /// rather than put on top of another one, which is the same answer the reference gives: its grids are built out
    /// of a fixed number of controls.
    /// </summary>
    [Test]
    public void NoCoverStandsBeyondTheRoomItsBandTakes()
    {
      foreach (string layout in MOSAICS)
      {
        for (int covers = 1; covers <= 8; covers++)
        {
          HomeContentGroup band = Band(layout, covers);
          Assert.That(band.Tiles.Count, Is.LessThanOrEqualTo(6),
              $"the {layout} table the skin drew has six places, and a band stands in no more of them than that");
          Assert.That(band.Tiles.Count, Is.EqualTo(Math.Min(covers, 6)),
              $"every cover that arrived in the {layout} band stands on it, and no place is kept empty beside it");

          for (int a = 0; a < band.Tiles.Count; a++)
          {
            for (int b = a + 1; b < band.Tiles.Count; b++)
            {
              HomeContentTile first = band.Tiles[a];
              HomeContentTile second = band.Tiles[b];
              bool apart = first.X + first.Width <= second.X + 0.5
                  || second.X + second.Width <= first.X + 0.5
                  || first.Y + first.Height <= second.Y + 0.5
                  || second.Y + second.Height <= first.Y + 0.5;
              Assert.That(apart, Is.True,
                  $"the {(a + 1)}th and {(b + 1)}th covers of the {layout} band stand over each other, which is "
                  + "what filling a table beyond its places does");
            }
          }

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

    /// <summary>
    /// The rail of icons takes only the column it stands in, while the band of leftover plugins takes the whole
    /// width the face drew it in. The reference lays those plugins out in a mosaic of its own - a band of 1500 of
    /// its 1920 lines - and not in a column, because a band that shrank to the squares it happens to fill would
    /// leave the rest of the wall standing as a gap beside it.
    /// </summary>
    [Test]
    public void ShortcutRailTakesItsColumnAndPluginsBandItsWholeMosaic()
    {
      HomeContentGroup rail = new HomeContentGroup("Shortcuts", "shortcuts", 175, 5);
      for (int i = 0; i < 5; i++)
        rail.AddShortcut(new NereusHomeShortcut("Search " + (i + 1), "Search", null, null));

      HomeContentGroup plugins = PluginsBand(HomeContentGroup.PLUGINS_BAND_SLOTS);

      Assert.That(rail.OccupiedWidth, Is.LessThanOrEqualTo(175).And.GreaterThan(150),
          $"the shortcut rail keeps the column the skin drew and fills it: it takes {rail.OccupiedWidth} of 175, "
          + "and a rail that took far less than its column would leave a gap beside the wall");
      Assert.That(rail.Tiles.Max(tile => tile.X + tile.Width), Is.LessThanOrEqualTo(175),
          "and no shortcut reaches past the column it was given");
      Assert.That(rail.Height, Is.EqualTo(880), "and every band keeps the height the skin drew it in");
      Assert.That(plugins.OccupiedWidth, Is.EqualTo(1500),
          $"the plugins band stands in the width the reference drew it in, not in the column its squares happen "
          + $"to fill: it takes {plugins.OccupiedWidth} of {plugins.Width}");

      HomeContentTile lowest = rail.Tiles[rail.Tiles.Count - 1];
      Assert.That(lowest.Y + lowest.Height + SHADOW, Is.LessThanOrEqualTo(rail.BoxHeight),
          "the lowest shortcut's shadow falls inside the rail's box, so the wall does not cut it off: "
          + (lowest.Y + lowest.Height) + " plus its shadow against " + rail.BoxHeight);
    }

    /// <summary>
    /// The reference's "Others" grid is seven columns by four rows, the first, sixth and thirteenth square twice as
    /// wide and high as the rest, tiled without a hole. The squares are the size that measure gives - about two
    /// hundred lines across - and not the slivers a single column cut to the band's height leaves behind: at the
    /// old column this band shrank its squares to fifty eight lines once a dozen plugins were left over.
    /// </summary>
    [Test]
    public void PluginsBandStandsInTheMosaicTheReferenceDrew()
    {
      HomeContentGroup plugins = PluginsBand(HomeContentGroup.PLUGINS_BAND_SLOTS);

      Assert.That(plugins.Tiles.Count, Is.EqualTo(HomeContentGroup.PLUGINS_BAND_SLOTS),
          "every leftover plugin the band has a place for stands on it");

      double small = plugins.Tiles[1].Width;
      Assert.That(small, Is.GreaterThanOrEqualTo(180),
          $"a plugin's square in a band 1500 wide across seven columns is about two hundred lines across, not "
          + $"{small}");

      foreach (int place in new[] { 0, 5, 12 })
      {
        // Two cells wide, less the five a square keeps on each side - which is ten lines more than two small
        // squares side by side, because the gap that lies between those two belongs to the big one's own cell pair.
        Assert.That(plugins.Tiles[place].Width, Is.EqualTo(small * 2 + 10).Within(1),
            $"the {(place + 1)}th square of the reference's grid stands over two cells, against the small one's "
            + $"{small}: the band put {plugins.Tiles[place].Width} there");
        Assert.That(plugins.Tiles[place].Height, Is.GreaterThan(plugins.Tiles[1].Height),
            "and it is taller than a small square too, so it is a big square and not a wide one");
      }

      for (int a = 0; a < plugins.Tiles.Count; a++)
      {
        for (int b = a + 1; b < plugins.Tiles.Count; b++)
        {
          HomeContentTile first = plugins.Tiles[a];
          HomeContentTile second = plugins.Tiles[b];
          bool apart = first.X + first.Width <= second.X + 0.5
              || second.X + second.Width <= first.X + 0.5
              || first.Y + first.Height <= second.Y + 0.5
              || second.Y + second.Height <= first.Y + 0.5;
          Assert.That(apart, Is.True,
              $"the {(a + 1)}th and {(b + 1)}th squares of the band stand over each other, which is what filling a "
              + "table beyond its places does");
        }
      }

      foreach (HomeContentTile tile in plugins.Tiles)
      {
        Assert.That(tile.X + tile.Width, Is.LessThanOrEqualTo(plugins.OccupiedWidth),
            $"the square at {tile.X},{tile.Y} stays inside the width the band takes");
        Assert.That(tile.Y + tile.Height + SHADOW, Is.LessThanOrEqualTo(plugins.BoxHeight),
            $"and its shadow still falls inside the band's box: {tile.Y + tile.Height} +{SHADOW} of "
            + plugins.BoxHeight);
      }
    }

    /// <summary>
    /// A mosaic has as many places as the face drew squares, and a plugin beyond the last of them is not shown -
    /// which is the reference's own answer, since its grid is built out of nineteen controls and its converter
    /// hands that grid the whole list to fill them. The place a leftover wants is made in the menu editor.
    /// </summary>
    [Test]
    public void PluginsBeyondTheirPlacesAreLeftOffTheWall()
    {
      HomeContentGroup plugins = new HomeContentGroup("Others", HomeContentGroup.PLUGINS_LAYOUT, 1500,
          HomeContentGroup.PLUGINS_BAND_SLOTS);
      for (int i = 0; i < HomeContentGroup.PLUGINS_BAND_SLOTS + 6; i++)
        plugins.AddMenuEntry(Plugin(i));

      Assert.That(plugins.Tiles.Count, Is.EqualTo(HomeContentGroup.PLUGINS_BAND_SLOTS),
          $"a twentieth leftover plugin has no square to stand on and is not drawn over the first one: the band "
          + $"holds {plugins.Tiles.Count} of 25");
    }

    /// <summary>
    /// Every band starts its squares on the same line under its heading, and a box grows when the squares it
    /// holds reach past the height the band was drawn in.
    /// </summary>
    [Test]
    public void EveryBandStartsItsSquaresOnTheSameLine()
    {
      HomeContentGroup mosaic = Band("poster1", 6);
      HomeContentGroup rail = new HomeContentGroup("Shortcuts", "shortcuts", 175, 5);
      for (int i = 0; i < 5; i++)
        rail.AddShortcut(new NereusHomeShortcut("Search " + (i + 1), "Search", null, null));

      HomeContentGroup plugins = PluginsBand(HomeContentGroup.PLUGINS_BAND_SLOTS);

      double line = mosaic.Tiles.Min(tile => tile.Y);
      Assert.That(rail.Tiles[0].Y, Is.EqualTo(line),
          $"the shortcut rail starts its first square at {rail.Tiles[0].Y} while a cover starts at {line}, so "
          + "the heading stands closer to its squares than to the covers beside them");
      Assert.That(plugins.Tiles[0].Y, Is.EqualTo(line),
          $"the plugins band starts at {plugins.Tiles[0].Y} against the covers' {line}");

      Assert.That(rail.BoxHeight, Is.GreaterThan(rail.Height),
          "five shortcuts standing on the covers' line reach past the height the band was drawn in, and the "
          + "box has to grow with them - otherwise the wall cuts the lowest shadow off again");
      HomeContentTile deepest = rail.Tiles[rail.Tiles.Count - 1];
      Assert.That(deepest.Y + deepest.Height + SHADOW, Is.LessThanOrEqualTo(rail.BoxHeight),
          "and the box it grows to still leaves that shadow its room: " + (deepest.Y + deepest.Height)
          + " plus its shadow against " + rail.BoxHeight);
    }

    private static HomeContentGroup PluginsBand(int plugins)
    {
      HomeContentGroup band = new HomeContentGroup("Others", HomeContentGroup.PLUGINS_LAYOUT, 1500, plugins);
      for (int i = 0; i < plugins; i++)
        band.AddMenuEntry(Plugin(i));
      return band;
    }

    private static MainMenuEntry Plugin(int number)
    {
      return new MainMenuEntry(null, null, "Plugin " + (number + 1), Array.Empty<string>(), "opens the plugin",
          null, homeImageId: Guid.NewGuid().ToString("D"));
    }
  }
}
