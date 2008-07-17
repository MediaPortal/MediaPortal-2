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
using System.Reflection;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.General.Exceptions;

namespace Presentation.SkinEngine.General
{
  using System.Collections;

  public delegate void DataChangedHandler(IDataDescriptor dd);

  /// <summary>
  /// Accessor interface for different kinds of property or data objects.
  /// </summary>
  public interface IDataDescriptor
  {
    /// <summary>
    /// Returns the information, if this data descriptor supports reading
    /// the underlaying data.
    /// </summary>
    bool SupportsRead
    { get; }

    /// <summary>
    /// Returns the information, if this data descriptor supports updating
    /// the underlaying data.
    /// </summary>
    bool SupportsWrite
    { get; }

    /// <summary>
    /// Returns the information, if this data descriptor supports change
    /// notifications for the underlaying value.
    /// </summary>
    bool SupportsChangeNotification
    { get; }

    /// <summary>
    /// Returns the information, if this data descriptor supports operations
    /// on the target object where the data property is defined.
    /// This will true for data descriptors
    /// which specify a property or dependency property.
    /// </summary>
    bool SupportsTargetOperations
    { get; }

    /// <summary>
    /// Represents the value of this data descriptor.
    /// Instances which return <c><see cref="SupportsRead"/> == true</c> will be able
    /// to get the value, those with <c><see cref="SupportsWrite"/> == true</c> support to
    /// set it.
    /// </summary>
    object Value
    { get; set; }

    /// <summary>
    /// Returns the type of the value to be get/set by this instance.
    /// </summary>
    Type DataType
    { get; }

    /// <summary>
    /// Returns the target object of the property described by this instance, if
    /// it is based on a target object. This may be called if
    /// <c><see cref="SupportsTargetOperations"/> == true</c>.
    /// </summary>
    object TargetObject
    { get; }

    /// <summary>
    /// Copies this data descriptor and exchanges the underlaying target object.
    /// The returned data descriptor will target the same property as the old,
    /// but on the <paramref name="newTarget"/> object. This method
    /// may be called if <c><see cref="SupportsTargetOperations"/> == true</c>.
    /// </summary>
    /// <param name="newTarget">The object the copied data descriptor
    /// will target. This parameter has to be compatible with the property
    /// defined by this data descriptor, (for example <paramref name="newTarget"/>
    /// of the same class as the old (<see cref="TargetObject"/>).</param>
    /// <returns>A copy of this data descriptor which is based on the
    /// <paramref name="newTarget"/> object.</returns>
    IDataDescriptor Retarget(object newTarget);

    /// <summary>
    /// Attaches an event handler for the change event. This method
    /// may be called if <c><see cref="SupportsChangeNotification"/> == true</c>.
    /// </summary>
    /// <param name="handler">The handler to attach.</param>
    void Attach(DataChangedHandler handler);

    /// <summary>
    /// Detaches the specified event handler. This method
    /// may be called if <c><see cref="SupportsChangeNotification"/> == true</c>.
    /// </summary>
    /// <param name="handler">The handler to detach.</param>
    void Detach(DataChangedHandler handler);
  }

  /// <summary>
  /// A data descriptor with an underlaying read-only value object.
  /// Just supports reading the value.
  /// </summary>
  public class ValueDataDescriptor : IDataDescriptor
  {
    #region Protected fields

    protected object _value;

    #endregion

    #region Ctor

    public ValueDataDescriptor(object value)
    {
      _value = value;
    }

    #endregion

    #region IDataDescriptor implementation

    public bool SupportsRead
    {
      get { return true; }
    }

    public bool SupportsWrite
    {
      get { return false; }
    }

    public bool SupportsChangeNotification
    {
      get { return false; }
    }

    public bool SupportsTargetOperations
    {
      get { return false; }
    }

    public object Value
    {
      get { return _value; }
      set { throw new Exception("Update not supported in ValueDataDescriptor"); }
    }

    public Type DataType
    {
      get { return _value == null ? null : _value.GetType();  }
    }

    public object TargetObject
    {
      // Not supported
      get { return null; }
    }

    public IDataDescriptor Retarget(object newTarget)
    {
      // Not supported
      return null;
    }

    public void Attach(DataChangedHandler handler)
    {
      // Not supported
    }

    public void Detach(DataChangedHandler handler)
    {
      // Not supported
    }

    #endregion
  }

  /// <summary>
  /// A data descriptor with an underlaying list object and an index to
  /// access that list.
  /// Supports reading and writing the indexed list value.
  /// </summary>
  public class ListIndexerDataDescriptor : IDataDescriptor
  {
    #region Protected fields

    protected IList _list;
    protected int _index;

    #endregion

    #region Ctor

