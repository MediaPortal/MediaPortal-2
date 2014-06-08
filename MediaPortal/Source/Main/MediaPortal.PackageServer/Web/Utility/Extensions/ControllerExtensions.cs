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

using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace MediaPortal.PackageServer.Utility.Extensions
{
  public static class ControllerExtensions
  {
    #region RenderPartialViewToString

    public static string RenderPartialToString(this ControllerBase controller, string partialName, object model)
    {
      var vd = new ViewDataDictionary(controller.ViewData);
      var vp = new ViewPage { ViewData = vd, ViewContext = new ViewContext(), Url = new UrlHelper(controller.ControllerContext.RequestContext) };
      ViewEngineResult result = ViewEngines.Engines.FindPartialView(controller.ControllerContext, partialName);
      if (result.View == null)
      {
        throw new InvalidOperationException(string.Format("The partial view '{0}' could not be found", partialName));
      }
      string partialPath = ((WebFormView)result.View).ViewPath;
      vp.ViewData.Model = model;
      Control control = vp.LoadControl(partialPath);
      vp.Controls.Add(control);

      var sb = new StringBuilder();
      using (var sw = new StringWriter(sb))
      {
        using (var tw = new HtmlTextWriter(sw))
        {
          vp.RenderControl(tw);
        }
      }
      return sb.ToString();
    }

    #endregion

    #region ExecuteSendFile extension: helper for downloading attachments

    #region Delegates

    public delegate void FileWriterDelegate(string filename, Stream outputStream);

    #endregion

    private static void ExecuteSendFile(HttpContextBase context, string filename, FileWriterDelegate fileWriter)
    {
      context.Response.ClearHeaders();
      context.Response.ClearContent();
      context.Response.AppendHeader("Content-disposition", string.Format("attachment;filename=\"{0}\"", HttpUtility.UrlPathEncode(Path.GetFileName(filename))));
      if (fileWriter != null)
      {
        fileWriter(filename, context.Response.OutputStream);
      }
      else
      {
        if (File.Exists(filename))
        {
          context.Response.WriteFile(filename);
        }
        else
        {
          throw new HttpException(404, "The requested file was not found.");
        }
      }
    }

    public static ActionResult SendFileAsAttachment(this Controller controller, string filename)
    {
      return new DelegatedActionResult(context => ExecuteSendFile(context.HttpContext, filename, null));
    }

    public static ActionResult SendFileAsAttachment(this Controller controller, string filename, FileWriterDelegate fileWriter)
    {
      return new DelegatedActionResult(context => ExecuteSendFile(context.HttpContext, filename, fileWriter));
    }

    #endregion
  }
}