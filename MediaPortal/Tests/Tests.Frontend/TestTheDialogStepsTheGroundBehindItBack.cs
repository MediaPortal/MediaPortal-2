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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;

namespace Tests.Frontend
{
  /// <summary>
  /// What a dialog does to the picture behind it, as far as the skin has a say: the ground is thrown out of focus
  /// by the shell and washed dark and lit around the panel by the face, and both of those are things a viewer
  /// judges by eye and a test can hold to without arguing about taste - how dark is the skin's answer, but a
  /// dialog that washes nothing, or a panel with no halo over a darkened home, is a bug and not a style.
  /// </summary>
  [TestFixture]
  public class TestTheDialogStepsTheGroundBehindItBack
  {
    private string _skinsFolderBefore = "";

    [SetUp]
    public void RememberTheSkinsFolder()
    {
      _skinsFolderBefore = SkinEngine.SkinsFolder;
    }

    [TearDown]
    public void PutTheSkinsFolderBack()
    {
      Application? app = Application.Current;
      if (app != null)
      {
        SkinEngine.Clear(app);
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
      }

      SkinEngine.SkinsFolder = _skinsFolderBefore;
    }

    private static void Pump()
    {
      for (int round = 0; round < 20; round++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(5);
      }
    }

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestTheDialogStepsTheGroundBehindItBack).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      string path = Path.GetFullPath(value!.Value!);
      Assert.That(Directory.Exists(path), Is.True, name + " points at '" + path + "', which is not a directory");
      return path;
    }

    private static void WearTheShippedSkin()
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      FrontendTheme.Apply(app, "nereus", out _);
      Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
    }

    private static WornSkin TheWornSkin()
    {
      WornSkin? worn = SkinEngine.Worn(Application.Current!);
      Assert.That(worn, Is.Not.Null, "a face has to be on for its dialogs to be looked at");
      return worn!;
    }

    /// <summary>
    /// A dialog washes the ground it stands over darker than the scrim the notification layers wear: half black,
    /// which is what the reference wrote for a home it merely dimmed, leaves a blurred home lighting the room the
    /// panel stands in, and a panel that is not the brightest thing on the television does not read as lit.
    /// </summary>
    [AvaloniaTest]
    public void AGroundBehindAQuestionIsWashedDark()
    {
      WearTheShippedSkin();

      UserControl screen = new UserControl { Classes = { "nereus-dialog-screen" }, Width = 400, Height = 300 };
      Window window = new Window { Content = screen, Width = 800, Height = 600 };
      window.Show();
      Pump();

      ISolidColorBrush? wash = screen.Background as ISolidColorBrush;
      Assert.That(wash, Is.Not.Null, "the ground a dialog is asked over is washed with one colour");

      Assert.That(wash!.Color.R, Is.EqualTo(0), "and the wash is black, not a colour of its own");
      Assert.That(wash.Color.G, Is.EqualTo(0));
      Assert.That(wash.Color.B, Is.EqualTo(0));

      ISolidColorBrush? scrim = TheWornSkin().Resources["ScrimBrush"] as ISolidColorBrush;
      Assert.That(scrim, Is.Not.Null, "the plain scrim the notification layers wear is in the palette");
      Assert.That(wash.Color.A, Is.GreaterThan(scrim!.Color.A),
          "a dialog's wash is darker than the scrim over a sharp screen, and the shipped face carries "
          + wash.Color.A + " against " + scrim.Color.A);
      Assert.That(wash.Color.A, Is.GreaterThanOrEqualTo((byte) 0x99),
          "and dark enough for the eye to notice without the home disappearing: " + wash.Color.A);

      window.Close();
    }

    /// <summary>
    /// Both dialog frames - the one with a heading and the one without - carry a glow of their own, because over
    /// a ground the shell has thrown out of focus and washed dark, a panel with no halo is a sticker. How far and
    /// how strongly a face lets that halo fall is its own business and is not held here; what is held is that what
    /// it wears is a glow: light falling off from outside the panel, so nothing spread - a spread layer is padded
    /// out to the panel's own shape before it falls off, which is a mount rather than a halo - and nothing inward,
    /// with at least one layer that is bright enough and soft enough to be seen.
    /// </summary>
    [AvaloniaTest]
    public void BothDialogFramesCarryAGlow()
    {
      WearTheShippedSkin();
      WornSkin worn = TheWornSkin();

      foreach (string key in new[] { "DialogFrameTemplate", "DialogHeadedFrameTemplate" })
      {
        IDataTemplate? frame = worn.Resources[key] as IDataTemplate;
        Assert.That(frame, Is.Not.Null, "the skin carries the dialog frame '" + key + "'");

        Control built = frame!.Build(new object());
        Border? panel = (built as Border)
            ?? built.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.BoxShadow != null);

        Assert.That(panel, Is.Not.Null, "the dialog frame '" + key + "' glows, or its panel would be a sticker on"
            + " a home the shell has stepped back");

        BoxShadows shadow = panel!.BoxShadow!;
        Assert.That(shadow.Count, Is.GreaterThan(0),
            "the frame '" + key + "' carries a shadow at all: it wears '" + shadow + "'");

        double widest = 0;
        int brightest = 0;
        foreach (BoxShadow layer in shadow)
        {
          Assert.That(layer.Spread, Is.EqualTo(0),
              "nothing in a glow spreads - a spread layer is padded out to the panel's own shape before it falls"
              + " off, which is a white mount around the panel rather than light falling off from it: '"
              + shadow + "'");
          Assert.That(layer.IsInset, Is.False,
              "and nothing falls inward, which would eat into the panel's own edge instead of lighting the ground"
              + " behind it: '" + shadow + "'");

          // A layer the face left fully transparent paints nothing and is not the glow's evidence either way.
          if (layer.Color.A == 0)
            continue;

          brightest = Math.Max(brightest, layer.Color.A);
          widest = Math.Max(widest, layer.Blur);
        }

        Assert.That(brightest, Is.GreaterThanOrEqualTo(0x40),
            "a visible layer is bright enough over a ground that is washed dark for the panel to read as lit: '"
            + shadow + "'");
        Assert.That(widest, Is.GreaterThanOrEqualTo(10),
            "and a visible layer falls off over a distance - a layer blurred by nothing is a hard copy of the"
            + " panel behind it, which is a border and not a glow: '" + shadow + "'");
      }
    }
  }
}