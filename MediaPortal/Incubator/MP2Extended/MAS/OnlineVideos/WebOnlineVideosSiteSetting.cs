using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos
{
  /// <summary>
  /// Usage:
  /// If PossibleValues.Length == 0 && IsBool == false ? Value is a simple string
  /// If PossibleValues.Length != 0 ? The value must be one of these options
  /// If IsBool == true ? the value must be true or false
  /// </summary>
  public class WebOnlineVideosSiteSetting
  {
    public string Id { get; set; }
    public string SiteId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Value { get; set; }
    public string[] PossibleValues { get; set; }
    public bool IsBool { get; set; }

    public override string ToString()
    {
      return Name;
    }
  }
}
