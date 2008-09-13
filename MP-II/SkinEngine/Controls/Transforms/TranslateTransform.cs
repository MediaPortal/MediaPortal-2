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

using MediaPortal.Presentation.DataObjects;
using SlimDX;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Transforms
{
  public class TranslateTransform : Transform
  {
    #region Private fields

    Property _XProperty;
    Property _YProperty;

    #endregion

    #region Ctor

    public TranslateTransform()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _YProperty = new Property(typeof(double), 0.0);
      _XProperty = new Property(typeof(double), 0.0);
    }

    void Attach()
    {
      _YProperty.Attach(OnPropertyChanged);
      _XProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _YProperty.Detach(OnPropertyChanged);
      _XProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      TranslateTransform t = (TranslateTransform) source;
      X = copyManager.GetCopy(t.X);
      Y = copyManager.GetCopy(t.Y);
      Attach();
    }

    #endregion

    protected void OnPropertyChanged(Property property)
    {
      _needUpdate = true;
      Fire();
    }

    public Property XProperty
    {
      get { return _XProperty; }
    }

    public double X
    {
      get { return (double)_XProperty.GetValue(); }
      set { _XProperty.SetValue(value); }
    }

    public Property YProperty
    {
      get { return _YProperty; }
    }

    public double Y
    {
      get { return (double)_YProperty.GetValue(); }
      set { _YProperty.SetValue(value); }
    }

    public override void UpdateTransform()
    {
      base.UpdateTransform();
      _matrix = Matrix.Translation((float)X * SkinContext.Zoom.Width, (float)Y * SkinContext.Zoom.Width, 0);
    }

    public override void UpdateTransformRel()
    {
      base.UpdateTransformRel();
      _matrixRel = Matrix.Translation((float)X , (float)Y , 0);
    }
  }
}
