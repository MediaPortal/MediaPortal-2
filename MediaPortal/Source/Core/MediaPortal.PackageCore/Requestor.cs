#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using System.Linq;
using System.Net;
using System.Net.Http;
using MediaPortal.Common.Logging;

namespace MediaPortal.PackageCore
{
  public class Requestor
  {
    public ILogger Log { get; private set; }

    public Requestor(ILogger log)
    {
      Log = log;
    }

    private bool IsSuccess(HttpResponseMessage response, HttpStatusCode[] successCodes)
    {
      return successCodes.Any(sc => sc == response.StatusCode);
    }

    public virtual bool IsSuccess(HttpResponseMessage response, string successMessage, params HttpStatusCode[] successCodes)
    {
      if (!IsSuccess(response, successCodes))
      {
        Log.Error("{0} ({1}): {2}", (int)response.StatusCode, response.StatusCode, response.ReasonPhrase);
        return false;
      }
      if (successMessage != null)
        Log.Info(successMessage);
      return true;
    }
  }
}
