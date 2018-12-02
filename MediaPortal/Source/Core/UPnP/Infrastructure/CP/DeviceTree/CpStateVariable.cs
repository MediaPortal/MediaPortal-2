#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.CP.DeviceTree
{
  /// <summary>
  /// UPnP state variable template which gets instantiated at the client (control point) side for each state variable
  /// the control point wants to connect to.
  /// </summary>
  /// <remarks>
  /// Parts of this class are intentionally parallel to the implementation in
  /// <see cref="UPnP.Infrastructure.Dv.DeviceTree.DvStateVariable"/>.
  /// </remarks>
  public class CpStateVariable
  {
    protected string _name;
    protected CpService _parentService;
    protected bool _sendEvents = true;
    protected bool _multicast = false;
    protected string _multicastEventLevel = UPnPConsts.MEL_UPNP_INFO;
    protected CpDataType _dataType;
    protected object _defaultValue; // Initial value of this state variable
    protected object _value;
    protected IList<string> _allowedValueList = null; // May be defined for string data type
    protected CpAllowedValueRange _allowedValueRange = null; // May be defined for numeric data types
    protected bool _isOptional = true;
    protected DeviceConnection _connection = null;

    /// <summary>
    /// Creates a new <see cref="CpStateVariable"/> instance.
    /// </summary>
    /// <param name="connection">Device connection instance which attends the connection with the server side.</param>
    /// <param name="parentService">Instance of the service which contains the new state variable.</param>
    /// <param name="name">Name of the state variable.</param>
    /// <param name="dataType">Data type of the state variable.</param>
    public CpStateVariable(DeviceConnection connection, CpService parentService, string name, CpDataType dataType)
    {
      _connection = connection;
      _parentService = parentService;
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
    /// Returns the data type of this state variable.
    /// </summary>
    public CpDataType DataType
    {
      get { return _dataType; }
    }

    /// <summary>
    /// Gets or sets a flag which controls the control point's matching behaviour.
    /// If <see cref="IsOptional"/> is set to <c>true</c>, the control point will also match services from the network
    /// which don't implement this state variable. If this flag is set to <c>false</c>, services without a state variable
    /// matching this state variable template won't be considered as matching services.
    /// </summary>
    public bool IsOptional
    {
      get { return _isOptional; }
      set { _isOptional = value; }
    }

    /// <summary>
    /// Returns the information if this state variable template is connected to a matching UPnP state variable.
    /// Will be set by the UPnP system.
    /// </summary>
    public bool IsConnected
    {
      get { return _connection != null; }
    }

    /// <summary>
    /// Returns the service which contains this state variable. Will be set by <see cref="CpService.AddStateVariable"/>.
    /// </summary>
    public CpService ParentService
    {
      get { return _parentService; }
      internal set { _parentService = value; }
    }

    /// <summary>
    /// Returns the SendEvents flag of this state variable. See <see cref="DvStateVariable.SendEvents"/>.
    /// Will be set by the UPnP subsystem.
    /// </summary>
    public bool SendEvents
    {
      get{ return _sendEvents; }
    }

    /// <summary>
    /// Returns the Multicast flag of this state variable. See <see cref="DvStateVariable.Multicast"/>.
    /// Will be set by the UPnP subsystem.
    /// </summary>
    public bool Multicast
    {
      get { return _multicast; }
    }

    /// <summary>
    /// Gets or sets the default value of this state variable.
    /// Will be set by the UPnP system.
    /// </summary>
    public object DefaultValue
    {
      get { return _defaultValue; }
      internal set { _defaultValue = value; }
    }

    /// <summary>
    /// Gets or sets the allowed values for this state variable.
    /// Will be set by the UPnP system.
    /// </summary>
    public IList<string> AllowedValueList
    {
      get { return _allowedValueList; }
      internal set { _allowedValueList = value; }
    }

    /// <summary>
    /// Gets or sets the allowed values for this state variable.
    /// Will be set by the UPnP system.
    /// </summary>
    public CpAllowedValueRange AllowedValueRange
    {
      get { return _allowedValueRange; }
      internal set { _allowedValueRange = value; }
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

    #region Connection

    internal static CpStateVariable ConnectStateVariable(DeviceConnection connection, CpService parentService,
        XPathNavigator svIt, IXmlNamespaceResolver nsmgr, DataTypeResolverDlgt dataTypeResolver)
    {
      string name = ParserHelper.SelectText(svIt, "s:name/text()", nsmgr);
      XPathNodeIterator dtIt = svIt.Select("s:dataType", nsmgr);
      if (!dtIt.MoveNext())
        throw new ArgumentException("Error evaluating data type element");
      CpDataType dataType = CpDataType.CreateDataType(dtIt.Current, nsmgr, dataTypeResolver);
      CpStateVariable result = new CpStateVariable(connection, parentService, name, dataType);
      XPathNodeIterator dvIt = svIt.Select("s:defaultValue", nsmgr);
      if (dvIt.MoveNext())
      {
        XmlReader reader = dvIt.Current.ReadSubtree();
        reader.MoveToContent();
        result.DefaultValue = dataType.SoapDeserializeValue(reader, true);  // Default value is always simple value (see DevArch)
      }
      XPathNodeIterator avlIt = svIt.Select("s:allowedValueList/s:allowedValue", nsmgr);
      if (avlIt.Count > 0)
      {
        IList<string> allowedValueList = new List<string>();
        while (avlIt.MoveNext())
          allowedValueList.Add(ParserHelper.SelectText(avlIt.Current, "text()", null));
        result.AllowedValueList = allowedValueList;
      }
      XPathNodeIterator avrIt = svIt.Select("s:allowedValueRange", nsmgr);
      if (avrIt.MoveNext())
        result.AllowedValueRange = CpAllowedValueRange.CreateAllowedValueRange(avrIt.Current, nsmgr);
      return result;
    }

    internal void Disconnect()
    {
      DeviceConnection connection = _connection;
      if (connection == null)
        return;
      lock (connection.CPData.SyncObj)
      {
        _connection = null;
      }
    }

    #endregion
  }
}
