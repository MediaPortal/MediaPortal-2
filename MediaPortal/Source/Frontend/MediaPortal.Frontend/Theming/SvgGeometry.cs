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
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media;

namespace MediaPortal.Frontend.Theming
{
  /// <summary>
  /// The shapes an icon file carries, read as the geometry Avalonia paints.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A skin's icons are vectors, the way every other front end's icons are: one glyph that has to stand on a
  /// 60 pixel plaque and on a 200 pixel one without going soft at either. Avalonia answers the word "SVG" with
  /// nothing of its own - the toolkit ships no SVG reader at all - so this is the reader the skin uses, and it is
  /// deliberately a small one. It reads the two things an icon file is made of.
  /// </para>
  /// <para>
  /// <b>The box</b>, which says what patch of drawing space the file describes. The <c>viewBox</c> is the answer -
  /// the Material-style icons the skin carries describe a 960x960 patch whose y axis runs from -960 to 0, which
  /// nothing outside an SVG file would ever write down - and a file that declares no viewBox falls back to its
  /// own width and height, and a file that declares neither falls back to the bounds of what it actually drew.
  /// That is the same ladder a browser climbs.
  /// </para>
  /// <para>
  /// <b>The paths</b>, whose <c>d</c> attributes are walked here rather than handed to anybody: the SVG path
  /// grammar read to the letter, including the relative commands, the parameters that repeat without being
  /// written again, the smooth curves whose first control point is only known from the curve before them, the
  /// elliptic arcs, and the numbers an icon exporter presses together against a minus sign or a second decimal
  /// point (<c>M578-320</c>, <c>5.5.5</c>) because a kilobyte saved across an icon set is worth a million
  /// unreadable ones to whoever writes the reader.
  /// </para>
  /// <para>
  /// <b>What it does not read</b> is everything an icon is not: rectangles, circles, gradients, transforms, text,
  /// style sheets and embedded pictures. An icon is one or more <c>&lt;path&gt;</c> elements, which is what every
  /// icon set on the market exports - a glyph that survives being a path survives being recoloured and resized by
  /// whoever wears it. A file made of anything else answers null, and the plaque that asked for it draws without a
  /// glyph rather than showing a hole.
  /// </para>
  /// <para>
  /// The colour is nobody's business here. The files carry a <c>fill</c> of their own; it is ignored, because the
  /// plaque a glyph stands on and the state it is in (resting, lit, focused) decide the colour, and a colour baked
  /// into a hundred files could not do that. What is kept is the <b>winding</b>: the holes in these glyphs are
  /// drawn as counter-wound subpaths, which needs the non-zero fill rule - and Avalonia's stream geometry defaults
  /// to even-odd, so the rule is said out loud here or every icon arrives with its own letters cut out of it. A
  /// file that asks for <c>fill-rule="evenodd"</c> is answered with even-odd.
  /// </para>
  /// </remarks>
  public static class SvgGeometry
  {
    /// <summary>
    /// The square the drawing is fitted into. The number is arbitrary - a template sizes its glyph with the
    /// control it stands on and stretches it uniformly, not with the geometry - but a fixed one keeps every icon in
    /// the skin answering in the same units, so a plaque that pads by four units pads by four units whatever glyph
    /// it holds.
    /// </summary>
    public const double GLYPH_BOX = 24.0;

