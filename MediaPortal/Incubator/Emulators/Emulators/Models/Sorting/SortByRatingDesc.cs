using Emulators.Common.Games;
using MediaPortal.UiComponents.Media.Models.Sorting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;

namespace Emulators.Models.Sorting
{
  public class SortByRatingDesc : AbstractSortByComparableValueAttribute<double>
  {
    public SortByRatingDesc() : base(EmulatorsConsts.RES_SORT_BY_RATING, EmulatorsConsts.RES_GROUP_BY_RATING, GameAspect.ATTR_RATING) { }

    public override int Compare(MediaItem x, MediaItem y)
    {
      int compare = base.Compare(x, y);
      return compare * -1;
    }
  }
}
