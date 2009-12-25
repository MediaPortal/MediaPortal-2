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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
  /// Delegate for the change event telling subscribers that a server's configuration changed.
  /// </summary>
  /// <param name="server">The server which changed its configuration.</param>
  public delegate void ServerConfigurationChangedDlgt(UPnPServer server);

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
    public static string DEFAULT_DESCRIPTION_URL_PREFIX = "upnphost/description";

    /// <summary>
    /// Prefix which is added to URLs for control requests.
    /// </summary>
    public static string DEFAULT_CONTROL_URL_PREFIX = "upnphost/control";

    /// <summary>
    /// Prefix which is added to URLs for event subsriptions.
    /// </summary>
    public static string DEFAULT_EVENT_SUB_URL_PREFIX = "upnphost/eventing";

    protected ICollection<DvDevice> _rootDevices = new List<DvDevice>();
    protected object _syncObj = new object();
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
      {
        if (!_serverData.IsActive)
          return;
        HandleHTTPRequest(context, e.Request);
      }
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
      foreach (DvDevice rootDevice in _rootDevices)
      {
        DvDevice result = rootDevice.FindDeviceByUDN(deviceUDN);
        if (result != null)
          return result;
      }
      return null;
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
      foreach (DvDevice rootDevice in _rootDevices)
        foreach (DvDevice matchingDevice in rootDevice.FindDevicesByDeviceTypeAndVersion(type, version, searchCompatible))
          yield return matchingDevice;
    }

    /// <summary>
    /// Starts this UPnP server, i.e. starts a network listener and sends notifications about provided devices.
    /// </summary>
    /// <param name="advertisementInterval">Interval in seconds to repeat UPnP advertisements in the network.
    /// The UPnP architecture document () states a minimum of 1800 seconds. For servers which will frequently change their
    /// availability, this value should be short, for more durable serves, this interval can be much longer (maybe a day).</param>
    public void Bind(int advertisementInterval)
    {
      lock (_syncObj)
      {
        if (_serverData.IsActive)
          throw new IllegalCallException("UPnP subsystem mustn't be started multiple times");

        _serverData.HTTPListenerV4 = HttpListener.Create(IPAddress.Any, 0);
        _serverData.HTTPListenerV4.RequestReceived += OnHttpListenerRequestReceived;
        if (Configuration.USE_IPV4)
        {
          _serverData.HTTPListenerV4.Start(DEFAULT_HTTP_REQUEST_QUEUE_SIZE); // Might fail if IPv4 isn't installed
          _serverData.HTTP_PORTv4 = (uint) _serverData.HTTPListenerV4.LocalEndpoint.Port;
        }
        else
          _serverData.HTTP_PORTv4 = 0;

        Configuration.LOGGER.Info("UPnP server: HTTP listener for IPv4 protocol started at port {0}", _serverData.HTTP_PORTv4);

        _serverData.HTTPListenerV6 = HttpListener.Create(IPAddress.IPv6Any, 0); // Might fail if IPv6 isn't installed
        _serverData.HTTPListenerV6.RequestReceived += OnHttpListenerRequestReceived;
        if (Configuration.USE_IPV6)
        {
          _serverData.HTTPListenerV6.Start(DEFAULT_HTTP_REQUEST_QUEUE_SIZE);
          _serverData.HTTP_PORTv6 = (uint) _serverData.HTTPListenerV6.LocalEndpoint.Port;
        }
        else
          _serverData.HTTP_PORTv6 = 0;

        Configuration.LOGGER.Info("UPnP server: HTTP listener for IPv6 protocol started at port {0}", _serverData.HTTP_PORTv6);

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
        Configuration.LOGGER.Info("UPnP server running hosting {0} UPnP root devices", _serverData.Server.RootDevices.Count);
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
      lock (_syncObj)
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
      lock (_syncObj)
      {
        if (!_serverData.IsActive)
          return;
        _serverData.IsActive = false;
        _serverData.GENAController.Close();
        _serverData.SSDPController.Close();
        if (Configuration.USE_IPV4)
          _serverData.HTTPListenerV4.Stop();
        if (Configuration.USE_IPV6)
          _serverData.HTTPListenerV6.Stop();
        _serverData.UPnPEndPoints.Clear();
      }
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
    protected void HandleHTTPRequest(IHttpClientContext context, IHttpRequest request)
    {
      string uri = request.Uri.AbsoluteUri;
      try
      {
        DvService service;
        foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
        {
          // Handle different HTTP methods here
          if (request.Method == "GET")
          { // GET of descriptions
            if (uri.StartsWith(config.DescriptionURLBase))
            {
              string acceptLanguage = request.Headers.Get("ACCEPT-LANGUAGE");
              CultureInfo culture = GetFirstCultureOrDefault(acceptLanguage, CultureInfo.InvariantCulture);
  
              string description = null;
              DvDevice rootDevice;
              if (config.RootDeviceDescriptionURLsToRootDevices.TryGetValue(uri, out rootDevice))
                description = rootDevice.BuildRootDeviceDescription(_serverData, config, culture);
              else if (config.SCPDURLsToServices.TryGetValue(uri, out service))
                description = service.BuildSCPDDocument(config, _serverData);
              if (description != null)
              {
                IHttpResponse response = request.CreateResponse(context);
                response.Status = HttpStatusCode.OK;
                response.ContentType = "text/xml; charset=utf-8";
                if (!string.IsNullOrEmpty(acceptLanguage))
                  response.AddHeader("CONTENT-LANGUAGE", culture.ToString());
                response.Body = new MemoryStream(Encoding.UTF8.GetBytes(description));
                SafeSendResponse(response);
                return;
              }
            }
          }
          else if (request.Method == "POST")
          { // POST of control messages
            if (config.ControlURLsToServices.TryGetValue(uri, out service))
            {
              string contentType = request.Headers.Get("CONTENT-TYPE");
              string userAgentStr = request.Headers.Get("USER-AGENT");
              IHttpResponse response = request.CreateResponse(context);
              int minorVersion;
              if (!ParserHelper.ParseUserAgentUPnP1MinorVersion(userAgentStr, out minorVersion))
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
              response.AddHeader("SERVER", Configuration.UPnPMachineInfoHeader);
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
                Configuration.LOGGER.Warn("Action invocation failed", e);
                result = SOAPHandler.CreateFaultDocument(501, "Action failed");
                status = HttpStatusCode.InternalServerError;
              }
              response.Status = status;
              StreamWriter s = new StreamWriter(response.Body, encoding);
              s.Write(result);
              s.Flush();
              SafeSendResponse(response);
              s.Close();
              return;
            }
          }
          else if (request.Method == "SUBSCRIBE" || request.Method == "UNSUBSCRIBE")
          {
            if (_serverData.GENAController.HandleHTTPRequest(request, context, config))
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
        Configuration.LOGGER.Error("UPnPServer: Error handling HTTP request '{0}'", e, uri);
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
      Int64 result = 0;
      foreach (DvDevice rootDevice in config.RootDeviceDescriptionURLsToRootDevices.Values)
      {
        string description = rootDevice.BuildRootDeviceDescription(_serverData, config, CultureInfo.InvariantCulture);
        result += HashGenerator.CalculateHash(0, description);
      }
      foreach (DvService service in config.SCPDURLsToServices.Values)
      {
        string description = service.BuildSCPDDocument(config, _serverData);
        result += HashGenerator.CalculateHash(0, description);
      }
      result += HashGenerator.CalculateHash(0, config.ControlURLBase + config.DescriptionURLBase + config.EventSubURLBase);
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
      ICollection<IPAddress> addresses = NetworkHelper.GetExternalIPAddresses();

      // Add new endpoints
      foreach (IPAddress address in addresses)
      {
        if (oldEndpoints.ContainsKey(address))
          continue;
        Configuration.LOGGER.Debug("UPnPServer: Initializing IP endpoint '{0}'", address);
        int port = (int) (address.AddressFamily == AddressFamily.InterNetwork ? _serverData.HTTP_PORTv4 : _serverData.HTTP_PORTv6);
        EndpointConfiguration config = new EndpointConfiguration
          {
              EndPointIPAddress = address,
              DescriptionURLBase = new UriBuilder("http", address.ToString(), port, DEFAULT_DESCRIPTION_URL_PREFIX).ToString(),
              ControlURLBase = new UriBuilder("http", address.ToString(), port, DEFAULT_CONTROL_URL_PREFIX).ToString(),
              EventSubURLBase = new UriBuilder("http", address.ToString(), port, DEFAULT_EVENT_SUB_URL_PREFIX).ToString(),
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
          Configuration.LOGGER.Debug("UPnPServer: Removing obsolete IP endpoint IP '{0}'", config.EndPointIPAddress);
          _serverData.GENAController.CloseGENAEndpoint(config);
          _serverData.SSDPController.CloseSSDPEndpoint(config, false);
          _serverData.UPnPEndPoints.Remove(config);
        }
    }

    #endregion
  }
}
