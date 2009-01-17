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
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MediaProviders;
using MediaPortal.Media.ClientMediaManager;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Models;
using MediaPortal.Presentation.Workflow;
using MediaPortal.Utilities;

namespace UiComponents.Media.Settings.Configuration
{
  /// <summary>
  /// Provides a model to attend the complex shares configuration process in the MP-II configuration.
  /// This model is the workflow model for this process.
  /// </summary>
  public class SharesConfigModel : IWorkflowModel
  {
    #region Consts

    public const string SHARESCONFIG_MODEL_ID_STR = "1768FC91-86B9-4f78-8A4C-E204F0D51502";

    public const string SHARES_OVERVIEW_STATE_ID_STR = "36B3F24A-29B4-4cb4-BC7D-434C51491CD2";

    public const string REMOVE_SHARES_STATE_ID_STR = "900BA520-F989-48c0-B076-5DAD61945845";
    
    public const string SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID_STR = "F3163500-3015-4a6f-91F6-A3DA5DC3593C";
    public const string SHARE_ADD_EDIT_PATH_STATE_ID_STR = "652C5A9F-EA50-4076-886B-B28FD167AD66";
    public const string SHARE_ADD_CHOOSE_PATH_STATE_ID_STR = "5652A9C9-6B20-45f0-889E-CFBF6086FB0A";
    public const string SHARE_ADD_EDIT_NAME_STATE_ID_STR = "ACDD705B-E60B-454a-9671-1A12A3A3985A";
    public const string SHARE_ADD_CHOOSE_CATEGORIES_STATE_ID_STR = "6218FE5B-767E-48e6-9691-65E466B6020B";
    public const string SHARE_ADD_CHOOSE_METADATA_EXTRACTOR_STATE_ID_STR = "B4D50B90-A5D7-48a1-8C0E-4DC2CB9B881D";

    // Keys for the ListItem's Labels in the ItemsLists
    public const string NAME_KEY = "Name";
    public const string ID_KEY = "Id";
    public const string MP_PATH_KEY = "MediaProviderPath";
    public const string PATH_KEY = "Path";
    public const string SHARE_MEDIAPROVIDER_KEY = "MediaProvider";
    public const string SHARE_CATEGORY_KEY = "Category";

    public static Guid SHARES_OVERVIEW_STATE_ID = new Guid(SHARES_OVERVIEW_STATE_ID_STR);
    
    public static Guid REMOVE_SHARES_STATE_ID = new Guid(REMOVE_SHARES_STATE_ID_STR);

    public static Guid SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID = new Guid(SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID_STR);
    public static Guid SHARE_ADD_EDIT_PATH_STATE_ID = new Guid(SHARE_ADD_EDIT_PATH_STATE_ID_STR);
    public static Guid SHARE_ADD_CHOOSE_PATH_STATE_ID = new Guid(SHARE_ADD_CHOOSE_PATH_STATE_ID_STR);
    public static Guid SHARE_ADD_EDIT_NAME_STATE_ID = new Guid(SHARE_ADD_EDIT_NAME_STATE_ID_STR);
    public static Guid SHARE_ADD_CHOOSE_CATEGORIES_STATE_ID = new Guid(SHARE_ADD_CHOOSE_CATEGORIES_STATE_ID_STR);
    public static Guid SHARE_ADD_CHOOSE_METADATA_EXTRACTOR_STATE_ID = new Guid(SHARE_ADD_CHOOSE_METADATA_EXTRACTOR_STATE_ID_STR);

    #endregion

    #region Protected fields

    protected ItemsList _sharesList;
    protected ItemsList _mediaProvidersList;
    protected Property _isSharesSelectedProperty;
    protected Property _isMediaProviderSelectedProperty;
    protected Property _mediaProviderProperty;
    protected Property _mediaProviderPathProperty;
    protected Property _isMediaProviderPathValidProperty;
    protected Property _mediaProviderPathDisplayNameProperty;
    protected ItemsList _mediaProviderPathsTree;
    protected Property _shareNameProperty;
    protected Property _isShareNameEmptyProperty;

    #endregion

    #region Ctor

