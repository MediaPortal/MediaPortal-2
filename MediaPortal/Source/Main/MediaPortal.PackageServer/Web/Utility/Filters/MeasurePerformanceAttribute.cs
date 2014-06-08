#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Diagnostics;
using System.Web;
using System.Web.Mvc;

namespace MediaPortal.PackageServer.Utility.Filters
{
  public class MeasurePerformanceAttribute : ActionFilterAttribute
  {
    #region IActionFilter Members

    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
      GetTimer(filterContext, "action").Start();
    }

    public override void OnActionExecuted(ActionExecutedContext filterContext)
    {
      GetTimer(filterContext, "action").Stop();
    }

    #endregion

    #region IResultFilter Members

    public override void OnResultExecuting(ResultExecutingContext filterContext)
    {
      GetTimer(filterContext, "render").Start();
    }

    public override void OnResultExecuted(ResultExecutedContext filterContext)
    {
      Stopwatch renderTimer = GetTimer(filterContext, "render");
      renderTimer.Stop();

      Stopwatch actionTimer = GetTimer(filterContext, "action");
      HttpResponseBase response = filterContext.HttpContext.Response;
      HttpRequestBase request = filterContext.HttpContext.Request;

      if (Debugger.IsAttached && response.ContentType == "text/html")
      {
        bool isChildOrAjax = filterContext.IsChildAction || request.IsAjaxRequest();
        string fmt = isChildOrAjax ? "<p class=\"invisible\">{0}.{1}: {2}ms + {3}ms<p>" : "<p class=\"invisible\">Hitting '{0}.{1}' took {2}ms, page rendered in {3}ms.<p>";
        string message = string.Format(fmt,
          filterContext.RouteData.Values["controller"],
          filterContext.RouteData.Values["action"],
          actionTimer.ElapsedMilliseconds,
          renderTimer.ElapsedMilliseconds);
        response.Write(message);
      }
      actionTimer.Stop();
    }

    #endregion

    private static Stopwatch GetTimer(ControllerContext context, string name)
    {
      string key = "__timer__" + name;
      if (context.HttpContext.Items.Contains(key))
      {
        return (Stopwatch)context.HttpContext.Items[key];
      }
      var result = new Stopwatch();
      context.HttpContext.Items[key] = result;
      return result;
    }
  }
}