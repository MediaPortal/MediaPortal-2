using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Extension;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Web;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TraktAPI;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Authentication
{
  public static class TraktAuth
  {
    const string APPLICATION_ID = "aea41e88de3cd0f8c8b2404d84d2e5d7317789e67fad223eba107aea2ef59068";
    const string SECRET_ID = "adafedb5cd065e6abeb9521b8b64bc66adb010a7c08128811bf32c989f35b77a";
    const string REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";


    public static TraktOAuthToken GetOAuthToken(string key)
    {
      // set our required headers now
      TraktWeb._customRequestHeaders.Clear();

      TraktWeb._customRequestHeaders.Add("trakt-api-key", APPLICATION_ID);
      TraktWeb._customRequestHeaders.Add("trakt-api-version", "2");

      string response = TraktWeb.PostToTrakt(TraktURIs.LoginOAuth, GetOAuthLogin(key), true);
      var loginResponse = response.FromJSON<TraktOAuthToken>();

      if (loginResponse == null || loginResponse.AccessToken == null)
        return loginResponse;

      // add the token for authenticated methods
      TraktWeb._customRequestHeaders.Add("Authorization", string.Format("Bearer {0}", loginResponse.AccessToken));

      return loginResponse;
    }

    /// <summary>
    /// Gets a oAuth Login object
    /// </summary>
    private static string GetOAuthLogin(string key)
    {
      bool isPinCode = key.Length == 8;

      return new TraktOAuthLogin
      {
        Code = isPinCode ? key : null,
        RefreshToken = isPinCode ? null : key,
        ClientId = APPLICATION_ID,
        ClientSecret = SECRET_ID,
        RedirectUri = REDIRECT_URI,
        GrantType = isPinCode ? "authorization_code" : "refresh_token"
      }
        .ToJSON();
    }
  }
}
