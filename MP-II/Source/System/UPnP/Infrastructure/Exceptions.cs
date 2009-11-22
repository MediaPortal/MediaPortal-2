#region Copyright (C) 2007-2009 Team MediaPortal

/* 
 *  Copyright (C) 2007-2009 Team MediaPortal
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

namespace UPnP.Infrastructure
{
  /// <summary>
  /// Thrown if a request of an usupported UPnP version should be handled.
  /// </summary>
  public class UnsupportedRequestException : ApplicationException
  {
    public UnsupportedRequestException(string msg, params object[] args) :
      base(string.Format(msg, args)) { }
    public UnsupportedRequestException(string msg, Exception ex, params object[] args) :
      base(string.Format(msg, args), ex) { }
  }

  /// <summary>
  /// Thrown if an action template is to be modified while it is already connected to a device's action.
  /// </summary>
  public class UPnPAlreadyConnectedException : ApplicationException
  {
    public UPnPAlreadyConnectedException(string msg, params object[] args) :
      base(string.Format(msg, args)) { }
    public UPnPAlreadyConnectedException(string msg, Exception ex, params object[] args) :
      base(string.Format(msg, args), ex) { }
  }
}
