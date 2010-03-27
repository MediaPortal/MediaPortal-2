#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/
#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Styles      
{
  public class Style: INameScope, IAddChild<SetterBase>, IImplicitKey
  {
    #region Protected fields

    protected IDictionary<string, object> _names = new Dictionary<string, object>();

    protected Style _basedOn = null;
    protected IList<SetterBase> _setters = new List<SetterBase>();
    protected AbstractProperty _targetTypeProperty;
    protected ResourceDictionary _resources;

    #endregion

    #region Ctor

    public Style()
    {
      Init();
    }

    void Init()
    {
      _targetTypeProperty = new SProperty(typeof(Type), null);
      _resources = new ResourceDictionary();
    }

    #endregion

    public Style BasedOn
    {
      get { return _basedOn; }
      set { _basedOn = value; }
    }

    public AbstractProperty TargetTypeProperty
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

    public ResourceDictionary Resources
    {
      get { return _resources; }
    }

    /// <summary>
    /// Applies this <see cref="Style"/> to the specified <paramref name="element"/>.
    /// </summary>
    /// <param name="element">The element to apply this <see cref="Style"/> to.</param>
    public void Set(UIElement element)
    {
      MergeResources(element);
      Update(element, new HashSet<string>());
    }

    protected void MergeResources(UIElement element)
    {
      if (_basedOn != null)
        _basedOn.MergeResources(element);
      // Merge resources with those from the target element
      element.Resources.Merge(Resources);
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
      foreach (SetterBase sb in _setters)
      {
        if (finishedProperties.Contains(sb.Property))
          continue;
        finishedProperties.Add(sb.Property);
        sb.Set(element);
      }
      if (_basedOn != null)
        _basedOn.Update(element, finishedProperties);
    }

    #region INamescope implementation

    public object FindName(string name)
    {
      if (_names.ContainsKey(name))
        return _names[name];
      return null;
    }

    public void RegisterName(string name, object instance)
    {
      _names.Add(name, instance);
    }

    public void UnregisterName(string name)
    {
      _names.Remove(name);
    }

    #endregion

    #region IAddChild implementation

    public void AddChild(SetterBase sb)
    {
      _setters.Add(sb);
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
