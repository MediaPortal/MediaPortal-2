using System.IO;
using System.Web;
using Microsoft.AspNet.Mvc;

namespace MediaPortal.Plugins.MP2Extended.Controllers.json
{
  [Route("[Controller]/json/[Action]")]
  public class DebugAccessServiceController : Controller
  {
    [HttpGet]
    [ApiExplorerSettings(GroupName = "DebugAccessService")]
    public FileResult Test()
    {
      string path = Path.Combine(MP2ExtendedService.ASSEMBLY_PATH, "www\\images\\ui-icons_cd0a0a_256x240.png");
      var file = System.IO.File.OpenRead(path);
      return File(file, MimeMapping.GetMimeMapping(path));
      //return System.IO.File.OpenRead(Path.Combine(MP2ExtendedService.ASSEMBLY_PATH, "www/images/ui-icons_cd0a0a_256x240.png"));
    } 
  }
}
