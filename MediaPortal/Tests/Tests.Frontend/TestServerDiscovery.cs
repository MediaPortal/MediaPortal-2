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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using MediaPortal.Common.UPnP;
using MediaPortal.Frontend.Discovery;

namespace Tests.Frontend
{
  /// <summary>
  /// Finding a server without being told where it is.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Three layers, and each is tested at the layer it lives on. What a message means is tested by reading real
  /// message text. What a search does is tested by sending a real datagram to a fake device on the loopback
  /// interface and reading its real answer back - mocking the socket would leave the only part worth testing, the
  /// part that touches the network, untested. And what a server is is tested against an API that is really
  /// listening in this process, because the claim the list makes is "this machine answered as a MediaPortal
  /// server", which nothing short of answering can prove.
  /// </para>
  /// <para>
  /// The searches here never touch the multicast group: they ask one port on 127.0.0.1, so that a NAS or a printer
  /// on the developer's network cannot answer and make a test pass for the wrong reason, and so that the tests can
  /// run in parallel and inside a container. That the same code finds a server through the group as well is what
  /// the SSDP gate is for, where a real server is started and asked.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestServerDiscovery
  {
    private readonly List<IDisposable> _cleanup = new List<IDisposable>();

    [TearDown]
    public void TearDown()
    {
      // Whatever a test left behind is closed before anything is complained about, and a thing that will not close
      // is written down rather than thrown: a TearDown that throws takes the results of the tests that ran before
      // it down with it, which is how one leak turns into a whole red fixture.
      var leftovers = new List<string>();
      foreach (IDisposable thing in _cleanup)
      {
        try
        {
          thing.Dispose();
        }
        catch (Exception e)
        {
          leftovers.Add(e.GetType().Name + ": " + e.Message);
        }
      }
      _cleanup.Clear();
      InProcessApi.RemoveMediaLibrary();

      if (leftovers.Count > 0) Assert.Fail(string.Join("; ", leftovers.ToArray()));
    }

    private T Keep<T>(T thing) where T : IDisposable
    {
      _cleanup.Add(thing);
      return thing;
    }

    private static string SearchAnswer(string location, string searchTarget)
    {
      return string.Join("\r\n", new[]
      {
          "HTTP/1.1 200 OK",
          "CACHE-CONTROL: max-age=1800",
          "ST: " + searchTarget,
          "USN: uuid:fa8a9a0b-1f2e-3a4b-5c6d-7e8f9a0b1c2d::" + searchTarget,
          "EXT:",
          "SERVER: Windows_NT/10.0.22631 UPnP/1.0 Mediaportal.MP2.UPnP/2.0.0",
          "LOCATION: " + location,
          "BOOTID.UPNP.ORG: 1712345678",
          "CONFIGID.UPNP.ORG: 1",
          "CONNECTION: close",
          string.Empty,
          string.Empty
      });
    }

    private static SsdpAnnouncement ReadAnswer(string location, string searchTarget)
    {
      return SsdpAnnouncement.TryParse(SearchAnswer(location, searchTarget), null)
          ?? throw new AssertionException("the answer to a search was not read as an announcement");
    }

    /// <summary>
    /// A MediaPortal API listening on this machine, and the announcement that machine would send about itself.
    /// </summary>
    private InProcessApi AServerOnThisMachine(out SsdpAnnouncement announcement)
    {
      List<IEnumerable<Guid>> ignored;
      InProcessApi.RegisterMediaLibrary(
          InProcessApi.CreateLibrary(InProcessApi.CreateAspects(), 3, out ignored).Object);
      InProcessApi api = Keep(InProcessApi.Start());
      announcement = ReadAnswer(
          "http://" + api.BaseUrl.Host + ":" + api.BaseUrl.Port + "/MediaPortal/UPnPServer_1/description.xml",
          SsdpAnnouncement.ServerSearchTarget);
      return api;
    }

    private static SsdpSearchOptions AskingOnlyThisPort(int port)
    {
      return new SsdpSearchOptions(
          SsdpAnnouncement.ServerSearchTarget, 1, TimeSpan.FromMilliseconds(400), false,
          new[] { new IPEndPoint(IPAddress.Loopback, port) });
    }

    [Test]
    public void TheAnswerToASearchNamesTheMachineAndTheTypeItAnnounces()
    {
      SsdpAnnouncement announcement = ReadAnswer(
          "http://192.168.1.20:55555/MediaPortal/UPnPServer_1/description.xml", SsdpAnnouncement.ServerSearchTarget);

      Assert.That(announcement.Location.Port, Is.EqualTo(55555));
      Assert.That(announcement.Location.Host, Is.EqualTo("192.168.1.20"));
      Assert.That(announcement.SearchTarget, Is.EqualTo(SsdpAnnouncement.ServerSearchTarget));
      Assert.That(announcement.Server, Does.Contain("MP2"));
      Assert.That(announcement.From, Is.Null, "a message read from text does not know where it came from, and says so");
    }

    [Test]
    public void AnAnnouncementSentOnItsOwnIsReadTheSameWay()
    {
      const string notify = "NOTIFY * HTTP/1.1\r\n" +
                            "HOST: 239.255.255.250:1900\r\n" +
                            "CACHE-CONTROL: max-age=1800\r\n" +
                            "LOCATION: http://192.168.1.31:55555/MediaPortal/UPnPServer_1/description.xml\r\n" +
                            "NT: " + SsdpAnnouncement.ServerSearchTarget + "\r\n" +
                            "NTS: ssdp:alive\r\n" +
                            "\r\n";

      SsdpAnnouncement? announcement = SsdpAnnouncement.TryParse(notify, null);

      // A device tells the network about itself when it starts up. Treating that as a search answer is not a
      // stretch - it is the same information, arriving without being asked for.
      Assert.That(announcement, Is.Not.Null);
      Assert.That(announcement!.Location.Host, Is.EqualTo("192.168.1.31"));
    }

    [Test]
    public void ASearchRequestFromSomebodyElseIsNotAnAnswer()
    {
      // A control point's request carries the same kind of lines; the first line is what tells them apart. Without
      // this rule a network of searching clients would list each other.
      const string someoneElsesRequest = "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\n" +
                                         "MAN: \"ssdp:discover\"\r\nMX: 1\r\nST: ssdp:all\r\n" +
                                         "LOCATION: http://192.168.1.9:55555/MediaPortal/UPnPServer_1/description.xml\r\n\r\n";

      Assert.That(SsdpAnnouncement.TryParse(someoneElsesRequest, null), Is.Null);
    }

    [Test]
    public void AnAnswerThatDoesNotSayWhereItLivesIsDropped()
    {
      const string noLocation = "HTTP/1.1 200 OK\r\nCACHE-CONTROL: max-age=1800\r\nST: ssdp:all\r\n\r\n";
      const string notAnHttpUrl = "HTTP/1.1 200 OK\r\nLOCATION: /MediaPortal/UPnPServer_1/description.xml\r\n\r\n";
      const string somebodyElsesScheme = "HTTP/1.1 200 OK\r\nLOCATION: rtsp://192.168.1.5/stream\r\n\r\n";

      Assert.That(SsdpAnnouncement.TryParse(noLocation, null), Is.Null);
      Assert.That(SsdpAnnouncement.TryParse(notAnHttpUrl, null), Is.Null);
      Assert.That(SsdpAnnouncement.TryParse(somebodyElsesScheme, null), Is.Null,
          "a machine we cannot ask over http is not a server we can list");
    }

    [Test]
    public void AServerIsRecognisedByTheDeviceTypeItAnnounces()
    {
      Assert.That(ReadAnswer("http://192.168.1.20:55555/MediaPortal/UPnPServer_1/description.xml",
          SsdpAnnouncement.ServerSearchTarget).IsMediaPortalServer, Is.True);
    }

    [Test]
    public void AServerIsAlsoRecognisedByThePathItServesItsDescriptionsFrom()
    {
      // Answering a search for "everything", a device names the type it thinks the asker cares about - which for a
      // root device announcement is not the MP2 type. The description path is MediaPortal's own and identifies it.
      SsdpAnnouncement announcement = ReadAnswer(
          "http://192.168.1.20:55555/MediaPortal/UPnPServer_1/description.xml", "upnp:rootdevice");

      Assert.That(announcement.IsMediaPortalServer, Is.True);
    }

    [Test]
    public void SomethingElseOnTheNetworkIsNotAServer()
    {
      SsdpAnnouncement printer = ReadAnswer("http://192.168.1.7:80/desc.xml", "urn:schemas-upnp-org:device:Printer:1");

      Assert.That(printer.IsMediaPortalServer, Is.False);
    }

    [Test]
    public void TheSearchTargetIsTheDeviceTypeTheServerPublishes()
    {
      // Written out in the front end because it may not reference the server's assemblies; this is where the two
      // are checked against each other. If the server renames its device type, the front end stops finding it, and
      // this is the test that says so.
      string expected = "urn:" + UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE + ":" +
                        UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION;

      Assert.That(SsdpAnnouncement.ServerSearchTarget, Is.EqualTo(expected));
    }

    [Test]
    public void TheSearchRequestSaysWhatSsdpRequires()
    {
      string request = SsdpAnnouncement.BuildSearchRequest(SsdpAnnouncement.ServerSearchTarget, 1);
      string[] lines = request.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

      Assert.That(lines[0], Is.EqualTo("M-SEARCH * HTTP/1.1"));
      Assert.That(lines[1], Is.EqualTo("HOST: 239.255.255.250:1900"));
      Assert.That(lines[2], Is.EqualTo("MAN: \"ssdp:discover\""),
          "without the extension line a device is required to ignore the request");
      Assert.That(lines[3], Is.EqualTo("MX: 1"));
      Assert.That(lines[4], Is.EqualTo("ST: " + SsdpAnnouncement.ServerSearchTarget));
      Assert.That(request, Does.EndWith("\r\n\r\n"), "the request has to end the headers");
    }

    [Test]
    public void TheApiAddressComesFromTheHostAndPortOfTheAnnouncement()
    {
      Uri baseurl = ServerLocator.BuildApiBaseUrl(
          new Uri("http://192.168.1.20:55555/MediaPortal/UPnPServer_1/description.xml"));

      Assert.That(baseurl.ToString(), Is.EqualTo("http://192.168.1.20:55555/MediaPortal/Server/"));
    }

    [Test]
    public void AnOrdinaryPortIsLeftOutOfTheAddress()
    {
      Uri baseurl = ServerLocator.BuildApiBaseUrl(new Uri("http://192.168.1.20/description.xml"));

      Assert.That(baseurl.ToString(), Is.EqualTo("http://192.168.1.20/MediaPortal/Server/"),
          "writing an explicit :80 works, but it is not what anybody types or expects to see");
    }

    [Test]
    public void AServerThatSaysItLivesSomewhereElseIsAskedThere()
    {
      Uri? corrected = ServerLocator.CorrectedBaseUrl(
          new Uri("http://192.168.1.20:55555/MediaPortal/Server/"), "/MP2/Api");

      Assert.That(corrected, Is.Not.Null);
      Assert.That(corrected!.ToString(), Is.EqualTo("http://192.168.1.20:55555/MP2/Api/"),
          "the health reply names the prefix it answered on, which beats guessing it");
    }

    [Test]
    public void AServerThatLivesWhereWeGuessedNeedsNoSecondTry()
    {
      Assert.That(ServerLocator.CorrectedBaseUrl(
          new Uri("http://192.168.1.20:55555/MediaPortal/Server/"), "/MediaPortal/Server"), Is.Null);
      Assert.That(ServerLocator.CorrectedBaseUrl(
          new Uri("http://192.168.1.20:55555/MediaPortal/Server/"), "/MediaPortal/Server/"), Is.Null,
          "a trailing slash in the report is not a different prefix");
      Assert.That(ServerLocator.CorrectedBaseUrl(
          new Uri("http://192.168.1.20:55555/MediaPortal/Server/"), null), Is.Null);
    }

    [Test]
    public async Task AServerThatAnswersItsApiIsListedWithWhatItSaid()
    {
      SsdpAnnouncement announcement;
      AServerOnThisMachine(out announcement);

      DetectedServer? server = await new ServerLocator().ProbeAsync(announcement.Location, CancellationToken.None);

      Assert.That(server, Is.Not.Null, "the machine answered, so it belongs in the list");
      Assert.That(server!.Name, Is.EqualTo("MediaPortal 2 Server"));
      Assert.That(server.Version, Is.Not.Empty);
      Assert.That(server.BaseUrl.ToString(), Is.EqualTo("http://127.0.0.1:" + announcement.Location.Port + "/MediaPortal/Server/"),
          "the address that gets saved is the one that just answered");
      Assert.That(server.AddressLine, Is.EqualTo("http://127.0.0.1:" + announcement.Location.Port));
    }

    [Test]
    public async Task AMachineThatAnswersNothingIsNotListed()
    {
      // Port 1 on this machine: nothing listens, so the request fails outright. This is the ordinary case on a
      // network - a TV or a printer answered the search, and its 55555 has no MediaPortal behind it.
      SsdpAnnouncement announcement = ReadAnswer("http://127.0.0.1:1/MediaPortal/UPnPServer_1/description.xml",
          SsdpAnnouncement.ServerSearchTarget);

      DetectedServer? server = await new ServerLocator(TimeSpan.FromMilliseconds(500))
          .ProbeAsync(announcement.Location, CancellationToken.None);

      Assert.That(server, Is.Null);
    }

    [Test]
    public async Task TwoAnnouncementsFromOneMachineAreOneEntry()
    {
      SsdpAnnouncement announcement;
      AServerOnThisMachine(out announcement);
      SsdpAnnouncement sameMachineAgain = ReadAnswer(announcement.Location.AbsoluteUri, "upnp:rootdevice");
      SsdpAnnouncement printer = ReadAnswer("http://127.0.0.1:1/desc.xml", SsdpAnnouncement.ServerSearchTarget);

      IReadOnlyList<DetectedServer> servers =
          await new ServerLocator(TimeSpan.FromMilliseconds(500))
              .LocateAsync(new[] { announcement, sameMachineAgain, printer }, CancellationToken.None);

      Assert.That(servers.Count, Is.EqualTo(1),
          "a device answers once per type it publishes, and the dead one in the middle is nobody");
      Assert.That(servers[0].Name, Is.EqualTo("MediaPortal 2 Server"));
    }

    [Test]
    public async Task ASearchOfARealSocketFindsTheDeviceThatAnswers()
    {
      SsdpAnnouncement announcement;
      AServerOnThisMachine(out announcement);
      using (FakeUpnpServer device = Keep(FakeUpnpServer.Announcing(announcement.Location.AbsoluteUri)))
      {
        SsdpSearchResult result = await new SsdpSearchClient(device.Port)
            .SearchAsync(AskingOnlyThisPort(device.Port), CancellationToken.None);

        Assert.That(result.FoundNothing, Is.False, "the device answered, so it has to be in the result");
        Assert.That(result.MessagesReceived, Is.EqualTo(1));
        Assert.That(result.Announcements[0].Location.AbsoluteUri, Is.EqualTo(announcement.Location.AbsoluteUri));
        Assert.That(result.Announcements[0].IsMediaPortalServer, Is.True);
        Assert.That(device.LastRequest, Does.Contain("ST: " + SsdpAnnouncement.ServerSearchTarget),
          "and what it received was the request we meant to send");
      }
    }

    [Test]
    public async Task AServerFoundByASearchIsListedWithWhatItAnswered()
    {
      // The whole path in one test: datagram out, announcement back, the announced machine asked over HTTP, and one
      // usable server standing at the end. This is what the screen shows a viewer.
      SsdpAnnouncement announcement;
      AServerOnThisMachine(out announcement);
      using (FakeUpnpServer device = Keep(FakeUpnpServer.Announcing(announcement.Location.AbsoluteUri)))
      {
        var searcher = new SsdpSearchClient(device.Port);
        var locator = new ServerLocator();

        SsdpSearchResult result = await searcher.SearchAsync(AskingOnlyThisPort(device.Port), CancellationToken.None);
        IReadOnlyList<DetectedServer> servers = await locator.LocateAsync(result.Announcements, CancellationToken.None);

        Assert.That(servers.Count, Is.EqualTo(1));
        Assert.That(servers[0].BaseUrl.ToString(),
            Is.EqualTo("http://127.0.0.1:" + announcement.Location.Port + "/MediaPortal/Server/"));
      }
    }

    [Test]
    public async Task ANetworkWithNoMediaPortalServerSaysSoRatherThanLookingBroken()
    {
      // Something is listening and stays quiet, which is the state of a network with printers on it. The request
      // provably went out (the device counted it), so "nothing answered" is a statement about the server, not about
      // our socket.
      using (FakeUpnpServer silent = Keep(FakeUpnpServer.Announcing("http://127.0.0.1:1/desc.xml", false)))
      {
        SsdpSearchResult result = await new SsdpSearchClient(silent.Port)
            .SearchAsync(AskingOnlyThisPort(silent.Port), CancellationToken.None);

        Assert.That(silent.Requests, Is.GreaterThan(0), "the search reached the network");
        Assert.That(result.FoundNothing, Is.True);
        Assert.That(result.MessagesReceived, Is.EqualTo(0));
        Assert.That(result.SendFailures, Is.Empty);
      }
    }

    [Test]
    public async Task APortNobodyHoldsIsReportedAndNotThrown()
    {
      // A free port on the loopback interface. Whether the operating system reports the refusal back to us or not is
      // its business; the front end's business is to come back with an empty list instead of an exception, because
      // a screen cannot show an exception to somebody holding a remote.
      int freePort;
      using (var probe = new System.Net.Sockets.UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
      {
        freePort = ((IPEndPoint)probe.Client.LocalEndPoint!).Port + 1;
      }

      SsdpSearchResult result = await new SsdpSearchClient(freePort)
          .SearchAsync(AskingOnlyThisPort(freePort), CancellationToken.None);

      Assert.That(result.FoundNothing, Is.True);
    }

    [Test]
    public void AnEntryIsTheSameEntryWhicheverWayItsAddressIsWritten()
    {
      var withSlash = new DetectedServer("Salon", "2.0", new Uri("http://192.168.1.20:55555/MediaPortal/Server/"),
          new Uri("http://192.168.1.20:55555/desc.xml"));
      var withoutSlash = new DetectedServer("Salon", "2.0", new Uri("http://192.168.1.20:55555/MediaPortal/Server"),
          new Uri("http://192.168.1.20:55555/desc.xml"));
      var otherMachine = new DetectedServer("Salon", "2.0", new Uri("http://192.168.1.21:55555/MediaPortal/Server"),
          new Uri("http://192.168.1.21:55555/desc.xml"));

      Assert.That(withSlash.Equals(withoutSlash), Is.True);
      Assert.That(withSlash.GetHashCode(), Is.EqualTo(withoutSlash.GetHashCode()));
      Assert.That(withSlash.Equals(otherMachine), Is.False);
    }

    [Test]
    public void AnEntryShowsWhatATrueViewerNeedsToSee()
    {
      var server = new DetectedServer("Salon", "2.0.0.1", new Uri("http://192.168.1.20:55555/MediaPortal/Server/"),
          new Uri("http://192.168.1.20:55555/desc.xml"), "Linux");

      Assert.That(server.AddressLine, Is.EqualTo("http://192.168.1.20:55555"));
      Assert.That(server.DetailLine, Does.Contain("192.168.1.20"));
      Assert.That(server.DetailLine, Does.Contain("2.0.0.1"));
      Assert.That(server.DetailLine, Does.Contain("Linux"), "two servers are told apart by what runs them");
    }
  }
}