    public SharesConfigModel()
    {
      // TODO Albert: break the event handler reference from the lists to the Skin's controls
      _sharesList = new ItemsList();
      _mediaProvidersList = new ItemsList();
      _isSharesSelectedProperty = new Property(typeof(bool), false);
      _isMediaProviderSelectedProperty = new Property(typeof(bool), false);
      _mediaProviderProperty = new Property(typeof(IMediaProvider), null);
      _mediaProviderPathProperty = new Property(typeof(string), string.Empty);
      _mediaProviderPathProperty.Attach(OnMediaProviderPathChanged);
      _isMediaProviderPathValidProperty = new Property(typeof(bool), false);
      _mediaProviderPathDisplayNameProperty = new Property(typeof(string), string.Empty);
      _mediaProviderPathsTree = new ItemsList();
      _shareNameProperty = new Property(typeof(string), string.Empty);
      _shareNameProperty.Attach(OnShareNameChanged);
      _isShareNameEmptyProperty = new Property(typeof(bool), true);
    }

    #endregion

    #region Public properties

    public ItemsList Shares
    {
      get { return _sharesList; }
    }

    public ItemsList MediaProviders
    {
      get { return _mediaProvidersList; }
    }

    public Property IsSharesSelectedProperty
    {
      get { return _isSharesSelectedProperty; }
    }

    public bool IsSharesSelected
    {
      get { return (bool) _isSharesSelectedProperty.GetValue(); }
      set { _isSharesSelectedProperty.SetValue(value); }
    }

    public Property IsMediaProviderSelectedProperty
    {
      get { return _isMediaProviderSelectedProperty; }
    }

    public bool IsMediaProviderSelected
    {
      get { return (bool) _isMediaProviderSelectedProperty.GetValue(); }
      set { _isMediaProviderSelectedProperty.SetValue(value); }
    }

    public Property MediaProviderProperty
    {
      get { return _mediaProviderProperty; }
    }

    public IMediaProvider MediaProvider
    {
      get { return (IMediaProvider) _mediaProviderProperty.GetValue(); }
      set { _mediaProviderProperty.SetValue(value); }
    }

    public Property MediaProviderPathProperty
    {
      get { return _mediaProviderPathProperty; }
    }

    public string MediaProviderPath
    {
      get { return (string) _mediaProviderPathProperty.GetValue(); }
      set { _mediaProviderPathProperty.SetValue(value); }
    }

    public Property IsMediaProviderPathValidProperty
    {
      get { return _isMediaProviderPathValidProperty; }
    }

    public bool IsMediaProviderPathValid
    {
      get { return (bool) _isMediaProviderPathValidProperty.GetValue(); }
      set { _isMediaProviderPathValidProperty.SetValue(value); }
    }

    public string MediaProviderPathDisplayName
    {
      get { return (string) _mediaProviderPathDisplayNameProperty.GetValue(); }
      set { _mediaProviderPathDisplayNameProperty.SetValue(value); }
    }

    public Property MediaProviderPathDisplayNameProperty
    {
      get { return _mediaProviderPathDisplayNameProperty; }
    }

    public ItemsList MediaProviderPathsTree
    {
      get { return _mediaProviderPathsTree; }
    }

    public Property ShareNameProperty
    {
      get { return _shareNameProperty; }
    }

    public string ShareName
    {
      get { return (string) _shareNameProperty.GetValue(); }
      set { _shareNameProperty.SetValue(value); }
    }

    public Property IsShareNameEmptyProperty
    {
      get { return _isShareNameEmptyProperty; }
    }

    public bool IsShareNameEmpty
    {
      get { return (bool) _isShareNameEmptyProperty.GetValue(); }
      set { _isShareNameEmptyProperty.SetValue(value); }
    }

    #endregion

    #region Public methods

    public void RemoveSelectedShares()
    {
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      foreach (ListItem shareItem in _sharesList)
      {
        if (shareItem.Selected)
        {
          Guid shareId = new Guid(shareItem[ID_KEY]);
          mediaManager.RemoveShare(shareId);
        }
      }
      UpdateSharesList();
    }

