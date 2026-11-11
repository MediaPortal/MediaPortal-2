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
using Avalonia.Media;
using System;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// One band's edge fade: the mask it wears and the depths its two ends were last read at.  The first card of a
  /// band that stands at its left edge has walked out of nothing and stands whole; a card the caret walked past
  /// an edge dissolves into that edge, and a band that still holds cards beyond an edge says so by dissolving the
  /// card it shows there - which is what the shelf's wall does at its own ends.
  /// </summary>
  /// <remarks>
  /// The shelf laid this over its own wall first; every other band - the details row, the person bands, the menu
  /// rail - asks for the same dress through this one reading, which is why the reading stands in a file of its
  /// own rather than in the middle of the shelf.
  /// </remarks>
  internal sealed class BandEndFade
  {
    private readonly ScrollViewer _band;

    /// <summary>Whether the list runs sideways (a row of cards) or down (the menu's rail).</summary>
    private readonly bool _sideways;

    private LinearGradientBrush? _mask;

    private double _before = UNREAD;

    private double _after = UNREAD;

    internal BandEndFade(ScrollViewer band, bool sideways = true)
    {
      _band = band;
      _sideways = sideways;
      band.AddHandler(ScrollViewer.ScrollChangedEvent, delegate
      {
        Update();
      });
    }

    /// <summary>The list this reading watches, so that a caller can ask whether it already holds the answer.</summary>
    internal ScrollViewer Band => _band;

    /// <summary>Says what each end of the band is hiding and, if the mask says otherwise, writes it on.</summary>
    internal void Update()
    {
      double view = _sideways ? _band.Viewport.Width : _band.Viewport.Height;
      if (view <= 0)
        return;

      double offset = _sideways ? _band.Offset.X : _band.Offset.Y;
      double extent = _sideways ? _band.Extent.Width : _band.Extent.Height;

      double zone = EDGE_FADE * view;
      double before = FadeDepth(offset, zone);
      double after = FadeDepth(extent - view - offset, zone);
      if (before == _before && after == _after)
        return;

      _before = before;
      _after = after;

      // Neither end has anything standing out of the picture - the whole band fits its window - so it wears no
      // mask at all, exactly as a wall shorter than its window wears none.
      if (before <= 0 && after <= 0)
      {
        _band.OpacityMask = null;
        return;
      }

      LinearGradientBrush mask = _mask ??= NewEdgeFade();
      mask.StartPoint = new RelativePoint(_sideways ? 0.0 : 0.5, _sideways ? 0.5 : 0.0, RelativeUnit.Relative);
      mask.EndPoint = new RelativePoint(_sideways ? 1.0 : 0.5, _sideways ? 0.5 : 1.0, RelativeUnit.Relative);
      mask.GradientStops[0].Color = FadeColour(before);
      mask.GradientStops[3].Color = FadeColour(after);
      _band.OpacityMask = mask;
    }

    /// <summary>
    /// How far in from an end of a list its rows dissolve once a whole fade zone of it has gone out of the
    /// picture: the twelve percent this front end's menu column wears.
    /// </summary>
    internal const double EDGE_FADE = 0.12;

    /// <summary>How finely the depth an end fades is read off the wall: eight steps between none and all.</summary>
    internal const double FADE_STEPS = 8;

    /// <summary>A depth no end has been asked about yet, so that the first reading is always written.</summary>
    internal const double UNREAD = -1;

    /// <summary>The depth an end is fading to, out of the eight the mask knows.</summary>
    internal static double FadeDepth(double hidden, double zone)
    {
      if (hidden <= 0 || zone <= 0)
        return 0;

      double gone = Math.Min(1.0, hidden / zone);
      return Math.Round(gone * FADE_STEPS) / FADE_STEPS;
    }

    /// <summary>The mask's colour for an end fading to the given depth.</summary>
    internal static Color FadeColour(double depth)
      => Color.FromArgb((byte)Math.Round((1.0 - depth) * 255.0), 0xFF, 0xFF, 0xFF);

    /// <summary>The edge mask with both ends standing opaque; the depths are written on it afterwards.</summary>
    internal static LinearGradientBrush NewEdgeFade()
    {
      LinearGradientBrush fade = new LinearGradientBrush
      {
        StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative)
      };
      fade.GradientStops.Add(new GradientStop(Colors.White, 0));
      fade.GradientStops.Add(new GradientStop(Colors.White, EDGE_FADE));
      fade.GradientStops.Add(new GradientStop(Colors.White, 1.0 - EDGE_FADE));
      fade.GradientStops.Add(new GradientStop(Colors.White, 1));
      return fade;
    }
  }
}