using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos
{
  class WebOnlineVideosSiteCategory
  {
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool HasSubCategories { get; set; }

    public override string ToString()
    {
      return Title;
    }
  }
}
