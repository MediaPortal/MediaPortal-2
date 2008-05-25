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

using MediaPortal.Presentation.Properties;
using SlimDX;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Transforms
{
  public class SkewTransform : Transform
  {
    #region Private fields

    Property _centerXProperty;
    Property _centerYProperty;
    Property _angleXProperty;
    Property _angleYProperty;

    #endregion

    #region Ctor

    public SkewTransform()
    {
      Init();
    }

    void Init()
    {
      _centerYProperty = new Property(typeof(double), 0.0);
      _centerXProperty = new Property(typeof(double), 0.0);
      _angleXProperty = new Property(typeof(double), 0.0);
      _angleYProperty = new Property(typeof(double), 0.0);

      _centerYProperty.Attach(OnPropertyChanged);
      _centerXProperty.Attach(OnPropertyChanged);
      _angleXProperty.Attach(OnPropertyChanged);
      _angleYProperty.Attach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      SkewTransform t = source as SkewTransform;
      CenterX = copyManager.GetCopy(t.CenterX);
      CenterY = copyManager.GetCopy(t.CenterY);
      AngleX = copyManager.GetCopy(t.AngleX);
      AngleY = copyManager.GetCopy(t.AngleY);
    }

    #endregion

    protected void OnPropertyChanged(Property property)
    {
      _needUpdate = true;
      Fire();
    }

    public Property CenterXProperty
    {
      get { return _centerXProperty; }
    }

    public double CenterX
    {
      get { return (double)_centerXProperty.GetValue(); }
      set { _centerXProperty.SetValue(value); }
    }

    public Property CenterYProperty
    {
      get { return _centerYProperty; }
    }

    public double CenterY
    {
      get { return (double)_centerYProperty.GetValue(); }
      set { _centerYProperty.SetValue(value); }
    }

    public Property AngleXProperty
    {
      get { return _angleXProperty; }
    }

    public double AngleX
    {
      get { return (double)_angleXProperty.GetValue(); }
      set { _angleXProperty.SetValue(value); }
    }

    public Property AngleYProperty
    {
      get { return _angleYProperty; }
    }

    public double AngleY
    {
      get { return (double)_angleYProperty.GetValue(); }
      set { _angleYProperty.SetValue(value); }
    }

    public override void UpdateTransform()
    {
      base.UpdateTransform();
      _matrix = Matrix.Identity;
      return;
      ///@todo: fix skew transform
      double cx = CenterX;
      double cy = CenterY;

      bool translation = ((cx != 0.0) || (cy != 0.0));
      if (translation)
        _matrix = Matrix.Translation((float)cx, (float)cy, 0);
      else
        _matrix = Matrix.Identity;

      double ax = AngleX;
      //      if (ax != 0.0)
      //        _matrix.xy = Math.Tan(ax * Math.PI / 180);

      double ay = AngleY;
      //if (ay != 0.0)
      //        _matrix.yx = Math.Tan(ay * Math.PI / 180);

      if (translation)
        _matrix = Matrix.Translation((float)-cx, (float)-cy, 0);
    }
  }
}
