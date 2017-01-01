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

using System.Drawing;
using MediaPortal.UI.Presentation.Geometries;

namespace MediaPortal.UI.SkinEngine.Geometry
{
  /// <summary>
  /// Cropping = Yes
  /// Stretch = Fill
  /// Zoom = None
  /// Shader = SmartZoom
  /// </summary>
  class GeometryIntelligentZoom : IGeometry
  {
    public const string NAME = "[Geometries.IntelligentZoom]";

    #region IGeometry Members

    public string Name
    {
      get { return NAME; }
    }

    public string Shader
    {
      get { return @"transforms\smart_zoom"; }
    }

    public bool RequiresCorrectAspectRatio
    {
      get { return true; }
    }

    public SizeF Transform(SizeF inputSize, SizeF targetSize)
    {
      return targetSize;
    }

    #endregion
  }
}
