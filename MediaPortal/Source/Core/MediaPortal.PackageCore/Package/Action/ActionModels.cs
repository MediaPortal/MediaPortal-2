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
// ReSharper disable PartialMethodWithSinglePart
// ReSharper disable InvocationIsSkipped

namespace MediaPortal.PackageCore.Package.Action
{
  public abstract partial class ActionsBaseModel
  {
    public ActionsBaseModel()
    {
      Actions = new List<ActionBaseModel>();
      Init();
    }

    partial void Init();

    [XmlElement(typeof(CopyDirectoryActionModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "CopyDirectory")]
    [XmlElement(typeof(CopyFileActionModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "CopyFile")]
    [XmlElement(typeof(DeleteDirectoryActionModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "DeleteDirectory")]
    [XmlElement(typeof(DeleteFileActionModel), Namespace = "http://schemas.team-mediaportal.com/mp2/package/2015.xsd", ElementName = "DeleteFile")]
    public List<ActionBaseModel> Actions { get; private set; }
  }

  public partial class InstallActionsModel : ActionsBaseModel
  {
    public InstallActionsModel()
    {
      Init();
    }

    partial void Init();

    [XmlAttribute]
    public bool UseForUpdate { get; set; }
  }

  public partial class UpdateActionsModel : ActionsBaseModel
  {
    public UpdateActionsModel()
    {
      Init();
    }

    partial void Init();
  }

  public partial class UninstallActionsModel : ActionsBaseModel
  {
    public UninstallActionsModel()
    {
      Init();
    }

    partial void Init();
  }


  public abstract partial class ActionBaseModel
  {
    public ActionBaseModel()
    {
      Init();
    }

    partial void Init();
  }

  public abstract partial class CanOverwriteActionBaseModel : ActionBaseModel
  {
    public CanOverwriteActionBaseModel()
    {
      Init();
    }

    partial void Init();

    [XmlAttribute]
    public bool Overwrite { get; set; }
  }

  public partial class CopyDirectoryActionModel : CanOverwriteActionBaseModel
  {
    public CopyDirectoryActionModel()
    {
      Init();
    }

    partial void Init();

    [XmlAttribute]
    public string Source { get; set; }

    [XmlAttribute]
    public string Target { get; set; }

    [XmlAttribute]
    public bool TargetRootCanExist { get; set; }
  }

  public partial class CopyFileActionModel : CanOverwriteActionBaseModel
  {
    public CopyFileActionModel()
    {
      Init();
    }

    partial void Init();

    [XmlAttribute]
    public string Source { get; set; }

    [XmlAttribute]
    public string Target { get; set; }
  }

  public partial class DeleteDirectoryActionModel : ActionBaseModel
  {
    public DeleteDirectoryActionModel()
    {
      Init();
    }

    partial void Init();

    [XmlAttribute]
    public string Target { get; set; }

    [XmlAttribute]
    public bool SkipInFotEmpty { get; set; }

    [XmlAttribute]
    public bool FailIfNotExists { get; set; }
  }

  public partial class DeleteFileActionModel : ActionBaseModel
  {
    public DeleteFileActionModel()
    {
      Init();
    }

    partial void Init();

    [XmlAttribute]
    public string Target { get; set; }

    [XmlAttribute]
    public bool FailIfNotExists { get; set; }
  }
}