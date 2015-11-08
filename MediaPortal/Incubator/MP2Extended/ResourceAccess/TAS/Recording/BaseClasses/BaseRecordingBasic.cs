using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Threading;
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Utilities;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Recording.BaseClasses
{
  class BaseRecordingBasic
  {
    internal WebRecordingBasic RecordingBasic(MediaItem item)
    {
      MediaItemAspect recordingAspect = item.Aspects[RecordingAspect.ASPECT_ID];
      ResourcePath path = ResourcePath.Deserialize((string)item.Aspects[ProviderResourceAspect.ASPECT_ID][ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH]);

      return new WebRecordingBasic
      {
        Id = item.MediaItemId.ToString(),
        Title = (string)item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_TITLE),
        ChannelName = (string)recordingAspect.GetAttributeValue(RecordingAspect.ATTR_CHANNEL),
        Description = (string)item.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_STORYPLOT),
        StartTime = (DateTime) (recordingAspect.GetAttributeValue(RecordingAspect.ATTR_STARTTIME) ?? DateTime.Now),
        EndTime = (DateTime) (recordingAspect.GetAttributeValue(RecordingAspect.ATTR_ENDTIME) ?? DateTime.Now),
        Genre = (item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_GENRES] as HashSet<object> != null) ? string.Join(", ", ((HashSet<object>)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_GENRES]).Cast<string>().ToArray()) : string.Empty,
        TimesWatched = (int)(item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_PLAYCOUNT] ?? 0),
        FileName = (path != null && path.PathSegments.Count > 0) ? StringUtils.RemovePrefixIfPresent(path.LastPathSegment.Path, "/") : string.Empty,
      };
    }
  }
}
