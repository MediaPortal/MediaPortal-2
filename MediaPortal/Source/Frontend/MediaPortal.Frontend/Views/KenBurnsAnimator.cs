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
using Avalonia;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The reference's Ken Burns animation, calculation and all: a still picture slowly zoomed into one of
  /// nine spots or panned between two of them while it stands on the screen.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Ported from <c>KenBurnsAnimator</c>, <c>KenBurnsEffects</c> and the two effect classes of the skin
  /// engine, table for table: the same nine spot points, the same fourteen pan routes for landscape and
  /// portrait pictures, the same tenth-of-a-step zoom, and the same flattening of time, which starts the
  /// movement fast and lets it settle, so the picture is never crawling to a stop at the moment the next
  /// one arrives.
  /// </para>
  /// <para>
  /// What comes out of <see cref="RectAt"/> is the reference's answer: the part of the picture the viewer
  /// should see right now, in fractions of the picture's own size, relative to the letterbox frame the
  /// whole picture occupies in the output area - which is why the fractions can reach past nought or one:
  /// the frame includes the black borders, exactly as the reference's renderer included them. The screen
  /// draws it by cropping its image brush to that rect.
  /// </para>
  /// <para>
  /// The randomizer comes in from outside so a test can ask for the zoom effect by name instead of waiting
  /// for the dice, and the clock is an argument for the same reason.
  /// </para>
  /// </remarks>
  public sealed class KenBurnsAnimator
  {
    /// <summary>The reference's zoom: one tenth, spread over the whole time one picture stands.</summary>
    public const double DEFAULT_ZOOM_FACTOR = 1.1;

    /// <summary>How many pan routes the reference offered for each orientation.</summary>
    public const int NUM_PAN_SPOTS = 14;

    // The nine points around and inside a picture:
    //
    //     1---------2---------3
    //     |                   |
    //     8         0         4
    //     |                   |
    //     7---------6---------5
    //
    // as signed offsets, nought being the middle, x to the right and y downward. The pan routes are pairs
    // of these: a picture drifts from the first point of its pair to the second while it stands.
    internal static readonly (int X, int Y)[] SPOT_POINTS =
    {
      (0, 0),               // 0
      (-1, -1), (0, -1), (1, -1),  // 1, 2, 3 - the top row, left to right
      (1, 0),               // 4 - middle right
      (1, 1), (0, 1), (-1, 1),     // 5, 6, 7 - the bottom row, right to left
      (-1, 0)               // 8 - middle left
    };

    internal static readonly (int Start, int End)[] LANDSCAPE_PAN_SPOTS =
    {
      (1, 4), (1, 5), (8, 3), (8, 4),
      (8, 5), (7, 4), (7, 3), (5, 8),
      (5, 1), (4, 7), (4, 8), (4, 1),
      (3, 7), (3, 8)
    };

    internal static readonly (int Start, int End)[] PORTRAIT_PAN_SPOTS =
    {
      (1, 6), (1, 5), (2, 7), (2, 6),
      (2, 5), (3, 7), (3, 6), (5, 2),
      (5, 1), (6, 3), (6, 2), (6, 1),
      (7, 3), (7, 2)
    };

    private readonly Random _random;
    private bool _zoomEffect;
    private int _zoomCenterClass;
    private int _panIndex;
    private DateTime _startTime;

    public KenBurnsAnimator(Random? random = null)
    {
      _random = random ?? new Random();
    }

    /// <summary>How long one picture stands; the movement spreads over exactly this long.</summary>
    public double Duration { get; set; } = 4;

    /// <summary>
    /// Choose the effect for the next picture the way the reference did: half the pictures are zoomed into
    /// one of three spots - the middle, or a corner that follows the picture's own long way - and half are
    /// panned along one of the fourteen routes.
    /// </summary>
    public void Initialize(DateTime now)
    {
      _startTime = now;
      _zoomEffect = _random.Next(2) == 0;
      _zoomCenterClass = _random.Next(3);
      _panIndex = Math.Max(0, Math.Min(NUM_PAN_SPOTS - 1, _random.Next(NUM_PAN_SPOTS)));
    }

    /// <summary>The part of the picture - and its letterbox frame - to show at <paramref name="now"/>.</summary>
    public Rect RectAt(DateTime now, Size imageSize, Size outputSize)
    {
      double seconds = (now - _startTime).TotalSeconds;
      double progress = Duration <= 0 ? 1 : seconds / Duration;
      return EffectAt(Flatten(progress), _zoomEffect, _zoomCenterClass, _panIndex, imageSize, outputSize);
    }

    /// <summary>
    /// The reference's flattening of time: nought stays nought, and the further the animation has run the
    /// slower it moves, without ever quite stopping. A linear slide would lurch into the next picture's
    /// first frame; this one is always easing.
    /// </summary>
    public static double Flatten(double progress)
    {
      // The reference clamped only the near end - a negative progress is nought, a long-overdue one just
      // keeps settling.
      if (progress < 0)
        progress = 0;
      return 1 - 1 / (5 * progress * progress + 1);
    }

    /// <summary>
    /// One frame of one named effect. Public and static because the screen applies what the math says, and
    /// a test should be able to ask the math directly instead of watching the screen.
    /// </summary>
    /// <param name="progress">Animation progress, nought (picture as it hangs) to one (fully moved).</param>
    /// <param name="zoom">True for the zoom effect, false for the pan.</param>
    /// <param name="zoomCenterClass">
    /// For the zoom: nought = top-left corner, one = middle, two = bottom-right corner - the reference's
    /// three classes, which it resolved to one of the nine points by the picture's orientation.
    /// </param>
    /// <param name="panIndex">For the pan: the route, nought to thirteen.</param>
    public static Rect EffectAt(double progress, bool zoom, int zoomCenterClass, int panIndex,
        Size imageSize, Size outputSize)
    {
      if (!zoom)
        return PanRectangleAt(progress, panIndex, imageSize, outputSize);

      bool landscape = IsLandscape(imageSize, outputSize);
      int point;
      switch (zoomCenterClass)
      {
        case 0:
          point = landscape ? 8 : 2;
          break;
        case 2:
          point = landscape ? 4 : 6;
          break;
        default:
          point = 0;
          break;
      }

      double zoomFactor = 1 + (DEFAULT_ZOOM_FACTOR - 1) * progress;
      return ZoomRectangle(zoomFactor, point, imageSize, outputSize);
    }

    /// <summary>The reference's zoom rectangle: the output area shrunk by the factor, anchored at one of the nine points.</summary>
    public static Rect ZoomRectangle(double zoomFactor, int zoomCenterPoint, Size imageSize, Size outputSize)
    {
      Size frame = ScaledOutput(imageSize, outputSize, zoomFactor);

      double left;
      double top;
      switch (zoomCenterPoint)
      {
        case 0: // centred both ways
          left = (imageSize.Width - frame.Width) / 2;
          top = (imageSize.Height - frame.Height) / 2;
          break;
        case 2: // width centred, top unchanged
          left = (imageSize.Width - frame.Width) / 2;
          top = 0;
          break;
        case 8: // height centred, left unchanged
          left = 0;
          top = (imageSize.Height - frame.Height) / 2;
          break;
        case 6: // width centred, bottom unchanged
          left = (imageSize.Width - frame.Width) / 2;
          top = imageSize.Height - frame.Height;
          break;
        case 4: // height centred, right unchanged
          left = imageSize.Width - frame.Width;
          top = (imageSize.Height - frame.Height) / 2;
          break;
        case 1: // top left unchanged
          left = 0;
          top = 0;
          break;
        case 3: // top right unchanged
          left = imageSize.Width - frame.Width;
          top = 0;
          break;
        case 7: // bottom left unchanged
          left = 0;
          top = imageSize.Height - frame.Height;
          break;
        default: // 5: bottom right unchanged - and any stranger number the same
          left = imageSize.Width - frame.Width;
          top = imageSize.Height - frame.Height;
          break;
      }

      return Fractions(left, top, frame, imageSize);
    }

    /// <summary>The reference's pan rectangle: the output area at a fixed zoom, slid between two points.</summary>
    public static Rect PanRectangleAt(double progress, int panIndex, Size imageSize, Size outputSize)
    {
      (int Start, int End) route = (IsLandscape(imageSize, outputSize)
          ? LANDSCAPE_PAN_SPOTS : PORTRAIT_PAN_SPOTS)[Math.Max(0, Math.Min(NUM_PAN_SPOTS - 1, panIndex))];
      (int X, int Y) from = SPOT_POINTS[route.Start];
      (int X, int Y) to = SPOT_POINTS[route.End];

      double panX = from.X + (to.X - from.X) * progress;
      double panY = from.Y + (to.Y - from.Y) * progress;

      Size frame = ScaledOutput(imageSize, outputSize, DEFAULT_ZOOM_FACTOR);
      double left = (imageSize.Width - frame.Width) * (panX + 1) * 0.5;
      double top = (imageSize.Height - frame.Height) * (panY + 1) * 0.5;
      return Fractions(left, top, frame, imageSize);
    }

    /// <summary>
    /// Whether the picture has borders at top and bottom (landscape) rather than left and right, against
    /// the place it has to fill - the reference's own test, ratio against ratio.
    /// </summary>
    public static bool IsLandscape(Size imageSize, Size outputSize)
    {
      return imageSize.Width / outputSize.Width > imageSize.Height / outputSize.Height;
    }

    /// <summary>
    /// The output area measured in the picture's own pixels - the letterbox frame included - shrunk by the
    /// zoom. The reference's normalisation fits the picture's longer side, so this frame reaches exactly to
    /// the picture's edge in one direction and past it in the other; that is where the reference's black
    /// borders came from.
    /// </summary>
    private static Size ScaledOutput(Size imageSize, Size outputSize, double zoomFactor)
    {
      double normalization = Math.Max(imageSize.Width / outputSize.Width, imageSize.Height / outputSize.Height);
      return new Size(outputSize.Width * normalization / zoomFactor,
          outputSize.Height * normalization / zoomFactor);
    }

    private static Rect Fractions(double left, double top, Size frame, Size imageSize)
    {
      return new Rect(left / imageSize.Width, top / imageSize.Height,
          frame.Width / imageSize.Width, frame.Height / imageSize.Height);
    }
  }
}