    private static readonly Regex SIDE = new Regex(
        @"(?<![\w-])(width|height)\s*=\s*""([^""]*)""|(?<![\w-])(width|height)\s*=\s*'([^']*)'",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PATH_ELEMENT = new Regex(
        @"<path\b[^>]*?/*>", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly Regex ATTRIBUTE = new Regex(
        @"(?<![\w-])([A-Za-z-]+)\s*=\s*""([^""]*)""|(?<![\w-])([A-Za-z-]+)\s*=\s*'([^']*)'",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// The shapes of the icon in the file, fitted into <see cref="GLYPH_BOX"/>. Null when the file is not there,
    /// cannot be read, or carries no path a screen could paint.
    /// </summary>
    public static Geometry? ReadFile(string path)
    {
      try
      {
        return Read(File.ReadAllText(path));
      }
      catch (Exception)
      {
        // A skin file a screen cannot use is a skin file that is not there. The front end is showing somebody
        // their television; a malformed icon has no claim on it.
        return null;
      }
    }

    /// <summary>
    /// The shapes an icon file's text carries, fitted into <see cref="GLYPH_BOX"/> and centred in it. Null when
    /// the text holds nothing that could be painted.
    /// </summary>
    public static Geometry? Read(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return null;

      List<SvgPath> paths = PathsIn(text);
      if (paths.Count == 0)
        return null;

      Rect box = BoxOf(text, paths);
      Fitting(box, out double scale, out double offsetX, out double offsetY);
      if (scale <= 0 || double.IsInfinity(scale) || double.IsNaN(scale))
        return null;

      // Two boxes are measured before anything is drawn. One is the box of every point the file names, the
      // control points of its curves included, and that is the box a control sizes a glyph by and lays into its
      // space from the top left. The other is how far the drawing actually paints. A box that is not a square
      // standing on that drawing leaves the glyph high in its plaque, so the two are needed to square it.
      Extent named = NamedBoxOf(paths, scale, offsetX, offsetY);
      Extent painted = PaintedBoxOf(paths, scale, offsetX, offsetY);

      StreamGeometry geometry = new StreamGeometry();
      using (StreamGeometryContext context = geometry.Open())
      {
        bool evenOdd = false;
        foreach (SvgPath path in paths)
          evenOdd |= path.EvenOdd;

        // Before the first figure, because the winding rule is asked once per geometry and not per figure.
        context.SetFillRule(evenOdd ? FillRule.EvenOdd : FillRule.NonZero);

        EmitSink sink = new EmitSink(context, scale, offsetX, offsetY);
        foreach (SvgPath path in paths)
        {
          try
          {
            Walk(path.Data, sink);
          }
          catch (FormatException)
          {
            // One path the reader cannot follow does not cancel a glyph: the paths that did read are drawn,
            // which is what any icon renderer that met a broken file in the wild would do too.
          }
        }

        sink.Finish();

        SquaredAboutItsMiddle(context, named, painted);
      }

      return geometry;
    }

    /// <summary>
    /// Every <c>&lt;path&gt;</c> element's drawing, in the order the file writes them - which is the order their
    /// fills stack, and why the holes in these glyphs come last and counter-wound.
    /// </summary>
    internal static List<SvgPath> PathsIn(string text)
    {
      List<SvgPath> paths = new List<SvgPath>();
      string document = Attribute(text, "fill-rule");

      foreach (Match element in PATH_ELEMENT.Matches(text))
      {
        string data = Attribute(element.Value, "d");
        if (data.Length == 0)
          continue;

        string rule = Attribute(element.Value, "fill-rule");
        if (rule.Length == 0)
          rule = document; // a file that asks for even-odd asks it of all of its paths

        paths.Add(new SvgPath(data, rule.Equals("evenodd", StringComparison.OrdinalIgnoreCase)));
      }

      return paths;
    }

    /// <summary>
    /// Where the drawing of the given box lands inside <see cref="GLYPH_BOX"/>: the uniform scale that fits the
    /// box and the offsets that move the box itself into the square and centre it there. The box's <em>origin</em>
    /// belongs to this arithmetic, not just its size - a Material glyph writes its canvas as
    /// <c>0 -960 960 960</c>, so its coordinates run from -960 to nothing and would be drawn entirely above the
    /// square if only the extent were fitted. Read out as three numbers rather than as a matrix so that the
    /// arithmetic a glyph is fitted with is checkable without drawing anything.
    /// </summary>
    internal static void Fitting(Rect box, out double scale, out double offsetX, out double offsetY)
    {
      scale = GLYPH_BOX / Math.Max(box.Width, box.Height);
      offsetX = (GLYPH_BOX - box.Width * scale) / 2 - box.X * scale;
      offsetY = (GLYPH_BOX - box.Height * scale) / 2 - box.Y * scale;
    }

    /// <summary>
    /// The patch of drawing space the file describes: its viewBox, else its own width and height, else the bounds
    /// of what it actually drew.
    /// </summary>
    internal static Rect BoxOf(string text, IReadOnlyList<SvgPath> paths)
    {
      Rect? declared = ViewBoxIn(text) ?? WidthAndHeightIn(text);
      if (declared != null)
        return declared.Value;

      double left = double.MaxValue;
      double top = double.MaxValue;
      double right = double.MinValue;
      double bottom = double.MinValue;
      BoundsSink bounds = new BoundsSink((x, y) =>
      {
        if (x < left)
          left = x;
        if (x > right)
          right = x;
        if (y < top)
          top = y;
        if (y > bottom)
          bottom = y;
      });

      foreach (SvgPath path in paths)
      {
        try
        {
          Walk(path.Data, bounds);
        }
        catch (FormatException)
        {
        }
      }

      if (left > right || top > bottom)
        return new Rect(0, 0, GLYPH_BOX, GLYPH_BOX);

      // Endpoints and control points only, which a file that declares no box of its own has to accept: the exact
      // outline would mean drawing it first, and a file silent about its own size is not one to insist on a
      // tight fit.
      return new Rect(left, top, Math.Max(right - left, 1e-6), Math.Max(bottom - top, 1e-6));
    }

    /// <summary>The file's declared viewBox, or null when it declares none or declares one unusable.</summary>
    internal static Rect? ViewBoxIn(string text)
    {
      string[] parts = Attribute(text, "viewBox")
          .Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length != 4)
        return null;

      double[] numbers = new double[4];
      for (int i = 0; i < 4; i++)
      {
        if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i]))
          return null;
      }

