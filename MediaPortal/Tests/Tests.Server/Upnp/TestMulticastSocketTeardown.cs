using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using UPnP.Infrastructure;
using UPnP.Infrastructure.Utils;

namespace Tests.Server.Upnp
{
  /// <summary>
  /// Tests for leaving multicast groups when a UPnP socket is closed.
  /// </summary>
  /// <remarks>
  /// The teardown used to name only the group when dropping membership, which asks the stack for a
  /// membership of <c>IPAddress.Any</c> that was never added. Windows answered that silently, Linux
  /// failed it for every socket the server closed, and the first Linux run of the server logged three
  /// warnings that looked like a multicast problem at startup but were teardown noise.
  /// </remarks>
  [TestFixture]
  public class TestMulticastSocketTeardown
  {
    ILogger _previousLogger;
    CapturingLogger _logger;

    [SetUp]
    public void SetUp()
    {
      _previousLogger = UPnPConfiguration.LOGGER;
      _logger = new CapturingLogger();
      UPnPConfiguration.LOGGER = _logger;
    }

    [TearDown]
    public void TearDown()
    {
      UPnPConfiguration.LOGGER = _previousLogger;
    }

    /// <summary>
    /// Opening the group and closing the socket afterwards must not produce any complaint.
    /// </summary>
    [TestCase("SSDP")]
    [TestCase("GENA")]
    public void ClosingAJoinedSocketReportsNothing(string kind)
    {
      IPAddress group = GroupOf(kind);
      Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
      if (!TryJoin(socket, group))
      {
        // No multicast route on this machine; the teardown still has to close the socket quietly.
        Assert.That(() => Dispose(kind, socket, true), Throws.Nothing);
        return;
      }

      Dispose(kind, socket, true);

      Assert.That(_logger.Warnings, Is.Empty,
          "leaving the group it joined must not be reported as a problem");
      AssertSocketIsClosed(socket);
    }

    /// <summary>
    /// The interface the group was joined on has to be the one named when leaving it. This is what the
    /// Linux stack refuses otherwise, so it is asserted by requiring the teardown to stay quiet.
    /// </summary>
    [Test]
    public void LeavingTheGroupNamesTheInterfaceThatJoinedIt()
    {
      Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
      if (!TryJoin(socket, UPnPConsts.SSDP_MULTICAST_ADDRESS_V4))
      {
        Assert.Pass("no multicast route on this machine");
      }

      // The group was joined with IPAddress.Loopback; a teardown naming any other interface - or none -
      // makes this call report a problem on at least one platform.
      NetworkHelper.DisposeSSDPMulticastSocket(socket, true);

      Assert.That(_logger.Warnings, Has.None.Contains("could not leave"));
    }

    /// <summary>
    /// A caller that knows the interface is gone asks for a close without leaving, and then nothing at
    /// all may be reported - a socket error there carries no information.
    /// </summary>
    [Test]
    public void ClosingWithoutLeavingTheGroupTouchesNoSocketOptions()
    {
      Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

      NetworkHelper.DisposeSSDPMulticastSocket(socket, false);

      Assert.That(_logger.Warnings, Is.Empty);
      AssertSocketIsClosed(socket);
    }

    /// <summary>
    /// If a teardown problem is reported, it has to say which protocol, which group and which interface
    /// it is about. The messages used to read "GENAServerController: Could not set IPv4 options" in five
    /// different places of three different operations.
    /// </summary>
    [Test]
    public void AReportedTeardownProblemNamesTheGroupAndTheInterface()
    {
      // A socket that was never joined: leaving its groups is expected to fail somewhere.
      Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

      Assert.That(() => NetworkHelper.DisposeSSDPMulticastSocket(socket, true), Throws.Nothing);

      foreach (string warning in _logger.Warnings)
      {
        Assert.That(warning, Does.Contain("SSDP"), "the message has to name the protocol");
        Assert.That(warning, Does.Contain(UPnPConsts.SSDP_MULTICAST_ADDRESS_V4.ToString()),
            "the message has to name the group");
        Assert.That(warning, Does.Contain(IPAddress.Loopback.ToString()),
            "the message has to name the interface");
      }
    }

    static void Dispose(string kind, Socket socket, bool exitMulticastGroup)
    {
      if (kind == "GENA")
        NetworkHelper.DisposeGENAMulticastSocket(socket, exitMulticastGroup);
      else
        NetworkHelper.DisposeSSDPMulticastSocket(socket, exitMulticastGroup);
    }

    static IPAddress GroupOf(string kind)
    {
      return kind == "GENA" ? UPnPConsts.GENA_MULTICAST_ADDRESS_V4 : UPnPConsts.SSDP_MULTICAST_ADDRESS_V4;
    }

    static bool TryJoin(Socket socket, IPAddress group)
    {
      try
      {
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
            new MulticastOption(group, IPAddress.Loopback));
        return true;
      }
      catch (SocketException)
      {
        return false;
      }
    }

    static void AssertSocketIsClosed(Socket socket)
    {
      Assert.That(() => socket.LocalEndPoint, Throws.TypeOf<ObjectDisposedException>());
    }

    class CapturingLogger : ILogger
    {
      public readonly List<string> Warnings = new List<string>();

      public void Debug(string format, params object[] args)
      { }

      public void Debug(string format, Exception ex, params object[] args)
      { }

      public void Info(string format, params object[] args)
      { }

      public void Info(string format, Exception ex, params object[] args)
      { }

      public void Warn(string format, params object[] args)
      {
        Warnings.Add(Format(format, args));
      }

      public void Warn(string format, Exception ex, params object[] args)
      {
        Warnings.Add(Format(format, args) + " | " + ex.GetType().Name + ": " + ex.Message);
      }

      public void Error(string format, params object[] args)
      {
        Warnings.Add("ERROR: " + Format(format, args));
      }

      public void Error(string format, Exception ex, params object[] args)
      {
        Warnings.Add("ERROR: " + Format(format, args));
      }

      public void Error(Exception ex)
      {
        Warnings.Add("ERROR: " + ex.Message);
      }

      public void Critical(string format, params object[] args)
      {
        Warnings.Add("CRITICAL: " + Format(format, args));
      }

      public void Critical(string format, Exception ex, params object[] args)
      {
        Warnings.Add("CRITICAL: " + Format(format, args));
      }

      public void Critical(Exception ex)
      {
        Warnings.Add("CRITICAL: " + ex.Message);
      }

      static string Format(string format, object[] args)
      {
        return args == null || args.Length == 0 ? format : string.Format(format, args);
      }
    }
  }
}
