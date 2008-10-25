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
using System.Collections;
using System.Threading;
using System.Runtime.Serialization;
using Intel.UPNP.AV.MediaServer.DV;
using Intel.UPNP.AV.CdsMetadata;

namespace Components.UPnPServer
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
      m_LockReferences = new object();
      m_ReferringItems = null;
      m_Deleting = false;
      m_RefItem = null;
    }

    public DvMediaItem2(XmlElement xmlElement)
      : base(xmlElement)
    {
      m_LockReferences = new object();
      m_ReferringItems = null;
      m_Deleting = false;
      m_RefItem = null;
    }

    private DvMediaItem2(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
      m_LockReferences = new object();
      m_ReferringItems = null;
      m_Deleting = false;
      m_RefItem = null;
    }

    public override void AddResource(IMediaResource newResource)
    {
      IDvResource addThis = (IDvResource)newResource;
      base.AddResource(addThis);
      NotifyRootOfChange();
    }

    public override void AddResources(ICollection newResources)
    {
      foreach (IDvResource resource in newResources)
      {
      }
      base.AddResources(newResources);
      NotifyRootOfChange();
    }

    public static void AttachRefItem(DvMediaItem2 underlayingItem, DvMediaItem2 refItem)
    {
      underlayingItem.LockReferenceList();
      if (underlayingItem.m_ReferringItems == null)
      {
        underlayingItem.m_ReferringItems = new ArrayList();
      }
      underlayingItem.m_ReferringItems.Add(refItem);
      refItem.m_RefItem = underlayingItem;
      refItem.m_RefID = "";
      underlayingItem.UnlockReferenceList();
    }

    public void ChangeParent(IDvContainer diffParent)
    {
      DvMediaContainer2.ChangeParent2(this, (DvMediaContainer2)diffParent);
    }

    public override void CheckRuntimeBindings(StackTrace st)
    {
      if (Parent != null)
      {
        ((DvMediaContainer2)Parent).CheckRuntimeBindings(st);
      }
    }

    public DvMediaReference2 CreateDvMediaReference2()
    {
      lock (m_LockReferences)
      {
        if (m_Deleting)
        {
          throw new Error_PendingDeleteException(this);
        }
        DvMediaReference2 reference = new DvMediaReference2(this);
        if (m_ReferringItems == null)
        {
          m_ReferringItems = new ArrayList(1);
        }
        m_ReferringItems.Add(reference);
        return reference;
      }
    }

    public IDvItem CreateReference()
    {
      string uniqueId = MediaBuilder.GetUniqueId();
      return CreateReference(uniqueId);
    }

    public IDvItem CreateReference(string id)
    {
      lock (m_LockReferences)
      {
        if (m_Deleting)
        {
          throw new Error_PendingDeleteException(this);
        }
        DvMediaItem2 item = new DvMediaItem2();
        item.m_ID = id;
        item.m_RefItem = this;
        if (m_ReferringItems == null)
        {
          m_ReferringItems = new ArrayList(1);
        }
        m_ReferringItems.Add(item);
        item.m_Restricted = base.m_Restricted;
        item.SetClass(Class.ToString(), Class.FriendlyName);
        item.Title = Title;
        return item;
      }
    }

    protected override void FinishInitFromXml(XmlElement xmlElement)
    {
      ArrayList list;
      UpdateEverything(true, true, typeof(DvMediaResource), typeof(DvMediaItem2), typeof(DvMediaContainer2), xmlElement, out list);
      if (!m_ID.StartsWith(MediaBuilder.Seed))
      {
        m_ID = MediaBuilder.GetUniqueId();
      }
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      base.GetObjectData(info, context);
    }

    internal IDvResource GetResource(string resourceID)
    {
      IDvResource resource = null;
      m_LockResources.AcquireReaderLock(-1);
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
      m_LockResources.ReleaseReaderLock();
      return resource;
    }

    protected override void Init()
    {
      base.Init();
      m_LockReferences = new object();
      m_ReferringItems = null;
      m_Deleting = false;
      m_RefItem = null;
    }

    public void LockReferenceList()
    {
      Monitor.Enter(m_LockReferences);
    }

    public void NotifyPendingDelete()
    {
      lock (m_LockReferences)
      {
        m_Deleting = true;
        ArrayList list = new ArrayList();
        if (m_ReferringItems != null)
        {
          foreach (IDvItem item in m_ReferringItems)
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
            m_ReferringItems.Remove(item);
          }
        }
        m_ReferringItems = null;
        m_RefItem = null;
      }
    }

    public void NotifyRootOfChange()
    {
      DvMediaContainer2 parent = (DvMediaContainer2)m_Parent;
      if (parent != null)
      {
        parent.NotifyRootOfChange();
      }
      LockReferenceList();
      if (m_ReferringItems != null)
      {
        foreach (IDvItem item in m_ReferringItems)
        {
          parent = (DvMediaContainer2)item.Parent;
          if (parent != null)
          {
            parent.NotifyRootOfChange();
          }
        }
      }
      UnlockReferenceList();
    }

    public override void RemoveResource(IMediaResource removeThis)
    {
      base.RemoveResource(removeThis);
      NotifyRootOfChange();
    }

    public override void RemoveResources(ICollection removeThese)
    {
      base.RemoveResources(removeThese);
      NotifyRootOfChange();
    }

    protected override void Sink_OnMediaPropertiesChanged(MediaProperties sender, int stateNumber)
    {
      UpdateCache();
      NotifyRootOfChange();
    }

    public override void TrimToSize()
    {
      base.TrimToSize();
      lock (m_LockReferences)
      {
        if (m_ReferringItems != null)
        {
          m_ReferringItems.TrimToSize();
        }
      }
    }

    public void UnlockReferenceList()
    {
      Monitor.Exit(m_LockReferences);
    }

    public override void UpdateMetadata(XmlElement xmlElement)
    {
      ArrayList list;
      UpdateEverything(false, false, typeof(DvMediaResource), typeof(DvMediaItem2), typeof(DvMediaContainer2), xmlElement, out list);
    }

    public override void UpdateObject(IUPnPMedia newObj)
    {
      foreach (IDvResource resource in newObj.Resources)
      {
      }
      base.UpdateObject(newObj);
      NotifyRootOfChange();
    }

    public override void UpdateObject(XmlElement xmlElement)
    {
      ArrayList list;
      UpdateEverything(true, false, typeof(DvMediaResource), typeof(DvMediaItem2), typeof(DvMediaContainer2), xmlElement, out list);
    }

    public override void WriteInnerXml(ToXmlFormatter formatter, object data, XmlTextWriter xmlWriter)
    {
      ToXmlDataDv dv = (ToXmlDataDv)data;
      InnerXmlWriter.WriteInnerXml(this, new InnerXmlWriter.DelegateWriteProperties(InnerXmlWriter.WriteInnerXmlProperties), new InnerXmlWriter.DelegateShouldPrintResources(PrintResources), new InnerXmlWriter.DelegateWriteResources(InnerXmlWriterDv.WriteInnerXmlResources), new InnerXmlWriter.DelegateWriteDescNodes(InnerXmlWriter.WriteInnerXmlDescNodes), formatter, dv, xmlWriter);
    }

    // Properties
    public override string ID
    {
      get { return base.ID; }
      set
      {
        CheckRuntimeBindings(new StackTrace());
        if (value == m_ID) return;
        m_ID = value;
        NotifyRootOfChange();
      }
    }

    public bool IsDeletePending
    {
      get
      {
        lock (m_LockReferences)
        {
          return m_Deleting;
        }
      }
    }

    public override bool IsRestricted
    {
      get { return base.IsRestricted; }
      set
      {
        bool restricted = m_Restricted;
        m_Restricted = value;
        if (restricted != value)
        {
          NotifyRootOfChange();
        }
      }
    }

    public override IMediaContainer Parent
    {
      get { return base.Parent; }
      set
      {
        CheckRuntimeBindings(new StackTrace());
        DvMediaContainer2 container = (DvMediaContainer2)value;
        base.Parent = container;
      }
    }

    public IList ReferenceItems
    {
      get
      {
        LockReferenceList();
        ArrayList list = (ArrayList)m_ReferringItems.Clone();
        UnlockReferenceList();
        return list;
      }
    }

    public override IMediaItem RefItem
    {
      get { return m_RefItem; }
      set
      {
        CheckRuntimeBindings(new StackTrace());
        m_RefItem = (DvMediaItem2)value;
      }
    }
  }
}
