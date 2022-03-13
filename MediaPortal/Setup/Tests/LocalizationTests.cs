#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using MediaPortal.Common.Logging;
using MP2BootstrapperApp.Localization;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Tests
{
  public class LocalizationTests
  {
    public static IEnumerable<object[]> Should_TranslateLocalizedStringLabel_Data
    {
      get
      {
        // Default english
        yield return new object[] { new CultureInfo("en-GB"), "[LocalizationTests.Test]", "English" };
        // German (Germany)
        yield return new object[] { new CultureInfo("de-DE"), "[LocalizationTests.Test]", "German" };
        // Missing translation in German (Germany) should fallback to translation in regionless German
        yield return new object[] { new CultureInfo("de-DE"), "[LocalizationTests.TestFallback]", "German fallback" };
        // Missing translation in German should fallback to translation in English
        yield return new object[] { new CultureInfo("de-DE"), "[LocalizationTests.TestEnglishFallback]", "Default english fallback" };
      }
    }

    [Theory]
    [MemberData(nameof(Should_TranslateLocalizedStringLabel_Data))]
    void Should_TranslateLocalizedStringLabel(CultureInfo culture, string label, string expected)
    {
      StringManager stringManager = new StringManager(new NoLogger());
      stringManager.Startup();
      stringManager.AddLanguageAssembly(typeof(LocalizationTests).Assembly);
      stringManager.ChangeLanguage(culture);

      string localized = stringManager.ToString(label);

      Assert.Equal(expected, localized);
    }
  }
}