      if (numbers[2] <= 0 || numbers[3] <= 0)
        return null;

      return new Rect(numbers[0], numbers[1], numbers[2], numbers[3]);
    }

    /// <summary>
    /// The square a width and height describe, for the files that measure themselves rather than declaring a
    /// viewBox. A unit ("48px") is read for its number; a percentage answers nothing, since there is no box here
    /// for it to be a percentage of.
    /// </summary>
    internal static Rect? WidthAndHeightIn(string text)
    {
      double width = 0;
      double height = 0;
      foreach (Match match in SIDE.Matches(text))
      {
        string name = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[3].Value;
        string value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[4].Value;
        double number = Value(value);
        if (number <= 0)
          continue;

        if (name == "width")
          width = number;
        else
          height = number;
      }

      if (width <= 0 || height <= 0)
        return null;

      return new Rect(0, 0, width, height);
    }

    /// <summary>The value of an attribute in a tag, or the empty string when the tag does not carry it.</summary>
    private static string Attribute(string tag, string name)
    {
      foreach (Match match in ATTRIBUTE.Matches(tag))
      {
        string found = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[3].Value;
        if (found.Equals(name, StringComparison.OrdinalIgnoreCase))
          return match.Groups[2].Success ? match.Groups[2].Value : match.Groups[4].Value;
      }

      return string.Empty;
    }

    /// <summary>A length read for its number. Unusable text is zero, which every caller already reads as absent.</summary>
    private static double Value(string text)
    {
      // "48px" is 48 and "100%" is nothing, since there is no box here to take a percentage of.
      text = text.Trim().TrimEnd('p', 'x', 'P', 'X', ' ', 'u', 'n', 'i', 't', 's');
      return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
          ? number
          : 0;
    }

    /// <summary>One <c>&lt;path&gt;</c> element: its drawing and the winding rule it asks for.</summary>
    internal sealed class SvgPath
    {
      internal SvgPath(string data, bool evenOdd)
      {
        Data = data;
        EvenOdd = evenOdd;
      }

      internal string Data { get; }
      internal bool EvenOdd { get; }
    }

    /// <summary>
    /// A drawing's commands, handed over one at a time and already absolute - a walker that kept the file's
    /// relative coordinates would make every consumer redo the arithmetic the file only meant as a saving.
    /// </summary>
    /// <remarks>
    /// Internal rather than private so that the grammar can be watched command by command: a glyph whose bounds
    /// are right can still be drawn out of order, and the only way to see that is to stand where the drawing is
    /// being dictated.
    /// </remarks>
    internal interface IPathSink
    {
      void Move(double x, double y);
      void Line(double x, double y);
      void Quadratic(double cx, double cy, double x, double y);
      void Cubic(double c1x, double c1y, double c2x, double c2y, double x, double y);
      void Arc(double rx, double ry, double rotation, bool large, bool sweep, double x, double y);
      void Close();
    }

