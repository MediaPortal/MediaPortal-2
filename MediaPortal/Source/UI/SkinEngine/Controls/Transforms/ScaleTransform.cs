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

using MediaPortal.Common.General;
using SharpDX;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Transforms
{
  public class ScaleTransform : Transform
  {
    #region Protected fields

    protected AbstractProperty _centerXProperty;
    protected AbstractProperty _centerYProperty;
    protected AbstractProperty _scaleXProperty;
    protected AbstractProperty _scaleYProperty;

    #endregion

    #region Ctor

    public ScaleTransform()
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
      _centerYProperty = new SProperty(typeof(double), 0.0);
      _centerXProperty = new SProperty(typeof(double), 0.0);
      _scaleXProperty = new SProperty(typeof(double), 1.0);
      _scaleYProperty = new SProperty(typeof(double), 1.0);
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
      CenterX = t.CenterX;
      CenterY = t.CenterY;
      ScaleX = t.ScaleX;
      ScaleY = t.ScaleY;
      Attach();
    }

    #endregion

    #region Public properties

    public AbstractProperty CenterXProperty
    {
      get { return _centerXProperty; }
    }

    public double CenterX
    {
      get { return (double) _centerXProperty.GetValue(); }
      set { _centerXProperty.SetValue(value); }
    }

    public AbstractProperty CenterYProperty
    {
      get { return _centerYProperty; }
      set { _centerYProperty = value; }
    }

    public double CenterY
    {
      get { return (double) _centerYProperty.GetValue(); }
      set { _centerYProperty.SetValue(value); }
    }

    public AbstractProperty ScaleXProperty
    {
      get { return _scaleXProperty; }
    }

    public double ScaleX
    {
      get { return (double) _scaleXProperty.GetValue(); }
      set { _scaleXProperty.SetValue(value); }
    }

    public AbstractProperty ScaleYProperty
    {
      get { return _scaleYProperty; }
    }

    public double ScaleY
    {
      get { return (double) _scaleYProperty.GetValue(); }
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

      double cx = CenterX;
      double cy = CenterY;

      if (cx == 0.0 && cy == 0.0)
        _matrix = Matrix.Scaling((float) sx, (float) sy, 1.0f);
      else
      {
        _matrix = Matrix.Translation((float) -cx, (float) -cy, 0);
        _matrix *= Matrix.Scaling((float) sx, (float) sy, 1.0f);
        _matrix *= Matrix.Translation((float) cx, (float) cy, 0);
      }
    }
  }
}
