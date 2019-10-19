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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using System;
using System.Collections.Generic;

namespace MediaPortal.Extensions.MetadataExtractors
{
  public class RecordingUtils
  {
    public static void CheckAndPrepareAspectRefresh(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      //If recording ended after last import we need to prepare the aspects for updating
      if (MediaItemAspect.TryGetAspect(extractedAspectData, ImporterAspect.Metadata, out var importerAspect) && MediaItemAspect.TryGetAspect(extractedAspectData, RecordingAspect.Metadata, out var recordingAspect) && 
        importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_LAST_IMPORT_DATE) < recordingAspect.GetAttributeValue<DateTime>(RecordingAspect.ATTR_ENDTIME))
      {
        //Remove thumbnail because it was from the start of the recording
        extractedAspectData.Remove(ThumbnailLargeAspect.ASPECT_ID);
      }
    }
  }
}
