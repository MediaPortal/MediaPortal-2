#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.PluginManager.Packages.ApiEndpoints;
using Newtonsoft.Json;

namespace MediaPortal.PackageCore
{
  internal class RequestExecutionHelper
  {
    private readonly Uri _packageServerUri;
    private readonly string _credentials;

    public RequestExecutionHelper(string userName = null, string password = null, string packageServerHostAddress = null)
    {
      _packageServerUri = new Uri(packageServerHostAddress ?? "http://localhost:57235");
      var hasCredentials = userName != null && password != null;
      _credentials = hasCredentials ? Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + ":" + password)) : null;
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
  }
}
