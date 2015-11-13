using System;
using System.Runtime.InteropServices;
using DirectShow;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UPnPRenderer.UPnP;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.UPnPRenderer.Players
{
  public static class PlayerHelpers
  {
    public static void AddSourceFilterOverride(Action fallbackAction, IResourceAccessor resourceAccessor, IGraphBuilder graphBuilder)
    {
      string sourceFilterName = GetSourceFilterName(resourceAccessor.ResourcePathName);
      if (string.IsNullOrEmpty(sourceFilterName))
      {
        fallbackAction();
      }
      else
      {
        AddStreamSourceFilter(sourceFilterName, resourceAccessor, graphBuilder);
      }
    }

    public static void AddStreamSourceFilter(string sourceFilterName, IResourceAccessor resourceAccessor, IGraphBuilder graphBuilder)
    {
      IBaseFilter sourceFilter = null;
      try
      {
        if (sourceFilterName == Utils.FilterName)
        {
          var filterPath = FileUtils.BuildAssemblyRelativePath(@"MPUrlSourceSplitter\MPUrlSourceSplitter.ax");
          sourceFilter = FilterLoader.LoadFilterFromDll(filterPath, new Guid(Utils.FilterCLSID));
          if (sourceFilter != null)
          {
            graphBuilder.AddFilter(sourceFilter, Utils.FilterName);
          }
        }
        else
        {
          sourceFilter = FilterGraphTools.AddFilterByName(graphBuilder, FilterCategory.LegacyAmFilterCategory, sourceFilterName);
        }

        if (sourceFilter == null)
          throw new UPnPRendererExceptions(string.Format("Could not create instance of source filter: '{0}'", sourceFilterName));

        string url = resourceAccessor.ResourcePathName;

        var filterStateEx = sourceFilter as OnlineVideos.MPUrlSourceFilter.IFilterStateEx;
        if (filterStateEx != null)
          LoadAndWaitForMPUrlSourceFilter(url, filterStateEx);
        else
        {
          var fileSourceFilter = sourceFilter as IFileSourceFilter;
          if (fileSourceFilter != null)
            Marshal.ThrowExceptionForHR(fileSourceFilter.Load(resourceAccessor.ResourcePathName, null));
          else
            throw new UPnPRendererExceptions(string.Format("'{0}' does not implement IFileSourceFilter", sourceFilterName));
        }

        FilterGraphTools.RenderOutputPins(graphBuilder, sourceFilter);
      }
      finally
      {
        FilterGraphTools.TryRelease(ref sourceFilter);
      }
    }

    public static void LoadAndWaitForMPUrlSourceFilter(string url, OnlineVideos.MPUrlSourceFilter.IFilterStateEx filterStateEx)
    {
      //string url = ApplyMPUrlSourceFilterSiteUserSettings(_resourceAccessor.ResourcePathName);
      int result = filterStateEx.LoadAsync(url);
      if (result < 0)
        throw new UPnPRendererExceptions("Loading URL async error:  {0}", result);

      WaitUntilReady(filterStateEx.IsStreamOpened, 1, "Check stream open error");
      WaitUntilReady(filterStateEx.IsFilterReadyToConnectPins, 50, "IsFilterReadyToConnectPins error");
    }

    public delegate int ExecDelegate(out bool isDone);
    public static void WaitUntilReady(ExecDelegate a, int sleepMs, string exceptionMessage)
    {
      bool isDone = false;
      while (!isDone)
      {
        System.Threading.Thread.Sleep(sleepMs);
        int result = a(out isDone);
        if (result < 0)
          throw new UPnPRendererExceptions(string.Format("{0}: {1}", exceptionMessage, result));
      }
    }

    public static string GetSourceFilterName(string url)
    {
      string sourceFilterName = null;
      Uri uri = new Uri(url);
      string protocol = uri.Scheme.Substring(0, Math.Min(uri.Scheme.Length, 4));
      switch (protocol)
      {
        case "http":
          sourceFilterName = url.ToLower().Contains(".asf") ? "WM ASF Reader" : Utils.FilterName;
          break;
        case "rtmp":
          sourceFilterName = Utils.FilterName;
          break;
        case "sop":
          sourceFilterName = "SopCast ASF Splitter";
          break;
        case "mms":
          sourceFilterName = "WM ASF Reader";
          break;
      }
      return sourceFilterName;
    }
  }
}
