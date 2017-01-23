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

/****************************************************************************
While the underlying libraries are covered by LGPL, this sample is released 
as public domain.  It is distributed in the hope that it will be useful, but 
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
or FITNESS FOR A PARTICULAR PURPOSE.  
*****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Permissions;
using DirectShow;
using DirectShow.Helper;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace MediaPortal.UI.Players.Video.Tools
{
  /// <summary>
  /// A collection of methods to do common DirectShow tasks.
  /// </summary>
  public static class FilterGraphTools
  {
    /// <summary>
    /// Add a filter to a DirectShow Graph using its CLSID
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <param name="clsid">a valid CLSID. This object must implement IBaseFilter</param>
    /// <param name="name">the name used in the graph (may be null)</param>
    /// <returns>an instance of the filter if the method successfully created it, null if not</returns>
    /// <remarks>
    /// You can use <see cref="IsThisComObjectInstalled">IsThisComObjectInstalled</see> to check is the CLSID is valid before calling this method
    /// </remarks>
    /// <example>This sample shows how to programmatically add a NVIDIA Video decoder filter to a graph
    /// <code>
    /// Guid nvidiaVideoDecClsid = new Guid("71E4616A-DB5E-452B-8CA5-71D9CC7805E9");
    /// 
    /// if (FilterGraphTools.IsThisComObjectInstalled(nvidiaVideoDecClsid))
    /// {
    ///   filter = FilterGraphTools.AddFilterFromClsid(graphBuilder, nvidiaVideoDecClsid, "NVIDIA Video Decoder");
    /// }
    /// else
    /// {
    ///   // use another filter...
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IsThisComObjectInstalled"/>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder is null</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">Thrown if errors occur when the filter is add to the graph</exception>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static IBaseFilter AddFilterFromClsid(IGraphBuilder graphBuilder, Guid clsid, string name)
    {
      IBaseFilter filter = null;

      if (graphBuilder == null)
      {
        throw new ArgumentNullException("graphBuilder");
      }

      try
      {
        Type type = Type.GetTypeFromCLSID(clsid);
        filter = (IBaseFilter)Activator.CreateInstance(type);

        int hr = graphBuilder.AddFilter(filter, name);
        new HRESULT(hr).Throw();
      }
      catch
      {
        if (filter != null)
        {
          Marshal.ReleaseComObject(filter);
          filter = null;
        }
      }

      return filter;
    }

    /// <summary>
    /// Add a filter to a DirectShow Graph using its name
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <param name="deviceCategory">the filter category (see <see cref="FilterCategory"/>)</param>
    /// <param name="friendlyName">the filter name (case-sensitive)</param>
    /// <returns>an instance of the filter if the method successfully created it, null if not</returns>
    /// <example>This sample shows how to programmatically add a NVIDIA Video decoder filter to a graph
    /// <code>
    /// filter = FilterGraphTools.AddFilterByName(graphBuilder, FilterCategory.LegacyAmFilterCategory, "NVIDIA Video Decoder");
    /// </code>
    /// </example>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder is null</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">Thrown if errors occur when the filter is add to the graph</exception>
    public static IBaseFilter AddFilterByName(IGraphBuilder graphBuilder, Guid deviceCategory, string friendlyName)
    {
      IBaseFilter filter = null;

      if (graphBuilder == null)
        return null; // throw new ArgumentNullException("graphBuilder");

      DSDevice dsDevice;
      using (DSCategory dsCategory = new DSCategory(deviceCategory))
        dsDevice = dsCategory.FirstOrDefault(d => String.Compare(d.Name, friendlyName, StringComparison.OrdinalIgnoreCase) == 0);

      if (dsDevice == null)
        return null;

      int hr = ((IFilterGraph2)graphBuilder).AddSourceFilterForMoniker(dsDevice.Value, null, friendlyName, out filter);
      if (hr != 0 || filter == null)
        return null; //new HRESULT(hr).Throw();

      return filter;
    }

    /// <summary>
    /// Add a filter to a DirectShow Graph using its Moniker's device path
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <param name="devicePath">a moniker path</param>
    /// <param name="name">the name to use for the filter in the graph</param>
    /// <returns>an instance of the filter if the method successfully creates it, null if not</returns>
    /// <example>This sample shows how to programmatically add a NVIDIA Video decoder filter to a graph
    /// <code>
    /// string devicePath = @"@device:sw:{083863F1-70DE-11D0-BD40-00A0C911CE86}\{71E4616A-DB5E-452B-8CA5-71D9CC7805E9}";
    /// filter = FilterGraphTools.AddFilterByDevicePath(graphBuilder, devicePath, "NVIDIA Video Decoder");
    /// </code>
    /// </example>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder is null</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">Thrown if errors occur when the filter is add to the graph</exception>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static IBaseFilter AddFilterByDevicePath(IGraphBuilder graphBuilder, string devicePath, string name)
    {
      IBaseFilter filter = null;
#if USING_NET11
			UCOMIBindCtx bindCtx = null;
			UCOMIMoniker moniker = null;
#else
      IBindCtx bindCtx = null;
      IMoniker moniker = null;
#endif

      if (graphBuilder == null)
      {
        throw new ArgumentNullException("graphBuilder");
      }

      try
      {
        int hr = NativeMethods.CreateBindCtx(0, out bindCtx);
        Marshal.ThrowExceptionForHR(hr);

        int eaten;
        hr = NativeMethods.MkParseDisplayName(bindCtx, devicePath, out eaten, out moniker);
        Marshal.ThrowExceptionForHR(hr);

        hr = ((IFilterGraph2)graphBuilder).AddSourceFilterForMoniker(moniker, bindCtx, name, out filter);
        new HRESULT(hr).Throw();
      }
      catch
      {
        // An error occur. Just returning null...
      }
      finally
      {
        if (bindCtx != null)
        {
          Marshal.ReleaseComObject(bindCtx);
        }
        if (moniker != null)
        {
          Marshal.ReleaseComObject(moniker);
        }
      }

      return filter;
    }

    /// <summary>
    /// Find a filter in a DirectShow Graph using its name
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <param name="filterName">the filter name to find (case-sensitive)</param>
    /// <returns>an instance of the filter if found, null if not</returns>
    /// <seealso cref="FindFilterByClsid"/>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder is null</exception>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static IBaseFilter FindFilterByName(IGraphBuilder graphBuilder, string filterName)
    {
      IBaseFilter filter = null;
      IEnumFilters enumFilters;

      if (graphBuilder == null)
      {
        throw new ArgumentNullException("graphBuilder");
      }

      int hr = graphBuilder.EnumFilters(out enumFilters);
      if (hr == 0)
      {
        IBaseFilter[] filters = new IBaseFilter[1];
        IntPtr fetched = Marshal.AllocCoTaskMem(4);
        while (enumFilters.Next(filters.Length, filters, fetched) == 0)
        {
          FilterInfo filterInfo;

          hr = filters[0].QueryFilterInfo(out filterInfo);
          if (hr == 0)
          {
            if (filterInfo.pGraph != null)
            {
              Marshal.ReleaseComObject(filterInfo.pGraph);
            }

            if (filterInfo.achName.Equals(filterName))
            {
              filter = filters[0];
              break;
            }
          }

          Marshal.ReleaseComObject(filters[0]);
          Marshal.FreeCoTaskMem(fetched);
        }
        Marshal.ReleaseComObject(enumFilters);
      }

      return filter;
    }

    /// <summary>
    /// Find a filter in a DirectShow Graph using its CLSID
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <param name="filterClsid">the CLSID to find</param>
    /// <returns>an instance of the filter if found, null if not</returns>
    /// <seealso cref="FindFilterByName"/>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder is null</exception>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static IBaseFilter FindFilterByClsid(IGraphBuilder graphBuilder, Guid filterClsid)
    {
      IBaseFilter filter = null;
      IEnumFilters enumFilters;

      if (graphBuilder == null)
      {
        throw new ArgumentNullException("graphBuilder");
      }

      int hr = graphBuilder.EnumFilters(out enumFilters);
      if (hr == 0)
      {
        IBaseFilter[] filters = new IBaseFilter[1];
        IntPtr fetched = Marshal.AllocCoTaskMem(4);

        while (enumFilters.Next(filters.Length, filters, fetched) == 0)
        {
          Guid clsid;

          hr = filters[0].GetClassID(out clsid);

          if ((hr == 0) && (clsid == filterClsid))
          {
            filter = filters[0];
            break;
          }

          Marshal.ReleaseComObject(filters[0]);
        }
        Marshal.ReleaseComObject(enumFilters);
        Marshal.FreeCoTaskMem(fetched);
      }

      return filter;
    }

    /// <summary>
    /// Find a filter in a DirectShow Graph that implements a given interface. The caller must call <see cref="Marshal.ReleaseComObject"/> on
    /// the returned instance when it is no longer needed.
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <returns>an instance of the filter if found, null if not</returns>
    /// <seealso cref="FindFilterByName"/>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder is null</exception>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static TE FindFilterByInterface<TE>(IGraphBuilder graphBuilder)
    {
      IBaseFilter filter = null;
      IEnumFilters enumFilters;

      if (graphBuilder == null)
      {
        throw new ArgumentNullException("graphBuilder");
      }

      int hr = graphBuilder.EnumFilters(out enumFilters);
      if (hr == 0)
      {
        IBaseFilter[] filters = new IBaseFilter[1];
        IntPtr fetched = Marshal.AllocCoTaskMem(4);

        while (enumFilters.Next(filters.Length, filters, fetched) == 0)
        {
          if (filters[0] is TE)
          {
            filter = filters[0];
            break;
          }
          if (Marshal.IsComObject(filters[0]))
            Marshal.ReleaseComObject(filters[0]);
        }
        Marshal.ReleaseComObject(enumFilters);
        Marshal.FreeCoTaskMem(fetched);
      }
      return (TE)filter;
    }

    /// <summary>
    /// Find all filter in a DirectShow Graph that implement a given interface. The caller must call <see cref="Marshal.ReleaseComObject"/> on
    /// all returned instances when they are no longer needed.
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <returns>an instance of the filter if found, null if not</returns>
    /// <seealso cref="FindFilterByName"/>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder is null</exception>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static List<TE> FindFiltersByInterface<TE>(IGraphBuilder graphBuilder)
    {
      if (graphBuilder == null)
        throw new ArgumentNullException("graphBuilder");

      List<TE> matchingFilters = new List<TE>();
      IEnumFilters enumFilters;
      if (graphBuilder.EnumFilters(out enumFilters) == 0)
      {
        IBaseFilter[] filters = new IBaseFilter[1];
        IntPtr fetched = Marshal.AllocCoTaskMem(4);

        while (enumFilters.Next(filters.Length, filters, fetched) == 0)
        {
          if (filters[0] is TE)
            matchingFilters.Add((TE)filters[0]);
          else
            if (Marshal.IsComObject(filters[0])) Marshal.ReleaseComObject(filters[0]);
        }
        Marshal.ReleaseComObject(enumFilters);
        Marshal.FreeCoTaskMem(fetched);
      }
      return matchingFilters;
    }

    /// <summary>
    /// Render a filter's pin in a DirectShow Graph
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <param name="source">the filter containing the pin to render</param>
    /// <param name="pinName">the pin name</param>
    /// <returns>true if rendering is a success, false if not</returns>
    /// <example>
    /// <code>
    /// hr = graphBuilder.AddSourceFilter(@"foo.avi", "Source Filter", out filter);
    /// new HRESULT(hr).Throw();
    /// 
    /// if (!FilterGraphTools.RenderPin(graphBuilder, filter, "Output"))
    /// {
    ///   // Something went wrong...
    /// }
    /// </code>
    /// </example>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder or source is null</exception>
    /// <remarks>This method assumes that the filter is part of the given graph</remarks>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static bool RenderPin(IGraphBuilder graphBuilder, IBaseFilter source, string pinName)
    {
      if (graphBuilder == null)
      {
        throw new ArgumentNullException("graphBuilder");
      }

      if (source == null)
      {
        throw new ArgumentNullException("source");
      }

      using (DSFilter dsFilter = new DSFilter(source))
      {
        var pin = dsFilter.GetPin(pinName);

        if (pin != null)
        {
          int hr = graphBuilder.Render(pin.Value);
          pin.Dispose();

          return (hr >= 0);
        }
      }

      return false;
    }

    /// <summary>
    /// Renders the output pins of the filter specified
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <param name="filter">The filter.</param>
    public static void RenderOutputPins(IGraphBuilder graphBuilder, IBaseFilter filter)
    {
      IEnumPins enumer;
      IPin[] pins = new IPin[2];
      IntPtr pFetched = Marshal.AllocCoTaskMem(4);
      filter.EnumPins(out enumer);
      try
      {
        while (enumer.Next(1, pins, pFetched) == 0)
        {
          if (Marshal.ReadInt32(pFetched) == 1 && pins[0] != null)
          {
            try
            {
              PinDirection pinDir;
              pins[0].QueryDirection(out pinDir);
              if (pinDir == PinDirection.Output)
              {
                int hr = graphBuilder.Render(pins[0]);
              }
            }
            finally
            {
              Marshal.ReleaseComObject(pins[0]);
            }
          }
        }
      }
      finally
      {
        Marshal.FreeCoTaskMem(pFetched);
        Marshal.ReleaseComObject(enumer);
      }
    }

    /// <summary>
    /// Enumerates the complete graph and renders all output pins that are not  automatically 
    /// connected from GraphBuilder (starting with ~ sign).
    /// </summary>
    /// <param name="graphBuilder">GraphBuilder</param>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static void RenderAllManualConnectPins(IGraphBuilder graphBuilder)
    {
      if (graphBuilder == null)
        return;

      IntPtr pFetched = Marshal.AllocCoTaskMem(4);
      IEnumFilters enumFilters;
      ArrayList filtersArray = new ArrayList();
      int hr = graphBuilder.EnumFilters(out enumFilters);
      try
      {
        new HRESULT(hr).Throw();
        IBaseFilter[] filters = new IBaseFilter[1];
        while (enumFilters.Next(filters.Length, filters, pFetched) == 0)
          filtersArray.Add(filters[0]);

        foreach (IBaseFilter filter in filtersArray)
        {
          RenderManualConnectPins(graphBuilder, filter);
          Marshal.ReleaseComObject(filter);
        }
        filtersArray.Clear();
      }
      finally
      {
        Marshal.FreeCoTaskMem(pFetched);
        Marshal.ReleaseComObject(enumFilters);
      }
    }

    /// <summary>
    /// Renders output pins that are not automatically connected from GraphBuilder (starting with ~ sign).
    /// </summary>
    /// <param name="graphBuilder">GraphBuilder</param>
    /// <param name="baseFilter">Filter to render</param>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static void RenderManualConnectPins(IGraphBuilder graphBuilder, IBaseFilter baseFilter)
    {
      if (baseFilter == null)
        return;

      IntPtr pFetched = Marshal.AllocCoTaskMem(4);
      try
      {
        IEnumPins pinEnum;
        int hr = baseFilter.EnumPins(out pinEnum);
        new HRESULT(hr).Throw();
        if (hr == 0 && pinEnum != null)
        {
          pinEnum.Reset();
          IPin[] pins = new IPin[1];
          while (pinEnum.Next(1, pins, pFetched) == 0)
          {
            if (Marshal.ReadInt32(pFetched) == 1 && pins[0] != null)
            {
              PinDirection pinDir;
              PinInfo pinInfo;
              pins[0].QueryDirection(out pinDir);
              pins[0].QueryPinInfo(out pinInfo);
              if (pinDir == PinDirection.Output && pinInfo.name.StartsWith("~"))
              {
                hr = graphBuilder.Render(pins[0]);
              }
              DsUtils.FreePinInfo(pinInfo);
              Marshal.ReleaseComObject(pins[0]);
            }
          }
          Marshal.ReleaseComObject(pinEnum);
        }
      }
      finally
      {
        Marshal.FreeCoTaskMem(pFetched);
      }
    }

    /// <summary>
    /// Enumerates all Pins of a given <paramref name="filter"/> having the <paramref name="direction"/> and returns 
    /// the currently connected pins into <paramref name="pinList"/>. This methods adds a reference to each returned Pin,
    /// so the caller has to do a Marshal.ReleaseComObject on them when done. When used in combination with <see cref="RestorePinConnections"/>
    /// the method takes care for releasing pins.
    /// </summary>
    /// <param name="filter">Filter</param>
    /// <param name="direction">PinDirection</param>
    /// <param name="pinList">List to be filled with IPins</param>
    public static void GetConnectedPins(IBaseFilter filter, PinDirection direction, IList<IPin> pinList)
    {
      IEnumPins enumer;
      filter.EnumPins(out enumer);
      if (enumer == null)
        return;

      IPin[] pins = new IPin[1];
      IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
      if (enumer.Next(1, pins, ptrFetched) == 0)
      {
        if (Marshal.ReadInt32(ptrFetched) == 1)
        {
          if (pins[0] != null)
          {
            PinDirection pinDir;
            pins[0].QueryDirection(out pinDir);
            if (pinDir == direction)
            {
              IntPtr pinConnect_ptr;
              if (pins[0].ConnectedTo(out pinConnect_ptr) == 0 && pinConnect_ptr != IntPtr.Zero)
                pinList.Add(Marshal.GetObjectForIUnknown(pinConnect_ptr) as IPin);

              Marshal.ReleaseComObject(pins[0]);
            }
          }
        }
      }
      Marshal.FreeCoTaskMem(ptrFetched);
      Marshal.ReleaseComObject(enumer);
    }

    /// <summary>
    /// Tries to reconnect the pins of the given <paramref name="filter"/> with the passed pins in <paramref name="previousConnectedPins"/>.
    /// After connection the pins are freed (Marshal.ReleaseComObject) and removed from the <paramref name="previousConnectedPins"/>.
    /// </summary>
    /// <param name="graphBuilder">Graphbuilder</param>
    /// <param name="filter">The filter to reconnect</param>
    /// <param name="direction">Pin direction</param>
    /// <param name="previousConnectedPins">List of pins to connect</param>
    public static void RestorePinConnections(IGraphBuilder graphBuilder, IBaseFilter filter, PinDirection direction, IList<IPin> previousConnectedPins)
    {
      IEnumPins enumer;
      filter.EnumPins(out enumer);
      if (enumer == null)
        return;

      IPin[] pins = new IPin[1];
      IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
      if (enumer.Next(1, pins, ptrFetched) == 0)
      {
        if (Marshal.ReadInt32(ptrFetched) == 1 && pins[0] != null)
        {
          PinDirection pinDir;
          pins[0].QueryDirection(out pinDir);
          if (pinDir == direction)
          {
            if (previousConnectedPins.Count > 0)
            {
              graphBuilder.Connect(previousConnectedPins[0], pins[0]);
              Marshal.ReleaseComObject(previousConnectedPins[0]);
              previousConnectedPins.RemoveAt(0);
            }
            Marshal.ReleaseComObject(pins[0]);
          }
        }
      }
      Marshal.FreeCoTaskMem(ptrFetched);
      Marshal.ReleaseComObject(enumer);
    }

    /// <summary>
    /// Disconnect all pins on a given filter
    /// </summary>
    /// <param name="filter">the filter on which to disconnect all the pins</param>
    /// <exception cref="System.ArgumentNullException">Thrown if filter is null</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">Thrown if errors occured during the disconnection process</exception>
    /// <remarks>Both input and output pins are disconnected</remarks>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static void DisconnectPins(IBaseFilter filter)
    {
      if (filter == null)
      {
        throw new ArgumentNullException("filter");
      }

      IEnumPins enumPins;
      IPin[] pins = new IPin[1];
      IntPtr fetched = Marshal.AllocCoTaskMem(4);

      int hr = filter.EnumPins(out enumPins);
      new HRESULT(hr).Throw();

      try
      {
        while (enumPins.Next(pins.Length, pins, fetched) == 0)
        {
          try
          {
            hr = pins[0].Disconnect();
            new HRESULT(hr).Throw();
          }
          finally
          {
            Marshal.ReleaseComObject(pins[0]);
          }
        }
      }
      finally
      {
        Marshal.ReleaseComObject(enumPins);
        Marshal.FreeCoTaskMem(fetched);
      }
    }

    /// <summary>
    /// Disconnect pins of all the filters in a DirectShow Graph
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder is null</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">Thrown if the method can't enumerate its filters</exception>
    /// <remarks>This method doesn't throw an exception if an error occurs during pin disconnections</remarks>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static void DisconnectAllPins(IGraphBuilder graphBuilder)
    {
      IEnumFilters enumFilters;

      if (graphBuilder == null)
      {
        throw new ArgumentNullException("graphBuilder");
      }

      int hr = graphBuilder.EnumFilters(out enumFilters);
      new HRESULT(hr).Throw();
      IntPtr fetched = Marshal.AllocCoTaskMem(4);

      try
      {
        IBaseFilter[] filters = new IBaseFilter[1];

        while (enumFilters.Next(filters.Length, filters, fetched) == 0)
        {
          try
          {
            DisconnectPins(filters[0]);
          }
          catch { }
          Marshal.ReleaseComObject(filters[0]);
        }
      }
      finally
      {
        Marshal.ReleaseComObject(enumFilters);
        Marshal.FreeCoTaskMem(fetched);
      }
    }

    /// <summary>
    /// Remove and release all filters from a DirectShow Graph
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <param name="removeAllReferences">True to release all COM references.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder is null</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">Thrown if the method can't enumerate its filters</exception>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static void RemoveAllFilters(IGraphBuilder graphBuilder, bool removeAllReferences)
    {
      IEnumFilters enumFilters;

      if (graphBuilder == null)
        throw new ArgumentNullException("graphBuilder");

      int hr = graphBuilder.EnumFilters(out enumFilters);
      new HRESULT(hr).Throw();

      IntPtr fetched = Marshal.AllocCoTaskMem(4);
      try
      {
        IBaseFilter[] filters = new IBaseFilter[1];

        while (enumFilters.Next(filters.Length, filters, fetched) == 0)
        {
          graphBuilder.RemoveFilter(filters[0]);
          enumFilters.Reset();
          TryRelease(ref filters[0], removeAllReferences);
        }
      }
      finally
      {
        TryRelease(ref enumFilters);
        Marshal.FreeCoTaskMem(fetched);
      }
    }

    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static void FreeAMMediaType(AMMediaType mediaType)
    {
      DsUtils.FreeAMMediaType(mediaType);
    }

    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static void FreePinInfo(PinInfo pinInfo)
    {
      DsUtils.FreePinInfo(pinInfo);
    }

    /// <summary>
    /// Queries the FilterInfo and frees the reference to FilterGraph.
    /// </summary>
    /// <param name="filter">IBaseFilter.</param>
    /// <returns>FilterInfo without references.</returns>
    public static FilterInfo QueryFilterInfoAndFree(IBaseFilter filter)
    {
      FilterInfo info;
      int hr = filter.QueryFilterInfo(out info);
      new HRESULT(hr).Throw();

      if (info.pGraph != null)
        Marshal.ReleaseComObject(info.pGraph);
      return info;
    }

    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static void FreeFilterInfo(FilterInfo info)
    {
      if (info.pGraph != null)
        Marshal.ReleaseComObject(info.pGraph);
    }

    /// <summary>
    /// Save a DirectShow Graph to a GRF file
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <param name="fileName">the file to be saved</param>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder is null</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">Thrown if errors occur during the file creation</exception>
    /// <seealso cref="LoadGraphFile"/>
    /// <remarks>This method overwrites any existing file</remarks>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static void SaveGraphFile(IGraphBuilder graphBuilder, string fileName)
    {
      IStorage storage = null;
#if USING_NET11
            UCOMIStream stream = null;
#else
      IStream stream = null;
#endif

      if (graphBuilder == null)
      {
        throw new ArgumentNullException("graphBuilder");
      }

      try
      {
        int hr = NativeMethods.StgCreateDocfile(
            fileName,
            STGM.Create | STGM.Transacted | STGM.ReadWrite | STGM.ShareExclusive,
            0,
            out storage
            );

        Marshal.ThrowExceptionForHR(hr);

        hr = storage.CreateStream(
            @"ActiveMovieGraph",
            STGM.Write | STGM.Create | STGM.ShareExclusive,
            0,
            0,
            out stream
            );

        Marshal.ThrowExceptionForHR(hr);

        hr = ((IPersistStream)graphBuilder).Save(Marshal.GetIUnknownForObject(stream), true);
        Marshal.ThrowExceptionForHR(hr);

        hr = storage.Commit(STGC.Default);
        Marshal.ThrowExceptionForHR(hr);
      }
      finally
      {
        if (stream != null)
        {
          Marshal.ReleaseComObject(stream);
        }
        if (storage != null)
        {
          Marshal.ReleaseComObject(storage);
        }
      }
    }

    /// <summary>
    /// Load a DirectShow Graph from a file
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <param name="fileName">the file to be loaded</param>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder is null</exception>
    /// <exception cref="System.ArgumentException">Thrown if the given file is not a valid graph file</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">Thrown if errors occur during loading</exception>
    /// <seealso cref="SaveGraphFile"/>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static void LoadGraphFile(IGraphBuilder graphBuilder, string fileName)
    {
      IStorage storage = null;
#if USING_NET11
			UCOMIStream stream = null;
#else
      IStream stream = null;
#endif

      if (graphBuilder == null)
      {
        throw new ArgumentNullException("graphBuilder");
      }

      try
      {
        if (NativeMethods.StgIsStorageFile(fileName) != 0)
        {
          throw new ArgumentException();
        }

        int hr = NativeMethods.StgOpenStorage(
            fileName,
            null,
            STGM.Transacted | STGM.Read | STGM.ShareDenyWrite,
            IntPtr.Zero,
            0,
            out storage
            );

        Marshal.ThrowExceptionForHR(hr);

        hr = storage.OpenStream(
            @"ActiveMovieGraph",
            IntPtr.Zero,
            STGM.Read | STGM.ShareExclusive,
            0,
            out stream
            );

        Marshal.ThrowExceptionForHR(hr);

        hr = ((IPersistStream)graphBuilder).Load(Marshal.GetIUnknownForObject(stream));
        Marshal.ThrowExceptionForHR(hr);
      }
      finally
      {
        if (stream != null)
        {
          Marshal.ReleaseComObject(stream);
        }
        if (storage != null)
        {
          Marshal.ReleaseComObject(storage);
        }
      }
    }

    /// <summary>
    /// Check if a DirectShow filter can display Property Pages
    /// </summary>
    /// <param name="filter">A DirectShow Filter</param>
    /// <exception cref="System.ArgumentNullException">Thrown if filter is null</exception>
    /// <seealso cref="ShowFilterPropertyPage"/>
    /// <returns>true if the filter has Property Pages, false if not</returns>
    /// <remarks>
    /// This method is intended to be used with <see cref="ShowFilterPropertyPage">ShowFilterPropertyPage</see>
    /// </remarks>
    public static bool HasPropertyPages(IBaseFilter filter)
    {
      if (filter == null)
      {
        throw new ArgumentNullException("filter");
      }

      return ((filter as ISpecifyPropertyPages) != null);
    }

    /// <summary>
    /// Display Property Pages of a given DirectShow filter
    /// </summary>
    /// <param name="filter">A DirectShow Filter</param>
    /// <param name="parent">A hwnd handle of a window to contain the pages</param>
    /// <exception cref="System.ArgumentNullException">Thrown if filter is null</exception>
    /// <seealso cref="HasPropertyPages"/>
    /// <remarks>
    /// You can check if a filter supports Property Pages with the <see cref="HasPropertyPages">HasPropertyPages</see> method.<br/>
    /// <strong>Warning</strong> : This method is blocking. It only returns when the Property Pages are closed.
    /// </remarks>
    /// <example>This sample shows how to check if a filter supports Property Pages and displays them
    /// <code>
    /// if (FilterGraphTools.HasPropertyPages(myFilter))
    /// {
    ///   FilterGraphTools.ShowFilterPropertyPage(myFilter, myForm.Handle);
    /// }
    /// </code>
    /// </example>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static void ShowFilterPropertyPage(IBaseFilter filter, IntPtr parent)
    {
      FilterInfo filterInfo;
      DsCAUUID caGuid;
      object[] objs;

      if (filter == null)
      {
        throw new ArgumentNullException("filter");
      }

      if (HasPropertyPages(filter))
      {
        int hr = filter.QueryFilterInfo(out filterInfo);
        new HRESULT(hr).Throw();

        if (filterInfo.pGraph != null)
        {
          Marshal.ReleaseComObject(filterInfo.pGraph);
        }

        hr = ((ISpecifyPropertyPages)filter).GetPages(out caGuid);
        new HRESULT(hr).Throw();

        try
        {
          objs = new object[1];
          objs[0] = filter;

          NativeMethods.OleCreatePropertyFrame(
              parent, 0, 0,
              filterInfo.achName,
              objs.Length, objs,
              caGuid.cElems, caGuid.pElems,
              0, 0,
              IntPtr.Zero
              );
        }
        finally
        {
          Marshal.FreeCoTaskMem(caGuid.pElems);
        }
      }
    }

    /// <summary>
    /// Check if a COM Object is available
    /// </summary>
    /// <param name="clsid">The CLSID of this object</param>
    /// <example>This sample shows how to check if the MPEG-2 Demultiplexer filter is available
    /// <code>
    /// if (FilterGraphTools.IsThisComObjectInstalled(typeof(MPEG2Demultiplexer).GUID))
    /// {
    ///   // Use it...
    /// }
    /// </code>
    /// </example>
    /// <returns>true if the object is available, false if not</returns>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static bool IsThisComObjectInstalled(Guid clsid)
    {
      bool retval = false;

      try
      {
        Type type = Type.GetTypeFromCLSID(clsid);
        object o = Activator.CreateInstance(type);
        retval = true;
        Marshal.ReleaseComObject(o);
      }
      catch { }

      return retval;
    }

    /// <summary>
    /// Connect pins from two filters
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <param name="upFilter">the upstream filter</param>
    /// <param name="sourcePinName">the upstream filter pin name</param>
    /// <param name="downFilter">the downstream filter</param>
    /// <param name="destPinName">the downstream filter pin name</param>
    /// <param name="useIntelligentConnect">indicate if the method should use DirectShow's Intelligent Connect</param>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder, upFilter or downFilter are null</exception>
    /// <exception cref="System.ArgumentException">Thrown if pin names are not found in filters</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">Thrown if pins can't connect</exception>
    /// <remarks>
    /// If useIntelligentConnect is true, this method can add missing filters between the two pins.<br/>
    /// If useIntelligentConnect is false, this method works only if the two media types are compatible.
    /// </remarks>
    public static void ConnectFilters(IGraphBuilder graphBuilder, IBaseFilter upFilter, string sourcePinName,
        IBaseFilter downFilter, string destPinName, bool useIntelligentConnect)
    {
      if (graphBuilder == null)
      {
        throw new ArgumentNullException("graphBuilder");
      }

      if (upFilter == null)
      {
        throw new ArgumentNullException("upFilter");
      }

      if (downFilter == null)
      {
        throw new ArgumentNullException("downFilter");
      }

      DSPin sourcePin = null;
      DSPin destPin = null;
      using (DSFilter dsUpFilter = new DSFilter(upFilter))
      using (DSFilter dsDownFilter = new DSFilter(downFilter))
      {
        try
        {
          destPin = dsDownFilter.GetPin(destPinName);
          sourcePin = dsUpFilter.GetPin(sourcePinName);

          if (sourcePin == null)
            throw new ArgumentException(@"The source filter has no pin called : " + sourcePinName, sourcePinName);

          if (destPin == null)
            throw new ArgumentException(@"The downstream filter has no pin called : " + destPinName, destPinName);

          ConnectFilters(graphBuilder, sourcePin.Value, destPin.Value, useIntelligentConnect);
        }
        finally
        {
          TryDispose(ref sourcePin);
          TryDispose(ref destPin);
        }
      }
    }

    /// <summary>
    /// Connect pins from two filters
    /// </summary>
    /// <param name="graphBuilder">the IGraphBuilder interface of the graph</param>
    /// <param name="sourcePin">the source (upstream / output) pin</param>
    /// <param name="destPin">the destination (downstream / input) pin</param>
    /// <param name="useIntelligentConnect">indicates if the method should use DirectShow's Intelligent Connect</param>
    /// <exception cref="System.ArgumentNullException">Thrown if graphBuilder, sourcePin or destPin are null</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">Thrown if pins can't connect</exception>
    /// <remarks>
    /// If useIntelligentConnect is true, this method can add missing filters between the two pins.<br/>
    /// If useIntelligentConnect is false, this method works only if the two media types are compatible.
    /// </remarks>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static void ConnectFilters(IGraphBuilder graphBuilder, IPin sourcePin, IPin destPin, bool useIntelligentConnect)
    {
      int hr;

      if (graphBuilder == null)
      {
        throw new ArgumentNullException("graphBuilder");
      }

      if (sourcePin == null)
      {
        throw new ArgumentNullException("sourcePin");
      }

      if (destPin == null)
      {
        throw new ArgumentNullException("destPin");
      }

      if (useIntelligentConnect)
      {
        hr = graphBuilder.Connect(sourcePin, destPin);
        new HRESULT(hr).Throw();
      }
      else
      {
        hr = graphBuilder.ConnectDirect(sourcePin, destPin, null);
        new HRESULT(hr).Throw();
      }
    }

    /// <summary>
    /// Checks and release filter as COM object.
    /// </summary>
    /// <param name="filterToRelease">any COM object to release</param>
    public static bool TryRelease<TE>(ref TE filterToRelease) where TE : class
    {
      return TryRelease(ref filterToRelease, false);
    }

    /// <summary>
    /// Checks and release filter as COM object.
    /// </summary>
    /// <param name="filterToRelease">any COM object to release</param>
    /// <param name="releaseAllReferences">true to loop until all references are removed</param>
    public static bool TryRelease<TE>(ref TE filterToRelease, bool releaseAllReferences) where TE : class
    {
      if (filterToRelease != null && Marshal.IsComObject(filterToRelease))
      {
        int remainingReferences;
        do
        {
          string strFilterToRelease = filterToRelease.ToString();
          remainingReferences = Marshal.ReleaseComObject(filterToRelease);
          if (remainingReferences > 0)
            ServiceRegistration.Get<ILogger>().Info("Releasing filter {0}, remaining references: {1}", strFilterToRelease, remainingReferences);

        } while (remainingReferences > 0 && releaseAllReferences);
        filterToRelease = default(TE);
        return true;
      }
      return false;
    }
    /// <summary>
    /// Disposes the object, if supported. Then sets reference to null.
    /// </summary>
    /// <typeparam name="TE"></typeparam>
    /// <param name="objectToDispose"></param>
    public static bool TryDispose<TE>(ref TE objectToDispose) where TE : class
    {
      if (objectToDispose == null)
        return false;

      if (objectToDispose is IDisposable)
        (objectToDispose as IDisposable).Dispose();

      objectToDispose = default(TE);
      return true;
    }

    /// <summary>
    /// Returns the friendly name of a moniker.
    /// </summary>
    /// <param name="mon">moniker</param>
    /// <returns>friendly name</returns>
    public static string GetFriendlyName(IMoniker mon)
    {
      return GetFilterProperty(mon, "FriendlyName");
    }
    /// <summary>
    /// Returns the friendly name of a moniker.
    /// </summary>
    /// <param name="mon">moniker</param>
    /// <returns>friendly name</returns>
    public static Guid GetCLSID(IMoniker mon)
    {
      String clsidString = GetFilterProperty(mon, "CLSID");
      return String.IsNullOrEmpty(clsidString) ? Guid.Empty : new Guid(clsidString);
    }

    /// <summary>
    /// Returns a propery from the Filter's property bag.
    /// </summary>
    /// <param name="mon">moniker</param>
    /// <param name="propertyName">name of property</param>
    /// <returns>friendly name</returns>
    public static string GetFilterProperty(IMoniker mon, String propertyName)
    {
      if (mon == null)
        return string.Empty;

      object bagObj = null;
      IPropertyBag bag = null;
      try
      {
        IErrorLog errorLog = null;
        Guid bagId = typeof(IPropertyBag).GUID;
        mon.BindToStorage(null, null, ref bagId, out bagObj);
        bag = (IPropertyBag)bagObj;
        object val;
        int hr = bag.Read(propertyName, out val, errorLog);
        Marshal.ThrowExceptionForHR(hr);

        string ret = val as string;
        if (string.IsNullOrEmpty(ret))
          throw new NotImplementedException("Filter: " + propertyName);

        return ret;
      }
      catch (Exception)
      {
        return null;
      }
      finally
      {
        bag = null;
        if (bagObj != null)
          Marshal.ReleaseComObject(bagObj);
        bagObj = null;
      }
    }
  }

  [Flags]
  internal enum STGM
  {
    Read = 0x00000000,
    Write = 0x00000001,
    ReadWrite = 0x00000002,
    ShareDenyNone = 0x00000040,
    ShareDenyRead = 0x00000030,
    ShareDenyWrite = 0x00000020,
    ShareExclusive = 0x00000010,
    Priority = 0x00040000,
    Create = 0x00001000,
    Convert = 0x00020000,
    FailIfThere = 0x00000000,
    Direct = 0x00000000,
    Transacted = 0x00010000,
    NoScratch = 0x00100000,
    NoSnapShot = 0x00200000,
    Simple = 0x08000000,
    DirectSWMR = 0x00400000,
    DeleteOnRelease = 0x04000000,
  }

  [Flags]
  internal enum STGC
  {
    Default = 0,
    Overwrite = 1,
    OnlyIfCurrent = 2,
    DangerouslyCommitMerelyToDiskCache = 4,
    Consolidate = 8
  }

  [Guid("0000000b-0000-0000-C000-000000000046"),
   InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IStorage
  {
    [PreserveSig]
    int CreateStream(
        [In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName,
        [In] STGM grfMode,
        [In] int reserved1,
        [In] int reserved2,
#if USING_NET11
			[Out] out UCOMIStream ppstm
#else
 [Out] out IStream ppstm
#endif
);

    [PreserveSig]
    int OpenStream(
        [In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName,
        [In] IntPtr reserved1,
        [In] STGM grfMode,
        [In] int reserved2,
#if USING_NET11
			[Out] out UCOMIStream ppstm
#else
 [Out] out IStream ppstm
#endif
);

    [PreserveSig]
    int CreateStorage(
        [In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName,
        [In] STGM grfMode,
        [In] int reserved1,
        [In] int reserved2,
        [Out] out IStorage ppstg
        );

    [PreserveSig]
    int OpenStorage(
        [In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName,
        [In] IStorage pstgPriority,
        [In] STGM grfMode,
        [In] int snbExclude,
        [In] int reserved,
        [Out] out IStorage ppstg
        );

    [PreserveSig]
    int CopyTo(
        [In] int ciidExclude,
        [In] Guid[] rgiidExclude,
        [In] string[] snbExclude,
        [In] IStorage pstgDest
        );

    [PreserveSig]
    int MoveElementTo(
        [In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName,
        [In] IStorage pstgDest,
        [In, MarshalAs(UnmanagedType.LPWStr)] string pwcsNewName,
        [In] STGM grfFlags
        );

    [PreserveSig]
    int Commit([In] STGC grfCommitFlags);

    [PreserveSig]
    int Revert();

    [PreserveSig]
    int EnumElements(
        [In] int reserved1,
        [In] IntPtr reserved2,
        [In] int reserved3,
        [Out, MarshalAs(UnmanagedType.Interface)] out object ppenum
        );

    [PreserveSig]
    int DestroyElement([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName);

    [PreserveSig]
    int RenameElement(
        [In, MarshalAs(UnmanagedType.LPWStr)] string pwcsOldName,
        [In, MarshalAs(UnmanagedType.LPWStr)] string pwcsNewName
        );

    [PreserveSig]
    int SetElementTimes(
        [In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName,
#if USING_NET11
			[In] FILETIME pctime,
			[In] FILETIME patime,
			[In] FILETIME pmtime
#else
 [In] FILETIME pctime,
 [In] FILETIME patime,
 [In] FILETIME pmtime
#endif
);

    [PreserveSig]
    int SetClass([In, MarshalAs(UnmanagedType.LPStruct)] Guid clsid);

    [PreserveSig]
    int SetStateBits(
        [In] int grfStateBits,
        [In] int grfMask
        );

    [PreserveSig]
    int Stat(
#if USING_NET11
			[Out] out STATSTG pStatStg,
#else
[Out] out STATSTG pStatStg,
#endif
 [In] int grfStatFlag
 );
  }

  internal static class NativeMethods
  {
    [DllImport("ole32.dll")]
    public static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

    [DllImport("ole32.dll")]
    public static extern int MkParseDisplayName(IBindCtx pcb, [MarshalAs(UnmanagedType.LPWStr)] string szUserName,
        out int pchEaten, out IMoniker ppmk);

    [DllImport("olepro32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int OleCreatePropertyFrame(
        [In] IntPtr hwndOwner,
        [In] int x,
        [In] int y,
        [In, MarshalAs(UnmanagedType.LPWStr)] string lpszCaption,
        [In] int cObjects,
        [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.IUnknown)] object[] ppUnk,
        [In] int cPages,
        [In] IntPtr pPageClsId,
        [In] int lcid,
        [In] int dwReserved,
        [In] IntPtr pvReserved
        );

    [DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int StgCreateDocfile(
        [In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName,
        [In] STGM grfMode,
        [In] int reserved,
        [Out] out IStorage ppstgOpen
        );

    [DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int StgIsStorageFile([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName);

    [DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int StgOpenStorage(
        [In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName,
        [In] IStorage pstgPriority,
        [In] STGM grfMode,
        [In] IntPtr snbExclude,
        [In] int reserved,
        [Out] out IStorage ppstgOpen
        );
  }
}