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
using Presentation.SkinEngine;
using Presentation.SkinEngine.Controls.Resources;
using Presentation.SkinEngine.XamlParser;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Defines a container for UI elements which are used as template controls
  /// for all types of UI-templates. Special template types
  /// like <see cref="ControlTemplate"/> or <see cref="DataTemplate"/> are derived
  /// from this class. This class basically has no other job than holding those
  /// UI elements and cloning them when the template should be applied
  /// (method <see cref="LoadContent(Window)"/>).
  /// </summary>
  /// <remarks>
  /// Templated controls such as <see cref="Button">Buttons</see> or
  /// <see cref="ListView">ListViews</see> implement several properties holding
  /// instances of <see cref="FrameworkTemplate"/>, for each templated feature.
  /// </remarks>
  public class FrameworkTemplate: NameScope, ICloneable, IAddChild
  {
    ResourceDictionary _resourceDictionary;
    UIElement _templateElement;

    #region Ctor

    public FrameworkTemplate()
    {
      Init();
    }

    public FrameworkTemplate(FrameworkTemplate template)
    {
      Init();
      _templateElement = template._templateElement;
      _resourceDictionary = template._resourceDictionary;
    }

    public virtual object Clone()
    {
      FrameworkTemplate result = new FrameworkTemplate(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
    }


    void Init()
    {
      _resourceDictionary = new ResourceDictionary();
    }

    #endregion

    #region properties

    public ResourceDictionary Resources
    {
      get
      {
        return _resourceDictionary;
      }
    }

    #endregion

    #region Public methods

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
      _templateElement.Resources.Merge(Resources);
    }

    #endregion
  }
}
