#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Collections.Generic;
using System.Xml.Serialization;
using MediaPortal.PackageCore.Package.Content;
// ReSharper disable PartialMethodWithSinglePart
// ReSharper disable InvocationIsSkipped

namespace MediaPortal.PackageCore.Package.Root
{
  [XmlRoot("Package", Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd")]
  public partial class PackageModel
  {
    public PackageModel()
    {
      Init();
    }

    partial void Init();

    [XmlElement(typeof(InstallOptionsModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "InstallOptions")]
    public InstallOptionsModel InstallOptions { get; set; }

    [XmlElement(typeof(ImagesModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "Images")]
    public ImagesModel Images { get; set; }

    [XmlElement(typeof(LinksModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "Links")]
    public LinksModel Links { get; set; }

    [XmlElement(typeof(ReleaseInfoModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "ReleaseInfo")]
    public ReleaseInfoModel ReleaseInfo { get; set; }

    [XmlElement(typeof(ContentModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "Content")]
    public ContentModel Content { get; set; }

    [XmlAttribute]
    public string Name { get; set; }

    [XmlAttribute]
    public string Version { get; set; }

    [XmlAttribute]
    public Channels Channel { get; set; }
  }


  public partial class InstallOptionsModel
  {
    public InstallOptionsModel()
    {
      Options = new List<InstallOptionModel>();
      Init();
    }

    partial void Init();

    [XmlElement(typeof(InstallOptionModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "Option")]
    public List<InstallOptionModel> Options { get; private set; }
  }


  public partial class InstallOptionModel
  {
    public InstallOptionModel()
    {
      Contents = new List<ContentRefModel>();
      Init();
    }

    partial void Init();

    [XmlElement(typeof(ContentRefModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "Content")]
    public List<ContentRefModel> Contents { get; private set; }

    [XmlAttribute]
    public string Name { get; set; }

    [XmlAttribute]
    public string Description { get; set; }

    [XmlAttribute]
    public bool IsDefault { get; set; }
  }

  public partial class ContentRefModel
  {
    public ContentRefModel()
    {
      Init();
    }

    partial void Init();

    [XmlAttribute]
    public string Name { get; set; }
  }


  public partial class ImagesModel
  {
    public ImagesModel()
    {
      Images = new List<ImageModel>();
      Init();
    }

    partial void Init();

    [XmlElement(typeof(ImageModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "Image")]
    public List<ImageModel> Images { get; private set; }
  }

  public partial class ImageModel
  {
    public ImageModel()
    {
      Init();
    }

    partial void Init();

    [XmlAttribute]
    public ImageTypes Type { get; set; }

    [XmlAttribute]
    public string Description { get; set; }

    [XmlText]
    public string Path { get; set; }
  }


  public partial class LinksModel
  {
    public LinksModel()
    {
      Links = new List<LinkModel>();
      Init();
    }

    partial void Init();

    [XmlElement(typeof(LinkModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "Link")]
    public List<LinkModel> Links { get; private set; }
  }

  public partial class LinkModel
  {
    [XmlAttribute]
    public string Description { get; set; }

    [XmlText]
    public string Url { get; set; }
  }


  public partial class ReleaseInfoModel
  {
    public ReleaseInfoModel()
    {
      Init();
    }

    partial void Init();

    [XmlAttribute]
    public string ReleaseNotes { get; set; }
  }
}