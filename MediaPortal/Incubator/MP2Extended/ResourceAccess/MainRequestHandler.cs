#region Copyright (C) 2007-2015 Team MediaPortal

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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Threading;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.HttpModules;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Threading;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  public class MainRequestHandler : HttpModule, IDisposable
  {
    private const string RESOURCE_ACCESS_PATH = "/MPExtended";
    private readonly Dictionary<string, IRequestModuleHandler> _requestModuleHandlers = new Dictionary<string, IRequestModuleHandler>
    {
      {"MediaAccessService", new MediaAccessServiceHandler()},
      {"StreamingService", new StreamingServiceHandler()}
    };
    



    private readonly string _serverOsVersion = null;
    private readonly string _product = null;




    public MainRequestHandler()
    {
      _serverOsVersion = "1.0";
      Assembly assembly = Assembly.GetExecutingAssembly();
      _product = "MediaPortal 2 MPExtended/" + AssemblyName.GetAssemblyName(assembly.Location).Version.ToString(2);
    }

    protected class Range
    {
      protected long _from;
      protected long _to;

      public Range(long from, long to)
      {
        _from = from;
        _to = to;
      }

      public long From
      {
        get { return _from; }
      }

      public long To
      {
        get { return _to; }
      }

      public long Length
      {
        get { return _to - _from + 1; }
      }
    }

    
    public static void Shutdown()
    {
      
    }

    protected IList<Range> ParseTimeRanges(string timeRangesSpecifier, double duration)
    {
      if (string.IsNullOrEmpty(timeRangesSpecifier) || duration == 0)
        return null;
      IList<Range> result = new List<Range>();
      try
      {
        string[] tokens = timeRangesSpecifier.Split(new char[] { '=', ':' });
        if (tokens.Length == 2 && tokens[0].Trim() == "npt")
          foreach (string rangeSpec in tokens[1].Split(new char[] { ',' }))
          {
            tokens = rangeSpec.Split(new char[] { '-' });
            if (tokens.Length != 2)
              return new Range[] { };
            if (!string.IsNullOrEmpty(tokens[0]))
              if (!string.IsNullOrEmpty(tokens[1]))
                result.Add(new Range(Convert.ToInt64(TimeSpan.Parse(tokens[0], CultureInfo.InvariantCulture).TotalSeconds), Convert.ToInt64(TimeSpan.Parse(tokens[1], CultureInfo.InvariantCulture).TotalSeconds)));
              else
                result.Add(new Range(Convert.ToInt64(TimeSpan.Parse(tokens[0], CultureInfo.InvariantCulture).TotalSeconds), Convert.ToInt64(duration) - 1));
            else
              result.Add(new Range(Math.Max(0, Convert.ToInt64(duration) - Convert.ToInt64(TimeSpan.Parse(tokens[1], CultureInfo.InvariantCulture).TotalSeconds)), Convert.ToInt64(duration) - 1));
          }
      }
      catch (Exception e)
      {
        Logger.Debug("ResourceAccessModule: Received illegal Range header", e);
        // As specified in RFC2616, section 14.35.1, ignore invalid range header
      }
      return result;
    }

    protected IList<Range> ParseByteRanges(string byteRangesSpecifier, long size)
    {
      if (string.IsNullOrEmpty(byteRangesSpecifier) || size == 0)
        return null;
      IList<Range> result = new List<Range>();
      try
      {
        string[] tokens = byteRangesSpecifier.Split(new char[] { '=', ':' });
        if (tokens.Length == 2 && tokens[0].Trim() == "bytes")
          foreach (string rangeSpec in tokens[1].Split(new char[] { ',' }))
          {
            tokens = rangeSpec.Split(new char[] { '-' });
            if (tokens.Length != 2)
              return new Range[] { };
            if (!string.IsNullOrEmpty(tokens[0]))
              if (!string.IsNullOrEmpty(tokens[1]))
                result.Add(new Range(long.Parse(tokens[0]), long.Parse(tokens[1])));
              else
                result.Add(new Range(long.Parse(tokens[0]), size - 1));
            else
              result.Add(new Range(Math.Max(0, size - long.Parse(tokens[1])), size - 1));
          }
      }
      catch (Exception e)
      {
        Logger.Debug("ResourceAccessModule: Received illegal Range header", e);
        // As specified in RFC2616, section 14.35.1, ignore invalid range header
      }
      return result;
    }

    public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      var uri = request.Uri;
      Guid mediaItemGuid = Guid.Empty;
      bool bHandled = false;
      Logger.Debug("MainRequestHandler: Received request {0}", request.Uri);

      try
      {
        response.AddHeader("Server", _serverOsVersion  + _product);
        response.AddHeader("Cache-control", "no-cache");
        response.Connection = ConnectionType.Close;

        // Check the request path to see if it's for us.
        if (!uri.AbsolutePath.StartsWith(RESOURCE_ACCESS_PATH))
        {
            return false;
        }


        // Pass the Processing to the right module
        string[] uriParts = uri.AbsolutePath.Split('/');
        Logger.Info("MainRequestHandler: AbsolutePath: {0}, uriParts.Length: {1}", uri.AbsolutePath, uriParts.Length);
        if (uriParts.Length > 2)
        {
          // The URL shoud look like this: /MPExtended/MediaAccessService/json/GetServiceDescription
          IRequestModuleHandler requestModuleHandler;
          if (_requestModuleHandlers.TryGetValue(uriParts[2], out requestModuleHandler))
            requestModuleHandler.Process(request, response, session);
          else
            ServiceRegistration.Get<ILogger>().Warn("RequestModule not found: {0}", uriParts[2]);
        }
      }
      catch (FileNotFoundException ex)
      {
        throw new InternalServerException("Failed to proccess '{0}'", ex);
      }

      return true;
    }

    


    protected void Send(IHttpRequest request, IHttpResponse response, Stream resourceStream, bool onlyHeaders, long start, long length)
    {

      
    }

    public void Dispose()
    {
      
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
