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

using DirectShow;
using DirectShow.Helper;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using MediaPortal.Plugins.Transcoding.Interfaces.MetaData;
using MediaPortal.Utilities.Exceptions;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MediaPortal.Plugins.Transcoding.Interfaces.SlimTv
{
  public class TranscodeChannel : IDisposable
  {
    private ConcurrentDictionary<string, Stream> _clients = new ConcurrentDictionary<string, Stream>();
    private IResourceAccessor _tvStreamRA = null;
    private MediaItem _metaData = null;
    private IFilterGraph2 _pGraph = null;
    private FilterFileWrapper _sourceFilter = null;
    private IBaseFilter _sampleGrabberFilter = null;
    private bool _streaming = false;

    public ConcurrentDictionary<string, Stream> Clients
    {
      get => _clients;
    }
    public MediaItem MetaData
    {
      get => _metaData;
    }

    #region Imports

    [ComImport, Guid(TSREADER_CLSID)]
    protected class TsReader { }

    [ComImport, Guid(QEDIT_CLSID)]
    protected class Qedit { }

    #endregion

    #region Constants and structs

    public const string QEDIT_CLSID = "C1F400A0-3F08-11D3-9F0B-006008039E37";
    public const string TSREADER_CLSID = "b9559486-E1BB-45D3-A2A2-9A7AFE49B23F";

    #endregion

    #region Graph building

    public void SetChannel(MediaItem mediaItem)
    {
      _metaData = mediaItem;

      var resourcePath = ResourcePath.Deserialize(mediaItem.PrimaryProviderResourcePath());
      _tvStreamRA = SlimTvResourceProvider.GetResourceAccessor(resourcePath.BasePathSegment.Path);
    }

    public void Dispose()
    {
      StopStreaming();

      // Free sample filter
      FilterGraphTools.TryRelease(ref _sampleGrabberFilter);

      // Free locally mounted remote resources
      FilterGraphTools.TryDispose(ref _tvStreamRA);

      // Free file source
      FilterGraphTools.TryDispose(ref _sourceFilter);
    }

    public void StopStreaming()
    {
      if (!_streaming)
        return;

      _pGraph.Abort();
      foreach (var stream in _clients.Where(c => c.Value != null).Select(c => c.Value))
        stream.Dispose();
      _streaming = false;
    }

    public void StartStreaming()
    {
      IFilterGraph2 pGraph = (IFilterGraph2)new FilterGraph();

      int hr = 0;
      // Graph builder
      ICaptureGraphBuilder2 pBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();
      hr = pBuilder.SetFiltergraph(pGraph);
      new HRESULT(hr).Throw();

      // Add ts render
      _sourceFilter = FilterLoader.LoadFilterFromDll("TsReader.ax", typeof(TsReader).GUID, true);
      var baseFilter = _sourceFilter.GetFilter();
      IFileSourceFilter fileSourceFilter = (IFileSourceFilter)baseFilter;
      ITsReader _tsReader = (ITsReader)baseFilter;
      _tsReader.SetRelaxedMode(1);
      hr = pGraph.AddFilter(baseFilter, "TsReader");
      new HRESULT(hr).Throw();

      // Add sample grabber
      _sampleGrabberFilter = FilterGraphTools.AddFilterFromClsid(pGraph, typeof(Qedit).GUID, "SampleGrabber");

      // Set callback
      SampleGrabberCallback callback = new SampleGrabberCallback();
      callback.OnSample += Callback_OnSample;
      hr = ((ISampleGrabber)_sampleGrabberFilter).SetCallback(callback, 0);
      new HRESULT(hr).Throw();

      // Connect source and SampleGrabber
      FilterGraphTools.ConnectFilters(pGraph, baseFilter, "Output", _sampleGrabberFilter, "Input", true);

      if (_tvStreamRA.CanonicalLocalResourcePath.IsNetworkResource)
      {
        // _resourceAccessor points to an rtsp:// stream or network file
        var sourcePathOrUrl = ((INetworkResourceAccessor)_tvStreamRA).URL;

        if (sourcePathOrUrl == null)
          throw new IllegalCallException("The DlnaVideoPlayer can only play network resources of type INetworkResourceAccessor");

        ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for stream '{1}'", "DlnaVideoPlayer", sourcePathOrUrl);

        hr = fileSourceFilter.Load(sourcePathOrUrl, null);
        new HRESULT(hr).Throw();
      }
      else
      {
        // _resourceAccessor points to a local or remote mapped .ts file
        var localFileSystemResourceAccessor = ((ILocalFsResourceAccessor)_tvStreamRA);

        if (localFileSystemResourceAccessor == null)
          throw new IllegalCallException("The DlnaVideoPlayer can only play file resources of type ILocalFsResourceAccessor");

        using (localFileSystemResourceAccessor.EnsureLocalFileSystemAccess())
        {
          ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for stream '{1}'", "DlnaVideoPlayer", localFileSystemResourceAccessor.LocalFileSystemPath);
          hr = fileSourceFilter.Load(localFileSystemResourceAccessor.LocalFileSystemPath, null);
          new HRESULT(hr).Throw();
        }
      }

      // Render the video
      hr = pBuilder.RenderStream(null, null, _sampleGrabberFilter, null, null);
      new HRESULT(hr).Throw();
      _streaming = true;
    }

    private void Callback_OnSample(byte[] buffer)
    {
      try
      {
        if (_clients.Keys.Count > 0)
        {
          foreach (var stream in _clients.Where(c => c.Value != null).Select(c => c.Value))
          {
            if (stream.CanWrite)
              stream.Write(buffer, 0, buffer.Length);
          }
        }
      }
      catch { }
    }

    public class SampleGrabberCallback : ISampleGrabberCB
    {
      #region Delegates

      public delegate void SampleEventHandler(byte[] buffer);

      #endregion

      #region Events

      public event SampleEventHandler OnSample = null;

      #endregion

      public SampleGrabberCallback()
      {
      }

      public int BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
      {
        return 0;
      }

      public int SampleCB(double SampleTime, IMediaSample pSample)
      {
        try
        {
          if (pSample == null) return -1;
          int len = pSample.GetActualDataLength();
          IntPtr pbuf;
          if (pSample.GetPointer(out pbuf) == 0 && len > 0)
          {
            byte[] buffer = new byte[len];
            Marshal.Copy(pbuf, buffer, 0, len);
            OnSample?.Invoke(buffer);
          }
          Marshal.ReleaseComObject(pSample);
        }
        catch { }
        return 0;
      }

      public int SampleCB(double sampleTime, IntPtr sample)
      {
        throw new NotImplementedException();
      }
    }

    #endregion
  }
}
