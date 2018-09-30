#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Threading.Tasks;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor;

namespace Tests.Server.FanArt.FanArtHandlersForTests
{
  class SeriesFanArtHandlerForTests : SeriesFanArtHandler
  {
    public Task TestExtractEpisodeFolderFanArt(Guid mediaItemId, ResourcePath path)
    {
      IResourceLocator locator = new ResourceLocator("test", path);
      return ExtractEpisodeFolderFanArt(locator, mediaItemId, "episode");
    }

    public Task TestExtractSeriesFolderFanArt(Guid mediaItemId, ResourcePath path, IList<Tuple<Guid,string>> actors = null)
    {
      IResourceLocator locator = new ResourceLocator("test", path);
      return ExtractSeriesFolderFanArt(locator, mediaItemId, "series", actors);
    }

    public Task TestExtractSeasonFolderFanArt(Guid mediaItemId, ResourcePath path, int? seasonNumber, IList<Tuple<Guid, string>> actors = null)
    {
      IResourceLocator locator = new ResourceLocator("test", path);
      return ExtractSeasonFolderFanArt(locator, mediaItemId, "season", seasonNumber, actors);
    }

    public FanArtPathCollection TestGetAdditionalSeasonFolderFanArt(IList<ResourcePath> potentialFanArtFiles, int seasonNumber)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      GetAdditionalSeasonFolderFanArt(paths, potentialFanArtFiles, seasonNumber);
      return paths;
    }
  }
}
