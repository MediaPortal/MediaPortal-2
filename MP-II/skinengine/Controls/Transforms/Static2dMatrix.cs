#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion
using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using Microsoft.DirectX;
namespace SkinEngine.Controls.Transforms
{
  public class Static2dMatrix : Transform
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="SkewTransform"/> class.
    /// </summary>
    public Static2dMatrix()
    {
      Init();
    }
    public Static2dMatrix(Static2dMatrix r)
      : base(r)
    {
    }
    void Init()
    {
    }

    public override object Clone()
    {
      return new Static2dMatrix(this);
    }

    public void Set2DMatrix(System.Drawing.Drawing2D.Matrix matrix2d)
    {
      float[] elements = matrix2d.Elements;
      _matrix = Matrix.Identity;
      _matrix.M11 = elements[0];
      _matrix.M12 = elements[1];
      _matrix.M21 = elements[2];
      _matrix.M22 = elements[3];
      _matrix.M14 = elements[4];
      _matrix.M24 = elements[5];
    }
  }
}
