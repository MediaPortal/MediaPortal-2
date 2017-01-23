#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.CP
{
  public class UPnPException : Exception
  {
    public UPnPException() { }
    public UPnPException(string message, params object[] parameters) :
        base(string.Format(message, parameters)) { }
    public UPnPException(string message, Exception innerException, params object[] parameters) :
        base(string.Format(message, parameters), innerException) { }
  }

  public class UPnPRemoteException : UPnPException
  {
    protected UPnPError _error;

    public UPnPRemoteException(UPnPError error) :
        base(error.ErrorDescription)
    {
      _error = error;
    }

    public UPnPError Error
    {
      get { return _error; }
    }
  }

  public class UPnPDisconnectedException : Exception
  {
    public UPnPDisconnectedException() { }
    public UPnPDisconnectedException(string message, params object[] parameters) :
        base(string.Format(message, parameters)) { }
    public UPnPDisconnectedException(string message, Exception innerException, params object[] parameters) :
        base(string.Format(message, parameters), innerException) { }
  }
}
