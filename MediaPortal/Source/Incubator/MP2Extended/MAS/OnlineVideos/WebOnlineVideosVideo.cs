using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos
{
  public class WebOnlineVideosVideo
  {
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string AirDate { get; set; }
    public string Length { get; set; }
    public string StartTime { get; set; }
    public string SubtitleText { get; set; }
    public string SubtitleUrl { get; set; }
    public string VideoUrl { get; set; }
    public string ThumbUrl { get; set; }

    public override string ToString()
    {
      return Title;
    }
  }
}
