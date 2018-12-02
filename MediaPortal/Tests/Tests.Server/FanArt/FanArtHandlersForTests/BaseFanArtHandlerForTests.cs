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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;

namespace Tests.Server.FanArt.FanArtHandlersForTests
{
  class BaseFanArtHandlerForTests : BaseFanArtHandler
  {
    public BaseFanArtHandlerForTests()
      : base(new FanArtHandlerMetadata(Guid.Empty, "TestBaseHandler"), new Guid[0])
    {
    }

    public FanArtPathCollection TestGetAllFolderFanArt(IList<ResourcePath> potentialFanArtFiles)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      ExtractAllFanArtImages(potentialFanArtFiles, paths);
      return paths;
    }

    public FanArtPathCollection TestGetAllFolderFanArt(IList<ResourcePath> potentialFanArtFiles, string filename)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      ExtractAllFanArtImages(potentialFanArtFiles, paths, filename);
      return paths;
    }

    public override Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      throw new NotImplementedException();
    }
  }
}
