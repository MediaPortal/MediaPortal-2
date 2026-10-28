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
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The application's wiring of the two information walls: the weather tile reads OpenWeatherMap and the
  /// news tile reads the feeds, exactly like the full screens of the same names do - the same settings files,
  /// the same readers, the same rules about whose key is tried first - because a home wall that disagreed with
  /// its own screen about the viewer's weather would be two weathers in one house.
  /// </summary>
  /// <remarks>
  /// This is the one place the two walls meet a real internet; the view model itself only ever holds two
  /// delegates, which is what keeps it testable without one. Every reader here may answer null - no place
  /// saved, no feed to ask, an endpoint that refused - and null simply means the tile keeps its help note,
  /// the same way a shelf the server cannot fill shows nothing.
  /// </remarks>
  public static class HomeInfoWalls
  {
    /// <summary>
    /// Hands the home view model its two internet readers and starts the quarter-of-an-hour watch, which the
    /// view model then keeps as quietly as the old news plugin kept its collector.
    /// </summary>
    public static void Attach(HomeViewModel home)
    {
      home.WeatherWallReader = ReadWeatherAsync;
      home.NewsWallReader = ReadNewsAsync;
      home.StartInfoWallWatch();
    }

    /// <summary>
    /// Today's sky for the first saved place - the first is "home", the weather screen's own word for it, and
    /// the only place a wall of a few squares can answer for without asking the viewer which one they meant.
    /// </summary>
    private static async Task<WeatherWallBrief?> ReadWeatherAsync(CancellationToken cancellation)
    {
      WeatherPlacesSettings places = WeatherPlacesSettings.Load(null, out _);
      WeatherPlaceEntry? place = places.Places.FirstOrDefault();
      if (place == null)
        return null;

      WeatherCityBookmark city = new WeatherCityBookmark(place.Name, place.Detail, place.Lat, place.Lon);
      WeatherApiClient client = new WeatherApiClient(
          userApiKey: places.ApiKey,
          cultureName: ClientSettings.Load(null, out _).Culture);
      WeatherAnswer answer = await client.ReadAsync(city, cancellation);
      if (answer.Error != null || answer.Now == null)
        return null;

      // The air is asked after the sky and apart from it: a refusal there is not a reason to lose the sky
      // beside it, and the wall knows how to stand with the air half empty. The radar picture needs no
      // asking - it is one address, and the image asks it when it hangs.
      WeatherAir? air = await client.ReadAirAsync(city, cancellation);
      return new WeatherWallBrief(
          place.Name + (place.Detail.Length > 0 ? ", " + place.Detail : ""),
          answer.Now,
          answer.Days ?? Array.Empty<WeatherDay>(),
          client.IsMetric,
          answer.Slots,
          air,
          client.RadarTileUrl(city));
    }

    /// <summary>
    /// Every feed the news screen would read - MediaPortal's list for the viewer's language plus the viewer's
    /// own - asked at once, and their headlines merged newest first: one round of the collector, the same one
    /// the full news screen runs when it is told to refresh.
    /// </summary>
    private static async Task<NewsWallBrief?> ReadNewsAsync(CancellationToken cancellation)
    {
      NewsApiClient client = new NewsApiClient();
      NewsFeedsSettings feedSettings = NewsFeedsSettings.Load(null, out _);
      IReadOnlyList<NewsFeedBookmark> bookmarks = NewsFeedsSettings.Combine(
          await client.GetDefaultFeedsAsync(ClientSettings.Load(null, out _).Culture, cancellation),
          feedSettings.ExtraBookmarks());
      if (bookmarks.Count == 0)
        return null;

      NewsFeedAnswer[] answers = await Task.WhenAll(bookmarks.Select(feed =>
          client.ReadFeedAsync(feed, cancellation)).ToArray());

      List<FeedWallRow> feeds = new List<FeedWallRow>();
      List<NewsHeadline> headlines = new List<NewsHeadline>();
      foreach (NewsFeedAnswer answer in answers)
      {
        IReadOnlyList<NewsHeadline> arrived = answer.Headlines ?? Array.Empty<NewsHeadline>();
        feeds.Add(new FeedWallRow(answer.Feed.Name, arrived.Count, answer.Error));
        headlines.AddRange(arrived);
      }

      headlines = headlines
          .OrderByDescending(headline => headline.Published ?? DateTimeOffset.MinValue)
          .ToList();
      return new NewsWallBrief(feeds, headlines);
    }
  }
}
