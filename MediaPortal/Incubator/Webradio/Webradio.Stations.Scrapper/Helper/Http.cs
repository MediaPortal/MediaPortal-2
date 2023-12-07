using System.Net;

namespace Webradio.Stations.Helper;

internal class Http
{
  public static HttpStatusCode StatusCode { get; set; }

  public static async Task<string> Request(string url)
  {
    ServicePointManager.SecurityProtocol =
      SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
    StatusCode = HttpStatusCode.Created;
    string message;

    try
    {
      var client = new HttpClient();
      client.DefaultRequestHeaders.Add("Accept", "text/html, application/json");
      client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
      using var response = await client.GetAsync(url);

      StatusCode = response.StatusCode;
      message = await response.Content.ReadAsStringAsync();
    }
    catch (Exception)
    {
      return "";
    }

    return message;
  }

  public static async Task<HttpStatusCode> Check(string url)
  {
    ServicePointManager.SecurityProtocol =
      SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

    try
    {
      var client = new HttpClient();
      using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
      return response.StatusCode;
    }
    catch (Exception)
    {
      return HttpStatusCode.RequestTimeout;
    }
  }

  public static async Task<string> GetSiteJson(string url)
  {
    var repeat = false;
    var ret = await Request(url);
    if (StatusCode != HttpStatusCode.OK && repeat == false)
    {
      repeat = true;
      ret = await Request(url);
    }

    if (ret == "")
    {
      Console.WriteLine("Cant read " + url);
      return "";
    }

    return ret.Substring("type=\"application/json\">", "<");
  }
}