    public ListIndexerDataDescriptor(IList list, int index)
    {
      _list = list;
      _index = index;
    }

    #endregion

    #region IDataDescriptor implementation

    public bool SupportsRead
    {
      get { return true; }
    }

    public bool SupportsWrite
    {
      get { return true; }
    }

    public bool SupportsChangeNotification
    {
      get { return false; }
    }

    public bool SupportsTargetOperations
    {
      get { return false; }
    }

    public object Value
    {
      get { return _list[_index]; }
      set { _list[_index] = value; }
    }

    public Type DataType
    {
      get
      {
        object val = Value;
        return val == null ? null : val.GetType();
      }
    }

    public object TargetObject
    {
      // Not supported
      get { return null; }
    }

    public IDataDescriptor Retarget(object newTarget)
    {
      // Not supported
      return null;
    }

    public void Attach(DataChangedHandler handler)
    {
      // Not supported
    }

    public void Detach(DataChangedHandler handler)
    {
      // Not supported
    }

    #endregion
  }

  /// <summary>
  /// A data descriptor with an underlaying simple property.
  /// Supports reading and writing of the value, if the underlaying
  /// property supports those operations.
  /// </summary>
  public class SimplePropertyDataDescriptor : IDataDescriptor
  {
    #region Protected fields

    protected object _obj;
    protected PropertyInfo _prop;
    protected object[] _indices = null;

    #endregion

    #region Ctor & static methods

    public SimplePropertyDataDescriptor(object obj, PropertyInfo prop)
    {
      _obj = obj;
      _prop = prop;
    }

    public static bool CreateSimplePropertyDataDescriptor(object targetObj,
        string propertyName, out SimplePropertyDataDescriptor result)
    {
      result = null;
      if (targetObj == null)
        throw new NullReferenceException("Target object 'null' is not supported");
      PropertyInfo pi;
      if (!FindSimpleProperty(targetObj.GetType(), propertyName, out pi))
        return false;
      result = new SimplePropertyDataDescriptor(targetObj, pi);
      return true;
    }

    public static bool FindSimpleProperty(Type type, string propertyName, out PropertyInfo pi)
    {
      pi = type.GetProperty(propertyName);
      return pi != null;
    }

    #endregion

    #region IDataDescriptor implementation

    public bool SupportsRead
    {
      get { return _prop.CanRead; }
    }

    public bool SupportsWrite
    {
      get { return _prop.CanWrite; }
    }

    public bool SupportsChangeNotification
    {
      get { return false; }
    }

    public bool SupportsTargetOperations
    {
      get { return true; }
    }

    public object Value
    {
      get { return _prop.GetValue(_obj, _indices); }
      set { _prop.SetValue(_obj, value, _indices); }
    }

    public Type DataType
    {
      get { return _prop.PropertyType; }
    }

    public object[] Indices
    {
      get { return _indices;  }
      set { _indices = value; }
    }

    public object TargetObject
    {
      get { return _obj; }
    }

    public IDataDescriptor Retarget(object newTarget)
    {
      if (newTarget == null)
        throw new NullReferenceException("Target object 'null' is not supported");
      if (!_prop.DeclaringType.IsAssignableFrom(newTarget.GetType()))
        throw new InvalidOperationException(string.Format(
            "Type of new target object is not compatible with this property descriptor (expected type: {0}, new target type: {1}",
            _prop.DeclaringType.Name, newTarget.GetType().Name));
      SimplePropertyDataDescriptor result = new SimplePropertyDataDescriptor(newTarget, _prop);
      result.Indices = _indices;
      return result;
    }

    public void Attach(DataChangedHandler handler)
    {
      // Not supported
    }

    public void Detach(DataChangedHandler handler)
    {
      // Not supported
    }

    #endregion
  }

  /// <summary>
  /// A data descriptor with an underlaying dependency property.
  /// Supports reading and writing of the value as well as change notifications.
  /// </summary>
  public class DependencyPropertyDataDescriptor : IDataDescriptor
  {

    #region Protected fields

    protected object _obj;
    protected string _propertyName;
    protected Property _prop;
    protected event DataChangedHandler _valueChanged;
    protected bool _attachedToProperty = false;

    #endregion

    #region Ctor & static methods

    public DependencyPropertyDataDescriptor(object obj, string propertyName, Property prop)
    {
      _obj = obj;
      _propertyName = propertyName;
      _prop = prop;
    }

    public static bool CreateDependencyPropertyDataDescriptor(object targetObj,
        string propertyName, out DependencyPropertyDataDescriptor result)
    {
      result = null;
      if (targetObj == null)
        throw new NullReferenceException("Target object 'null' is not supported");
      Property prop;
      if (!FindDependencyProperty(targetObj, ref propertyName, out prop))
        return false;
      result = new DependencyPropertyDataDescriptor(targetObj, propertyName, prop);
      return true;
    }