    /// <summary>
    /// The SVG path grammar, read to the letter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Absolute and relative letters are one command with one parameter list, so the file's letter is remembered
    /// only long enough to know whether the numbers are endpoints or offsets. What follows a command without a
    /// letter of its own repeats that command, which is how a polygon writes one <c>L</c> and forty pairs of
    /// numbers - except after <c>M</c>, where repeating means drawing, because a second position after a move
    /// has nothing else to mean.
    /// </para>
    /// <para>
    /// The smooth commands (<c>S</c> and <c>T</c>) name only their far half: their near control point is the
    /// previous curve's far control point seen through the current point, which is what makes the join smooth and
    /// is only known while the curve before them still is. After anything that is not a curve the current point is
    /// its own control point, and the smooth command is an ordinary one.
    /// </para>
    /// </remarks>
    internal static void Walk(string data, IPathSink sink)
    {
      Tokens tokens = new Tokens(data);

      double x = 0;
      double y = 0;
      double startX = 0;
      double startY = 0;
      double controlX = 0;
      double controlY = 0;
      char before = ' ';

      char? command;
      while ((command = tokens.PeekCommand()) != null)
      {
        char letter = tokens.NextCommand();
        for (bool first = true; first || tokens.HasNumber(); first = false)
        {
          char use = first ? letter : Repeat(letter);
          bool relative = use >= 'a';

          switch (char.ToUpperInvariant(use))
          {
            case 'M':
            {
              double nx = tokens.Number() + (relative ? x : 0);
              double ny = tokens.Number() + (relative ? y : 0);
              sink.Move(nx, ny);
              startX = x = nx;
              startY = y = ny;
              break;
            }
            case 'L':
            {
              double nx = tokens.Number() + (relative ? x : 0);
              double ny = tokens.Number() + (relative ? y : 0);
              sink.Line(nx, ny);
              x = nx;
              y = ny;
              break;
            }
            case 'H':
            {
              x = tokens.Number() + (relative ? x : 0);
              sink.Line(x, y);
              break;
            }
            case 'V':
            {
              y = tokens.Number() + (relative ? y : 0);
              sink.Line(x, y);
              break;
            }
            case 'C':
            {
              double c1x = tokens.Number() + (relative ? x : 0);
              double c1y = tokens.Number() + (relative ? y : 0);
              double c2x = tokens.Number() + (relative ? x : 0);
              double c2y = tokens.Number() + (relative ? y : 0);
              double nx = tokens.Number() + (relative ? x : 0);
              double ny = tokens.Number() + (relative ? y : 0);
              sink.Cubic(c1x, c1y, c2x, c2y, nx, ny);
              controlX = c2x;
              controlY = c2y;
              x = nx;
              y = ny;
              break;
            }
            case 'S':
            {
              bool smooth = before == 'C' || before == 'S';
              double c1x = smooth ? 2 * x - controlX : x;
              double c1y = smooth ? 2 * y - controlY : y;
              double c2x = tokens.Number() + (relative ? x : 0);
              double c2y = tokens.Number() + (relative ? y : 0);
              double nx = tokens.Number() + (relative ? x : 0);
              double ny = tokens.Number() + (relative ? y : 0);
              sink.Cubic(c1x, c1y, c2x, c2y, nx, ny);
              controlX = c2x;
              controlY = c2y;
              x = nx;
              y = ny;
              break;
            }
            case 'Q':
            {
              double cx = tokens.Number() + (relative ? x : 0);
              double cy = tokens.Number() + (relative ? y : 0);
              double nx = tokens.Number() + (relative ? x : 0);
              double ny = tokens.Number() + (relative ? y : 0);
              sink.Quadratic(cx, cy, nx, ny);
              controlX = cx;
              controlY = cy;
              x = nx;
              y = ny;
              break;
            }
            case 'T':
            {
              bool smooth = before == 'Q' || before == 'T';
              double cx = smooth ? 2 * x - controlX : x;
              double cy = smooth ? 2 * y - controlY : y;
              double nx = tokens.Number() + (relative ? x : 0);
              double ny = tokens.Number() + (relative ? y : 0);
              sink.Quadratic(cx, cy, nx, ny);
              controlX = cx;
              controlY = cy;
              x = nx;
              y = ny;
              break;
            }
            case 'A':
            {
              double rx = Math.Abs(tokens.Number());
              double ry = Math.Abs(tokens.Number());
              double rotation = tokens.Number();
              bool large = tokens.Flag() != 0;
              bool sweep = tokens.Flag() != 0;
              double nx = tokens.Number() + (relative ? x : 0);
              double ny = tokens.Number() + (relative ? y : 0);

              if (rx == 0 || ry == 0 || (nx == x && ny == y))
              {
                // A degenerate ellipse draws the line between the endpoints, and an arc back to the point it
                // started from draws nothing. Both are the specification's answer, not a shrug.
                if (nx != x || ny != y)
                  sink.Line(nx, ny);
              }
              else
              {
                // Radii too small to span the gap are grown until they fit rather than refused or - worse -
                // answered with a different arc than the file asked for.
                double halfX = Math.Abs(nx - x) / 2;
                double halfY = Math.Abs(ny - y) / 2;
                double over = (halfX * halfX) / (rx * rx) + (halfY * halfY) / (ry * ry);
                if (over > 1)
                {
                  double root = Math.Sqrt(over);
                  rx *= root;
                  ry *= root;
                }

                sink.Arc(rx, ry, rotation, large, sweep, nx, ny);
              }

              x = nx;
              y = ny;
              break;
            }
            case 'Z':
            {
              sink.Close();
              x = startX;
              y = startY;
              break;
            }
            default:
              throw new FormatException("unknown path command");
          }

          before = char.ToUpperInvariant(use);
        }
      }
    }

    /// <summary>What a command means when its parameters are written again without writing the letter.</summary>
    private static char Repeat(char letter)
    {
      return letter == 'M' ? 'L' : letter == 'm' ? 'l' : letter;
    }

    /// <summary>
    /// The drawing written into a geometry, with the file's coordinates moved and scaled into the glyph box as
    /// they are written - rather than left to a transform on the finished geometry, because a template that
    /// stretches a glyph over its plaque asks the geometry for its bounds and wants the answer to be true.
    /// </summary>
    private sealed class EmitSink : IPathSink
    {
      private readonly StreamGeometryContext _context;
      private readonly double _scale;
      private readonly double _offsetX;
      private readonly double _offsetY;
      private bool _open;

