#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Diagnostics;
using System.Xml;
using System.IO;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using Intel.UPNP;
using Intel.UPNP.AV;
using Intel.UPNP.AV.MediaServer.DV;
using Intel.UPNP.AV.CdsMetadata;
//using MetadataParser;

namespace Components.UPnPServer
{
  public class MediaServerDevice2 : IUPnPDevice
  {
    // Fields
    private DvConnectionManager ConnectionManager;
    private DvContentDirectory ContentDirectory;
    private UPnPDevice Device;
    private bool EnableHttp;
    private static bool ENCODE_UTF8 = MediaObject.ENCODE_UTF8;
    private Mutex Lock_ContainerUpdateIDs = new Mutex();
    private Mutex Lock_SystemUpdateID = new Mutex();
    private LifeTimeMonitor.LifeTimeHandler LTMDelegate = null;
    private Hashtable m_Cache = new Hashtable(0x9c4);
    private Hashtable m_Connections = new Hashtable();
    private Hashtable m_HttpTransfers = new Hashtable();
    private LifeTimeMonitor m_LFT = new LifeTimeMonitor();
    private ReaderWriterLock m_LockConnections = new ReaderWriterLock();
    private ReaderWriterLock m_LockHttpTransfers = new ReaderWriterLock();
    private ReaderWriterLock m_LockSinkProtocolInfo = new ReaderWriterLock();
    private ReaderWriterLock m_LockSourceProtocolInfo = new ReaderWriterLock();
    private DvRootContainer2 m_Root;
    private string m_SearchCapabilities;
    private ArrayList m_SinkProtocolInfoSet = new ArrayList();
    private string m_SortCapabilities;
    private ArrayList m_SourceProtocolInfoSet = new ArrayList();
    private Statistics m_Stats = new Statistics();
    private string m_VirtualDirName;
    public int MaxConnections = 0x7fffffff;
    private int NextConnId = -1;
    public Delegate_PrepareForConnection OnCallPrepareForConnection;
    public Delegate_FileNotMappedHandler OnFileNotMapped;
    public Delegate_AddBranch OnRequestAddBranch;
    public Delegate_ChangeMetadata OnRequestChangeMetadata;
    public Delegate_ModifyBinary OnRequestDeleteBinary;
    public Delegate_RemoveBranch OnRequestRemoveBranch;
    public Delegate_ModifyBinary OnRequestSaveBinary;
    private const int StartConnId = -1;
    private static Tags CdsTags = Tags.GetInstance();
    private static long VirtualDirCounter = -1L;
    private static int XML_BUFFER_SIZE = MediaObject.XML_BUFFER_SIZE;

    // Events
    public event Delegate_MediaServerHandler OnHttpTransfersChanged;

    public event Delegate_MediaServerHandler OnStatsChanged;

    // Methods
    public MediaServerDevice2(DeviceInfo info, UPnPDevice parent, bool enableHttpContentServing, string initialSourceProtocolInfoSet, string initialSinkProtocolInfoSet)
    {
      EnableHttp = enableHttpContentServing;
      LTMDelegate = new LifeTimeMonitor.LifeTimeHandler(Sink_OnExpired);
      m_LFT.OnExpired += LTMDelegate;
      if (parent == null)
      {
        Device = UPnPDevice.CreateRootDevice(info.CacheTime, 1, info.LocalRootDirectory);
        if (info.CustomUDN != "")
        {
          Device.UniqueDeviceName = info.CustomUDN;
        }
      }
      else
      {
        Device = UPnPDevice.CreateEmbeddedDevice(1, Guid.NewGuid().ToString());
        parent.AddDevice(Device);
      }
      Device.HasPresentation = false;
      Device.StandardDeviceType = "MediaServer";
      Device.FriendlyName = info.FriendlyName;
      Device.Manufacturer = info.Manufacturer;
      Device.ManufacturerURL = info.ManufacturerURL;
      Device.ModelName = info.ModelName;
      Device.ModelDescription = info.ModelDescription;
      if (info.ModelURL != null)
      {
        try
        {
          Device.ModelURL = new Uri(info.ModelURL);
        }
        catch
        {
          Device.ModelURL = null;
        }
      }
      Device.ModelNumber = info.ModelNumber;
      if (info.INMPR03)
      {
        Device.AddCustomFieldInDescription("INMPR03", "1.0", "");
      }
      ConnectionManager = new DvConnectionManager();
      ContentDirectory = new DvContentDirectory();
      ContentDirectory.ModerationDuration_SystemUpdateID = 2;
      ContentDirectory.ModerationDuration_ContainerUpdateIDs = 2;
      ContentDirectory.Accumulator_ContainerUpdateIDs = new Accumulator_ContainerUpdateIDs();
      if (!info.AllowRemoteContentManagement)
      {
        ContentDirectory.RemoveAction_CreateObject();
        ContentDirectory.RemoveAction_CreateReference();
        ContentDirectory.RemoveAction_DeleteResource();
        ContentDirectory.RemoveAction_DestroyObject();
        ContentDirectory.RemoveAction_ImportResource();
        ContentDirectory.RemoveAction_UpdateObject();
        ContentDirectory.RemoveAction_ExportResource();
        ContentDirectory.RemoveAction_GetTransferProgress();
        ContentDirectory.RemoveAction_StopTransferResource();
      }
      if (!info.EnablePrepareForConnection)
      {
        ConnectionManager.RemoveAction_PrepareForConnection();
      }
      if (!info.EnableConnectionComplete)
      {
        ConnectionManager.RemoveAction_ConnectionComplete();
      }
      if (!(info.EnablePrepareForConnection || info.EnableConnectionComplete))
      {
        ProtocolInfoString prot = new ProtocolInfoString("http-get:*:*:*");
        Connection newConnection = new Connection(GetConnectionID(), -1, -1, -1, prot, "/", DvConnectionManager.Enum_A_ARG_TYPE_Direction.OUTPUT, DvConnectionManager.Enum_A_ARG_TYPE_ConnectionStatus.UNKNOWN);
        AddConnection(newConnection);
      }
      if (!info.EnableSearch)
      {
        ContentDirectory.RemoveAction_Search();
      }
      m_SearchCapabilities = info.SearchCapabilities;
      m_SortCapabilities = info.SortCapabilities;
      if (ConnectionManager.Evented_CurrentConnectionIDs == null)
      {
        ConnectionManager.Evented_CurrentConnectionIDs = "";
      }
      UpdateProtocolInfoSet(false, initialSinkProtocolInfoSet);
      UpdateProtocolInfoSet(true, initialSourceProtocolInfoSet);
      ContentDirectory.Evented_ContainerUpdateIDs = "";
      ContentDirectory.Evented_SystemUpdateID = 0;
      ContentDirectory.Evented_TransferIDs = "";
      ConnectionManager.External_ConnectionComplete = SinkCm_ConnectionComplete;
      ConnectionManager.External_GetCurrentConnectionIDs = SinkCm_GetCurrentConnectionIDs;
      ConnectionManager.External_GetCurrentConnectionInfo = SinkCm_GetCurrentConnectionInfo;
      ConnectionManager.External_GetProtocolInfo = SinkCm_GetProtocolInfo;
      ConnectionManager.External_PrepareForConnection = SinkCm_PrepareForConnection;
      ContentDirectory.External_Browse = SinkCd_Browse;
      ContentDirectory.External_CreateObject = SinkCd_CreateObject;
      ContentDirectory.External_CreateReference = SinkCd_CreateReference;
      ContentDirectory.External_DeleteResource = SinkCd_DeleteResource;
      ContentDirectory.External_DestroyObject = SinkCd_DestroyObject;
      ContentDirectory.External_ExportResource = SinkCd_ExportResource;
      ContentDirectory.External_GetSearchCapabilities = SinkCd_GetSearchCapabilities;
      ContentDirectory.External_GetSortCapabilities = SinkCd_GetSortCapabilities;
      ContentDirectory.External_GetSystemUpdateID = SinkCd_GetSystemUpdateID;
      ContentDirectory.External_GetTransferProgress = SinkCd_GetTransferProgress;
      ContentDirectory.External_ImportResource = SinkCd_ImportResource;
      ContentDirectory.External_Search = SinkCd_Search;
      ContentDirectory.External_StopTransferResource = SinkCd_StopTransferResource;
      ContentDirectory.External_UpdateObject = SinkCd_UpdateObject;
      Device.AddService(ConnectionManager);
      Device.AddService(ContentDirectory);
      Interlocked.Increment(ref VirtualDirCounter);
      m_VirtualDirName = "MediaServerContent_" + VirtualDirCounter;
      Device.AddVirtualDirectory(m_VirtualDirName, WebServer_OnHeaderReceiveSink, WebServer_OnPacketReceiveSink);
      MediaBuilder.container container = new MediaBuilder.container("Root");
      container.Searchable = true;
      container.IsRestricted = true;
      m_Root = DvMediaBuilder2.CreateRoot(container);
      m_Root.OnContainerChanged += Sink_ContainerChanged;
    }

    private IDvMedia _GetEntry(string id)
    {
      return (IDvMedia)GetDescendent(id);
    }

    private void AddConnection(Connection newConnection)
    {
      m_LockConnections.AcquireWriterLock(-1);
      m_Connections.Add(newConnection.ConnectionId, newConnection);
      UpdateConnections();
      m_LockConnections.ReleaseWriterLock();
    }

