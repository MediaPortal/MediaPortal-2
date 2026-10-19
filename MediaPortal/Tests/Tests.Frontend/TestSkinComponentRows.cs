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

using Avalonia;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;

namespace Tests.Frontend
{
  /// <summary>
  /// The component screens in the worn skin's own reading. The news, the weather and the party asked the
  /// palette and nothing else; now they name a row template the way the home menu and the settings hub do,
  /// and both shipped skins answer - Nereus a quiet sentence under each word, WMC the whole width with the
  /// state on the band's edge. The weather's pictures have their own test now
  /// (<see cref="TestWeatherIcons"/>): the reference grabber's translation tables live in this tree and said
  /// which picture belongs to which OpenWeatherMap answer all along.
  /// </summary>
  [TestFixture]
  public class TestSkinComponentRows
  {
    private static readonly string[] ROW_KEYS = { "NewsHeadlineTemplate", "WeatherDayTemplate", "PartyQueueTemplate" };

    [TearDown]
    public void PutTheDefaultBack()
    {
      Application? app = Application.Current;
      if (app != null)
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
    }

    private static object? Find(Application app, string key)
    {
      return app.TryFindResource(key, out object? value) ? value : null;
    }

    [AvaloniaTest]
    public void BothShippedSkinsAnswerTheComponentRows()
    {
      Application app = Application.Current!;

      Assert.That(SkinEngine.Apply(app, "nereus", out string? nereusProblem), Is.True, nereusProblem ?? "applied");
      foreach (string key in ROW_KEYS)
        Assert.That(Find(app, key),
            Is.InstanceOf<Avalonia.Controls.Templates.IDataTemplate>(),
            "Nereus draws " + key + " in its own reading");

      Assert.That(SkinEngine.Apply(app, "wmc", out string? wmcProblem), Is.True, wmcProblem ?? "applied");
      foreach (string key in ROW_KEYS)
        Assert.That(Find(app, key),
            Is.InstanceOf<Avalonia.Controls.Templates.IDataTemplate>(),
            "and WMC wears its own answer to the same key, the band rather than the row");

      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
      foreach (string key in ROW_KEYS)
        Assert.That(Find(app, key), Is.Null,
            "the default theme keeps silent about " + key + ", and the screen draws what it was written with");
    }
  }
}
