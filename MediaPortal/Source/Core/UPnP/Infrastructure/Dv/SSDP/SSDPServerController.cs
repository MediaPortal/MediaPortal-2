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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Utils.HTTP;
using UPnP.Infrastructure.Utils;
using InvalidDataException=MediaPortal.Utilities.Exceptions.InvalidDataException;

namespace UPnP.Infrastructure.Dv.SSDP
{
  /// <summary>
  /// Active controller class which attends the SSDP protocol in a UPnP server.
  /// </summary>
  public class SSDPServerController : IDisposable
  {
    protected Timer _advertisementTimer = null;
    protected Timer _searchResponseTimer = null;
    protected static Random _rnd = new Random();
    protected ServerData _serverData;

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
      // If we cannot acquire our lock for some reason, avoid blocking an infinite number of timer threads here
      if (Monitor.TryEnter(_serverData.SyncObj, UPnPConsts.TIMEOUT_TIMER_LOCK_ACCESS))
        try
        {
          if (!_serverData.IsActive)
            return;
          Advertise();
          // We currently only sent one set of advertisements for the initial time and each expiration interval
          ReconfigureAdvertisementTimer();
        }
        finally
        {
          Monitor.Exit(_serverData.SyncObj);
        }
      else
        UPnPConfiguration.LOGGER.Error("SSDPServerController.OnAdvertisementTimerElapsed: Cannot acquire synchronization lock. Maybe a deadlock happened.");
    }

    private void OnSearchResponseTimerElapsed(object state)
    {
      lock (_serverData.SyncObj)
      {
        if (!_serverData.IsActive)
          return;
        foreach (PendingSearchRequest ps in _serverData.PendingSearches)
          ProcessSearchRequest(ps);
        _serverData.PendingSearches.Clear();
      }
    }

