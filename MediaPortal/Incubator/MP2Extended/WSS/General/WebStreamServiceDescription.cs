namespace MediaPortal.Plugins.MP2Extended.WSS.General
{
  public class WebStreamServiceDescription
  {
    public bool SupportsMedia { get; set; }
    public bool SupportsRecordings { get; set; }
    public bool SupportsTV { get; set; }

    public int ApiVersion { get; set; }
    public string ServiceVersion { get; set; }
  }
}