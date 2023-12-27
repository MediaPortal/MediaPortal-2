using System.Runtime.Serialization;

namespace Webradio.Stations;

public class CountryCodes : List<Translation>
{
  public string GetCountryCode(string value)
  {
    var code = "ZZ";

    if (value == null)
      return code;

    if (value.ToLower() == "usa")
      return "US";

    foreach (var trl in this)
      if (value.Contains(trl.Value) || trl.Value.Contains(value))
      {
        code = trl.Name;
        break;
      }

    return code.Replace("Country.", "");
  }
}

public class LanguageCodes : List<Translation>
{
  public string GetLanguageCode(string value)
  {
    var code = "";

    if (value == null)
      return code;

    foreach (var trl in this)
      if (trl.Value == value)
      {
        code = trl.Name;
        break;
      }

    return code.Replace("Language.", "");
    ;
  }
}

public class Translations : List<Translation>
{
}

[DataContract]
public class Translation
{
  [DataMember(Name = "Name")] public string Name { get; set; } = string.Empty;

  [DataMember(Name = "Value")] public string Value { get; set; } = string.Empty;
}
