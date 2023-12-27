
namespace Webradio.OnlineLibraries.Helper
{
  internal class Json
  {
    /// <summary>
    ///     Deserialize a json string as Object
    /// </summary>
    /// <typeparam name="T">Object Type</typeparam>
    /// <param name="json">Json String</param>
    /// <returns></returns>
    public static T Deserialize<T>(string json)
    {
      try
      {
        var settings = new Newtonsoft.Json.JsonSerializerSettings()
          { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, settings);
      }
      catch (System.Exception ex)
      {
        return (T)System.Activator.CreateInstance(typeof(T));
      }
    }
  }
}


