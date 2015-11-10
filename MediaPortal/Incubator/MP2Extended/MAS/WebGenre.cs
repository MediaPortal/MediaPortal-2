namespace MediaPortal.Plugins.MP2Extended.MAS
{
  public class WebGenre : WebObject, ITitleSortable
  {
    public string Title { get; set; }

    public WebGenre()
    {
    }

    public WebGenre(string title)
    {
      Title = title;
    }

    public override string ToString()
    {
      return Title;
    }

    public override bool Equals(object obj)
    {
      WebGenre r = obj is string ? new WebGenre((string)obj) : obj as WebGenre;
      return (object)r != null && this.Title == r.Title;
    }

    public override int GetHashCode()
    {
      return Title.GetHashCode();
    }

    public static bool operator ==(WebGenre a, WebGenre b)
    {
      return ReferenceEquals(a, b) || (((object)a) != null && ((object)b) != null && a.Title == b.Title);
    }

    public static bool operator !=(WebGenre a, WebGenre b)
    {
      return !(a == b);
    }

    public static implicit operator WebGenre(string value)
    {
      return new WebGenre(value);
    }

    public static implicit operator string(WebGenre value)
    {
      return value.Title;
    }
  }
}