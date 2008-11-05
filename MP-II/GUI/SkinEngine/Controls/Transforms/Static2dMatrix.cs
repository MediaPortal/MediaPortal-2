#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using SlimDX;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Transforms
{
  public class Static2dMatrix: Transform
  {
    #region Private fields

    float[] _elements;

    #endregion

    #region Ctor

    public Static2dMatrix()
    { }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Static2dMatrix m = (Static2dMatrix) source;
      _elements = (float[]) m._elements.Clone();
      CreateMatrix();
    }

    #endregion

    public void Set2DMatrix(System.Drawing.Drawing2D.Matrix matrix2d)
    {
      _elements = matrix2d.Elements;
      CreateMatrix();
    }

    void CreateMatrix()
    {
      _matrix = Matrix.Identity;
      _matrix.M11 = _elements[0];
      _matrix.M12 = _elements[1];
      _matrix.M21 = _elements[2];
      _matrix.M22 = _elements[3];
      _matrix.M41 = _elements[4];
      _matrix.M42 = _elements[5];
      _needUpdate = false;
    }
  }
}
