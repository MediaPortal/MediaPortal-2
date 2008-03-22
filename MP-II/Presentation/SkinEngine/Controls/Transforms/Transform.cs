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
using System;
using System.Collections.Generic;
using System.Text;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.Core;
using MediaPortal.Presentation.Properties;
using MediaPortal.Presentation.WindowManager;
using Presentation.SkinEngine.Controls.Bindings;
using Presentation.SkinEngine;
namespace Presentation.SkinEngine.Controls.Transforms
{
  public class Transform : Property, ICloneable, IBindingCollection
  {
    protected bool _needUpdate = true;
    protected bool _needUpdateRel = true;
    protected Matrix _matrix = Matrix.Identity;
    protected Matrix _matrixRel = Matrix.Identity;
    BindingCollection _bindings;
    bool _initialized;

    public Transform()
    {
      Init();
    }

    public Transform(Transform r)
    {
      Init();
      foreach (Binding b in r._bindings)
      {
        _bindings.Add((Binding)b.Clone());
      }
    }

    public virtual object Clone()
    {
      return new Transform(this);
    }

    void Init()
    {
      _initialized = false;
      _bindings = new BindingCollection();
      WindowManager mgr = (WindowManager)ServiceScope.Get<IWindowManager>();
      mgr.Utils.ZoomProperty.Attach(new PropertyChangedHandler(OnZoomChanged));
    }
    void OnZoomChanged(Property prop)
    {
      _needUpdate = true;
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


    /// <summary>
    /// Gets the transform.
    /// </summary>
    /// <param name="m">The matrix</param>
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

    /// <summary>
    /// Updates the transform.
    /// </summary>
    public virtual void UpdateTransform()
    {
      InitializeBindings();
    }
    public virtual void UpdateTransformRel()
    {
      InitializeBindings();
    }


    #region IBindingCollection Members

    public void Add(Binding binding)
    {
      _bindings.Add(binding);
    }

    public virtual void InitializeBindings()
    {
      if (_initialized) return;
      if (_bindings.Count == 0) return;
      WindowManager mgr = (WindowManager)ServiceScope.Get<IWindowManager>();
      Window window = (Window)mgr.CurrentWindow;
      foreach (Binding binding in _bindings)
      {
        binding.Initialize(this, window.Visual);
      }
      _initialized = true;
    }
    #endregion
  }
}
