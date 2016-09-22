using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UiComponents.Media.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  public class FilterByAlbumCriterion : RelationshipMLFilterCriterion
  {
    public FilterByAlbumCriterion() :
      base(AudioAlbumAspect.ROLE_ALBUM, AudioAspect.ROLE_TRACK, Consts.NECESSARY_ALBUM_MIAS, Consts.OPTIONAL_ALBUM_MIAS,
        new SortInformation(AudioAlbumAspect.ATTR_ALBUM, SortDirection.Ascending))
    {
    }
  }
}
