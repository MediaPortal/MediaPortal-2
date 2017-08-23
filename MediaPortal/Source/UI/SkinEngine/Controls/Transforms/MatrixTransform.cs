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

using System.Globalization;
using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Transforms
{
  public class MatrixTransform : Transform
  {
    #region Protected fields

    protected AbstractProperty _matrixStringProperty;

    #endregion

    #region Ctor

    public MatrixTransform()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    void Init()
    {
      _matrixStringProperty = new SProperty(typeof(string), string.Empty);
    }

    void Attach()
    {
      _matrixStringProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _matrixStringProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      MatrixTransform t = (MatrixTransform) source;
      Matrix = t.Matrix;
      Attach();
    }

    #endregion

    public AbstractProperty MatrixProperty
    {
      get { return _matrixStringProperty; }
    }

    public string Matrix
    {
      get { return (string) _matrixStringProperty.GetValue(); }
      set { _matrixStringProperty.SetValue(value); }
    }

    public override void UpdateTransform()
    {
      base.UpdateTransform();
      _matrix = SharpDX.Matrix.Identity;
      string matrix = Matrix;
      if (string.IsNullOrEmpty(matrix))
        return;

      string[] matrixParts = matrix.Split(',');
      if (matrixParts.Length != 6) 
        return;

      ParseToFloat(ref _matrix.M11, matrixParts[0]);
      ParseToFloat(ref _matrix.M12, matrixParts[1]);
      ParseToFloat(ref _matrix.M21, matrixParts[2]);
      ParseToFloat(ref _matrix.M22, matrixParts[3]);
      ParseToFloat(ref _matrix.M41, matrixParts[4]);
      ParseToFloat(ref _matrix.M42, matrixParts[5]);
      _needUpdate = false;
    }

    static void ParseToFloat(ref float target, string value)
    {
      target = float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
  }
}