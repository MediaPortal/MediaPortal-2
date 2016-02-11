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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using HttpServer.Exceptions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MediaPortal.Plugins.AspNetWebApi.Controllers
{
  /// <summary>
  /// AspNet MVC Controller for System Information
  /// </summary>
  [Route("v1/Server/[Controller]")]
  public class InformationController : Controller
  {
    #region Private fields

    private readonly ILogger _logger;

    #endregion

    #region Constructor

    public InformationController(ILoggerFactory loggerFactory)
    {
      _logger = loggerFactory.CreateLogger<MediaItemsController>();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// GET /api/v1/Server/Information
    /// </summary>
    /// <returns>Resturns an object representing the current Server Status</returns>
    [HttpGet]
    public ServerInformation.ServerInformation Get()
    {
      return new ServerInformation.ServerInformation();
    }

    #endregion
  }
}
