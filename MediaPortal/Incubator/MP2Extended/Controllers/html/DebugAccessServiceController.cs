using System.IO;
using System.Web;
using Microsoft.AspNet.Mvc;

namespace MediaPortal.Plugins.MP2Extended.Controllers.html
{
  [Route("[Controller]/html/[Action]")]
  public class DebugAccessServiceController : Controller
  {
    [HttpGet]
    [ApiExplorerSettings(GroupName = "DebugAccessService")]
    public IActionResult Index() => new ViewResult();

    [HttpGet]
    [ApiExplorerSettings(GroupName = "DebugAccessService")]
    public IActionResult Settings() => new ViewResult();

    [HttpGet]
    [ApiExplorerSettings(GroupName = "DebugAccessService")]
    public IActionResult Users() => new ViewResult();
  }
}
