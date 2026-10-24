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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The skin's icons as vectors: the SVG path grammar read to the letter, the box a file declares read as the
  /// measure it is, and the worn skin asked for a shape the way it is asked for a picture.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The grammar is watched command by command rather than by the picture it makes, for two reasons. The headless
  /// toolkit the tests run on answers a geometry with a stub, so the drawing a test could compare against is not
  /// available to it; and a glyph can have perfect bounds and still be drawn inside out, in the wrong order, or
  /// with a smooth curve that stopped being smooth three commands back. What the walker dictates - here is a
  /// figure, here is a line to there, here is a cubic with these two controls - is the whole claim the reader
  /// makes, and a recording sink hears it.
  /// </para>
  /// <para>
  /// The equivalence tests are the shape this file is written in: a spelling that only a browser's parser is
  /// expected to survive is compared against the plain absolute writing that means the same drawing. If the
  /// reader understood the shorthand, the two recordings are letter for letter identical.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestSvgGeometry
  {
    /// <summary>Relative letters land on the same absolute points as the letters they stand for.</summary>
    [Test]
    public void RelativeCommandsLandWhereTheAbsoluteOnesDo()
    {
      Assert.That(Walked("m10 20l30 40"), Is.EqualTo(Walked("M10 20L40 60")));
      Assert.That(Walked("m10 20l30 40"), Is.EqualTo(new[] { "M 10 20", "L 40 60" }));

      // A relative command before anything has been drawn moves from the origin, which is where the pen starts.
      Assert.That(Walked("m5 5"), Is.EqualTo(new[] { "M 5 5" }));
    }

    /// <summary>The horizontal and vertical shorthand draws the lines it abbreviates, and nothing else.</summary>
    [Test]
    public void HorizontalAndVerticalShorthandDrawsTheSameLines()
    {
      Assert.That(Walked("M2 3h5v4"), Is.EqualTo(Walked("M2 3L7 3L7 7")));
      Assert.That(Walked("M2 3H7V7"), Is.EqualTo(Walked("M2 3L7 3L7 7")));
      Assert.That(Walked("M2 3h5v4"), Is.EqualTo(new[] { "M 2 3", "L 7 3", "L 7 7" }));
    }

    /// <summary>
    /// Parameters written without their letter again repeat the command - and after a move they mean lines, which
    /// is the one place where repeating means something else than the letter says.
    /// </summary>
    [Test]
    public void ParametersWrittenWithoutTheirLetterRepeatTheCommand()
    {
      Assert.That(Walked("L1 1 2 2 3 3"), Is.EqualTo(Walked("L1 1L2 2L3 3")));
      Assert.That(Walked("M1 1 2 2 3 3"), Is.EqualTo(new[] { "M 1 1", "L 2 2", "L 3 3" }));
      Assert.That(Walked("m1 1 2 2"), Is.EqualTo(new[] { "M 1 1", "L 3 3" }));
      Assert.That(Walked("M0 0h2v2z"), Is.EqualTo(new[] { "M 0 0", "L 2 0", "L 2 2", "Z" }));
    }

    /// <summary>
    /// A smooth curve names only its far half and takes its near control point from the curve before it; after
    /// anything that is not a curve it is an ordinary curve with the current point as its control.
    /// </summary>
    [Test]
    public void ASmoothCurveContinuesTheCurveBeforeIt()
    {
      Assert.That(Walked("M0 10C5 0 15 0 20 10S35 20 40 10"),
          Is.EqualTo(Walked("M0 10C5 0 15 0 20 10C25 20 35 20 40 10")));
      Assert.That(Walked("M0 10Q5 0 10 10T20 10"),
          Is.EqualTo(Walked("M0 10Q5 0 10 10Q15 20 20 10")));
      Assert.That(Walked("M0 0L10 0S20 10 30 0"),
          Is.EqualTo(Walked("M0 0L10 0C10 0 20 10 30 0")));

      // Smooth through a smooth: the second S reflects the first S's far control, not the far control before it.
      Assert.That(Walked("M0 10C5 0 15 0 20 10S35 20 40 10S55 0 60 10"),
          Is.EqualTo(Walked("M0 10C5 0 15 0 20 10C25 20 35 20 40 10C45 0 55 0 60 10")));
    }

    /// <summary>
    /// The numbers an exporter presses together are read as the numbers they are: against a minus sign, against a
    /// second decimal point, and with an exponent that may carry its own sign.
    /// </summary>
    [Test]
    public void NumbersPressedTogetherAreReadAsSeparateNumbers()
    {
      Assert.That(Walked("M100-50l50 50"), Is.EqualTo(new[] { "M 100 -50", "L 150 0" }));
      Assert.That(Walked("M0.5.5L1.5.5"), Is.EqualTo(new[] { "M 0.5 0.5", "L 1.5 0.5" }));
      Assert.That(Walked("M1,2L3,4"), Is.EqualTo(new[] { "M 1 2", "L 3 4" }));
      Assert.That(Walked("M1e2 0L2E-1 0"), Is.EqualTo(new[] { "M 100 0", "L 0.2 0" }));
      Assert.That(Walked("M.5-.5"), Is.EqualTo(new[] { "M 0.5 -0.5" }));
    }

    /// <summary>
    /// An arc's two flags are single digits and may be written pressed together, which no number reader would
    /// survive - "01" is eleven to anything that reads it as a number.
    /// </summary>
    [Test]
    public void TwoArcFlagsPressedTogetherAreTwoFlags()
    {
      Assert.That(Walked("M0 10A10 10 0 0120 10"), Is.EqualTo(Walked("M0 10A10 10 0 0 1 20 10")));
      Assert.That(Walked("M0 10A10 10 0 0 1 20 10"),
          Is.EqualTo(new[] { "M 0 10", "A 10 10 0 0 1 20 10" }));
    }

    /// <summary>
    /// Radii too small to reach from one endpoint to the other are grown until they fit, which is the
    /// specification's answer rather than a licence to draw a different arc or none at all.
    /// </summary>
    [Test]
    public void AnArcTooSmallForItsEndpointsIsGrownUntilItFits()
    {
      // One unit of radius cannot span ten: it is grown by the factor the endpoints demand, both radii alike.
      Assert.That(Walked("M0 0A1 1 0 0 1 10 0"), Is.EqualTo(new[] { "M 0 0", "A 5 5 0 0 1 10 0" }));
      Assert.That(Walked("M0 0A2 1 0 0 1 10 0").Last(), Is.EqualTo("A 5 2.5 0 0 1 10 0"),
          "and a flat ellipse that cannot span the gap is grown on both axes at once");
    }

    /// <summary>
    /// An arc with no radius is the straight line its endpoints describe, and an arc back to the point it started
    /// from is nothing at all. Both are the specification's answer, and both are better than a shape nobody asked
    /// for.
    /// </summary>
    [Test]
    public void ADegenerateArcIsAnsweredAsTheSpecificationSays()
    {
      Assert.That(Walked("M0 0A0 5 0 0 1 10 0"), Is.EqualTo(new[] { "M 0 0", "L 10 0" }));
      Assert.That(Walked("M5 5A5 5 0 0 1 5 5"), Is.EqualTo(new[] { "M 5 5" }));
    }

    /// <summary>
    /// A file's own box is what the glyph is fitted by: its viewBox first, then the width and height it measures
    /// itself with, then - for the file that declares nothing - the extent of what it actually drew.
    /// </summary>
    [Test]
    public void TheBoxAFileDeclaresIsWhatTheGlyphIsFittedBy()
    {
      // The box every icon in this skin declares: a square whose y axis runs from -960 up to nothing.
      Assert.That(SvgGeometry.ViewBoxIn("<svg viewBox=\"0 -960 960 960\"/>"),
          Is.EqualTo(new Rect(0, -960, 960, 960)));
      Assert.That(SvgGeometry.ViewBoxIn("<svg viewBox='0 0 24 24'/>"), Is.EqualTo(new Rect(0, 0, 24, 24)));
      Assert.That(SvgGeometry.ViewBoxIn("<svg viewBox=\"0 0 0 10\"/>"), Is.Null,
          "a box of no width could not be fitted into anything");
      Assert.That(SvgGeometry.ViewBoxIn("<svg viewBox=\"0 0 10\"/>"), Is.Null,
          "three numbers are not a box, whatever they were meant to be");

      Assert.That(SvgGeometry.WidthAndHeightIn("<svg width=\"48px\" height=\"48px\"/>"),
          Is.EqualTo(new Rect(0, 0, 48, 48)));
      Assert.That(SvgGeometry.WidthAndHeightIn("<svg width=\"100%\" height=\"100%\"/>"), Is.Null,
          "a percentage has no box here to be a percentage of");

      // Nothing declared at all: the drawing's own extent answers, endpoints and control points as they come.
      List<SvgGeometry.SvgPath> paths = new List<SvgGeometry.SvgPath>
      {
          new SvgGeometry.SvgPath("M10 10h20v20", false)
      };
      Assert.That(SvgGeometry.BoxOf("<svg><path d=\"M10 10h20v20\"/></svg>", paths),
          Is.EqualTo(new Rect(10, 10, 20, 20)));
    }

    /// <summary>
    /// The fitting is uniform and centred, and it moves the file's whole box into the square: a glyph is never
    /// stretched, a glyph whose art stops short of its own edges keeps that margin in the middle rather than
    /// drifting into a corner, and - because a canvas is a place and not only a size - a file whose coordinates
    /// do not start at nought is carried along with its own origin instead of being drawn beside the square.
    /// </summary>
    [Test]
    public void TheFittingIsUniformAndCentred()
    {
      // The box every icon in this skin declares, and the origin that comes with it: coordinates that run from
      // -960 to nothing have to be lifted by the whole height to stand inside the square.
      SvgGeometry.Fitting(new Rect(0, -960, 960, 960), out double square, out double squareX, out double squareY);
      Assert.That(square, Is.EqualTo(SvgGeometry.GLYPH_BOX / 960).Within(1e-9));
      Assert.That(squareX, Is.EqualTo(0).Within(1e-9));
      Assert.That(squareY, Is.EqualTo(SvgGeometry.GLYPH_BOX).Within(1e-9),
          "the box's own y origin is part of the fitting, or the glyph is drawn above the tile");

      SvgGeometry.Fitting(new Rect(0, 0, 200, 100), out double wide, out double wideX, out double wideY);
      Assert.That(wide, Is.EqualTo(SvgGeometry.GLYPH_BOX / 200).Within(1e-9),
          "the longer side decides the scale, which is what keeps the glyph from being stretched");
      Assert.That(wideX, Is.EqualTo(0).Within(1e-9));
      Assert.That(wideY, Is.EqualTo((SvgGeometry.GLYPH_BOX - 100 * wide) / 2).Within(1e-9),
          "and the shorter side is what gets centred");

      // A box that starts somewhere else entirely - the fallback for a file silent about its own size measures
      // the drawing where the drawing happens to be.
      SvgGeometry.Fitting(new Rect(10, 10, 20, 20), out double shifted, out double shiftedX, out double shiftedY);
      Assert.That(shifted, Is.EqualTo(SvgGeometry.GLYPH_BOX / 20).Within(1e-9));
      Assert.That(shiftedX, Is.EqualTo(-10 * shifted).Within(1e-9),
          "so the drawing is brought back to the square rather than left out there");
      Assert.That(shiftedY, Is.EqualTo(-10 * shifted).Within(1e-9));
    }

    /// <summary>
    /// Only paths are read. An icon that draws itself with rectangles or text is not an icon the skin can tint,
    /// and the tile that asked for it is better off with bare glass and no glyph than with half a picture.
    /// </summary>
    [Test]
    public void OnlyPathsAreRead()
    {
      Assert.That(SvgGeometry.PathsIn("<svg><rect width=\"10\" height=\"10\"/></svg>"), Is.Empty);
      Assert.That(SvgGeometry.PathsIn("<svg><path/><path d=\"\"/></svg>"), Is.Empty,
          "a path that draws nothing is not an icon");
      Assert.That(SvgGeometry.PathsIn("<svg><path d='M0 0h1'/><path d=\"M2 2h1\"/></svg>"), Has.Count.EqualTo(2),
          "single and double quotes both carry a drawing, and a file may write either");

      List<SvgGeometry.SvgPath> wound = SvgGeometry.PathsIn(
          "<svg fill-rule=\"evenodd\"><path d=\"M0 0h1\"/></svg>");
      Assert.That(wound.Single().EvenOdd, Is.True,
          "a file that asks for even-odd asks it of all of its paths");
      Assert.That(SvgGeometry.PathsIn("<svg><path d=\"M0 0h1\" fill-rule=\"evenodd\"/></svg>").Single().EvenOdd,
          Is.True, "and a single path may ask for itself");
      Assert.That(SvgGeometry.PathsIn("<svg><path d=\"M0 0h1\"/></svg>").Single().EvenOdd, Is.False,
          "which leaves non-zero - the winding these glyphs are drawn with - as the answer by default");
    }

    /// <summary>
    /// The icons the shipped skin carries: every one of them reads as a shape, the same shape comes back every
    /// time it is asked for, and a name the skin carries only as a picture stays unanswered rather than answered
    /// with that picture.
    /// </summary>
    /// <remarks>
    /// The last assertion is the arrangement the shortcuts were moved to: a screen asking for a shape and being
    /// handed a bitmap instead is how an icon set quietly stops being one, and how a glyph stops taking the colour
    /// and the state the tile is in.
    /// </remarks>
    [AvaloniaTest]
    public void TheIconsTheSkinCarriesAreShapesAndNotPictures()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      try
      {
        Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
        try
        {
          WornSkin worn = SkinEngine.Worn(app)!;
          foreach (string icon in new[]
          {
              "MediaMenu/genre", "MediaMenu/year", "MediaMenu/age", "MediaMenu/actor", "MediaMenu/search",
              "MediaMenu/artist"
          })
          {
            Assert.That(worn.Vectors.Has(icon), Is.True,
                "the shortcut model names '" + icon + "' and the worn skin carries no icon of that name");
            Assert.That(worn.Vectors.Has(icon + ".svg"), Is.True,
                "and spelling it as it stands on disk answers too, the way the picture catalog answers it");

            Geometry? shape = worn.Vectors.Load(icon);
            Assert.That(shape, Is.Not.Null, "'" + icon + "' is on disk and did not read as a shape");
            Assert.That(worn.Vectors.Load(icon), Is.SameAs(shape),
                "a glyph is parsed once and kept, not parsed again for every tile that wears it");
          }

          // The music rail's own three names, which are the ones the batch that finished the icon set added last.
          // The picture that used to be each icon is still on disk beside its vector and stays out of the way: a
          // shortcut takes a shape, because a shape is what can wear the colour and the focus state of its tile.
          foreach (string icon in new[] { "MediaMenu/track", "MediaMenu/album", "MediaMenu/audiogenre" })
          {
            Assert.That(worn.Vectors.Load(icon), Is.InstanceOf<Geometry>(),
                "'" + icon + "' is what the music rail asks for, and the shortcut model is only as good as the "
                + "drawing behind the name");
            Assert.That(worn.Images.Load(icon), Is.InstanceOf<Bitmap>(),
                "the PNG that used to be the icon is still lying beside the vector - and a shortcut that wants a "
                + "vector is still answered by the vector");
          }

          // Every mark the foot of the playback screen can draw. The screen picks one of these per button and
          // per moment, so a name missing from the skin is a button a viewer sees without a picture - and on a
          // black film, without a hint of what it does either.
          foreach (string icon in NowPlayingViewModel.FootMarkIcons)
          {
            Assert.That(worn.Vectors.Load(icon), Is.InstanceOf<Geometry>(),
                "the playback screen asks for '" + icon + "' and the worn skin carries no shape of that name");
          }

          Assert.That(worn.Vectors.Load("MediaMenu/nothing-written-here"), Is.Null,
              "and a name nobody wrote stays unanswered instead of being filled with a guess");
        }
        finally
        {
          SkinEngine.Clear(app);
        }
      }
      finally
      {
        SkinEngine.SkinsFolder = previous;
      }
    }

    /// <summary>
    /// Where the shipped glyphs land: inside the square. Reading a file whole is one thing - arriving on the tile
    /// is the other. A canvas that runs from -960 upwards was read correctly, reported correct extents and was
    /// drawn entirely above its control, which is how the shortcuts showed fragments instead of icons.
    /// </summary>
    [AvaloniaTest]
    public void TheShippedGlyphsLandInsideTheSquare()
    {
      string folder = Path.Combine(MetadataPath("FrontendSource"), "Skins", "nereus", "images", "MediaMenu");
      foreach (string icon in new[] { "genre", "year", "age", "actor", "search", "artist" })
      {
        Geometry? shape = SvgGeometry.ReadFile(Path.Combine(folder, icon + ".svg"));
        Assert.That(shape, Is.Not.Null, "'" + icon + "' is on disk and did not read as a shape");
        Rect bounds = shape!.Bounds;
        Assert.That(bounds.X, Is.GreaterThanOrEqualTo(-0.5), icon + " stands left of the square");
        Assert.That(bounds.Y, Is.GreaterThanOrEqualTo(-0.5), icon + " stands above the square");
        Assert.That(bounds.Right, Is.LessThanOrEqualTo(SvgGeometry.GLYPH_BOX + 0.5),
            icon + " reaches out of the square to the right");
        Assert.That(bounds.Bottom, Is.LessThanOrEqualTo(SvgGeometry.GLYPH_BOX + 0.5),
            icon + " reaches out of the square downwards");
      }

      // The playback marks come from the same icon family, drawn on the same canvas that runs from -960 upwards
      // and lands entirely above its control when the reader only believes it fitted. A mark drawn off its plate
      // is not a wrong picture but no picture, along the foot of a film where that is the only hint there is.
      string marks = Path.Combine(MetadataPath("FrontendSource"), "Skins", "nereus", "images", "PlayerControl");
      foreach (string key in NowPlayingViewModel.FootMarkIcons)
      {
        string icon = key.Substring(NowPlayingViewModel.ICONS.Length);
        Geometry? shape = SvgGeometry.ReadFile(Path.Combine(marks, icon + ".svg"));
        Assert.That(shape, Is.Not.Null, "'" + icon + "' is on disk and did not read as a shape");
        Rect bounds = shape!.Bounds;
        Assert.That(bounds.X, Is.GreaterThanOrEqualTo(-0.5), icon + " stands left of the square");
        Assert.That(bounds.Y, Is.GreaterThanOrEqualTo(-0.5), icon + " stands above the square");
        Assert.That(bounds.Right, Is.LessThanOrEqualTo(SvgGeometry.GLYPH_BOX + 0.5),
            icon + " reaches out of the square to the right");
        Assert.That(bounds.Bottom, Is.LessThanOrEqualTo(SvgGeometry.GLYPH_BOX + 0.5),
            icon + " reaches out of the square downwards");
      }
    }

    /// <summary>
    /// Where the shipped glyphs stand: the box a control sizes a glyph by is a square standing on the middle of
    /// what the file drew, so that a mark sits in the middle of its plaque rather than in the top of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A control that stretches a shape scales it by the widest side of the shape's box and lays that box into
    /// the space it was given from the top left - there is no centring in the toolkit's scale at all. A mark
    /// drawn across a canvas has a box wider than it is tall, so all of the room a plaque had to spare gathered
    /// beneath the mark: the two skip marks of the playback foot rode seven pixels above the middle of their
    /// plaque, and every bound the files reported read as perfectly centred while they did it.
    /// </para>
    /// <para>
    /// Watched over every icon the skin carries rather than over the two that were complained about, because the
    /// defect shows on a triangle and hides on a square - and watched for its size too, since a box square enough
    /// to stand straight is one stride away from a glyph drawn smaller than the file sized it.
    /// </para>
    /// </remarks>
    [AvaloniaTest]
    public void TheShippedGlyphsStandInTheMiddleOfTheBoxTheyAreFittedBy()
    {
      string images = Path.Combine(MetadataPath("FrontendSource"), "Skins", "nereus", "images");
      List<string> files = new List<string>(Directory.GetFiles(Path.Combine(images, "MediaMenu"), "*.svg"));
      files.AddRange(Directory.GetFiles(Path.Combine(images, "PlayerControl"), "*.svg"));
      Assert.That(files.Count, Is.GreaterThan(20), "the skin carries icons in both folders");

      foreach (string file in files)
      {
        string name = Path.GetFileName(file);
        Geometry? shape = SvgGeometry.ReadFile(file);
        Assert.That(shape, Is.Not.Null, name + " is on disk and did not read as a shape");

        string text = File.ReadAllText(file);
        IReadOnlyList<SvgGeometry.SvgPath> paths = SvgGeometry.PathsIn(text);
        SvgGeometry.Fitting(SvgGeometry.BoxOf(text, paths), out double scale, out double offsetX,
            out double offsetY);
        SvgGeometry.Extent painted = SvgGeometry.PaintedBoxOf(paths, scale, offsetX, offsetY);
        if (painted.Width <= 0 || painted.Height <= 0)
          continue;

        Rect bounds = shape!.Bounds;
        double middleX = bounds.X + bounds.Width / 2;
        double middleY = bounds.Y + bounds.Height / 2;

        // Three tenths of the square. On a plaque seventy pixels wide that is less than a pixel of screen, and
        // what stands against it here was eleven.
        Assert.That(Math.Abs(middleX - painted.MidX), Is.LessThan(0.3), name + " hangs left or right of its ink");
        Assert.That(Math.Abs(middleY - painted.MidY), Is.LessThan(0.3), name + " hangs above or below its ink");
        Assert.That(bounds.Width, Is.EqualTo(bounds.Height).Within(0.05),
            name + " is fitted by a box that is not square, so the room it does not fill gathers at its bottom");

        // And the glyph is not resized to buy that centring: the box it is fitted by keeps the widest measure the
        // file's own points already had.
        SvgGeometry.Extent named = SvgGeometry.NamedBoxOf(paths, scale, offsetX, offsetY);
        Assert.That(bounds.Width, Is.EqualTo(Math.Max(named.Width, named.Height)).Within(0.05),
            name + " was drawn other than the size the file sized it");
      }
    }

    /// <summary>An icon file that is not there, or not readable, answers without a shape rather than loudly.</summary>
    [AvaloniaTest]
    public void AnIconThatCannotBeReadIsAnsweredAsAnIconThatIsNotThere()
    {
      string file = Path.Combine(Path.GetTempPath(), "nereus-broken-icon-" + Guid.NewGuid().ToString("N") + ".svg");
      File.WriteAllText(file, "<svg viewBox=\"0 0 10 10\"><path d=\"M0 0L\"/></svg>");
      try
      {
        Assert.That(SvgGeometry.ReadFile(file), Is.Not.Null,
            "the path that reads is drawn, and the one that does not is left out");
      }
      finally
      {
        File.Delete(file);
      }

      Assert.That(SvgGeometry.ReadFile(Path.Combine(Path.GetTempPath(), "no-such-icon-" + Guid.NewGuid().ToString("N") + ".svg")),
          Is.Null);
      Assert.That(SvgGeometry.Read("<svg/>"), Is.Null, "a file with no drawing has no shape to hand over");
      Assert.That(SvgGeometry.Read("not xml at all"), Is.Null);
    }

    /// <summary>The drawing a path dictates, recorded as the commands it was dictated as.</summary>
    private static List<string> Walked(string data)
    {
      Recorder recorder = new Recorder();
      SvgGeometry.Walk(data, recorder);
      return recorder.Lines;
    }

    /// <summary>A sink that writes down what it was told to draw, in the order it was told.</summary>
    private sealed class Recorder : SvgGeometry.IPathSink
    {
      internal readonly List<string> Lines = new List<string>();

      public void Move(double x, double y) => Lines.Add("M " + N(x) + " " + N(y));
      public void Line(double x, double y) => Lines.Add("L " + N(x) + " " + N(y));

      public void Quadratic(double cx, double cy, double x, double y)
          => Lines.Add("Q " + N(cx) + " " + N(cy) + " " + N(x) + " " + N(y));

      public void Cubic(double c1x, double c1y, double c2x, double c2y, double x, double y)
          => Lines.Add("C " + N(c1x) + " " + N(c1y) + " " + N(c2x) + " " + N(c2y) + " " + N(x) + " " + N(y));

      public void Arc(double rx, double ry, double rotation, bool large, bool sweep, double x, double y)
          => Lines.Add("A " + N(rx) + " " + N(ry) + " " + N(rotation) + " " + (large ? 1 : 0) + " "
              + (sweep ? 1 : 0) + " " + N(x) + " " + N(y));

      public void Close() => Lines.Add("Z");

      /// <summary>
      /// A command's numbers, written the way a test writes them: plain, invariant, and short enough that a
      /// difference between two recordings says something a reader can see.
      /// </summary>
      private static string N(double value)
      {
        return value.ToString("0.####", CultureInfo.InvariantCulture);
      }
    }

    /// <summary>
    /// A directory the test project was told about at build time - see <c>TestSkinFromDisk</c> for why a test
    /// cannot derive these from the working directory.
    /// </summary>
    private static string MetadataPath(string name)
    {
      System.Reflection.AssemblyMetadataAttribute? value = typeof(TestSvgGeometry).Assembly
          .GetCustomAttributes<System.Reflection.AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      string path = Path.GetFullPath(value!.Value!);
      Assert.That(Directory.Exists(path), Is.True, name + " points at '" + path + "', which is not a directory");
      return path;
    }
  }
}
