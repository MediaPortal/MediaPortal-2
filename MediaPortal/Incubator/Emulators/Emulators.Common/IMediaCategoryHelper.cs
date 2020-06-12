using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common
{
  public interface IMediaCategoryHelper
  {
    ICollection<string> GetMediaCategories(ResourcePath path);
  }
}
