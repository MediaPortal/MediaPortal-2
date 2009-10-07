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

namespace UPnP.Infrastructure.Common
{
  /// <summary>
  /// Contains runtime data for an UPnP error which occured during the invocation of a UPnP action.
  /// </summary>
  /// <remarks>
  /// <list type="table">
  /// <listheader><term>ErrorCode</term><description>Description</description></listheader>
  /// <item><term>401 - Invalid Action</term><description>No action by that name at this service.</description></item>
  /// <item><term>402 - Invalid Args</term><description>Could be any of the following: not enough in args, args in the wrong order,
  /// one or more in args are of the wrong data type.</description></item>
  /// <item><term>403 - (Do Not Use)</term><description>(This code has been deprecated.)</description></item>
  /// <item><term>501 - Action Failed</term><description>MAY be returned if current state of service prevents invoking
  /// that action.</description></item>
  /// <item><term>600 - Argument Value Invalid</term><description>The argument value is invalid.</description></item>
  /// <item><term>601 - Argument Value Out of Range</term><description>An argument value is less than the minimum or more
  /// than the maximum value of the allowed value range, or is not in the allowed value list.</description></item>
  /// <item><term>602 - Optional Action Not Implemented</term><description>The requested action is optional and is not implemented
  /// by the device.</description></item>
  /// <item><term>603 - Out of Memory</term><description>The device does not have sufficient memory available to complete
  /// the action. This MAY be a temporary condition; the control point MAY choose to retry the unmodified request again
  /// later and it MAY succeed if memory is available.</description></item>
  /// <item><term>604 - Human Intervention Required</term><description>The device has encountered an error condition which
  /// it cannot resolve itself and required human intervention such as a reset or power cycle. See the device display or
  /// documentation for further guidance.</description></item>
  /// <item><term>605 - String Argument Too Long</term><description>A string argument is too long for the device to handle
  /// properly.</description></item>
  /// <item><term>606-612 - Reserved</term><description>These ErrorCodes are reserved for UPnP DeviceSecurity.</description></item>
  /// <item><term>613-699 - TBD</term><description>Common action errors. Defined by UPnP Forum Technical
  /// Committee.</description></item>
  /// <item><term>700-799 - TBD</term><description>Action-specific errors defined by UPnP Forum working committee.</description></item>
  /// <item><term>800-899 - TBD</term><description>Action-specific errors for non-standard actions. Defined by UPnP
  /// vendor.</description></item>
  /// </list>
  /// </remarks>
  public class UPnPError
  {
    protected int _errorCode;
    protected string _errorDescription;

    public UPnPError(int errorCode, string errorDescription)
    {
      _errorCode = errorCode;
      _errorDescription = errorDescription;
    }

    public int ErrorCode
    {
      get { return _errorCode; }
    }

    public string ErrorDescription
    {
      get { return _errorDescription; }
    }
  }

}
