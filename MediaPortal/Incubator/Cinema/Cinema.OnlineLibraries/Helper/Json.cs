using System;
using Newtonsoft.Json;

namespace Cinema.OnlineLibraries.Helper
{
  /// <summary>
  ///   Json Functions
  /// </summary>
  public class Json
  {
    public static T Deserialize<T>(string json)
    {
      try
      {
        var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        return JsonConvert.DeserializeObject<T>(json, settings);
      }
      catch (Exception ex)
      {
        return (T)Activator.CreateInstance(typeof(T));
      }
    }

    public static string Serialize<T>(T jsonObject)
    {
      try
      {
        return JsonConvert.SerializeObject(jsonObject);
      }
      catch (Exception ex)
      {
        return "";
      }
    }
  }
}