      internal EmitSink(StreamGeometryContext context, double scale, double offsetX, double offsetY)
      {
        _context = context;
        _scale = scale;
        _offsetX = offsetX;
        _offsetY = offsetY;
      }

      private Point At(double x, double y)
      {
        return new Point(x * _scale + _offsetX, y * _scale + _offsetY);
      }

      public void Move(double x, double y)
      {
        if (_open)
          _context.EndFigure(false);
        _context.BeginFigure(At(x, y));
        _open = true;
      }

      public void Line(double x, double y)
      {
        if (!_open)
          Move(x, y); // a line out of nowhere starts its figure there, which is what a browser makes of it too

        _context.LineTo(At(x, y));
      }

      public void Quadratic(double cx, double cy, double x, double y)
      {
        if (!_open)
          Move(x, y);

        _context.QuadraticBezierTo(At(cx, cy), At(x, y));
      }

      public void Cubic(double c1x, double c1y, double c2x, double c2y, double x, double y)
      {
        if (!_open)
          Move(x, y);

        _context.CubicBezierTo(At(c1x, c1y), At(c2x, c2y), At(x, y));
      }

      public void Arc(double rx, double ry, double rotation, bool large, bool sweep, double x, double y)
      {
        if (!_open)
          Move(x, y);

        // The arcs are turned into curves rather than handed to the renderer's own arc, so that one glyph draws
        // the same on a platform whose arc is exact and on one whose arc is approximated.
        _context.PreciseArcTo(At(x, y), new Size(rx * _scale, ry * _scale), rotation, large,
            sweep ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
      }

      public void Close()
      {
        if (_open)
          _context.EndFigure(true);
        _open = false;
      }

      /// <summary>Closes the last figure of a drawing that did not end on a <c>Z</c>, so nothing hangs open.</summary>
      internal void Finish()
      {
        if (_open)
          _context.EndFigure(false);
        _open = false;
      }
    }

    /// <summary>
    /// The box a control sizes a glyph by: every point a file names, the control points of its curves included.
    /// </summary>
    /// <remarks>
    /// An arc is counted at its ends only, which keeps this box inside the box the renderer reports rather than
    /// outside it - a box wider than the renderer's would size the glyph down without drawing a pixel more of it.
    /// </remarks>
    internal static Extent NamedBoxOf(IEnumerable<SvgPath> paths, double scale, double offsetX, double offsetY)
    {
      Extent box = new Extent();

      BoundsSink sink = new BoundsSink((x, y) => box.Add(new Point(x * scale + offsetX, y * scale + offsetY)));

      foreach (SvgPath path in paths)
      {
        try
        {
          Walk(path.Data, sink);
        }
        catch (FormatException)
        {
          // Measured over what could be read, which is what is drawn.
        }
      }

      return box;
    }

    /// <summary>
    /// How far a drawing actually paints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A curve is further out than its ends only where its slope turns flat, so a quadratic is watched at the one
    /// moment its slope is zero and a cubic at the two moments its slope's quadratic is zero. That is the
    /// drawing's own extent rather than a guess at it. An arc is measured by the toolkit, whose arc already knows
    /// how far it swings.
    /// </para>
    /// <para>
    /// Measuring this is not pedantry: the two flat triangles of the skip marks paint ten units below the box
    /// their own control points make, and a control that centres by that box hangs the glyph in the top of the
    /// plaque. Nothing else notices.
    /// </para>
    /// </remarks>
    internal static Extent PaintedBoxOf(IEnumerable<SvgPath> paths, double scale, double offsetX, double offsetY)
    {
      Extent box = new Extent();

      InkSink sink = new InkSink(scale, offsetX, offsetY, rect => box.Add(rect));

      foreach (SvgPath path in paths)
      {
        try
        {
          Walk(path.Data, sink);
        }
        catch (FormatException)
        {
          // Measured over what could be read, which is what is drawn.
        }
      }

      return box;
    }