    private void OnSSDPReceive(IAsyncResult ar)
    {
      lock (_serverData.SyncObj)
        if (!_serverData.IsActive)
          return;
      UDPAsyncReceiveState<EndpointConfiguration> state = (UDPAsyncReceiveState<EndpointConfiguration>) ar.AsyncState;
      EndpointConfiguration config = state.Endpoint;
      Socket socket = state.Socket;
      try
      {
        EndPoint remoteEP = new IPEndPoint(
            state.Endpoint.AddressFamily == AddressFamily.InterNetwork ?
            IPAddress.Any : IPAddress.IPv6Any, 0);
        // To retrieve the remote endpoint address, it is necessary that the SocketOptionName.PacketInformation is set to true on the socket
        using (Stream stream = new MemoryStream(state.Buffer, 0, socket.EndReceiveFrom(ar, ref remoteEP)))
        {
          try
          {
            SimpleHTTPRequest header;
            SimpleHTTPRequest.Parse(stream, out header);
            HandleSSDPRequest(header, config, (IPEndPoint) remoteEP);
          }
          catch (Exception e)
          {
            UPnPConfiguration.LOGGER.Debug(
                "SSDPServerController: Problem parsing incoming packet at IP endpoint '{0}'. Error message: '{1}'", e,
                NetworkHelper.IPAddrToString(config.EndPointIPAddress), e.Message);
          }
        }
        StartReceive(state);
      }
      catch (Exception) // SocketException, ObjectDisposedException
      {
        // Socket was closed - ignore this exception
        UPnPConfiguration.LOGGER.Info("SSDPServerController: Stopping listening for multicast messages at IP endpoint '{0}'",
            NetworkHelper.IPAddrToString(config.EndPointIPAddress));
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
        _searchResponseTimer = new Timer(OnSearchResponseTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        // Wait a random time from 0 to 100 milliseconds, as proposed in the UPnP device architecture specification
        _advertisementTimer = new Timer(OnAdvertisementTimerElapsed, null,
            _rnd.Next(UPnPConsts.INITIAL_ADVERTISEMENT_MAX_WAIT_MS), Timeout.Infinite);
      }
    }

    /// <summary>
    /// Removes the UDP port binding.
    /// </summary>
    public void Close()
    {
      ManualResetEvent notifyObject = new ManualResetEvent(false);
      lock (_serverData.SyncObj)
      {
        RevokeAdvertisements();
        _advertisementTimer.Dispose(notifyObject);
        notifyObject.WaitOne();
        notifyObject.Reset();
        _searchResponseTimer.Dispose(notifyObject);
      }
      notifyObject.WaitOne();
      notifyObject.Close();
      lock (_serverData.SyncObj)
        CloseSSDPEndpoints();
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
      IPAddress address = config.EndPointIPAddress;
      AddressFamily family = address.AddressFamily;
      config.SSDPMulticastAddress = NetworkHelper.GetSSDPMulticastAddressForInterface(address);
      config.SSDPSearchPort = UPnPConsts.DEFAULT_SSDP_SEARCH_PORT;

      // Multicast socket - used for sending and receiving multicast messages
      Socket socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
      config.SSDP_UDP_MulticastReceiveSocket = socket;
      try
      {
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
        NetworkHelper.BindAndConfigureSSDPMulticastSocket(socket, address);
        StartReceive(new UDPAsyncReceiveState<EndpointConfiguration>(config, UPnPConsts.UDP_SSDP_RECEIVE_BUFFER_SIZE, socket));
      }
      catch (Exception) // SocketException, SecurityException
      {
        UPnPConfiguration.LOGGER.Info("SSDPServerController: Unable to bind to multicast address(es) for endpoint '{0}'",
            NetworkHelper.IPAddrToString(address));
      }

      // Unicast sender and receiver socket - used for receiving unicast M-SEARCH queries and sending M-SEARCH responses
      socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
      config.SSDP_UDP_UnicastSocket = socket;
      try
      {
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
        try
        {
          // Try to bind our unicast receiver socket to the default SSDP port
          socket.Bind(new IPEndPoint(address, config.SSDPSearchPort));
        }
        catch (SocketException e)
        {
          if (e.SocketErrorCode != SocketError.AddressAlreadyInUse)
            throw;
          // If binding to the default SSDP port doesn't work, try a random port...
          socket.Bind(new IPEndPoint(address, 0));
          // ... which will be stored in the SSDPSearchPort variable which will be used for the SEARCHPORT.UPNP.ORG SSDP header.
          config.SSDPSearchPort = ((IPEndPoint) socket.LocalEndPoint).Port;
        }
        UPnPConfiguration.LOGGER.Info("UPnPServerController: SSDP enabled for IP endpoint '{0}', search port is {1}",
            NetworkHelper.IPAddrToString(address), config.SSDPSearchPort);
        // The following is necessary to retrieve the remote IP address when we receive SSDP packets
        if (family == AddressFamily.InterNetwork)
          try
          {
            // Receiving options
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            // Sending options
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive,
                UPnPConfiguration.SSDP_UDP_TTL_V4);
          }
          catch (SocketException e)
          {
            UPnPConfiguration.LOGGER.Warn("GENAServerController: Could not set IPv4 options", e);
          }
        else
          try
          {
            // Receiving options
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
            // Sending options
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.HopLimit,
                UPnPConfiguration.SSDP_UDP_HOP_LIMIT_V6);
          }
          catch (SocketException e)
          {
            UPnPConfiguration.LOGGER.Warn("GENAServerController: Could not set IPv6 options", e);
          }
        StartReceive(new UDPAsyncReceiveState<EndpointConfiguration>(config, UPnPConsts.UDP_SSDP_RECEIVE_BUFFER_SIZE, socket));
      }
      catch (Exception) // SocketException, SecurityException
      {
        UPnPConfiguration.LOGGER.Info("SSDPServerController: Unable to bind to unicast address '{0}'",
            NetworkHelper.IPAddrToString(address));
      }
    }

    /// <summary>
    /// Closes the SSDP listeners at all registered network endpoints.
    /// </summary>
    public void CloseSSDPEndpoints()
    {
      foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
        CloseSSDPEndpoint(config, true);
    }

    /// <summary>
    /// Closes the SSDP listener for the given network endpoint.
    /// </summary>
    /// <param name="config">The endpoint configuration which should be closed.</param>
    /// <param name="exitMulticastGroup">If set to <c>true</c>, this method exits the multicast group
    /// before closing the given endpoint <paramref name="config"/>. This should only be done if the
    /// endpoint is still available. If this parameter is set to <c>true</c> and the endpoint is not available
    /// any more, we'll produce a socket error at the socket API, which will be caught by this method.</param>
    public void CloseSSDPEndpoint(EndpointConfiguration config, bool exitMulticastGroup)
    {
      Socket socket = config.SSDP_UDP_MulticastReceiveSocket;
      if (socket != null)
      {
        UPnPConfiguration.LOGGER.Info("UPnPServerController: SSDP disabled for IP endpoint '{0}'",
            NetworkHelper.IPAddrToString(config.EndPointIPAddress));
        config.SSDP_UDP_MulticastReceiveSocket = null;
        NetworkHelper.DisposeSSDPMulticastSocket(socket);
      }
      socket = config.SSDP_UDP_UnicastSocket;
      config.SSDP_UDP_UnicastSocket = null;
      if (socket != null)
        socket.Close();
    }

    protected void StartReceive(UDPAsyncReceiveState<EndpointConfiguration> state)
    {
      EndPoint remoteEP = new IPEndPoint(state.Endpoint.AddressFamily == AddressFamily.InterNetwork ?
          IPAddress.Any : IPAddress.IPv6Any, 0);
      try
      {
        state.Socket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length, SocketFlags.None,
            ref remoteEP, OnSSDPReceive, state);
      }
      catch (Exception e) // SocketException and ObjectDisposedException
      {
        UPnPConfiguration.LOGGER.Info("SSDPServerController: UPnP SSDP subsystem unable to receive from IP address '{0}'", e,
            NetworkHelper.IPAddrToString(state.Endpoint.EndPointIPAddress));
      }
    }

    /// <summary>
    /// Calculates a random interval less than the half of the expiration interval, as supposed by the UPnP device
    /// architecture specification.
    /// </summary>
    /// <returns>Advertisement repetition interval in milliseconds.</returns>
    protected int GetAdvertisementRepetitionTime()
    {
      int advertisementTime = _serverData.AdvertisementExpirationTime;
      if (advertisementTime / 2 < UPnPConsts.MIN_ADVERTISEMENT_INTERVAL)
        return _rnd.Next((advertisementTime/2)*1000);
      return UPnPConsts.MIN_ADVERTISEMENT_INTERVAL * 1000 + _rnd.Next((advertisementTime/2 - UPnPConsts.MIN_ADVERTISEMENT_INTERVAL)*1000);
    }

    protected void ReconfigureAdvertisementTimer()
    {
      _advertisementTimer.Change(GetAdvertisementRepetitionTime(), Timeout.Infinite);
    }

    protected void ReconfigureSearchResponseTimer(int milliseconds)
    {
      _searchResponseTimer.Change(milliseconds, Timeout.Infinite);
    }

    protected void ProcessSearchRequest(PendingSearchRequest ps)
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
            UPnPConfiguration.LOGGER.Debug("SSDPServerController: Problem parsing incoming packet, UPnP device or service search query '{0}'", ps.ST);
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
    /// <exception cref="UnsupportedRequestException">If the specified header denotes an unsupported UPnP version.</exception>
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
        ReconfigureSearchResponseTimer(_rnd.Next(maxDelaySeconds * 1000));
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
