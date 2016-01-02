using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using System.IO;
using System.Web;

namespace MediaPortal.Plugins.MP2Extended.Controllers
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