    public void SelectMediaProviderAndContinue()
    {
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      IMediaProvider mediaProvider = null;
      foreach (ListItem mediaProviderItem in _mediaProvidersList)
      {
        if (mediaProviderItem.Selected)
        {
          Guid mediaProviderId = new Guid(mediaProviderItem[ID_KEY]);
          if (mediaManager.LocalMediaProviders.TryGetValue(mediaProviderId, out mediaProvider))
            break;
        }
      }
      MediaProvider = mediaProvider;
      // Check, if the choosen MP implements a known navigation interface and go to the navigation screen,
      // if supported
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      if (mediaProvider is IFileSystemMediaProvider)
        workflowManager.NavigatePush(SHARE_ADD_CHOOSE_PATH_STATE_ID);
      else
        // Fallback
        workflowManager.NavigatePush(SHARE_ADD_EDIT_PATH_STATE_ID);
    }

    public void RefreshOrClearSubPathItems(TreeItem pathItem, bool clearSubItems)
    {
      if (clearSubItems)
      {
        pathItem.SubItems.Clear();
        pathItem.SubItems.FireChange();
      }
      else
        RefreshMediaProviderPathList(pathItem.SubItems, pathItem[MP_PATH_KEY]);
    }

    #endregion

    #region Protected methods

    protected void OnShareItemSelectionChanged(Property shareItem, object oldValue)
    {
      UpdateIsSharesSelected();
    }

    protected void OnMediaProviderItemSelectionChanged(Property shareItem, object oldValue)
    {
      UpdateIsMediaProviderSelected();
    }

    protected void OnMediaProviderPathChanged(Property mediaProviderURL, object oldValue)
    {
      UpdateIsMediaProviderPathValid();
      UpdateMediaProviderPathDisplayName();
    }

    protected void OnTreePathSelectionChanged(Property property, object oldValue)
    {
      UpdateMediaProviderTreePath();
    }

    protected void OnShareNameChanged(Property shareName, object oldValue)
    {
      UpdateIsShareNameEmpty();
    }

    protected void UpdateIsSharesSelected()
    {
      bool result = false;
      foreach (ListItem shareItem in _sharesList)
        if (shareItem.Selected)
        {
          result = true;
          break;
        }
      IsSharesSelected = result;
    }

    protected void UpdateIsMediaProviderSelected()
    {
      bool result = false;
      foreach (ListItem mediaProviderItem in _mediaProvidersList)
        if (mediaProviderItem.Selected)
        {
          result = true;
          break;
        }
      IsMediaProviderSelected = result;
    }

    protected static string FindMediaProviderTreePath(ItemsList items)
    {
      foreach (TreeItem directoryItem in items)
        if (directoryItem.Selected)
          return directoryItem.Label(MP_PATH_KEY, null).Evaluate();
        else
        {
          string childPath = FindMediaProviderTreePath(directoryItem.SubItems);
          if (!string.IsNullOrEmpty(childPath))
            return childPath;
        }
      return null;
    }

    protected void UpdateMediaProviderTreePath()
    {
      MediaProviderPath = FindMediaProviderTreePath(MediaProviderPathsTree);
    }

    protected void UpdateIsMediaProviderPathValid()
    {
      IMediaProvider mediaProvider = MediaProvider;
      IsMediaProviderPathValid = mediaProvider != null && mediaProvider.IsResource(MediaProviderPath);
    }

    protected void UpdateMediaProviderPathDisplayName()
    {
      IMediaProvider mediaProvider = MediaProvider;
      MediaProviderPathDisplayName = mediaProvider != null ? mediaProvider.GetFullName(MediaProviderPath) : string.Empty;
    }

    protected void UpdateIsShareNameEmpty()
    {
      IsShareNameEmpty = string.IsNullOrEmpty(ShareName);
    }

