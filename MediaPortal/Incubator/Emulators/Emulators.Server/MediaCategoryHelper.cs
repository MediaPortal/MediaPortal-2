using Emulators.Common;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Server
{
  public class MediaCategoryHelper : IMediaCategoryHelper
  {
    public ICollection<string> GetMediaCategories(ResourcePath path)
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();

      ICollection<Share> shares = mediaLibrary.GetShares(systemResolver.LocalSystemId).Values;
      Share bestShare = SharesHelper.BestContainingPath(shares, path);

      List<string> categories = new List<string>();
      if (bestShare != null)
        categories.AddRange(bestShare.MediaCategories);
      return categories;
    }
  }
}
