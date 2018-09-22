﻿#region Copyright (C) 2007-2017 Team MediaPortal

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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace Tests.Server.NfoMetadataExtractor
{
  static class TestUtils
  {
    public static bool HasExternalId(IList<MediaItemAspect> externalIdentifierAspects, string source, string type, string id)
    {
      MediaItemAspect idAspect = externalIdentifierAspects.SingleOrDefault(e => 
        e.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE) == source &&
        e.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE) == type &&
        e.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID) == id);
      return idAspect != null;
    }

  }
}
