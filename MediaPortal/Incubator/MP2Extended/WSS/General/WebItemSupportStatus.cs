namespace MediaPortal.Plugins.MP2Extended.WSS.General
{
  public class WebItemSupportStatus
  {
    public bool Supported { get; set; }
    public string Reason { get; set; }

    public WebItemSupportStatus()
    {
    }

    public WebItemSupportStatus(bool supported, string reason)
    {
      Supported = supported;
      Reason = reason;
    }
  }
}