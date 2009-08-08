using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Utilities.Exceptions;

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Base UPnP state variable class.
  /// Either subclasses can be implemented for a special state variable of a concrete UPnP service or instances of this class
  /// can be created and configured from outside.
  /// </summary>
  public class DvStateVariable
  {
    protected string _name;
    protected DvDataType _dataType;
    protected DvService _parentService = null;
    protected bool _sendEvents = true;
    protected bool _multicast = false;
    protected string _multicastEventLevel = Consts.MEL_UPNP_INFO;
    protected object _defaultValue = null; // Initial value of this state variable
    protected object _value = null;
    protected IList<string> _allowedValueList = null; // Only allowed for string data type
    protected DvAllowedValueRange _allowedValueRange = null; // Only allowed for numeric data types
    protected TimeSpan? _moderatedMaximumRate = null;
    protected double _moderatedMinimumDelta = 0;

    /// <summary>
    /// Creates a new instance of <see cref="DvStateVariable"/>.
    /// </summary>
    /// <param name="name">Name of the variable.</param>
    /// <param name="dataType">Data type of the variable.</param>
    public DvStateVariable(string name, DvDataType dataType)
    {
      _name = name;
      _dataType = dataType;
    }

    /// <summary>
    /// Returns the name of this state variable.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Returns the service, this state variable belongs to. Will be assigned by method <see cref="DvService.AddStateVariable"/>.
    /// </summary>
    public DvService ParentService
    {
      get { return _parentService; }
      internal set { _parentService = value; }
    }

    /// <summary>
    /// Gets or sets the SendEvents flag of this state variable.
    /// TODO: Documentation for this flag
    /// </summary>
    public bool SendEvents
    {
      get{ return _sendEvents; }
      set { _sendEvents = value; }
    }

    /// <summary>
    /// Gets or sets the Multicast flag of this state variable.
    /// TODO: Documentation for this flag
    /// </summary>
    public bool Multicast
    {
      get { return _multicast; }
      set { _multicast = value; }
    }

    /// <summary>
    /// Event level for multicasted state variables. If <see cref="Multicast"/> is set to <c>true</c>, this event level must be
    /// set. Default event levels are defined in the MEL_XXX constants in the <see cref="Consts"/> class.
    /// </summary>
    public string MulticastEventLevel
    {
      get { return _multicastEventLevel; }
      set { _multicastEventLevel = value; }
    }

    /// <summary>
    /// Gets the datatype of this state variable.
    /// </summary>
    public DvDataType DataType
    {
      get { return _dataType; }
    }

    /// <summary>
    /// Gets or sets the default value of this state variable.
    /// </summary>
    /// <remarks>
    /// MUST satisfy the <see cref="AllowedValueList"/> or <see cref="AllowedValueRange"/> constraints, if present.
    /// This parameter MUST NOT be set if the <see cref="DataType"/> is a <see cref="DvExtendedDataType"/>.
    /// </remarks>
    public object DefaultValue
    {
      get { return _defaultValue; }
      set
      {
        if (_dataType is DvExtendedDataType)
          throw new IllegalCallException("Default value must not be set if the state variable has an extended data type");
        _defaultValue = value;
      }
    }

    /// <summary>
    /// Gets or sets the allowed values for this state variable.
    /// </summary>
    /// <remarks>
    /// Enumerates legal string values.
    /// PROHIBITED for other data types than the UPnP standard string.
    /// This parameter MUST NOT be set if the <see cref="DataType"/> is a <see cref="DvExtendedDataType"/>.
    /// </remarks>
    public IList<string> AllowedValueList
    {
      get { return _allowedValueList; }
      set
      {
        if (_dataType is DvExtendedDataType)
          throw new IllegalCallException("Allowed value list must not be set if the state variable has an extended data type");
        _allowedValueList = value;
      }
    }

    /// <summary>
    /// Gets or sets the allowed values for this state variable.
    /// </summary>
    /// <remarks>
    /// Defines bounds for legal numeric values. Defines resolution for numeric values.
    /// Defined only for standard UPnP numeric data types.
    /// This parameter MUST NOT be set if the <see cref="DataType"/> is a <see cref="DvExtendedDataType"/>.
    /// </remarks>
    public DvAllowedValueRange AllowedValueRange
    {
      get { return _allowedValueRange; }
      set
      {
        if (_dataType is DvExtendedDataType)
          throw new IllegalCallException("Allowed value range must not be set if the state variable has an extended data type");
        _allowedValueRange = value;
      }
    }

    /// <summary>
    /// Gets or sets the value of this state variable.
    /// </summary>
    public object Value
    {
      get { return _value; }
      set
      {
        if (value != null && !IsValueAssignable(value))
          throw new ArgumentException(string.Format("Value of type {0} cannot be assigned to data type {1}",
              value.GetType().Name, _dataType));
        if (!IsValueInRange(value))
          throw new ArgumentException(string.Format("Value '{0}' is out of range for state variable '{1}'",
              value, _name));
        _value = value;
        FireStateVariableChanged();
      }
    }

    /// <summary>
    /// Maximum rate in seconds where changes of this state variable lead to new event messages.
    /// If the state variable changes earlier then this timespan again, the next event message will be triggered earliest
    /// this timespan later.
    /// </summary>
    public TimeSpan? ModeratedMaximumRate
    {
      get { return _moderatedMaximumRate; }
      set { _moderatedMaximumRate = value; }
    }

    /// <summary>
    /// Minimum value change which leads to an event message.
    /// TODO Albert: The (DevArch) documentation is very bad for this value. We must find a sensible implementation for it.
    /// </summary>
    public double ModeratedMinimumDelta
    {
      get { return _moderatedMinimumDelta; }
      set { _moderatedMinimumDelta = value; }
    }

    /// <summary>
    /// Checks if the given <paramref name="value"/> can be assigned to this state variable.
    /// </summary>
    /// <param name="value">Value to be checked.</param>
    /// <returns><c>true</c>, if the given <paramref name="value"/> can be assigned, else <c>false</c>.</returns>
    public bool IsValueAssignable(object value)
    {
      return _dataType.IsValueAssignable(value) && IsValueInRange(value);
    }

    /// <summary>
    /// Checks if the given <paramref name="value"/> is in the specified <see cref="AllowedValueRange"/> of this state variable.
    /// </summary>
    /// <param name="value">Value to be checked.</param>
    /// <returns><c>true</c>, if the given <paramref name="value"/> is in the correct range, else <c>false</c>.</returns>
    public bool IsValueInRange(object value)
    {
      if (_allowedValueRange == null)
        return true;
      return _allowedValueRange.IsValueInRange(value);
    }

    protected void FireStateVariableChanged()
    {
      if (_parentService != null)
        _parentService.FireStateVariableChanged(this);
    }

    #region Description generation

    internal void AddSCDPDescriptionForStateVariable(StringBuilder result, IDictionary<string, string> dataTypeSchemas2NSPrefix)
    {
      result.Append(
          "<stateVariable sendEvents=\"");
      result.Append(_sendEvents ? "yes" : "no");
      result.Append("\" multicast=\"");
      result.Append(_multicast ? "yes" : "no");
      result.Append("\">" +
            "<name>");
      result.Append(_name);
      result.Append("</name>");
      _dataType.AddSCDPDescriptionForStandardDataType(result, dataTypeSchemas2NSPrefix);
      if (_defaultValue != null)
      {
        result.Append(
            "<defaultValue>");
        result.Append(
              _dataType.SoapSerializeValue(_defaultValue, true));
        result.Append(
            "</defaultValue>");
      }
      if (_allowedValueList != null && _allowedValueList.Count > 0)
      {
        result.Append(
            "<allowedValueList>");
        foreach (string value in _allowedValueList)
        {
          result.Append(
              "<allowedValue>");
          result.Append(
                value);
          result.Append(
              "</allowedValue>");
        }
        result.Append(
            "</allowedValueList>");
      }
      if (_allowedValueRange != null)
        _allowedValueRange.AddSCDPDescriptionForValueRange(result);
      result.Append(
          "</stateVariable>");
    }

    #endregion
  }
}
