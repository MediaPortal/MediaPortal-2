using Newtonsoft.Json;

namespace Webradio.Stations.Helper;

/// <summary>
///   Json Functions
/// </summary>
internal class Json
{
  /// <summary>
  ///   Deserialize a json string as Object
  /// </summary>
  /// <typeparam name="T">Object Type</typeparam>
  /// <param name="json">Json String</param>
  /// <returns></returns>
  public static T Deserialize<T>(string json)
  {
    //try
    //{
    //    using var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
    //    var settings = new System.Runtime.Serialization.Json.DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };
    //    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T), settings);
    //    return (T)serializer.ReadObject(ms);
    //}
    //catch (System.Exception ex)
    //{
    //    return (T)System.Activator.CreateInstance(typeof(T));
    //}
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
    //try
    //{
    //    using var ms = new System.IO.MemoryStream();
    //    var settings = new System.Runtime.Serialization.Json.DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };
    //    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T), settings);
    //    serializer.WriteObject(ms, jsonObject);

    //    ms.Position = 0;
    //    StreamReader sr = new StreamReader(ms);
    //    return sr.ReadToEnd();
    //}
    //catch (System.Exception ex)
    //{
    //    return "";
    //}
    try
    {
      //var settings = new Newtonsoft.Json.JsonSerializerSettings() { Formatting = Formatting.Indented};
      //return JsonConvert.SerializeObject(jsonObject, settings);
      return JsonConvert.SerializeObject(jsonObject);
    }
    catch (Exception ex)
    {
      return "";
    }
  }
}
