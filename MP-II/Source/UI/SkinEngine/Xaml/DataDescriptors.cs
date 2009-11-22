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

using System;

using System.Reflection;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.Utilities;

namespace MediaPortal.UI.SkinEngine.Xaml
{
  public delegate void DataChangedHandler(IDataDescriptor dd);

  /// <summary>
  /// Accessor interface for different kinds of property or data objects.
  /// </summary>
  /// <remarks>
  /// Implementing classes have to implement the <see cref="object.Equals(object)"/> method in that way,
  /// that it returns <c>true</c> if and only if the same property is targeted.
  /// For implementing classes with <c><see cref="SupportsTargetOperations"/>==false</c>,
  /// the <see cref="object.Equals(object)"/> method should base its comparison on the equality of the
  /// <see cref="Value"/> property.
  /// </remarks>
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

    /// <summary>
    /// Returns the information if the specified <paramref name="other"/> descriptor
    /// references the same <see cref="Value"/>.
    /// </summary>
    /// <param name="other">Other descriptor whose <see cref="Value"/> should be compared.</param>
    public bool ValueEquals(ValueDataDescriptor other)
    {
      return _value == null ? (other == null) : _value.Equals(other._value);
    }

    public override int GetHashCode()
    {
      return _value == null ? 0 : _value.GetHashCode();
    }

