using System;

namespace UPnP.Infrastructure
{
  /// <summary>
  /// Thrown if a request of an usupported UPnP version should be handled.
  /// </summary>
  public class UnsupportedRequestException : ApplicationException
  {
    public UnsupportedRequestException(string msg, params object[] args) :
      base(string.Format(msg, args)) { }
    public UnsupportedRequestException(string msg, Exception ex, params object[] args) :
      base(string.Format(msg, args), ex) { }
  }

  /// <summary>
  /// Thrown if an action template is to be modified while it is already connected to a device's action.
  /// </summary>
  public class UPnPAlreadyConnectedException : ApplicationException
  {
    public UPnPAlreadyConnectedException(string msg, params object[] args) :
      base(string.Format(msg, args)) { }
    public UPnPAlreadyConnectedException(string msg, Exception ex, params object[] args) :
      base(string.Format(msg, args), ex) { }
  }
}
