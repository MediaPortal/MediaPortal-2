namespace UPnP.Infrastructure.Common
{
  /// <summary>
  /// Contains runtime data for an UPnP error which occured during the invocation of a UPnP action.
  /// </summary>
  public class UPnPError
  {
    protected int _errorCode;
    protected string _errorDescription;

    public UPnPError(int errorCode, string errorDescription)
    {
      _errorCode = errorCode;
      _errorDescription = errorDescription;
    }

    public int ErrorCode
    {
      get { return _errorCode; }
    }

    public string ErrorDescription
    {
      get { return _errorDescription; }
    }
  }

}
