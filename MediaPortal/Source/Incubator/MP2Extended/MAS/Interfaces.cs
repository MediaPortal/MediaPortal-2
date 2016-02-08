using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.MAS
{
  public interface IActors
  {
    IList<WebActor> Actors { get; set; }
  }

  public interface IGuestStars
  {
    IList<WebActor> GuestStars { get; set; }
  }

  public interface IArtwork
  {
    IList<WebArtwork> Artwork { get; set; }
  }
}