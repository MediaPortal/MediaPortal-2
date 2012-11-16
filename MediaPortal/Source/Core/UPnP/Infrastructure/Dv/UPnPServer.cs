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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using HttpServer;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Dv.GENA;
using UPnP.Infrastructure.Dv.SOAP;
using UPnP.Infrastructure.Dv.SSDP;
using UPnP.Infrastructure.Utils;
using HttpListener=HttpServer.HttpListener;

namespace UPnP.Infrastructure.Dv
{
  /// <summary>
  /// Represents a container for all UPnP devices and services and provides the network functionality for the UPnP system.
  /// </summary>
  public class UPnPServer : IDisposable
  {
    /// <summary>
    /// Size of the queue which holds open HTTP requests before they are evaluated.
    /// </summary>
    public static int DEFAULT_HTTP_REQUEST_QUEUE_SIZE = 5;

    /// <summary>
    /// Prefix which is added to URLs for description documents.
    /// </summary>
    public static string DEFAULT_DESCRIPTION_URL_PREFIX = "/upnphost/description";

    /// <summary>
    /// Prefix which is added to URLs for control requests.
    /// </summary>
    public static string DEFAULT_CONTROL_URL_PREFIX = "/upnphost/control";

    /// <summary>
    /// Prefix which is added to URLs for event subsriptions.
    /// </summary>
    public static string DEFAULT_EVENT_SUB_URL_PREFIX = "/upnphost/eventing";

    protected ICollection<DvDevice> _rootDevices = new List<DvDevice>();
    protected ServerData _serverData = new ServerData();

    /// <summary>
    /// Creates a new UPnP server instance. After creating this instance, its root devices should be populated by calling
    /// <see cref="AddRootDevice"/> for each device.
    /// </summary>
    public UPnPServer()
    {
      _serverData.Server = this;
    }

    /// <summary>
    /// Disposes this <see cref="UPnPServer"/>, i.e. revokes UPnP network advertisements and closes all receiving servers.
    /// </summary>
    public virtual void Dispose()
    {
      Close();
      foreach (DvDevice rootDevice in _rootDevices)
        rootDevice.Dispose();
    }

    #region Event Handlers

    private void OnNetworkAddressChanged(object sender, EventArgs e)
    {
      lock (_serverData.SyncObj)
      {
        // To go around finding out which interface was changed, we simply raise an Update notification followed by a new advertisement
        UpdateInterfaceConfiguration();
        _serverData.SSDPController.Advertise();
      }
    }

    private void OnHttpListenerRequestReceived(object sender, RequestEventArgs e)
    {
      IHttpClientContext context = (IHttpClientContext) sender;
      lock (_serverData.SyncObj)
        if (!_serverData.IsActive)
          return;
      HandleHTTPRequest_NoLock(context, e.Request);
    }

    #endregion

    /// <summary>
    /// Returns the collection of root devices of this UPnP server.
    /// </summary>
    public ICollection<DvDevice> RootDevices
    {
      get { return _rootDevices; }
    }

    /// <summary>
    /// Adds a new UPnP root device. Should be done before <see cref="Bind"/> is called.
    /// </summary>
    /// <param name="device">Device to add to the <see cref="RootDevices"/> collection.</param>
    public void AddRootDevice(DvDevice device)
    {
      _rootDevices.Add(device);
    }

    /// <summary>
    /// Finds the device with the specified <paramref name="deviceUDN"/> in all device trees starting with the root devices.
    /// </summary>
    /// <param name="deviceUDN">Device UDN to search. The device UDN needs to be in the format "uuid:[device-UUID]"</param>
    /// <returns>UPnP device instance with the given <paramref name="deviceUDN"/> or <c>null</c>, if the specified device
    /// wasn't found in any of the root device trees.</returns>
    public DvDevice FindDeviceByUDN(string deviceUDN)
    {
      return _rootDevices.Select(rootDevice => rootDevice.FindDeviceByUDN(deviceUDN)).FirstOrDefault(result => result != null);
    }