    /// <summary>
    /// Squares a shape's box and stands that square on the middle of what was painted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A control that stretches a shape scales it by the widest side of its box and lays the box into the space
    /// from the top left - the toolkit adds no centring to the scale at all. A box wider than it is tall, which
    /// any mark drawn across a canvas is, therefore keeps every pixel of room the control had to spare beneath
    /// it: the skip marks of the playback foot rode seven pixels above the middle of their plaque that way while
    /// every bound in the file read as centred, and the loudspeaker sat two pixels above its mark's own middle.
    /// </para>
    /// <para>
    /// The box is made a square of the side its widest measure already was, standing on the middle of the
    /// drawing. The scale a control works out of it is the same one as before, so no glyph is drawn a pixel
    /// larger or smaller; what changes is that the spare room is shared above and below the mark instead of
    /// gathering all of it underneath. And were a toolkit ever to centre the box rather than top-left it, a
    /// square standing on the middle of its drawing is stood in the middle either way.
    /// </para>
    /// <para>
    /// The square is written as two marks, one at each of two opposite corners. A mark is a line a thousandth of
    /// a unit long pointing inwards: it counts as a point of the shape and so widens the box the shape is fitted
    /// by, while no pixel of it can land on a screen at any size a plaque is ever drawn. The glyph itself is not
    /// moved - a glyph moved would move its box with it and change nothing - only the box is made honest.
    /// </para>
    /// </remarks>
    private static void SquaredAboutItsMiddle(StreamGeometryContext context, Extent named, Extent painted)
    {
      const double tolerance = 0.02;
      if (named.IsEmpty || painted.IsEmpty)
        return;

      if (painted.Width <= 0 || painted.Height <= 0)
        return;

      // A measurement that falls outside the box of named points is a misreading rather than an off-centre
      // glyph, and a misreading is left alone.
      if (painted.Left < named.Left - tolerance || painted.Right > named.Right + tolerance
          || painted.Top < named.Top - tolerance || painted.Bottom > named.Bottom + tolerance)
        return;

      double side = Math.Max(named.Width, named.Height);
      double middleX = painted.MidX;
      double middleY = painted.MidY;

      if (Math.Abs(named.Width - side) < tolerance && Math.Abs(named.Height - side) < tolerance
          && Math.Abs(named.MidX - middleX) < tolerance && Math.Abs(named.MidY - middleY) < tolerance)
        return;

      Mark(context, middleX - side / 2, middleY - side / 2, 1, 1);
      Mark(context, middleX + side / 2, middleY + side / 2, -1, -1);
    }

    /// <summary>
    /// A point that widens a shape's box without painting a pixel of itself. The line it is drawn as points
    /// <paramref name="inwardsX"/> and <paramref name="inwardsY"/> so that the mark reaches its corner without
    /// leaning a thousandth of a unit past it.
    /// </summary>
    private static void Mark(StreamGeometryContext context, double x, double y, double inwardsX, double inwardsY)
    {
      context.BeginFigure(new Point(x, y));
      context.LineTo(new Point(x + 0.001 * inwardsX, y + 0.001 * inwardsY));
      context.EndFigure(false);
    }

    /// <summary>
    /// A box that grows to hold whatever is measured into it, and is asked for its own middle.
    /// </summary>
    /// <remarks>
    /// A point is a box with no size and is kept, because a drawing that only touches a place still reached it.
    /// </remarks>
    internal struct Extent
    {
      private double _left;
      private double _top;
      private double _right;
      private double _bottom;
      private bool _seen;

      internal bool IsEmpty => !_seen;
      internal double Left => _left;
      internal double Top => _top;
      internal double Right => _right;
      internal double Bottom => _bottom;
      internal double Width => _right - _left;
      internal double Height => _bottom - _top;
      internal double MidX => (_left + _right) / 2;
      internal double MidY => (_top + _bottom) / 2;

      internal void Add(Rect rect)
      {
        if (rect.Width <= 0 && rect.Height <= 0)
          Grow(rect.X, rect.Y, rect.X, rect.Y);
        else
          Grow(rect.Left, rect.Top, rect.Right, rect.Bottom);
      }

      internal void Add(Point point) => Grow(point.X, point.Y, point.X, point.Y);

      private void Grow(double left, double top, double right, double bottom)
      {
        if (!_seen)
        {
          _left = left;
          _top = top;
          _right = right;
          _bottom = bottom;
          _seen = true;
          return;
        }

        if (left < _left)
          _left = left;
        if (top < _top)
          _top = top;
        if (right > _right)
          _right = right;
        if (bottom > _bottom)
          _bottom = bottom;
      }
    }

    /// <summary>
    /// The box of named points, handed every point a drawing names in turn.
    /// </summary>
    private sealed class BoundsSink : IPathSink
    {
      private readonly Action<double, double> _mark;

      internal BoundsSink(Action<double, double> mark)
      {
        _mark = mark;
      }

      public void Move(double x, double y) => _mark(x, y);
      public void Line(double x, double y) => _mark(x, y);

      public void Quadratic(double cx, double cy, double x, double y)
      {
        _mark(cx, cy);
        _mark(x, y);
      }

      public void Cubic(double c1x, double c1y, double c2x, double c2y, double x, double y)
      {
        _mark(c1x, c1y);
        _mark(c2x, c2y);
        _mark(x, y);
      }

