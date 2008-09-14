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
using System.Xml.Serialization;
using MediaPortal.Core;
using MediaPortal.Core.ExtensionManager;
using MediaPortal.Core.PathManager;

namespace Components.ExtensionUpdater.ExtensionManager
{
  [Serializable]
  public class ExtensionQueueObject : IExtensionQueueObject
  {
    public ExtensionQueueObject()
    {
      PackageName = string.Empty;
      PackageId = string.Empty;
      Action = string.Empty;
      FileName = string.Empty;
      PackageExtensionId = string.Empty;
    }

    public ExtensionQueueObject(IExtensionPackage package, string action)
    {
      PackageName = package.Name;
      PackageId = package.PackageId;
      PackageExtensionId = package.ExtensionId;
      Action = action;
      FileName = package.FileName;
    }

    string _packageName;
    /// <summary>
    /// Gets or sets the name of the package.
    /// </summary>
    /// <value>The name of the package.</value>
    [XmlAttribute] 
    public string PackageName
    {
      get
      {
        return _packageName;
      }
      set
      {
        _packageName = value;
      }
    }

    string _packageExtensionId;

    /// <summary>
    /// Gets or sets the package extension id.
    /// </summary>
    /// <value>The package extension id.</value>
    [XmlAttribute]    
    public string PackageExtensionId
    {
      get
      {
        return _packageExtensionId;
      }
      set
      {
        _packageExtensionId = value;
      }
    }
    string _packageId;
    /// <summary>
    /// Gets or sets the package id.
    /// </summary>
    /// <value>The package id.</value>
    [XmlAttribute]
    public string PackageId
    {
      get
      {
        return _packageId;
      }
      set
      {
        _packageId = value;
      }
    }
    
    string _fileName;
    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    /// <value>The name of the file.</value>
    [XmlAttribute] 
    public string FileName
    {
      get
      {
        if (_fileName == null)
        {
          _fileName = String.Format(@"{0}\{1}.xml", ServiceScope.Get<IPathManager>().GetPath("<MPINSTALLER>"),PackageId);
        }
        return _fileName;
      }
      set
      {
        _fileName = value;
      }
    }

    string _action;
    [XmlAttribute]
    /// <summary>
      /// Gets or sets the action.
      /// </summary>
      /// <value>The action.</value>
      public string Action
    {
      get
      {
        return _action;
      }
      set
      {
        _action = value;
      }
    }
  }
}