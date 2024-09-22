using DirectShow;
using MediaPortal.UI.Players.MPUrlSource.Interfaces;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Tools;
using System;
using System.IO;

namespace MediaPortal.UI.Players.MPUrlSource
{
  public class MPUrlSourcePlayer : VideoPlayer
  {
    public const string FilterName = "MediaPortal Url Source Splitter";
    public const string FilterCLSID = "59ED045A-A938-4A09-A8A6-8231F5834259";
    private FilterFileWrapper _filterWrapper;

    protected override void AddSourceFilter()
    {
      IBaseFilter sourceFilter = null;
      try
      {
        // TODO Morpheus_xx, 2020-01-25: Support x64 build here (x86/x64 subfolders) once there is a x64 build of MPUrlSourceSplitter!
        _filterWrapper = FilterLoader.LoadFilterFromDll(
            Path.Combine(Path.GetDirectoryName(typeof(MPUrlSourcePlayer).Assembly.Location), @"MPUrlSourceSplitter\MPUrlSourceSplitter.ax"),
            new Guid(FilterCLSID));
        sourceFilter = _filterWrapper.GetFilter();
        if (sourceFilter != null)
          _graphBuilder.AddFilter(sourceFilter, FilterName);
        else
          throw new VideoPlayerException("Could not create instance of source filter: '{0}'", FilterName);

        var filterStateEx = sourceFilter as IFilterStateEx;
        LoadAndWaitForMPUrlSourceFilter(filterStateEx);

        FilterGraphTools.RenderOutputPins(_graphBuilder, sourceFilter);
      }
      finally
      {
        FilterGraphTools.TryRelease(ref sourceFilter);
      }
    }

    protected override void FreeCodecs()
    {
      base.FreeCodecs();
      FilterGraphTools.TryDispose(ref _filterWrapper);
    }

    void LoadAndWaitForMPUrlSourceFilter(IFilterStateEx filterStateEx)
    {
      string url = GetMPUrlSourceFilterUrl(_resourceAccessor.ResourcePathName);
      int result = filterStateEx.LoadAsync(url);
      if (result < 0)
        throw new VideoPlayerException("Loading URL async error: " + result);

      bool opened = false;
      while (!opened)
      {
        System.Threading.Thread.Sleep(1);
        result = filterStateEx.IsStreamOpened(out opened);
        if (result < 0)
          throw new VideoPlayerException("Check stream open error: " + result);
      }

      bool ready = false;
      while (!ready)
      {
        System.Threading.Thread.Sleep(50);
        result = filterStateEx.IsFilterReadyToConnectPins(out ready);
        if (result != 0)
          throw new VideoPlayerException("IsFilterReadyToConnectPins error: " + result);
      }
    }

    protected virtual string GetMPUrlSourceFilterUrl(string url)
    {
      return url;
    }
  }
}
