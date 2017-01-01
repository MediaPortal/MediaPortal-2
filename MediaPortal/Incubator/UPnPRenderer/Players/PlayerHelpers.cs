#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

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
    public static void AddSourceFilterOverride(Action fallbackAction, IResourceAccessor resourceAccessor, IGraphBuilder graphBuilder, out FilterFileWrapper filterWrapper)
    {
      string sourceFilterName = GetSourceFilterName(resourceAccessor.ResourcePathName);
      filterWrapper = null;
      if (string.IsNullOrEmpty(sourceFilterName))
      {
        fallbackAction();
      }
      else
      {
        AddStreamSourceFilter(sourceFilterName, resourceAccessor, graphBuilder, out filterWrapper);
      }
    }

    public static void AddStreamSourceFilter(string sourceFilterName, IResourceAccessor resourceAccessor, IGraphBuilder graphBuilder, out FilterFileWrapper filterWrapper)
    {
      filterWrapper = null;
      IBaseFilter sourceFilter = null;
      try
      {
        if (sourceFilterName == Utils.FilterName)
        {
          var filterPath = FileUtils.BuildAssemblyRelativePath(@"MPUrlSourceSplitter\MPUrlSourceSplitter.ax");
          filterWrapper = FilterLoader.LoadFilterFromDll(filterPath, new Guid(Utils.FilterCLSID));
          sourceFilter = filterWrapper.GetFilter();
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
