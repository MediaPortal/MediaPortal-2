using System;
using System.IO;
using System.Web;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.json.Settings;
using Microsoft.AspNet.Mvc;

namespace MediaPortal.Plugins.MP2Extended.Controllers.json
{
  [Route("[Controller]/json/[Action]")]
  public class DebugAccessServiceController : Controller
  {
    [HttpGet]
    [ApiExplorerSettings(GroupName = "DebugAccessService")]
    public WebBoolResult ChangeSetting(string name, string value)
    {
      return new ChangeSetting().Process(name, value);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "DebugAccessService")]
    public WebBoolResult CreateUser(string username, string type, string password)
    {
      return new CreateUser().Process(username, type, password);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "DebugAccessService")]
    public WebBoolResult DeleteUser(Guid id)
    {
      return new DeleteUser().Process(id);
    }
  }
}