      public void Arc(double rx, double ry, double rotation, bool large, bool sweep, double x, double y)
      {
        _mark(x, y);
      }

      public void Close()
      {
      }
    }

    /// <summary>
    /// The extent a drawing paints, watched one command at a time.
    /// </summary>
    /// <remarks>
    /// A curve leaves the box of its ends only where its slope turns flat, so each curve is measured at those
    /// moments rather than sampled at a stride: a quadratic has one such moment per axis and a cubic has two, and
    /// what is marked is the point of the curve at that moment, which is ink by definition and can therefore
    /// never claim more room than the glyph paints.
    /// </remarks>
    private sealed class InkSink : IPathSink
    {
      private readonly double _scale;
      private readonly double _offsetX;
      private readonly double _offsetY;
      private readonly Action<Rect> _mark;

      private Point _at;
      private Point _start;
      private bool _open;

      internal InkSink(double scale, double offsetX, double offsetY, Action<Rect> mark)
      {
        _scale = scale;
        _offsetX = offsetX;
        _offsetY = offsetY;
        _mark = mark;
      }

      private Point At(double x, double y) => new Point(x * _scale + _offsetX, y * _scale + _offsetY);

      public void Move(double x, double y)
      {
        _at = _start = At(x, y);
        _open = true;
        _mark(new Rect(_at.X, _at.Y, 0, 0));
      }

      public void Line(double x, double y)
      {
        Point to = At(x, y);
        if (!_open)
          _start = to;

        _mark(RectBetween(_at, to));
        _at = to;
        _open = true;
      }

      public void Quadratic(double cx, double cy, double x, double y)
      {
        Point from = _at;
        Point control = At(cx, cy);
        Point to = At(x, y);
        if (!_open)
          _start = to;

        _mark(RectBetween(from, to));
        double denominator = from.X - 2 * control.X + to.X;
        if (Math.Abs(denominator) > 1e-9)
          FlatPoint(Math.Abs((from.X - control.X) / denominator), t => QuadraticAt(t, from, control, to));

        denominator = from.Y - 2 * control.Y + to.Y;
        if (Math.Abs(denominator) > 1e-9)
          FlatPoint(Math.Abs((from.Y - control.Y) / denominator), t => QuadraticAt(t, from, control, to));

        _at = to;
        _open = true;
      }

      public void Cubic(double c1x, double c1y, double c2x, double c2y, double x, double y)
      {
        Point from = _at;
        Point first = At(c1x, c1y);
        Point second = At(c2x, c2y);
        Point to = At(x, y);
        if (!_open)
          _start = to;

        _mark(RectBetween(from, to));
        foreach (double t in CubicTurns(from.X, first.X, second.X, to.X))
          FlatPoint(t, u => CubicAt(u, from, first, second, to));
        foreach (double t in CubicTurns(from.Y, first.Y, second.Y, to.Y))
          FlatPoint(t, u => CubicAt(u, from, first, second, to));

        _at = to;
        _open = true;
      }

