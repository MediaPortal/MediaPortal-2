#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Dv.HTTP;
using UPnP.Infrastructure.Utils;
using InvalidDataException=MediaPortal.Utilities.Exceptions.InvalidDataException;

namespace UPnP.Infrastructure.Dv.SSDP
{
  /// <summary>
  /// Active controller class which attends the SSDP protocol in a UPnP server.
  /// </summary>
  public class SSDPServerController : IDisposable
  {
    /// <summary>
    /// Receive buffer size for the UDP socket.
    /// </summary>
    public static int UDP_RECEIVE_BUFFER_SIZE = 4096;

    /// <summary>
    /// Maximum random time in milliseconds to wait until an initial advertisement of all devices is made.
    /// </summary>
    public static int INITIAL_ADVERTISEMENT_MAX_WAIT_MS = 100;

    /// <summary>
    /// Time in seconds until UPnP advertisments will expire.
    /// </summary>
    public static int DEFAULT_ADVERTISEMENT_EXPIRATION_TIME = 1800;

    /// <summary>
    /// Minimum advertisement interval in seconds.
    /// </summary>
    public static int MIN_ADVERTISEMENT_INTERVAL = 600;

    protected Timer _advertisementTimer = null;
    protected Timer _searchResponseTimer = null;
    protected static Random rnd = new Random();
    protected ServerData _serverData;

    protected class UDPAsyncReceiveState
    {
      protected byte[] _buffer;
      protected EndpointConfiguration _config;
      protected Socket _socket;

      public UDPAsyncReceiveState(EndpointConfiguration config, int bufferSize, Socket socket)
      {
        _config = config;
        _buffer = new byte[bufferSize];
        _socket = socket;
      }

      public EndpointConfiguration Endpoint
      {
        get { return _config; }
      }

      public Socket Socket
      {
        get { return _socket; }
      }

      public byte[] Buffer
      {
        get { return _buffer; }
      }
    }

    /// <summary>
    /// Creates a new <see cref="SSDPServerController"/> for a <see cref="UPnPServer"/>.
    /// </summary>
    /// <param name="serverData">The UPnP server configuration data structure to use.</param>
    public SSDPServerController(ServerData serverData)
    {
      _serverData = serverData;
    }

    public void Dispose()
    {
      Close();
    }

    #region Eventhandlers

    private void OnAdvertisementTimerElapsed(object state)
    {
      lock (_serverData.SyncObj)
      {
        if (!_serverData.IsActive)
          return;
        Advertise();
        // We currently only sent one set of advertisements for the initial time and each expiration interval
        ReconfigureAdvertisementTimer();
      }
    }

    private void OnSearchResponseTimerElapsed(object state)
    {
      lock (_serverData.SyncObj)
      {
        if (!_serverData.IsActive)
          return;
        foreach (PendingSearchRequest ps in _serverData.PendingSearches)
          ProcessSearch(ps);
        _serverData.PendingSearches.Clear();
      }
    }

    private void OnSSDPReceive(IAsyncResult ar)
    {
      lock (_serverData.SyncObj)
        if (!_serverData.IsActive)
          return;
      UDPAsyncReceiveState state = (UDPAsyncReceiveState) ar.AsyncState;
      EndpointConfiguration config = state.Endpoint;
      Socket socket = state.Socket;
      try
      {
        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        Stream stream = new MemoryStream(state.Buffer, 0, socket.EndReceiveFrom(ar, ref remoteEP));
        try
        {
          SimpleHTTPRequest header;
          SimpleHTTPRequest.Parse(stream, out header);
          HandleSSDPRequest(header, config, (IPEndPoint) remoteEP);
        }
        catch (Exception e)
        {
          Configuration.LOGGER.Debug("SSDPServerController: Problem parsing incoming packet. Error message: '{0}'", e.Message);
          NetworkHelper.DiscardInput(stream);
        }
        StartReceive(state);
      }
      catch (ObjectDisposedException)
      {
        // Socket was closed - ignore this exception
      }
    }

    #endregion

    /// <summary>
    /// Gets or sets the time in seconds after that the UPnP device advertisements will expire.
    /// The advertisements will be repeated before the old advertisements expire.
    /// </summary>
    public int AdvertisementExpirationTime
    {
      get
      {
        lock (_serverData.SyncObj)
          return _serverData.AdvertisementExpirationTime;
      }
      set
      {
        lock (_serverData.SyncObj)
          _serverData.AdvertisementExpirationTime = value;
      }
    }