    /// <summary>
    /// Finds all devices in all root device trees with the specified device <paramref name="type"/> and
    /// <paramref name="version"/>.
    /// </summary>
    /// <param name="type">Device type to search.</param>
    /// <param name="version">Version number of the device type to search.</param>
    /// <param name="searchCompatible">If set to <c>true</c>, this method also searches compatible devices,
    /// i.e. devices with a higher version number than requested.</param>
    public IEnumerable<DvDevice> FindDevicesByDeviceTypeAndVersion(string type, int version, bool searchCompatible)
    {
      return _rootDevices.SelectMany(rootDevice => rootDevice.FindDevicesByDeviceTypeAndVersion(type, version, searchCompatible));
    }

    /// <summary>
    /// Starts this UPnP server, i.e. starts a network listener and sends notifications about provided devices.
    /// </summary>
    /// <param name="advertisementInterval">Interval in seconds to repeat UPnP advertisements in the network.
    /// The UPnP architecture document (UPnP-arch-DeviceArchitecture-v1 1-20081015, 1.2.2, page 21) states a
    /// minimum of 1800 seconds. But in the world of today, that value is much to high for many applications and in many
    /// cases, a value of much less than 1800 seconds is choosen. For servers which will frequently change their
    /// availability, this value should be short, for more durable serves, this interval can be much longer (maybe a day).</param>
    public void Bind(int advertisementInterval = UPnPConsts.DEFAULT_ADVERTISEMENT_EXPIRATION_TIME)
    {
      lock (_serverData.SyncObj)
      {
        if (_serverData.IsActive)
          throw new IllegalCallException("UPnP subsystem mustn't be started multiple times");

        if (UPnPConfiguration.USE_IPV4)
        {
          _serverData.HTTPListenerV4 = HttpListener.Create(IPAddress.Any, 0);
          _serverData.HTTPListenerV4.RequestReceived += OnHttpListenerRequestReceived;
          try
          {
            _serverData.HTTPListenerV4.Start(DEFAULT_HTTP_REQUEST_QUEUE_SIZE); // Might fail if IPv4 isn't installed
            _serverData.HTTP_PORTv4 = _serverData.HTTPListenerV4.LocalEndpoint.Port;
            UPnPConfiguration.LOGGER.Info("UPnP server: HTTP listener for IPv4 protocol started at port {0}", _serverData.HTTP_PORTv4);
          }
          catch (SocketException e)
          {
            _serverData.HTTPListenerV4 = null;
            _serverData.HTTP_PORTv4 = 0;
            UPnPConfiguration.LOGGER.Warn("UPnPServer: Error starting HTTP server (IPv4)", e);
          }
        }
        else
        {
          _serverData.HTTPListenerV4 = null;
          _serverData.HTTP_PORTv4 = 0;
          UPnPConfiguration.LOGGER.Info("UPnP server: IPv4 protocol disabled, so no HTTP listener started for IPv4");
        }

        if (UPnPConfiguration.USE_IPV6)
        {
          _serverData.HTTPListenerV6 = HttpListener.Create(IPAddress.IPv6Any, 0); // Might fail if IPv6 isn't installed
          _serverData.HTTPListenerV6.RequestReceived += OnHttpListenerRequestReceived;
          try
          {
            _serverData.HTTPListenerV6.Start(DEFAULT_HTTP_REQUEST_QUEUE_SIZE);
            _serverData.HTTP_PORTv6 = _serverData.HTTPListenerV6.LocalEndpoint.Port;
            UPnPConfiguration.LOGGER.Info("UPnP server: HTTP listener for IPv6 protocol started at port {0}", _serverData.HTTP_PORTv6);
          }
          catch (SocketException e)
          {
            _serverData.HTTPListenerV6 = null;
            _serverData.HTTP_PORTv6 = 0;
            UPnPConfiguration.LOGGER.Warn("UPnPServer: Error starting HTTP server (IPv6)", e);
          }
        }
        else
        {
          _serverData.HTTPListenerV6 = null;
          _serverData.HTTP_PORTv6 = 0;
          UPnPConfiguration.LOGGER.Info("UPnP server: IPv6 protocol disabled, so no HTTP listener started for IPv6");
        }

        _serverData.SSDPController = new SSDPServerController(_serverData)
          {
              AdvertisementExpirationTime = advertisementInterval
          };
        _serverData.GENAController = new GENAServerController(_serverData);

        InitializeDiscoveryEndpoints();

        NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
        _serverData.IsActive = true;

        // At the end, start the controllers
        _serverData.SSDPController.Start();
        _serverData.GENAController.Start();
        UPnPConfiguration.LOGGER.Info("UPnP server online hosting {0} UPnP root devices", _serverData.Server.RootDevices.Count);
      }
    }

