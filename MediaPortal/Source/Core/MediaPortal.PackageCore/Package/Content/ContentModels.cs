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

using System.Collections.Generic;
using System.Xml.Serialization;
using MediaPortal.PackageCore.Package.Action;
// ReSharper disable PartialMethodWithSinglePart
// ReSharper disable InvocationIsSkipped

namespace MediaPortal.PackageCore.Package.Content
{
  public partial class ContentModel
  {
    public ContentModel()
    {
      Contents = new List<ContentBaseModel>();
      Init();
    }

    partial void Init();

    [XmlElement(typeof(PluginContentModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "Plugin")]
    [XmlElement(typeof(PluginExtensionContentModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "PluginExtension")]
    [XmlElement(typeof(CoreContentModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "Core")]
    [XmlElement(typeof(UtilityContentModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "Utility")]
    [XmlElement(typeof(CustomContentModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "Custom")]
    public List<ContentBaseModel> Contents { get; private set; }
  }

  public abstract partial class ContentBaseModel
  {
    public ContentBaseModel()
    {
      Init();
    }

    partial void Init();

    [XmlElement(typeof(InstallActionsModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "InstallActions")]
    public InstallActionsModel InstallActions { get; set; }

    [XmlElement(typeof(UpdateActionsModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "UpdateActions")]
    public UpdateActionsModel UpdateActions { get; set; }

    [XmlElement(typeof(UninstallActionsModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "UninstallActions")]
    public UninstallActionsModel UninstallActions { get; set; }

    [XmlAttribute]
    public string Name { get; set; }

    [XmlAttribute]
    public InstallTarget Target { get; set; }

    [XmlAttribute]
    public string RootDirectory { get; set; }
  }

  public partial class PluginContentModel : ContentBaseModel
  {
    public PluginContentModel()
    {
      Init();
    }

    partial void Init();
  }

  public partial class PluginExtensionContentModel : ContentBaseModel
  {
    public PluginExtensionContentModel()
    {
      Init();
    }

    partial void Init();
  }

  public partial class CoreContentModel : ContentBaseModel
  {
    public CoreContentModel()
    {
      Init();
    }

    partial void Init();
  }

  public partial class UtilityContentModel : ContentBaseModel
  {
    public UtilityContentModel()
    {
      Init();
    }

    partial void Init();
  }

  public partial class CustomContentModel : ContentBaseModel
  {
    public CustomContentModel()
    {
      Init();
    }

    partial void Init();
  }
}