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
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using MediaPortal.Control.InputManager;
using SkinEngine;
using SkinEngine.DirectX;
using Rectangle = System.Drawing.Rectangle;
using SkinEngine.Controls.Visuals.Styles;
using MyXaml.Core;

namespace SkinEngine.Controls.Visuals
{
  public class FrameworkTemplate : ICloneable, IAddChild
  {
    ResourceDictionary _resourceDictionary;
    UIElement _templateElement;
    Property _keyProperty;
    #region ctor
    public FrameworkTemplate()
    {
      Init();
    }

    public FrameworkTemplate(FrameworkTemplate template)
    {
      Init();
      _templateElement = template._templateElement;
      _resourceDictionary = template._resourceDictionary;
      Key = template.Key;
    }

    public virtual object Clone()
    {
      return new FrameworkTemplate(this);
    }


    void Init()
    {
      _resourceDictionary = new ResourceDictionary();
      _keyProperty = new Property("");
    }
    #endregion

    #region properties

    /// <summary>
    /// Gets or sets the key property.
    /// </summary>
    /// <value>The key property.</value>
    public Property KeyProperty
    {
      get
      {
        return _keyProperty;
      }
      set
      {
        _keyProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    /// <value>The key.</value>
    public string Key
    {
      get
      {
        return _keyProperty.GetValue() as string;
      }
      set
      {
        _keyProperty.SetValue(value);
      }
    }
    public ResourceDictionary Resources
    {
      get
      {
        return _resourceDictionary;
      }
    }
    #endregion

    #region methods
    public UIElement LoadContent(Window w)
    {
      ///@optimize: 
      if (_templateElement == null) return null;
      UIElement element= _templateElement.Clone() as UIElement;
      element.SetWindow(w);
      return element;
    }
    #endregion
    
    #region IAddChild Members

    public void AddChild(object o)
    {
      _templateElement = o as UIElement;
      _templateElement.Resources.Merge(this.Resources);
    }

    #endregion
  }
}