    /// <summary>
    /// Has to be called when the server's configuration (i.e. its devices, services, actions or state variables)
    /// was changed.
    /// </summary>
    /// <remarks>
    /// It is recommended not to change the server's configuration at runtime. Instead, the server's configuration
    /// should remain stable at runtime. Changes in the server's capabilities should be announced by appropriate
    /// state variables.
    /// </remarks>
    public void UpdateConfiguration()
    {
      lock (_serverData.SyncObj)
      {
        foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
        {
          GenerateObjectURLs(config);
          config.ConfigId = GenerateConfigId(config);
        }
        _serverData.SSDPController.Advertise();
      }
    }

    /// <summary>
    /// Removes all network components for the UPnP server.
    /// </summary>
    public void Close()
    {
      lock (_serverData.SyncObj)
      {
        if (!_serverData.IsActive)
          return;
        _serverData.IsActive = false;
      }
      _serverData.GENAController.Close();
      _serverData.SSDPController.Close();
      if (_serverData.HTTPListenerV4 != null)
        _serverData.HTTPListenerV4.Stop();
      if (_serverData.HTTPListenerV6 != null)
        _serverData.HTTPListenerV6.Stop();
      lock (_serverData.SyncObj)
        _serverData.UPnPEndPoints.Clear();
    }

    #region Protected methods

    private static CultureInfo GetFirstCultureOrDefault(string cultureList, CultureInfo defaultCulture)
    {
      if (string.IsNullOrEmpty(cultureList))
        return defaultCulture;
      int index = cultureList.IndexOf(',');
      if (index > -1)
        try
        {
          return CultureInfo.GetCultureInfo(cultureList.Substring(0, index));
        }
        catch (ArgumentException) { }
      return defaultCulture;
    }

