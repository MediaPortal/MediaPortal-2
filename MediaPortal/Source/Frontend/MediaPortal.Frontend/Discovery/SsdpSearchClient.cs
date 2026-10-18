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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Frontend.Discovery
{
  /// <summary>
  /// Where to send an SSDP search and for how long to listen for the answers.
  /// </summary>
  /// <param name="SearchTarget">The device type to look for.</param>
  /// <param name="MxSeconds">The <c>MX</c> value: how long a device may wait before answering.</param>
  /// <param name="Window">How long the search listens in total. Has to outlast <paramref name="MxSeconds"/>,
  /// otherwise a device that answers at the latest allowed moment is filtered out by us.</param>
  /// <param name="Multicast">Send to the SSDP group. This is the normal way and the one that reaches other
  /// machines; a network or a container without multicast routing makes it fail, which is why
  /// <paramref name="UnicastTargets"/> exists.</param>
  /// <param name="UnicastTargets">Extra endpoints to ask directly, bypassing multicast. Used for the localhost
  /// and by the CI gate, where a search that only succeeds by luck on a shared LAN would prove nothing.</param>
  public sealed record SsdpSearchOptions(string SearchTarget = SsdpAnnouncement.ServerSearchTarget,
                                        int MxSeconds = 1,
                                        TimeSpan? Window = null,
                                        bool Multicast = true,
                                        IReadOnlyList<IPEndPoint>? UnicastTargets = null)
  {
  /// <summary>
  /// The endpoints to ask without going through the multicast group: this machine.
  /// </summary>
  /// <remarks>
  /// A media centre front end and its server often live on the same box, and a hotel network, a container or a
  /// VPN occasionally swallows multicast altogether. Asking localhost costs one datagram and cannot disturb
  /// anybody else, so a server on the machine in front of the television is found even when nothing else would be.
  /// </remarks>
  public static IReadOnlyList<IPEndPoint> ThisMachine => new[] { new IPEndPoint(IPAddress.Loopback, SsdpAnnouncement.SsdpPort) };

  /// <summary>The search a front end performs when it wants to know what is out there.</summary>
  public static SsdpSearchOptions ForNetworkSearch(TimeSpan? window = null)
  {
    return new SsdpSearchOptions(Window: window, UnicastTargets: ThisMachine);
  }

  /// <summary>The window the search listens for answers.</summary>
  public TimeSpan ListenWindow => Window ?? TimeSpan.FromSeconds(MxSeconds + 2);
  }

  /// <summary>
  /// What a search produced, including the reason it produced nothing.
  /// </summary>
  /// <param name="Announcements">The distinct devices that answered, in the order their addresses sort.</param>
  /// <param name="MessagesReceived">How many SSDP messages arrived, including repeats of the same device. Zero
  /// with an empty <paramref name="Announcements"/> means the network was silent.</param>
  /// <param name="SendFailures">One line per place the request could not be sent. A search that could not be
  /// sent is not a search that found nothing, and a screen has to be able to tell those two apart.</param>
  public sealed record SsdpSearchResult(IReadOnlyList<SsdpAnnouncement> Announcements,
                                        int MessagesReceived,
                                        IReadOnlyList<string> SendFailures)
  {
    /// <summary>Nothing arrived anywhere.</summary>
    public bool FoundNothing => Announcements.Count == 0;
  }

  /// <summary>
  /// Asks the network which MediaPortal 2 servers are there.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is a control point's search, and only that: one <c>M-SEARCH</c>, listen for the answers, stop. It does
  /// not keep a subscription to the SSDP group, so a server that starts while the front end is running appears
  /// the next time the user presses "Search again" rather than by itself. That is a deliberate limit - holding
  /// port 1900 open would collide with the operating system's own SSDP service on Windows - and the rescan
  /// button is the answer to it.
  /// </para>
  /// <para>
  /// Answers are collected for the whole window instead of returning the first one, because the point of the
  /// exercise is a list for the user to choose from: several servers in one house is the situation this exists
  /// for. Repeats of the same description URL are one server, not several - devices answer once per device type
  /// they publish.
  /// </para>
  /// </remarks>
  public sealed class SsdpSearchClient
  {
    private readonly int _port;

    public SsdpSearchClient(int port = SsdpAnnouncement.SsdpPort)
    {
      _port = port;
    }

    /// <summary>
    /// Runs one search. Never throws for network reasons: a front end that is being pointed at a server has to
    /// be able to report a missing network in words, and the caller gets that from
    /// <see cref="SsdpSearchResult.SendFailures"/>.
    /// </summary>
    public async Task<SsdpSearchResult> SearchAsync(SsdpSearchOptions options, CancellationToken cancellation)
    {
      if (options == null) throw new ArgumentNullException(nameof(options));

      var announcements = new Dictionary<string, SsdpAnnouncement>(StringComparer.OrdinalIgnoreCase);
      var failures = new List<string>();
      int messages = 0;
      byte[] buffer = new byte[4096];

      using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
      {
        try
        {
          // A server on the same machine answers our own multicast, so the loopback has to stay on - that is
          // what lets the CI gate and a single-computer setup find a server at all.
          socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
          socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
          socket.Bind(new IPEndPoint(IPAddress.Any, 0));
        }
        catch (SocketException e)
        {
          return new SsdpSearchResult(Array.Empty<SsdpAnnouncement>(), 0,
              new[] { "could not open the discovery socket: " + e.Message });
        }

        byte[] request = Encoding.UTF8.GetBytes(SsdpAnnouncement.BuildSearchRequest(options.SearchTarget, options.MxSeconds));
        var targets = new List<IPEndPoint>();
        if (options.Multicast) targets.Add(new IPEndPoint(SsdpAnnouncement.MulticastAddress, _port));
        if (options.UnicastTargets != null) targets.AddRange(options.UnicastTargets);
        if (targets.Count == 0)
        {
          return new SsdpSearchResult(Array.Empty<SsdpAnnouncement>(), 0, new[] { "nowhere to send the search to" });
        }

        foreach (IPEndPoint target in targets)
        {
          try
          {
            socket.SendTo(request, target);
          }
          catch (SocketException e)
          {
            failures.Add("no route to " + target + ": " + e.Message);
          }
          catch (Exception e)
          {
            failures.Add("sending to " + target + " failed: " + e.Message);
          }
        }

        DateTime deadline = DateTime.UtcNow + options.ListenWindow;
        while (DateTime.UtcNow < deadline && !cancellation.IsCancellationRequested)
        {
          TimeSpan remaining = deadline - DateTime.UtcNow;
          using (var window = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
          {
            window.CancelAfter(remaining);
            try
            {
              SocketReceiveFromResult received = await socket.ReceiveFromAsync(
                  new ArraySegment<byte>(buffer), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0), window.Token)
                  .ConfigureAwait(false);
              messages++;
              SsdpAnnouncement? announcement =
                  SsdpAnnouncement.TryParse(Encoding.UTF8.GetString(buffer, 0, received.ReceivedBytes),
                      received.RemoteEndPoint as IPEndPoint);
              if (announcement != null)
              {
                announcements[announcement.Location.AbsoluteUri] = announcement;
              }
            }
            catch (OperationCanceledException)
            {
              // The window closed, or the user pressed escape or Search again. Both end the search.
              break;
            }
            catch (SocketException e)
            {
              // A ICMP port-unreachable from an earlier send surfaces here on Windows. It ends the listening,
              // not the search: whatever answered so far already is in the list.
              failures.Add("listening stopped: " + e.Message);
              break;
            }
            catch (Exception e)
            {
              failures.Add("listening stopped: " + e.Message);
              break;
            }
          }
        }
      }

      cancellation.ThrowIfCancellationRequested();
      IReadOnlyList<SsdpAnnouncement> sorted = announcements.Values
          .OrderBy(a => a.Location.AbsoluteUri, StringComparer.OrdinalIgnoreCase)
          .ToList();
      return new SsdpSearchResult(sorted, messages, failures);
    }
  }
}
