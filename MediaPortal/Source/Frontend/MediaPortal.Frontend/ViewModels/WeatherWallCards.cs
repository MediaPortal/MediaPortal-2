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
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The weather wall, written once: the four bands the designed wall stands in and the cards they carry,
  /// read from OpenWeatherMap the one honest way. Both the home's weather wall (HomeViewModel) and the
  /// weather screen of its own (WeatherViewModel) put up their weather through this class, which is the
  /// whole reason it exists - a screen that dressed the same sky in its own second set of templates would
  /// be two weathers in one house, and the two would drift apart the week one of them was mended.
  /// </summary>
  /// <remarks>
  /// The band measures are the designed wall's own, and they are kept here rather than in either screen:
  /// today's card is as narrow as its content allows (368), the hours chart takes the room its own columns
  /// need (980), the forecast is two columns of two uprights (410), and air and radar share one card (460).
  /// </remarks>
  public static class WeatherWallCards
  {
    /// <summary>
    /// Puts the four bands of the weather wall onto <paramref name="wall"/>, in the order the design stands
    /// them, and answers whether any of them came to stand at all. A half the endpoint did not answer leaves
    /// its band away - the wall knows how to stand with the chart or the air missing - but a wall with no
    /// band at all is answered with false, which is what leaves the tile its help note.
    /// </summary>
    public static bool BuildBands(IList<HomeContentGroup> wall, WeatherWallBrief brief)
    {
      bool any = false;
      if (brief.Now != null)
      {
        // Today's weather takes a band of its own - one narrow card, the temperature the biggest number on
        // it, the sky's picture under it, the place under that, and the rest of the sky written as a fact
        // list. As designed: narrow as its content allows.
        HomeContentGroup today = new HomeContentGroup("Current weather", "card1", 368, 1);
        today.AddCard(TodayCard(brief));
        wall.Add(today);
        any = true;
      }

      IReadOnlyList<WeatherSlot> slots = brief.Slots ?? Array.Empty<WeatherSlot>();
      if (slots.Count >= 2)
      {
        // The hours of the next two days as one chart: temperature in red and rain chance in light blue
        // above, the millimetres as bars below. A line needs two hours at least; one alone draws nothing.
        HomeContentGroup chart = new HomeContentGroup("Today", "cardchart", 980, 1);
        chart.AddCard(new HomeInfoCard("Today", kind: "chart", slots: slots));
        wall.Add(chart);
        any = true;
      }

      IReadOnlyList<WeatherDay> days = brief.Days ?? Array.Empty<WeatherDay>();
      if (days.Count > 0)
      {
        // The days as designed: two columns of two, each day an upright of its own - name, picture between
        // name and numbers, chance and millimetres each in their own line. A fifth day has no place, the
        // same way a twentieth leftover plugin has no square in "Others".
        int places = Math.Min(4, days.Count);
        HomeContentGroup forecast = new HomeContentGroup("Forecast", "cardday4", 410, places);
        for (int i = 0; i < places; i++)
          forecast.AddCard(DayCard(days[i], brief.Metric));
        wall.Add(forecast);
        any = true;
      }

      if (brief.Air != null || brief.RadarUrl != null)
      {
        // Air and radar share one card as designed: the rain picture above, the air the place breathes
        // under it. Either half alone still stands - the radar answers without an air reading and the
        // numbers without a picture.
        HomeContentGroup air = new HomeContentGroup("Air & radar", "card1", 460, 1);
        air.AddCard(AirCard(brief));
        wall.Add(air);
        any = true;
      }

      return any;
    }

    /// <summary>
    /// Reads one place's whole sky: the moment, the hours, the days - and after them, apart and allowed to
    /// fail on its own, the air. The brief is what both walls of this house are built from; the radar picture
    /// needs no asking, being one address the image asks when it hangs.
    /// </summary>
    public static async Task<WeatherWallBrief?> ReadBriefAsync(WeatherApiClient client,
        WeatherPlaceEntry place, CancellationToken cancellation)
    {
      WeatherCityBookmark city = new WeatherCityBookmark(place.Name, place.Detail, place.Lat, place.Lon);
      WeatherAnswer answer = await client.ReadAsync(city, cancellation);
      if (answer.Error != null || answer.Now == null)
        return null;

      // The air is asked after the sky and apart from it: a refusal there is not a reason to lose the sky
      // beside it, and the wall knows how to stand with the air half empty.
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
    /// Today's card as the design redrew it: the temperature biggest, the sky's own word under it, the
    /// picture centred beneath that, the place under the picture, and everything else the endpoint answers
    /// for the moment written as a fact list - feels-like, damp, pressure, wind (with its gusts when it has
    /// any), the sun's two times, sight, clouds, and how much has fallen. The air rides as a small
    /// pill in the corner when it came in at all.
    /// </summary>
    public static HomeInfoCard TodayCard(WeatherWallBrief brief)
    {
      WeatherNow now = brief.Now!;
      NumberFormatInfo numbers = CultureInfo.CurrentCulture.NumberFormat;
      string degree = brief.Metric ? "C" : "F";

      List<CardFact> facts = new List<CardFact>();
      facts.Add(new CardFact("feels like", now.FeelsLike.ToString("0", numbers) + "°" + degree));
      facts.Add(new CardFact("humidity", now.Humidity + " %"));
      if (now.Pressure > 0)
        facts.Add(new CardFact("pressure", now.Pressure + " hPa"));
      string wind = now.WindDirection + " " + now.WindSpeed.ToString("0.0", numbers)
          + (brief.Metric ? " m/s" : " mph");
      if (now.WindGust > 0)
        wind += " (gust " + now.WindGust.ToString("0", numbers) + ")";
      facts.Add(new CardFact("wind", wind));
      if (!string.IsNullOrEmpty(now.Sunrise) || !string.IsNullOrEmpty(now.Sunset))
        facts.Add(new CardFact("sun", "\u2191 " + now.Sunrise + "  \u2193 " + now.Sunset));
      if (now.VisibilityMeters > 0)
        facts.Add(new CardFact("visibility", (brief.Metric
            ? now.VisibilityMeters / 1000.0
            : now.VisibilityMeters / 1609.34).ToString("0.#", numbers) + (brief.Metric ? " km" : " mi")));
      facts.Add(new CardFact("clouds", now.CloudsPercent + " %"));
      if (brief.Slots != null && brief.Slots.Count > 0)
        facts.Add(new CardFact("rain 3 h", brief.Slots[0].PrecipMm.ToString("0.#", numbers) + " mm"));

      return new HomeInfoCard(brief.Place,
          bigText: now.Temp.ToString("0", numbers) + "°" + degree,
          sub: WeatherApiClient.SkyWord(now),
          iconName: now.Icon == null ? null : "Weather/128x128/" + now.Icon,
          kind: "today",
          facts: facts,
          air: brief.Air);
    }

    /// <summary>
    /// One day of the forecast as the design redrew its upright: the day's name, its high and low in one
    /// line, and precipitation in two of its own - how likely and how much, the millimetres being what the
    /// free endpoint has answered since the three-hourly entries carry them.
    /// </summary>
    public static HomeInfoCard DayCard(WeatherDay day, bool metric)
    {
      NumberFormatInfo numbers = CultureInfo.CurrentCulture.NumberFormat;
      string degree = metric ? "C" : "F";
      return new HomeInfoCard(
          // The weekday in the same English the whole card speaks - the wall's words are English by design,
          // and "Mittwoch" among "feels like" and "rain chance" reads like two skins on one wall.
          day.Date.ToString("dddd", CultureInfo.InvariantCulture.DateTimeFormat),
          detail: day.High.ToString("0", numbers) + "°" + degree + " / "
              + day.Low.ToString("0", numbers) + "°" + degree,
          // Chance and millimetres in two properties of their own, so the card can draw a hairline between
          // the temperature, the chance, and the millimetres - three readings, three rows, two lines.
          sub: Math.Round(day.PrecipitationChance * 100).ToString("0", numbers) + " %",
          sub2: day.PrecipitationMm.ToString("0.#", numbers) + " mm",
          iconName: day.Icon == null ? null : "Weather/128x128/" + day.Icon,
          kind: "day");
    }

    /// <summary>
    /// The air and the radar on one card: the rain picture of the region above, the index as a huge figure
    /// in its own colour under it, and the four named pollutants as a fact list. Whichever half did not
    /// arrive simply leaves its room empty.
    /// </summary>
    public static HomeInfoCard AirCard(WeatherWallBrief brief)
    {
      List<CardFact> facts = new List<CardFact>();
      WeatherAir? air = brief.Air;
      if (air != null)
      {
        NumberFormatInfo numbers = CultureInfo.CurrentCulture.NumberFormat;
        facts.Add(new CardFact("PM2.5", air.Pm25.ToString("0.#", numbers) + " \u00B5g/m\u00B3"));
        facts.Add(new CardFact("PM10", air.Pm10.ToString("0.#", numbers) + " \u00B5g/m\u00B3"));
        facts.Add(new CardFact("NO\u2082", air.No2.ToString("0.#", numbers) + " \u00B5g/m\u00B3"));
        facts.Add(new CardFact("O\u2083", air.O3.ToString("0.#", numbers) + " \u00B5g/m\u00B3"));
      }

      return new HomeInfoCard("Air quality",
          kind: "air",
          facts: facts,
          air: air,
          radarUrl: brief.RadarUrl);
    }
  }
}
