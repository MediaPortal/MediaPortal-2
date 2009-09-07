using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
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

    /// <summary>
    /// HTTP listening port for event subscription requests.
    /// </summary>
    public static int DEFAULT_HTTP_EVENT_SUBSCRIPTION_PORT = 8081;

    protected ICollection<DvDevice> _rootDevices = new List<DvDevice>();
    protected object _syncObj = new object();
    protected ServerData _serverData = new ServerData();
    
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
      if (_serverData.IsActive)
        throw new IllegalCallException("UPnP subsystem mustn't be started multiple times");

      _serverData.HTTPListener = HttpListener.Create(IPAddress.Any, 0);
      _serverData.HTTPListener.RequestReceived += OnHttpListenerRequestReceived;
      _serverData.HTTPListener.Start(DEFAULT_HTTP_REQUEST_QUEUE_SIZE);
      _serverData.HTTP_PORT = (uint) _serverData.HTTPListener.LocalEndpoint.Port;

      _serverData.SSDPController = new SSDPServerController(_serverData)
        {
            AdvertisementExpirationTime = advertisementInterval
        };
      _serverData.GENAController = new GENAServerController(_serverData);

      InitializeDiscoveryEndpoints();
      _serverData.SSDPController.Start();
      _serverData.GENAController.Start();

      NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
      _serverData.IsActive = true;
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
      _serverData.ConfigId++;
      foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
        GenerateObjectURLs(config);
      _serverData.SSDPController.Advertise();
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
        _serverData.HTTPListener.Stop();
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
      try
      {
        string uri = request.Uri.AbsoluteUri;
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
                description = service.BuildSCDPDocument(_serverData);
              if (description != null)
              {
                IHttpResponse response = request.CreateResponse(context);
                response.Status = HttpStatusCode.OK;
                response.ContentType = "text/xml; charset=utf-8";
                if (!string.IsNullOrEmpty(acceptLanguage))
                  response.AddHeader("CONTENT-LANGUAGE", culture.ToString());
                response.Body = new MemoryStream(Encoding.UTF8.GetBytes(description));
                response.Send();
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
                response.Send();
                return;
              }
              string mediaType;
              Encoding encoding;
              if (!EncodingUtils.TryParseContentTypeEncoding(contentType, Encoding.UTF8, out mediaType, out encoding))
                throw new ArgumentException("Unable to parse content type");
              if (mediaType != "text/xml")
              { // As specified in (DevArch), 3.2.1
                response.Status = HttpStatusCode.UnsupportedMediaType;
                response.Send();
                return;
              }
              response.AddHeader("DATE", DateTime.Now.ToString("R"));
              response.AddHeader("SERVER", Configuration.UPnPMachineInfoHeader);
              string result;
              HttpStatusCode status = SOAPHandler.HandleRequest(service, request.Body, encoding, minorVersion >= 1, out result);
              if (result != null)
              {
                response.AddHeader("CONTENT-TYPE", "text/xml; charset=\"utf-8\"");
                StreamWriter s = new StreamWriter(response.Body, encoding);
                s.Write(result);
              } // else: request will be ignored
              response.Status = status;
              response.Send();
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
      catch (Exception)
      {
        IHttpResponse response = request.CreateResponse(context);
        response.Status = HttpStatusCode.InternalServerError;
        response.Send();
        return;
      }
    }

    protected void GenerateObjectURLs(EndpointConfiguration config)
    {
      DeviceTreeURLGenerator.GenerateObjectURLs(this, config);
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
      ICollection<IPAddress> addresses = NetworkHelper.GetLocalIPAddresses();

      // Add new endpoints
      foreach (IPAddress address in addresses)
      {
        if (oldEndpoints.ContainsKey(address))
          continue;
        EndpointConfiguration config = new EndpointConfiguration
          {
              EndPointIPAddress = address,
              DescriptionURLBase = string.Format(
                  "http://{0}/{1}", new IPEndPoint(address, (int) _serverData.HTTP_PORT), DEFAULT_DESCRIPTION_URL_PREFIX),
              ControlURLBase = string.Format(
                  "http://{0}/{1}", new IPEndPoint(address, (int) _serverData.HTTP_PORT), DEFAULT_CONTROL_URL_PREFIX),
              EventSubURLBase = string.Format(
                  "http://{0}/{1}", new IPEndPoint(address, DEFAULT_HTTP_EVENT_SUBSCRIPTION_PORT), DEFAULT_EVENT_SUB_URL_PREFIX)
          };
        GenerateObjectURLs(config);
        _serverData.UPnPEndPoints.Add(config);
        _serverData.SSDPController.StartSSDPEndpoint(config);
        _serverData.GENAController.InitializeGENAEndpoint(config);
      }
      // Remove obsolete endpoints
      foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
        if (!addresses.Contains(config.EndPointIPAddress))
        {
          _serverData.GENAController.CloseGENAEndpoint(config);
          _serverData.SSDPController.CloseSSDPEndpoint(config);
          _serverData.UPnPEndPoints.Remove(config);
        }
    }

    #endregion

  }
}
