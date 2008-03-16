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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Services.MPIManager;
using MediaPortal.Core.MPIManager;
using MediaPortal.Presentation.WindowManager;
using MediaPortal.Core;
using MediaPortal.Core.Properties;
using MediaPortal.Core.Collections;

namespace MyExtensions
{
  public class PackageProperty : MPIEnumeratorObject
  {
    private Property _nameProperty;
    private Property _descriptionProperty;
    private Property _authorProperty;
    private Property _longNameProperty;
    private Property _screeShotProperty;
    private readonly ExtensionFactory _factory;
    private ItemsCollection _dependon;
    private ItemsCollection _versions;
    MPInstaller Installer = ServiceScope.Get<IMPInstaller>() as MPInstaller;

    //public PackageProperty(MPIEnumeratorObject obj)
    //      :base(obj)
    //{
    //  _nameProperty = new Property(this.Name);
    //}
    
    public PackageProperty()
      : base()
    {
      _nameProperty = new Property(this.Name);
      _longNameProperty = new Property(this.Name);
      _descriptionProperty = new Property(this.Description);
      _authorProperty = new Property(this.Author);
      _dependon = new ItemsCollection();
      _versions = new ItemsCollection();
      _factory = new ExtensionFactory();
      _screeShotProperty = new Property();
    }

    public void Set(MPIEnumeratorObject obj)
    {
      _nameProperty.SetValue(obj.Name);
      _descriptionProperty.SetValue(obj.Description);
      _authorProperty.SetValue(obj.Author);
      _longNameProperty.SetValue(string.Format("{0} - {1} - {2}",obj.Name,obj.Version,obj.VersionType));
      _screeShotProperty.SetValue(_factory.GetThumb(obj));
      _dependon.Clear();
      _versions.Clear();
      foreach (MPIDependency dep in obj.Dependencies)
      {
        MPIEnumeratorObject depobj = Installer.Enumerator.GetExtensions(dep.ExtensionId);
        ExtensionItem item = new ExtensionItem(depobj);
        item.Add("Name", depobj.Name);
        _dependon.Add(item);
      }
      _factory.LoadItems(ref _versions, Installer.Enumerator.Items[obj.ExtensionId]);
      _dependon.FireChange();
      _versions.FireChange();
    }
    
    public Property NameProperty
    {
      get
      {
        return _nameProperty;
      }
    }

    public Property LongNameProperty
    {
      get
      {
        return _longNameProperty;
      }
    }

    public ItemsCollection DependOn
    {
      get
      {
        return _dependon;
      }
    }

    public ItemsCollection Versions
    {
      get
      {
        return _versions;
      }
    }

    public Property DescriptionProperty
    {
      get
      {
        return _descriptionProperty;
      }
    }

    public Property AuthorProperty
    {
      get
      {
        return _authorProperty;
      }
    }
    
    public Property ScreeShotProperty
    {
      get
      {
        return _screeShotProperty;
      }
    }
  }
}
