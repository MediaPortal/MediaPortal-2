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
  public class TranslateTransform : Transform
  {
    #region Private fields

    AbstractProperty _XProperty;
    AbstractProperty _YProperty;

    #endregion

    #region Ctor

    public TranslateTransform()
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
      _YProperty = new SProperty(typeof(double), 0.0);
      _XProperty = new SProperty(typeof(double), 0.0);
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

    protected void OnPropertyChanged(AbstractProperty property)
    {
      _needUpdate = true;
      Fire();
    }

    public AbstractProperty XProperty
    {
      get { return _XProperty; }
    }

    public double X
    {
      get { return (double)_XProperty.GetValue(); }
      set { _XProperty.SetValue(value); }
    }

    public AbstractProperty YProperty
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
