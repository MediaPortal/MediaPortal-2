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

using System;
using SharpDX;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes.Animation
{
  public enum Stretch
  {
    // The content is resized to fit in the destination dimensions while it preserves its
    // native aspect ratio. If the aspect ratio of the destination rectangle differs from
    // the source, the content won't fill the whole destionation area.
    Uniform,

    // The content is resized to fill the destination dimensions while it preserves its
    // native aspect ratio. 
    // If the aspect ratio of the destination rectangle differs from the source, the source content is 
    // clipped to fit in the destination dimensions completely.
    UniformToFill
  }

  /// <summary>
  /// Contains default values and calculation methods for Ken Burns zoom and pan effects.
  /// </summary>
  public class KenBurnsEffects
  {
    /* Zoom and pan points arround the rectangle:
     * For zoom, the selected point will be held at the same place while zooming the rectangle.
     * For pan, the image is moved from one pan spot to the other.
     *
     *     1---------2---------3
     *     |                   |
     *     8         0         4
     *     |                   |
     *     7---------6---------5
     *
     */

    public const float KENBURNS_DEFAULT_ZOOM_FACTOR = 1.1f;

    public const float KENBURNS_DEFAULT_PAN_ZOOM_FACTOR = 1.1f;

    public const int NUM_PAN_SPOTS = 14;

    public static readonly Point[] LANDSCAPE_PAN_SPOTS = new Point[NUM_PAN_SPOTS]
        {
          new Point(1, 4), new Point(1, 5), new Point(8, 3), new Point(8, 4),
          new Point(8, 5), new Point(7, 4), new Point(7, 3), new Point(5, 8),
          new Point(5, 1), new Point(4, 7), new Point(4, 8), new Point(4, 1),
          new Point(3, 7), new Point(3, 8)
        };

    public static readonly Point[] PORTRAIT_PAN_SPOTS = new Point[NUM_PAN_SPOTS]
        {
          new Point(1, 6), new Point(1, 5), new Point(2, 7), new Point(2, 6),
          new Point(2, 5), new Point(3, 7), new Point(3, 6), new Point(5, 2),
          new Point(5, 1), new Point(6, 3), new Point(6, 2), new Point(6, 1),
          new Point(7, 3), new Point(7, 2)
        };

    public static readonly Point[] SPOT_POINTS = new Point[]
      {
          new Point(0, 0), // 0
          new Point(-1, -1), new Point(0, -1), new Point(1, -1), // 1, 2, 3
          new Point(1, 0), // 4
          new Point(1, 1), new Point(0, 1), new Point(-1, 1), // 5, 6, 7
          new Point(-1, 0) // 8
      };

    protected static readonly Random _randomizer = new Random(DateTime.Now.Millisecond);

    public static RectangleF GetKenBurnsZoomRectangle(float zoomFactor, int zoomCenterPoint, SizeF imageSize, SizeF outputSize)
    {
      float normalizationFactor = NormalizeOutputSizeToImageSize(imageSize, outputSize, Stretch.UniformToFill);
      
      float scaledOutputWidth = outputSize.Width * normalizationFactor / zoomFactor;
      float scaledOutputHeight = outputSize.Height * normalizationFactor / zoomFactor;

      return CalculateZoomRect(imageSize, new SizeF(scaledOutputWidth, scaledOutputHeight), zoomCenterPoint);
    }

    public static RectangleF GetKenBurnsPanRectangle(float zoomFactor, float panX, float panY, SizeF imageSize, SizeF outputSize)
    {
      float normalizationFactor = NormalizeOutputSizeToImageSize(imageSize, outputSize, Stretch.UniformToFill);
      
      float scaledOutputWidth = outputSize.Width * normalizationFactor / zoomFactor;
      float scaledOutputHeight = outputSize.Height * normalizationFactor / zoomFactor;

      return CalculatePanRect(imageSize, new SizeF(scaledOutputWidth, scaledOutputHeight), panX, panY);
    }

    protected static float NormalizeOutputSizeToImageSize(SizeF imageSize, SizeF outputSize, Stretch stretch)
    {
      // Calculate zoom for best fit (largest image length (X/Y) fits completely in output area)
      float zoomFactorX = imageSize.Width / outputSize.Width;
      float zoomFactorY = imageSize.Height / outputSize.Height;

      return zoomFactorX > zoomFactorY == (stretch == Stretch.Uniform) ? zoomFactorX : zoomFactorY;
    }

    protected static RectangleF CalculateZoomRect(SizeF outerSize, SizeF innerSize, int zoomType)
    {
      float left;
      float top;

      switch (zoomType)
      {
        case 0: // centered, centered
          left = (outerSize.Width - innerSize.Width) / 2f;
          top = (outerSize.Height - innerSize.Height) / 2f;
          break;
        case 2: // Width centered, top unchanged
          left = (outerSize.Width - innerSize.Width) / 2f;
          top = 0;
          break;
        case 8: // Heigth centered, left unchanged
          left = 0;
          top = (outerSize.Height - innerSize.Height) / 2f;
          break;
        case 6: // Width centered, bottom unchanged
          left = (outerSize.Width - innerSize.Width) / 2f;
          top = outerSize.Height - innerSize.Height;
          break;
        case 4: // Height centered, right unchanged
          left = outerSize.Width - innerSize.Width;
          top = (outerSize.Height - innerSize.Height) / 2f;
          break;
        case 1: // Top left unchanged
          left = 0;
          top = 0;
          break;
        case 3: // Top right unchanged
          left = outerSize.Width - innerSize.Width;
          top = 0;
          break;
        case 7: // Bottom left unchanged
          left = 0;
          top = outerSize.Height - innerSize.Height;
          break;
        case 5: // Bottom right unchanged
          left = outerSize.Width - innerSize.Width;
          top = outerSize.Height - innerSize.Height;
          break;
        default:
          top = 0;
          left = 0;
          break;
      }
      return new RectangleF(left / outerSize.Width, top / outerSize.Height, innerSize.Width / outerSize.Width, innerSize.Height / outerSize.Height);
    }

    protected static RectangleF CalculatePanRect(SizeF outerSize, SizeF innerSize, float panX, float panY)
    {
      float panFactorX = (panX + 1) * 0.5f;
      float panFactorY = (panY + 1) * 0.5f;

      float left = (outerSize.Width - innerSize.Width) * panFactorX;
      float top = (outerSize.Height - innerSize.Height) * panFactorY;

      return new RectangleF(left / outerSize.Width, top / outerSize.Height, innerSize.Width / outerSize.Width, innerSize.Height / outerSize.Height);
    }
  }
}