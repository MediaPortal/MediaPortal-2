using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Webradio.OnlineLibraries.Helper;
using Webradio.OnlineLibraries.MusicBrainz;
using Webradio.OnlineLibraries.RadioNet;

namespace Webradio.OnlineLibraries
{
  public class TrackInfo
  {
    public List<string> ArtistBackgrounds { get; set; } = new List<string>();
    public string FrontCover { get; set; }
    public string Biography { get; set; }

    private List<string> _releaseIds;
    private string _artistId;
    private string _mBArtist;

    public TrackInfo()
    {
    }

    public TrackInfo(string artist, string title)
    {
      SetMbIds(artist, title);
      foreach (var relId in _releaseIds)
      {
        var fc = CoverUrl(relId);
        if (fc != "")
        {
          FrontCover = fc;
          break;
        }
      }

      ArtistBackgrounds = Backgrounds(_artistId);

      //if(_mBArtist != null)
      //  Biography = GetBiography(_mBArtist);
    }

    private void SetMbIds(string artist, string title)
    {
      _releaseIds = new List<string>();
      string url = "https://musicbrainz.org/ws/2/recording?query=" + title + " AND artist:\"" + artist + "\"&fmt=json";

      try
      {
        var resp = Http.Request(url);
        if (resp != "")
        {
          var jsn = Json.Deserialize<MbRecording>(resp);
          if (jsn != null)
          {
            if (jsn.Recordings != null)
            {
              foreach (var rec in jsn.Recordings)
              {
                if (_artistId == null)
                {
                  if (rec.ArtistCredit != null)
                  {
                    if (rec.ArtistCredit.First().Artist != null)
                    {
                      if (rec.ArtistCredit.First().Artist.Id != null)
                      {
                        _mBArtist = rec.ArtistCredit.First().Artist.Name;
                        _artistId = rec.ArtistCredit.First().Artist.Id;
                      }
                    }
                  }
                }

                if (rec.Releases != null)
                {
                  foreach (var rel in rec.Releases)
                  {
                    if (!_releaseIds.Contains(rel.Id))
                      _releaseIds.Add(rel.Id);
                  }
                }
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }
    }

    private string CoverUrl(string mbid)
    {
      string cUrl = "";
      string url = "http://www.coverartarchive.org/release/" + mbid;

      try
      {
        var resp = Http.Request(url);
        if (resp != "")
        {
          var jsn = Json.Deserialize<CaRelease>(resp);
          if (jsn != null)
          {
            if (jsn.Images != null)
            {
              string cu = "";
              if (jsn.Images.First().Image != null)
              {
                cu = jsn.Images.First().Image;
              }

              foreach (var img in jsn.Images)
              {
                if (img.Front)
                {
                  cu = img.Image;
                  break;
                }
              }

              if (cu != "")
                cUrl = Http.GetRedirectedUrl(cu);
            }
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }

      return cUrl;
    }

    private string GetBiography(string artist)
    {
      string bio = "";

      var id = GetDiscogsId(artist);
      if (id == "") return bio;
      string url = "https://api.discogs.com/artists/" + id + "?key=lvGHliBqYzdOLarMLnOx&secret=VxSChxKcJmeSPiZeMXSBuCEyZtuwxoyB";

      try
      {
        var resp = Http.Request(url);
        if (resp != "")
        {
          var jsn = Json.Deserialize<Webradio.OnlineLibraries.Discogs.Artist>(resp);
          if (jsn != null)
          {
            if (jsn.Profile != null)
            {
              bio = jsn.Profile;
            }
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        return bio;
      }

      return bio;
    }

    private string GetDiscogsId(string artist)
    {
      string id = "";
      string url = "https://api.discogs.com/database/search?typ=artist&q=" + artist + "&key=lvGHliBqYzdOLarMLnOx&secret=VxSChxKcJmeSPiZeMXSBuCEyZtuwxoyB&per_page=1&page=1";

      try
      {
        var resp = Http.Request(url);
        if (resp != "")
        {
          var jsn = Json.Deserialize<Webradio.OnlineLibraries.Discogs.Query>(resp);
          if (jsn != null)
          {
            if (jsn.Results != null)
            {
              if (jsn.Results.First() != null)
              {
                if (jsn.Results.First().Id != null)
                {
                  id = jsn.Results.First().Id.ToString();
                }
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        return id;
      }

      return id;
    }

    private List<string> Backgrounds(string artistid)
    {
      List<string> images = new List<string>();
      string url = "http://webservice.fanart.tv/v3/music/albums/" + artistid + "?api_key=5c83b254980c5139932c4608cb224eeb";

      try
      {
        var resp = Http.Request(url);
        if (resp != "")
        {
          var jsn = Json.Deserialize<FanartTv.Artist>(resp);
          if (jsn != null)
          {
            if (jsn.Artistbackground != null)
            {
              foreach (var ab in jsn.Artistbackground)
              {
                images.Add(ab.Url);
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }

      return images;
    }
  }

  public class PodcastInfo
  {



    public PodcastInfo(string url)
    {
      var js = GetSiteJson(url);
      var pc = Json.Deserialize<Podcast>(js);


    }



    private string GetSiteJson(string url)
    {
      var ret = Http.Request(url);
      if (Http.StatusCode != HttpStatusCode.OK)
      {
        ret = Http.Request(url);
      }

      return ret.Substring("type=\"application/json\">", "<");
    }
  }

  internal static class StringExtensions
  {
    public static string Substring(this string value, string start, string ende)
    {
      try
      {
        var a = value.IndexOf(start) + start.Length;
        if (a - start.Length <= 0) return "";
        var b = value.IndexOf(ende, a, StringComparison.Ordinal);
        return value.Substring(a, b - a);
      }
      catch (Exception)
      {
        return "";
      }
    }

    public static string Substring(this string value, string start, string start2, string ende)
    {
      try
      {
        var a = value.IndexOf(start) + start.Length;
        var a2 = value.IndexOf(start2, a, StringComparison.Ordinal) + start2.Length;
        var b = value.IndexOf(ende, a2, StringComparison.Ordinal);
        return value.Substring(a2, b - a2);
      }
      catch (Exception)
      {
        return "";
      }
    }
  }
}