    public override bool Equals(object other)
    {
      if (other is ValueDataDescriptor)
        return ValueEquals((ValueDataDescriptor) other);
      else
        return false;
    }
  }

  /// <summary>
  /// A data descriptor which applies indices to an underlaying object. The object can be a
  /// list, but also can be any other object with a <c>this[]</c> oberator.
  /// Supports reading and writing the indexed value if the underlaying indexer supports
  /// those operations. Also supports target operations.
  /// </summary>
  public class IndexerDataDescriptor : IDataDescriptor
  {
    #region Protected fields

    protected object _target;
    protected object[] _indices;

    #endregion

    #region Ctor

    public IndexerDataDescriptor(object target, object[] indices)
    {
      if (target == null)
        throw new ArgumentNullException("Target object for indexer cannot be null");
      _target = target;
      if (!IndicesCompatible(_target.GetType(), indices))
        throw new ArgumentException("Indices are not compatible with indexer parameters");
      _indices = indices;
    }

    #endregion

    #region IDataDescriptor implementation

    public bool SupportsRead
    {
      get
      {
        PropertyInfo itemPi = GetIndexerPropertyInfo(_target.GetType());
        return itemPi == null ? false : itemPi.CanRead;
      }
    }

    public bool SupportsWrite
    {
      get
      {
        PropertyInfo itemPi = GetIndexerPropertyInfo(_target.GetType());
        return itemPi == null ? false : itemPi.CanWrite;
      }
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
      get
      {
        PropertyInfo itemPi = GetIndexerPropertyInfo(_target.GetType());
        try
        {
          return itemPi == null ? null : itemPi.GetValue(_target, _indices);
        }
        catch (Exception e)
        {
          throw new XamlBindingException("Unable to evaluate index expression '{0}' on object '{1}'", e, StringUtils.Join(", ", _indices), _target);
        }

      }
      set
      {
        try
        {
          PropertyInfo itemPi = GetIndexerPropertyInfo(_target.GetType());
          if (itemPi == null)
            return;
          itemPi.SetValue(_target, value, _indices);
        }
        catch (Exception e)
        {
          throw new XamlBindingException("Unable to set value '{0}' on object '{1}' with index expression '{2}'", e, value, _target, StringUtils.Join(", ", _indices));
        }
      }
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
      get { return _target; }
    }

    public IDataDescriptor Retarget(object newTarget)
    {
      if (newTarget == null)
        throw new NullReferenceException("Target object 'null' is not supported");
      if (!IndicesCompatible(newTarget.GetType(), _indices))
        throw new InvalidOperationException(
            "Type of new target object is not compatible with the indices of this property descriptor");
      IndexerDataDescriptor result = new IndexerDataDescriptor(newTarget, _indices);
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

    protected static bool IndicesCompatible(Type t, object[] indices)
    {
      ParameterInfo[] pis = GetIndexerTypes(t);
      int i = 0;
      for (; i < pis.Length; i++)
      {
        object index = indices[i];
        ParameterInfo pi = pis[i];
        if (indices.Length <= i && pi.IsOptional)
          break;
        if (!pi.ParameterType.IsAssignableFrom(index == null ? null :index.GetType()))
          return false;
      }
      if (i < indices.Length)
        return false;
      return true;
    }

    public static ParameterInfo[] GetIndexerTypes(Type t)
    {
      PropertyInfo pi = GetIndexerPropertyInfo(t);
      return pi == null ? null : pi.GetIndexParameters();
    }

    public static PropertyInfo GetIndexerPropertyInfo(Type t)
    {
      return t.GetProperty("Item");
    }

    public bool IndicesEquals(object[] otherIndices)
    {
      if (_indices == null)
        return otherIndices == null;
      else if (otherIndices == null)
        return false;
      if (_indices.GetLength(0) != otherIndices.GetLength(0))
        return false;
      for (int i = 0; i < _indices.GetLength(0); i++)
        if (!_indices[i].Equals(otherIndices[i]))
          return false;
      return true;
    }
  
    /// <summary>
    /// Returns the information if the specified <paramref name="other"/> descriptor
    /// is targeted at the same list index on the same list.
    /// </summary>
    /// <param name="other">Other descriptor whose target object and property should be compared.</param>
    public bool TargetEquals(IndexerDataDescriptor other)
    {
      return _target.Equals(other._target) && IndicesEquals(other._indices);
    }

    public override int GetHashCode()
    {
      return _target.GetHashCode()+_indices.GetHashCode();
    }

    public override bool Equals(object other)
    {
      if (other is IndexerDataDescriptor)
        return TargetEquals((IndexerDataDescriptor) other);
      else
        return false;
    }
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
      if (obj == null)
        throw new ArgumentNullException("obj", "Target object for property access cannot be null");
      if (prop == null)
        throw new ArgumentNullException("prop", "Property type cannot be null");
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
      get
      {
        try
        {
          return _prop.GetValue(_obj, _indices);
        }
        catch (Exception e)
        {
          throw new XamlBindingException("Unable to get property '{0}' on object '{1}' (Indices: '{2}')", e, _prop, _obj, StringUtils.Join(", ", _indices));
        }
      }
      set
      {
        try
        {
          _prop.SetValue(_obj, value, _indices);
        }
        catch (Exception e)
        {
          throw new XamlBindingException("Unable to set value '{0}' on object '{1}', property '{2}' (Indices: '{3}')", e, value, _obj, _prop, StringUtils.Join(", ", _indices));
        }
      }
    }

    public Type DataType
    {
      get { return _prop.PropertyType; }
    }

    public object[] Indices
    {
      get { return _indices; }
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

    public bool IndicesEquals(object[] otherIndices)
    {
      if (_indices == null)
        return otherIndices == null;
      else if (otherIndices == null)
        return false;
      if (_indices.GetLength(0) != otherIndices.GetLength(0))
        return false;
      for (int i = 0; i < _indices.GetLength(0); i++)
        if (!_indices[i].Equals(otherIndices[i]))
          return false;
      return true;
    }

    /// <summary>
    /// Returns the information if the specified <paramref name="other"/> descriptor
    /// is targeted at the same property on the same object.
    /// </summary>
    /// <param name="other">Other descriptor whose target object and property should be compared.</param>
    public bool TargetEquals(SimplePropertyDataDescriptor other)
    {
      return _obj.Equals(other._obj) && _prop.Equals(other._prop) && IndicesEquals(other._indices);
    }

    public override int GetHashCode()
    {
      int sum = 0;
      if (_indices != null)
        foreach (object o in _indices)
          sum += o.GetHashCode();
      return _obj.GetHashCode() + _prop.GetHashCode() + sum;
    }

    public override bool Equals(object other)
    {
      if (other is SimplePropertyDataDescriptor)
        return TargetEquals((SimplePropertyDataDescriptor) other);
      else
        return false;
    }
  }

  /// <summary>
  /// A data descriptor with an underlaying field.
  /// Supports reading and writing of the value.
  /// </summary>
  public class FieldDataDescriptor : IDataDescriptor
  {
    #region Protected fields

    protected object _obj;
    protected FieldInfo _fld;

    #endregion

    #region Ctor & static methods

    public FieldDataDescriptor(object obj, FieldInfo fld)
    {
      if (obj == null)
        throw new ArgumentNullException("obj", "Target object for field access cannot be null");
      if (fld == null)
        throw new ArgumentNullException("fld", "Field type cannot be null");
      _obj = obj;
      _fld = fld;
    }

    public static bool CreateFieldDataDescriptor(object targetObj,
        string fieldName, out FieldDataDescriptor result)
    {
      result = null;
      if (targetObj == null)
        throw new NullReferenceException("Target object 'null' is not supported");
      FieldInfo fi;
      if (!FindField(targetObj.GetType(), fieldName, out fi))
        return false;
      result = new FieldDataDescriptor(targetObj, fi);
      return true;
    }

    public static bool FindField(Type type, string fieldName, out FieldInfo fi)
    {
      fi = type.GetField(fieldName);
      return fi != null;
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
      get { return true; }
    }

    public object Value
    {
      get
      {
        try
        {
          return _fld.GetValue(_obj);
        }
        catch (Exception e)
        {
          throw new XamlBindingException("Unable to get field '{0}' on object '{1}'", e, _fld, _obj);
        }
      }
      set
      {
        try
        {
          _fld.SetValue(_obj, value);
        }
        catch (Exception e)
        {
          throw new XamlBindingException("Unable to get field '{0}' on object '{1}'", e, _fld, _obj);
        }
      }
    }

    public Type DataType
    {
      get { return _fld.FieldType; }
    }

    public object TargetObject
    {
      get { return _obj; }
    }

    public IDataDescriptor Retarget(object newTarget)
    {
      if (newTarget == null)
        throw new NullReferenceException("Target object 'null' is not supported");
      if (!_fld.DeclaringType.IsAssignableFrom(newTarget.GetType()))
        throw new InvalidOperationException(string.Format(
            "Type of new target object is not compatible with this property descriptor (expected type: {0}, new target type: {1}",
            _fld.DeclaringType.Name, newTarget.GetType().Name));
      FieldDataDescriptor result = new FieldDataDescriptor(newTarget, _fld);
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

    /// <summary>
    /// Returns the information if the specified <paramref name="other"/> descriptor
    /// is targeted at the same field on the same object.
    /// </summary>
    /// <param name="other">Other descriptor whose target object and field should be compared.</param>
    public bool TargetEquals(FieldDataDescriptor other)
    {
      return _obj.Equals(other._obj) && _fld.Equals(other._fld);
    }

    public override int GetHashCode()
    {
      return _obj.GetHashCode() + _fld.GetHashCode();
    }

    public override bool Equals(object other)
    {
      if (other is FieldDataDescriptor)
        return TargetEquals((FieldDataDescriptor) other);
      else
        return false;
    }
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
      if (obj == null)
        throw new ArgumentNullException("obj", "Target object for dependency property access cannot be null");
      if (propertyName == null)
        throw new ArgumentNullException("propertyName", "Property name cannot be null");
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

    protected void OnPropertyChanged(Property property, object oldValue)
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

    /// <summary>
    /// Returns the information if the specified <paramref name="other"/> descriptor
    /// is targeted at the same property on the same object.
    /// </summary>
    /// <param name="other">Other descriptor whose target object and property should be compared.</param>
    public bool TargetEquals(DependencyPropertyDataDescriptor other)
    {
      return _obj.Equals(other._obj) && _propertyName.Equals(other._propertyName) && _prop.Equals(other._prop);
    }

    public override int GetHashCode()
    {
      return _obj.GetHashCode() + _propertyName.GetHashCode() + _prop.GetHashCode();
    }

    public override bool Equals(object other)
    {
      if (other is DependencyPropertyDataDescriptor)
        return TargetEquals((DependencyPropertyDataDescriptor) other);
      else
        return false;
    }

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
    protected bool _negate = false;

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

    public bool Negate
    {
      get { return _negate; }
      set
      {
        if (value == _negate)
          return;
        _negate = value;
        OnSourceValueChange(null);
      }
    }

    #endregion

    #region Protected methods

    protected void OnSourceValueChange(IDataDescriptor dd)
    {
      if (_valueChanged != null)
        _valueChanged(this);
    }

    protected object Convert(object source)
    {
      if (!_negate)
        return source;
      // If negate, we need to convert to bool
      object value = false;
      if (source != null)
      {
        value = source;
        if (!TypeConverter.Convert(value, typeof(bool), out value))
          return value != null;
      }
      return !(bool) value;
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
      get { return _value == null ? null : Convert(_value.Value); }
      set
      {
        if (_value != null)
          _value.Value = Convert(value);
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

    /// <summary>
    /// Returns the information if the specified <paramref name="other"/> descriptor
    /// is based on the same underlaying target descriptor.
    /// </summary>
    /// <param name="other">Other descriptor whose target descriptor should be compared.</param>
    public bool TargetEquals(DataDescriptorRepeater other)
    {
      return _value.Equals(other._value);
    }

    public override int GetHashCode()
    {
      return _value.GetHashCode();
    }

    public override bool Equals(object other)
    {
      if (other is DataDescriptorRepeater)
        return TargetEquals((DataDescriptorRepeater) other);
      else
        return false;
    }
  }
}
