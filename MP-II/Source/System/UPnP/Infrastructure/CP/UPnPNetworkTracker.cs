#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP.SSDP;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.CP
{
  /// <summary>
  /// Delegate to be called when a network device was added and its given <paramref name="rootDescriptor"/> was filled
  /// completely.
  /// </summary>
  /// <param name="rootDescriptor">Descriptor containing all description documents of the new UPnP device.</param>
  public delegate void DeviceAddedDlgt(RootDescriptor rootDescriptor);

  /// <summary>
  /// Delegate to be called when a network device was removed.
  /// </summary>
  /// <param name="rootDescriptor">Descriptor of the UPnP device which was removed.</param>
  public delegate void DeviceRemovedDlgt(RootDescriptor rootDescriptor);

  /// <summary>
  /// Delegate to be called when a reboot of a network device was detected.
  /// </summary>
  /// <param name="rootDescriptor">Descriptor of the UPnP device which was rebooted.</param>
  public delegate void DeviceRebootedDlgt(RootDescriptor rootDescriptor);

  /// <summary>
  /// Tracks UPnP devices which are available in the network. Provides materialized descriptions for each available
  /// device and service. Provides an event <see cref="RootDeviceAdded"/> which gets fired when all description documents
  /// were fetched from the device's server. Provides an event <see cref="RootDeviceRemoved"/> which gets fired when
  /// the device disappears.
  /// </summary>
  public class UPnPNetworkTracker : IDisposable
  {
    protected class DescriptionRequestState
    {
      protected RootDescriptor _rootDescriptor;
      protected HttpWebRequest _httpWebRequest;
      protected IDictionary<ServiceDescriptor, string> _pendingServiceDescriptions =
          new Dictionary<ServiceDescriptor, string>();
      protected ServiceDescriptor _currentServiceDescriptor = null;

      public DescriptionRequestState(RootDescriptor rootDescriptor, HttpWebRequest httpWebRequest)
      {
        _rootDescriptor = rootDescriptor;
        _httpWebRequest = httpWebRequest;
      }

      public RootDescriptor RootDescriptor
      {
        get { return _rootDescriptor; }
      }

      public HttpWebRequest Request
      {
        get { return _httpWebRequest; }
        set { _httpWebRequest = value; }
      }

      public ServiceDescriptor CurrentServiceDescriptor
      {
        get { return _currentServiceDescriptor; }
        set { _currentServiceDescriptor = value; }
      }

      public IDictionary<ServiceDescriptor, string> PendingServiceDescriptions
      {
        get { return _pendingServiceDescriptions; }
      }
    }

    /// <summary>
    /// Timeout for a pending request for a description document in seconds.
    /// </summary>
    protected const int PENDING_REQUEST_TIMEOUT = 30;

    protected const string KEY_ROOT_DESCRIPTOR = "ROOT-DESCRIPTOR";

    protected ICollection<DescriptionRequestState> _pendingRequests = new List<DescriptionRequestState>();
    protected bool _active = false;
    protected CPData _cpData;

    #region Ctor

    /// <summary>
    /// Creates a new UPnP network tracker instance.
    /// </summary>
    /// <param name="cpData">Shared control point data instance.</param>
    public UPnPNetworkTracker(CPData cpData)
    {
      _cpData = cpData;
    }

    public void Dispose()
    {
      Close();
    }

    #endregion

    #region Public members

    /// <summary>
    /// Gets called when a new device appeared at the network.
    /// </summary>
    public event DeviceAddedDlgt RootDeviceAdded;

    /// <summary>
    /// Gets called when a device disappeared from the network. Will be called when the device explicitly cancelled its
    /// advertisement as well as when its advertisements expired.
    /// </summary>
    public event DeviceRemovedDlgt RootDeviceRemoved;

    /// <summary>
    /// Gets called when the SSDP subsystem detects a reboot of one of our known devices.
    /// </summary>
    public event DeviceRebootedDlgt DeviceRebooted;

    /// <summary>
    /// Returns the information whether this UPnP network tracker is active, i.e. the network listener is active and this
    /// instance is tracking network devices.
    /// </summary>
    public bool IsActive
    {
      get { return _active; }
    }

    /// <summary>
    /// Returns a mapping of root device UUIDs to descriptors containing the information about that device for all known
    /// UPnP network devices. Returns <c>null</c> if this network tracker isn't active.
    /// </summary>
    public IDictionary<string, RootDescriptor> KnownRootDevices
    {
      get
      {
        lock (_cpData.SyncObj)
        {
          if (!_active)
            return null;
          IDictionary<string, RootDescriptor> result = new Dictionary<string, RootDescriptor>();
          foreach (RootEntry entry in _cpData.SSDPController.RootEntries)
          {
            RootDescriptor rd = GetRootDescriptor(entry);
            if (rd == null)
              continue;
            result.Add(rd.SSDPRootEntry.RootDeviceID, rd);
          }
          return result;
        }
      }
    }

    /// <summary>
    /// Data which is shared among all components of the control point system.
    /// </summary>
    public CPData SharedControlPointData
    {
      get { return _cpData; }
    }

    /// <summary>
    /// Starts this UPnP network tracker. After the tracker is started, its <see cref="RootDeviceAdded"/> and
    /// <see cref="RootDeviceRemoved"/> events will begin to be raised when UPnP devices become available at the network of when
    /// UPnP devices disappear from the network.
    /// </summary>
    public void Start()
    {
      lock (_cpData.SyncObj)
      {
        if (_active)
          throw new IllegalCallException("UPnPNetworkTracker is already active");
        _active = true;
        SSDPClientController ssdpController = new SSDPClientController(_cpData);
        ssdpController.RootDeviceAdded += OnSSDPRootDeviceAdded;
        ssdpController.RootDeviceRemoved += OnSSDPRootDeviceRemoved;
        ssdpController.DeviceRebooted += OnSSDPDeviceRebooted;
        _cpData.SSDPController = ssdpController;
        ssdpController.Start();
      }
    }

    /// <summary>
    /// Stops this UPnP network tracker's activity, i.e. closes the UPnP network listener and clears all
    /// <see cref="KnownRootDevices"/>.
    /// </summary>
    public void Close()
    {
      lock (_cpData.SyncObj)
      {
        if (!_active)
          return;
        _active = false;
        foreach (RootEntry entry in _cpData.SSDPController.RootEntries)
        {
          RootDescriptor rd = GetRootDescriptor(entry);
          if (rd == null)
            continue;
          InvalidateDescriptor(rd);
        }
        SSDPClientController ssdpController = _cpData.SSDPController;
        ssdpController.Close();
        ssdpController.RootDeviceAdded -= OnSSDPRootDeviceAdded;
        ssdpController.RootDeviceRemoved -= OnSSDPRootDeviceRemoved;
        ssdpController.DeviceRebooted -= OnSSDPDeviceRebooted;
        _cpData.SSDPController = null;
        foreach (DescriptionRequestState state in _pendingRequests)
          state.Request.Abort();
        _pendingRequests.Clear();
      }
    }

    #endregion

    #region Private/protected members

    protected void InvokeRootDeviceAdded(RootDescriptor rd)
    {
      DeviceAddedDlgt dlgt = RootDeviceAdded;
      if (dlgt != null)
        dlgt(rd);
    }

    protected void InvokeRootDeviceRemoved(RootDescriptor rd)
    {
      DeviceRemovedDlgt dlgt = RootDeviceRemoved;
      if (dlgt != null)
        dlgt(rd);
    }

    protected void InvokeDeviceRebooted(RootDescriptor rd)
    {
      DeviceRebootedDlgt dlgt = DeviceRebooted;
      if (dlgt != null)
        dlgt(rd);
    }

    private void OnSSDPRootDeviceAdded(RootEntry rootEntry)
    {
      RootDescriptor rd = new RootDescriptor(rootEntry)
        {
            State = RootDescriptorState.AwaitingDeviceDescription
        };
      lock (_cpData.SyncObj)
        SetRootDescriptor(rootEntry, rd);
      HttpWebRequest request = CreateHttpGetRequest(rootEntry.DescriptionLocation);
      DescriptionRequestState state = new DescriptionRequestState(rd, request);
      lock (_cpData.SyncObj)
        _pendingRequests.Add(state);
      IAsyncResult result = request.BeginGetResponse(OnDeviceDescriptionReceived, state);
      NetworkHelper.AddTimeout(request, result, PENDING_REQUEST_TIMEOUT * 1000);
    }

    private void OnDeviceDescriptionReceived(IAsyncResult asyncResult)
    {
      DescriptionRequestState state = (DescriptionRequestState) asyncResult.AsyncState;
      RootDescriptor rd = state.RootDescriptor;
      if (rd.State != RootDescriptorState.AwaitingDeviceDescription)
        return;
      HttpWebRequest request = state.Request;
      try
      {
        WebResponse response = request.EndGetResponse(asyncResult);
        try
        {
          Stream body = response.GetResponseStream();
          XmlDocument doc = new XmlDocument();
          doc.Load(body);
          lock (_cpData.SyncObj)
          {
            rd.DeviceDescription = doc;
            XmlElement rootDeviceElement = (XmlElement) doc.DocumentElement.SelectSingleNode("device");
            ExtractServiceDescriptorsRecursive(rootDeviceElement, rd.ServiceDescriptors, state.PendingServiceDescriptions);
            rd.State = RootDescriptorState.AwaitingServiceDescriptions;
          }
          ContinueGetServiceDescription(state);
        }
        catch (Exception)
        {
          rd.State = RootDescriptorState.Erroneous;
        }
        finally
        {
          response.Close();
        }
      }
      catch (WebException)
      {
        rd.State = RootDescriptorState.Erroneous;
      }
    }

    private void ContinueGetServiceDescription(DescriptionRequestState state)
    {
      RootDescriptor rootDescriptor = state.RootDescriptor;

      IEnumerator<KeyValuePair<ServiceDescriptor, string>> enumer = state.PendingServiceDescriptions.GetEnumerator();
      if (!enumer.MoveNext())
      {
        lock (_cpData.SyncObj)
        {
          if (rootDescriptor.State != RootDescriptorState.AwaitingServiceDescriptions)
            return;
          _pendingRequests.Remove(state);
          rootDescriptor.State = RootDescriptorState.Ready;
        }
        InvokeRootDeviceAdded(rootDescriptor);
      }
      else
      {
        lock (_cpData.SyncObj)
          if (rootDescriptor.State != RootDescriptorState.AwaitingServiceDescriptions)
            return;
        state.CurrentServiceDescriptor = enumer.Current.Key;
        string url = state.PendingServiceDescriptions[state.CurrentServiceDescriptor];
        state.PendingServiceDescriptions.Remove(state.CurrentServiceDescriptor);
        state.CurrentServiceDescriptor.State = ServiceDescriptorState.AwaitingDescription;
        HttpWebRequest request = CreateHttpGetRequest(url);
        state.Request = request;
        IAsyncResult result = request.BeginGetResponse(OnServiceDescriptionReceived, state);
        NetworkHelper.AddTimeout(request, result, PENDING_REQUEST_TIMEOUT * 1000);
      }
    }

    private void OnServiceDescriptionReceived(IAsyncResult asyncResult)
    {
      DescriptionRequestState state = (DescriptionRequestState) asyncResult.AsyncState;
      RootDescriptor rd = state.RootDescriptor;
      lock (_cpData.SyncObj)
      {
        if (rd.State != RootDescriptorState.AwaitingDeviceDescription)
          return;
        HttpWebRequest request = state.Request;
        try
        {
          WebResponse response = request.EndGetResponse(asyncResult);
          try
          {
            Stream body = response.GetResponseStream();
            XmlDocument doc = new XmlDocument();
            doc.Load(body);
            state.CurrentServiceDescriptor.ServiceDescription = doc;
            state.CurrentServiceDescriptor.State = ServiceDescriptorState.Ready;
          }
          catch (Exception)
          {
            state.CurrentServiceDescriptor.State = ServiceDescriptorState.Erroneous;
            lock (_cpData.SyncObj)
              rd.State = RootDescriptorState.Erroneous;
          }
          finally
          {
            response.Close();
          }
        }
        catch (WebException)
        {
          state.CurrentServiceDescriptor.State = ServiceDescriptorState.Erroneous;
          lock (_cpData.SyncObj)
            rd.State = RootDescriptorState.Erroneous;
        }
      }
      // Don't hold the lock while calling ContinueGetServiceDescription - that method is calling event handlers
      try
      {
        ContinueGetServiceDescription(state);
      }
      catch (Exception)
      {
        rd.State = RootDescriptorState.Erroneous;
      }
    }

    private void OnSSDPRootDeviceRemoved(RootEntry rootEntry)
    {
      RootDescriptor rd = GetRootDescriptor(rootEntry);
      if (rd == null)
        return;
      lock (_cpData.SyncObj)
        InvalidateDescriptor(rd);
      InvokeRootDeviceRemoved(rd);
    }

    private void OnSSDPDeviceRebooted(RootEntry rootEntry)
    {
      RootDescriptor rd = GetRootDescriptor(rootEntry);
      if (rd == null)
        return;
      InvokeDeviceRebooted(rd);
    }

    /// <summary>
    /// Given an XML &lt;device&gt; element containing a device description, this method extracts two kinds of
    /// data:
    /// <list type="bullet">
    /// <item><see cref="ServiceDescriptor"/>s for services of the given device and all embedded devices (organized
    /// in a dictionary mapping device UUIDs to lists of service descriptors for services in the device) in
    /// <paramref name="serviceDescriptors"/></item>
    /// <item>A mapping of all service descriptors which are returned in <paramref name="serviceDescriptors"/> to
    /// their SCPD urls in <paramref name="pendingServiceDescriptions"/></item>
    /// </list>
    /// </summary>
    /// <param name="deviceElement">XML &lt;device&gt; element containing the device description.</param>
    /// <param name="serviceDescriptors">Dictionary of device UUIDs to collections of service descriptors, containing
    /// descriptors of services which are contained in the device with the key UUID.</param>
    /// <param name="pendingServiceDescriptions">Dictionary of device service descriptors mapped to the SCPD url of the
    /// service.</param>
    private static void ExtractServiceDescriptorsRecursive(XmlElement deviceElement,
        IDictionary<string, IDictionary<string, ServiceDescriptor>> serviceDescriptors,
        IDictionary<ServiceDescriptor, string> pendingServiceDescriptions)
    {
      if (deviceElement == null)
        return;
      string deviceUuid = ParserHelper.ExtractUUIDFromUDN(ParserHelper.SelectText(deviceElement, "UDN/text()"));
      XmlNodeList nl = deviceElement.SelectNodes("serviceList/service");
      if (nl.Count > 0)
      {
        IDictionary<string, ServiceDescriptor> sds = serviceDescriptors[deviceUuid] = new Dictionary<string, ServiceDescriptor>();
        foreach (XmlElement serviceElement in nl)
        {
          string descriptionURL;
          ServiceDescriptor sd = ExtractServiceDescriptor(serviceElement, out descriptionURL);
          sds.Add(sd.ServiceTypeVersion_URN, sd);
          pendingServiceDescriptions[sd] = descriptionURL;
        }
      }
      foreach (XmlElement embeddedDeviceElement in deviceElement.SelectNodes("deviceList/device"))
        ExtractServiceDescriptorsRecursive(embeddedDeviceElement, serviceDescriptors, pendingServiceDescriptions);
    }

    /// <summary>
    /// Given an XML &lt;service&gt; element containing a service description, this method extracts the returned
    /// <see cref="ServiceDescriptor"/> and the SCDP description url.
    /// </summary>
    /// <param name="serviceElement">XML &lt;service&gt; element containing the service description.</param>
    /// <param name="descriptionURL">Returns the description URL for the service.</param>
    /// <returns>Extracted service descriptor.</returns>
    private static ServiceDescriptor ExtractServiceDescriptor(XmlElement serviceElement, out string descriptionURL)
    {
      descriptionURL = ParserHelper.SelectText(serviceElement, "SCPDURL/text()");
      string serviceType;
      int serviceTypeVersion;
      if (!ParserHelper.TryParseTypeVersion_URN(ParserHelper.SelectText(serviceElement, "serviceType/text()"), out serviceType, out serviceTypeVersion))
        throw new ArgumentException("'serviceType' content has the wrong format");
      string controlURL = ParserHelper.SelectText(serviceElement, "controlURL");
      string eventSubURL = ParserHelper.SelectText(serviceElement, "eventSubURL");
      return new ServiceDescriptor(serviceType, serviceTypeVersion, ParserHelper.SelectText(serviceElement, "serviceId/text()"), controlURL, eventSubURL);
    }

    private static HttpWebRequest CreateHttpGetRequest(string url)
    {
      HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
      request.Method = "GET";
      request.KeepAlive = true;
      request.AllowAutoRedirect = true;
      request.UserAgent = Configuration.UPnPMachineInfoHeader;
      return request;
    }

    protected void InvalidateDescriptor(RootDescriptor rd)
    {
      rd.State = RootDescriptorState.Invalid;
      foreach (IDictionary<string, ServiceDescriptor> sdDict in rd.ServiceDescriptors.Values)
        foreach (ServiceDescriptor sd in sdDict.Values)
          sd.State = ServiceDescriptorState.Invalid;
    }

    protected RootDescriptor GetRootDescriptor(RootEntry rootEntry)
    {
      object rdObj;
      if (!rootEntry.ClientProperties.TryGetValue(KEY_ROOT_DESCRIPTOR, out rdObj))
        return null;
      return (RootDescriptor) rdObj;
    }

    protected void SetRootDescriptor(RootEntry rootEntry, RootDescriptor rootDescriptor)
    {
      rootEntry.ClientProperties[KEY_ROOT_DESCRIPTOR] = rootDescriptor;
    }

    #endregion
  }
}
