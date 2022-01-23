#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal static class GetServiceDescription
  {
    public static Task<WebMediaServiceDescription> ProcessAsync(IOwinContext context)
    {
      WebMediaServiceDescription webMediaServiceDescription = new WebMediaServiceDescription();
      webMediaServiceDescription.ApiVersion = GlobalVersion.API_VERSION;
      webMediaServiceDescription.AvailableFileSystemLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 10,
          Name = "MP Movie Shares",
          Version = GlobalVersion.VERSION
        },
        new WebBackendProvider
        {
          Id = 11,
          Name = "MP Picture Shares",
          Version = GlobalVersion.VERSION
        },
        new WebBackendProvider
        {
          Id = 5,
          Name = "MP Shares",
          Version = GlobalVersion.VERSION
        },
        new WebBackendProvider
        {
          Id = 9,
          Name = "MP Music Shares",
          Version = GlobalVersion.VERSION
        },
        new WebBackendProvider
        {
          Id = 14,
          Name = "MP Series Shares",
          Version = GlobalVersion.VERSION
        },
      };
      webMediaServiceDescription.AvailableMovieLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 7,
          Name = "MP Video",
          Version = GlobalVersion.VERSION
        }
      };
      webMediaServiceDescription.AvailableMusicLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 4,
          Name = "MP Music",
          Version = GlobalVersion.VERSION
        }
      };
      webMediaServiceDescription.AvailablePictureLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 8,
          Name = "MP Pictures",
          Version = GlobalVersion.VERSION
        }
      };
      webMediaServiceDescription.AvailableTvShowLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 6,
          Name = "MP Series",
          Version = GlobalVersion.VERSION
        }
      };
      webMediaServiceDescription.DefaultFileSystemLibrary = 5;
      webMediaServiceDescription.DefaultMovieLibrary = 7;
      webMediaServiceDescription.DefaultMusicLibrary = 4;
      webMediaServiceDescription.DefaultPictureLibrary = 8;
      webMediaServiceDescription.DefaultTvShowLibrary = 6;
      webMediaServiceDescription.ServiceVersion = GlobalVersion.VERSION;

      return Task.FromResult(webMediaServiceDescription);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
