#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using MediaPortal.Core.General;
using SlimDX;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Controls.Transforms
{
  public class ScaleTransform : Transform
  {
    #region Private fields

    Property _centerXProperty;
    Property _centerYProperty;
    Property _scaleXProperty;
    Property _scaleYProperty;

    #endregion

    #region Ctor

    public ScaleTransform()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _centerYProperty = new Property(typeof(double), 0.0);
      _centerXProperty = new Property(typeof(double), 0.0);
      _scaleXProperty = new Property(typeof(double), 0.0);
      _scaleYProperty = new Property(typeof(double), 0.0);
    }

    void Attach()
    {
      _centerYProperty.Attach(OnPropertyChanged);
      _centerXProperty.Attach(OnPropertyChanged);
      _scaleXProperty.Attach(OnPropertyChanged);
      _scaleYProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _centerYProperty.Detach(OnPropertyChanged);
      _centerXProperty.Detach(OnPropertyChanged);
      _scaleXProperty.Detach(OnPropertyChanged);
      _scaleYProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ScaleTransform t = (ScaleTransform) source;
      CenterX = copyManager.GetCopy(t.CenterX);
      CenterY = copyManager.GetCopy(t.CenterY);
      ScaleX = copyManager.GetCopy(t.ScaleX);
      ScaleY = copyManager.GetCopy(t.ScaleY);
      Attach();
    }

    #endregion

    #region Public properties

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
      set { _centerYProperty = value; }
    }

    public double CenterY
    {
      get { return (double)_centerYProperty.GetValue(); }
      set { _centerYProperty.SetValue(value); }
    }

    public Property ScaleXProperty
    {
      get { return _scaleXProperty; }
    }

    public double ScaleX
    {
      get { return (double)_scaleXProperty.GetValue(); }
      set { _scaleXProperty.SetValue(value); }
    }

    public Property ScaleYProperty
    {
      get { return _scaleYProperty; }
    }

    public double ScaleY
    {
      get { return (double)_scaleYProperty.GetValue(); }
      set { _scaleYProperty.SetValue(value); }
    }

    #endregion

    public override void UpdateTransform()
    {
      base.UpdateTransform();
      double sx = ScaleX;
      double sy = ScaleY;

      if (sx == 0.0) sx = 0.00002;
      if (sy == 0.0) sy = 0.00002;

      double cx = CenterX * SkinContext.Zoom.Width;
      double cy = CenterY * SkinContext.Zoom.Height;

      if (cx == 0.0 && cy == 0.0)
      {
        _matrix=Matrix.Scaling((float)sx, (float)sy, 1.0f);
      }
      else
      {
        _matrix=Matrix.Translation((float)-cx, (float)-cy, 0);
        _matrix *= Matrix.Scaling((float)sx, (float)sy, 1.0f);
        _matrix *= Matrix.Translation((float)cx, (float)cy, 0);
      }
    }

    public override void UpdateTransformRel()
    {
      base.UpdateTransformRel();
      double sx = ScaleX;
      double sy = ScaleY;

      if (sx == 0.0) sx = 0.00002;
      if (sy == 0.0) sy = 0.00002;

      double cx = CenterX ;
      double cy = CenterY ;

      if (cx == 0.0 && cy == 0.0)
      {
        _matrixRel = Matrix.Scaling((float)sx, (float)sy, 1.0f);
      }
      else
      {
        _matrixRel = Matrix.Translation((float)-cx, (float)-cy, 0);
        _matrixRel *= Matrix.Scaling((float)sx, (float)sy, 1.0f);
        _matrixRel *= Matrix.Translation((float)cx, (float)cy, 0);
      }
    }

  }
}