    protected void UpdateSharesList()
    {
      // TODO: Re-validate this when we have implemented the communication with the MP-II server
      // Perhaps we should show the server's shares too?
      _sharesList.Clear();
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      foreach (ShareDescriptor share in mediaManager.GetSharesBySystem(SystemName.GetLocalSystemName()).Values)
      {
        ListItem shareItem = new ListItem(NAME_KEY, share.Name);
        shareItem.SetLabel(ID_KEY, share.ShareId.ToString());
        IMediaProvider mediaProvider;
        if (!mediaManager.LocalMediaProviders.TryGetValue(share.MediaProviderId, out mediaProvider))
          mediaProvider = null;
        shareItem.SetLabel(MP_PATH_KEY, share.Path);
        shareItem.SetLabel(PATH_KEY, mediaProvider == null ? share.Path : mediaProvider.GetFullName(share.Path));
        shareItem.SetLabel(SHARE_MEDIAPROVIDER_KEY, mediaProvider == null ? null : mediaProvider.Metadata.Name);
        string categories = StringUtils.Join(", ", share.MediaCategories);
        shareItem.SetLabel(SHARE_CATEGORY_KEY, categories);
        shareItem.SelectedProperty.Attach(OnShareItemSelectionChanged);
        _sharesList.Add(shareItem);
      }
      IsSharesSelected = false;
    }

    protected void UpdateMediaProvidersList()
    {
      _mediaProvidersList.Clear();
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      bool selected = false;
      foreach (IMediaProvider mediaProvider in mediaManager.LocalMediaProviders.Values)
      {
        MediaProviderMetadata metadata = mediaProvider.Metadata;
        ListItem mediaProviderItem = new ListItem(NAME_KEY, metadata.Name);
        mediaProviderItem.SetLabel(ID_KEY, metadata.MediaProviderId.ToString());
        if (MediaProvider != null && MediaProvider.Metadata.MediaProviderId == metadata.MediaProviderId)
        {
          mediaProviderItem.Selected = true;
          selected = true;
        }
        mediaProviderItem.SelectedProperty.Attach(OnMediaProviderItemSelectionChanged);
        _mediaProvidersList.Add(mediaProviderItem);
      }
      IsMediaProviderSelected = selected;
    }

    protected void RefreshMediaProviderPathList(ItemsList items, string path)
    {
      IMediaProvider mp = MediaProvider;
      if (!(mp is IFileSystemMediaProvider))
        // Error case - The path tree can only be shown if the media provider is a file system provider
        return;
      IFileSystemMediaProvider mediaProvider = (IFileSystemMediaProvider) mp;
      foreach (string childPath in mediaProvider.GetChildDirectories(path))
      {
        TreeItem directoryItem = new TreeItem(NAME_KEY, mediaProvider.GetShortName(childPath));
        directoryItem.SetLabel(MP_PATH_KEY, childPath);
        directoryItem.SetLabel(PATH_KEY, mediaProvider.GetFullName(childPath));
        if (!string.IsNullOrEmpty(MediaProviderPath) && MediaProviderPath == childPath)
          directoryItem.Selected = true;
        directoryItem.SelectedProperty.Attach(OnTreePathSelectionChanged);
        items.Add(directoryItem);
      }
      items.FireChange();
    }

    protected void UpdateMediaProviderPathTree()
    {
      _mediaProviderPathsTree.Clear();
      RefreshMediaProviderPathList(_mediaProviderPathsTree, "/");
    }

    protected void ClearAllProperties()
    {
      MediaProvider = null;
      MediaProviderPath = string.Empty;
      ShareName = string.Empty;
    }

    protected void PrepareState(Guid workflowState)
    {
      if (workflowState == SHARES_OVERVIEW_STATE_ID)
      {
        UpdateSharesList();
        ClearAllProperties();
      }
      else if (workflowState == REMOVE_SHARES_STATE_ID)
      {
        UpdateSharesList();
      }
      else if (workflowState == SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID)
      {
        UpdateMediaProvidersList();
      }
      else if (workflowState == SHARE_ADD_CHOOSE_PATH_STATE_ID)
      {
        UpdateMediaProviderPathTree();
      }
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(SHARESCONFIG_MODEL_ID_STR); }
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      PrepareState(newContext.WorkflowState.StateId);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // TODO
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      PrepareState(newContext.WorkflowState.StateId);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      PrepareState(newContext.WorkflowState.StateId);
    }

    public void UpdateMenuActions(NavigationContext context, ICollection<WorkflowStateAction> actions)
    {
      // Not used yet, currently we don't show any menu during the shares configuration process.
      // Perhaps we'll add menu actions for different convenience procedures like initializing the
      // shares to their default setting, ...
    }

    public void UpdateContextMenuActions(NavigationContext context, ICollection<WorkflowStateAction> actions)
    {
      // Not used yet
    }

    #endregion
  }
}
