#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.UI.SkinEngine.Controls.Animations.EasingFunctions
{
  /// <summary>
  /// Interface that represents a transformation of normalized time (progress).
  /// Animations use this interface to transform their progress before interpolation.
  /// </summary>
  public interface IEasingFunction
  {
    /// <summary>
    /// Transforms normalized time to control the pace of an animation.
    /// </summary>
    /// <param name="normalizedTime">normalized time (progress) of the animation</param>
    /// <returns>transformed progress</returns>
    double Ease(double normalizedTime);
  }
}
