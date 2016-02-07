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
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MediaPortal.Plugins.AspNetWebApi.Controllers
{
  /// <summary>
  /// AspNet MVC Controller for <see cref="MediaItem"/>s
  /// </summary>
  [Route("v1/Server/[Controller]")]
  public class ServerControllerController : Controller
  {
    #region Private fields

    private readonly ILogger _logger;
    private readonly IClientManager _clientManager;

    #endregion

    #region Constructor

    public ServerControllerController(ILoggerFactory loggerFactory)
    {
      _logger = loggerFactory.CreateLogger<ServerControllerController>();
      if (ServiceRegistration.IsRegistered<IClientManager>())
        _clientManager = ServiceRegistration.Get<IClientManager>();
      else
        _logger.LogWarning("The Service IClientManager is not registered");
    }

    #endregion

    #region Public methods

    /// <summary>
    /// GET /api/v1/Server/ServerController/AttachClient/[clientSystemId]
    /// </summary>
    [HttpGet("AttachClient/{clientSystemId}")]
    public void AttachClient(string clientSystemId)
    {
      if (_clientManager == null)
        throw new HttpException(HttpStatusCode.InternalServerError, "IServerController is null");

      _clientManager.AttachClient(clientSystemId);
    }

    /// <summary>
    /// GET /api/v1/Server/ServerController/DetachClient/[clientSystemId]
    /// </summary>
    [HttpGet("DetachClient/{clientSystemId}")]
    public void DetachClient(string clientSystemId)
    {
      if (_clientManager == null)
        throw new HttpException(HttpStatusCode.InternalServerError, "IServerController is null");

      _clientManager.DetachClientAndRemoveShares(clientSystemId);
    }

    /// <summary>
    /// GET /api/v1/Server/ServerController/GetAttachedClients]
    /// </summary>
    [HttpGet("GetAttachedClients")]
    public ICollection<MPClientMetadata> GetAttachedClients()
    {
      if (_clientManager == null)
        throw new HttpException(HttpStatusCode.InternalServerError, "IServerController is null");

      return _clientManager.AttachedClients.Values;
    }

    /// <summary>
    /// GET /api/v1/Server/ServerController/GetConnectedClients]
    /// </summary>
    [HttpGet("GetConnectedClients")]
    public IEnumerable<string> GetConnectedClients()
    {
      if (_clientManager == null)
        throw new HttpException(HttpStatusCode.InternalServerError, "IServerController is null");

      return _clientManager.ConnectedClients.Select(clientConnection => clientConnection.Descriptor.MPFrontendServerUUID);
    }

    /// <summary>
    /// GET /api/v1/Server/ServerController/GetSystemNameForSystemId/[systemId]
    /// </summary>
    [HttpGet("GetSystemNameForSystemId/{systemId}")]
    public SystemName GetSystemNameForSystemId(string systemId)
    {
      if (_clientManager == null)
        throw new HttpException(HttpStatusCode.InternalServerError, "IServerController is null");

      return _clientManager.GetSystemNameForSystemId(systemId);
    }

    #endregion
  }
}