      public void Arc(double rx, double ry, double rotation, bool large, bool sweep, double x, double y)
      {
        Point to = At(x, y);
        if (!_open)
        {
          // An arc with nothing to run from starts where it lands, which is all that can be said of it here.
          _start = to;
          _mark(new Rect(to.X, to.Y, 0, 0));
          _at = to;
          _open = true;
          return;
        }

        // The swing is handed to the toolkit's own arc, which knows where it reaches, rather than worked out a
        // second time from the file's endpoint recipe.
        StreamGeometry piece = new StreamGeometry();
        using (StreamGeometryContext context = piece.Open())
        {
          context.BeginFigure(_at);
          context.PreciseArcTo(to, new Size(rx * _scale, ry * _scale), rotation, large,
              sweep ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
          context.EndFigure(false);
        }

        // The two ends are ink whichever way the arc went, and are counted even when the toolkit answered the
        // swing with nothing at all.
        _mark(RectBetween(_at, to));
        if (piece.Bounds.Width > 0 || piece.Bounds.Height > 0)
          _mark(piece.Bounds);

        _at = to;
        _open = true;
      }

      public void Close()
      {
        if (_open)
          _mark(RectBetween(_at, _start));
        _at = _start;
        _open = false;
      }

      /// <summary>Marks the curve's own point at a moment that is a moment and not the end of the curve.</summary>
      private void FlatPoint(double t, Func<double, Point> at)
      {
        if (t <= 0 || t >= 1)
          return;

        Point point = at(t);
        _mark(new Rect(point.X, point.Y, 0, 0));
      }

      private static Rect RectBetween(Point a, Point b)
      {
        return new Rect(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
      }

      private static Point QuadraticAt(double t, Point from, Point control, Point to)
      {
        double left = (1 - t) * (1 - t);
        double middle = 2 * (1 - t) * t;
        double right = t * t;
        return new Point(left * from.X + middle * control.X + right * to.X,
            left * from.Y + middle * control.Y + right * to.Y);
      }

      private static Point CubicAt(double t, Point from, Point first, Point second, Point to)
      {
        double left = (1 - t) * (1 - t) * (1 - t);
        double next = 3 * (1 - t) * (1 - t) * t;
        double prior = 3 * (1 - t) * t * t;
        double right = t * t * t;
        return new Point(left * from.X + next * first.X + prior * second.X + right * to.X,
            left * from.Y + next * first.Y + prior * second.Y + right * to.Y);
      }

      /// <summary>The moments one axis of a cubic turns flat, which are the roots of its slope's quadratic.</summary>
      private static IEnumerable<double> CubicTurns(double from, double first, double second, double to)
      {
        double a = -from + 3 * first - 3 * second + to;
        double b = 2 * from - 4 * first + 2 * second;
        double c = -from + first;

        if (Math.Abs(a) < 1e-9)
        {
          if (Math.Abs(b) > 1e-9)
            yield return -c / b;
          yield break;
        }

        double discriminant = b * b - 4 * a * c;
        if (discriminant < 0)
          yield break;

        double root = Math.Sqrt(discriminant);
        yield return (-b - root) / (2 * a);
        yield return (-b + root) / (2 * a);
      }
    }

    /// <summary>
    /// The numbers of a path, read where they stand rather than split out first, because an SVG path writes
    /// numbers with nothing between them: <c>578-320</c> is two numbers, <c>5.5.5</c> is two numbers, and
    /// <c>0 011-.5</c> is four - two of which are flags.
    /// </summary>
    private sealed class Tokens
    {
      private const string COMMANDS = "MmLlHhVvCcSsQqTtAaZz";

      private readonly string _text;
      private int _at;

      internal Tokens(string text)
      {
        _text = text;
      }

      private int Skipped()
      {
        int i = _at;
        while (i < _text.Length && (_text[i] == ' ' || _text[i] == ',' || _text[i] == '\t'
            || _text[i] == '\r' || _text[i] == '\n'))
          i++;
        return i;
      }

      internal char? PeekCommand()
      {
        int i = Skipped();
        return i < _text.Length && COMMANDS.IndexOf(_text[i]) >= 0 ? _text[i] : (char?) null;
      }

      internal char NextCommand()
      {
        _at = Skipped();
        char c = _text[_at];
        if (COMMANDS.IndexOf(c) < 0)
          throw new FormatException("a path command was expected");
        _at++;
        return c;
      }

      /// <summary>Whether another parameter follows - anything that is neither a command nor the end.</summary>
      internal bool HasNumber()
      {
        int i = Skipped();
        return i < _text.Length && COMMANDS.IndexOf(_text[i]) < 0;
      }

      internal double Number()
      {
        int start = _at = Skipped();
        int i = _at;
        if (i < _text.Length && (_text[i] == '+' || _text[i] == '-'))
          i++;

        bool point = false;
        bool digits = false;
        while (i < _text.Length)
        {
          char c = _text[i];
          if (c == '.')
          {
            // A second decimal point belongs to the number after it: "5.5.5" is 5.5 and .5.
            if (point)
              break;
            point = true;
          }
          else if (c >= '0' && c <= '9')
          {
            digits = true;
          }
          else
          {
            break;
          }

          i++;
        }

        // An exponent may follow, and only when a digit follows it - 'e' spells nothing else in this grammar.
        if (digits && i < _text.Length && (_text[i] == 'e' || _text[i] == 'E'))
        {
          int after = i + 1;
          if (after < _text.Length && (_text[after] == '+' || _text[after] == '-'))
            after++;
          if (after < _text.Length && _text[after] >= '0' && _text[after] <= '9')
          {
            i = after;
            while (i < _text.Length && _text[i] >= '0' && _text[i] <= '9')
              i++;
          }
        }

        if (!digits && !point)
          throw new FormatException("a number was expected");

        _at = i;
        if (!double.TryParse(_text.Substring(start, i - start), NumberStyles.Float,
                CultureInfo.InvariantCulture, out double value))
          throw new FormatException("the number could not be read");

        return value;
      }

      /// <summary>
      /// An arc's large-arc or sweep flag, read as one character. It cannot be read as a number, because the
      /// files write two flags pressed together ("01") exactly as often as they write them apart.
      /// </summary>
      internal double Flag()
      {
        _at = Skipped();
        if (_at >= _text.Length)
          throw new FormatException("an arc flag was expected");

        char c = _text[_at];
        if (c != '0' && c != '1')
          throw new FormatException("an arc flag was expected");

        _at++;
        return c - '0';
      }
    }
  }
}