    private ArrayList AddRangeSets(ArrayList rangeSets, string rangeStr, long contentLength)
    {
      bool flag = true;
      flag = false;
      DText text = new DText();
      text.ATTRMARK = "=";
      text.MULTMARK = ",";
      text.SUBVMARK = "-";
      text[0] = rangeStr;
      int num = text.DCOUNT(2);
      for (int i = 1; i <= num; i++)
      {
        string s = text[2, i, 1].Trim();
        string str2 = text[2, i, 2].Trim();
        long position = -1L;
        long length = -1L;
        long num5 = -1L;
        if ((s == "") && (str2 == ""))
        {
          flag = true;
          break;
        }
        if ((s == "") && (str2 != ""))
        {
          try
          {
            position = 0L;
            num5 = long.Parse(str2);
            length = num5 + 1L;
          }
          catch
          {
            flag = true;
            break;
          }
        }
        else if ((s != "") && (str2 == ""))
        {
          try
          {
            position = long.Parse(s);
            num5 = contentLength - 1L;
            length = contentLength - position;
          }
          catch
          {
            flag = true;
            break;
          }
        }
        else
        {
          try
          {
            position = long.Parse(s);
            num5 = long.Parse(str2);
            if (position <= num5)
            {
              length = (num5 - position) + 1L;
            }
            else
            {
              flag = true;
            }
          }
          catch
          {
            flag = true;
            break;
          }
        }
        if (!flag)
        {
          Debug.Assert(position >= 0L);
          Debug.Assert(length >= 0L);
          Debug.Assert(num5 >= 0L);
          HTTPSession.Range range = new HTTPSession.Range(position, length);
          rangeSets.Add(range);
        }
      }
      if (flag)
      {
        rangeSets.Clear();
      }
      return rangeSets;
    }

    private void AddTransfer(HTTPSession session, HttpTransfer transferInfo)
    {
      m_LockHttpTransfers.AcquireWriterLock(-1);
      SessionData stateObject = (SessionData)session.StateObject;
      stateObject.Transfers.Enqueue(transferInfo);
      stateObject.Requested++;
      uint hashCode = (uint)session.GetHashCode();
      while (m_HttpTransfers.ContainsKey(hashCode))
      {
        hashCode++;
      }
      m_HttpTransfers.Add(hashCode, transferInfo);
      transferInfo.m_TransferId = hashCode;
      m_LockHttpTransfers.ReleaseWriterLock();
      FireHttpTransfersChange();
    }

    private void BadMetadata()
    {
      throw new Error_BadMetadata("");
    }

    private static string BuildXmlRepresentation(string[] baseUrls, ArrayList properties, ICollection entries)
    {
      ToXmlDataDv data = new ToXmlDataDv();
      data.BaseUris = new ArrayList(baseUrls);
      data.DesiredProperties = properties;
      data.IsRecursive = false;
      return MediaBuilder.BuildDidl(ToXmlFormatter.DefaultFormatter, data, entries);
    }

    private static string BuildXmlRepresentation(ArrayList baseUrls, ArrayList properties, ICollection entries)
    {
      ToXmlDataDv data = new ToXmlDataDv();
      if (baseUrls.Count == 0)
      {
        throw new ArgumentException("MediaServerDevice2.BuildXmlRepresentation() requires that 'baseUrls' be non-empty.");
      }
      foreach (string str in baseUrls)
      {
      }
      data.BaseUri = null;
      data.BaseUris = baseUrls;
      data.DesiredProperties = properties;
      data.IsRecursive = false;
      return MediaBuilder.BuildDidl(ToXmlFormatter.DefaultFormatter, data, entries);
    }

    private static string BuildXmlRepresentation(string baseUrl, ArrayList properties, ICollection entries)
    {
      ToXmlDataDv data = new ToXmlDataDv();
      data.BaseUri = baseUrl;
      data.DesiredProperties = properties;
      data.IsRecursive = false;
      return MediaBuilder.BuildDidl(ToXmlFormatter.DefaultFormatter, data, entries);
    }

    private uint CreateTransferId(HTTPSession session)
    {
      uint hashCode = (uint)session.GetHashCode();
      m_LockHttpTransfers.AcquireWriterLock(-1);
      while (m_HttpTransfers.ContainsKey(hashCode))
      {
        hashCode++;
      }
      m_LockHttpTransfers.ReleaseWriterLock();
      return hashCode;
    }

    public void Dispose()
    {
      m_Root.OnContainerChanged -= Sink_ContainerChanged;
      m_LFT.OnExpired -= Sink_OnExpired;
    }

    private long ExtractContentLength(HTTPMessage msg)
    {
      long num = 0L;
      string tag = msg.GetTag("CONTENT-LENGTH");
      try
      {
        num = long.Parse(tag);
      }
      catch
      {
      }
      return num;
    }

    private void FireHttpTransfersChange()
    {
      m_LockHttpTransfers.AcquireReaderLock(-1);
      int num = 0;
      ICollection values = m_HttpTransfers.Values;
      StringBuilder builder = new StringBuilder();
      foreach (HttpTransfer transfer in values)
      {
        if (transfer.ImportExportTransfer)
        {
          uint transferID = transfer.TransferID;
          if (num > 0)
          {
            builder.AppendFormat(",{0}", transferID.ToString());
          }
          else
          {
            builder.AppendFormat("{0}", transferID.ToString());
          }
          num++;
        }
      }
      string strA = ContentDirectory.Evented_TransferIDs;
      string strB = builder.ToString();
      m_LockHttpTransfers.ReleaseReaderLock();
      if (OnHttpTransfersChanged != null)
      {
        if (string.Compare(strA, strB) != 0)
        {
          ContentDirectory.Evented_TransferIDs = strB;
        }
        OnHttpTransfersChanged(this);
      }
    }

    private void FireStatsChange()
    {
      if (OnStatsChanged != null)
      {
        OnStatsChanged(this);
      }
    }

    private string GetBaseUrlByInterface()
    {
      IPEndPoint receiver = ConnectionManager.GetUPnPService().GetReceiver();
      StringBuilder builder = new StringBuilder(0x23);
      builder.AppendFormat("http://{0}:{1}/{2}", receiver.Address.ToString(), receiver.Port.ToString(), m_VirtualDirName);
      return builder.ToString();
    }

    private string[] GetBaseUrlsByInterfaces()
    {
      string baseUrlByInterface = GetBaseUrlByInterface();
      if (Device.LocalIPEndPoints != null)
      {
        IPEndPoint[] localIPEndPoints = Device.LocalIPEndPoints;
        string[] strArray = new string[localIPEndPoints.Length];
        int index = -1;
        for (int i = 0; i < localIPEndPoints.Length; i++)
        {
          StringBuilder builder = new StringBuilder(localIPEndPoints[i].ToString().Length * 2);
          builder.AppendFormat("http://{0}/{1}", localIPEndPoints[i].ToString(), m_VirtualDirName);
          strArray[i] = builder.ToString();
          if (string.Compare(builder.ToString(), baseUrlByInterface, true) == 0)
          {
            index = i;
          }
        }
        if ((index >= 0) && (index > 0))
        {
          string str2 = strArray[index];
          strArray[index] = strArray[0];
          strArray[0] = str2;
        }
        return strArray;
      }
      return new string[] { "127.0.0.1" };
    }

    private IDvMedia GetCdsEntry(string id)
    {
      IDvMedia media = _GetEntry(id);
      if (media == null)
      {
        throw new Error_NoSuchObject("(" + id + ")");
      }
      return media;
    }

    private int GetConnectionID()
    {
      bool flag = true;
      bool flag2 = false;
      int nextConnId = NextConnId;
      while (flag)
      {
        if (NextConnId < MaxConnections)
        {
          NextConnId++;
        }
        else
        {
          NextConnId = -1;
        }
        if (!m_Connections.ContainsKey(NextConnId))
        {
          flag = false;
        }
        else if (NextConnId == nextConnId)
        {
          flag2 = true;
          flag = false;
        }
      }
      if (flag2)
      {
        throw new Error_MaximumConnectionsExceeded("");
      }
      return NextConnId;
    }

    private DvMediaContainer2 GetContainer(string id)
    {
      IDvMedia descendent = (IDvMedia)GetDescendent(id);
      if ((descendent != null) && descendent.IsContainer)
      {
        return (DvMediaContainer2)descendent;
      }
      return null;
    }

    private IUPnPMedia GetDescendent(string id)
    {
      if (id == "0")
      {
        return m_Root;
      }
      IUPnPMedia target = null;
      WeakReference reference = (WeakReference)m_Cache[id];
      if (reference != null && reference.IsAlive)
      {
        target = (IUPnPMedia)reference.Target;
      }
      if (target == null)
      {
        target = m_Root.GetDescendent(id, m_Cache);
      }
      GC.Collect();
      return target;
    }

