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
using System.Threading;
using System.Timers;
using DirectShow;
using DirectShow.Helper;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Players.Video.Interfaces;
using Timer = System.Timers.Timer;

namespace MediaPortal.UI.Players.Video.Tools
{
  /// <summary>
  /// GraphRebuilder allows to rebuild a DirectShow graph after media types have changed. 
  /// </summary>
  public class GraphRebuilder
  {
    #region Fields

    private readonly IGraphBuilder _graphBuilder;
    private readonly IBaseFilter _fileSource;
    private readonly Action _afterRebuild;
    private bool _rebuilding = false;

    public delegate void GraphRebuiltCallback(ChangedMediaType changedMediaTypes);

    #endregion

    #region Properties

    public string PlayerName { get; set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a <see cref="GraphRebuilder"/> instance.
    /// </summary>
    /// <param name="graphBuilder">The IGraphBuilder that originally created the graph</param>
    /// <param name="fileSource">The input source filter (usually IFileSourceFilter)</param>
    /// <param name="afterRebuild">Action to be called after rebuild is done</param>
    public GraphRebuilder(IGraphBuilder graphBuilder, IBaseFilter fileSource, Action afterRebuild)
    {
      _graphBuilder = graphBuilder;
      _fileSource = fileSource;
      _afterRebuild = afterRebuild;
    }

    #endregion

    #region Members

    /// <summary>
    /// Executes the <see cref="DoGraphRebuild"/> asyncronously.
    /// </summary>
    public void DoAsynchRebuild()
    {
      if (_rebuilding)
        return;
      Timer timer = new Timer(1) { AutoReset = false };
      timer.Elapsed += AsynchRebuild;
      timer.Enabled = true;
    }

    void AsynchRebuild(object sender, ElapsedEventArgs e)
    {
      _rebuilding = true;
      DoGraphRebuild();
      if (_afterRebuild != null)
        _afterRebuild();

      _rebuilding = false;
    }

    /// <summary>
    /// DoGraphRebuild stops the graph and examines all Pin connections if they are still matching the new
    /// types. If all Pins are matching the types, they get reconnected. Otherwise all Pins are disconnected
    /// and new rendered. This can also cause new filters to be added dynamically to graph.
    /// </summary>
    public void DoGraphRebuild()
    {
      IMediaControl mediaCtrl = _graphBuilder as IMediaControl;
      ServiceRegistration.Get<ILogger>().Info(string.Format("{0}: DoGraphRebuild()", PlayerName));
      if (mediaCtrl == null)
        return;
      int hr = mediaCtrl.Stop();
      if (hr != 0)
        ServiceRegistration.Get<ILogger>().Error("Error stopping graph: ({0:x})", hr);

      for (; ; )
      {
        FilterState state;
        hr = mediaCtrl.GetState(200, out state);
        if (hr != 0)
          ServiceRegistration.Get<ILogger>().Info("GetState failed: {0:x}", hr);
        else if (state == FilterState.Stopped)
          break;

        ServiceRegistration.Get<ILogger>().Info(
          string.Format("{0}:OnMediaTypeChanged(): Graph not yet stopped, waiting some more.", PlayerName));
        mediaCtrl.Stop();
        Thread.Sleep(100);
      }

      ServiceRegistration.Get<ILogger>().Info("Graph stopped.");
      bool needRebuild = GraphNeedsRebuild(_fileSource);

      if (needRebuild)
      {
        ServiceRegistration.Get<ILogger>().Info("Doing full graph rebuild.");
        DisconnectAllPins(_graphBuilder, _fileSource);
        FilterGraphTools.RenderOutputPins(_graphBuilder, _fileSource);
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Info("Reconnecting all pins of base filter.");
        ReConnectAll(_graphBuilder, _fileSource);
      }
      mediaCtrl.Run();
      ServiceRegistration.Get<ILogger>().Info("Reconfigure graph done");
    }

    /// <summary>
    /// Checks if the pin connections can be kept, or if a graph rebuilding is necessary.
    /// </summary>
    /// <param name="baseFilter">Filter to check</param>
    /// <returns>True if graph needs to be rebuilt</returns>
    private static bool GraphNeedsRebuild(IBaseFilter baseFilter)
    {
      IEnumPins pinEnum;
      int hr = baseFilter.EnumPins(out pinEnum);
      if (hr != 0 || pinEnum == null)
        return true;
      IPin[] pins = new IPin[1];
      IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
      for (; ; )
      {
        hr = pinEnum.Next(1, pins, ptrFetched);
        if (hr != 0 || Marshal.ReadInt32(ptrFetched) == 0)
          break;
        try
        {
          IPin other = null;
          IntPtr other_ptr;
          if (pins[0].ConnectedTo(out other_ptr) == 0 && other_ptr != IntPtr.Zero)
          {
            try
            {
              other = Marshal.GetObjectForIUnknown(other_ptr) as IPin;
              PinInfo pinInfo;
              pins[0].QueryPinInfo(out pinInfo);
              FilterInfo filterInfo = FilterGraphTools.QueryFilterInfoAndFree(pinInfo.filter);
              try
              {
                if (!QueryConnect(pins[0], other))
                {
                  ServiceRegistration.Get<ILogger>().Info("Graph needs a rebuild. Filter: {0}, Pin: {1}", filterInfo.achName, pinInfo.name);
                  return true;
                }
              }
              finally
              {
                FilterGraphTools.FreePinInfo(pinInfo);
              }
            }
            finally
            {
              if (other != null && Marshal.IsComObject(other))
                Marshal.ReleaseComObject(other);
            }
          }
        }
        finally
        {
          Marshal.ReleaseComObject(pins[0]);
        }
      }
      Marshal.ReleaseComObject(pinEnum);
      Marshal.FreeCoTaskMem(ptrFetched);
      ServiceRegistration.Get<ILogger>().Info("Graph does not need a rebuild");
      return false;
    }

    /// <summary>
    /// QueryConnect checks if two Pins can be connected.
    /// </summary>
    /// <param name="pin">Pin 1</param>
    /// <param name="other">Pin 2</param>
    /// <returns>True if they accept connection</returns>
    static bool QueryConnect(IPin pin, IPin other)
    {
      var pin1 = new DSPin(pin);
      var pinOther = new DSPin(other);

      foreach (var mediaType in pin1.MediaTypes)
      {
        if (pinOther.IsAccepted(mediaType))
          return true;
      }

      PinInfo info;
      PinInfo infoOther;
      pin.QueryPinInfo(out info);
      other.QueryPinInfo(out infoOther);
      ServiceRegistration.Get<ILogger>().Info("Pins {0} and {1} do not accept each other. Tested {2} media types", info.name, infoOther.name, pin1.MediaTypes.Count);
      FilterGraphTools.FreePinInfo(info);
      FilterGraphTools.FreePinInfo(infoOther);
      return false;
    }

    /// <summary>
    /// Reconnects all filters in graph.
    /// </summary>
    /// <param name="graphBuilder">IGraphBuilder</param>
    /// <param name="filter">Current IBaseFilter in graph</param>
    static void ReConnectAll(IGraphBuilder graphBuilder, IBaseFilter filter)
    {
      IEnumPins pinEnum;
      FilterInfo info = FilterGraphTools.QueryFilterInfoAndFree(filter);
      IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
      int hr = filter.EnumPins(out pinEnum);
      if ((hr == 0) && (pinEnum != null))
      {
        ServiceRegistration.Get<ILogger>().Info("got pins");
        IPin[] pins = new IPin[1];
        int iFetched;
        int iPinNo = 0;
        do
        {
          // Get the next pin
          iPinNo++;
          hr = pinEnum.Next(1, pins, ptrFetched);

          // In case of error stop the pin enumeration
          if (hr != 0)
            break;

          iFetched = Marshal.ReadInt32(ptrFetched);
          if (iFetched == 1 && pins[0] != null)
          {
            PinInfo pinInfo;
            hr = pins[0].QueryPinInfo(out pinInfo);
            if (hr == 0)
            {
              ServiceRegistration.Get<ILogger>().Info("  got pin#{0}:{1}", iPinNo - 1, pinInfo.name);
              FilterGraphTools.FreePinInfo(pinInfo);
            }
            else
            {
              ServiceRegistration.Get<ILogger>().Info("  got pin:?");
            }
            PinDirection pinDir;
            pins[0].QueryDirection(out pinDir);
            if (pinDir == PinDirection.Output)
            {
              IntPtr other_ptr;
              hr = pins[0].ConnectedTo(out other_ptr);
              if (hr == 0 && other_ptr != IntPtr.Zero)
              {
                ServiceRegistration.Get<ILogger>().Info("Reconnecting {0}:{1}", info.achName, pinInfo.name);
                hr = graphBuilder.Reconnect(pins[0]);
                if (hr != 0)
                  ServiceRegistration.Get<ILogger>().Warn("Reconnect failed: {0}:{1}, code: 0x{2:x}", info.achName, pinInfo.name, hr);
                IPin other = Marshal.GetObjectForIUnknown(other_ptr) as IPin;
                PinInfo otherPinInfo;
                other.QueryPinInfo(out otherPinInfo);
                ReConnectAll(graphBuilder, otherPinInfo.filter);
                FilterGraphTools.FreePinInfo(otherPinInfo);
                Marshal.ReleaseComObject(other);
              }
            }
            Marshal.ReleaseComObject(pins[0]);
          }
          else
          {
            ServiceRegistration.Get<ILogger>().Info("no pins?");
            break;
          }
        }
        while (iFetched == 1);
        FilterGraphTools.TryRelease(ref pinEnum);
        Marshal.FreeCoTaskMem(ptrFetched);
      }
    }

    /// <summary>
    /// Disconnects all pins of all filters in graph.
    /// </summary>
    /// <param name="graphBuilder">IGraphBuilder</param>
    /// <param name="filter">Current IBaseFilter in graph</param>
    bool DisconnectAllPins(IGraphBuilder graphBuilder, IBaseFilter filter)
    {
      IEnumPins pinEnum;
      int hr = filter.EnumPins(out pinEnum);
      if (hr != 0 || pinEnum == null) return false;
      FilterInfo info = FilterGraphTools.QueryFilterInfoAndFree(filter);
      ServiceRegistration.Get<ILogger>().Info("Disconnecting all pins from filter {0}", info.achName);
      bool allDisconnected = true;
      IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
      for (; ; )
      {
        IPin[] pins = new IPin[1];

        hr = pinEnum.Next(1, pins, ptrFetched);
        if (hr != 0 || Marshal.ReadInt32(ptrFetched) == 0) break;
        PinInfo pinInfo;
        pins[0].QueryPinInfo(out pinInfo);
        if (pinInfo.dir == PinDirection.Output)
        {
          if (!DisconnectPin(graphBuilder, pins[0]))
            allDisconnected = false;
        }
        FilterGraphTools.FreePinInfo(pinInfo);
        Marshal.ReleaseComObject(pins[0]);
      }
      Marshal.ReleaseComObject(pinEnum);
      Marshal.FreeCoTaskMem(ptrFetched);
      return allDisconnected;
    }

    /// <summary>
    /// Disconnects a single Pin.
    /// </summary>
    /// <param name="graphBuilder">IGraphBuilder</param>
    /// <param name="pin">Pin to disconnect</param>
    /// <returns>True if successful</returns>
    bool DisconnectPin(IGraphBuilder graphBuilder, IPin pin)
    {
      IntPtr other_ptr;
      int hr = pin.ConnectedTo(out other_ptr);
      bool allDisconnected = true;
      if (hr == 0 && other_ptr != IntPtr.Zero)
      {
        IPin other = Marshal.GetObjectForIUnknown(other_ptr) as IPin;
        PinInfo info;
        pin.QueryPinInfo(out info);
        ServiceRegistration.Get<ILogger>().Info("Disconnecting pin {0}", info.name);
        FilterGraphTools.FreePinInfo(info);

        other.QueryPinInfo(out info);
        if (!DisconnectAllPins(graphBuilder, info.filter))
          allDisconnected = false;

        FilterGraphTools.FreePinInfo(info);

        hr = pin.Disconnect();
        if (hr != 0)
        {
          allDisconnected = false;
          ServiceRegistration.Get<ILogger>().Error("Error disconnecting: {0:x}", hr);
        }
        hr = other.Disconnect();
        if (hr != 0)
        {
          allDisconnected = false;
          ServiceRegistration.Get<ILogger>().Error("Error disconnecting other: {0:x}", hr);
        }
        Marshal.ReleaseComObject(other);
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Info("  Not connected");
      }
      return allDisconnected;
    }

    #endregion
  }
}
