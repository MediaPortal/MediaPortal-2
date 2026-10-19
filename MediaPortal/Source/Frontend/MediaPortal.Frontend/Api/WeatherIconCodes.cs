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

using System.Collections.Generic;
using System.Globalization;

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// Which of the skin's weather pictures belongs to which OpenWeatherMap condition code - the reference
  /// weather plugin's own translation tables, carried over number for number.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The skin folders carry <c>images/Weather/64x64</c> and <c>images/Weather/128x128</c> filled with numbered
  /// pictures (0 to 47, plus <c>na</c>); the numbers are the old plugin's condition codes, and the only honest
  /// dictionary from OpenWeatherMap's ids to those numbers is the one the plugin itself used -
  /// <c>OpenWeatherMapCatcher._weatherCodeTranslation</c> and its night twin, copied here unchanged. A mapping
  /// invented from the pictures' look would put snow next to sun as soon as its author guessed once.
  /// </para>
  /// <para>
  /// The answer is the picture's name without its extension (<c>"4"</c>, <c>"na"</c>), because that is how
  /// <see cref="Theming.SkinImages"/> knows its pictures; a screen pastes it into a path of its own -
  /// <c>Weather/64x64/{0}</c> - and an unknown code answers <c>"na"</c> exactly as the reference's
  /// <c>GetWeatherIcon</c> did, so even the "we don't know" case is a picture and never a hole.
  /// </para>
  /// <para>
  /// Night matters because the pictures differ (a moon where day has a sun, code 31 where day answers 32) and
  /// the reference asked the question only for the current weather - it compared the clock with the city's own
  /// sunrise and sunset - while its forecast days always took the day table. Both rules are kept.
  /// </para>
  /// </remarks>
  public static class WeatherIconCodes
  {
    /// <summary>The name of the picture for a condition nobody recognised, in every size the skin carries.</summary>
    public const string NotAvailable = "na";

    /// <summary>The reference grabber's day table (OpenWeatherMap id → picture number), verbatim.</summary>
    public static readonly IReadOnlyDictionary<int, int> DayNumbers = new Dictionary<int, int>
    {
      { 200, 4 }, { 201, 4 }, { 202, 3 }, { 210, 4 }, { 211, 4 }, { 212, 3 }, { 221, 38 }, { 230, 4 },
      { 231, 4 }, { 232, 4 },
      { 300, 9 }, { 301, 9 }, { 302, 9 }, { 310, 9 }, { 311, 9 }, { 312, 9 }, { 313, 9 }, { 314, 9 },
      { 321, 9 },
      { 500, 11 }, { 501, 11 }, { 502, 11 }, { 503, 11 }, { 504, 11 }, { 511, 11 }, { 520, 11 },
      { 521, 11 }, { 522, 11 }, { 531, 40 },
      { 600, 14 }, { 601, 16 }, { 602, 41 }, { 611, 18 }, { 612, 6 }, { 615, 5 }, { 616, 5 }, { 620, 14 },
      { 621, 46 }, { 622, 43 },
      { 701, 20 }, { 711, 22 }, { 721, 21 }, { 731, 19 }, { 741, 20 }, { 751, 19 }, { 761, 19 }, { 762, 19 },
      { 771, 2 }, { 781, 0 },
      { 800, 32 }, { 801, 34 }, { 802, 30 }, { 803, 30 }, { 804, 28 },
      { 900, 0 }, { 901, 1 }, { 902, 2 }, { 903, 25 }, { 904, 36 }, { 905, 24 }, { 906, 17 },
      { 951, 33 }, { 952, 24 }, { 953, 24 }, { 954, 24 }, { 955, 24 }, { 956, 24 }, { 957, 23 }, { 958, 23 },
      { 959, 23 }, { 960, 4 }, { 961, 3 }, { 962, 2 }
    };

    /// <summary>The reference grabber's night table, verbatim; asked only for the current weather.</summary>
    public static readonly IReadOnlyDictionary<int, int> NightNumbers = new Dictionary<int, int>
    {
      { 200, 47 }, { 201, 47 }, { 202, 47 }, { 210, 47 }, { 211, 47 }, { 212, 47 }, { 221, 47 }, { 230, 47 },
      { 231, 47 }, { 232, 47 },
      { 300, 45 }, { 301, 45 }, { 302, 45 }, { 310, 45 }, { 311, 45 }, { 312, 45 }, { 313, 45 }, { 314, 45 },
      { 321, 45 },
      { 500, 45 }, { 501, 45 }, { 502, 45 }, { 503, 45 }, { 504, 45 }, { 511, 45 }, { 520, 45 },
      { 521, 45 }, { 522, 45 }, { 531, 45 },
      { 600, 46 }, { 601, 46 }, { 602, 46 }, { 611, 46 }, { 612, 46 }, { 615, 46 }, { 616, 46 }, { 620, 46 },
      { 621, 46 }, { 622, 46 },
      { 701, 29 }, { 711, 29 }, { 721, 29 }, { 731, 29 }, { 741, 29 }, { 751, 29 }, { 761, 29 }, { 762, 29 },
      { 771, 29 }, { 781, 29 },
      { 800, 31 }, { 801, 33 }, { 802, 29 }, { 803, 29 }, { 804, 27 },
      { 900, 29 }, { 901, 29 }, { 902, 27 }, { 903, 33 }, { 904, 31 }, { 905, 27 }, { 906, 45 },
      { 951, 31 }, { 952, 31 }, { 953, 33 }, { 954, 33 }, { 955, 29 }, { 956, 29 }, { 957, 29 }, { 958, 27 },
      { 959, 27 }, { 960, 27 }, { 961, 45 }, { 962, 45 }
    };

    /// <summary>
    /// The picture's name for one OpenWeatherMap condition code: the number the reference answered with, or
    /// <see cref="NotAvailable"/> when its tables never knew the code - the same "na.png" the reference fell
    /// back to.
    /// </summary>
    public static string IconName(int openWeatherMapCode, bool isNight)
    {
      IReadOnlyDictionary<int, int> numbers = isNight ? NightNumbers : DayNumbers;
      return numbers.TryGetValue(openWeatherMapCode, out int number)
          ? number.ToString(CultureInfo.InvariantCulture)
          : NotAvailable;
    }
  }
}
