#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Globalization;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// How the head line spells its clock and its day, which is the worn skin's answer and not the front end's:
  /// the reference kept both formats in its skin base settings, wrote them from a line in its configuration
  /// program, and told the running header to spell itself again the moment one was saved.
  /// </summary>
  [TestFixture]
  public class TestChromeFormats
  {
    [Test]
    public void TheHeadLineWritesItselfTheWayTheSkinWasWritten()
    {
      ChromeInfo chrome = new ChromeInfo();
      chrome.WearFormats("HH:mm", "yyyy-MM-dd");

      Assert.That(chrome.Clock, Does.Match("^[0-9]{2}:[0-9]{2}$"),
          "a skin that asked for twenty-four hours gets them, this minute and not the one it started on");
      Assert.That(chrome.Date, Does.Match("^[0-9]{4}-[0-9]{2}-[0-9]{2}$"),
          "and the day follows the same sentence");

      chrome.WearFormats("h:mm", "d");
      Assert.That(chrome.Clock, Does.Match("^[0-9]{1,2}:[0-9]{2}$"),
          "the head line is said again the moment the format is saved, as the reference's shout across the "
          + "skin did - a viewer who has just typed a format is standing in front of the old one");
    }

    [Test]
    public void AChromeWearingNoSkinOfItsOwnSpellsTheReferencesAnswers()
    {
      ChromeInfo chrome = new ChromeInfo();
      CultureInfo culture = ClientSettings.UiCulture();

      Assert.That(DateTime.TryParseExact(chrome.Clock, SkinSettingsBase.DEFAULT_TIME_FORMAT, culture,
              DateTimeStyles.None, out DateTime _),
          Is.True, "the clock is the short time of the language the interface reads in, which is what both "
              + "reference skins shipped and what \"t\" means in this culture");
      Assert.That(chrome.Date, Is.EqualTo(DateTime.Now.ToString(SkinSettingsBase.DEFAULT_DATE_FORMAT, culture)),
          "and the day is its long date");
    }

    [Test]
    public void AnUnusableFormatLeavesTheClockWriting()
    {
      ChromeInfo chrome = new ChromeInfo();
      chrome.WearFormats("HH:mm", "yyyy-MM-dd");

      // A format somebody typed that the calendar cannot read - and a box one has only begun to type into is
      // the same case - may stop the head line from changing, never stop it from speaking.
      chrome.WearFormats("%", "");

      CultureInfo culture = ClientSettings.UiCulture();
      Assert.That(DateTime.TryParseExact(chrome.Clock, SkinSettingsBase.DEFAULT_TIME_FORMAT, culture,
              DateTimeStyles.None, out DateTime _),
          Is.True, "the shipped short time took the unusable one's place");
      Assert.That(chrome.Date, Is.EqualTo(DateTime.Now.ToString(SkinSettingsBase.DEFAULT_DATE_FORMAT, culture)),
          "and the same for the day, whose box was left empty");
    }
  }
}
