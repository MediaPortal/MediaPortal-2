using System;
using System.Globalization;

using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration.Settings
{

  /// <summary>
  /// Holds all settings related to localization.
  /// </summary>
  public class LocalizationSettings
  {

    #region Variables

    private string _language;
    private string _continent;
    private string _country;
    private string _region;
    private string _city;

    #endregion

    #region Properties

    [Setting(SettingScope.User, "en")]
    public string LanguageCode
    {
      get { return _language; }
      set { _language = value; }
    }

    [Setting(SettingScope.User, "")]
    public string Continent
    {
      get { return _language; }
      set { _language = value; }
    }

    [Setting(SettingScope.User, "")]
    public string CountryCode
    {
      get
      {
        if (_country == "") // Force the default value
          _country = RegionInfo.CurrentRegion.TwoLetterISORegionName.ToLower(new CultureInfo("en"));
        return _country;
      }
      set { _country = value; }
    }

    [Setting(SettingScope.User, "")]
    public string Region
    {
      get { return _region; }
      set { _region = value; }
    }

    [Setting(SettingScope.User, "")]
    public string City
    {
      get { return _city; }
      set { _city = value; }
    }

    #endregion

  }
}
