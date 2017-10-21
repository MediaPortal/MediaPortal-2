using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie.BaseClasses
{
  class BaseMovieActors
  {
    internal List<WebActor> MovieActors(MediaItem item)
    {
      List<WebActor> output = new List<WebActor>();
      
      var movieActors = (HashSet<object>)item[VideoAspect.Metadata][VideoAspect.ATTR_ACTORS];
      if (movieActors != null)
      {
        output.AddRange(movieActors.Select(actor => new WebActor
        {
          Title = actor.ToString(), PID = 0
        }));
      }

      return output;
    }
  }
}
