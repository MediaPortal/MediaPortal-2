using System;
using System.IO;
using System.Reflection;
using System.Web;
using Microsoft.AspNet.Mvc;
using System.Linq;

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
    public string GetControllers()
    {
      Assembly asm = Assembly.GetExecutingAssembly();

      /*var controlleractionlist = asm.GetTypes()
              .Where(type => typeof(Controller).IsAssignableFrom(type))
              .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
              .Where(m => !m.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true).Any())
              .Select(x => new { Controller = x.DeclaringType.Name, Action = x.Name, ReturnType = x.ReturnType.Name, Attributes = String.Join(",", x.GetCustomAttributes().Select(a => a.GetType().Name.Replace("Attribute", ""))) })
              .OrderBy(x => x.Controller).ThenBy(x => x.Action).ToList();*/

      var controlleractionlist = asm.GetTypes()
              .Where(type => typeof(Controller).IsAssignableFrom(type))
              .Select(x => new { Controller = x.Name, Attributes = String.Join(",", x.GetCustomAttributes().Select(a => a.GetType().Name.Replace("Attribute", "") + " = " + x.GetCustomAttributesData().Where(y => y.Constructor.DeclaringType.Name == a.GetType().Name).Select(y => y.ConstructorArguments[0].Value))), AttrValue = String.Join(",", x.GetCustomAttributesData().Select(z => z.ConstructorArguments[0].Value)) })
              .OrderBy(x => x.Controller).ThenBy(x => x.Controller).ToList();

      return string.Join("; ", controlleractionlist.Select(x => x.Controller + " = " + x.Attributes + " = " + x.AttrValue + "\r\n").ToArray());
    }

    

    [HttpGet]
    [ApiExplorerSettings(GroupName = "DebugAccessService")]
    public IActionResult Settings() => new ViewResult();

    [HttpGet]
    [ApiExplorerSettings(GroupName = "DebugAccessService")]
    public IActionResult Users() => new ViewResult();
  }
}