    private ArrayList GetFilters(string filters)
    {
      ArrayList list = new ArrayList();
      filters = filters.Trim();
      if (filters == "")
      {
        return null;
      }
      if ((filters != ",") && (filters.IndexOf('*') < 0))
      {
        DText text = new DText();
        text.ATTRMARK = ",";
        text[0] = filters;
        int num = text.DCOUNT();
        bool flag = false;
        bool flag2 = false;
        for (int i = 1; i <= num; i++)
        {
          string strA = text[i].Trim();
          if (strA == "res")
          {
            flag2 = true;
          }
          if (strA.StartsWith("@"))
          {
            strA = strA.Substring(1);
            if (string.Compare(strA, CdsTags[_ATTRIB.parentID], true) == 0)
            {
              list.Add("item@parentID");
              list.Add("container@parentID");
            }
            else if (string.Compare(strA, CdsTags[_ATTRIB.childCount], true) == 0)
            {
              list.Add("container@childCount");
            }
            else if (string.Compare(strA, CdsTags[_ATTRIB.bitrate], true) == 0)
            {
              flag = true;
              list.Add("res@bitrate");
            }
            else if (string.Compare(strA, CdsTags[_ATTRIB.bitsPerSample], true) == 0)
            {
              flag = true;
              list.Add("res@bitsPerSample");
            }
            else if (string.Compare(strA, CdsTags[_ATTRIB.colorDepth], true) == 0)
            {
              flag = true;
              list.Add("res@colorDepth");
            }
            else if (string.Compare(strA, CdsTags[_ATTRIB.duration], true) == 0)
            {
              flag = true;
              list.Add("res@duration");
            }
            else if (string.Compare(strA, CdsTags[_ATTRIB.importUri], true) == 0)
            {
              flag = true;
              list.Add("res@importUri");
            }
            else if (string.Compare(strA, CdsTags[_ATTRIB.nrAudioChannels], true) == 0)
            {
              flag = true;
              list.Add("res@nrAudioChannels");
            }
            else if (string.Compare(strA, CdsTags[_ATTRIB.protocolInfo], true) == 0)
            {
              flag = true;
            }
            else if (string.Compare(strA, CdsTags[_ATTRIB.protection], true) == 0)
            {
              flag = true;
              list.Add("res@protection");
            }
            else if (string.Compare(strA, CdsTags[_ATTRIB.resolution], true) == 0)
            {
              flag = true;
              list.Add("res@resolution");
            }
            else if (string.Compare(strA, CdsTags[_ATTRIB.sampleFrequency], true) == 0)
            {
              flag = true;
              list.Add("res@sampleFrequency");
            }
            else if (string.Compare(strA, CdsTags[_ATTRIB.size], true) == 0)
            {
              flag = true;
              list.Add("res@size");
            }
            else if (string.Compare(strA, CdsTags[_ATTRIB.name], true) == 0)
            {
              list.Add("upnp:class");
              list.Add("upnp:class@name");
              list.Add("upnp:searchClass");
              list.Add("upnp:searchClass@name");
            }
          }
          else
          {
            list.Add(strA);
          }
        }
        if (!(!flag || flag2))
        {
          list.Add("res");
        }
      }
      return list;
    }

    private DvMediaItem2 GetItem(string id)
    {
      IDvMedia descendent = (IDvMedia)GetDescendent(id);
      if ((descendent != null) && descendent.IsItem)
      {
        return (DvMediaItem2)descendent;
      }
      return null;
    }

    private void GetObjectResourceIDS(Uri theUri, out string objectID, out string resourceID)
    {
      objectID = "";
      resourceID = "";
      try
      {
        string str = theUri.ToString();
        string str2 = "/" + m_VirtualDirName + "/";
        int index = str.IndexOf(str2);
        string str3 = str.Substring(index + str2.Length);
        DText text = new DText();
        text.ATTRMARK = "/";
        text[0] = str3;
        resourceID = text[1];
        objectID = text[2];
      }
      catch
      {
      }
    }

    private ProtocolInfoString[] GetProtocolInfoSet(bool sourceProtocolInfo)
    {
      ArrayList sourceProtocolInfoSet;
      ReaderWriterLock lockSourceProtocolInfo;
      if (sourceProtocolInfo)
      {
        sourceProtocolInfoSet = m_SourceProtocolInfoSet;
        lockSourceProtocolInfo = m_LockSourceProtocolInfo;
      }
      else
      {
        sourceProtocolInfoSet = m_SinkProtocolInfoSet;
        lockSourceProtocolInfo = m_LockSinkProtocolInfo;
      }
      lockSourceProtocolInfo.AcquireReaderLock(-1);
      ProtocolInfoString[] strArray = new ProtocolInfoString[sourceProtocolInfoSet.Count];
      for (int i = 0; i < sourceProtocolInfoSet.Count; i++)
      {
        strArray[i] = (ProtocolInfoString)sourceProtocolInfoSet[i];
      }
      lockSourceProtocolInfo.ReleaseReaderLock();
      return strArray;
    }

    private void GetRequest_OnHeaderReceiveSink(HTTPSession WebSession, HTTPMessage msg, Stream stream)
    {
      long num = ExtractContentLength(msg);
      SessionData stateObject = (SessionData)WebSession.StateObject;
      if (stateObject.Transfers.Count != 1)
      {
        throw new Error_TransferProblem(0, null);
      }
      HttpTransfer transfer = (HttpTransfer)stateObject.Transfers.Peek();
      transfer.m_TransferSize = num;
      WebSession.OnHeader -= new HTTPSession.ReceiveHeaderHandler(GetRequest_OnHeaderReceiveSink);
    }

    private IDvResource GetResource(string objectID, string resourceID)
    {
      IDvMedia media = _GetEntry(objectID);
      if (media == null)
      {
        return null;
      }
      if (media.GetType() == new DvMediaContainer2().GetType())
      {
        DvMediaContainer2 container = (DvMediaContainer2)media;
        return container.GetResource(resourceID);
      }
      if (media.GetType() != new DvMediaItem2().GetType())
      {
        throw new ApplicationException("Found non-DvMediaxxx in content hierarchy.");
      }
      DvMediaItem2 item = (DvMediaItem2)media;
      return item.GetResource(resourceID);
    }

    public UPnPDevice GetUPnPDevice()
    {
      return Device;
    }

