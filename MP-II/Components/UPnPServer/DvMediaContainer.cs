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
using System.Runtime.Serialization;
using Intel.UPNP.AV.MediaServer.DV;
using Intel.UPNP.AV.CdsMetadata;
//using MetadataParser;

namespace Components.UPnPServer
{
  [Serializable]
  public class DvMediaContainer2 : MediaContainer, IDvContainer
  {
    // Fields
    [NonSerialized]
    public Delegate_UpdateMetadata Callback_UpdateMetadata;

    public delegate void Delegate_AddChildren(DvMediaContainer2 parent);
    private static Type TYPE_DV_ROOT = typeof(DvRootContainer2);

    // Events
    public event DvDelegates.Delegate_OnChildrenRemove OnChildrenRemoved;

    public event DvDelegates.Delegate_OnChildrenRemove OnChildrenToRemove;

    public event Delegate_AddChildren OnAddChildren;

    [NonSerialized]
    public object Context;

    // Methods
    public DvMediaContainer2()
    {
      Callback_UpdateMetadata = null;
      HashingMethod = IdSorter;
    }

    public DvMediaContainer2(XmlElement xmlElement) : base(xmlElement)
    {
      Callback_UpdateMetadata = null;
      HashingMethod = IdSorter;
    }

    protected DvMediaContainer2(SerializationInfo info, StreamingContext context) : base(info, context)
    {
      Callback_UpdateMetadata = null;
    }

    public virtual void AddBranch(IDvMedia branch)
    {
      branch.ID = MediaBuilder.GetUniqueId();
      AddObject(branch, false);
    }

    public virtual void AddBranches(ICollection branches)
    {
      foreach (IUPnPMedia media in branches)
      {
        media.ID = MediaBuilder.GetUniqueId();
      }
      AddObjects(branches, false);
    }

    private void AddContainer(IDvContainer addThis)
    {
      AddObject(addThis, false);
    }

    public DvMediaReference2 AddDvMediaReference2(DvMediaItem2 underlayingItem)
    {
      underlayingItem.LockReferenceList();
      DvMediaReference2 newObject = underlayingItem.CreateDvMediaReference2();
      AddObject(newObject, false);
      underlayingItem.UnlockReferenceList();
      return newObject;
    }

    private void AddItem(IDvItem addThis)
    {
      AddObject(addThis, false);
    }

    public override void AddObject(IUPnPMedia newObject, bool overWrite)
    {
      //Console.WriteLine("{0}.Add {1}", Title, newObject.Title);
      IDvMedia media = (IDvMedia)newObject;
      ThrowExceptionIfBad(media);
      base.AddObject(newObject, overWrite);
      NotifyRootOfChange();
    }

    public override void AddObjects(ICollection newObjects, bool overWrite)
    {
      foreach (IDvMedia media in newObjects)
      {
        //  Console.WriteLine("{0}.Add {1}", Title, media.Title);
        ThrowExceptionIfBad(media);
      }
      base.AddObjects(newObjects, overWrite);
      NotifyRootOfChange();
    }

