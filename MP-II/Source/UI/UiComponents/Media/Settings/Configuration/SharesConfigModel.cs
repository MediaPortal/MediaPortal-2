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
    #region Enums

    protected enum ShareConfigMode
    {
      AddShare,
      EditShare,
    }

    #endregion

    #region Consts

    public const string SHARESCONFIG_MODEL_ID_STR = "1768FC91-86B9-4f78-8A4C-E204F0D51502";

    public const string SHARES_OVERVIEW_STATE_ID_STR = "36B3F24A-29B4-4cb4-BC7D-434C51491CD2";

    public const string SHARES_REMOVE_STATE_ID_STR = "900BA520-F989-48c0-B076-5DAD61945845";
    
    public const string SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID_STR = "F3163500-3015-4a6f-91F6-A3DA5DC3593C";
    public const string SHARE_ADD_EDIT_PATH_STATE_ID_STR = "652C5A9F-EA50-4076-886B-B28FD167AD66";
    public const string SHARE_ADD_CHOOSE_PATH_STATE_ID_STR = "5652A9C9-6B20-45f0-889E-CFBF6086FB0A";
    public const string SHARE_ADD_EDIT_NAME_STATE_ID_STR = "ACDD705B-E60B-454a-9671-1A12A3A3985A";
    public const string SHARE_ADD_CHOOSE_CATEGORIES_STATE_ID_STR = "6218FE5B-767E-48e6-9691-65E466B6020B";
    public const string SHARE_ADD_CHOOSE_METADATA_EXTRACTORS_STATE_ID_STR = "B4D50B90-A5D7-48a1-8C0E-4DC2CB9B881D";

    public const string SHARE_EDIT_STATE_ID_STR = "F68E8EB1-00B2-4951-9948-110CFCA93E8D";
    public const string SHARE_EDIT_EDIT_NAME_STATE_ID_STR = "09AB8C58-AE4F-411d-AB53-BB02031A31BD";
    public const string SHARE_EDIT_CHOOSE_CATEGORIES_STATE_ID_STR = "B34CDB4A-2B3F-4df3-B5D1-2B560D8051FD";
    public const string SHARE_EDIT_CHOOSE_METADATA_EXTRACTORS_STATE_ID_STR = "2820E9DE-4140-46bb-A36A-34C2EA765B88";

    // Keys for the ListItem's Labels in the ItemsLists
    public const string NAME_KEY = "Name";
    public const string ID_KEY = "Id";
    public const string DESCRIPTION_KEY = "Description";
    public const string MP_PATH_KEY = "MediaProviderPath";
    public const string PATH_KEY = "Path";
    public const string SHARE_MEDIAPROVIDER_KEY = "MediaProvider";
    public const string SHARE_CATEGORY_KEY = "Category";

    public const string ADD_SHARE_TITLE = "[SharesConfig.AddShare]";
    public const string EDIT_SHARE_TITLE = "[SharesConfig.EditShare]";

    public static Guid SHARES_OVERVIEW_STATE_ID = new Guid(SHARES_OVERVIEW_STATE_ID_STR);
    
    public static Guid SHARES_REMOVE_STATE_ID = new Guid(SHARES_REMOVE_STATE_ID_STR);

    public static Guid SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID = new Guid(SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID_STR);
    public static Guid SHARE_ADD_EDIT_PATH_STATE_ID = new Guid(SHARE_ADD_EDIT_PATH_STATE_ID_STR);
    public static Guid SHARE_ADD_CHOOSE_PATH_STATE_ID = new Guid(SHARE_ADD_CHOOSE_PATH_STATE_ID_STR);
    public static Guid SHARE_ADD_EDIT_NAME_STATE_ID = new Guid(SHARE_ADD_EDIT_NAME_STATE_ID_STR);
    public static Guid SHARE_ADD_CHOOSE_CATEGORIES_STATE_ID = new Guid(SHARE_ADD_CHOOSE_CATEGORIES_STATE_ID_STR);
    public static Guid SHARE_ADD_CHOOSE_METADATA_EXTRACTORS_STATE_ID = new Guid(SHARE_ADD_CHOOSE_METADATA_EXTRACTORS_STATE_ID_STR);

    public static Guid SHARE_EDIT_STATE_ID = new Guid(SHARE_EDIT_STATE_ID_STR);
    public static Guid SHARE_EDIT_EDIT_NAME_STATE_ID = new Guid(SHARE_EDIT_EDIT_NAME_STATE_ID_STR);
    public static Guid SHARE_EDIT_CHOOSE_CATEGORIES_STATE_ID = new Guid(SHARE_EDIT_CHOOSE_CATEGORIES_STATE_ID_STR);
    public static Guid SHARE_EDIT_CHOOSE_METADATA_EXTRACTORS_STATE_ID = new Guid(SHARE_EDIT_CHOOSE_METADATA_EXTRACTORS_STATE_ID_STR);

    #endregion

    #region Protected fields

    protected ItemsList _sharesList;
    protected ShareConfigMode _configMode;
    protected ItemsList _allMediaProvidersList;
    protected Property _isSharesSelectedProperty;
    protected Property _isMediaProviderSelectedProperty;
    protected Property _mediaProviderProperty;
    protected Property _mediaProviderPathProperty;
    protected Property _isMediaProviderPathValidProperty;
    protected Property _mediaProviderPathDisplayNameProperty;
    protected ItemsList _mediaProviderPathsTree;
    protected Property _shareNameProperty;
    protected Property _isShareNameEmptyProperty;
    protected ItemsList _allMediaCategoriesList;
    protected Property _isMetadataExtractorsSelectedProperty;
    protected ItemsList _allMetadataExtractorsList;
    protected ICollection<string> _mediaCategories = new HashSet<string>();
    protected ICollection<Guid> _metadataExtractorIds = new HashSet<Guid>();
    protected Guid _currentShareId;

    #endregion

    #region Ctor

    public SharesConfigModel()
    {
      // TODO Albert: break the event handler reference from the lists to the Skin's controls
      _sharesList = new ItemsList();
      _allMediaProvidersList = new ItemsList();
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
      _allMediaCategoriesList = new ItemsList();
      _isMetadataExtractorsSelectedProperty = new Property(typeof(bool), false);
      _allMetadataExtractorsList = new ItemsList();
      _mediaCategories = new HashSet<string>();
      _metadataExtractorIds = new HashSet<Guid>();
    }

    #endregion

    #region Public properties

    /// <summary>
    /// List of all shares to be displayed in the shares config screens. To be used in the GUI.
    /// </summary>
    public ItemsList Shares
    {
      get { return _sharesList; }
    }

    public string ConfigShareTitle
    {
      get { return _configMode == ShareConfigMode.AddShare ? ADD_SHARE_TITLE : EDIT_SHARE_TITLE; }
    }

    /// <summary>
    /// List of all available media providers. To be used in the GUI.
    /// </summary>
    public ItemsList AllMediaProviders
    {
      get { return _allMediaProvidersList; }
    }

    public Property IsSharesSelectedProperty
    {
      get { return _isSharesSelectedProperty; }
    }

    /// <summary>
    /// <c>true</c> if at least one share is selected. To be used in the GUI.
    /// </summary>
    public bool IsSharesSelected
    {
      get { return (bool) _isSharesSelectedProperty.GetValue(); }
      set { _isSharesSelectedProperty.SetValue(value); }
    }

    public Property IsMediaProviderSelectedProperty
    {
      get { return _isMediaProviderSelectedProperty; }
    }

    /// <summary>
    /// <c>true</c> if at least one media provider is selected. To be used in the GUI.
    /// </summary>
    public bool IsMediaProviderSelected
    {
      get { return (bool) _isMediaProviderSelectedProperty.GetValue(); }
      set { _isMediaProviderSelectedProperty.SetValue(value); }
    }

    public Property MediaProviderProperty
    {
      get { return _mediaProviderProperty; }
    }

    /// <summary>
    /// Selected media provider. To be used in the GUI.
    /// </summary>
    public IMediaProvider MediaProvider
    {
      get { return (IMediaProvider) _mediaProviderProperty.GetValue(); }
      set { _mediaProviderProperty.SetValue(value); }
    }

    public Property MediaProviderPathProperty
    {
      get { return _mediaProviderPathProperty; }
    }

    /// <summary>
    /// Selected media provider path. To be used in the GUI.
    /// </summary>
    public string MediaProviderPath
    {
      get { return (string) _mediaProviderPathProperty.GetValue(); }
      set { _mediaProviderPathProperty.SetValue(value); }
    }

    public Property IsMediaProviderPathValidProperty
    {
      get { return _isMediaProviderPathValidProperty; }
    }

    /// <summary>
    /// <c>true</c> if the selected media provider path is valid in the media provider.
    /// To be used in the GUI.
    /// </summary>
    public bool IsMediaProviderPathValid
    {
      get { return (bool) _isMediaProviderPathValidProperty.GetValue(); }
      set { _isMediaProviderPathValidProperty.SetValue(value); }
    }

    public Property MediaProviderPathDisplayNameProperty
    {
      get { return _mediaProviderPathDisplayNameProperty; }
    }

    /// <summary>
    /// Human-readable display name of the selected media provider path. To be used in the GUI.
    /// </summary>
    public string MediaProviderPathDisplayName
    {
      get { return (string) _mediaProviderPathDisplayNameProperty.GetValue(); }
      set { _mediaProviderPathDisplayNameProperty.SetValue(value); }
    }

    /// <summary>
    /// Paths tree of the selected media provider, if the media provider supports path
    /// navigation. To be used in the GUI.
    /// </summary>
    public ItemsList MediaProviderPathsTree
    {
      get { return _mediaProviderPathsTree; }
    }

    public Property ShareNameProperty
    {
      get { return _shareNameProperty; }
    }

    /// <summary>
    /// Edited name for the current share. To be used in the GUI.
    /// </summary>
    public string ShareName
    {
      get { return (string) _shareNameProperty.GetValue(); }
      set { _shareNameProperty.SetValue(value); }
    }

    public Property IsShareNameEmptyProperty
    {
      get { return _isShareNameEmptyProperty; }
    }

    /// <summary>
    /// <c>true</c> if the edited share name is empty. To be used in the GUI.
    /// </summary>
    public bool IsShareNameEmpty
    {
      get { return (bool) _isShareNameEmptyProperty.GetValue(); }
      set { _isShareNameEmptyProperty.SetValue(value); }
    }

    /// <summary>
    /// List of all media categories. To be used in the GUI.
    /// </summary>
    public ItemsList AllMediaCategories
    {
      get { return _allMediaCategoriesList; }
    }

    /// <summary>
    /// Collection of choosen media categories for the current share.
    /// </summary>
    public ICollection<string> MediaCategories
    {
      get { return _mediaCategories; }
    }

    public Property IsMetadataExtractorsSelectedProperty
    {
      get { return _isMetadataExtractorsSelectedProperty; }
    }

    /// <summary>
    /// <c>true</c> if at least one metadata extractor was selected. To be used in the GUI.
    /// </summary>
    public bool IsMetadataExtractorsSelected
    {
      get { return (bool) _isMetadataExtractorsSelectedProperty.GetValue(); }
      set { _isMetadataExtractorsSelectedProperty.SetValue(value); }
    }

    /// <summary>
    /// List of all available metadata extractors. To be used in the GUI.
    /// </summary>
    public ItemsList AllMetadataExtractors
    {
      get { return _allMetadataExtractorsList; }
    }

    /// <summary>
    /// Collection of the ids of the choosen metadata extractors.
    /// </summary>
    public ICollection<Guid> MetadataExtractorIds
    {
      get { return _metadataExtractorIds; }
    }

    /// <summary>
    /// Gets or sets the id of the share currently edited.
    /// </summary>
    public Guid CurrentShareId
    {
      get { return _currentShareId; }
      set { _currentShareId = value; }
    }

    #endregion

    #region Public methods

    public void RemoveSelectedShares()
    {
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      foreach (ListItem shareItem in _sharesList)
        if (shareItem.Selected)
        {
          Guid shareId = new Guid(shareItem[ID_KEY]);
          mediaManager.RemoveShare(shareId);
        }
      ClearAllConfiguredProperties();
    }

    public void SelectMediaProviderAndContinue()
    {
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      IMediaProvider mediaProvider = null;
      foreach (ListItem mediaProviderItem in _allMediaProvidersList)
      {
        if (mediaProviderItem.Selected)
        {
          Guid mediaProviderId = new Guid(mediaProviderItem[ID_KEY]);
          if (mediaManager.LocalMediaProviders.TryGetValue(mediaProviderId, out mediaProvider))
            break;
        }
      }
      if (mediaProvider == null)
        // Error case: Should not happen
        return;
      if (MediaProvider == null ||
          MediaProvider.Metadata.MediaProviderId != mediaProvider.Metadata.MediaProviderId)
        ClearAllConfiguredProperties();
      MediaProvider = mediaProvider;
      // Check if the choosen MP implements a known path navigation interface and go to that screen,
      // if supported
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      if (mediaProvider is IFileSystemMediaProvider)
        workflowManager.NavigatePush(SHARE_ADD_CHOOSE_PATH_STATE_ID);
      else // If needed, add other path navigation screens here
        // Fallback: Simple TextBox path editor screen
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

    public void FinishShareConfiguration()
    {
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      if (_configMode == ShareConfigMode.AddShare)
      {
        mediaManager.RegisterShare(SystemName.GetLocalSystemName(), MediaProvider.Metadata.MediaProviderId,
            MediaProviderPath, ShareName, MediaCategories, MetadataExtractorIds);
      }
      else if (_configMode == ShareConfigMode.EditShare)
      {
        mediaManager.SetShareName(CurrentShareId, ShareName);
        mediaManager.SetShareCategoriesAndMetadataExtractors(CurrentShareId, MediaCategories, MetadataExtractorIds);
      }
      else
        throw new NotImplementedException(string.Format("ShareConfigMode '{0}' is not implemented", _configMode));
      ClearAllConfiguredProperties();
    }

    public void EditSelectedShare()
    {
      foreach (ListItem shareItem in _sharesList)
        if (shareItem.Selected)
        {
          CurrentShareId = new Guid(shareItem[ID_KEY]);
          InitializePropertiesWithShare(CurrentShareId);
          break;
        }
      _configMode = ShareConfigMode.EditShare;
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(SHARE_EDIT_EDIT_NAME_STATE_ID);
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

    protected void OnMediaCategoryItemSelectionChanged(Property property, object oldValue)
    {
      UpdateMediaCategories();
    }

    protected void OnMetadataExtractorItemSelectionChanged(Property property, object oldValue)
    {
      UpdateMetadataExtractors();
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
      foreach (ListItem mediaProviderItem in _allMediaProvidersList)
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
      MediaProviderPathDisplayName = mediaProvider != null ? mediaProvider.GetResourcePath(MediaProviderPath) : string.Empty;
    }

    protected void UpdateIsShareNameEmpty()
    {
      IsShareNameEmpty = string.IsNullOrEmpty(ShareName);
    }

    protected void UpdateMediaCategories()
    {
      _mediaCategories.Clear();
      foreach (ListItem categoryItem in _allMediaCategoriesList)
        if (categoryItem.Selected)
          _mediaCategories.Add(categoryItem[NAME_KEY]);
      UpdateMetadataExtractorsFromMediaCategories();
    }

    protected void UpdateMetadataExtractors()
    {
      _metadataExtractorIds.Clear();
      foreach (ListItem metadataExtractorItem in _allMetadataExtractorsList)
        if (metadataExtractorItem.Selected)
          _metadataExtractorIds.Add(new Guid(metadataExtractorItem[ID_KEY]));
      IsMetadataExtractorsSelected = _metadataExtractorIds.Count > 0;
    }

    protected void UpdateMetadataExtractorsFromMediaCategories()
    {
      _metadataExtractorIds.Clear();
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      foreach (IMetadataExtractor me in mediaManager.LocalMetadataExtractors.Values)
      {
        MetadataExtractorMetadata metadata = me.Metadata;
        if (CollectionUtils.Intersection(metadata.ShareCategories, MediaCategories).Count > 0)
          _metadataExtractorIds.Add(metadata.MetadataExtractorId);
      }
      IsMetadataExtractorsSelected = _metadataExtractorIds.Count > 0;
    }

    protected void UpdateSharesList()
    {
      // TODO: Re-validate this when we have implemented the communication with the MP-II server
      // Perhaps we should show the server's shares too? In this case, we also have to re-validate
      // the way of building the list of media providers and metadata extractors
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
        shareItem.SetLabel(PATH_KEY, mediaProvider == null ? share.Path : mediaProvider.GetResourcePath(share.Path));
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
      _allMediaProvidersList.Clear();
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
        _allMediaProvidersList.Add(mediaProviderItem);
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
      ICollection<string> directories = mediaProvider.GetChildDirectories(path);
      if (directories != null)
        foreach (string childPath in directories)
        {
          TreeItem directoryItem = new TreeItem(NAME_KEY, mediaProvider.GetResourceName(childPath));
          directoryItem.SetLabel(MP_PATH_KEY, childPath);
          directoryItem.SetLabel(PATH_KEY, mediaProvider.GetResourcePath(childPath));
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

    protected static ICollection<string> GetAllAvailableCategories()
    {
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      ICollection<string> result = new HashSet<string>();
      foreach (IMetadataExtractor me in mediaManager.LocalMetadataExtractors.Values)
      {
        MetadataExtractorMetadata metadata = me.Metadata;
        CollectionUtils.AddAll(result, metadata.ShareCategories);
      }
      return result;
    }

    protected void UpdateMediaCategoriesList()
    {
      _allMediaCategoriesList.Clear();
      List<string> allCategories = new List<string>(GetAllAvailableCategories());
      allCategories.Sort();
      foreach (string mediaCategory in allCategories)
      {
        ListItem categoryItem = new ListItem(NAME_KEY, mediaCategory);
        if (MediaCategories.Contains(mediaCategory))
          categoryItem.Selected = true;
        categoryItem.SelectedProperty.Attach(OnMediaCategoryItemSelectionChanged);
        _allMediaCategoriesList.Add(categoryItem);
      }
    }

    protected void UpdateMetadataExtractorsList()
    {
      _allMetadataExtractorsList.Clear();
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      foreach (IMetadataExtractor me in mediaManager.LocalMetadataExtractors.Values)
      {
        MetadataExtractorMetadata metadata = me.Metadata;
        ListItem metadataExtractorItem = new ListItem(NAME_KEY, metadata.Name);
        metadataExtractorItem.SetLabel(ID_KEY, metadata.MetadataExtractorId.ToString());
        if (MetadataExtractorIds.Contains(metadata.MetadataExtractorId))
          metadataExtractorItem.Selected = true;
        metadataExtractorItem.SelectedProperty.Attach(OnMetadataExtractorItemSelectionChanged);
        _allMetadataExtractorsList.Add(metadataExtractorItem);
      }
    }

    protected void ClearAllConfiguredProperties()
    {
      MediaProvider = null;
      MediaProviderPath = string.Empty;
      ShareName = string.Empty;
      MediaCategories.Clear();
      MetadataExtractorIds.Clear();
      // IsMetadataExtractorsSelected has to be cleared also because it is derived from
      // MediaCategories and MetadataExtractors
      IsMetadataExtractorsSelected = false;
    }

    protected bool InitializePropertiesWithShare(Guid shareId)
    {
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      ShareDescriptor shareDescriptor = mediaManager.GetShare(shareId);
      if (shareDescriptor == null)
        return false;
      IMediaProvider mediaProvider;
      if (!mediaManager.LocalMediaProviders.TryGetValue(shareDescriptor.MediaProviderId, out mediaProvider))
        return false;
      MediaProvider = mediaProvider;
      MediaProviderPath = shareDescriptor.Path;
      ShareName = shareDescriptor.Name;
      MediaCategories.Clear();
      CollectionUtils.AddAll(MediaCategories, shareDescriptor.MediaCategories);
      MetadataExtractorIds.Clear();
      CollectionUtils.AddAll(MetadataExtractorIds, shareDescriptor.MetadataExtractorIds);
      // IsMetadataExtractorsSelected has always to be set also because it is derived from
      // MediaCategories and MetadataExtractors
      IsMetadataExtractorsSelected = MetadataExtractorIds.Count > 0;
      return true;
    }

    /// <summary>
    /// Prepares the internal data of this model to match the specified new
    /// <paramref name="workflowState"/>. This method will be called in result of a
    /// forward state navigation as well as for a backward navigation.
    /// </summary>
    protected void PrepareState(Guid workflowState)
    {
      if (workflowState == SHARES_OVERVIEW_STATE_ID)
      {
        UpdateSharesList();
      }
      else if (workflowState == SHARES_REMOVE_STATE_ID)
      {
        UpdateSharesList();
      }
      else if (workflowState == SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID)
      {
        _configMode = ShareConfigMode.AddShare;
        UpdateMediaProvidersList();
      }
      else if (workflowState == SHARE_ADD_EDIT_PATH_STATE_ID)
      {
        // Nothing to prepare
      }
      else if (workflowState == SHARE_ADD_CHOOSE_PATH_STATE_ID)
      {
        UpdateMediaProviderPathTree();
      }
      else if (workflowState == SHARE_ADD_EDIT_NAME_STATE_ID)
      {}
      else if (workflowState == SHARE_ADD_CHOOSE_CATEGORIES_STATE_ID)
      {
        UpdateMediaCategoriesList();
        // We'll reset the choosen metadata extractors here, if the user goes back
        // to the categories screen - thats for simplicity, else, we would have
        // to track which MEs the user has choosen explicitly and which are in the
        // list because a category was choosen
        UpdateMetadataExtractorsFromMediaCategories();
      }
      else if (workflowState == SHARE_ADD_CHOOSE_METADATA_EXTRACTORS_STATE_ID)
      {
        UpdateMetadataExtractorsList();
      }
      else if (workflowState == SHARE_EDIT_STATE_ID)
      {
        UpdateSharesList();
      }
      else if (workflowState == SHARE_EDIT_EDIT_NAME_STATE_ID)
      {}
      else if (workflowState == SHARE_EDIT_CHOOSE_CATEGORIES_STATE_ID)
      {
        UpdateMediaCategoriesList();
        // We'll reset the choosen metadata extractors here, if the user goes back
        // to the categories screen - thats for simplicity, else, we would have
        // to track which MEs the user has choosen explicitly and which are in the
        // list because a category was choosen
        UpdateMetadataExtractorsFromMediaCategories();
      }
      else if (workflowState == SHARE_EDIT_CHOOSE_METADATA_EXTRACTORS_STATE_ID)
      {
        UpdateMetadataExtractorsList();
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
