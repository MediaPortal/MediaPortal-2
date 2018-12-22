#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Utilities.Exceptions;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Hosting.Tracing;
using Owin;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Dv.GENA;
using UPnP.Infrastructure.Dv.SOAP;
using UPnP.Infrastructure.Dv.SSDP;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Dv
{
  /// <summary>
  /// Represents a container for all UPnP devices and services and provides the network functionality for the UPnP system.
  /// </summary>
  public class UPnPServer : IDisposable
  {
    /// <summary>
    /// The default port number that is used for http listening. It has to be added to Httpsys Url reservation and the Windows firewall.
    /// Note: When changing this constant here, the installer project needs to be changed as well to match the new port in CustomActions.
    /// </summary>
    public static int DEFAULT_UPNP_AND_SERVICE_PORT_NUMBER = 55555;

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

    //private void OnHttpListenerRequestReceived(object sender, RequestEventArgs e)
    //{
    //  IHttpClientContext context = (IHttpClientContext)sender;
    //  lock (_serverData.SyncObj)
    //    if (!_serverData.IsActive)
    //      return;
    //  HandleHTTPRequest_NoLock(context, e.Request);
    //}

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

        //var port = _serverData.HTTP_PORTv4 = NetworkHelper.GetFreePort(_serverData.HTTP_PORTv4);
        var servicePrefix = "/MediaPortal/UPnPServer_" + Guid.NewGuid().GetHashCode().ToString("X");
        _serverData.ServicePrefix = servicePrefix;
        var startOptions = BuildStartOptions(servicePrefix);

        IDisposable server = null;
        try
        {
          try
          {
            server = WebApp.Start(startOptions, builder => { builder.Use((context, func) => HandleHTTPRequest(context)); });
            UPnPConfiguration.LOGGER.Info("UPnP server: HTTP listener started on addresses {0}", String.Join(", ", startOptions.Urls));
            _serverData.HTTPListeners.Add(server);
          }
          catch (Exception ex)
          {
            if (UPnPConfiguration.IP_ADDRESS_BINDINGS.Count > 0)
              UPnPConfiguration.LOGGER.Warn("UPnP server: Error starting HTTP server with filters. Fallback to no filters", ex);
            else
              throw ex;

            server?.Dispose();
            startOptions = UPnPServer.BuildStartOptions(servicePrefix, new List<string>());
            server = WebApp.Start(startOptions, builder => { builder.Use((context, func) => HandleHTTPRequest(context)); });
            UPnPConfiguration.LOGGER.Info("UPnP server: HTTP listener started on addresses {0}", String.Join(", ", startOptions.Urls));
            _serverData.HTTPListeners.Add(server);
          }
        }
        catch (SocketException e)
        {
          server?.Dispose();
          UPnPConfiguration.LOGGER.Warn("UPnPServer: Error starting HTTP server", e);
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

    public static StartOptions BuildStartOptions(string servicePrefix)
    {
      return BuildStartOptions(servicePrefix, UPnPConfiguration.IP_ADDRESS_BINDINGS);
    }

    public static StartOptions BuildStartOptions(string servicePrefix, List<string> filters)
    {
      ICollection<IPAddress> listenAddresses = new HashSet<IPAddress>();
      if (UPnPConfiguration.USE_IPV4)
        foreach (IPAddress address in NetworkHelper.GetBindableIPAddresses(AddressFamily.InterNetwork, filters))
          listenAddresses.Add(address);
      if (UPnPConfiguration.USE_IPV6)
        foreach (IPAddress address in NetworkHelper.GetBindableIPAddresses(AddressFamily.InterNetworkV6, filters))
          listenAddresses.Add(address);

      StartOptions startOptions = new StartOptions();
      int port = UPnPServer.DEFAULT_UPNP_AND_SERVICE_PORT_NUMBER;
      foreach (IPAddress address in listenAddresses)
      {
        var bindableAddress = NetworkHelper.TranslateBindableAddress(address);
        string formattedAddress = $"http://{bindableAddress}:{port}{servicePrefix}";
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
          if (Equals(address, IPAddress.IPv6Any))
            continue;
          formattedAddress = $"http://[{bindableAddress}]:{port}{servicePrefix}";
        }
        startOptions.Urls.Add(formattedAddress);
      }

      // If no explicit url bindings defined, use the wildcard binding
      if (startOptions.Urls.Count == 0)
      {
        var formattedAddress = $"http://+:{port}{servicePrefix}";
        startOptions.Urls.Add(formattedAddress);
      }

      // Disable built-in owin tracing by using a null traceoutput. It causes crashes by concurrency issues.
      // See: https://stackoverflow.com/questions/17948363/tracelistener-in-owin-self-hosting
      startOptions.Settings.Add(
        typeof(ITraceOutputFactory).FullName,
        typeof(NullTraceOutputFactory).AssemblyQualifiedName);
      return startOptions;
    }

    public class NullTraceOutputFactory : ITraceOutputFactory
    {
      public TextWriter Create(string outputFile)
      {
        // Beware that there's a multi threaded race condition using StreamWriter.Null, since it's also used by Console.Write* when no console is attached, e.g. from Windows Services.
        // It's better to use TextWriter.Synchronized(new StreamWriter(Stream.Null)) instead.
        return TextWriter.Synchronized(new StreamWriter(Stream.Null));
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
      _serverData.HTTPListeners.ForEach(x => x.Dispose());
      _serverData.HTTPListeners.Clear();
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
    protected async Task HandleHTTPRequest(IOwinContext context)
    {
      var request = context.Request;
      var response = context.Response;
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
                  description = rootDevice.BuildRootDeviceDescription(request, _serverData, config, culture);
                else if (config.SCPDPathsToServices.TryGetValue(pathAndQuery, out service))
                  description = service.BuildSCPDDocument(config, _serverData);
              if (description != null)
              {
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "text/xml; charset=utf-8";
                if (!string.IsNullOrEmpty(acceptLanguage))
                  response.Headers["CONTENT-LANGUAGE"] = culture.ToString();
                using (MemoryStream responseStream = new MemoryStream(UPnPConsts.UTF8_NO_BOM.GetBytes(description)))
                  await CompressionHelper.WriteCompressedStream(acceptEncoding, response, responseStream);
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
              int minorVersion;
              if (string.IsNullOrEmpty(userAgentStr))
                minorVersion = 0;
              else if (!ParserHelper.ParseUserAgentUPnP1MinorVersion(userAgentStr, out minorVersion))
              {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
              }
              string mediaType;
              Encoding encoding;
              if (!EncodingUtils.TryParseContentTypeEncoding(contentType, Encoding.UTF8, out mediaType, out encoding))
                throw new ArgumentException("Unable to parse content type");
              if (mediaType != "text/xml")
              { // As specified in (DevArch), 3.2.1
                response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
                return;
              }
              response.Headers["DATE"] = DateTime.Now.ToUniversalTime().ToString("R");
              response.Headers["SERVER"] = UPnPConfiguration.UPnPMachineInfoHeader;
              response.Headers["CONTENT-TYPE"] = "text/xml; charset=\"utf-8\"";
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
              response.StatusCode = (int)status;
              using (MemoryStream responseStream = new MemoryStream(encoding.GetBytes(result)))
                await CompressionHelper.WriteCompressedStream(acceptEncoding, response, responseStream);
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
            context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            //context.Respond(HttpHelper.HTTP11, HttpStatusCode.MethodNotAllowed, null);
            return;
          }
        }
        // Url didn't match
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        //context.Respond(HttpHelper.HTTP11, HttpStatusCode.NotFound, null);
      }
      catch (Exception e)
      {
        UPnPConfiguration.LOGGER.Error("UPnPServer: Error handling HTTP request '{0}'", e, uri);
        response.StatusCode = (int)HttpStatusCode.InternalServerError;
      }
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
      //result += HashGenerator.CalculateHash(0, config.ServicePrefix);
      result += HashGenerator.CalculateHash(0, config.ControlPathBase + config.DescriptionPathBase + config.EventSubPathBase);
      return (int)result;
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
      IList<IPAddress> addresses = NetworkHelper.OrderAddressesByScope(NetworkHelper.GetUPnPEnabledIPAddresses(UPnPConfiguration.IP_ADDRESS_BINDINGS));

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
          DescriptionPathBase = _serverData.ServicePrefix + DEFAULT_DESCRIPTION_URL_PREFIX,
          ControlPathBase = _serverData.ServicePrefix + DEFAULT_CONTROL_URL_PREFIX,
          EventSubPathBase = _serverData.ServicePrefix + DEFAULT_EVENT_SUB_URL_PREFIX,
          //ServicePrefix = _serverData.ServicePrefix,
          //HTTPServerPort = family == AddressFamily.InterNetwork ? _serverData.HTTP_PORTv4 : _serverData.HTTP_PORTv6
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
