using System;
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.CP
{
  public class UPnPException : Exception
  {
    public UPnPException() { }
    public UPnPException(string message, params object[] parameters) :
        base(string.Format(message, parameters)) { }
    public UPnPException(string message, Exception innerException, params object[] parameters) :
        base(string.Format(message, parameters), innerException) { }
  }

  public class UPnPRemoteException : UPnPException
  {
    protected UPnPError _error;

    public UPnPRemoteException(UPnPError error) :
        base(error.ErrorDescription)
    {
      _error = error;
    }

    public UPnPError Error
    {
      get { return _error; }
    }
  }
}