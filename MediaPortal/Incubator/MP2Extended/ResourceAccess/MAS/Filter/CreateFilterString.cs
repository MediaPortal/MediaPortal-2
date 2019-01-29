#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Filter
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebStringResult),
    Summary = "Create a filter string from a given set of parameters. The result of this method can be used as the \"filter\"\r\nparameter in other MPExtended APIs.\r\n\r\nA filter consists of a field name (alphabetic, case-sensitive), followed by an operator (only special characters),\r\nfollowed by the value. Multiple filters are separated with a comma. \r\n\r\nTo define multiple filters, call this method multiple times and join them together. ")]
  [ApiFunctionParam(Name = "field", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "op", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "value", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "conjunction", Type = typeof(string), Nullable = true)]
  internal class CreateFilterString
  {
    public static Task<WebStringResult> ProcessAsync(IOwinContext context, string field, string op, string value, string conjunction)
    {
      if (field == null)
        throw new BadRequestException("CreateFilterString: field is null");

      if (op == null)
        throw new BadRequestException("CreateFilterString: op is null");

      if (value == null)
        throw new BadRequestException("CreateFilterString: value is null");
      
      string val = value.Replace("\\", "\\\\").Replace("'", "\\'");
      return Task.FromResult(new WebStringResult(conjunction == null ?
          String.Format("{0}{1}'{2}'", field, op, val) :
          String.Format("{0}{1}'{2}'{3} ", field, op, val, conjunction == "and" ? "," : "|")));
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