    private void HandleGetOrHeadRequest(HTTPMessage msg, HTTPSession session)
    {
      bool flag = true;
      Exception exception = null;
      string resourceID = null;
      string objectID = null;
      try
      {
        DText text = new DText();
        text.ATTRMARK = "/";
        text[0] = msg.DirectiveObj;
        resourceID = text[2];
        objectID = text[3];
        IDvResource res = GetResource(objectID, resourceID);
        if (res == null)
        {
          throw new Error_GetRequestError(msg.DirectiveObj, null);
        }
        string aUTOMAPFILE = MediaResource.AUTOMAPFILE;
        string path = res.ContentUri.Substring(aUTOMAPFILE.Length);
        string mimeType = res.ProtocolInfo.MimeType;
        switch (mimeType)
        {
          case null:
          case "":
          case "*":
            throw new Error_GetRequestError(msg.DirectiveObj, res);
        }
        if (Directory.Exists(path))
        {
          throw new Error_GetRequestError(msg.DirectiveObj, res);
        }
        FileNotMapped fileNotMapped = new FileNotMapped();
        fileNotMapped.RequestedResource = res;
        fileNotMapped.LocalInterface = session.Source.ToString();
        fileNotMapped.RedirectedStream = null;
        if (File.Exists(path))
        {
          fileNotMapped.RedirectedStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        else
        {
          try
          {
            if (OnFileNotMapped != null)
            {
              OnFileNotMapped(this, fileNotMapped);
            }
          }
          catch (Exception)
          {
            fileNotMapped.RedirectedStream = null;
          }
        }
        if (fileNotMapped.RedirectedStream != null)
        {
          lock (session)
          {
            string tag;
            ArrayList list;
            long contentLength = -1L;
            if (fileNotMapped.OverrideRedirectedStreamLength)
            {
              contentLength = fileNotMapped.ExpectedStreamLength;
            }
            else
            {
              contentLength = fileNotMapped.RedirectedStream.Length;
            }
            if (string.Compare(msg.Directive, "HEAD", true) == 0)
            {
              HTTPMessage packet = new HTTPMessage();
              packet.StatusCode = 200;
              packet.StatusData = "OK";
              packet.ContentType = mimeType;
              if (contentLength >= 0L)
              {
                packet.OverrideContentLength = true;
                tag = msg.GetTag("RANGE");
                if (string.IsNullOrEmpty(tag))
                {
                  packet.AddTag("CONTENT-LENGTH", contentLength.ToString());
                  packet.AddTag("ACCEPT-RANGES", "bytes");
                }
                else
                {
                  list = new ArrayList();
                  packet.StatusCode = 0xce;
                  AddRangeSets(list, tag.Trim().ToLower(), contentLength);
                  if (list.Count == 1)
                  {
                    string[] strArray = new string[] { "bytes ", ((HTTPSession.Range)list[0]).Position.ToString(), "-", ((int)((((HTTPSession.Range)list[0]).Position + ((HTTPSession.Range)list[0]).Length) - 1L)).ToString(), "/", contentLength.ToString() };
                    packet.AddTag("Content-Range", string.Concat(strArray));
                    packet.AddTag("Content-Length", ((HTTPSession.Range)list[0]).Length.ToString());
                  }
                }
              }
              else
              {
                packet.AddTag("ACCEPT-RANGES", "none");
              }
              session.Send(packet);
              flag = false;
            }
            else
            {
              list = new ArrayList();
              tag = msg.GetTag("RANGE");
              if (((tag == null) || (tag != "")) && (contentLength >= 0L))
              {
                AddRangeSets(list, tag.Trim().ToLower(), contentLength);
              }
              HttpTransfer transferInfo = new HttpTransfer(false, false, session, res, fileNotMapped.RedirectedStream, contentLength);
              AddTransfer(session, transferInfo);
              if (list.Count > 0)
              {
                session.SendStreamObject(fileNotMapped.RedirectedStream, (HTTPSession.Range[])list.ToArray(typeof(HTTPSession.Range)), mimeType);
              }
              else
              {
                fileNotMapped.RedirectedStream.Seek(0L, SeekOrigin.Begin);
                if (contentLength >= 0L)
                {
                  session.SendStreamObject(fileNotMapped.RedirectedStream, contentLength, mimeType);
                }
                else
                {
                  session.SendStreamObject(fileNotMapped.RedirectedStream, mimeType);
                }
              }
              flag = false;
            }
          }
        }
      }
      catch (Exception exception3)
      {
        exception = exception3;
      }
      if (flag)
      {
        StringBuilder builder = new StringBuilder();
        builder.Append("File not found.");
        builder.AppendFormat("\r\n\tRequested: \"{0}\"", msg.DirectiveObj);
        if (objectID != null)
        {
          builder.AppendFormat("\r\n\tObjectID=\"{0}\"", objectID);
        }
        if (resourceID != null)
        {
          builder.AppendFormat("\r\n\tResourceID=\"{0}\"", resourceID);
        }
        Error_GetRequestError error = exception as Error_GetRequestError;
        if (error != null)
        {
          builder.Append("\r\n");
          IUPnPMedia media = _GetEntry(objectID);
          if (media == null)
          {
            builder.AppendFormat("\r\n\tCould not find object with ID=\"{0}\"", objectID);
          }
          else
          {
            builder.AppendFormat("\r\n\tFound object with ID=\"{0}\"", objectID);
            builder.Append("\r\n---Metadata---\r\n");
            builder.Append(media.ToDidl());
          }
          builder.Append("\r\n");
          if (error.Resource == null)
          {
            builder.Append("\r\n\tResource is null.");
          }
          else
          {
            builder.Append("\r\n\tResource is not null.");
            string contentUri = error.Resource.ContentUri;
            if (contentUri == null)
            {
              builder.Append("\r\n\t\tContentUri of resource is null.");
            }
            else if (contentUri == "")
            {
              builder.Append("\r\n\t\tContentUri of resource is empty.");
            }
            else
            {
              builder.AppendFormat("\r\n\t\tContentUri of resource is \"{0}\"", contentUri);
            }
          }
        }
        if (exception != null)
        {
          builder.Append("\r\n");
          Exception innerException = exception;
          builder.Append("\r\n!!! Exception information !!!");
          while (innerException != null)
          {
            builder.AppendFormat("\r\nMessage=\"{0}\".\r\nStackTrace=\"{1}\"", innerException.Message, innerException.StackTrace);
            innerException = innerException.InnerException;
            if (innerException != null)
            {
              builder.Append("\r\n---InnerException---");
            }
          }
        }
        HTTPMessage message2 = new HTTPMessage();
        message2.StatusCode = 0x194;
        message2.StatusData = "File not found";
        message2.StringBuffer = builder.ToString();
        session.Send(message2);
      }
    }

    private void HandlePostedFileToServer(HTTPMessage msg, HTTPSession WebSession)
    {
      DText text = new DText();
      text.ATTRMARK = "/";
      text[0] = msg.DirectiveObj;
      string resourceID = text[2];
      string objectID = text[3];
      IDvResource resource = GetResource(objectID, resourceID);
      WebSession.UserStream = null;
      if ((resource != null) && resource.AllowImport)
      {
        if (OnRequestSaveBinary == null)
        {
          OnRequestSaveBinary(this, resource);
        }
        string path = resource.ContentUri.Substring(MediaResource.AUTOMAPFILE.Length);
        string str5 = MimeTypes.MimeToExtension(msg.ContentType);
        if (!path.EndsWith(str5))
        {
          path = path + str5;
        }
        long expectedLength = 0L;
        try
        {
          expectedLength = ExtractContentLength(msg);
        }
        catch
        {
        }
        FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 0x1000);
        WebSession.UserStream = stream;
        HttpTransfer transferInfo = new HttpTransfer(true, true, WebSession, resource, WebSession.UserStream, expectedLength);
        AddTransfer(WebSession, transferInfo);
      }
    }

    private void MarkTransferForRemoval(HTTPSession TheSession, Stream stream)
    {
      m_LockHttpTransfers.AcquireWriterLock(-1);
      SessionData stateObject = (SessionData)TheSession.StateObject;
      HttpTransfer transfer = (HttpTransfer)stateObject.Transfers.Dequeue();
      stateObject.Completed++;
      uint transferID = transfer.TransferID;
      if (m_HttpTransfers.ContainsKey(transferID))
      {
        HttpTransfer transfer2 = (HttpTransfer)m_HttpTransfers[transferID];
        if (transfer2 != transfer)
        {
          throw new ApplicationException("Bad Evil. The transfers must match.");
        }
        if (transfer2.Stream != stream)
        {
          throw new ApplicationException("Bad Evil. The streams need to match too.");
        }
        transfer2.Close(false);
        if (transfer2.Incoming)
        {
          transfer2.Resource.CheckLocalFileExists();
        }
      }
      m_LockHttpTransfers.ReleaseWriterLock();
      if (transfer == null)
      {
        throw new ApplicationException("Bad evil. We should always have an HttpTransfer object to remove.");
      }
      m_LFT.Add(transfer, 40);
    }

    private void ObtainBranchIDs(IUPnPMedia branch, StringBuilder newIds)
    {
      if (newIds.Length == 0)
      {
        newIds.Append(branch.ID);
      }
      else
      {
        newIds.AppendFormat(", {0}", branch.ID);
      }
      IMediaContainer container = branch as IMediaContainer;
      if (container != null)
      {
        foreach (IUPnPMedia media in container.CompleteList)
        {
          ObtainBranchIDs(media, newIds);
        }
      }
    }

    private void RecurseNewBranches(IList newBranches, StringBuilder newIds, XmlTextWriter resultXml)
    {
      string baseUrlByInterface = GetBaseUrlByInterface();
      foreach (IUPnPMedia media in newBranches)
      {
        ObtainBranchIDs(media, newIds);
        ToXmlDataDv data = new ToXmlDataDv();
        data.BaseUri = baseUrlByInterface;
        data.DesiredProperties = new ArrayList(0);
        data.IsRecursive = true;
        media.ToXml(ToXmlFormatter.DefaultFormatter, data, resultXml);
      }
    }

    private void RemoveConnection(Connection theConnection)
    {
      m_LockConnections.AcquireWriterLock(-1);
      m_Connections.Remove(theConnection.ConnectionId);
      UpdateConnections();
      m_LockConnections.ReleaseWriterLock();
    }

    private void RemoveTransfer(HttpTransfer transferInfo)
    {
      uint transferId = transferInfo.m_TransferId;
      m_LockHttpTransfers.AcquireWriterLock(-1);
      bool flag = false;
      if (m_HttpTransfers.ContainsKey(transferId))
      {
        HttpTransfer transfer = (HttpTransfer)m_HttpTransfers[transferId];
        if (transfer == transferInfo)
        {
          m_HttpTransfers.Remove(transferId);
        }
        else
        {
          flag = true;
        }
      }
      else
      {
        flag = true;
      }
      m_LockHttpTransfers.ReleaseWriterLock();
      if (flag)
      {
        throw new Error_TransferProblem(transferId, transferInfo);
      }
      FireHttpTransfersChange();
    }

    private void SetupSessionForTransfer(HTTPSession session)
    {
      if (session.StateObject == null)
      {
        session.StateObject = new SessionData();
        session.OnClosed += new HTTPSession.SessionHandler(WebSession_OnSessionClosed);
        session.OnStreamDone += new HTTPSession.StreamDoneHandler(WebSession_OnStreamDone);
      }
    }

    private void Sink_ContainerChanged(DvRootContainer2 sender, DvMediaContainer2 thisChanged)
    {
      Lock_SystemUpdateID.WaitOne();
      ContentDirectory.Evented_SystemUpdateID++;
      Lock_SystemUpdateID.ReleaseMutex();
      Lock_ContainerUpdateIDs.WaitOne();
      StringBuilder builder = new StringBuilder(20);
      builder.AppendFormat("{0}{1}{2}", thisChanged.ID, ",", thisChanged.UpdateID);
      ContentDirectory.Evented_ContainerUpdateIDs = builder.ToString();
      Lock_ContainerUpdateIDs.ReleaseMutex();
    }

    private void Sink_OnExpired(LifeTimeMonitor sender, object obj)
    {
      if (obj.GetType() != typeof(HttpTransfer))
      {
        throw new Error_TransferProblem(0, null);
      }
      RemoveTransfer((HttpTransfer)obj);
    }