    /// <summary>
    /// Starts the SSDP system, i.e. starts internal timers.
    /// </summary>
    public void Start()
    {
      lock (_serverData.SyncObj)
      {
        // Wait a random time from 0 to 100 milliseconds, as proposed in the UPnP device architecture specification
        _advertisementTimer = new Timer(OnAdvertisementTimerElapsed, null,
            rnd.Next(INITIAL_ADVERTISEMENT_MAX_WAIT_MS), Timeout.Infinite);
        _searchResponseTimer = new Timer(OnSearchResponseTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
      }
    }

    /// <summary>
    /// Removes the UDP port binding.
    /// </summary>
    public void Close()
    {
      lock (_serverData.SyncObj)
      {
        RevokeAdvertisements();
        _advertisementTimer.Dispose();
        _searchResponseTimer.Dispose();
        CloseSSDPEndpoints();
      }
    }

    /// <summary>
    /// Advertises the UPnP server to the network.
    /// </summary>
    /// <remarks>
    /// Has to be called:
    /// <list type="bullet">
    /// <item>Periodically</item>
    /// <item>After the set of UPnP endpoints has changed</item>
    /// <item>When the underlaying UPnP server changes its configuration</item>
    /// </list>
    /// </remarks>
    public void Advertise()
    {
      DeviceTreeNotificationProducer.SendMessagesServer(_serverData.Server, new AliveMessageSender(_serverData));
    }

    /// <summary>
    /// Updates the UPnP server advertisements.
    /// </summary>
    /// <remarks>
    /// Has to be called when the set of UPnP endpoints changes.
    /// </remarks>
    public void Update()
    {
      int nextBootId = _serverData.BootId + 1;
      DeviceTreeNotificationProducer.SendMessagesServer(_serverData.Server, new UpdateMessageSender(_serverData, _serverData.BootId, nextBootId));
      _serverData.BootId = nextBootId;
    }

    /// <summary>
    /// Revokes all UPnP server advertisements.
    /// </summary>
    public void RevokeAdvertisements()
    {
      DeviceTreeNotificationProducer.SendMessagesServer(_serverData.Server, new ByeByeMessageSender(_serverData));
    }

    /// <summary>
    /// Starts the SSDP listeners at all registered network endpoints.
    /// </summary>
    public void StartSSDPEndpoints()
    {
      foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
        StartSSDPEndpoint(config);
    }

    /// <summary>
    /// Configures the SSDP part of the given endpoint <paramref name="config"/>.
    /// Starts the SSDP UDP listener client for the given network endpoint.
    /// </summary>
    /// <param name="config">The endpoint configuration which should be started.</param>
    public void StartSSDPEndpoint(EndpointConfiguration config)
    {
      AddressFamily family = config.EndPointIPAddress.AddressFamily;
      if (family == AddressFamily.InterNetwork)
        config.SSDPMulticastAddress = Consts.SSDP_MULTICAST_ADDRESS_V4;
      else if (family == AddressFamily.InterNetworkV6)
        config.SSDPMulticastAddress = Consts.SSDP_MULTICAST_ADDRESS_V6;
      else
        return;
      config.SSDPSearchPort = Consts.DEFAULT_SSDP_SEARCH_PORT;

      // Multicast receiver socket - used for receiving multicast messages
      Socket socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
      socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
      // Need to bind the multicast socket to the multicast port. Which meaning does the IP address have?
      socket.Bind(new IPEndPoint(config.EndPointIPAddress, Consts.SSDP_MULTICAST_PORT));
      if (family == AddressFamily.InterNetwork)
      {
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
            new MulticastOption(config.SSDPMulticastAddress));
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
      }
      else
      {
        socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership,
            new IPv6MulticastOption(config.SSDPMulticastAddress));
        socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
      }

      config.SSDP_UDP_MulticastReceiveSocket = socket;
      UDPAsyncReceiveState state = new UDPAsyncReceiveState(config, UDP_RECEIVE_BUFFER_SIZE, socket);
      StartReceive(state);

