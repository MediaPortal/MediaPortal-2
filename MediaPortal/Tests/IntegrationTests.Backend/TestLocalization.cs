using System;
using System.Collections.Generic;
using System.Globalization;
using MediaPortal.Common.Localization;

namespace IntegrationTests.Backend
{
  class TestLocalization : ILocalization
  {
    public ICollection<CultureInfo> AvailableLanguages { get; }
    public CultureInfo CurrentCulture { get; }

    public void Startup()
    {
      throw new NotImplementedException();
    }

    public void AddLanguageDirectory(string directory)
    {
      throw new NotImplementedException();
    }

    public void ChangeLanguage(CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    public bool TryTranslate(string section, string name, out string translation, params object[] parameters)
    {
      throw new NotImplementedException();
    }

    public string ToString(string label, params object[] parameters)
    {
      return string.Format(label, parameters);
    }

    public CultureInfo GetBestAvailableLanguage()
    {
      throw new NotImplementedException();
    }
  }
}
