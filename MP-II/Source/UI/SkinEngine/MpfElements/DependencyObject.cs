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
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.MarkupExtensions;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.Xaml;
using MediaPortal.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.SkinEngine.Controls
{

  /// <summary>
  /// Represents an object which can contain foreign attached properties.
  /// This class also implements the <see cref="DependencyObject.DataContext"/>
  /// which is needed for
  /// <see cref="MediaPortal.SkinEngine.MarkupExtensions.BindingMarkupExtension">bindings</see>.
  /// </summary>
  public class DependencyObject: IDeepCopyable, IInitializable
  {
    #region Protected fields

    protected ICollection<BindingBase> _bindings = null;
    protected IDictionary<string, Property> _attachedProperties = null; // Lazy initialized
    protected Property _dataContextProperty;
    protected Property _logicalParentProperty;

    #endregion

    #region Ctor

    public DependencyObject()
    {
      Init();
    }

    void Init()
    {
      _dataContextProperty = new Property(typeof(BindingMarkupExtension), null);
      _logicalParentProperty = new Property(typeof(DependencyObject), null);
    }

    public virtual void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      DependencyObject d = (DependencyObject) source;
      if (d._attachedProperties != null)
        foreach (KeyValuePair<string, Property> kvp in d._attachedProperties)
          AddAttachedProperty(kvp.Key, copyManager.GetCopy(kvp.Value.GetValue()), kvp.Value.PropertyType);
      DataContext = copyManager.GetCopy(d.DataContext);
      LogicalParent = copyManager.GetCopy(d.LogicalParent);
      if (d._bindings != null)
        foreach (BindingBase binding in d._bindings)
          copyManager.GetCopy(binding);
    }

    #endregion

    #region Public properties

    public Property DataContextProperty
    {
      get { return _dataContextProperty; }
    }

    /// <summary>
    /// Gets or sets the data context binding.
    /// </summary>
    public BindingMarkupExtension DataContext
    {
      get { return (BindingMarkupExtension) _dataContextProperty.GetValue(); }
      set { _dataContextProperty.SetValue(value); }
    }

    public Property LogicalParentProperty
    {
      get { return _logicalParentProperty; }
    }

    public DependencyObject LogicalParent
    {
      get { return (DependencyObject) _logicalParentProperty.GetValue(); }
      set { _logicalParentProperty.SetValue(value); }
    }

    #endregion

    public BindingMarkupExtension GetOrCreateDataContext()
    {
      if (DataContext == null)
        DataContext = new BindingMarkupExtension(this);
      return DataContext;
    }

    public ICollection<BindingBase> GetOrCreateBindingCollection()
    {
      if (_bindings == null)
        _bindings = new List<BindingBase>();
      return _bindings;
    }

    public virtual INameScope FindNameScope()
    {
      if (this is INameScope)
        return this as INameScope;
      return LogicalParent == null ? null : LogicalParent.FindNameScope();
    }

    #region Attached properties implementation

    public void SetAttachedPropertyValue<T>(string name, T value)
    {
      Property result = GetAttachedProperty(name);
      if (result == null)
        AddAttachedProperty(name, value, typeof(T));
      else
        result.SetValue(value);
    }

    public T GetAttachedPropertyValue<T>(string name, T defaultValue)
    {
      Property property = GetAttachedProperty(name);
      return property == null ? defaultValue : (T) property.GetValue();
    }

    public Property GetAttachedProperty(string name)
    {
      if (_attachedProperties != null && _attachedProperties.ContainsKey(name))
        return _attachedProperties[name];
      return null;
    }

    public Property GetOrCreateAttachedProperty<T>(string name, T defaultValue)
    {
      Property result = GetAttachedProperty(name);
      if (result == null)
        result = AddAttachedProperty(name, defaultValue, typeof(T));
      return result;
    }

    public void RemoveAttachedProperty(string name)
    {
      if (_attachedProperties == null)
        return;
      _attachedProperties.Remove(name);
    }

    private Property AddAttachedProperty(string name, object value, Type t)
    {
      if (_attachedProperties == null)
        _attachedProperties = new Dictionary<string, Property>();
      Property result = new Property(t);
      result.SetValue(value);
      _attachedProperties[name] = result;
      return result;
    }

    #endregion

    #region IInitializable implementation

    public virtual void Initialize(IParserContext context)
    {
      IEnumerator<ElementContextInfo> eeci = (context.ContextStack as IEnumerable<ElementContextInfo>).GetEnumerator();
      if (eeci.MoveNext() && eeci.MoveNext())
        LogicalParent = eeci.Current.Instance as DependencyObject;
    }

    #endregion
  }
}
