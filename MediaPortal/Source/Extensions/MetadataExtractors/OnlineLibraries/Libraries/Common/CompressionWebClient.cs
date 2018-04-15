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
using System.Net;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Common
{
  public class CompressionWebClient : WebClient
  {
    protected bool _enableCompression;
    protected int _requestTimeOut = 20000;

    public int RequestTimeout
    {
      get { return _requestTimeOut; }
      set { _requestTimeOut = value; }
    }

    public CompressionWebClient()
      : this(true)
    {
    }

    public CompressionWebClient(bool enableCompression)
    {
      _enableCompression = enableCompression;
    }

    protected override WebRequest GetWebRequest(Uri address)
    {
      HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
      if (request != null && _enableCompression)
      {
        Headers["Accept-Encoding"] = "gzip, deflate";
        request.Timeout = RequestTimeout; // Use 20 seconds - default is 100 seconds
        request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
      }
      return request;
    }
  }
}