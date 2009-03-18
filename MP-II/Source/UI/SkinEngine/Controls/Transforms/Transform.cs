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

using MediaPortal.Core.General;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.MpfElements;
using MediaPortal.SkinEngine.SkinManagement;
using MediaPortal.Utilities.DeepCopy;
using SlimDX;
using MediaPortal.SkinEngine;

namespace MediaPortal.SkinEngine.Controls.Transforms
{
  public class Transform : DependencyObject, IObservable
  {
    #region Private/protected fields

    protected bool _needUpdate = true;
    protected bool _needUpdateRel = true;
    protected Matrix _matrix = Matrix.Identity;
    protected Matrix _matrixRel = Matrix.Identity;

    #endregion
    
    #region Ctor

    public Transform()
    {
      Attach();
    }

    void Attach()
    {
      SkinContext.ZoomProperty.Attach(OnZoomChanged);
    }

    void Detach()
    {
      SkinContext.ZoomProperty.Detach(OnZoomChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      _needUpdate = true;
      _needUpdateRel = true;
    }

    #endregion

    public event ObjectChangedHandler ObjectChanged;

    #region Protected methods

    protected void OnPropertyChanged(Property property, object oldValue)
    {
      _needUpdate = true;
      _needUpdateRel = true;
      Fire();
    }

    #endregion

    void OnZoomChanged(Property prop, object oldValue)
    {
      _needUpdate = true;
      _needUpdateRel = true; // Albert: Is this line necessary?
    }

    protected void Fire()
    {
      if (ObjectChanged != null)
        ObjectChanged(this);
    }

    public void GetTransform(out ExtendedMatrix m)
    {
      SlimDX.Matrix matrix;
      GetTransform(out matrix);
      m = new ExtendedMatrix();
      m.Matrix *= matrix;
    }

    public void GetTransformRel(out ExtendedMatrix m)
    {
      SlimDX.Matrix matrix;
      GetTransformRel(out matrix);
      m = new ExtendedMatrix();
      m.Matrix *= matrix;
    }
    
    public virtual void GetTransform(out Matrix m)
    {
      if (_needUpdate)
      {
        UpdateTransform();
        _needUpdate = false;
      }
      m = _matrix;
    }

    public virtual void GetTransformRel(out Matrix m)
    {
      if (_needUpdateRel)
      {
        UpdateTransformRel();
        _needUpdateRel = false;
      }
      m = _matrixRel;
    }

    public virtual void UpdateTransform()
    { }

    public virtual void UpdateTransformRel()
    { }
  }
}
