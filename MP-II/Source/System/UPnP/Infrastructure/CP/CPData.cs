using UPnP.Infrastructure.CP.SSDP;

namespace UPnP.Infrastructure.CP
{
  /// <summary>
  /// Contains data shared throughout the control point system.
  /// </summary>
  public class CPData
  {
    protected object _syncObj = new object();
    protected uint _httpPort = 0;
    protected SSDPClientController _ssdpClientController = null;

    /// <summary>
    /// Synchronization object for the UPnP control point system.
    /// </summary>
    public object SyncObj
    {
      get { return _syncObj; }
    }

    /// <summary>
    /// Gets or sets the HTTP listening port for used for event messages.
    /// </summary>
    public uint HttpPort
    {
      get { return _httpPort; }
      internal set { _httpPort = value; }
    }

    /// <summary>
    /// Gets or sets the SSDP controller of the UPnP client.
    /// </summary>
    public SSDPClientController SSDPController
    {
      get { return _ssdpClientController; }
      internal set { _ssdpClientController = value; }
    }
  }
}
