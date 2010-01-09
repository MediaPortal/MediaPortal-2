#region Copyright (C) 2007-2010 Team MediaPortal

/* 
 *  Copyright (C) 2007-2010 Team MediaPortal
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
using System.Xml;
using System.Xml.XPath;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.CP.DeviceTree
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

  /// <summary>
  /// UPnP action argument template which gets instantiated at the client (control point) side for each UPnP action argument.
  /// </summary>
  /// <remarks>
  /// Parts of this class are intentionally parallel to the implementation in <see cref="UPnP.Infrastructure.Dv.DeviceTree.DvArgument"/>.
  /// </remarks>
  public class CpArgument
  {
    protected string _name;
    protected ArgumentDirection _direction;
    protected bool _isReturnValue;
    protected CpAction _parentAction;
    protected CpStateVariable _relatedStateVariable; // References the related state variable in our parent action's parent service

    public CpArgument(CpAction parentAction, string name, CpStateVariable relatedStateVariable, ArgumentDirection direction,
        bool isReturnValue)
    {
      _parentAction = parentAction;
      _name = name;
      _relatedStateVariable = relatedStateVariable;
      _direction = direction;
      _isReturnValue = isReturnValue;
    }

    public CpArgument(CpAction parentAction, string name, CpStateVariable relatedStateVariable, ArgumentDirection direction) :
        this(parentAction, name, relatedStateVariable, direction, false) { }

    public string Name
    {
      get { return _name; }
    }

    public CpAction ParentAction
    {
      get { return _parentAction; }
      internal set { _parentAction = value; }
    }

    public ArgumentDirection Direction
    {
      get { return _direction; }
    }

    public bool IsReturnValue
    {
      get { return _isReturnValue; }
    }

    public CpStateVariable RelatedStateVar
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

    public void SoapParseArgument(XmlReader reader, bool isSimpleValue, out object value)
    {
      value = _relatedStateVariable.DataType.SoapDeserializeValue(reader, isSimpleValue);
    }

    public void SoapSerializeArgument(object value, bool forceSimpleValue, XmlWriter writer)
    {
      _relatedStateVariable.DataType.SoapSerializeValue(value, forceSimpleValue, writer);
    }

    #region Connection

    internal static ArgumentDirection ParseArgumentDirection(string direction)
    {
      switch (direction)
      {
        case "in":
          return ArgumentDirection.In;
        case "out":
          return ArgumentDirection.Out;
        default:
          throw new ArgumentException(string.Format("Argument direction '{0}' not known", direction));
      }
    }

    internal static CpArgument CreateArgument(CpAction parentAction, CpService parentService, XPathNavigator argumentNav,
        IXmlNamespaceResolver nsmgr)
    {
      string name = ParserHelper.SelectText(argumentNav, "s:name/text()", nsmgr);
      string relatedStateVariableName = ParserHelper.SelectText(argumentNav, "s:relatedStateVariable/text()", nsmgr);
      CpStateVariable relatedStateVariable;
      if (!parentService.StateVariables.TryGetValue(relatedStateVariableName, out relatedStateVariable))
        throw new ArgumentException("Related state variable '{0}' is not present in service", relatedStateVariableName);
      string direction = ParserHelper.SelectText(argumentNav, "s:direction/text()", nsmgr);
      XPathNodeIterator retValIt = argumentNav.Select("s:retval", nsmgr);
      CpArgument result = new CpArgument(parentAction, name, relatedStateVariable, ParseArgumentDirection(direction), retValIt.MoveNext());
      return result;
    }

    #endregion
  }
}
