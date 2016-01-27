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
using System.Net;
using System.Reflection;
using System.Xml;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MediaPortal.Plugins.MP2Web.Controllers
{
  /// <summary>
  /// AspNet MVC Controller for Trailers
  /// </summary>
  [Route("/api/[Controller]")]
  public class TrailersController : Controller
  {
    #region Private fields

    private readonly ILogger _logger;

    const string API_URL = "http://api.traileraddict.com/";

    #endregion

    #region Constructor

    public TrailersController(ILoggerFactory loggerFactory)
    {
      _logger = loggerFactory.CreateLogger<TrailersController>();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// GET /api/Trailers/[imdbId]
    /// </summary>
    /// <param name="imdbId">The IMDB Id of the movie</param>
    /// <param name="count">How many trailers the Api should return</param>
    /// <param name="width">The width of the player</param>
    /// <returns>Collection of Trailers matching the given IMDB ID</returns>
    [HttpGet("{imdbId}")]
    public List<Trailer> Get(string imdbId, int count = 1, int width = 680)
    {
      string _imdbId = imdbId.StartsWith("tt") ? imdbId.Substring(2) : imdbId;
      string url = $"{API_URL}?imdb={_imdbId}&count={count}&width={width}";

      List<Trailer> output = new List<Trailer>();

      var xmlDoc = new XmlDocument();
      xmlDoc.Load(url);
      XmlNodeList trailers = xmlDoc.GetElementsByTagName("trailer");
      foreach (XmlNode trailer in trailers)
      {
        output.Add(new Trailer
        {
          Title = trailer.SelectSingleNode("title").InnerText,
          Embed = trailer.SelectSingleNode("embed").InnerText
        });
      }
      return output;
    }

    #endregion
  }
}

public class Trailer
{
  public string Title { get; set; }
  public string Embed { get; set; }
}