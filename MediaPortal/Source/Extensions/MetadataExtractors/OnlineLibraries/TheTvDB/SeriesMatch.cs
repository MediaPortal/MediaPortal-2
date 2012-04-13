namespace MediaPortal.Extensions.OnlineLibraries
{
  public class SeriesMatch
  {
    public string SeriesName;
    public string TvDBName;
    public int TvDBID;
    public override string ToString()
    {
      return string.Format("{0}: {1} [{2}]", SeriesName, TvDBName, TvDBID);
    }
  }
}