    /// <summary>
    /// Handles all kinds of HTTP over TCP requests - Description, Control and Event subscriptions.
    /// </summary>
    /// <param name="context">HTTP client context of the current request.</param>
    /// <param name="request">HTTP request to handle.</param>
    protected void HandleHTTPRequest_NoLock(IHttpClientContext context, IHttpRequest request)
    {
      Uri uri = request.Uri;
      string hostName = uri.Host;
      string pathAndQuery = uri.LocalPath; // Unfortunately, Uri.PathAndQuery doesn't decode characters like '{' and '}', so we use the Uri.LocalPath property
      try
      {
        DvService service;
        ICollection<EndpointConfiguration> endpoints;
        lock (_serverData.SyncObj)
          endpoints = _serverData.UPnPEndPoints;
        foreach (EndpointConfiguration config in endpoints)
        {
          if (!NetworkHelper.HostNamesEqual(hostName, NetworkHelper.IPAddrToHostName(config.EndPointIPAddress)))
            continue;

          // Common check for supported encodings
          string acceptEncoding = request.Headers.Get("ACCEPT-ENCODING") ?? string.Empty;
          bool canCompress = acceptEncoding.Contains(CompressionHelper.PREFERRED_COMPRESSION);

          // Handle different HTTP methods here
          if (request.Method == "GET")
          { // GET of descriptions
            if (pathAndQuery.StartsWith(config.DescriptionPathBase))
            {
              string acceptLanguage = request.Headers.Get("ACCEPT-LANGUAGE");
              CultureInfo culture = GetFirstCultureOrDefault(acceptLanguage, CultureInfo.InvariantCulture);

              string description = null;
              DvDevice rootDevice;
              lock (_serverData.SyncObj)
                if (config.RootDeviceDescriptionPathsToRootDevices.TryGetValue(pathAndQuery, out rootDevice))
                  description = rootDevice.BuildRootDeviceDescription(_serverData, config, culture);
                else if (config.SCPDPathsToServices.TryGetValue(pathAndQuery, out service))
                  description = service.BuildSCPDDocument(config, _serverData);
              if (description != null)
              {
                IHttpResponse response = request.CreateResponse(context);
                response.Status = HttpStatusCode.OK;
                response.ContentType = "text/xml; charset=utf-8";
                if (!string.IsNullOrEmpty(acceptLanguage))
                  response.AddHeader("CONTENT-LANGUAGE", culture.ToString());
                using (var responseStream = new MemoryStream(UPnPConsts.UTF8_NO_BOM.GetBytes(description)))
                  CompressionHelper.WriteCompressedStream(response, responseStream, canCompress);
                SafeSendResponse(response);
                return;
              }
            }
          }
          else if (request.Method == "POST")
          { // POST of control messages
            if (config.ControlPathsToServices.TryGetValue(pathAndQuery, out service))
            {
              string contentType = request.Headers.Get("CONTENT-TYPE");
              string userAgentStr = request.Headers.Get("USER-AGENT");
              IHttpResponse response = request.CreateResponse(context);
              int minorVersion;
              if (string.IsNullOrEmpty(userAgentStr))
                minorVersion = 0;
              else if (!ParserHelper.ParseUserAgentUPnP1MinorVersion(userAgentStr, out minorVersion))
              {
                response.Status = HttpStatusCode.BadRequest;
                SafeSendResponse(response);
                return;
              }
              string mediaType;
              Encoding encoding;
              if (!EncodingUtils.TryParseContentTypeEncoding(contentType, Encoding.UTF8, out mediaType, out encoding))
                throw new ArgumentException("Unable to parse content type");
              if (mediaType != "text/xml")
              { // As specified in (DevArch), 3.2.1
                response.Status = HttpStatusCode.UnsupportedMediaType;
                SafeSendResponse(response);
                return;
              }
              response.AddHeader("DATE", DateTime.Now.ToUniversalTime().ToString("R"));
              response.AddHeader("SERVER", UPnPConfiguration.UPnPMachineInfoHeader);
              response.AddHeader("CONTENT-TYPE", "text/xml; charset=\"utf-8\"");
              string result;
              HttpStatusCode status;
              try
              {
                CallContext callContext = new CallContext(request, context, config);
                status = SOAPHandler.HandleRequest(service, request.Body, encoding, minorVersion >= 1, callContext, out result);
              }
              catch (Exception e)
              {
                UPnPConfiguration.LOGGER.Warn("Action invocation failed", e);
                result = SOAPHandler.CreateFaultDocument(501, "Action failed");
                status = HttpStatusCode.InternalServerError;
              }
              response.Status = status;
              using (var responseStream = new MemoryStream(encoding.GetBytes(result)))
                CompressionHelper.WriteCompressedStream(response, responseStream, canCompress);
              SafeSendResponse(response);
              return;
            }
          }
          else if (request.Method == "SUBSCRIBE" || request.Method == "UNSUBSCRIBE")
          {
            GENAServerController gsc;
            lock (_serverData.SyncObj)
              gsc = _serverData.GENAController;
            if (gsc.HandleHTTPRequest(request, context, config))
              return;
          }
          else
          {
            context.Respond(HttpHelper.HTTP11, HttpStatusCode.MethodNotAllowed, null);
            return;
          }
        }
        // Url didn't match
        context.Respond(HttpHelper.HTTP11, HttpStatusCode.NotFound, null);
        return;
      }
      catch (Exception e)
      {
        UPnPConfiguration.LOGGER.Error("UPnPServer: Error handling HTTP request '{0}'", e, uri);
        IHttpResponse response = request.CreateResponse(context);
        response.Status = HttpStatusCode.InternalServerError;
        SafeSendResponse(response);
        return;
      }
    }

