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

using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using MediaPortal.Common.General;

namespace MediaPortal.PackageServer.Utility.Extensions
{
  public static class HtmlExtensions
  {
    public static IHtmlString AreaActionLink(this HtmlHelper html, string linkText, string action, string controller, string area)
    {
      return html.ActionLink(linkText, action, controller, new { area }, null);
    }

    #region Paging

    /// <summary>
    ///   Creates a generic pager for any data source
    /// </summary>
    /// <param name = "urlFormat">The link format, like /controller/method/{0}</param>
    /// <param name = "totalPages">The count of pages</param>
    /// <param name = "pageSize">The size of a page (number of items to display per page)</param>
    /// <param name = "currentPage">The current page number (note that this isn't an index)</param>
    /// <returns>System.String</returns>
    public static MvcHtmlString NumericPager(this HtmlHelper helper, string urlFormat, int totalPages, int currentPage, int pageSize)
    {
      const string linkFormat = "<a class=\"page-link\" href=\"{0}\" data-page=\"{1}\" data-page-size=\"{2}\">{1}</a>";
      bool isFirst = true;
      var sb = new StringBuilder();
      for (int page = 1; page <= totalPages; page++)
      {
        if (!isFirst)
        {
          sb.Append(" ");
        }
        if (currentPage != page)
        {
          // render as link
          var url = string.Format(urlFormat, page, pageSize);
          sb.AppendFormat(linkFormat, url, page, pageSize);
        }
        else
        {
          // render current page as text
          sb.AppendFormat("<b>{0}</b>", page);
        }
        isFirst = false;
      }
      return new MvcHtmlString(sb.ToString());
    }

    #endregion

    #region ActionConditional and RenderActionConditional

    public static void RenderActionConditional(this HtmlHelper htmlHelper, bool condition, ActionResult result)
    {
      if (condition)
      {
        var controller = result.GetPropertyValue<string>("Controller");
        var action = result.GetPropertyValue<string>("Action");
        var routeValues = result.GetPropertyValue<RouteValueDictionary>("RouteValueDictionary");
        htmlHelper.RenderAction(action, controller, routeValues);
      }
    }

    public static MvcHtmlString ActionConditional(this HtmlHelper htmlHelper, bool condition, ActionResult result)
    {
      if (condition)
      {
        var controller = result.GetPropertyValue<string>("Controller");
        var action = result.GetPropertyValue<string>("Action");
        var routeValues = result.GetPropertyValue<RouteValueDictionary>("RouteValueDictionary");
        return htmlHelper.Action(action, controller, routeValues);
      }
      return new MvcHtmlString(string.Empty);
    }

    #endregion

    #region PartialConditional

    public static MvcHtmlString PartialConditional(this HtmlHelper htmlHelper, bool condition, string partialViewName)
    {
      return PartialConditional(htmlHelper, condition, partialViewName, null, null);
    }

    public static MvcHtmlString PartialConditional(this HtmlHelper htmlHelper, bool condition, string partialViewName, object model)
    {
      return PartialConditional(htmlHelper, condition, partialViewName, model, null);
    }

    public static MvcHtmlString PartialConditional(this HtmlHelper htmlHelper, bool condition, string partialViewName, ViewDataDictionary viewData)
    {
      return PartialConditional(htmlHelper, condition, partialViewName, null, viewData);
    }

    public static MvcHtmlString PartialConditional(this HtmlHelper htmlHelper, bool condition, string partialViewName, object model, ViewDataDictionary viewData)
    {
      if (condition)
      {
        if (model != null && viewData != null)
        {
          return htmlHelper.Partial(partialViewName, model, viewData);
        }
        if (model != null)
        {
          return htmlHelper.Partial(partialViewName, model);
        }
        if (viewData != null)
        {
          return htmlHelper.Partial(partialViewName, viewData);
        }
        return htmlHelper.Partial(partialViewName);
      }
      return new MvcHtmlString(string.Empty);
    }

    #endregion

    #region RenderPartialConditional

    public static void RenderPartialConditional(this HtmlHelper htmlHelper, bool condition, string partialViewName)
    {
      RenderPartialConditional(htmlHelper, condition, partialViewName, null, null);
    }

    public static void RenderPartialConditional(this HtmlHelper htmlHelper, bool condition, string partialViewName, object model)
    {
      RenderPartialConditional(htmlHelper, condition, partialViewName, model, null);
    }

    public static void RenderPartialConditional(this HtmlHelper htmlHelper, bool condition, string partialViewName, ViewDataDictionary viewData)
    {
      RenderPartialConditional(htmlHelper, condition, partialViewName, null, viewData);
    }

    public static void RenderPartialConditional(this HtmlHelper htmlHelper, bool condition, string partialViewName, object model, ViewDataDictionary viewData)
    {
      if (condition)
      {
        if (model != null && viewData != null)
        {
          htmlHelper.RenderPartial(partialViewName, model, viewData);
        }
        else if (model != null)
        {
          htmlHelper.RenderPartial(partialViewName, model);
        }
        else if (viewData != null)
        {
          htmlHelper.RenderPartial(partialViewName, viewData);
        }
        else
        {
          htmlHelper.RenderPartial(partialViewName);
        }
      }
    }

    #endregion
  }
}