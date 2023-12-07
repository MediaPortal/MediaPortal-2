using System;
using System.IO;
using System.Net;
using Cinema.Models;

namespace Cinema.Helper
{
  internal class CachedImage
  {
    public string Url { get; set; }

    public string Name { get; set; }

    public string FullPath { get; set; }

    public CachedImage(string url, string id, string typ)
    {
      Url = url;
      Name = id + "-" + typ + url.Substring(url.LastIndexOf("."));
      FullPath = Path.Combine(CinemaHome.CachedImagesFolder, Name);
    }

    public void LoadImageFromWeb()
    {
      using (WebClient client = new WebClient())
      {
        client.DownloadFileAsync(new Uri(Url), FullPath);
      }
    }
  }
}
