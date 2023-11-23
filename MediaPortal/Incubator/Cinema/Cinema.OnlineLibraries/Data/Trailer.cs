namespace Cinema.OnlineLibraries.Data
{
  public class Trailer
  {
    public string Name { get; set; } = string.Empty;
    
    public string Url { get; set; } = string.Empty;

    public Trailer(string name, string url)
    {
      Name = name;
      Url = "https://youtu.be/" + url;
    }

    public Trailer()
    {

    }
  }
}
