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
  public sealed class DvMediaReference2 : IDvItem, IMediaItem, IDvMedia, IUPnPMedia
  {
    // Fields
    private IMediaContainer m_Parent = null;
    private DvMediaItem2 m_Underlying = null;
    private string m_uniqueId = null;
    private static Tags T = Tags.GetInstance();

    // Methods
    public DvMediaReference2(DvMediaItem2 underlyingItem)
    {
      this.m_uniqueId = MediaBuilder.GetUniqueId();
      this.m_Underlying = underlyingItem;
    }

    public void AddDescNode(string[] ignored)
    {
    }

    public void AddDescNode(string ignored)
    {
    }

    public void AddResource(IMediaResource ignored)
    {
    }

    public void AddResources(ICollection ignored)
    {
    }

    public void AddResources(IMediaResource[] ignored)
    {
    }

    public void ChangeParent(IDvContainer ignored)
    {
    }

    public void CheckRuntimeBindings(StackTrace ignored)
    {
    }

    public IDvItem CreateReference()
    {
      throw new ApplicationException("Calling this method is not legal.");
    }

    public void LockReferenceList()
    {
    }

    public IUPnPMedia MetadataCopy()
    {
      return null;
    }

    public void NotifyPendingDelete()
    {
    }

    public void NotifyRootOfChange()
    {
    }

    private bool PrintResources(ArrayList desiredProperties)
    {
      if (desiredProperties != null)
      {
        if (desiredProperties.Count == 0)
        {
          return true;
        }
        foreach (string str in desiredProperties)
        {
          string str2 = str.ToLower();
          if (str2.StartsWith(T[_DIDL.Res]))
          {
            return true;
          }
          foreach (string str3 in MediaResource.GetPossibleAttributes())
          {
            if (("@" + str3.ToLower()) == str2)
            {
              return true;
            }
          }
        }
      }
      return false;
    }

    public void RemoveDescNode(string ignored)
    {
    }

    public void RemoveResource(IMediaResource ignored)
    {
    }

    public void RemoveResources(ICollection ignored)
    {
    }

    public void SetMetadata(MediaBuilder.CoreMetadata ignored)
    {
    }

    public void SetPropertyValue(string ignored, IList ignored2)
    {
    }

    public string ToDidl()
    {
      string str;
      ArrayList list = new ArrayList();
      StringBuilder sb = null;
      StringWriter w = null;
      MemoryStream stream = null;
      XmlTextWriter xmlWriter = null;
      if (MediaObject.ENCODE_UTF8)
      {
        stream = new MemoryStream(MediaObject.XML_BUFFER_SIZE);
        xmlWriter = new XmlTextWriter(stream, Encoding.UTF8);
      }
      else
      {
        sb = new StringBuilder(MediaObject.XML_BUFFER_SIZE);
        w = new StringWriter(sb);
        xmlWriter = new XmlTextWriter(w);
      }
      xmlWriter.Formatting = Formatting.Indented;
      xmlWriter.Namespaces = true;
      xmlWriter.WriteStartDocument();
      MediaObject.WriteResponseHeader(xmlWriter);
      ToXmlData data = (ToXmlData)MediaObject.ToXmlData_AllRecurse.Clone();
      data.IsRecursive = false;
      this.ToXml(ToXmlFormatter.DefaultFormatter, data, xmlWriter);
      xmlWriter.WriteEndDocument();
      xmlWriter.Flush();
      if (MediaObject.ENCODE_UTF8)
      {
        int count = stream.ToArray().Length - 3;
        str = new UTF8Encoding(false, true).GetString(stream.ToArray(), 3, count);
      }
      else
      {
        str = sb.ToString();
      }
      xmlWriter.Close();
      int index = str.IndexOf("\r\n");
      index = str.IndexOf('<', index);
      return str.Remove(0, index);
    }

    public void ToXml(ToXmlFormatter formatter, object data, XmlTextWriter xmlWriter)
    {
      xmlWriter.WriteStartElement(T[_DIDL.Item]);
      xmlWriter.WriteAttributeString(T[_ATTRIB.id], this.ID);
      xmlWriter.WriteAttributeString(T[_ATTRIB.refID], this.m_Underlying.ID);
      xmlWriter.WriteAttributeString(T[_ATTRIB.parentID], this.m_Parent.ID);
      xmlWriter.WriteAttributeString(T[_ATTRIB.restricted], "1");
      InnerXmlWriter.WriteInnerXml(this, new InnerXmlWriter.DelegateWriteProperties(InnerXmlWriter.WriteInnerXmlProperties), new InnerXmlWriter.DelegateShouldPrintResources(MediaObject.ShouldPrintResources), new InnerXmlWriter.DelegateWriteResources(InnerXmlWriter.WriteInnerXmlResources), new InnerXmlWriter.DelegateWriteDescNodes(InnerXmlWriter.WriteInnerXmlDescNodes), formatter, (ToXmlData)data, xmlWriter);
      xmlWriter.WriteEndElement();
    }

    public void UnlockReferenceList()
    {
    }

    public void UpdateObject(IUPnPMedia ignored)
    {
    }

    public void UpdateObject(string ignored)
    {
    }

    public void UpdateObject(XmlElement ignored)
    {
    }

    // Properties
    public MediaClass Class
    {
      get
      {
        return this.m_Underlying.Class;
      }
      set
      {
      }
    }

    public string Creator
    {
      get
      {
        return this.m_Underlying.Creator;
      }
      set
      {
      }
    }

    public IList DescNodes
    {
      get
      {
        return null;
      }
    }

    public string ID
    {
      get
      {
        return this.m_uniqueId;
      }
      set
      {
      }
    }

    public bool IsContainer
    {
      get
      {
        return false;
      }
    }

    public bool IsDeletePending
    {
      get
      {
        return false;
      }
    }

    public bool IsItem
    {
      get
      {
        return true;
      }
    }

    public bool IsReference
    {
      get
      {
        return true;
      }
    }

    public bool IsRestricted
    {
      get
      {
        return true;
      }
      set
      {
      }
    }

    public bool IsSearchable
    {
      get
      {
        return false;
      }
      set
      {
      }
    }

    public IList MergedDescNodes
    {
      get
      {
        return this.m_Underlying.DescNodes;
      }
    }

    public IMediaProperties MergedProperties
    {
      get
      {
        return this.m_Underlying.Properties;
      }
    }

    public IMediaResource[] MergedResources
    {
      get
      {
        return this.m_Underlying.Resources;
      }
    }

    public IMediaContainer Parent
    {
      get
      {
        return this.m_Parent;
      }
      set
      {
        this.m_Parent = value;
      }
    }

    public string ParentID
    {
      get
      {
        return this.m_Parent.ID;
      }
      set
      {
      }
    }

    public IMediaProperties Properties
    {
      get
      {
        return null;
      }
    }

    public IList ReferenceItems
    {
      get
      {
        return null;
      }
    }

    public string RefID
    {
      get
      {
        return this.m_Underlying.ID;
      }
      set
      {
      }
    }

    public IMediaItem RefItem
    {
      get
      {
        return this.m_Underlying;
      }
      set
      {
      }
    }

    public IMediaResource[] Resources
    {
      get
      {
        return null;
      }
    }

    public object Tag
    {
      get
      {
        return null;
      }
      set
      {
      }
    }

    public string Title
    {
      get
      {
        return this.m_Underlying.Title;
      }
      set
      {
      }
    }

    public EnumWriteStatus WriteStatus
    {
      get
      {
        return EnumWriteStatus.NOT_WRITABLE;
      }
      set
      {
      }
    }
  }

}