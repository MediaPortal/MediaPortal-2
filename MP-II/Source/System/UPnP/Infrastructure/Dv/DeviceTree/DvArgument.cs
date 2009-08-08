using System;
using System.Text;
using System.Xml;
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Specifies the direction of a UPnP argument.
  /// </summary>
  public enum ArgumentDirection
  {
    /// <summary>
    /// Input argument.
    /// </summary>
    In,

    /// <summary>
    /// Output argument.
    /// </summary>
    Out,
  }

  public class DvArgument
  {
    protected string _name;
    protected ArgumentDirection _direction;
    protected bool _isReturnValue;
    protected DvAction _parentAction = null; // Reference to the enclosing action, lazy initialized
    protected DvStateVariable _relatedStateVariable; // References the related state variable in our parent action's parent service

    public DvArgument(string name, DvStateVariable relatedStateVariable, ArgumentDirection direction)
    {
      _name = name;
      _relatedStateVariable = relatedStateVariable;
      _direction = direction;
    }

    public string Name
    {
      get { return _name; }
    }

    public DvAction ParentAction
    {
      get { return _parentAction; }
      internal set { _parentAction = value; }
    }

    public DvStateVariable RelatedStateVar
    {
      get { return _relatedStateVariable; }
    }

    public bool IsValueAssignable(object value)
    {
      return _relatedStateVariable.IsValueAssignable(value);
    }

    public bool IsValueInRange(object value)
    {
      return _relatedStateVariable.IsValueInRange(value);
    }

    public UPnPError SoapParseArgument(XmlElement enclosingElement, bool isSimpleValue, out object value)
    {
      try
      {
        value = _relatedStateVariable.DataType.SoapDeserializeValue(enclosingElement, isSimpleValue);
      }
      catch (Exception)
      {
        value = null;
        return new UPnPError(402, "Invalid Args");
      }
      if (IsValueInRange(value))
      {
        if (!IsValueAssignable(value))
          return new UPnPError(600, "Argument Value Invalid");
      }
      else
        return new UPnPError(601, "Argument Value Out Of Range");
      return null;
    }

    public UPnPError SoapSerializeArgument(object value, bool forceSimpleValue, out string serializedValue)
    {
      try
      {
        serializedValue = _relatedStateVariable.DataType.SoapSerializeValue(value, forceSimpleValue);
        return null;
      }
      catch (Exception)
      {
        serializedValue = null;
        return new UPnPError(501, "Action Failed");
      }
    }

    #region Description generation

    internal void AddSCDPDescriptionForArgument(StringBuilder result)
    {
      result.Append(
          "<argument>" +
            "<name>");
      result.Append(_name);
      result.Append("</name>" +
            "<direction>");
      result.Append(_direction == ArgumentDirection.In ? "in" : "out");
      result.Append("</direction>" +
            "<relatedStateVariable>");
      result.Append(_relatedStateVariable.Name);
      result.Append("</relatedStateVariable>" +
          "</argument>");
    }

    #endregion
  }
}
