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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The weather's pictures, asked by the reference's own numbers. Both shipped skins carry
  /// images/Weather/64x64 and 128x128 filled with numbered pictures, and the only honest dictionary from
  /// OpenWeatherMap's condition ids to those numbers is the weather plugin's own -
  /// <c>OpenWeatherMapCatcher._weatherCodeTranslation</c> and its night twin. The front end's copy
  /// (<see cref="WeatherIconCodes"/>) is checked against the reference table entry by entry here, so the day
  /// a future edit "improves" one number is the day this test names it.
  /// </summary>
  [TestFixture]
  public class TestWeatherIcons
  {
    /// <summary>
    /// The reference grabber's day table, spelled out again on this side. Not a second implementation - an
    /// accusation: if these two tables ever disagree, one of them stopped being a copy.
    /// </summary>
    private static readonly Dictionary<int, int> REFERENCE_DAY = new Dictionary<int, int>
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

    /// <summary>The reference grabber's night table, spelled out again on this side, same accusation.</summary>
    private static readonly Dictionary<int, int> REFERENCE_NIGHT = new Dictionary<int, int>
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

    [TearDown]
    public void PutTheDefaultBack()
    {
      Application? app = Application.Current;
      if (app != null)
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
    }

    [Test]
    public void TheDayTableIsTheReferenceTable()
    {
      Assert.That(WeatherIconCodes.DayNumbers, Is.EquivalentTo(REFERENCE_DAY),
          "the day pictures are the weather plugin's own, entry for entry");
    }

    [Test]
    public void TheNightTableIsTheReferenceTable()
    {
      Assert.That(WeatherIconCodes.NightNumbers, Is.EquivalentTo(REFERENCE_NIGHT),
          "and so are the night ones, which is why clear sky by night is the moon and not the sun");
    }

    [Test]
    public void AnUnknownCodeAnswersThePictureNobodyKnows()
    {
      // The reference returned "na.png" for a code its tables never knew, and na.png sits in every one of
      // the skin folders: the honest "we don't know" is a picture, never a hole.
      Assert.That(WeatherIconCodes.IconName(1234, false), Is.EqualTo("na"));
      Assert.That(WeatherIconCodes.IconName(1234, true), Is.EqualTo("na"));
      Assert.That(WeatherIconCodes.IconName(800, false), Is.EqualTo("32"),
          "clear sky by day is the sun");
      Assert.That(WeatherIconCodes.IconName(800, true), Is.EqualTo("31"),
          "clear sky by night is the moon");
    }

    [AvaloniaTest]
    public void BothShippedSkinsCarryEveryPictureTheTablesCanAskFor()
    {
      Application app = Application.Current!;

      HashSet<string> asked = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
      foreach (int number in REFERENCE_DAY.Values.Concat(REFERENCE_NIGHT.Values))
        asked.Add(number.ToString(CultureInfo.InvariantCulture));
      asked.Add(WeatherIconCodes.NotAvailable);

      foreach (string skin in new[] { "nereus", "wmc" })
      {
        Assert.That(SkinEngine.Apply(app, skin, out string? problem), Is.True, problem ?? "applied");
        WornSkin worn = SkinEngine.Worn(app)!;
        foreach (string name in asked)
        {
          Assert.That(worn.Images.Has("Weather/64x64/" + name), Is.True,
              skin + " owes the forecast rows a Weather/64x64/" + name);
          Assert.That(worn.Images.Has("Weather/128x128/" + name), Is.True,
              skin + " owes the current-weather panel a Weather/128x128/" + name);
        }
      }
    }

    [Test]
    public void TheRowsCarryTheirPictureAndTolerateItsAbsence()
    {
      WeatherDayRow withPicture = new WeatherDayRow(
          new WeatherDay(System.DateTimeOffset.Now, "sunny", 6.0, 1.0, 0.1, "32"), true,
          CultureInfo.InvariantCulture);
      Assert.That(withPicture.Icon, Is.EqualTo("32"), "the day's midday picture travels with the row");

      WeatherDayRow without = new WeatherDayRow(
          new WeatherDay(System.DateTimeOffset.Now, "sunny", 6.0, 1.0, 0.1), true,
          CultureInfo.InvariantCulture);
      Assert.That(without.Icon, Is.Null,
          "a day the response gave no code for simply has no picture, and the row says so");
    }
  }
}
