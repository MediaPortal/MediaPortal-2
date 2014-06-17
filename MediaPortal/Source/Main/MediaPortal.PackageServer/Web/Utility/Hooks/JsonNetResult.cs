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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace MediaPortal.PackageServer.Utility.Hooks
{
  public class JsonNetResult : JsonResult
  {
    public JsonSerializerSettings SerializerSettings { get; set; }
    public Formatting Formatting { get; set; }
 
    public JsonNetResult(object data)
    {
      Data = data;
      SerializerSettings = JsonConvert.DefaultSettings();
      Formatting = Formatting.Indented;
    }
 
    public override void ExecuteResult(ControllerContext context)
    {
      if (context == null)
      {
        throw new ArgumentNullException("context");
      }

      var response = context.HttpContext.Response;
      response.ContentType = !string.IsNullOrEmpty(ContentType) ? ContentType : "application/json";

      if (ContentEncoding != null)
      {
        response.ContentEncoding = ContentEncoding;
      }

      if (Data != null)
      {
        var writer = new JsonTextWriter(response.Output) { Formatting = Formatting };
        var serializer = JsonSerializer.Create(SerializerSettings);
        serializer.Serialize(writer, Data);
        writer.Flush();
      }
    }
  }
}
