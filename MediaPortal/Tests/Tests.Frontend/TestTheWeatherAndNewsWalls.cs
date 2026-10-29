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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// The weather and the news tiles are the two whose content was never the server's, and the reference gave
  /// each of them a home content of its own rather than media lists: the weather shows the sky right now and
  /// the days ahead, the news shows its feeds and their newest headlines. Those walls are built out of cards -
  /// focusable squares that answer a standing viewer exactly like the covers beside them and open nothing when
  /// pressed - and they follow the reference's HasItems rule: an answer with nothing in it builds no band, and
  /// the tile's help note gets its turn.
  /// </summary>
  [TestFixture]
  public class TestTheWeatherAndNewsWalls
  {
    private static readonly string WEATHER_ACTION = HomeViewModel.WEATHER_ACTION_ID.ToString("D");
    private static readonly string NEWS_ACTION = HomeViewModel.NEWS_ACTION_ID.ToString("D");

    private static WeatherWallBrief TodayBrief()
    {
      WeatherNow now = new WeatherNow("light rain", 7.4, 4.1, 81, 1009, 3.6, "SW", "10",
          VisibilityMeters: 10000, CloudsPercent: 64, WindGust: 8.2, Sunrise: "07:12", Sunset: "18:04");
      WeatherDay[] days =
      {
        new WeatherDay(new DateTimeOffset(2026, 2, 3, 12, 0, 0, TimeSpan.Zero), "broken clouds", 6.2, 1.1, 0.4, "04", 6.0),
        new WeatherDay(new DateTimeOffset(2026, 2, 4, 12, 0, 0, TimeSpan.Zero), "clear sky", 8.0, -0.5, 0.05, "5", 0),
        new WeatherDay(new DateTimeOffset(2026, 2, 5, 12, 0, 0, TimeSpan.Zero), "scattered clouds", 5.5, 2.0, 0.55, "9", 7.0)
      };
      WeatherSlot[] hours = Enumerable.Range(0, 8).Select(i => new WeatherSlot(
          new DateTimeOffset(2026, 2, 3, 12, 0, 0, TimeSpan.Zero).AddHours(3 * i), "light rain",
          7.0 - i * 0.6, 0.4 + (i % 3) * 0.2, i * 0.7, "9")).ToArray();
      return new WeatherWallBrief("Berlin, Germany", now, days, true, hours,
          new WeatherAir(2, 12.1, 19.4, 14.2, 61.3),
          "https://tile.example.invalid/map/precipitation_new/7/68/42.png?appid=key&t=100");
    }

    private static NewsWallBrief NewsBrief()
    {
      FeedWallRow[] feeds =
      {
        new FeedWallRow("tagesschau", 5, null),
        new FeedWallRow("heise", 4, null),
        new FeedWallRow("dead feed", 0, "did not answer")
      };
      NewsHeadline[] headlines = Enumerable.Range(0, 12).Select(i => new NewsHeadline(
          "Headline " + (i + 1), "feed " + (i % 3),
          new DateTimeOffset(2026, 2, 3, 12, 0, 0, TimeSpan.Zero).AddMinutes(-i), "summary")).ToArray();
      return new NewsWallBrief(feeds, headlines);
    }

    private static MainMenuEntry TileOf(HomeViewModel home, string actionId)
    {
      MainMenuEntry entry = home.DisplayMenu.Single(item => item.HomeImageId == actionId);
      home.SelectedEntry = entry;
      return entry;
    }

    /// <summary>
    /// The weather tile's wall, as the design review redrew it: the shortcuts it always had, today's weather
    /// as one narrow card, the next hours as one chart, the days ahead in two columns of two, and the air and
    /// the radar on one card of their own - no help note anywhere, because something answered.
    /// </summary>
    [Test]
    public async Task TheWeatherTileStandsOnCardsAndNotOnAHelpNote()
    {
      HomeViewModel home = new HomeViewModel();
      home.WeatherWallReader = delegate (CancellationToken cancellation)
      {
        return Task.FromResult<WeatherWallBrief?>(TodayBrief());
      };
      MainMenuEntry entry = TileOf(home, WEATHER_ACTION);

      await home.ShowContentAsync(entry);

      HomeContentGroup today = home.ContentGroups.Single(group => group.Heading == "Current weather");
      Assert.That(today.Layout, Is.EqualTo("card1"),
          "today's weather is not the single-card band the wall cuts its squares from");
      Assert.That(today.Width, Is.EqualTo(368),
          "today's band is not as narrow as the design redrew it");
      Assert.That(today.Tiles, Has.Count.EqualTo(1));
      HomeInfoCard now = today.Tiles[0].Card!;
      Assert.That(today.Tiles[0].IsCard, Is.True, "the square carrying today is not known as a card");
      Assert.That(today.Tiles[0].IsCover, Is.False,
          "a card counts as a cover, so it would be dressed as a poster with a dark plate under it");
      Assert.That(today.Tiles[0].IsFocusable, Is.True,
          "the reference's weather tile could be stood on, and this one cannot");
      Assert.That(now.IsToday, Is.True, "today's card does not wear the face the design gave it");
      Assert.That(now.Label, Is.EqualTo("Berlin, Germany"), "today's card does not name the place");
      Assert.That(now.BigText, Does.Contain("°C"),
          "the temperature is not signed with the units the reader said the answer was measured in");
      Assert.That(now.IconName, Is.EqualTo("Weather/128x128/10"),
          "today's card does not wear the sky the answer named");
      Assert.That(now.HasCenteredIcon, Is.True,
          "today's picture does not stand centred under the numbers as the design drew it");
      Assert.That(now.Facts!.Single(fact => fact.Name == "humidity").Value, Is.EqualTo("81 %"),
          "the fact list does not carry the damp the endpoint answered");
      Assert.That(now.Facts!.Single(fact => fact.Name == "sun").Value, Does.Contain("07:12"),
          "the fact list does not carry the sun's own times");
      Assert.That(now.Facts!.Single(fact => fact.Name == "rain 3 h").Value, Does.Contain("mm"),
          "the fact list does not say how much has fallen");
      Assert.That(now.Air, Is.Not.Null,
          "the air came in but today's card does not know it - the air band leans on this same reading");

      HomeContentGroup chart = home.ContentGroups.Single(group => group.Heading == "Today");
      Assert.That(chart.Layout, Is.EqualTo("cardchart"),
          "the hours do not stand on the chart band the design drew");
      Assert.That(chart.Tiles, Has.Count.EqualTo(1));
      HomeInfoCard hours = chart.Tiles[0].Card!;
      Assert.That(hours.IsChart, Is.True, "the hours card does not wear the chart face");
      Assert.That(hours.Slots, Has.Count.EqualTo(8),
          "the chart was not handed the eight hours the endpoint answers with");
      Assert.That(hours.Slots![0].PrecipMm, Is.EqualTo(0),
          "the first hour's millimetres did not survive the brief");

      HomeContentGroup forecast = home.ContentGroups.Single(group => group.Heading == "Forecast");
      Assert.That(forecast.Layout, Is.EqualTo("cardday4"),
          "the days do not stand two over two as the design redrew them");
      Assert.That(forecast.Width, Is.EqualTo(410),
          "the forecast band is not as narrow as the design redrew it");
      Assert.That(forecast.Tiles, Has.Count.EqualTo(3));
      HomeInfoCard day = forecast.Tiles[0].Card!;
      Assert.That(day.IsDayFace, Is.True, "the day card does not wear the upright face the design drew");
      Assert.That(day.Label, Is.Not.Empty.And.Not.EqualTo(now.Label), "the day card does not name its day");
      Assert.That(day.Detail, Does.Contain(" / "), "the day card does not carry its span high / low");
      Assert.That(day.Sub + day.Sub2, Does.Contain("%").And.Contain("mm"),
          "the day card does not say how likely the rain is and how much of it");
      Assert.That(day.HasCenteredIcon, Is.True, "the day's picture does not stand in the middle of the card");

      HomeContentGroup air = home.ContentGroups.Single(group => group.Heading == "Air & radar");
      Assert.That(air.Tiles, Has.Count.EqualTo(1));
      HomeInfoCard breath = air.Tiles[0].Card!;
      Assert.That(breath.IsAir, Is.True, "the air card does not wear the air-and-radar face");
      Assert.That(breath.HasRadar, Is.True, "the radar picture was not handed to the card");
      Assert.That(breath.AqiNumber, Is.EqualTo("2"), "the air index is not the number the endpoint answered");
      Assert.That(breath.AqiGrade, Is.EqualTo("fair"), "the air index is not said in its one word");
      Assert.That(breath.Facts, Has.Count.EqualTo(4), "the four named pollutants are not listed");
      Assert.That(breath.Facts![0].Value, Does.Contain("µg/m³"),
          "the pollutants are not said in the units everyone grades air in");

      Assert.That(home.ContentGroups.Any(group => group.Heading == "Shortcuts"), Is.True,
          "the weather's own shortcuts went missing while its wall grew");
      Assert.That(home.ContentRows.Any(row => row.IsNote), Is.False,
          "something answered the weather tile and the pane still says what pressing it would do");
    }

    /// <summary>
    /// The news tile's wall: the feeds in their narrow column - a dead one saying so on its square - and the
    /// newest headlines on the same nineteen-place mosaic the leftover plugins stand on.
    /// </summary>
    [Test]
    public async Task TheNewsTileStandsOnItsFeedsAndItsHeadlines()
    {
      HomeViewModel home = new HomeViewModel();
      home.NewsWallReader = delegate (CancellationToken cancellation)
      {
        return Task.FromResult<NewsWallBrief?>(NewsBrief());
      };
      MainMenuEntry entry = TileOf(home, NEWS_ACTION);

      await home.ShowContentAsync(entry);

      HomeContentGroup feeds = home.ContentGroups.Single(group => group.Heading == "Feeds");
      Assert.That(feeds.Layout, Is.EqualTo("cardfeed"),
          "the feeds are not the square grid the viewer's mockup drew them as");
      Assert.That(feeds.Tiles, Has.Count.EqualTo(3));
      Assert.That(feeds.Tiles[2].Card!.Detail, Is.EqualTo("did not answer"),
          "a feed that refused does not say so on its own square");
      Assert.That(feeds.Tiles[0].Card!.Detail, Is.EqualTo("5 headlines"),
          "a feed that answered does not say how much it handed over");

      HomeContentGroup top = home.ContentGroups.Single(group => group.Heading == "Top news");
      Assert.That(top.Layout, Is.EqualTo(HomeContentGroup.PLUGINS_LAYOUT),
          "the top news does not take the mosaic the reference handed its TopNews list to");
      Assert.That(top.Tiles, Has.Count.EqualTo(12));
      HomeInfoCard headline = top.Tiles[0].Card!;
      Assert.That(headline.Label, Is.EqualTo("Headline 1"),
          "the headlines are not newest first on the mosaic");
      Assert.That(headline.Detail, Does.Contain("feed "), "a headline does not name its feed");
      Assert.That(headline.HasBadge, Is.False,
          "the viewer sent the letter badges away, and no headline wears one again");
    }

    /// <summary>
    /// The reference's one test for a list was HasItems, and it holds for these walls as it does for the
    /// shelves: an answer with nothing in it leaves no heading and no band - the tile then says what pressing
    /// it does, which is all the pane has left to say and the honest thing to say.
    /// </summary>
    [Test]
    public async Task WhatNothingAnswersLeavesNoBandAndKeepsTheWord()
    {
      HomeViewModel empty = new HomeViewModel();
      empty.WeatherWallReader = delegate (CancellationToken cancellation)
      {
        return Task.FromResult<WeatherWallBrief?>(
            new WeatherWallBrief("Nowhere", null, Array.Empty<WeatherDay>(), true));
      };
      MainMenuEntry weather = TileOf(empty, WEATHER_ACTION);

      await empty.ShowContentAsync(weather);

      Assert.That(empty.ContentGroups.Any(group => group.Layout.StartsWith("card", StringComparison.Ordinal)),
          Is.False, "an answer with no sky and no days built a band anyway");
      Assert.That(empty.ContentRows.Any(row => row.IsNote), Is.True,
          "with nothing on its wall the weather tile says nothing at all about what pressing it does");

      HomeViewModel unread = new HomeViewModel();
      MainMenuEntry news = TileOf(unread, NEWS_ACTION);
      await unread.ShowContentAsync(news);
      Assert.That(unread.ContentGroups.Any(group => group.Layout.StartsWith("card", StringComparison.Ordinal)),
          Is.False, "a news tile with no reader put up cards out of nothing");
    }

    /// <summary>
    /// Walking off the tile and back does not ask the internet twice - the answer stands a quarter of an
    /// hour, as it did in the old plugin - and the Refresh shortcut is the one press that throws the answer
    /// away and asks again, because there the viewer asked to see it happen.
    /// </summary>
    [Test]
    public async Task TheWallIsReadOnceAndTheRefreshShortcutAsksAgain()
    {
      int asked = 0;
      HomeViewModel home = new HomeViewModel();
      home.WeatherWallReader = delegate (CancellationToken cancellation)
      {
        Interlocked.Increment(ref asked);
        return Task.FromResult<WeatherWallBrief?>(TodayBrief());
      };
      MainMenuEntry entry = TileOf(home, WEATHER_ACTION);

      await home.ShowContentAsync(entry);
      await home.ShowContentAsync(entry);
      Assert.That(Volatile.Read(ref asked), Is.EqualTo(1),
          "walking onto the weather tile again asked the internet again instead of standing on the answer");

      home.RefreshInfoWall();
      int deadline = Environment.TickCount + 10_000;
      while (Volatile.Read(ref asked) < 2 && Environment.TickCount < deadline)
        Thread.Sleep(50);
      Assert.That(Volatile.Read(ref asked), Is.EqualTo(2),
          "the Refresh shortcut did not throw the answer away and read again");
    }

    /// <summary>
    /// On the shown screen a card is dressed as the reference dressed its weather and news tiles: it answers
    /// a viewer exactly like a cover and a plaque on the same wall - the shared class, the focusable square,
    /// the shared edge, the name in the shared fade - and a card is not counted among the plaques, so the
    /// dress the plugin test counts cannot quietly start counting weather.
    /// </summary>
    [AvaloniaTest]
    public void ACardOnTheShownWallWearsTheDressTheCoversAnswerIn()
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      ShellWindow? window = null;
      try
      {
        SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        HomeViewModel home = new HomeViewModel();
        home.WeatherWallReader = delegate (CancellationToken cancellation)
        {
          return Task.FromResult<WeatherWallBrief?>(TodayBrief());
        };
        MainMenuEntry entry = TileOf(home, WEATHER_ACTION);
        home.ShowContentAsync(entry).GetAwaiter().GetResult();

        window = new ShellWindow(new HomeView(home, new WorkflowManager(),
            sectionScreen: delegate (HomeSection section) { return null!; }));
        window.Show();
        Pump();

        ScrollViewer content = window.GetVisualDescendants().OfType<ScrollViewer>()
            .Single(viewer => viewer.Name == "nereusContentPane");
        List<Button> cards = content.GetVisualDescendants().OfType<Button>()
            .Where(button => button.Classes.Contains("nereus-card-tile") && OnScreen(button))
            .ToList();

        // Today, the hours chart, three days, and the air - six cards on the shown wall.
        Assert.That(cards, Has.Count.EqualTo(6),
            "the weather wall did not put up its cards on the shown screen; groups: "
            + string.Join(" | ", home.ContentGroups.Select(group =>
                group.Heading + "/" + group.Layout + "=" + group.Tiles.Count)));

        Assert.That(cards.Any(button => button.Classes.Contains("nereus-plugin-tile")), Is.False,
            "a card wears the plaque's class, so a screen with weather on it would count the sky as a plugin");

        foreach (Button card in cards)
        {
          HomeContentTile tile = (HomeContentTile) card.Tag!;
          Assert.That(card.Focusable && tile.IsFocusable, Is.True,
              "the reference's cards could be stood on; this one steps past the arrow");
          Assert.That(card.GetVisualDescendants().OfType<Border>()
              .Any(edge => edge.Classes.Contains("home-tile-edge")), Is.True,
              "a card does not wear the edge every other tile on the wall lights when a viewer stands on it");
          if (tile.Card!.IsChart)
            continue; // the chart speaks through its band's heading; it wears no name of its own
          // Every face is written into every card and only one of them is visible; among their title
          // TextBlocks the standing one is the one whose face is on, and all of them read the same name.
          Assert.That(card.GetVisualDescendants().OfType<TextBlock>()
              .Where(line => line.Classes.Contains("home-tile-caption-title"))
              .Any(line => line.Text == tile.Card.Label), Is.True,
              "the card's name does not stand in the plain letters the design gave it");
        }
      }
      finally
      {
        window?.Close();
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out _);
      }
    }

    private static void Pump()
    {
      for (int round = 0; round < 20; round++)
        Dispatcher.UIThread.RunJobs();
    }

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestTheWeatherAndNewsWalls).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      string path = Path.GetFullPath(value!.Value!);
      Assert.That(Directory.Exists(path), Is.True, name + " points at '" + path + "', which is not a directory");
      return path;
    }

    private static bool OnScreen(Visual? control)
    {
      for (Visual? node = control; node != null; node = node.GetVisualParent())
      {
        if (!node.IsVisible)
          return false;
      }

      return true;
    }
  }
}
