using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.PluginManager.Packages.ApiEndpoints;
using MediaPortal.Common.Services.Settings;
using MediaPortal.UiComponents.PackageManager.Settings;
using Newtonsoft.Json;

namespace MediaPortal.UiComponents.PackageManager.API
{
  internal class RequestExecutionHelper : IDisposable
  {
    private SettingsChangeWatcher<PackageManagerClientSettings> _settings = new SettingsChangeWatcher<PackageManagerClientSettings>();
    private Uri _packageServerUri;
    private string _credentials;

    public RequestExecutionHelper()
    {
      _settings.SettingsChanged += SettingsChanged;
      UpdateSettings();
    }

    private void SettingsChanged(object sender, EventArgs eventArgs)
    {
      UpdateSettings();
    }

    private void UpdateSettings()
    {
      _packageServerUri = new Uri(_settings.Settings.PackageRepository);
      var hasCredentials = !string.IsNullOrWhiteSpace(_settings.Settings.UserName) && !string.IsNullOrWhiteSpace(_settings.Settings.Password);
      _credentials = hasCredentials ? Convert.ToBase64String(Encoding.ASCII.GetBytes(_settings.Settings.UserName + ":" + _settings.Settings.Password)) : null;
    }

    // TODO use multi-part for file uploads
    // see http://stackoverflow.com/questions/16906711/httpclient-how-to-upload-multiple-files-at-once
    // and http://stackoverflow.com/questions/15638622/how-to-upload-files-to-asp-net-mvc-4-0-action-running-in-iis-express-with-httpcl/15638623#15638623

    public async Task<HttpResponseMessage> ExecuteRequestAsync(HttpMethod method, string path, object model = null)
    {
      using (var client = new HttpClient())
      {
        var request = new HttpRequestMessage(method, new Uri(_packageServerUri, path));
        if (_credentials != null)
        {
          request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _credentials);
        }
        if (model != null)
        {
          string json = JsonConvert.SerializeObject(model, Formatting.None);
          request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
          request.Content.Headers.Add("Content-Type", "application/json");
        }
        return await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
      }
    }

    public HttpResponseMessage ExecuteRequest(HttpMethod method, string path, object model = null)
    {
      return ExecuteRequestAsync(method, path, model).Result;
    }

    public HttpResponseMessage ExecuteRequest(ApiEndpoint apiEndpoint, object model, object urlParameterValues = null)
    {
      return ExecuteRequestAsync(apiEndpoint.HttpMethod, apiEndpoint.GetUrl(urlParameterValues), model).Result;
    }

    public async Task<T> GetResponseContentAsync<T>(HttpResponseMessage response)
    {
      return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
    }

    public T GetResponseContent<T>(HttpResponseMessage response)
    {
      return GetResponseContentAsync<T>(response).Result;
    }

    public void Dispose()
    {
      if (_settings != null)
      {
        _settings.SettingsChanged -= SettingsChanged;
        _settings.Dispose();
      }
      _settings = null;
    }
  }
}
