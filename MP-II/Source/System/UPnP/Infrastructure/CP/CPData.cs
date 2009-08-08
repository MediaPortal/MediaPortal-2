namespace UPnP.Infrastructure.CP
{
  /// <summary>
  /// Contains data shared throughout the control point system.
  /// </summary>
  public class CPData
  {
    protected object _syncObj = new object();
    protected uint _httpPort = 0;

    /// <summary>
    /// Synchronization object for the UPnP control point system.
    /// </summary>
    public object SyncObj
    {
      get { return _syncObj; }
    }

    /// <summary>
    /// HTTP listening port for used for event messages.
    /// </summary>
    public uint HttpPort
    {
      get { return _httpPort; }
      set { _httpPort = value; }
    }
  }
}
