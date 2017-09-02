using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Utilities;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Recording.BaseClasses
{
  class BaseRecordingBasic
  {
    internal WebRecordingBasic RecordingBasic(MediaItem item)
    {
      MediaItemAspect recordingAspect = item[RecordingAspect.Metadata];
      ResourcePath path = ResourcePath.Deserialize((string)item[ProviderResourceAspect.Metadata][ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH]);

      return new WebRecordingBasic
      {
        Id = item.MediaItemId.ToString(),
        Title = (string)item[MediaAspect.Metadata].GetAttributeValue(MediaAspect.ATTR_TITLE),
        ChannelName = (string)recordingAspect.GetAttributeValue(RecordingAspect.ATTR_CHANNEL),
        Description = (string)item[VideoAspect.Metadata].GetAttributeValue(VideoAspect.ATTR_STORYPLOT),
        StartTime = (DateTime) (recordingAspect.GetAttributeValue(RecordingAspect.ATTR_STARTTIME) ?? DateTime.Now),
        EndTime = (DateTime) (recordingAspect.GetAttributeValue(RecordingAspect.ATTR_ENDTIME) ?? DateTime.Now),
        Genre = (item[VideoAspect.Metadata][VideoAspect.ATTR_GENRES] as HashSet<object> != null) ? string.Join(", ", ((HashSet<object>)item[VideoAspect.Metadata][VideoAspect.ATTR_GENRES]).Cast<string>().ToArray()) : string.Empty,
        TimesWatched = (int)(item[MediaAspect.Metadata][MediaAspect.ATTR_PLAYCOUNT] ?? 0),
        FileName = (path != null && path.PathSegments.Count > 0) ? StringUtils.RemovePrefixIfPresent(path.LastPathSegment.Path, "/") : string.Empty,
      };
    }
  }
}
