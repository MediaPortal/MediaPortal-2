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
  /// This class implements the Ken Burns pan effect.
  /// </summary>
  public class KenBurnsPanEffect : AbstractKenBurnsEffect
  {
    protected int _panPointsIndex;
    protected float _zoomFactor;

    /// <summary>
    /// Simple convenience constructor which initializes the animation variables with defaults or random values.
    /// </summary>
    public KenBurnsPanEffect() : this(_randomizer.Next(KenBurnsEffects.NUM_PAN_SPOTS), KenBurnsEffects.KENBURNS_DEFAULT_PAN_ZOOM_FACTOR) {}

    /// <summary>
    /// Constructor to explicitly set all animation values.
    /// </summary>
    /// <param name="panPointsIndex">Index into the pan points fields <see cref="KenBurnsEffects.LANDSCAPE_PAN_SPOTS"/> or
    /// <see cref="KenBurnsEffects.PORTRAIT_PAN_SPOTS"/>, depending on the orientation of the image to animate.</param>
    /// <param name="zoomFactor">Zoom factor to use all over the animation. This could be
    /// <see cref="KenBurnsEffects.KENBURNS_DEFAULT_PAN_ZOOM_FACTOR"/>.</param>
    public KenBurnsPanEffect(int panPointsIndex, float zoomFactor)
    {
      if (panPointsIndex < 0)
        panPointsIndex = 0;
      if (panPointsIndex >= KenBurnsEffects.NUM_PAN_SPOTS)
        panPointsIndex = KenBurnsEffects.NUM_PAN_SPOTS;
      _panPointsIndex = panPointsIndex;
      _zoomFactor = zoomFactor;
    }

    public override RectangleF GetZoomRect(float animationProgress, Size imageSize, Size outputSize)
    {
      bool isLandscape = IsLandscape(imageSize.ToSize2F(), outputSize.ToSize2F());
      Point startEndPanPoints = isLandscape ? KenBurnsEffects.LANDSCAPE_PAN_SPOTS[_panPointsIndex] : KenBurnsEffects.PORTRAIT_PAN_SPOTS[_panPointsIndex];
      PointF panStartPoint = KenBurnsEffects.SPOT_POINTS[startEndPanPoints.X];
      PointF panEndPoint = KenBurnsEffects.SPOT_POINTS[startEndPanPoints.Y];

      return KenBurnsEffects.GetKenBurnsPanRectangle(_zoomFactor,
          panStartPoint.X + (panEndPoint.X - panStartPoint.X) * animationProgress,
          panStartPoint.Y + (panEndPoint.Y - panStartPoint.Y) * animationProgress, imageSize.ToSize2F(), outputSize.ToSize2F());
    }
  }
}