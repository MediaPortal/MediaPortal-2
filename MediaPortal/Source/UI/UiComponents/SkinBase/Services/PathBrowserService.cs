#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Utilities;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UiComponents.SkinBase.Services
{
  /// <summary>
  /// Service which implements the <see cref="IPathBrowser"/> API.
  /// </summary>
  public class PathBrowserService : IPathBrowser
  {
    #region Protected fields

    protected ItemsList _pathTreeRoot = new ItemsList();

    protected AbstractProperty _choosenResourcePathProperty = new WProperty(typeof(ResourcePath), null);
    protected AbstractProperty _isChoosenPathValidProperty = new WProperty(typeof(bool), false);
    protected AbstractProperty _choosenResourcePathDisplayNameProperty = new WProperty(typeof(string), null);
    protected AbstractProperty _headerTextProperty = new WProperty(typeof(string), null);
    protected AbstractProperty _showSystemResourcesProperty = new WProperty(typeof(bool), false);

    protected Guid _dialogHandle = Guid.Empty;
    protected Guid _dialogInstanceId = Guid.Empty;
    protected bool _dialogAccepted = false;
    protected bool _enumerateFiles = false;
    protected ValidatePathDlgt _validatePathDlgt = null;

    #endregion

    public PathBrowserService()
    {
      _choosenResourcePathProperty.Attach(OnChoosenResourcePathChanged);
    }

    void OnChoosenResourcePathChanged(AbstractProperty resourceProviderURL, object oldValue)
    {
      UpdateIsChoosenPathValid();
      UpdateChoosenPathDisplayName();
    }

    void OnTreePathSelectionChanged(AbstractProperty property, object oldValue)
    {
      UpdateChoosenResourcePath();
      UpdateIsChoosenPathValid();
    }

    public AbstractProperty ShowSystemResourcesProperty
    {
      get { return _showSystemResourcesProperty; }
    }

    public bool ShowSystemResources
    {
      get { return (bool) _showSystemResourcesProperty.GetValue(); }
      set { _showSystemResourcesProperty.SetValue(value); }
    }

    public Guid DialogHandle
    {
      get { return _dialogHandle; }
    }

    public Guid DialogInstanceId
    {
      get { return _dialogInstanceId; }
    }

    public bool DialogAccepted
    {
      get { return _dialogAccepted; }
    }

    public ItemsList PathTreeRoot
    {
      get { return _pathTreeRoot; }
    }

    /// <summary>
    /// Selected resource provider path as resource path instance.
    /// </summary>
    /// <remarks>
    /// This property will automatically be updated from the directory tree selection change handler.
    /// After that workflow state where the path was choosen, this property contains the new path of the edited share.
    /// </remarks>
    public ResourcePath ChoosenResourcePath
    {
      get { return (ResourcePath) _choosenResourcePathProperty.GetValue(); }
      set { _choosenResourcePathProperty.SetValue(value); }
    }

    public AbstractProperty IsChoosenPathValidProperty
    {
      get { return _isChoosenPathValidProperty; }
    }

    /// <summary>
    /// <c>true</c> if the choosen resource provider path (<see cref="ChoosenResourcePath"/>) is a valid path in the
    /// target system.
    /// </summary>
    public bool IsChoosenPathValid
    {
      get { return (bool) _isChoosenPathValidProperty.GetValue(); }
      set { _isChoosenPathValidProperty.SetValue(value); }
    }

    public AbstractProperty ChoosenResourcePathDisplayNameProperty
    {
      get { return _choosenResourcePathDisplayNameProperty; }
    }

    /// <summary>
    /// Human-readable display name of the selected resource provider path.
    /// </summary>
    public string ChoosenResourcePathDisplayName
    {
      get { return (string) _choosenResourcePathDisplayNameProperty.GetValue(); }
      set { _choosenResourcePathDisplayNameProperty.SetValue(value); }
    }

    public AbstractProperty HeaderTextProperty
    {
      get { return _headerTextProperty; }
    }

    public string HeaderText
    {
      get { return (string) _headerTextProperty.GetValue(); }
      set { _headerTextProperty.SetValue(value); }
    }

    public void RefreshOrClearSubPathItems(TreeItem pathItem, bool clearSubItems)
    {
      if (clearSubItems)
      {
        pathItem.SubItems.Clear();
        pathItem.SubItems.FireChange();
      }
      else
        RefreshResourceProviderPathList(pathItem.SubItems, (ResourcePath) pathItem.AdditionalProperties[Consts.KEY_RESOURCE_PATH]);
    }

    public void AcceptPath()
    {
      _dialogAccepted = true;
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.CloseDialog(_dialogInstanceId);
    }

    protected void OnDialogClosed(string dialogName, Guid dialogInstanceId)
    {
      if (_dialogInstanceId != dialogInstanceId)
        return;
      if (_dialogAccepted)
        PathBrowserMessaging.SendPathChoosenMessage(_dialogHandle, ChoosenResourcePath);
      else
        PathBrowserMessaging.SendDialogCancelledMessage(_dialogHandle);

      _dialogInstanceId = Guid.Empty;
      _dialogHandle = Guid.Empty;
    }

    protected static ResourcePath FindChoosenResourcePath(ItemsList items)
    {
      foreach (TreeItem directoryItem in items)
        if (directoryItem.Selected)
          return (ResourcePath) directoryItem.AdditionalProperties[Consts.KEY_RESOURCE_PATH];
        else
        {
          ResourcePath childPath = FindChoosenResourcePath(directoryItem.SubItems);
          if (childPath != null)
            return childPath;
        }
      return null;
    }

    protected void UpdateChoosenResourcePath()
    {
      ChoosenResourcePath = FindChoosenResourcePath(_pathTreeRoot);
    }

    protected internal void UpdateIsChoosenPathValid()
    {
      ResourcePath choosenResourcePath = ChoosenResourcePath;
      ResourcePath path = choosenResourcePath;
      bool result = path == null ? false : GetIsPathValid(choosenResourcePath);
      IsChoosenPathValid = result;
    }

    protected bool GetIsPathValid(ResourcePath path)
    {
      return _validatePathDlgt == null ? true : _validatePathDlgt(path);
    }

    protected void UpdateChoosenPathDisplayName()
    {
      ResourcePath path = ChoosenResourcePath;
      ChoosenResourcePathDisplayName = path == null ? string.Empty : path.FileName;
    }

    /// <summary>
    /// We need a class to provide a property <see cref="IsExpanded"/> together with its <see cref="IsExpandedProperty"/>
    /// in order to make the SkinEngine bind to our expansion flag.
    /// </summary>
    protected class ExpansionHelper
    {
      protected AbstractProperty _isExpandedProperty = new WProperty(typeof(bool), false);
      protected PathBrowserService _parent;
      protected TreeItem _directoryItem;

      public ExpansionHelper(TreeItem directoryItem, PathBrowserService parent)
      {
        _parent = parent;
        _directoryItem = directoryItem;
        _isExpandedProperty.Attach(OnExpandedChanged);
      }

      void OnExpandedChanged(AbstractProperty property, object oldvalue)
      {
        bool expanded = (bool) property.GetValue();
        _parent.RefreshOrClearSubPathItems(_directoryItem, !expanded);
      }

      public AbstractProperty IsExpandedProperty
      {
        get { return _isExpandedProperty; }
      }

      public bool IsExpanded
      {
        get { return (bool) _isExpandedProperty.GetValue(); }
        set { _isExpandedProperty.SetValue(value); }
      }
    }

    protected void RefreshResourceProviderPathList(ItemsList items, ResourcePath path)
    {
      items.Clear();
      IEnumerable<ResourcePathMetadata> res = GetChildDirectoriesData(path);
      if (res != null)
        AddResources(res, items);
      if (_enumerateFiles)
      {
        res = GetFilesData(path);
        if (res != null)
          AddResources(res, items);
      }
      items.FireChange();
    }

    protected void AddResources(IEnumerable<ResourcePathMetadata> resources, ItemsList items)
    {
      List<ResourcePathMetadata> resourcesMetadata = new List<ResourcePathMetadata>(resources);
      resourcesMetadata.Sort((a, b) => a.ResourceName.CompareTo(b.ResourceName));
      foreach (ResourcePathMetadata resourceMetadata in resourcesMetadata)
      {
        TreeItem directoryItem = new TreeItem(Consts.KEY_NAME, resourceMetadata.ResourceName);
        directoryItem.AdditionalProperties[Consts.KEY_RESOURCE_PATH] = resourceMetadata.ResourcePath;
        directoryItem.SetLabel(Consts.KEY_PATH, resourceMetadata.HumanReadablePath);
        if (ChoosenResourcePath == resourceMetadata.ResourcePath)
          directoryItem.Selected = true;
        directoryItem.SelectedProperty.Attach(OnTreePathSelectionChanged);
        directoryItem.AdditionalProperties[Consts.KEY_EXPANSION] = new ExpansionHelper(directoryItem, this);
        items.Add(directoryItem);
      }
    }

    protected IEnumerable<ResourcePathMetadata> GetChildDirectoriesData(ResourcePath path)
    {
      IResourceAccessor ra;
      if (path.TryCreateLocalResourceAccessor(out ra))
      {
        using (ra)
        {
          IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
          if (fsra == null)
            yield break;
          ICollection<IFileSystemResourceAccessor> res = FileSystemResourceNavigator.GetChildDirectories(fsra, ShowSystemResources);
          if (res != null)
            foreach (IFileSystemResourceAccessor childAccessor in res)
              using (childAccessor)
              {
                yield return new ResourcePathMetadata
                  {
                      ResourceName = childAccessor.ResourceName,
                      HumanReadablePath = childAccessor.ResourcePathName,
                      ResourcePath = childAccessor.CanonicalLocalResourcePath
                  };
              }
        }
      }
      else
        ServiceRegistration.Get<ILogger>().Warn("FileBrowserModel: Cannot access resource path '{0}' for getting child directories", path);
    }

    protected IEnumerable<ResourcePathMetadata> GetFilesData(ResourcePath path)
    {
      IResourceAccessor ra;
      if (path.TryCreateLocalResourceAccessor(out ra))
      {
        using (ra)
        {
          IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
          if (fsra == null)
            yield break;
          ICollection<IFileSystemResourceAccessor> res = FileSystemResourceNavigator.GetFiles(fsra, ShowSystemResources);
          if (res != null)
            foreach (IFileSystemResourceAccessor fileAccessor in res)
              using(fileAccessor)
              {
                yield return new ResourcePathMetadata
                  {
                      ResourceName = fileAccessor.ResourceName,
                      HumanReadablePath = fileAccessor.ResourcePathName,
                      ResourcePath = fileAccessor.CanonicalLocalResourcePath
                  };
              }
        }
      }
      else
        ServiceRegistration.Get<ILogger>().Warn("FileBrowserModel: Cannot access resource path '{0}' for getting files", path);
    }

    /// <summary>
    /// Updates the data for the resource provider path tree.
    /// </summary>
    protected void UpdateResourceProviderPathTree()
    {
      RefreshResourceProviderPathList(_pathTreeRoot, LocalFsResourceProviderBase.GetResourcePath("/"));
      UpdateIsChoosenPathValid();
    }

    #region IPathBrowser implementation

    public Guid ShowPathBrowser(string headerText, bool enumerateFiles, bool showSystemResources, ValidatePathDlgt validatePathDlgt)
    {
      return ShowPathBrowser(headerText, enumerateFiles, showSystemResources, null, validatePathDlgt);
    }

    public Guid ShowPathBrowser(string headerText, bool enumerateFiles, bool showSystemResources, ResourcePath initialPath, ValidatePathDlgt validatePathDlgt)
    {
      ChoosenResourcePath = null;
      UpdateResourceProviderPathTree();
      HeaderText = headerText;
      _dialogHandle = Guid.NewGuid();
      _dialogAccepted = false;
      _enumerateFiles = enumerateFiles;
      _validatePathDlgt = validatePathDlgt;
      ShowSystemResources = showSystemResources;

      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      Guid? dialogInstanceId = screenManager.ShowDialog(Consts.DIALOG_PATH_BROWSER, OnDialogClosed);
      if (!dialogInstanceId.HasValue)
        throw new InvalidDataException("File browser could not be shown");
      _dialogInstanceId = dialogInstanceId.Value;
      return _dialogHandle;
    }

    #endregion
  }
}