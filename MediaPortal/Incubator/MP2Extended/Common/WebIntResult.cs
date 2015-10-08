using System;

namespace MediaPortal.Plugins.MP2Extended.Common
{
  public class WebIntResult
  {
    public int Result { get; set; }

    public WebIntResult()
    {
    }

    public WebIntResult(int value)
    {
      Result = value;
    }

    public override string ToString()
    {
      return Result.ToString();
    }

    public override bool Equals(object obj)
    {
      WebIntResult r = obj is string ? new WebIntResult((int)obj) : obj as WebIntResult;
      return (object)r != null && this.Result == r.Result;
    }

    public override int GetHashCode()
    {
      return Result.GetHashCode();
    }

    public static bool operator ==(WebIntResult a, WebIntResult b)
    {
      return ReferenceEquals(a, b) || (((object)a) != null && ((object)b) != null && a.Result == b.Result);
    }

    public static bool operator !=(WebIntResult a, WebIntResult b)
    {
      return !(a == b);
    }

    public static implicit operator WebIntResult(int value)
    {
      return new WebIntResult(value);
    }

    public static implicit operator int(WebIntResult value)
    {
      return value.Result;
    }
  }
}