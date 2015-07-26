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
using System.Xml.Serialization;
using MediaPortal.Common.FileEventNotification;
using MediaPortal.Common.Logging;
using MediaPortal.PackageCore.Package.Action;
using MediaPortal.PackageCore.Package.Root;

namespace MediaPortal.PackageCore.Package.Content
{
  partial class ContentBaseModel : ICheckable
  {
    #region private fields

    private WeakReference<ContentModel> _parent;

    #endregion

    #region public properties

    [XmlIgnore]
    public ContentModel Parent
    {
      get
      {
        ContentModel package;
        if (_parent != null && _parent.TryGetTarget(out package))
        {
          return package;
        }
        return null;
      }
      private set { _parent = new WeakReference<ContentModel>(value); }
    }

    [XmlIgnore]
    public PackageModel Package
    {
      get { return Parent == null ? null : Parent.Package; }
    }

    #endregion

    #region public methods
    
    public void Install(InstallContext context)
    {
      ActionsBaseModel actions;
      switch (context.InstallType)
      {
        case InstallType.Install:
          actions = InstallActions;
          break;

        case InstallType.Update:
          if (InstallActions.UseForUpdate)
          {
            actions = InstallActions;
            actions.SetAllOverwriteTrue();
          }
          else
          {
            actions = UpdateActions;
          }
          break;

        case InstallType.Uninstall:
          actions = UninstallActions;
          break;

        default:
          throw new NotSupportedDriveTypeException("Install action not supported");
      }

      actions.Install(context);
    }

    public string GetPath(string name)
    {
      if (String.Equals(name, "root", StringComparison.OrdinalIgnoreCase))
      {
        return RootDirectory;
      }
      return null;
    }

    #endregion

    #region internal/private methods

    internal virtual void Initialize(ContentModel content, ILogger log)
    {
      Parent = content;
      if (InstallActions != null)
      {
        InstallActions.Initialize(this, log);
      }
      if (UpdateActions != null)
      {
        UpdateActions.Initialize(this, log);
      }
      if (UninstallActions != null)
      {
        UninstallActions.Initialize(this, log);
      }
    }

    #endregion

    #region Implementation of ICheckable

    [XmlIgnore]
    public abstract string ElementsName { get; }

    public virtual bool CheckElements(ILogger log)
    {
      bool ok = this.CheckNotNullOrEmpty(Name, "Name", log) &
             this.CheckNotNullAndContent(InstallActions, "InstallActions", log) &
             this.CheckNotNullAndContent(UninstallActions, "UninstallActions", log);
      if (InstallActions != null && InstallActions.UseForUpdate)
      {
        if (UpdateActions != null)
        {
          if (log != null)
          {
            log.Error(String.Format("{0}: UpdateActions are not allowed when InstallActions.UseForUpdate is true", ElementsName));
          }
          ok = false;
        }
      }
      else
      {
        if (UpdateActions == null)
        {
          if (log != null)
          {
            log.Error(String.Format("{0}: UpdateActions are required when InstallActions.UseForUpdate is false", ElementsName));
          }
          ok = false;
        }
        else
        {
          ok &= UpdateActions.CheckElements(log);
        }
      }
      return ok;
    }

    #endregion
  }
}