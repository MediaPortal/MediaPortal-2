using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;

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