    private void SinkCd_Browse(string objectID, DvContentDirectory.Enum_A_ARG_TYPE_BrowseFlag browseFlag, string filter, uint startingIndex, uint requestedCount, string sortCriteria, out string result, out uint numberReturned, out uint totalMatches, out uint updateID)
    {
      try
      {
        IList list;
        DvMediaContainer2 parent;
        numberReturned = 0;
        result = "";
        totalMatches = 0;
        if (requestedCount == 0)
        {
          requestedCount = Convert.ToUInt32(0x7fffffff);
        }
        IDvMedia cdsEntry = GetCdsEntry(objectID);
        if (cdsEntry.IsContainer && (browseFlag == DvContentDirectory.Enum_A_ARG_TYPE_BrowseFlag.BROWSEDIRECTCHILDREN))
        {
          parent = (DvMediaContainer2)cdsEntry;
          if (sortCriteria.Trim() == "")
          {
            list = parent.Browse(startingIndex, requestedCount, out totalMatches);
          }
          else
          {
            MediaSorter sorter = new MediaSorter(true, sortCriteria);
            list = parent.BrowseSorted(startingIndex, requestedCount, sorter, out totalMatches);
          }
          numberReturned = Convert.ToUInt32(list.Count);
          updateID = parent.UpdateID;
        }
        else
        {
          list = new ArrayList();
          list.Add(cdsEntry);
          totalMatches = 1;
          numberReturned = 1;
          IDvMedia media2 = cdsEntry;
          parent = media2 as DvMediaContainer2;
          if (parent == null)
          {
            parent = (DvMediaContainer2)media2.Parent;
          }
          updateID = parent.UpdateID;
        }
        ArrayList filters = GetFilters(filter);
        string[] baseUrlsByInterfaces = GetBaseUrlsByInterfaces();
        result = BuildXmlRepresentation(baseUrlsByInterfaces, filters, list);
      }
      catch (Exception exception)
      {
        Exception exception2 = new Exception("MediaServerDevice2.SinkCd_Browse()", exception);
        throw exception2;
      }
      m_Stats.Browse++;
      FireStatsChange();
    }

    private void SinkCd_CreateObject(string containerID, string Elements, out string objectID, out string Result)
    {
      try
      {
        DvMediaContainer2 parentContainer = GetContainer(containerID);
        if (parentContainer == null)
        {
          throw new Error_NoSuchObject("The container \"" + containerID + "\" does not exist.");
        }
        if (OnRequestAddBranch == null)
        {
          throw new Error_InvalidServerConfiguration("CreateObject() cannot be supported until the vendor configures the server correctly.");
        }
        IList mediaBranches = DvMediaBuilder2.BuildMediaBranches(Elements);
        IDvMedia[] addTheseBranches = new IDvMedia[mediaBranches.Count];
        for (int i = 0; i < mediaBranches.Count; i++)
        {
          addTheseBranches[i] = (IDvMedia)mediaBranches[i];
        }
        OnRequestAddBranch(this, parentContainer, ref addTheseBranches);
        foreach (IDvMedia media in addTheseBranches)
        {
          parentContainer.AddObject(media, false);
        }
        StringBuilder newIds = new StringBuilder(9 * addTheseBranches.Length);
        StringBuilder sb = null;
        StringWriter w = null;
        MemoryStream stream = null;
        XmlTextWriter xmlWriter = null;
        if (ENCODE_UTF8)
        {
          stream = new MemoryStream(XML_BUFFER_SIZE);
          xmlWriter = new XmlTextWriter(stream, Encoding.UTF8);
        }
        else
        {
          sb = new StringBuilder(XML_BUFFER_SIZE);
          w = new StringWriter(sb);
          xmlWriter = new XmlTextWriter(w);
        }
        xmlWriter.Formatting = Formatting.Indented;
        xmlWriter.Namespaces = true;
        MediaObject.WriteResponseHeader(xmlWriter);
        RecurseNewBranches(addTheseBranches, newIds, xmlWriter);
        MediaObject.WriteResponseFooter(xmlWriter);
        xmlWriter.Flush();
        objectID = newIds.ToString();
        if (ENCODE_UTF8)
        {
          int index = 3;
          int count = stream.ToArray().Length - index;
          Result = new UTF8Encoding(false, true).GetString(stream.ToArray(), index, count);
        }
        else
        {
          Result = sb.ToString();
        }
        xmlWriter.Close();
      }
      catch (Exception exception)
      {
        Exception exception2 = new Exception("MediaServerDevice2.CreateObject()", exception);
        throw exception2;
      }
      m_Stats.CreateObject++;
      FireStatsChange();
    }

    private void SinkCd_CreateReference(string containerID, string objectID, out string NewID)
    {
      DvMediaContainer2 parentContainer = GetContainer(containerID);
      if (parentContainer == null)
      {
        throw new Error_NoSuchContainer("(" + containerID + ")");
      }
      DvMediaItem2 item = GetItem(objectID);
      if (item == null)
      {
        throw new Error_NoSuchObject("(" + objectID + ")");
      }
      item.LockReferenceList();
      IDvItem item2 = item.CreateReference();
      item.UnlockReferenceList();
      IDvMedia[] addTheseBranches = new IDvMedia[] { item2 };
      if (OnRequestAddBranch == null)
      {
        throw new Error_InvalidServerConfiguration("CreateReference() cannot be supported until the vendor configures the server correctly.");
      }
      OnRequestAddBranch(this, parentContainer, ref addTheseBranches);
      NewID = item2.ID;
      m_Stats.CreateReference++;
      FireStatsChange();
    }

    private void SinkCd_DeleteResource(Uri ResourceURI)
    {
      string str;
      string str2;
      GetObjectResourceIDS(ResourceURI, out str, out str2);
      if ((str == "") || (str2 == ""))
      {
        throw new Error_NoSuchResource(ResourceURI.ToString());
      }
      IDvResource resource = GetResource(str, str2);
      if (resource == null)
      {
        throw new Error_NoSuchResource(ResourceURI.ToString());
      }
      if (OnRequestDeleteBinary == null)
      {
        throw new Error_InvalidServerConfiguration("DeleteResource() cannot be supported until the vendor configures the server correctly.");
      }
      OnRequestDeleteBinary(this, resource);
      if (resource.Owner.IsContainer)
      {
        ((DvMediaContainer2)resource.Owner).RemoveResource(resource);
      }
      else
      {
        ((DvMediaItem2)resource.Owner).RemoveResource(resource);
      }
      m_Stats.DeleteResource++;
      FireStatsChange();
    }

    private void SinkCd_DestroyObject(string objectID)
    {
      try
      {
        IDvMedia cdsEntry = GetCdsEntry(objectID);
        if (cdsEntry == null)
        {
          throw new Error_NoSuchObject(objectID);
        }
        if (cdsEntry.ID == "0")
        {
          throw new Error_RestrictedObject("Cannot destroy container 0");
        }
        DvMediaContainer2 parent = (DvMediaContainer2)cdsEntry.Parent;
        if (cdsEntry.IsRestricted)
        {
          throw new Error_RestrictedObject("Cannot destroy object " + objectID);
        }
        if (OnRequestRemoveBranch == null)
        {
          throw new Error_InvalidServerConfiguration("DestroyObject() cannot be supported until the vendor configures the server correctly.");
        }
        OnRequestRemoveBranch(this, parent, cdsEntry);
        m_Cache.Remove(objectID);
        parent.RemoveObject(cdsEntry);
        GC.Collect();
      }
      catch (Exception exception)
      {
        Exception exception2 = new Exception("MediaServer.SinkCd_DestroyObject()", exception);
        throw exception2;
      }
      m_Stats.DestroyObject++;
      FireStatsChange();
    }

    private void SinkCd_ExportResource(Uri SourceURI, Uri DestinationURI, out uint TransferID)
    {
      string str;
      string str2;
      TransferID = 0;
      Uri uri = DestinationURI;
      GetObjectResourceIDS(SourceURI, out str2, out str);
      if ((str2 == "") || (str == ""))
      {
        throw new Error_NoSuchResource(SourceURI.ToString());
      }
      IDvResource res = GetResource(str2, str);
      if (res == null)
      {
        throw new Error_NoSuchResource("");
      }
      Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      IPEndPoint remoteEP = null;
      if (uri.HostNameType == UriHostNameType.Dns)
      {
        remoteEP = new IPEndPoint(Dns.GetHostEntry(uri.Host).AddressList[0], uri.Port);
      }
      else
      {
        remoteEP = new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port);
      }
      if (remoteEP == null)
      {
        throw new UPnPCustomException(800, "Could not connect to the socket.");
      }
      string path = res.ContentUri.Substring(MediaResource.AUTOMAPFILE.Length);
      if (Directory.Exists(path))
      {
        throw new Error_NoSuchResource("The binary could not be found on the system.");
      }
      FileNotMapped fileNotMapped = new FileNotMapped();
      StringBuilder builder = new StringBuilder();
      builder.AppendFormat("{0}:{1}", SourceURI.Host, SourceURI.Port);
      fileNotMapped.LocalInterface = builder.ToString();
      fileNotMapped.RequestedResource = res;
      fileNotMapped.RedirectedStream = null;
      if (File.Exists(path))
      {
        fileNotMapped.RedirectedStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
      }
      else
      {
        try
        {
          if (OnFileNotMapped != null)
          {
            OnFileNotMapped(this, fileNotMapped);
          }
        }
        catch (Exception)
        {
          fileNotMapped.RedirectedStream = null;
        }
      }
      if (fileNotMapped.RedirectedStream == null)
      {
        throw new Error_NoSuchResource("The binary could not be found on the system.");
      }
      try
      {
        socket.Connect(remoteEP);
      }
      catch
      {
        throw new UPnPCustomException(800, "Could not connect to the remote address of " + remoteEP.ToString() + ":" + remoteEP.Port.ToString());
      }
      HTTPSession session = null;
      SetupSessionForTransfer(session);
      SessionData stateObject = (SessionData)session.StateObject;
      stateObject.HttpVer1_1 = true;
      HttpTransfer transferInfo = new HttpTransfer(false, true, session, res, fileNotMapped.RedirectedStream, fileNotMapped.RedirectedStream.Length);
      AddTransfer(session, transferInfo);
      session.PostStreamObject(fileNotMapped.RedirectedStream, uri.PathAndQuery, res.ProtocolInfo.MimeType);
      TransferID = transferInfo.m_TransferId;
      m_Stats.ExportResource++;
      FireStatsChange();
    }

