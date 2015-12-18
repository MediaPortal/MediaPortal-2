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
using System.Net;
using System.Reflection;
using System.Text;
using HttpServer;
using HttpServer.Authentication;
using HttpServer.Exceptions;
using HttpServer.HttpModules;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Authentication;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS;
using MediaPortal.Plugins.Transcoding.Service;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.Base;
using MediaPortal.Utilities.SystemAPI;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using MediaPortal.Plugins.MP2Extended.ErrorPages;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  public class MainRequestHandler : HttpModule, IDisposable
  {
    public const string RESOURCE_ACCESS_PATH = "/MPExtended";
    public TimeSpan CACHE_CLEANUP_INTERVAL = TimeSpan.FromMinutes(1);

    internal static readonly Dictionary<string, IRequestModuleHandler> REQUEST_MODULE_HANDLERS = new Dictionary<string, IRequestModuleHandler>(StringComparer.OrdinalIgnoreCase)
    {
      { "MediaAccessService", new MediaAccessServiceHandler() },
      { "TVAccessService", new TVAccessServiceHandler() },
      { "StreamingService", new StreamingServiceHandler() },
      { "DebugAccessService", new DebugAccessServiceHandler() }
    };


    private readonly string _serverOsVersion = null;
    private readonly string _product = null;
    private AuthRequestHandler _authRequestHandler;

    public MainRequestHandler()
    {
      _serverOsVersion = WindowsAPI.GetOsVersionString();
      Assembly assembly = Assembly.GetExecutingAssembly();
      _product = "MediaPortal 2 MPExtended Server/" + AssemblyName.GetAssemblyName(assembly.Location).Version.ToString(2);

      // Authentication
      _authRequestHandler = new AuthRequestHandler(RESOURCE_ACCESS_PATH);
    }

    public static void Shutdown()
    {
      foreach (KeyValuePair<string, StreamItem> stream in StreamControl.GetStreamItems())
      {
        StreamControl.StopStreaming(stream.Key);
        if (stream.Value.TranscoderObject != null)
        {
          try
          {
            if (stream.Value.TranscoderObject.IsStreaming)
            {
              stream.Value.TranscoderObject.StopStreaming();
              Logger.Debug("MainRequestHandler: Stopping stream of mediaitem ", stream.Value.TranscoderObject.MediaSource.MediaItemId);
            }
          }
          catch (Exception e)
          {
            Logger.Warn("MainRequestHandler: Error stopping stream", e);
          }
          try
          {
            if (stream.Value.TranscoderObject.IsTranscoding)
            {
              stream.Value.TranscoderObject.StopTranscoding();
              Logger.Debug("MainRequestHandler: Aborting transcoding of mediaitem ", stream.Value.TranscoderObject.MediaSource.MediaItemId);
            }
          }
          catch (Exception e)
          {
            Logger.Warn("MainRequestHandler: Error stopping transcoding", e);
          }
        }
      }
    }

    public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      var uri = request.Uri;
      Guid mediaItemGuid = Guid.Empty;
      Logger.Info("MainRequestHandler: Received request {0}", request.Uri);

      try
      {
        response.AddHeader("Server", _serverOsVersion + " " + _product);
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
          if (REQUEST_MODULE_HANDLERS.TryGetValue(uriParts[2], out requestModuleHandler))
            requestModuleHandler.Process(request, response, session);
          else
            ServiceRegistration.Get<ILogger>().Warn("RequestModule not found: {0}", uriParts[2]);
        }
      }
      catch (Exception ex)
      {
        Logger.Error("MainRequestHandler: Exception:", ex);
        //throw new InternalServerException(string.Format("Failed to proccess! - Exception: {0}\n\nStackTrace:\n{1}", ex.Message, ex.StackTrace), ex);
        new SendErrorPage().Process(request, response, session, ex);
      }

      return true;
    }


    protected void Send(IHttpRequest request, IHttpResponse response, Stream resourceStream, bool onlyHeaders, long start, long length)
    {
    }

    public void Dispose()
    {
      Shutdown();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
