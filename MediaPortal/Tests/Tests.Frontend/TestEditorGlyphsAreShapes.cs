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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;
using GlyphShape = Avalonia.Controls.Shapes.Path;

namespace Tests.Frontend
{
  /// <summary>
  /// The five faces of the home-menu editor: shapes the worn skin carries, not pictures of them. A shape takes
  /// the screen's white and the button's state, which is the whole reason the shortcut glyphs were moved off
  /// bitmaps years ago and the reason the editor's buttons were moved off them too - a photograph of a glyph
  /// cannot be lit, dimmed or greyed, so an editor whose buttons are photographs has no way of saying which
  /// button a viewer is standing on.
  /// </summary>
  [TestFixture]
  public class TestEditorGlyphsAreShapes
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

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestEditorGlyphsAreShapes).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      string path = Path.GetFullPath(value!.Value!);
      Assert.That(Directory.Exists(path), Is.True, name + " points at '" + path + "', which is not a directory");
      return path;
    }

    /// <summary>The skin this front end ships, read from the source folder rather than from a build's copy.</summary>
    private static WornSkin WearTheShippedSkin()
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");

      // The palette first and the skin over it, because that is how a real window is dressed: the glyph asks
      // for the screen's white by name, and that name is answered by the palette the skin is worn under.
      FrontendTheme.Apply(app, "nereus", out _);
      Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");

      WornSkin? worn = SkinEngine.Worn(app);
      Assert.That(worn, Is.Not.Null, "the shipped skin came off the disk and onto the application");
      return worn!;
    }

    /// <summary>
    /// Every face the editor's buttons ask for is a vector the shipped skin carries, and it reads as a shape
    /// standing inside its square rather than as a picture or as a canvas reaching outside it.
    /// </summary>
    [AvaloniaTest]
    public void TheEditorAsksForFacesTheSkinCarriesAsShapes()
    {
      WornSkin worn = WearTheShippedSkin();

      foreach (string face in new[] { "Editor/up", "Editor/down", "Editor/remove", "Editor/configure", "Editor/add" })
      {
        Assert.That(worn.Vectors.Has(face), Is.True,
            "the editor's buttons ask for '" + face + "' and the skin carries no icon of that name");

        Geometry? shape = worn.Vectors.Load(face);
        Assert.That(shape, Is.Not.Null, "'" + face + "' is on disk and did not read as a shape");

        Rect bounds = shape!.Bounds;
        Assert.That(bounds.X, Is.GreaterThanOrEqualTo(-0.5), face + " stands left of the square");
        Assert.That(bounds.Y, Is.GreaterThanOrEqualTo(-0.5), face + " stands above the square");
        Assert.That(bounds.Right, Is.LessThanOrEqualTo(SvgGeometry.GLYPH_BOX + 0.5),
            face + " reaches out of the square to the right");
        Assert.That(bounds.Bottom, Is.LessThanOrEqualTo(SvgGeometry.GLYPH_BOX + 0.5),
            face + " reaches out of the square downwards");
      }
    }

    /// <summary>
    /// A button of the editor's kind carries the shape the skin answers with, tinted rather than photographed,
    /// at half lit; it comes to full white when something stands on it and falls to a tenth when it cannot be
    /// pressed - the first tile's up arrow, the last tile's down arrow, the gear of a tile that opens no list.
    /// </summary>
    [AvaloniaTest]
    public void AGlyphButtonDrawsTheShapeAndLightsItAsTheButtonIsStoodOn()
    {
      WearTheShippedSkin();

      Button up = new Button();
      up.Classes.Add("nereus-editor-up");
      up.Classes.Add("nereus-editor-glyph");

      Window window = new Window { Content = up, Width = 400, Height = 300 };
      window.Show();
      Pump();

      GlyphShape? face = up.GetVisualDescendants().OfType<GlyphShape>().SingleOrDefault();
      Assert.That(face, Is.Not.Null, "the button the skin dresses for the editor's rows carries a face");
      Assert.That(face!.Data, Is.Not.Null,
          "and the skin answered the button's ask for its 'Editor/up' glyph with a shape - a button that binds "
          + "its face inside a style's setter is how this came to be empty once");
      Assert.That(face.Fill, Is.InstanceOf<ISolidColorBrush>(),
          "and it is tinted with the screen's white, so the screen can decide its colour");
      Assert.That(face.Opacity, Is.EqualTo(0.5).Within(0.001), "at rest it stands half lit");

      window.Focus();
      Pump();
      up.Focus();
      Pump();
      Assert.That(up.IsFocused, Is.True, "the arrangement asks for a focused button");
      Assert.That(face.Opacity, Is.EqualTo(1.0).Within(0.001), "stood on, the face comes to full white");

      up.IsEnabled = false;
      Pump();
      Assert.That(face.Opacity, Is.EqualTo(0.1).Within(0.001),
          "and a face on a button nobody can press keeps the row's shape without claiming to be pressable");

      window.Close();
    }
  }
}
