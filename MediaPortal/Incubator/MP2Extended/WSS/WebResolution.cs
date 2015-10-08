namespace MediaPortal.Plugins.MP2Extended.WSS
{
  public class WebResolution
  {
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString()
    {
      return Width + "x" + Height;
    }
  }
}