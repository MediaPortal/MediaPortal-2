#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Text;
using System.Xml.XPath;
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

    /// <summary>
    /// Creates a new formal argument for a device's action.
    /// </summary>
    /// <param name="name">Name of the argument. The name SHOULD be chosen to reflect the semantic use
    /// of the argument. MUST NOT contain a hyphen character ("-") nor
    /// a hash character (“#”, 23 Hex in UTF-8). Case sensitive. First character MUST be a USASCII
    /// letter ("A"-"Z", "a"-"z"), USASCII digit ("0"-"9"), an underscore ("_"), or a non-experimental
    /// Unicode letter or digit greater than U+007F. Succeeding characters MUST be a USASCII letter
    /// ("A"-"Z", "a"-"z"), USASCII digit ("0"-"9"), an underscore ("_"), a period ("."), a Unicode
    /// combiningchar, an extender, or a non-experimental Unicode letter or digit greater than
    /// U+007F. The first three letters MUST NOT be "XML" in any combination of case.
    /// Case sensitive. SHOULD be < 32 characters.
    /// </param>
    /// <param name="relatedStateVariable">Defines the type of the argument; see further explanation in (DevArch).</param>
    /// <param name="direction">Defines whether argument is an input or output parameter.</param>
    /// <param name="isReturnValue">Returns the information if this argument is the return value of the parent
    /// action. This argument is allowed to be <c>true</c> at most for one argument per action. If it is set to
    /// <c>true</c> for an argument, that argument MUST be the first output argument of the action.</param>
    public DvArgument(string name, DvStateVariable relatedStateVariable, ArgumentDirection direction, bool isReturnValue)
    {
      _name = name;
      _relatedStateVariable = relatedStateVariable;
      _direction = direction;
      _isReturnValue = isReturnValue;
    }

    public DvArgument(string name, DvStateVariable relatedStateVariable, ArgumentDirection direction) :
        this(name, relatedStateVariable, direction, false) { }

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

    public UPnPError SoapParseArgument(XPathNavigator enclosingElementNav, bool isSimpleValue, out object value)
    {
      try
      {
        value = _relatedStateVariable.DataType.SoapDeserializeValue(enclosingElementNav, isSimpleValue);
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
      result.Append("</direction>");
      if (_isReturnValue)
        result.Append(
            "<retval/>");
      result.Append(
            "<relatedStateVariable>");
      result.Append(_relatedStateVariable.Name);
      result.Append("</relatedStateVariable>" +
          "</argument>");
    }

    #endregion
  }
}