    private void SinkCd_GetSearchCapabilities(out string SearchCaps)
    {
      SearchCaps = m_SearchCapabilities;
      m_Stats.GetSearchCapabilities++;
      FireStatsChange();
    }

    private void SinkCd_GetSortCapabilities(out string SortCaps)
    {
      SortCaps = m_SortCapabilities;
      m_Stats.GetSortCapabilities++;
      FireStatsChange();
    }

    private void SinkCd_GetSystemUpdateID(out uint id)
    {
      id = ContentDirectory.Evented_SystemUpdateID;
      m_Stats.GetSystemUpdateID++;
      FireStatsChange();
    }

    private void SinkCd_GetTransferProgress(uint TransferID, out DvContentDirectory.Enum_A_ARG_TYPE_TransferStatus TransferStatus, out string TransferLength, out string TransferTotal)
    {
      if (!m_HttpTransfers.ContainsKey(TransferID))
      {
        throw new Error_NoSuchFileTransfer("(" + TransferID.ToString() + ")");
      }
      HttpTransfer transfer = (HttpTransfer)m_HttpTransfers[TransferID];
      TransferLength = transfer.Position.ToString();
      TransferTotal = transfer.TransferSize.ToString();
      TransferStatus = transfer.TransferStatus;
      m_Stats.GetTransferProgress++;
      FireStatsChange();
    }

    private void SinkCd_ImportResource(Uri SourceURI, Uri DestinationURI, out uint TransferID)
    {
      string str;
      string str2;
      GetObjectResourceIDS(DestinationURI, out str, out str2);
      if ((str == "") || (str2 == ""))
      {
        throw new Error_NoSuchResource(DestinationURI.ToString());
      }
      if (!SourceURI.Scheme.ToLower().StartsWith("http"))
      {
        throw new Error_NonHttpImport(DestinationURI.ToString());
      }
      IDvResource resource = GetResource(str, str2);
      if (OnRequestSaveBinary == null)
      {
        throw new Error_InvalidServerConfiguration("ImportResource() cannot be supported until the vendor configures the server correctly.");
      }
      OnRequestSaveBinary(this, resource);
      IPAddress address;
      try
      {
        if (SourceURI.HostNameType == UriHostNameType.Dns)
        {
          address = new IPAddress(Dns.GetHostEntry(SourceURI.Host).AddressList[0].Address);
        }
        else
        {
          address = IPAddress.Parse(SourceURI.Host);
        }
      }
      catch
      {
        throw new Error_ConnectionProblem("Could parse or resolve the SourceURI IP address represented by" + SourceURI.ToString());
      }
      IPEndPoint remoteEP = new IPEndPoint(address, SourceURI.Port);
      Socket theSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      try
      {
        theSocket.Connect(remoteEP);
      }
      catch
      {
        throw new Error_ConnectionProblem("Could not connect to the remote URI " + DestinationURI.ToString());
      }
      string path = resource.ContentUri.Substring(MediaResource.AUTOMAPFILE.Length);
      if (Directory.Exists(path))
      {
        throw new Error_ImportError("System error. Resource has been mapped incorrectly. Cannot overwrite a directory with a binary.");
      }
      HTTPSession session = new HTTPSession(theSocket, null, null);
      SetupSessionForTransfer(session);
      session.OnHeader += new HTTPSession.ReceiveHeaderHandler(GetRequest_OnHeaderReceiveSink);
      try
      {
        session.UserStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
      }
      catch
      {
        throw new Error_ImportError("System busy. Could not open file from local system for writing.");
      }
      if (session.UserStream == null)
      {
        throw new Error_ImportError("System error. Cannot write to a null stream.");
      }
      SessionData stateObject = (SessionData)session.StateObject;
      stateObject.HttpVer1_1 = false;
      HTTPMessage packet = new HTTPMessage();
      packet.Directive = "GET";
      packet.DirectiveObj = HTTPMessage.UnEscapeString(SourceURI.PathAndQuery);
      packet.AddTag("HOST", remoteEP.ToString());
      packet.Version = "1.0";
      long expectedLength = 0L;
      HttpTransfer transferInfo = new HttpTransfer(true, true, session, resource, session.UserStream, expectedLength);
      AddTransfer(session, transferInfo);
      TransferID = transferInfo.m_TransferId;
      session.Send(packet);
      m_Stats.ImportResource++;
      FireStatsChange();
    }

    private void SinkCd_Search(string containerID, string searchCriteria, string filter, uint startingIndex, uint requestedCount, string sortCriteria, out string Result, out uint numberReturned, out uint totalMatches, out uint updateID)
    {
      try
      {
        IList list;
        MediaComparer comparer;
        numberReturned = 0;
        Result = "";
        totalMatches = 0;
        updateID = 0;
        IDvMedia cdsEntry = GetCdsEntry(containerID);
        if (!cdsEntry.IsContainer)
        {
          throw new Error_NoSuchContainer("(" + containerID + ")");
        }
        DvMediaContainer2 container = (DvMediaContainer2)cdsEntry;
        if (sortCriteria.Trim() == "")
        {
          comparer = new MediaComparer(searchCriteria);
          list = container.Search(comparer, startingIndex, requestedCount, out totalMatches);
        }
        else
        {
          MediaSorter sorter = new MediaSorter(true, sortCriteria);
          comparer = new MediaComparer(searchCriteria);
          list = container.SearchSorted(comparer, sorter, startingIndex, requestedCount, out totalMatches);
        }
        for (int i = 0; i < startingIndex; i++)
        {
          list.RemoveAt(0);
        }
        numberReturned = Convert.ToUInt32(list.Count);
        updateID = container.UpdateID;
        ArrayList filters = GetFilters(filter);
        string[] baseUrlsByInterfaces = GetBaseUrlsByInterfaces();
        Result = BuildXmlRepresentation(baseUrlsByInterfaces, filters, list);
      }
      catch (Exception exception)
      {
        Exception exception2 = new Exception("MediaServer.SinkCd_Search()", exception);
        throw exception2;
      }
      m_Stats.Search++;
      FireStatsChange();
    }

    private void SinkCd_StopTransferResource(uint TransferID)
    {
      if (!m_HttpTransfers.ContainsKey(TransferID))
      {
        throw new Error_NoSuchFileTransfer("(" + TransferID.ToString() + ")");
      }
      HttpTransfer transfer = (HttpTransfer)m_HttpTransfers[TransferID];
      bool flag = false;
      if ((transfer != null) && transfer.ImportExportTransfer)
      {
        flag = true;
        transfer.Close(true);
      }
      m_Stats.StopTransferResource++;
      if (!flag)
      {
        throw new Error_NoSuchFileTransfer("(" + TransferID.ToString() + ")");
      }
    }

    private void SinkCd_UpdateObject(string objectID, string currentTagValue, string newTagValue)
    {
      Exception exception;
      try
      {
        string str2;
        int index;
        IDvMedia media2;
        IDvMedia cdsEntry = GetCdsEntry(objectID);
        if (cdsEntry == null)
        {
          throw new Error_NoSuchObject("(" + objectID + ")");
        }
        if (cdsEntry.IsReference)
        {
          throw new UPnPCustomException(830, "This server will not allow UpdateObject() on a reference.");
        }
        DText text = new DText();
        DText text2 = new DText();
        text.ATTRMARK = ",";
        text2.ATTRMARK = ",";
        text[0] = currentTagValue;
        text2[0] = newTagValue;
        int num = text.DCOUNT();
        int num2 = text2.DCOUNT();
        if (num != num2)
        {
          throw new Error_ParameterMismatch("The number of tag/value pairs is not the same between currentTagValue and newTagValue.");
        }
        string baseUrlByInterface = GetBaseUrlByInterface();
        ArrayList properties = new ArrayList();
        ArrayList entries = new ArrayList();
        entries.Add(cdsEntry);
        try
        {
          str2 = BuildXmlRepresentation(baseUrlByInterface, properties, entries);
        }
        catch (Exception exception1)
        {
          exception = exception1;
          throw exception;
        }
        string xml = str2;
        for (int i = 1; i <= num; i++)
        {
          string str4 = text[i].Trim();
          string str5 = text2[i].Trim();
          if ((!str4.StartsWith("<") || !str4.EndsWith(">")) && (str4 != ""))
          {
            throw new Error_InvalidCurrentTagValue("Invalid args. (" + str4 + ") is not an xml element.");
          }
          index = xml.IndexOf(str4);
          if ((str4 != "") && (index < 0))
          {
            throw new Error_InvalidCurrentTagValue("Cannot find xml element (" + str4 + ").");
          }
          if (str4 == "")
          {
            StringBuilder builder;
            if (cdsEntry.IsContainer)
            {
              builder = new StringBuilder(str5.Length + 10);
              builder.AppendFormat("{0}</container>", new object[0]);
              xml = xml.Replace("</container>", builder.ToString());
            }
            else
            {
              builder = new StringBuilder(str5.Length + 10);
              builder.AppendFormat("{0}</item>", str5);
              xml = xml.Replace("</item>", builder.ToString());
            }
          }
          else if (str5 == "")
          {
            xml = xml.Replace(str4, "");
          }
          else
          {
            xml = xml.Replace(str4, str5);
          }
        }
        XmlDocument document = new XmlDocument();
        document.LoadXml(xml);
        XmlNode node = document.GetElementsByTagName(CdsTags[(Intel.UPNP.AV.CdsMetadata._ATTRIB)0])[0];
        XmlElement xmlElement = (XmlElement)node.ChildNodes[0];
        if (cdsEntry.IsContainer)
        {
          media2 = new DvMediaContainer2(xmlElement);
        }
        else
        {
          media2 = new DvMediaItem2(xmlElement);
        }
        foreach (IMediaResource resource in media2.Resources)
        {
          string contentUri = resource.ContentUri;
          index = contentUri.IndexOf(m_VirtualDirName);
          if (index > 0)
          {
            string str7 = contentUri.Substring(index + m_VirtualDirName.Length);
            DText text3 = new DText();
            text3.ATTRMARK = "/";
            text3[0] = str7;
            string resourceID = text3[2];
            string str9 = text3[3];
            Debug.Assert(str9 == objectID);
            IDvResource resource2 = GetResource(objectID, resourceID);
            resource[CdsTags[_RESATTRIB.importUri]] = null;
            resource.ContentUri = resource2.ContentUri;
          }
        }
        if (OnRequestChangeMetadata != null)
        {
          OnRequestChangeMetadata(this, cdsEntry, media2);
        }
        cdsEntry.UpdateObject(media2);
      }
      catch (Exception exception3)
      {
        exception = exception3;
        Exception exception2 = new Exception("MediaServer.SinkCd_UpdateObject()", exception);
        throw exception2;
      }
      m_Stats.UpdateObject++;
      FireStatsChange();
    }

