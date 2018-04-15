using System;
using System.Text;

namespace NativeWifi
{
  
  enum WinProfileAuthenticationEnumeration { open, shared, WPA, WPAPSK, WPA2, WPA2PSK };

  enum WinProfileEncryptionEnumeration { none, WEP, TKIP, AES };
  
  public static class Helper
  {
    /// <summary>
    /// Converts a 802.11 SSID to a string.
    /// </summary>
    internal static string GetStringForSSID(Wlan.Dot11Ssid ssid)
    {
      return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
    }

    /// <summary>
    /// Create a valid Profile xml according to: http://msdn.microsoft.com/en-us/library/ms707381(v=VS.85).aspx
    /// </summary>
    /// <param name="ssid"></param>
    /// <param name="key"></param>
    /// <param name="authAlg"></param>
    /// <param name="encAlg"></param>
    /// <returns></returns>
    internal static string GetProfileXml(string ssid, string key, Wlan.Dot11AuthAlgorithm authAlg, Wlan.Dot11CipherAlgorithm encAlg)
    {
      WinProfileAuthenticationEnumeration? auth = null;
      WinProfileEncryptionEnumeration? enc = null;
      switch (authAlg)
      {
        case Wlan.Dot11AuthAlgorithm.IEEE80211_SharedKey:
          auth = WinProfileAuthenticationEnumeration.open;
          enc = WinProfileEncryptionEnumeration.WEP;
          break;
        case Wlan.Dot11AuthAlgorithm.WPA_PSK:
          auth = WinProfileAuthenticationEnumeration.WPAPSK;
          break;
        case Wlan.Dot11AuthAlgorithm.RSNA_PSK:
          auth = WinProfileAuthenticationEnumeration.WPA2PSK;
          break;
      }
      switch (encAlg)
      {
        case Wlan.Dot11CipherAlgorithm.TKIP:
          enc = WinProfileEncryptionEnumeration.TKIP;
          break;
        case Wlan.Dot11CipherAlgorithm.CCMP:
          enc = WinProfileEncryptionEnumeration.AES;
          break;
      }

      if (enc != null && auth != null)
      {
        return string.Format(@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
	<name>{0}</name>
	<SSIDConfig>
		<SSID>
			<name>{0}</name>
		</SSID>
	</SSIDConfig>
	<connectionType>ESS</connectionType>
	<connectionMode>auto</connectionMode>
	<MSM>
		<security>
			<authEncryption>
				<authentication>{2}</authentication>
				<encryption>{3}</encryption>
				<useOneX>false</useOneX>
			</authEncryption>
			<sharedKey>
				<keyType>passPhrase</keyType>
				<protected>false</protected>
				<keyMaterial>{1}</keyMaterial>
			</sharedKey>
		</security>
	</MSM>
</WLANProfile>", ssid, key, auth.ToString(), enc.ToString());
      }
      else
      {
        return null;
      }
    }

  }

}
