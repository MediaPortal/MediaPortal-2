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
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.XamlParser;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.MpfElements;

namespace Presentation.SkinEngine.Controls.Visuals.Styles      
{
  public class Style: NameScope, IAddChild, IImplicitKey, IDeepCopyable
  {
    #region Private fields

    IList<Setter> _setters;
    Property _targetTypeProperty;

    #endregion

    #region Ctor

    public Style()
    {
      Init();
    }

    void Init()
    {
      _setters = new List<Setter>();
      _targetTypeProperty = new Property(typeof(Type), null);
    }

    public virtual void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Style s = source as Style;
      foreach (Setter se in s._setters)
        _setters.Add(copyManager.GetCopy(se));
      TargetType = copyManager.GetCopy(s.TargetType);
    }

    #endregion

    /// <summary>
    /// Gets or sets the based on property (we dont use it in our xaml engine, but real xaml requires it)
    /// FIXME: New XAML engine will use it!
    /// </summary>
    public Style BasedOn
    {
      get
      {
        return null;
      }
      set
      {
      }
    }

    public Property TargetTypeProperty
    {
      get { return _targetTypeProperty; }
    }

    /// <summary>
    /// Gets or sets the type of the target this setter can be applied to.
    /// </summary>
    public Type TargetType
    {
      get { return _targetTypeProperty.GetValue() as Type; }
      set { _targetTypeProperty.SetValue(value); }
    }

    public FrameworkElement Get()
    {
      foreach (Setter setter in _setters)
      {
        if (setter.Property == "Template")
        {
          FrameworkElement source;
          FrameworkElement element;
          if (setter.Value is FrameworkTemplate)
          {
            source = (FrameworkElement)((FrameworkTemplate)setter.Value).LoadContent();
            element = source;
          }
          else
          {
            source = (FrameworkElement)setter.Value;
            element = MpfCopyManager.DeepCopy(source);
          }
          foreach (Setter setter2 in _setters)
          {
            if (setter2.Property != "Template")
              setter2.Execute(element, null);
          }
          return element;
        }
      }
      return null;
    }

    /// <summary>
    /// Applies this <see cref="Style"/> to the specified <paramref name="element"/>.
    /// </summary>
    /// <param name="element">The element to apply this <see cref="Style"/> to.</param>
    public void Set(UIElement element)
    {
      foreach (Setter setter in _setters)
      {
        setter.Setup(element);
        setter.Execute(element, null);
      }
    }

    #region IAddChild implementation

    public void AddChild(object o)
    {
      _setters.Add((Setter)o);
    }

    #endregion

    #region IImplicitKey implementation

    public object GetImplicitKey()
    {
      return TargetType;
    }

    #endregion
  }
}
