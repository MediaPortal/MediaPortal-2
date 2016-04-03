using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.AspNetWebApi.Controllers.ServerPlugins
{
  public class SettingDescription
  {
    public string Name { get; set; }
    public dynamic Value { get; set; }
    public string Type { get; set; }
  }
}
