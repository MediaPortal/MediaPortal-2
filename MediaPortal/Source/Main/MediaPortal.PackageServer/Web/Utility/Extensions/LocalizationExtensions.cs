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

using System.Globalization;
using System.Web;
using System.Web.Compilation;
using System.Web.Mvc;

namespace MediaPortal.PackageServer.Utility.Extensions
{
  public static class LocalizationExtensions
  {
    public static string Resource(this HtmlHelper htmlhelper, string expression, params object[] args)
    {
      string virtualPath = GetVirtualPath(htmlhelper);
      return GetResourceString(htmlhelper.ViewContext.HttpContext, expression, virtualPath, args);
    }

    public static string Resource(this Controller controller, string expression, params object[] args)
    {
      return GetResourceString(controller.HttpContext, expression, "~/", args);
    }

    private static string GetResourceString(HttpContextBase httpContext, string expression, string virtualPath, object[] args)
    {
      ExpressionBuilderContext context = new ExpressionBuilderContext(virtualPath);
      ResourceExpressionBuilder builder = new ResourceExpressionBuilder();
      ResourceExpressionFields fields = (ResourceExpressionFields)builder.ParseExpression(expression, typeof(string), context);

      if (!string.IsNullOrEmpty(fields.ClassKey))
        return string.Format((string)httpContext.GetGlobalResourceObject(fields.ClassKey, fields.ResourceKey, CultureInfo.CurrentUICulture), args);

      return string.Format((string)httpContext.GetLocalResourceObject(virtualPath, fields.ResourceKey, CultureInfo.CurrentUICulture), args);
    }

    private static string GetVirtualPath(HtmlHelper htmlhelper)
    {
      var razorView = htmlhelper.ViewContext.View as RazorView;
      if (razorView != null)
        return razorView.ViewPath;

      var webFormView = htmlhelper.ViewContext.View as WebFormView;
      if (webFormView != null)
        return webFormView.ViewPath;

      return null;
    }
  }
}