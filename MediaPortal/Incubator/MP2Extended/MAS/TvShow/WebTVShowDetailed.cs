namespace MediaPortal.Plugins.MP2Extended.MAS.TvShow
{
  public class WebTVShowDetailed : WebTVShowBasic
  {
    public string Summary { get; set; }
    public string Status { get; set; }
    public string Network { get; set; }
    public string AirsDay { get; set; }
    public string AirsTime { get; set; }
    public int Runtime { get; set; }
  }
}