/*
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

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Discovery;

namespace Tests.Frontend
{
  /// <summary>
  /// A UPnP device on the loopback interface, for the discovery tests to find.
  /// </summary>
  /// <remarks>
  /// <para>
  /// It answers a search request the way a device does - a <c>200 OK</c> back to whoever asked, naming the
  /// description URL it wants to be known by - which is what lets the tests send a real datagram over a real
  /// socket and read a real answer back. Mocking the socket would have left the only interesting part of discovery
  /// (does the request go out, does the answer get read) untested.
  /// </para>
  /// <para>
  /// It listens on 127.0.0.1 on a port of its own rather than on 239.255.255.250:1900, so that several tests can
  /// run at once and so that whatever else is on the developer's network - a printer, a NAS, Windows' own SSDP
  /// service - cannot answer the search and turn a passing test into a coincidence.
  /// </para>
  /// </remarks>
  internal sealed class FakeUpnpServer : IDisposable
  {
    private readonly UdpClient _socket;
    private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
    private readonly Task _listener;
    private readonly string _location;
    private readonly bool _answersSearches;
    private bool _disposed;
    private int _requests;

    private FakeUpnpServer(UdpClient socket, int port, string location, bool answersSearches)
    {
      _socket = socket;
      Port = port;
      _location = location;
      _answersSearches = answersSearches;
      _listener = Task.Run(Listen);
    }

    /// <summary>The loopback port this device answers on.</summary>
    public int Port { get; }

    /// <summary>The description URL it announces.</summary>
    public string Location => _location;

    /// <summary>How many search requests it has seen. A search that reached nothing is not a search that found nothing.</summary>
    public int Requests => Volatile.Read(ref _requests);

    /// <summary>The last request in full, so that a failing test can be read rather than guessed.</summary>
    public string? LastRequest { get; private set; }

    /// <param name="location">The description URL to announce.</param>
    /// <param name="answersSearches">
    /// <c>false</c> for a device that is there but stays quiet, which is the state of a network with no MediaPortal
    /// server on it. It has to be a real listener on the port: sending to a port nobody holds produces an ICMP
    /// destination-unreachable, which is a different situation and tested separately.
    /// </param>
    public static FakeUpnpServer Announcing(string location, bool answersSearches = true)
    {
      var socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
      int port = ((IPEndPoint)socket.Client.LocalEndPoint!).Port;
      return new FakeUpnpServer(socket, port, location, answersSearches);
    }

    /// <summary>The endpoint to point a search at.</summary>
    public IPEndPoint Endpoint => new IPEndPoint(IPAddress.Loopback, Port);

    private async Task Listen()
    {
      while (!_cancellation.IsCancellationRequested)
      {
        try
        {
          UdpReceiveResult received = await _socket.ReceiveAsync(_cancellation.Token);
          string text = Encoding.UTF8.GetString(received.Buffer);
          Interlocked.Increment(ref _requests);
          LastRequest = text;

          if (!_answersSearches || !text.StartsWith("M-SEARCH", StringComparison.Ordinal)) continue;

          string searchTarget = Header(text, "ST") ?? SsdpAnnouncement.AllSearchTarget;
          if (!searchTarget.Equals(SsdpAnnouncement.AllSearchTarget, StringComparison.OrdinalIgnoreCase) &&
              !searchTarget.Equals(SsdpAnnouncement.ServerSearchTarget, StringComparison.OrdinalIgnoreCase))
          {
            continue; // A device does not answer for a type it does not publish.
          }

          byte[] response = Encoding.UTF8.GetBytes(Response());
          await _socket.SendAsync(response, received.RemoteEndPoint, _cancellation.Token);
        }
        catch (OperationCanceledException)
        {
          break;
        }
        catch (ObjectDisposedException)
        {
          break;
        }
        catch (SocketException)
        {
          break;
        }
      }
    }

    private string Response()
    {
      return string.Join("\r\n", new[]
      {
          "HTTP/1.1 200 OK",
          "CACHE-CONTROL: max-age=1800",
          "ST: " + SsdpAnnouncement.ServerSearchTarget,
          "USN: uuid:fa8a9a0b-1f2e-3a4b-5c6d-7e8f9a0b1c2d::" + SsdpAnnouncement.ServerSearchTarget,
          "EXT:",
          "SERVER: Windows_NT/10.0.22631 UPnP/1.0 Mediaportal.MP2.UPnP/2.0.0",
          "LOCATION: " + _location,
          "BOOTID.UPNP.ORG: 1712345678",
          "CONFIGID.UPNP.ORG: 1",
          "CONNECTION: close",
          string.Empty,
          string.Empty
      });
    }

    private static string? Header(string message, string name)
    {
      foreach (string line in message.Replace("\r\n", "\n").Split('\n'))
      {
        int colon = line.IndexOf(':');
        if (colon <= 0) continue;
        if (line.Substring(0, colon).Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
          return line.Substring(colon + 1).Trim();
      }
      return null;
    }

    public void Dispose()
    {
      // Idempotent on purpose. A test that used a <c>using</c> block and a fixture that sweeps up after itself are
      // both reasonable ways to end the device, and the second one to arrive must find nothing left to do rather
      // than throw - a TearDown that throws takes the results of every test in the fixture down with it.
      if (_disposed) return;
      _disposed = true;

      try
      {
        _cancellation.Cancel();
      }
      catch (ObjectDisposedException)
      {
        // Already cancelled and disposed by the first caller.
      }

      try
      {
        _socket.Dispose();
      }
      catch (ObjectDisposedException)
      {
        // Already gone, which is what we wanted.
      }

      try
      {
        _listener.Wait(TimeSpan.FromSeconds(2));
      }
      catch (AggregateException)
      {
        // The listener ended because the socket went away under it. Expected on the way out.
      }

      _cancellation.Dispose();
    }
  }
}
