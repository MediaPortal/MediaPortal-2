using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Http.Features;

namespace MediaPortal.Plugins.MP2Extended.Controllers.html
{
  [Route("[Controller]/[Action]")]
  public class ErrorController : Controller
  {
    [HttpGet]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Error()
    {
      var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
      var error = feature?.Error;
      return View("~/Views/Error/Error.cshtml", error);
    }
  }
}