      // Unicast sender and receiver socket - used for receiving unicast M-SEARCH queries and sending M-SEARCH responses
      socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
      try
      {
        // Try to bind our unicast receiver socket to the default SSDP port
        socket.Bind(new IPEndPoint(config.EndPointIPAddress, config.SSDPSearchPort));
      }
      catch (SocketException e)
      {
        if (e.SocketErrorCode != SocketError.AddressAlreadyInUse)
          throw;
        // If binding to the default SSDP port doesn't work, try a random port...
        socket.Bind(new IPEndPoint(config.EndPointIPAddress, 0));
        // ... which will be stored in the SSDPSearchPort variable which will be used for the SEARCHPORT.UPNP.ORG SSDP header.
        config.SSDPSearchPort = ((IPEndPoint) socket.LocalEndPoint).Port;
      }
      if (family == AddressFamily.InterNetwork)
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
      else
        socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
      config.SSDP_UDP_UnicastSocket = socket;
      state = new UDPAsyncReceiveState(config, UDP_RECEIVE_BUFFER_SIZE, socket);
      StartReceive(state);
    }

    /// <summary>
    /// Closes the SSDP listeners at all registered network endpoints.
    /// </summary>
    public void CloseSSDPEndpoints()
    {
      foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
        CloseSSDPEndpoint(config);
    }