    public static bool FindDependencyProperty(object obj, ref string propertyName, out Property prop)
    {
      prop = null;
      PropertyInfo pi;
      string name;
      if (!SimplePropertyDataDescriptor.FindSimpleProperty(
          obj.GetType(), name = (propertyName + "Property"), out pi))
        if (!SimplePropertyDataDescriptor.FindSimpleProperty(obj.GetType(), name = propertyName, out pi))
          return false;
      if (typeof(Property).IsAssignableFrom(pi.PropertyType))
      {
        prop = pi.GetValue(obj, null) as Property;
        if (prop == null)
          throw new XamlBindingException("Member {0}.{1} doesn't return a Property instance on object '{2}'",
            obj.GetType().Name, name, obj.ToString());
        propertyName = name;
        return true;
      }
      return false;
    }

    #endregion

    #region Protected methods

    protected void OnPropertyChanged(Property property)
    {
      if (_valueChanged != null)
        // Delegate to our handlers
        _valueChanged(this);
    }

    #endregion

    #region IDataDescriptor implementation

    public bool SupportsRead
    {
      get { return true; }
    }

    public bool SupportsWrite
    {
      get { return true; }
    }

    public bool SupportsChangeNotification
    {
      get { return true; }
    }

    public bool SupportsTargetOperations
    {
      get { return true; }
    }

    public object Value
    {
      get { return _prop.GetValue(); }
      set { _prop.SetValue(value); }
    }

    public Type DataType
    {
      get { return _prop.PropertyType; }
    }

    public object TargetObject
    {
      get { return _obj; }
    }

    public virtual IDataDescriptor Retarget(object newTarget)
    {
      DependencyPropertyDataDescriptor result;
      if (!CreateDependencyPropertyDataDescriptor(newTarget, _propertyName, out result))
        throw new InvalidOperationException(string.Format(
            "New target object '{0}' has no property '{1}'", newTarget, _propertyName));
      return result;
    }

    public void Attach(DataChangedHandler handler)
    {
      _valueChanged += handler;
      if (!_attachedToProperty)
        _prop.Attach(OnPropertyChanged);
      _attachedToProperty = true;
    }

    public void Detach(DataChangedHandler handler)
    {
      _valueChanged -= handler;
      if (_valueChanged == null)
      {
        _prop.Detach(OnPropertyChanged);
        _attachedToProperty = false;
      }
    }

    #endregion
  }

  /// <summary>
  /// Repeater class to attach targets to source values via the
  /// <see cref="IDataDescriptor"/> interface. This class supports
  /// to exchange the source value while preserving all change listeners
  /// on the actual instance. It's also possible for clients to bind to the
  /// acutal instance, while the source value is not available yet.
  /// </summary>
  public class DataDescriptorRepeater : IDataDescriptor
  {
    #region Protected fields

    protected IDataDescriptor _value = null;
    protected event DataChangedHandler _valueChanged;

    #endregion

    #region Public properties

    public IDataDescriptor SourceValue
    {
      get { return _value; }
      set
      {
        if (_value != null && _value.SupportsChangeNotification)
          _value.Detach(OnSourceValueChange);
        bool valueChanged = (_value != null && value != null) ? _value.Value != value.Value: _value != value;
        _value = value;
        if (_value != null && _value.SupportsChangeNotification)
          _value.Attach(OnSourceValueChange);
        if (valueChanged && _valueChanged != null)
          _valueChanged(this);
      }
    }

    #endregion

    #region Protected methods

    protected void OnSourceValueChange(IDataDescriptor dd)
    {
      if (_valueChanged != null)
        _valueChanged(this);
    }

    #endregion

    #region IDataDescriptor implementation

    public bool SupportsRead
    {
      get { return _value != null && _value.SupportsRead; }
    }

    public bool SupportsWrite
    {
      get { return _value != null && _value.SupportsWrite; }
    }

    public bool SupportsChangeNotification
    {
      get { return true; }
    }

    public bool SupportsTargetOperations
    {
      get { return false; }
    }

    public object Value
    {
      get { return _value == null ? null : _value.Value; }
      set
      {
        if (_value != null)
          _value.Value = value;
      }
    }

    public Type DataType
    {
      get { return _value == null ? typeof(object) : _value.DataType; }
    }

    public object TargetObject
    {
      // Not supported
      get { return null; }
    }

    public IDataDescriptor Retarget(object newTarget)
    {
      // Not supported
      return null;
    }

    public void Attach(DataChangedHandler handler)
    {
      _valueChanged += handler;
    }

    public void Detach(DataChangedHandler handler)
    {
      _valueChanged -= handler;
    }

    #endregion
  }
}
