using System;
using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.MAS.General
{
  public class WebMediaItem : WebObject, IDateAddedSortable, ITitleSortable, ITypeSortable, IArtwork
  {
    public WebMediaItem()
    {
      DateAdded = new DateTime(1970, 1, 1);
      Path = new List<string>();
      Artwork = new List<WebArtwork>();
    }

    public string Id { get; set; }
    public IList<string> Path { get; set; }
    public DateTime DateAdded { get; set; }
    public string Title { get; set; }
    public IList<WebArtwork> Artwork { get; set; }
    public virtual WebMediaType Type { get; set; }
  }
}