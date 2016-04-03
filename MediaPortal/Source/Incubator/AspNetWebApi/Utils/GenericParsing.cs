namespace MediaPortal.Plugins.AspNetWebApi.Utils
{
  public static partial class GenericParsing
  {
    static GenericParsing()
    {
      SetTryParseMethod<string>(TryParseString);
      //SetTryParseMethod<Guid>(TryParseGuid);
    }
  }
}
