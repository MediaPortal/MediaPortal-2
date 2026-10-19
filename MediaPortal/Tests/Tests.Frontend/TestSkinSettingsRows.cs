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
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;

namespace Tests.Frontend
{
  /// <summary>
  /// The settings list in the worn skin's own reading. The reference drew its configuration menu from the
  /// skin - Nereus quiet rows, WMC full-width bands - and the hub here asks the skin for that sentence the
  /// same way the home menu does; these tests hold both skins to answering.
  /// </summary>
  [TestFixture]
  public class TestSkinSettingsRows
  {
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
    public void BothShippedSkinsAnswerTheSettingsRow()
    {
      Application app = Application.Current!;

      Assert.That(SkinEngine.Apply(app, "nereus", out string? nereusProblem), Is.True, nereusProblem ?? "applied");
      Assert.That(Find(app, "SettingsRowTemplate"),
          Is.InstanceOf<Avalonia.Controls.Templates.IDataTemplate>(),
          "Nereus draws its settings rows the way its configuration menu drew them");

      Assert.That(SkinEngine.Apply(app, "wmc", out string? wmcProblem), Is.True, wmcProblem ?? "applied");
      Assert.That(Find(app, "SettingsRowTemplate"),
          Is.InstanceOf<Avalonia.Controls.Templates.IDataTemplate>(),
          "and WMC wears its own answer to the same key, the band rather than the row");

      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
      Assert.That(Find(app, "SettingsRowTemplate"), Is.Null,
          "the default theme keeps silent about the settings rows, and the hub draws what it was written with");
    }
  }
}
