#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;
using System.Drawing.Imaging;
using Intel.UPNP;
using Intel.Utilities;
using Intel.UPNP.AV;
using Intel.UPNP.AV.MediaServer;
using Intel.UPNP.AV.MediaServer.DV;
using Intel.UPNP.AV.MediaServer.CP;
using Intel.UPNP.AV.CdsMetadata;
//using MetadataParser;

namespace MediaPortal.UPnPServer
{
  [Serializable]
  public sealed class DvMediaItem2 : MediaItem, IDvItem, IMediaItem, IDvMedia, IUPnPMedia
  {
    // Fields
    [NonSerialized]
    private bool m_Deleting;
    [NonSerialized]
    private object m_LockReferences;
    [NonSerialized]
    private ArrayList m_ReferringItems;
    [NonSerialized]
    internal MediaItem m_RefItem;

    // Methods
    public DvMediaItem2()
    {
      this.m_LockReferences = new object();
      this.m_ReferringItems = null;
      this.m_Deleting = false;
      this.m_RefItem = null;
    }

    public DvMediaItem2(XmlElement xmlElement)
      : base(xmlElement)
    {
      this.m_LockReferences = new object();
      this.m_ReferringItems = null;
      this.m_Deleting = false;
      this.m_RefItem = null;
    }

    private DvMediaItem2(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
      this.m_LockReferences = new object();
      this.m_ReferringItems = null;
      this.m_Deleting = false;
      this.m_RefItem = null;
    }

    public override void AddResource(IMediaResource newResource)
    {
      IDvResource addThis = (IDvResource)newResource;
      base.AddResource(addThis);
      this.NotifyRootOfChange();
    }

    public override void AddResources(ICollection newResources)
    {
      foreach (IDvResource resource in newResources)
      {
      }
      base.AddResources(newResources);
      this.NotifyRootOfChange();
    }

    public static void AttachRefItem(DvMediaItem2 underlyingItem, DvMediaItem2 refItem)
    {
      underlyingItem.LockReferenceList();
      if (underlyingItem.m_ReferringItems == null)
      {
        underlyingItem.m_ReferringItems = new ArrayList();
      }
      underlyingItem.m_ReferringItems.Add(refItem);
      refItem.m_RefItem = underlyingItem;
      refItem.m_RefID = "";
      underlyingItem.UnlockReferenceList();
    }

    public void ChangeParent(IDvContainer diffParent)
    {
      DvMediaContainer2.ChangeParent2(this, (DvMediaContainer2)diffParent);
    }

    public override void CheckRuntimeBindings(StackTrace st)
    {
      if (this.Parent != null)
      {
        ((DvMediaContainer2)this.Parent).CheckRuntimeBindings(st);
      }
    }

    public DvMediaReference2 CreateDvMediaReference2()
    {
      lock (this.m_LockReferences)
      {
        if (this.m_Deleting)
        {
          throw new Error_PendingDeleteException(this);
        }
        DvMediaReference2 reference = new DvMediaReference2(this);
        if (this.m_ReferringItems == null)
        {
          this.m_ReferringItems = new ArrayList(1);
        }
        this.m_ReferringItems.Add(reference);
        return reference;
      }
    }

    public IDvItem CreateReference()
    {
      string uniqueId = MediaBuilder.GetUniqueId();
      return this.CreateReference(uniqueId);
    }

    public IDvItem CreateReference(string id)
    {
      lock (this.m_LockReferences)
      {
        if (this.m_Deleting)
        {
          throw new Error_PendingDeleteException(this);
        }
        DvMediaItem2 item = new DvMediaItem2();
        item.m_ID = id;
        item.m_RefItem = this;
        if (this.m_ReferringItems == null)
        {
          this.m_ReferringItems = new ArrayList(1);
        }
        this.m_ReferringItems.Add(item);
        item.m_Restricted = base.m_Restricted;
        item.SetClass(this.Class.ToString(), this.Class.FriendlyName);
        item.Title = this.Title;
        return item;
      }
    }

    protected override void FinishInitFromXml(XmlElement xmlElement)
    {
      ArrayList list;
      base.UpdateEverything(true, true, typeof(DvMediaResource), typeof(DvMediaItem2), typeof(DvMediaContainer2), xmlElement, out list);
      if (!base.m_ID.StartsWith(MediaBuilder.Seed))
      {
        base.m_ID = MediaBuilder.GetUniqueId();
      }
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      base.GetObjectData(info, context);
    }

    internal IDvResource GetResource(string resourceID)
    {
      IDvResource resource = null;
      base.m_LockResources.AcquireReaderLock(-1);
      if (base.m_Resources != null)
      {
        foreach (IDvResource resource2 in base.m_Resources)
        {
          if (resource2.ResourceID == resourceID)
          {
            resource = resource2;
            break;
          }
        }
      }
      base.m_LockResources.ReleaseReaderLock();
      return resource;
    }

    protected override void Init()
    {
      base.Init();
      this.m_LockReferences = new object();
      this.m_ReferringItems = null;
      this.m_Deleting = false;
      this.m_RefItem = null;
    }