    /// <summary>
    /// Closes the SSDP listener for the given network endpoint.
    /// </summary>
    /// <param name="config">The endpoint configuration which should be closed.</param>
    public void CloseSSDPEndpoint(EndpointConfiguration config)
    {
      Socket socket = config.SSDP_UDP_MulticastReceiveSocket;
      socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership,
          new MulticastOption(config.SSDPMulticastAddress));
      socket.Close();
      config.SSDP_UDP_UnicastSocket.Close();
    }

    protected void StartReceive(UDPAsyncReceiveState state)
    {
      EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
      state.Socket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length, SocketFlags.None,
          ref remoteEP, OnSSDPReceive, state);
    }

    /// <summary>
    /// Calculates a random interval less than the half of the expiration interval, as supposed by the UPnP device
    /// architecture specification.
    /// </summary>
    /// <returns>Advertisement repetition interval in milliseconds.</returns>
    protected int GetAdvertisementRepetitionTime()
    {
      int advertisementTime = _serverData.AdvertisementExpirationTime;
      if (advertisementTime / 2 < MIN_ADVERTISEMENT_INTERVAL)
        return rnd.Next((advertisementTime/2)*1000);
      else
        return MIN_ADVERTISEMENT_INTERVAL * 1000 + rnd.Next((advertisementTime/2 - MIN_ADVERTISEMENT_INTERVAL)*1000);
    }

    protected void ReconfigureAdvertisementTimer()
    {
      _advertisementTimer.Change(GetAdvertisementRepetitionTime(), Timeout.Infinite);
    }

    protected void ReconfigureSearchResponseTimer(int milliseconds)
    {
      _searchResponseTimer.Change(milliseconds, Timeout.Infinite);
    }

    protected void ProcessSearch(PendingSearchRequest ps)
    {
      lock (_serverData.SyncObj)
      {
        SearchResultMessageSender srms = new SearchResultMessageSender(_serverData, ps.LocalEndpointConfiguration, ps.RequesterEndPoint);
        if (ps.ST == "ssdp:all")
          DeviceTreeNotificationProducer.SendMessagesServer(_serverData.Server, srms);
        else if (ps.ST == "upnp:rootdevice")
        { // Search root device
          foreach (DvDevice rootDevice in _serverData.Server.RootDevices)
          {
            string deviceUDN = rootDevice.UDN;
            srms.SendMessage("upnp:rootdevice", deviceUDN + "::upnp:rootdevice", rootDevice);
          }
        }
        else if (ps.ST.StartsWith("uuid:"))
        { // Search by device id
          string deviceUDN = ps.ST;
          DvDevice device = _serverData.Server.FindDeviceByUDN(deviceUDN);
          if (device != null)
            srms.SendMessage(deviceUDN, deviceUDN, device.RootDevice);
        }
        else if (ps.ST.StartsWith("urn:") && (ps.ST.IndexOf(":device:") > -1 || ps.ST.IndexOf(":service:") > -1))
        { // Search by device type or service type and version
          string type;
          int version;
          if (!ParserHelper.TryParseTypeVersion_URN(ps.ST, out type, out version))
          {
            Configuration.LOGGER.Debug("SSDPServerController: Problem parsing incoming packet, UPnP device or service search query '{0}'", ps.ST);
            return;
          }
          if (type.IndexOf(":device:") > -1)
          {
            IEnumerable<DvDevice> devices = _serverData.Server.FindDevicesByDeviceTypeAndVersion(
                type, version, true);
            foreach (DvDevice device in devices)
              srms.SendMessage(device.DeviceTypeVersion_URN, device.UDN + "::" + device.DeviceTypeVersion_URN, device.RootDevice);
          }
          else if (type.IndexOf(":service:") > -1)
          {
            foreach (DvDevice rootDevice in _serverData.Server.RootDevices)
            {
              IEnumerable<DvService> services = rootDevice.FindServicesByServiceTypeAndVersion(
                  type, version, true);
              foreach (DvService service in services)
                srms.SendMessage(service.ServiceTypeVersion_URN, service.ParentDevice.UDN + "::" + service.ServiceTypeVersion_URN, service.ParentDevice.RootDevice);
            }
          }
        }
      }
    }

    /// <summary>
    /// Checks the specified <paramref name="userAgentStr"/> for validity.
    /// </summary>
    /// <param name="userAgentStr">USER-AGENT header entry of a UPnP discovery request.</param>
    /// <exception cref="MediaPortal.Utilities.Exceptions.InvalidDataException">If the specified header value is malformed.</exception>
    /// <exception cref="UnsupportedRequestException">If the specified header denotes an unsupported client.</exception>
    protected static void CheckUserAgentUPnPVersion(string userAgentStr)
    {
      int minorVersion;
      if (!ParserHelper.ParseUserAgentUPnP1MinorVersion(userAgentStr, out minorVersion))
        throw new UnsupportedRequestException("Unsupported UPnP version in USER-AGENT header entry: '{0}'", userAgentStr);
    }

    /// <summary>
    /// Adds the given <paramref name="ps"/> data to the collection of pending searches and reconfigures the internal
    /// timer to send pending search responses in max. <paramref name="maxDelaySeconds"/> seconds.
    /// </summary>
    /// <param name="ps">Data containing the pending search request.</param>
    /// <param name="maxDelaySeconds">The search response will be send in a random time from 0 to
    /// <paramref name="maxDelaySeconds"/> seconds.</param>
    protected void DelaySearchResponse(PendingSearchRequest ps, int maxDelaySeconds)
    {
      lock (_serverData.SyncObj)
      {
        _serverData.PendingSearches.Add(ps);
        ReconfigureSearchResponseTimer(rnd.Next(maxDelaySeconds * 1000));
      }
    }

    /// <summary>
    /// Handles SSDP M-SEARCH requests over UDP multicast and unicast.
    /// </summary>
    /// <param name="header">HTTP request header of the request to handle.</param>
    /// <param name="config">Endpoint configuration which received the request.</param>
    /// <param name="remoteEP">Remote endpoint which sent the request.</param>
    protected void HandleSSDPRequest(SimpleHTTPRequest header, EndpointConfiguration config, IPEndPoint remoteEP)
    {
      switch (header.Method)
      {
        case "M-SEARCH":
          if (header.Param != "*" || header["MAN"] != "\"ssdp:discover\"")
            throw new InvalidDataException("Unsupported Request");
          // We don't make a difference between multicast and unicast search requests here,
          // the only difference is the existance of the MX header field.
          // If it is present, we simply use this random delay for the answer, and if it is
          // not present, we send the answer at once.
          int mx; // Max. seconds to delay response
          if (!int.TryParse(header["MX"], out mx))
            mx = 0;
          else if (mx < 1)
              // Malformed request
            throw new InvalidDataException("Invalid MX header value");
          if (mx > 5)
            mx = 5; // Should be bounded to 5, according to (DevArch)
          string st = header["ST"]; // Search target
          if (header.ContainsHeader("USER-AGENT")) // Optional
            CheckUserAgentUPnPVersion(header["USER-AGENT"]);
          DelaySearchResponse(new PendingSearchRequest(st, config, remoteEP), mx);
          break;
      }
    }
  }
}
