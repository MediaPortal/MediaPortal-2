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
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MediaPortal.Utilities.Network.Netbios
{
  /// <summary>
  /// This class is used to send and receive Netbios Name Service packets
  /// </summary>
  public class NbNsClient : IDisposable
  {
    #region Consts

    private const int TARGET_PORT = 137;
    private const int SEND_TIMEOUT = 500;
    private const int BCAST_REQ_RETRY_TIMEOUT = 250;
    private const int BCAST_REQ_RETRY_COUNT = 3;
    private const int RECEIVE_BUFFER_SIZE = NbNsPacketBase.MAX_DATAGRAM_LENGTH;

    #endregion

    #region Private Classes

    /// <summary>
    /// This class is used to identify Netbios Name Service packets and answers to a specific packet
    /// </summary>
    /// <remarks>
    /// Every Netbios Name Service packet has a unique transaction Id in its header (<see cref="NbNsHeader.NameTrnId"/>).
    /// When a Netbios Name Service request is sent, the respective respone has the same transaction Id in its header so
    /// that we can attribute the response to the request. The transaction Id is only two bytes, i.e. it has a maximum value
    /// of 65535. When sending many requests, it may therefore be that two requests have the same Id and, hence, it is not
    /// possible only be way of the transaction Id to identify the response to a given request. It is therefore recommended
    /// to use both, the transaction Id and the IP address to which a request was sent, to identify the response. I.e. if
    /// we receive a response with a specific transaction Id from a specific IP address, it is the response to the request
    /// that was sent to that specific IP address with the specific transaction Id.
    /// Therefore we store IP address and transaction Id in this class. Since we use this class as key in an IDictionary, it
    /// has all necessary equality methods implemented and overriden, so that two instances of this class are equal if
    /// the IP address is equal and the transaction Id is equal.
    /// </remarks>
    private class PacketIdentifier : IEquatable<PacketIdentifier>
    {
      private UInt16 TrnId { get; set; }
      private IPAddress IpAddress { get; set; }

      public PacketIdentifier(UInt16 id, IPAddress address)
      {
        if (address == null)
          throw new ArgumentNullException("address");
        TrnId = id;
        IpAddress = address;
      }

      public bool Equals(PacketIdentifier identifier)
      {
        if (ReferenceEquals(identifier, null))
          return false;
        return TrnId == identifier.TrnId && IpAddress.Equals(identifier.IpAddress);
      }

      public override bool Equals(object obj)
      {
        return Equals(obj as PacketIdentifier);
      }

      public override int GetHashCode()
      {
        return TrnId.GetHashCode() ^ IpAddress.GetHashCode();
      }

      public static bool operator ==(PacketIdentifier identifier1, PacketIdentifier identifier2)
      {
        return ReferenceEquals(identifier1, null) ? ReferenceEquals(identifier2, null) : identifier1.Equals(identifier2);
      }

      public static bool operator !=(PacketIdentifier identifier1, PacketIdentifier identifier2)
      {
        return !(identifier1 == identifier2);
      }
    }

    #endregion

    #region Private fields

    // Socket used for send and receive operations
    private readonly Socket _socket;
    
    // Dictionary used to temporarily store the send requests to be able to find the matching response
    private readonly ConcurrentDictionary<PacketIdentifier, TaskCompletionSource<NbNsPacketBase>> _pendingUnicastRequests;
    
    // Task used for recieving Netbios Name Service packets
    private readonly Task _receiverTask;
    
    // When this TaskCompletionSource is completed, the _receiverTask stops receiving packets
    private readonly TaskCompletionSource<object> _endReceive;

      #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="NbNsClient"/>
    /// </summary>
    /// <param name="sourcePort">
    /// Local port used to send and receive Netbios Name Service packets. The default value of 0 lets the
    /// socket subsystem choose a random free port above 1023.
    /// </param>
    /// <remarks>
    /// The constructor in particular creates and binds the socket and starts the task that receives packets.
    /// </remarks>
    public NbNsClient(int sourcePort = 0)
    {
      if (sourcePort < 0 || sourcePort > 65535)
        throw new ArgumentOutOfRangeException("sourcePort");

      _pendingUnicastRequests = new ConcurrentDictionary<PacketIdentifier, TaskCompletionSource<NbNsPacketBase>>();

      _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      _socket.Bind(new IPEndPoint(IPAddress.Any, 0));
      _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);

      _endReceive = new TaskCompletionSource<object>();
      _receiverTask = Receive();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Method used in <see cref="_receiverTask"/> to receive Netbios Name Service packets
    /// </summary>
    /// <returns>Task that completes when the <see cref="_receiverTask"/> stops receiving packets</returns>
    private async Task Receive()
    {
      var buffer = new byte[RECEIVE_BUFFER_SIZE];
      EndPoint localEndPoint = new IPEndPoint(0, 0);

      while (true)
      {
        var tcs = new TaskCompletionSource<EndPoint>();
        try
        {
          _socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref localEndPoint, asynchronousResult =>
          {
            var t = (TaskCompletionSource<EndPoint>)asynchronousResult.AsyncState;
            EndPoint ep = new IPEndPoint(0, 0);
            try
            {
              _socket.EndReceiveFrom(asynchronousResult, ref ep);
              t.TrySetResult(ep);
            }
            catch (Exception)
            {
              t.TrySetResult(null);
            }
          }, tcs);
        }
        catch (Exception)
        {
          if (_endReceive.Task.IsCompleted)
            return;
          continue;
        }

        // Wait until a new packet has been received or NbNsClient is disposed
        if (await Task.WhenAny(tcs.Task, _endReceive.Task) == _endReceive.Task)
          return;

        // RemoteEndPoint is null when there was an exception in EndReceive. In that case, discard the packet.
        var remoteEndPoint = (await tcs.Task) as IPEndPoint;
        if (remoteEndPoint == null)
          continue;

        // If we cannot parse the received packet, discard it.
        NbNsPacketBase packet;
        if (!NbNsPacketBase.TryParse(buffer, out packet))
          continue;
        
        // If the received packet is the response to a known request, set the request task result to the received packet.
        var identifier = new PacketIdentifier(packet.Header.NameTrnId, remoteEndPoint.Address);
        TaskCompletionSource<NbNsPacketBase> result;
        if (_pendingUnicastRequests.TryGetValue(identifier, out result))
          result.TrySetResult(packet);
      }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Sends a Netbios Name Service packet asynchronously
    /// </summary>
    /// <param name="packet">Netbios Name Service packet to send</param>
    /// <param name="address">Target address to send the packet to</param>
    /// <param name="timeOutMilliseconds">
    /// Maximum time in milliseconds to wait for the send operation to finish. If it takes longer, the target IP address
    /// most likely does not exist in a local subnet (although the address is part of a local subnet) so that the ARP
    /// protocol is not able to find the MAC address for the given IP address.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that completes with result = <c>true</c> when the packet was successfully sent or
    /// with result = <c>false</c> if the send operation was not successful witin the given timeout.
    /// </returns>
    public async Task<bool> SendPacketAsync(NbNsPacketBase packet, IPAddress address, int timeOutMilliseconds = SEND_TIMEOUT)
    {
      if (packet == null)
        throw new ArgumentNullException("packet");
      if (address == null)
        throw new ArgumentNullException("address");
      var endPoint = new IPEndPoint(address, TARGET_PORT);
      var tcs = new TaskCompletionSource<int>();
      try
      {
        _socket.BeginSendTo(packet, 0, packet.Length, SocketFlags.None, endPoint, asynchronousResult =>
        {
          var t = (TaskCompletionSource<int>)asynchronousResult.AsyncState;
          try
          {
            t.TrySetResult(_socket.EndSendTo(asynchronousResult));
          }
          catch (Exception)
          {
            t.TrySetResult(0);
          }
        }, tcs);
      }
      catch (Exception)
      {
        return false;
      }
      if (await Task.WhenAny(tcs.Task, Task.Delay(timeOutMilliseconds)) != tcs.Task)
        return false;
      
      // The send operation was successful if as many bytes as are contained in the packet were actually sent.
      return packet.Length == await tcs.Task;
    }

    /// <summary>
    /// Sends a Netbios Name Service request to a unicast address and returns the respective response
    /// </summary>
    /// <param name="packet">Netbios Name Service packet to send</param>
    /// <param name="address">Target address to send the packet to</param>
    /// <param name="timeOutMilliseconds">
    /// If after this timeout following sending the request packet no response has been received, the request is sent again.
    /// This is tried <see cref="numRetries"/> times. We intentionally use the "wrong" timeout value here. For unicast
    /// requests RFC 1002 defines a UCAST_REQ_RETRY_TIMEOUT of 5 seconds. However, with the default retry count of 3, this
    /// results in a maximum time of 15 seconds until we can be sure that no response was sent. This is extremely long and
    /// tests have shown that even in a WLan, computers respond to requests withing less than 100 ms. We therefore by default
    /// use the BCAST_REQ_RETRY_TIMEOUT of 250ms, which is meant for broadcast requests, also for unicast requests.
    /// </param>
    /// <param name="numRetries">Number of retries. Default is 3.</param>
    /// <returns>
    /// A <see cref="Task"/> that completes when either (a) the respective response was received (in which case the result value
    /// of the Task contains the response packet) or (b) when the send operation was not successful or (c) when after <see cref="numRetries"/>
    /// retries no response was received (in the latter two cases the result value of the Task is <c>null</c>).
    /// </returns>
    public async Task<NbNsPacketBase> SendUnicastRequestAsync(NbNsPacketBase packet, IPAddress address, int timeOutMilliseconds = BCAST_REQ_RETRY_TIMEOUT, int numRetries = BCAST_REQ_RETRY_COUNT)
    {
      if (packet == null)
        throw new ArgumentNullException("packet");
      if (address == null)
        throw new ArgumentNullException("address");
      var identifier = new PacketIdentifier(packet.Header.NameTrnId, address);

      // If there is a request pending with the same transaction Id and the same target IP address, we do not
      // send the request again, but simply return the Task representing the already pending request.
      var newTcs = new TaskCompletionSource<NbNsPacketBase>();
      var tcs = _pendingUnicastRequests.GetOrAdd(identifier, newTcs);
      if (tcs != newTcs)
        return await tcs.Task;

      for (var retryCount = 1; retryCount <= numRetries; retryCount++)
      {
        // If the send operation was not successful (most likely the given IP address does not exist in the local subnet)
        // we immediately return null.
        if (!await SendPacketAsync(packet, address))
        {
          _pendingUnicastRequests.TryRemove(identifier, out newTcs);
          return null;
        }

        // If we received a response within the given timeout, we return the response; if not, we resend the request
        if (await Task.WhenAny(tcs.Task, Task.Delay(timeOutMilliseconds)) == tcs.Task)
        {
          _pendingUnicastRequests.TryRemove(identifier, out newTcs);
          return await tcs.Task;
        }
      }

      _pendingUnicastRequests.TryRemove(identifier, out newTcs);
      return null;
    }

    /// <summary>
    /// Conveience method for sending a <see cref="NbNsNodeStatusRequest"/> and receiving a <see cref="NbNsNodeStatusResponse"/>
    /// </summary>
    /// <param name="packet"><see cref="NbNsNodeStatusRequest"/> to send</param>
    /// <param name="address">Target address to send the <see cref="NbNsNodeStatusRequest"/> to</param>
    /// <returns>
    /// <see cref="Task"/> that completes when either a <see cref="NbNsNodeStatusResponse"/> was received (which is then the
    /// result value of the task) or when - given the default timeout and retry values - no response has been received
    /// (in which case the Task's result value is null).
    /// </returns>
    public async Task<NbNsNodeStatusResponse> SendUnicastNodeStatusRequestAsync(NbNsNodeStatusRequest packet, IPAddress address)
    {
      var response = await SendUnicastRequestAsync(packet, address);
      return response as NbNsNodeStatusResponse;
    }

    #endregion

    #region IDisposable implementation

    /// <summary>
    /// Stops the _receiverTask and disposes the socket
    /// </summary>
    public void Dispose()
    {
      _endReceive.TrySetResult(null);
      _receiverTask.Wait();
      _socket.Dispose();
    }

    #endregion
  }
}