    public void LockReferenceList()
    {
      Monitor.Enter(this.m_LockReferences);
    }

    public void NotifyPendingDelete()
    {
      lock (this.m_LockReferences)
      {
        this.m_Deleting = true;
        ArrayList list = new ArrayList();
        if (this.m_ReferringItems != null)
        {
          foreach (IDvItem item in this.m_ReferringItems)
          {
            IDvContainer parent = (IDvContainer)item.Parent;
            if (parent != null)
            {
              parent.RemoveObject(item);
            }
            else
            {
              list.Add(item);
            }
          }
          foreach (IDvItem item in list)
          {
            this.m_ReferringItems.Remove(item);
          }
        }
        this.m_ReferringItems = null;
        this.m_RefItem = null;
      }
    }

    public void NotifyRootOfChange()
    {
      DvMediaContainer2 parent = (DvMediaContainer2)base.m_Parent;
      if (parent != null)
      {
        parent.NotifyRootOfChange();
      }
      this.LockReferenceList();
      if (this.m_ReferringItems != null)
      {
        foreach (IDvItem item in this.m_ReferringItems)
        {
          parent = (DvMediaContainer2)item.Parent;
          if (parent != null)
          {
            parent.NotifyRootOfChange();
          }
        }
      }
      this.UnlockReferenceList();
    }

    public override void RemoveResource(IMediaResource removeThis)
    {
      base.RemoveResource(removeThis);
      this.NotifyRootOfChange();
    }

    public override void RemoveResources(ICollection removeThese)
    {
      base.RemoveResources(removeThese);
      this.NotifyRootOfChange();
    }

    protected override void Sink_OnMediaPropertiesChanged(MediaProperties sender, int stateNumber)
    {
      base.UpdateCache();
      this.NotifyRootOfChange();
    }

    public override void TrimToSize()
    {
      base.TrimToSize();
      lock (this.m_LockReferences)
      {
        if (this.m_ReferringItems != null)
        {
          this.m_ReferringItems.TrimToSize();
        }
      }
    }

    public void UnlockReferenceList()
    {
      Monitor.Exit(this.m_LockReferences);
    }

    public override void UpdateMetadata(XmlElement xmlElement)
    {
      ArrayList list;
      base.UpdateEverything(false, false, typeof(DvMediaResource), typeof(DvMediaItem2), typeof(DvMediaContainer2), xmlElement, out list);
    }

    public override void UpdateObject(IUPnPMedia newObj)
    {
      foreach (IDvResource resource in newObj.Resources)
      {
      }
      base.UpdateObject(newObj);
      this.NotifyRootOfChange();
    }

    public override void UpdateObject(XmlElement xmlElement)
    {
      ArrayList list;
      base.UpdateEverything(true, false, typeof(DvMediaResource), typeof(DvMediaItem2), typeof(DvMediaContainer2), xmlElement, out list);
    }

    public override void WriteInnerXml(ToXmlFormatter formatter, object data, XmlTextWriter xmlWriter)
    {
      ToXmlDataDv dv = (ToXmlDataDv)data;
      InnerXmlWriter.WriteInnerXml(this, new InnerXmlWriter.DelegateWriteProperties(InnerXmlWriter.WriteInnerXmlProperties), new InnerXmlWriter.DelegateShouldPrintResources(this.PrintResources), new InnerXmlWriter.DelegateWriteResources(InnerXmlWriterDv.WriteInnerXmlResources), new InnerXmlWriter.DelegateWriteDescNodes(InnerXmlWriter.WriteInnerXmlDescNodes), formatter, dv, xmlWriter);
    }

    // Properties
    public override string ID
    {
      get
      {
        return base.ID;
      }
      set
      {
        this.CheckRuntimeBindings(new StackTrace());
        if (string.Compare(value, base.m_ID) != 0)
        {
          base.m_ID = value;
          this.NotifyRootOfChange();
        }
      }
    }

    public bool IsDeletePending
    {
      get
      {
        lock (this.m_LockReferences)
        {
          return this.m_Deleting;
        }
      }
    }

    public override bool IsRestricted
    {
      get
      {
        return base.IsRestricted;
      }
      set
      {
        bool restricted = base.m_Restricted;
        base.m_Restricted = value;
        if (restricted != value)
        {
          this.NotifyRootOfChange();
        }
      }
    }

    public override IMediaContainer Parent
    {
      get
      {
        return base.Parent;
      }
      set
      {
        this.CheckRuntimeBindings(new StackTrace());
        DvMediaContainer2 container = (DvMediaContainer2)value;
        base.Parent = container;
      }
    }

    public IList ReferenceItems
    {
      get
      {
        this.LockReferenceList();
        ArrayList list = (ArrayList)this.m_ReferringItems.Clone();
        this.UnlockReferenceList();
        return list;
      }
    }

    public override IMediaItem RefItem
    {
      get
      {
        return this.m_RefItem;
      }
      set
      {
        this.CheckRuntimeBindings(new StackTrace());
        this.m_RefItem = (DvMediaItem2)value;
      }
    }
  }


}