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
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Mock
{
  class MockRelationshipExtractor : IRelationshipExtractor
  {
    private static readonly RelationshipExtractorMetadata _METADATA = new RelationshipExtractorMetadata(Guid.Empty, "MockRelationshipExtractor");

    public RelationshipExtractorMetadata Metadata
    {
      get { return _METADATA; }
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid role, Guid linkedRole, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedAspectData, bool forceQuickMode)
    {
      Console.WriteLine("Extracting {0} / {1}", role, linkedRole);
      return false;
    }
  }
}