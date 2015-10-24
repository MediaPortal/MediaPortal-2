#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Linq;
using System.Text;
using MediaPortal.Common.MediaManagement;
using HttpServer;
using HttpServer.Sessions;
using System.IO;
using MediaPortal.Plugins.MediaServer.DLNA;
using MediaPortal.Common.Logging;
using MediaPortal.Common;

namespace MediaPortal.Plugins.MediaServer.Protocols
{
  public class GenericAccessProtocol
  {
    public enum ResourceAccessProtocol
    {
      GenericAccessProtocol,
      SamsungAccessProtocol,
      XBoxAccessProtocol
    }
    public static GenericAccessProtocol GetProtocolResourceHandler(ResourceAccessProtocol ResourceProtocol)
    {
      if (ResourceProtocol == ResourceAccessProtocol.SamsungAccessProtocol)
      {
        return new SamsungProtocolHandler();
      }
      else if (ResourceProtocol == ResourceAccessProtocol.XBoxAccessProtocol)
      {
        return new XBoxProtocolHandler();
      }
      return new GenericAccessProtocol();
    }

    public virtual bool HandleRequest(IHttpRequest request, IHttpResponse response, IHttpSession session, DlnaMediaItem item)
    {
      return false;
    }

    public virtual bool CanHandleRequest(IHttpRequest request)
    {
      return false;
    }

    public virtual Stream HandleResourceRequest(IHttpRequest request, IHttpResponse response, IHttpSession session, DlnaMediaItem item)
    {
      return null;
    }

    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
