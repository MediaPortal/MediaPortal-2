#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Drawing;

namespace MediaPortal.UI.Players.Picture.Animation
{
  /// <summary>
  /// Ken Burns effect uses different pan and zoom operations to animate a picture.
  /// </summary>
  public class KenBurns: IPictureAnimator
  {
    public enum EffectType
    {
      None,
      Zoom,
      Pan
    }

    /* Zoom points arround the rectangle
     * Selected zoom point will be held at the same place while zooming the rectangle
     *
     *     1---------2---------3
     *     |                   |
     *     8         0         4
     *     |                   |
     *     7---------6---------5
     *
     */

    protected const float KENBURNS_ZOOM_FACTOR = 1.1f;

    protected static readonly Point[] LANDSCAPE_PAN_SPOTS = new Point[]
        {
          new Point(1, 4), new Point(1, 5), new Point(8, 3), new Point(8, 4),
          new Point(8, 5), new Point(7, 4), new Point(7, 3), new Point(5, 8),
          new Point(5, 1), new Point(4, 7), new Point(4, 8), new Point(4, 1),
          new Point(3, 7), new Point(3, 8)
        };

    protected static readonly Point[] PORTRAIT_PAN_SPOTS = new Point[]
        {
          new Point(1, 6), new Point(1, 5), new Point(2, 7), new Point(2, 6),
          new Point(2, 5), new Point(3, 7), new Point(3, 6), new Point(5, 2),
          new Point(5, 1), new Point(6, 3), new Point(6, 2), new Point(6, 1),
          new Point(7, 3), new Point(7, 2)
        };

    protected static readonly Point[] SPOT_POINTS = new Point[]
      {
          new Point(0, 0), // 0
          new Point(-1, -1), new Point(0, -1), new Point(1, -1), // 1, 2, 3
          new Point(1, 0), // 4
          new Point(1, 1), new Point(0, 1), new Point(-1, 1), // 5, 6, 7
          new Point(-1, 0) // 8
      };

    protected static readonly Random _randomizer = new Random(DateTime.Now.Millisecond);

    protected EffectType _currentEffectType = EffectType.None;
    protected int _zoomCenterClass = 0;
    protected float _startZoomFactor = 1;
    protected float _endZoomFactor = 1;
    protected int _panPointsIndex = 0;

    protected Size _imageSize;

    public EffectType CurrentEffect
    {
      get { return _currentEffectType; }
    }

    public void Initialize(Size imageSize)
    {
      _imageSize = imageSize;

      _currentEffectType = (EffectType) 1 + _randomizer.Next(2);
      switch (_currentEffectType)
      {
        case EffectType.Zoom:
          _zoomCenterClass = _randomizer.Next(3);
          _startZoomFactor = 1;
          _endZoomFactor = KENBURNS_ZOOM_FACTOR;
          // Not necessary
          _panPointsIndex = 0;
          break;
        case EffectType.Pan:
          _panPointsIndex = _randomizer.Next(14);
          // Not necessary
          _zoomCenterClass = 0;
          _startZoomFactor = KENBURNS_ZOOM_FACTOR;
          _endZoomFactor = KENBURNS_ZOOM_FACTOR;
          break;
        default:
          // No effects
          _zoomCenterClass = 0;
          _startZoomFactor = 1;
          _endZoomFactor = 1;
          _panPointsIndex = 0;
          break;
      }
    }

    protected bool IsLandscape(SizeF imageSize, SizeF outputSize)
    {
      return imageSize.Width / outputSize.Width > imageSize.Height / outputSize.Height;
    }

    public RectangleF GetZoomRect(float animationProgress, Size outputSize)
    {
      bool isLandscape = IsLandscape(_imageSize, outputSize);
      switch (_currentEffectType)
      {
        case EffectType.Zoom:
          int zoomCenterPoint = 0;
          if (isLandscape)
            switch (_zoomCenterClass)
            {
              case 0:
                zoomCenterPoint = 8;
                break;
              case 1:
                zoomCenterPoint = 0;
                break;
              case 2:
                zoomCenterPoint = 4;
                break;
            }
          else
            switch (_zoomCenterClass)
            {
              case 0:
                zoomCenterPoint = 2;
                break;
              case 1:
                zoomCenterPoint = 0;
                break;
              case 2:
                zoomCenterPoint = 6;
                break;
            }
          return GetKenBurnsZoomRectangle(_startZoomFactor + (_endZoomFactor - _startZoomFactor) * animationProgress,
              zoomCenterPoint, _imageSize, outputSize);
        case EffectType.Pan:
          Point startEndPanPoints = isLandscape ? LANDSCAPE_PAN_SPOTS[_panPointsIndex] : PORTRAIT_PAN_SPOTS[_panPointsIndex];
          PointF panStartPoint = SPOT_POINTS[startEndPanPoints.X];
          PointF panEndPoint = SPOT_POINTS[startEndPanPoints.Y];

          return GetKenBurnsPanRectangle(KENBURNS_ZOOM_FACTOR,
              panStartPoint.X + (panEndPoint.X - panStartPoint.X) * animationProgress,
              panStartPoint.Y + (panEndPoint.Y - panStartPoint.Y) * animationProgress, _imageSize, outputSize);
        default:
          // No effects
          return new RectangleF(0, 0, 1, 1);
      }
    }

    protected enum Stretch
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

    protected static RectangleF GetKenBurnsZoomRectangle(float zoomFactor, int zoomCenterPoint, SizeF imageSize, SizeF outputSize)
    {
      float normalizationFactor = NormalizeOutputSizeToImageSize(imageSize, outputSize, Stretch.UniformToFill);
      
      float scaledOutputWidth = outputSize.Width * normalizationFactor / zoomFactor;
      float scaledOutputHeight = outputSize.Height * normalizationFactor / zoomFactor;

      return CalculateZoomRect(imageSize, new SizeF(scaledOutputWidth, scaledOutputHeight), zoomCenterPoint);
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

    protected static RectangleF GetKenBurnsPanRectangle(float zoomFactor, float panX, float panY, SizeF imageSize, SizeF outputSize)
    {
      float normalizationFactor = NormalizeOutputSizeToImageSize(imageSize, outputSize, Stretch.UniformToFill);
      
      float scaledOutputWidth = outputSize.Width * normalizationFactor / zoomFactor;
      float scaledOutputHeight = outputSize.Height * normalizationFactor / zoomFactor;

      return CalculatePanRect(imageSize, new SizeF(scaledOutputWidth, scaledOutputHeight), panX, panY);
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