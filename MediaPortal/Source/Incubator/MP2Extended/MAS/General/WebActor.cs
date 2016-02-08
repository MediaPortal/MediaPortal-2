namespace MediaPortal.Plugins.MP2Extended.MAS.General
{
  public class WebActor : WebObject, ITitleSortable
  {
    public string Title { get; set; }

    public WebActor()
    {
    }

    public WebActor(string name)
    {
      Title = name;
    }

    public override string ToString()
    {
      return Title;
    }

    public override bool Equals(object obj)
    {
      WebActor r = obj is string ? new WebActor((string)obj) : obj as WebActor;
      return (object)r != null && this.Title == r.Title;
    }

    public override int GetHashCode()
    {
      return Title.GetHashCode();
    }

    public static bool operator ==(WebActor a, WebActor b)
    {
      return ReferenceEquals(a, b) || (((object)a) != null && ((object)b) != null && a.Title == b.Title);
    }

    public static bool operator !=(WebActor a, WebActor b)
    {
      return !(a == b);
    }

    public static implicit operator WebActor(string value)
    {
      return new WebActor(value);
    }

    public static implicit operator string(WebActor value)
    {
      return value.Title;
    }
  }
}