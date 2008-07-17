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
using Presentation.SkinEngine.XamlParser.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.MpfElements;

namespace Presentation.SkinEngine.Controls.Visuals.Styles      
{
  public class Style: NameScope, IAddChild<Setter>, IImplicitKey, IDeepCopyable
  {
    #region Protected fields

    protected Style _basedOn = null;
    protected IList<Setter> _setters;
    protected Property _targetTypeProperty;

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

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Style s = source as Style;
      foreach (Setter se in s._setters)
        _setters.Add(copyManager.GetCopy(se));
      TargetType = copyManager.GetCopy(s.TargetType);
      BasedOn = copyManager.GetCopy(s.BasedOn);
    }

    #endregion

    public Style BasedOn
    {
      get { return _basedOn; }
      set { _basedOn = value; }
    }

    public Property TargetTypeProperty
    {
      get { return _targetTypeProperty; }
    }

    /// <summary>
    /// Gets or sets the type of the target element this style can be applied to.
    /// </summary>
    public Type TargetType
    {
      get { return _targetTypeProperty.GetValue() as Type; }
      set { _targetTypeProperty.SetValue(value); }
    }

    // FIXME Albert78: Rework the TreeView and HeaderedItemsControl methods PrepareItemContainer,
    // which are the only pieces of code using this method. Then remove this method.
    public FrameworkElement Get()
    {
      foreach (Setter setter in _setters)
      {
        if (setter.Property == "Template")
        {
          FrameworkElement element;
          if (setter.Value is FrameworkTemplate)
            element = (FrameworkElement)((FrameworkTemplate)setter.Value).LoadContent();
          else
            element = MpfCopyManager.DeepCopyCutLP(setter.Value) as FrameworkElement;
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
      // We should use a HashSet<string> instead of a list here, but HashSet<T> will
      // be introduced in .net 3.5, and by now we want to be compatible with .net 2.0
      Update(element, new List<string>());
    }

    /// <summary>
    /// Worker method to apply all setters on the specified <paramref name="element"/> which
    /// have not been set yet. The set of properties already assigned will be given in parameter
    /// <paramref name="finishedProperties"/>; all properties whose names are stored in this
    /// parameter will be skipped.
    /// </summary>
    /// <param name="element">The UI element this style will be applied on.</param>
    /// <param name="finishedProperties">Set of property names which should be skipped.</param>
    protected void Update(UIElement element, ICollection<string> finishedProperties)
    {
      foreach (Setter setter in _setters)
      {
        if (finishedProperties.Contains(setter.Property))
          continue;
        finishedProperties.Add(setter.Property);
        setter.Setup(element);
        setter.Execute(element, null);
      }
      if (_basedOn != null)
        _basedOn.Update(element, finishedProperties);
    }

    #region IAddChild implementation

    public void AddChild(Setter o)
    {
      _setters.Add(o);
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