    public IDvItem AddReference(IDvItem underlayingItem)
    {
      underlayingItem.LockReferenceList();
      IDvItem newObject = underlayingItem.CreateReference();
      AddObject(newObject, false);
      underlayingItem.UnlockReferenceList();
      return newObject;
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

    public void ChangeParent(IDvContainer diffParent)
    {
      ChangeParent2(this, (DvMediaContainer2)diffParent);
    }

    internal static void ChangeParent2(IDvMedia target, DvMediaContainer2 np)
    {
      Exception innerException = null;
      IUPnPMedia media = null;
      IDvMedia[] removedThese = new IDvMedia[] { target };
      DvMediaContainer2 parent = (DvMediaContainer2)target.Parent;
      if (parent.OnChildrenToRemove != null)
      {
        parent.OnChildrenToRemove(parent, removedThese);
      }
      parent.m_LockListing.AcquireWriterLock(-1);
      np.m_LockListing.AcquireWriterLock(-1);
      try
      {
        int index = parent.HashingMethod.Get(parent.m_Listing, target);
        parent.m_Listing.RemoveAt(index);
        target.Parent = null;
        if (parent.m_Listing.Count == 0)
        {
          parent.m_Listing = null;
        }
        if (np.m_Listing == null)
        {
          np.m_Listing = new ArrayList();
        }
        try
        {
          np.HashingMethod.Set(np.m_Listing, target, true);
          target.Parent = np;
        }
        catch (KeyCollisionException)
        {
          media = target;
        }
      }
      catch (Exception exception2)
      {
        innerException = exception2;
      }
      np.m_LockListing.ReleaseWriterLock();
      parent.m_LockListing.ReleaseWriterLock();
      if (innerException != null)
      {
        throw new Exception("Unexpected rrror in DvMediaContainer2.ChangeParent2", innerException);
      }
      if (media != null)
      {
        throw new Error_DuplicateIdException(media);
      }
      if (parent.OnChildrenRemoved != null)
      {
        parent.OnChildrenRemoved(parent, removedThese);
      }
      parent.NotifyRootOfChange();
      np.NotifyRootOfChange();
    }

    public override void CheckRuntimeBindings(StackTrace st)
    {
      Type declaringType = st.GetFrame(0).GetMethod().DeclaringType;
      Type type = GetType();
      bool flag = false;
      if (((declaringType.Namespace == type.Namespace) || (declaringType.Namespace == typeof(MediaObject).Namespace)) && ((declaringType.Assembly == type.Assembly) || (declaringType.Assembly == typeof(MediaObject).Assembly)))
      {
        flag = true;
      }
      if (!flag)
      {
        throw new Error_MetadataCallerViolation();
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
      AddObjects(list, true);
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      base.GetObjectData(info, context);
    }

    protected internal IDvResource GetResource(string resourceID)
    {
      IDvResource resource = null;
      m_LockResources.AcquireReaderLock(-1);
      if (m_Resources != null)
      {
        foreach (IDvResource resource2 in m_Resources)
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

    public virtual void NotifyRootOfChange()
    {
      m_UpdateID++;
      IDvContainer parent = this;
      while (parent.Parent != null)
      {
        parent = (IDvContainer)parent.Parent;
      }
      if (parent.GetType() == TYPE_DV_ROOT)
      {
        ((DvRootContainer2)parent).FireOnContainerChanged(this);
      }
    }

    public virtual void RemoveBranch(IDvMedia branch)
    {
      RemoveObject(branch);
    }

    private void RemoveContainer(IDvContainer container)
    {
      RemoveObject(container);
    }

    private void RemoveItem(IDvItem item)
    {
      RemoveObject(item);
    }

    public override void RemoveObject(IUPnPMedia obj)
    {
      IDvMedia media = obj as IDvMedia;
      IDvItem item = obj as IDvItem;
      IDvContainer container = obj as IDvContainer;
      ArrayList removedThese = new ArrayList(1);
      removedThese.Add(obj);
      if (OnChildrenToRemove != null)
      {
        OnChildrenToRemove(this, removedThese);
      }
      if (item != null)
      {
        item.NotifyPendingDelete();
      }
      else if (container != null)
      {
        IList completeList = container.CompleteList;
        foreach (IDvMedia media2 in completeList)
        {
          container.RemoveObject(media2);
        }
      }
      base.RemoveObject(obj);
      if (OnChildrenRemoved != null)
      {
        OnChildrenRemoved(this, removedThese);
      }
      NotifyRootOfChange();
    }

    public override void RemoveObjects(ICollection removeThese)
    {
      if (OnChildrenToRemove != null)
      {
        OnChildrenToRemove(this, removeThese);
      }
      foreach (IUPnPMedia media in removeThese)
      {
        IDvMedia media2 = media as IDvMedia;
        IDvItem item = media as IDvItem;
        IDvContainer container = media as IDvContainer;
        if (item != null)
        {
          item.NotifyPendingDelete();
        }
        else if (container != null)
        {
          IList completeList = container.CompleteList;
          foreach (IDvMedia media3 in completeList)
          {
            container.RemoveObject(media3);
          }
        }
        base.RemoveObject(media);
      }
      if (OnChildrenRemoved != null)
      {
        OnChildrenRemoved(this, removeThese);
      }
      NotifyRootOfChange();
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
      Sink_OnMediaPropertiesChanged(sender, stateNumber);
      NotifyRootOfChange();
    }

    private static void ThrowExceptionIfBad(IDvMedia media)
    {
      if (media.IsItem)
      {
        IDvItem item = (IDvItem)media;
        if (media.IsReference)
        {
          IDvItem refItem = item.RefItem as IDvItem;
          if (refItem == null)
          {
            throw new InvalidCastException("Cannot convert media.RefItem to IDvItem");
          }
          if (refItem.IsDeletePending)
          {
            throw new Error_PendingDeleteException(refItem);
          }
        }
      }
      else
      {
        IDvContainer container = (IDvContainer)media;
      }
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
      if (Callback_UpdateMetadata != null)
      {
        Callback_UpdateMetadata(this);
      }
      InnerXmlWriter.WriteInnerXml(this, InnerXmlWriter.WriteInnerXmlProperties, PrintResources,
          InnerXmlWriterDv.WriteInnerXmlResources, InnerXmlWriter.WriteInnerXmlDescNodes,
          formatter, (ToXmlData) data, xmlWriter);
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

    public override bool IsRestricted
    {
      get { return base.IsRestricted; }
      set
      {
        if (value == m_Restricted) return;
        m_Restricted = value;
        NotifyRootOfChange();
      }
    }

    public override IMediaContainer Parent
    {
      get { return base.Parent; }
      set
      {
        CheckRuntimeBindings(new StackTrace());
        IDvContainer container = (IDvContainer)value;
        base.Parent = container;
      }
    }

    // Nested Types
    public delegate void Delegate_UpdateMetadata(DvMediaContainer2 media);

    public override IList Browse(uint startingIndex, uint requestedCount, out uint totalMatches)
    {
      ////      Console.WriteLine("browsing:{0} {1} {2}", Title, startingIndex, requestedCount);
      if (OnAddChildren != null)
      {
        OnAddChildren(this);
      }
      return base.Browse(startingIndex, requestedCount, out totalMatches);
    }
  }




}
