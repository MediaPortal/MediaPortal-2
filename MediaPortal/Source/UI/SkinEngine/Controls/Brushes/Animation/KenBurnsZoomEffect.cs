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

using SharpDX;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes.Animation
{
  /// <summary>
  /// Defines the zoom spot for a Ken Burns zoom effect. Depending on the orientation of the image (landscape/portrait), only the three
  /// spots in the middle (of the available 9 spots) can be used for the zoom operation.
  /// </summary>
  public enum ZoomCenterClass
  {
    TopLeft,
    Middle,
    BottomRight
  }

  /// <summary>
  /// This class implements the Ken Burns zoom effect.
  /// </summary>
  public class KenBurnsZoomEffect : AbstractKenBurnsEffect
  {
    protected ZoomCenterClass _zoomCenterClass;
    protected float _startZoomFactor;
    protected float _endZoomFactor;

    /// <summary>
    /// Simple convenience constructor which initializes the animation variables with defaults or random values.
    /// </summary>
    public KenBurnsZoomEffect() : this((ZoomCenterClass) _randomizer.Next(3), 1, KenBurnsEffects.KENBURNS_DEFAULT_ZOOM_FACTOR) {}

    /// <summary>
    /// Constructor to explicitly set all animation values.
    /// </summary>
    /// <param name="zoomCenterClass">Defines which spot the animation will use as zoom spot.</param>
    /// <param name="startZoomFactor">Initial zoom factor. This value is normally near <c>1</c>.</param>
    /// <param name="endZoomFactor">End zoom factor. This value could be <see cref="KenBurnsEffects.KENBURNS_DEFAULT_ZOOM_FACTOR"/>.</param>
    public KenBurnsZoomEffect(ZoomCenterClass zoomCenterClass, float startZoomFactor, float endZoomFactor)
    {
      _zoomCenterClass = zoomCenterClass;
      _startZoomFactor = startZoomFactor;
      _endZoomFactor = endZoomFactor;
    }

    public override RectangleF GetZoomRect(float animationProgress, Size imageSize, Size outputSize)
    {
      bool isLandscape = IsLandscape(imageSize.ToSize2F(), outputSize.ToSize2F());
      int zoomCenterPoint = 0;
      switch (_zoomCenterClass)
      {
        case ZoomCenterClass.TopLeft:
          zoomCenterPoint = isLandscape ? 8 : 2;
          break;
        case ZoomCenterClass.Middle:
          zoomCenterPoint = 0;
          break;
        case ZoomCenterClass.BottomRight:
          zoomCenterPoint = isLandscape ? 4 : 6;
          break;
      }
      return KenBurnsEffects.GetKenBurnsZoomRectangle(_startZoomFactor + (_endZoomFactor - _startZoomFactor) * animationProgress,
          zoomCenterPoint, imageSize.ToSize2F(), outputSize.ToSize2F());
    }
  }
}