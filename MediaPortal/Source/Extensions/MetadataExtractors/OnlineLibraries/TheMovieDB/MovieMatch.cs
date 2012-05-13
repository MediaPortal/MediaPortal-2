namespace MediaPortal.Extensions.OnlineLibraries
{
  public class MovieMatch
  {
    public string MovieName;
    public string MovieDBName;
    public int ID;
    public bool FanArtDownloaded;
    public override string ToString()
    {
      return string.Format("{0}: {1} [{2}]", MovieName, MovieDBName, ID);
    }
  }
}