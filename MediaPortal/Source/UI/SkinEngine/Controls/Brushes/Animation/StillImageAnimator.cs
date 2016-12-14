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

namespace MediaPortal.UI.SkinEngine.Controls.Brushes.Animation
{
  /// <summary>
  /// StillImage animation class provides static images only.
  /// </summary>
  public class StillImageAnimator: IImageAnimator
  {
    protected static readonly RectangleF _zoomRect = new RectangleF(0, 0, 1, 1);

    #region IImageAnimator implementation

    public void Initialize()
    {
    }

    public RectangleF GetZoomRect(Size imageSize, Size outputSize, DateTime displayTime)
    {
      return _zoomRect;
    }

    #endregion
  }
}