    protected void SafeSendResponse(IHttpResponse response)
    {
      try
      {
        response.Send();
      }
      catch (IOException) { }
    }

    protected void GenerateObjectURLs(EndpointConfiguration config)
    {
      DeviceTreeURLGenerator.GenerateObjectURLs(this, config);
    }

    protected Int32 GenerateConfigId(EndpointConfiguration config)
    {
      Int64 result = config.RootDeviceDescriptionPathsToRootDevices.Values.Select(
          rootDevice => rootDevice.BuildRootDeviceDescription(
              _serverData, config, CultureInfo.InvariantCulture)).Aggregate<string, long>(
                  0, (current, description) => current + HashGenerator.CalculateHash(0, description));
      result = config.SCPDPathsToServices.Values.Select(service => service.BuildSCPDDocument(
          config, _serverData)).Aggregate(result, (current, description) => current + HashGenerator.CalculateHash(0, description));
      result += HashGenerator.CalculateHash(0, NetworkHelper.IPAddrToString(config.EndPointIPAddress));
      result += config.HTTPServerPort;
      result += HashGenerator.CalculateHash(0, config.ControlPathBase + config.DescriptionPathBase + config.EventSubPathBase);
      return (int) result;
    }

    protected void UpdateInterfaceConfiguration()
    {
      InitializeDiscoveryEndpoints();

      _serverData.SSDPController.Update();
    }

    protected void InitializeDiscoveryEndpoints()
    {
      IDictionary<IPAddress, EndpointConfiguration> oldEndpoints = new Dictionary<IPAddress, EndpointConfiguration>();
      foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
        oldEndpoints.Add(config.EndPointIPAddress, config);
      IList<IPAddress> addresses = NetworkHelper.OrderAddressesByScope(NetworkHelper.GetUPnPEnabledIPAddresses());

      // Add new endpoints
      foreach (IPAddress address in addresses)
      {
        if (oldEndpoints.ContainsKey(address))
          continue;
        AddressFamily family = address.AddressFamily;
        if (family == AddressFamily.InterNetwork && !UPnPConfiguration.USE_IPV4)
          continue;
        if (family == AddressFamily.InterNetworkV6 && !UPnPConfiguration.USE_IPV6)
          continue;

        UPnPConfiguration.LOGGER.Debug("UPnPServer: Initializing IP endpoint '{0}'", NetworkHelper.IPAddrToString(address));
        EndpointConfiguration config = new EndpointConfiguration
          {
              EndPointIPAddress = address,
              DescriptionPathBase = DEFAULT_DESCRIPTION_URL_PREFIX,
              ControlPathBase = DEFAULT_CONTROL_URL_PREFIX,
              EventSubPathBase = DEFAULT_EVENT_SUB_URL_PREFIX,
              HTTPServerPort = family == AddressFamily.InterNetwork ? _serverData.HTTP_PORTv4 : _serverData.HTTP_PORTv6
          };
        GenerateObjectURLs(config);
        config.ConfigId = GenerateConfigId(config);
        _serverData.UPnPEndPoints.Add(config);
        _serverData.SSDPController.StartSSDPEndpoint(config);
        _serverData.GENAController.InitializeGENAEndpoint(config);
      }
      // Remove obsolete endpoints
      foreach (EndpointConfiguration config in new List<EndpointConfiguration>(_serverData.UPnPEndPoints))
        if (!addresses.Contains(config.EndPointIPAddress))
        {
          UPnPConfiguration.LOGGER.Debug("UPnPServer: Removing obsolete IP endpoint IP '{0}'", NetworkHelper.IPAddrToString(config.EndPointIPAddress));
          _serverData.GENAController.CloseGENAEndpoint(config);
          _serverData.SSDPController.CloseSSDPEndpoint(config, false);
          _serverData.UPnPEndPoints.Remove(config);
        }
    }

    #endregion
  }
}
