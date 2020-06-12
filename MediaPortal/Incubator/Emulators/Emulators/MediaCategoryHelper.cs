using Emulators.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.MediaManagement.Helpers;

namespace Emulators
{
  public class MediaCategoryHelper : IMediaCategoryHelper
  {
    public ICollection<string> GetMediaCategories(ResourcePath path)
    {
      List<string> categories = new List<string>();
      var systemResolver = ServiceRegistration.Get<ISystemResolver>();
      var scm = ServiceRegistration.Get<IServerConnectionManager>();
      var cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return categories;

      ICollection<Share> shares = cd.GetSharesAsync(systemResolver.LocalSystemId, SharesFilter.All).Result;
      Share bestShare = SharesHelper.BestContainingPath(shares, path);
      if (bestShare != null)
        categories.AddRange(bestShare.MediaCategories);
      return categories;
    }
  }
}