    private void SinkCm_ConnectionComplete(int ConnectionID)
    {
      if (ConnectionID > 0x7fffffff)
      {
        throw new Error_InvalidConnection("(" + ConnectionID + ")");
      }
      int key = ConnectionID;
      m_LockConnections.AcquireWriterLock(-1);
      if (m_Connections.ContainsKey(key))
      {
        Connection theConnection = (Connection)m_Connections[key];
        RemoveConnection(theConnection);
      }
      m_Stats.ExportResource++;
      FireStatsChange();
    }

    private void SinkCm_GetCurrentConnectionIDs(out string ConnectionIDs)
    {
      ConnectionIDs = ConnectionManager.Evented_CurrentConnectionIDs;
      m_Stats.GetCurrentConnectionIDs++;
      FireStatsChange();
    }

    private void SinkCm_GetCurrentConnectionInfo(int ConnectionID, out int RcsID, out int AVTransportID, out string ProtocolInfo, out string PeerConnectionManager, out int PeerConnectionID, out DvConnectionManager.Enum_A_ARG_TYPE_Direction Direction, out DvConnectionManager.Enum_A_ARG_TYPE_ConnectionStatus Status)
    {
      if (!m_Connections.ContainsKey(ConnectionID))
      {
        throw new Error_InvalidConnection("(" + ConnectionID + ")");
      }
      Connection connection = (Connection)m_Connections[ConnectionID];
      RcsID = connection.RcsId;
      AVTransportID = connection.AVTransportId;
      ProtocolInfo = connection.ProtocolInfo.ToString();
      PeerConnectionManager = connection.PeerConnectionManager;
      PeerConnectionID = connection.PeerConnectionId;
      Direction = connection.Direction;
      Status = connection.Status;
      m_Stats.GetCurrentConnectionInfo++;
      FireStatsChange();
    }

    private void SinkCm_GetProtocolInfo(out string Source, out string Sink)
    {
      Source = ConnectionManager.Evented_SourceProtocolInfo;
      Sink = ConnectionManager.Evented_SinkProtocolInfo;
      m_Stats.GetProtocolInfo++;
      FireStatsChange();
    }

    private void SinkCm_PrepareForConnection(string RemoteProtocolInfo, string PeerConnectionManager, int PeerConnectionID, DvConnectionManager.Enum_A_ARG_TYPE_Direction Direction, out int ConnectionID, out int AVTransportID, out int RcsID)
    {
      bool flag = true;
      ProtocolInfoString protInfo = new ProtocolInfoString(RemoteProtocolInfo);
      ValidateConnectionRequest(protInfo, Direction);
      if ((EnableHttp && (string.Compare(protInfo.Protocol, "http-get", true) == 0)) && (Direction == DvConnectionManager.Enum_A_ARG_TYPE_Direction.OUTPUT))
      {
        flag = false;
      }
      ConnectionID = GetConnectionID();
      DvConnectionManager.Enum_A_ARG_TYPE_ConnectionStatus uNKNOWN = DvConnectionManager.Enum_A_ARG_TYPE_ConnectionStatus.UNKNOWN;
      if (flag)
      {
        if (OnCallPrepareForConnection == null)
        {
          throw new Error_InvalidServerConfiguration("PrepareForConnection() cannot be supported until the vendor configures the server correctly.");
        }
        OnCallPrepareForConnection(RemoteProtocolInfo, PeerConnectionManager, Direction, out AVTransportID, out RcsID, out uNKNOWN);
      }
      else
      {
        AVTransportID = -1;
        RcsID = -1;
      }
      Connection newConnection = new Connection(ConnectionID, PeerConnectionID, RcsID, AVTransportID, protInfo, PeerConnectionManager, Direction, uNKNOWN);
      AddConnection(newConnection);
      m_Stats.PrepareForConnection++;
      FireStatsChange();
    }

    public void Start()
    {
      Device.StartDevice();
    }

    public void Start(int portNumber)
    {
      Device.StartDevice(portNumber);
    }

    public void Stop()
    {
      Device.StopDevice();
    }

    private void UpdateConnections()
    {
      StringBuilder builder = new StringBuilder(m_Connections.Count * 50);
      int num = 0;
      foreach (int num2 in m_Connections.Keys)
      {
        if (num > 0)
        {
          builder.AppendFormat(", {0}", num2);
        }
        else
        {
          builder.AppendFormat("{0}", num2);
        }
        num++;
      }
      ConnectionManager.Evented_CurrentConnectionIDs = builder.ToString();
    }

    private void UpdateProtocolInfoSet(bool sourceProtocolInfo, ProtocolInfoString[] array)
    {
      ArrayList sourceProtocolInfoSet;
      ReaderWriterLock lockSourceProtocolInfo;
      if (sourceProtocolInfo)
      {
        sourceProtocolInfoSet = m_SourceProtocolInfoSet;
        lockSourceProtocolInfo = m_LockSourceProtocolInfo;
      }
      else
      {
        sourceProtocolInfoSet = m_SinkProtocolInfoSet;
        lockSourceProtocolInfo = m_LockSinkProtocolInfo;
      }
      lockSourceProtocolInfo.AcquireWriterLock(-1);
      StringBuilder builder = new StringBuilder();
      if (array != null)
      {
        sourceProtocolInfoSet.Clear();
        sourceProtocolInfoSet.AddRange(array);
        for (int i = 0; i < sourceProtocolInfoSet.Count; i++)
        {
          if (i > 0)
          {
            builder.Append(",");
          }
          builder.Append(array[i].ToString());
        }
      }
      if (sourceProtocolInfo)
      {
        ConnectionManager.Evented_SourceProtocolInfo = builder.ToString();
      }
      else
      {
        ConnectionManager.Evented_SinkProtocolInfo = builder.ToString();
      }
      lockSourceProtocolInfo.ReleaseWriterLock();
    }

    private void UpdateProtocolInfoSet(bool sourceProtocolInfo, string protocolInfoSet)
    {
      DText text = new DText();
      text.ATTRMARK = ",";
      text[0] = protocolInfoSet;
      int num = text.DCOUNT();
      ArrayList list = new ArrayList();
      for (int i = 1; i <= num; i++)
      {
        string protocolInfo = text[i].Trim();
        if (protocolInfo != "")
        {
          ProtocolInfoString str2 = new ProtocolInfoString("*:*:*:*");
          bool flag = false;
          try
          {
            str2 = new ProtocolInfoString(protocolInfo);
          }
          catch
          {
            flag = true;
          }
          if (!flag)
          {
            list.Add(str2);
          }
        }
      }
      ProtocolInfoString[] array = null;
      if (list.Count > 0)
      {
        array = (ProtocolInfoString[])list.ToArray(list[0].GetType());
      }
      UpdateProtocolInfoSet(sourceProtocolInfo, array);
    }

    private void ValidateConnectionRequest(ProtocolInfoString protInfo, DvConnectionManager.Enum_A_ARG_TYPE_Direction dir)
    {
      ArrayList sinkProtocolInfoSet;
      bool flag = true;
      if (dir == DvConnectionManager.Enum_A_ARG_TYPE_Direction.INPUT)
      {
        sinkProtocolInfoSet = m_SinkProtocolInfoSet;
      }
      else
      {
        if (dir != DvConnectionManager.Enum_A_ARG_TYPE_Direction.OUTPUT)
        {
          throw new Error_InvalidDirection("");
        }
        sinkProtocolInfoSet = m_SourceProtocolInfoSet;
      }
      foreach (ProtocolInfoString str in sinkProtocolInfoSet)
      {
        if (protInfo.Matches(str))
        {
          flag = false;
          break;
        }
      }
      if (flag)
      {
        throw new Error_IncompatibleProtocolInfo("(" + protInfo.ToString() + ")");
      }
    }

