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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;

namespace Tests.Frontend
{
  /// <summary>
  /// The skin's primary button: the shortcut tile's glass with its corners at fifty per cent, and a height of its
  /// own. The height is the reason this is a test - an OK button that only knows the height of its letters is a
  /// button a crowded dialog squeezes into a sliver, which is what the front end shipped for a while: the viewer
  /// could see that something was down there and could not see what it was.
  /// </summary>
  [TestFixture]
  public class TestThePrimaryButtonIsGlassAtHalfRound
  {
    private string _skinsFolderBefore = "";

    [SetUp]
    public void RememberWhereSkinsComeFrom()
    {
      _skinsFolderBefore = SkinEngine.SkinsFolder;
    }

    [TearDown]
    public void PutTheSkinBack()
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
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Pumping the dispatcher for a stretch of real time. The veil, the frame and the word change on a transition
    /// of a quarter of a second, and a dispatcher that is only emptied once reads them before they have moved.
    /// </summary>
    private static void PumpFor(int milliseconds)
    {
      System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
      while (watch.ElapsedMilliseconds < milliseconds)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(10);
      }

      Pump();
    }

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestThePrimaryButtonIsGlassAtHalfRound).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      string path = Path.GetFullPath(value!.Value!);
      Assert.That(Directory.Exists(path), Is.True, name + " points at '" + path + "', which is not a directory");
      return path;
    }

    /// <summary>The skin this front end ships, read from the source folder rather than from a build's copy.</summary>
    private static void WearTheShippedSkin()
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      FrontendTheme.Apply(app, "nereus", out _);
      Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
    }

    /// <summary>
    /// A primary button brings its own height, and its corner is half of it: the class says "primary action"
    /// and every number that makes it one comes with it, whoever writes it and wherever they write it.
    /// </summary>
    [AvaloniaTest]
    public void APrimaryButtonStandsAtItsOwnHeightWithHalfRoundCorners()
    {
      WearTheShippedSkin();

      Button ok = new Button { Content = "OK" };
      ok.Classes.Add("nereus-primary");

      Window window = new Window { Content = ok, Width = 600, Height = 300 };
      window.Show();
      Pump();

      Assert.That(ok.Height, Is.EqualTo(56),
          "the height belongs to the button, so a dialog running out of room takes it off the list above and not "
          + "off the button the viewer has to press");
      Assert.That(ok.Bounds.Height, Is.GreaterThanOrEqualTo(50), "and the window gave it that room");
      Assert.That(ok.CornerRadius.TopLeft, Is.EqualTo(ok.Height / 2),
          "fifty per cent radius, which at one height for the whole skin is twenty-eight");

      window.Close();
    }

    /// <summary>
    /// The glass of the shortcut tiles, at rest and when stood on: a veil that lightens, a frame that comes up to
    /// full light, and a word that comes to full white with both.
    /// </summary>
    [AvaloniaTest]
    public void APrimaryButtonWearsTheTilesGlassAndLightsItAsTheTileDoes()
    {
      WearTheShippedSkin();

      Button ok = new Button { Content = "OK" };
      ok.Classes.Add("nereus-primary");

      Window window = new Window { Content = ok, Width = 600, Height = 300 };
      window.Show();
      PumpFor(600);

      Border? veil = ok.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Classes.Contains("nereus-primary-veil"));
      Border? edge = ok.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Classes.Contains("nereus-primary-edge"));
      ContentPresenter? word = ok.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault();

      Assert.That(veil, Is.Not.Null, "the button carries the tile's veil");
      Assert.That(edge, Is.Not.Null, "and the frame that catches the light");
      Assert.That(word, Is.Not.Null, "and the word standing in the middle of it");
      Assert.That(word!.Content, Is.EqualTo("OK"),
          "and what the button was given actually reached the presenter - a ContentPresenter in a template is "
          + "not handed the control's Content on its own, and one TemplateBinding too few is a glass plate with "
          + "no word on it");

      Assert.That(veil!.Background, Is.InstanceOf<LinearGradientBrush>(),
          "the veil is the tile's gradient - light along the top, weight at the bottom - and not a flat plate");
      Assert.That(veil.Opacity, Is.EqualTo(0.9).Within(0.001), "at rest it is the tile's own ninety per cent");
      Assert.That(edge!.BorderThickness.Left, Is.EqualTo(2), "the frame is two, the tile's two");
      Assert.That(word!.Opacity, Is.EqualTo(0.7).Within(0.001), "and the word stands back at seventy per cent");

      window.Focus();
      Pump();
      ok.Focus();
      PumpFor(600);
      Assert.That(ok.IsFocused, Is.True, "the arrangement asks for a focused button");

      Assert.That(veil.Opacity, Is.EqualTo(1.0).Within(0.001), "stood on, the veil lightens");
      Assert.That(word.Opacity, Is.EqualTo(1.0).Within(0.001), "and the word comes to full white with it");

      window.Close();
    }
  }
}
