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
  public class FilterByMovieCollectionCriterion : RelationshipMLFilterCriterion
  {
    public FilterByMovieCollectionCriterion() :
      base(MovieCollectionAspect.ROLE_MOVIE_COLLECTION, MovieAspect.ROLE_MOVIE, Consts.NECESSARY_MOVIE_COLLECTION_MIAS,
        new SortInformation(MovieCollectionAspect.ATTR_COLLECTION_NAME, SortDirection.Ascending))
    {
    }
  }
}