    private void WebServer_OnHeaderReceiveSink(UPnPDevice sender, HTTPMessage msg, HTTPSession WebSession, string VirtualDir)
    {
      SetupSessionForTransfer(WebSession);
      SessionData stateObject = (SessionData)WebSession.StateObject;
      if ((msg.Version == "1.0") || (msg.Version == "0.0"))
      {
        stateObject.HttpVer1_1 = false;
      }
      else
      {
        stateObject.HttpVer1_1 = true;
      }
    }

    private void WebServer_OnPacketReceiveSink(UPnPDevice sender, HTTPMessage msg, HTTPSession WebSession, string VirtualDir)
    {
      if (string.Compare(msg.Directive, "POST", true) == 0)
      {
        HandlePostedFileToServer(msg, WebSession);
      }
      else if ((string.Compare(msg.Directive, "GET", true) == 0) || (string.Compare(msg.Directive, "HEAD", true) == 0))
      {
        HandleGetOrHeadRequest(msg, WebSession);
      }
    }

    private void WebSession_OnSessionClosed(HTTPSession TheSession)
    {
      TheSession.CancelAllEvents();
    }

    private void WebSession_OnStreamDone(HTTPSession TheSession, Stream stream)
    {
      lock (TheSession)
      {
        SessionData stateObject = (SessionData)TheSession.StateObject;
        if (stateObject.Transfers.Count <= 0)
        {
          throw new ApplicationException("bad evil. Can't mark a stream for removal if there's nothing to remove.");
        }
        MarkTransferForRemoval(TheSession, stream);
        if (!stateObject.HttpVer1_1)
        {
        }
      }
    }

    // Properties
    public UPnPDevice _Device
    {
      get { return Device; }
    }

    public Connection[] Connections
    {
      get
      {
        m_LockConnections.AcquireReaderLock(-1);
        Connection[] connectionArray = new Connection[m_Connections.Count];
        int index = 0;
        foreach (uint num2 in m_Connections.Keys)
        {
          connectionArray[index] = (Connection)m_Connections[num2];
          index++;
        }
        m_LockConnections.ReleaseReaderLock();
        return connectionArray;
      }
    }

    public IList HttpTransfers
    {
      get
      {
        m_LockHttpTransfers.AcquireReaderLock(-1);
        ArrayList list = new ArrayList(m_HttpTransfers.Count);
        list.AddRange(m_HttpTransfers.Values);
        m_LockHttpTransfers.ReleaseReaderLock();
        return list;
      }
    }

    public DvMediaContainer2 Root
    {
      get { return m_Root; }
    }

    public string SearchCapabilities
    {
      get { return m_SearchCapabilities; }
      set { m_SearchCapabilities = value; }
    }

    public ProtocolInfoString[] SinkProtocolInfoSet
    {
      get
      {
        GetProtocolInfoSet(false);
        return null;
      }
      set
      {
        ProtocolInfoString[] array = value;
        UpdateProtocolInfoSet(false, array);
      }
    }

    public string SortCapabilities
    {
      get { return m_SortCapabilities; }
      set { m_SortCapabilities = value; }
    }

    public ProtocolInfoString[] SourceProtocolInfoSet
    {
      get
      {
        GetProtocolInfoSet(true);
        return null;
      }
      set
      {
        ProtocolInfoString[] array = value;
        UpdateProtocolInfoSet(true, array);
      }
    }

    public Statistics Stats
    {
      get { return m_Stats; }
    }

    public string VirtualDirName
    {
      get { return m_VirtualDirName; }
    }

    // Nested Types
    [StructLayout(LayoutKind.Sequential)]
    public struct Connection
    {
      public int ConnectionId;
      public int RcsId;
      public int AVTransportId;
      public ProtocolInfoString ProtocolInfo;
      public string PeerConnectionManager;
      public int PeerConnectionId;
      public DvConnectionManager.Enum_A_ARG_TYPE_Direction Direction;
      public DvConnectionManager.Enum_A_ARG_TYPE_ConnectionStatus Status;
      public Connection(int id, int peerId, int rcs, int avt, ProtocolInfoString prot, string peer, DvConnectionManager.Enum_A_ARG_TYPE_Direction dir, DvConnectionManager.Enum_A_ARG_TYPE_ConnectionStatus status)
      {
        if (id < 0)
        {
          throw new ApplicationException("ConnectionId cannot be negative.");
        }
        ConnectionId = id;
        PeerConnectionId = peerId;
        RcsId = rcs;
        AVTransportId = avt;
        ProtocolInfo = prot;
        PeerConnectionManager = peer;
        Direction = dir;
        Status = status;
      }
    }

    public delegate void Delegate_AddBranch(MediaServerDevice2 sender, DvMediaContainer2 parentContainer, ref IDvMedia[] addTheseBranches);

    public delegate void Delegate_ChangeMetadata(MediaServerDevice2 sender, IDvMedia oldObject, IDvMedia newObject);

    public delegate void Delegate_FileNotMappedHandler(MediaServerDevice2 sender, MediaServerDevice2.FileNotMapped fileNotMapped);

    public delegate void Delegate_MediaServerHandler(MediaServerDevice2 sender);

    public delegate void Delegate_ModifyBinary(MediaServerDevice2 sender, IDvResource resource);

    public delegate void Delegate_PrepareForConnection(string RemoteProtocolInfo, string PeerConnectionManager, DvConnectionManager.Enum_A_ARG_TYPE_Direction Direction, out int AVTransportID, out int RcsID, out DvConnectionManager.Enum_A_ARG_TYPE_ConnectionStatus status);

    public delegate void Delegate_RemoveBranch(MediaServerDevice2 sender, DvMediaContainer2 parentContainer, IDvMedia removeThisBranch);

    public class FileNotMapped
    {
      // Fields
      public long ExpectedStreamLength = -1L;
      public string LocalInterface;
      public bool OverrideRedirectedStreamLength = false;
      public Stream RedirectedStream;
      public IMediaResource RequestedResource;
    }

    public class HttpTransfer
    {
      // Fields
      private bool ClosedOrDone;
      internal bool CriticalError;
      public readonly IPEndPoint Destination;
      internal bool ImportExportTransfer;
      public readonly bool Incoming;
      private long lastKnownPos = 0L;
      internal uint m_TransferId;
      internal long m_TransferSize;
      public readonly IDvResource Resource;
      private HTTPSession Session;
      public readonly IPEndPoint Source;
      internal readonly Stream Stream;

      // Methods
      internal HttpTransfer(bool incoming, bool importExportTransfer, HTTPSession session, IDvResource res, Stream stream, long expectedLength)
      {
        Incoming = incoming;
        ImportExportTransfer = importExportTransfer;
        Session = session;
        Source = session.Source;
        Destination = session.Remote;
        Resource = res;
        Stream = stream;
        m_TransferSize = expectedLength;
        ClosedOrDone = false;
        CriticalError = session == null;
      }

      public void Close(bool deleteFile)
      {
        ClosedOrDone = true;
        lastKnownPos = Stream.Position;
        Session.CloseStreamObject(Stream);
        if ((Stream.GetType().ToString() == "System.IO.FileStream") && deleteFile)
        {
          FileStream stream = (FileStream)Stream;
          File.Delete(stream.Name);
        }
      }

      internal bool IsSessionMatch(HTTPSession session)
      {
        return (session == Session);
      }

      // Properties
      public long Position
      {
        get
        {
          try
          {
            if (ClosedOrDone)
            {
              return lastKnownPos;
            }
          }
          catch
          {
          }
          return Stream.Position;
        }
      }

      public uint TransferID
      {
        get { return m_TransferId; }
      }

      public long TransferSize
      {
        get { return m_TransferSize; }
      }

      public DvContentDirectory.Enum_A_ARG_TYPE_TransferStatus TransferStatus
      {
        get
        {
          if (CriticalError)
          {
            return DvContentDirectory.Enum_A_ARG_TYPE_TransferStatus.ERROR;
          }
          if ((TransferSize == Position) && (TransferSize != 0L))
          {
            return DvContentDirectory.Enum_A_ARG_TYPE_TransferStatus.COMPLETED;
          }
          if (ClosedOrDone)
          {
            return DvContentDirectory.Enum_A_ARG_TYPE_TransferStatus.STOPPED;
          }
          return DvContentDirectory.Enum_A_ARG_TYPE_TransferStatus.IN_PROGRESS;
        }
      }
    }

    private class SessionData
    {
      // Fields
      public int Completed = 0;
      public bool HttpVer1_1;
      public int Requested = 0;
      public Queue Transfers = new Queue();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Statistics
    {
      public int Browse;
      public int ExportResource;
      public int StopTransferResource;
      public int DestroyObject;
      public int UpdateObject;
      public int GetSystemUpdateID;
      public int GetTransferProgress;
      public int CreateObject;
      public int ImportResource;
      public int CreateReference;
      public int DeleteResource;
      public int ConnectionComplete;
      public int GetCurrentConnectionInfo;
      public int GetCurrentConnectionIDs;
      public int GetProtocolInfo;
      public int PrepareForConnection;
      public int GetSearchCapabilities;
      public int GetSortCapabilities;
      public int Search;
    }
  }
}
