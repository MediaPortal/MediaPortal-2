using System.IO;
using System.Runtime.InteropServices;
using DirectShow;
using MediaPortal.Extensions.UPnPRenderer.MediaItems;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;
using System;
using UPnPRenderer.UPnP;

namespace MediaPortal.Extensions.UPnPRenderer.Players
{
    public class UPnPRendererAudioPlayer : BaseDXPlayer, IAudioPlayer, IReusablePlayer
    {
      public const string MIMETYPE = "upnpaudio/upnprenderer";
      public const string DUMMY_FILE = "UPnPRenderer://localhost/UPnPRendererAudio.upnp";


      protected override void AddSourceFilter()
       {
         string sourceFilterName = GetSourceFilterName(_resourceAccessor.ResourcePathName);
         if (!string.IsNullOrEmpty(sourceFilterName))
         {
           IBaseFilter sourceFilter = null;
           try
           {
             if (sourceFilterName == utils.FilterName)
             {
               sourceFilter = FilterLoader.LoadFilterFromDll(
                   Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), @"MPUrlSourceSplitter\MPUrlSourceSplitter.ax"),
                   new Guid(utils.FilterCLSID), false);
               if (sourceFilter != null)
               {
                 _graphBuilder.AddFilter(sourceFilter, utils.FilterName);
               }
             }
             else
             {
               sourceFilter = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, sourceFilterName);
             }

             if (sourceFilter == null)
               throw new UPnPRendererExceptions(string.Format("Could not create instance of source filter: '{0}'", sourceFilterName));

             var filterStateEx = sourceFilter as OnlineVideos.MPUrlSourceFilter.IFilterStateEx;
             if (filterStateEx != null)
               LoadAndWaitForMPUrlSourceFilter(filterStateEx);
             else
             {
               var fileSourceFilter = sourceFilter as IFileSourceFilter;
               if (fileSourceFilter != null)
                 Marshal.ThrowExceptionForHR(fileSourceFilter.Load(_resourceAccessor.ResourcePathName, null));
               else
                 throw new UPnPRendererExceptions(string.Format("'{0}' does not implement IFileSourceFilter", sourceFilterName));
             }

             FilterGraphTools.RenderOutputPins(_graphBuilder, sourceFilter);
           }
           finally
           {
             FilterGraphTools.TryRelease(ref sourceFilter);
           }
         }
         else
         {
           base.AddSourceFilter();
         }
       }

       void LoadAndWaitForMPUrlSourceFilter(OnlineVideos.MPUrlSourceFilter.IFilterStateEx filterStateEx)
       {
         //string url = ApplyMPUrlSourceFilterSiteUserSettings(_resourceAccessor.ResourcePathName);
         string url = _resourceAccessor.ResourcePathName;
         int result = filterStateEx.LoadAsync(url);
         if (result < 0)
           throw new UPnPRendererExceptions("Loading URL async error: " + result);

         bool opened = false;
         while (!opened)
         {
           System.Threading.Thread.Sleep(1);
           result = filterStateEx.IsStreamOpened(out opened);
           if (result < 0)
             throw new UPnPRendererExceptions("Check stream open error: " + result);
         }

         bool ready = false;
         while (!ready)
         {
           System.Threading.Thread.Sleep(50);
           result = filterStateEx.IsFilterReadyToConnectPins(out ready);
           if (result != 0)
             throw new UPnPRendererExceptions("IsFilterReadyToConnectPins error: " + result);
         }
       }

       static string GetSourceFilterName(string url)
       {
         string sourceFilterName = null;
         Uri uri = new Uri(url);
         string protocol = uri.Scheme.Substring(0, Math.Min(uri.Scheme.Length, 4));
         switch (protocol)
         {
           case "http":
             sourceFilterName = url.ToLower().Contains(".asf") ?
                 "WM ASF Reader" : utils.FilterName;
             break;
           case "rtmp":
             sourceFilterName = utils.FilterName;
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

      /////////////////////////////


        /*PlayerSettings settings;

        object timestampSync = new object();
        double sampleRate;
        uint startStamp;
        uint stopStamp;
        TimeSpan currentDuration;

        public UPnPRendererAudioPlayer(PlayerSettings settings)
        {
            this.settings = settings;
            sampleRate = (double)settings.Source.SampleRate;
        }

        protected override void AddSourceFilter()
        {
            var sourceFilter = new GenericPushSourceFilter(settings.Source, settings.GetMediaType());
            int hr = _graphBuilder.AddFilter(sourceFilter, sourceFilter.Name);
            new HRESULT(hr).Throw();
            DSFilter source2 = new DSFilter(sourceFilter);
            hr = source2.OutputPin.Render();
            new HRESULT(hr).Throw();
            return;
        }*/

        public override string Name
        {
            get { return "UPnPRenderer Audio Player"; }
        }

        /*public void UpdateDurationInfo(uint startStamp, uint stopStamp)
        {
            lock (timestampSync)
            {
                this.startStamp = startStamp;
                this.stopStamp = stopStamp;
                currentDuration = TimeSpan.FromSeconds((stopStamp - startStamp) / sampleRate);
            }
        }

        public override TimeSpan CurrentTime
        {
            get
            {
                lock (timestampSync)
                {
                    if (currentDuration != null)
                    {
                        double currentTime = (settings.GetLastTimeStamp() - startStamp) / sampleRate;
                        if (currentTime <= currentDuration.TotalSeconds)
                            return TimeSpan.FromSeconds(currentTime);
                    }
                }
                return TimeSpan.Zero;
            }
            set { }
        }

        public override TimeSpan Duration
        {
            get
            {
                lock (timestampSync)
                {
                    if (currentDuration != null)
                        return currentDuration;
                }
                return TimeSpan.Zero;
            }
        }*/

        public bool NextItem(MediaPortal.Common.MediaManagement.MediaItem mediaItem, StartTime startTime)
        {
            AudioItem audioItem = mediaItem as AudioItem;
            return audioItem != null/* && audioItem.PlayerSettings == null*/;
        }

        public event RequestNextItemDlgt NextItemRequest;
    }
}
