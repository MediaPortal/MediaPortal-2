#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Globalization;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Shares;
using MediaPortal.Utilities;

namespace UiComponents.SkinBase.Settings.Configuration.Shares
{
  /// <summary>
  /// Provides a workflow model to attend the complex configuration process for local shares in the MP-II configuration.
  /// </summary>
  public class SharesConfigModel : IWorkflowModel, IDisposable
  {
    #region Enums

    protected enum ShareEditMode
    {
      AddShare,
      EditShare,
    }

    #endregion

    #region Consts

    public const string SHARESCONFIG_MODEL_ID_STR = "1768FC91-86B9-4f78-8A4C-E204F0D51502";

    public const string SHARES_OVERVIEW_STATE_ID_STR = "36B3F24A-29B4-4cb4-BC7D-434C51491CD2";

    public const string SHARES_REMOVE_STATE_ID_STR = "900BA520-F989-48c0-B076-5DAD61945845";
    
    public const string SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID_STR = "6F7EB06A-2AC6-4bcb-9003-F5DA44E03C26";
    public const string SHARE_EDIT_CHOOSE_MEDIA_PROVIDER_STATE_ID_STR = "F3163500-3015-4a6f-91F6-A3DA5DC3593C";
    public const string SHARE_EDIT_EDIT_PATH_STATE_ID_STR = "652C5A9F-EA50-4076-886B-B28FD167AD66";
    public const string SHARE_EDIT_CHOOSE_PATH_STATE_ID_STR = "5652A9C9-6B20-45f0-889E-CFBF6086FB0A";
    public const string SHARE_EDIT_EDIT_NAME_STATE_ID_STR = "ACDD705B-E60B-454a-9671-1A12A3A3985A";
    public const string SHARE_EDIT_CHOOSE_CATEGORIES_STATE_ID_STR = "6218FE5B-767E-48e6-9691-65E466B6020B";

    public const string SHARE_EDIT_STATE_ID_STR = "F68E8EB1-00B2-4951-9948-110CFCA93E8D";

    // Keys for the ListItem's Labels in the ItemsLists
    public const string NAME_KEY = "Name";
    public const string ID_KEY = "Id";
    public const string DESCRIPTION_KEY = "Description";
    public const string RESOURCE_ACCESSOR_KEY = "ResourceAccessor";
    public const string PATH_KEY = "Path";
    public const string SHARE_MEDIAPROVIDER_KEY = "MediaProvider";
    public const string SHARE_CATEGORY_KEY = "Category";

    public const string ADD_SHARE_TITLE_RES = "[SharesConfig.AddShare]";
    public const string EDIT_SHARE_TITLE_RES = "[SharesConfig.EditShare]";

    public const string SHARES_CONFIG_RELOCATE_DIALOG_SCREEN = "shares_config_relocate_dialog";

    public static Guid SHARES_OVERVIEW_STATE_ID = new Guid(SHARES_OVERVIEW_STATE_ID_STR);
    
    public static Guid SHARES_REMOVE_STATE_ID = new Guid(SHARES_REMOVE_STATE_ID_STR);

    public static Guid SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID = new Guid(SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID_STR);
    public static Guid SHARE_EDIT_CHOOSE_MEDIA_PROVIDER_STATE_ID = new Guid(SHARE_EDIT_CHOOSE_MEDIA_PROVIDER_STATE_ID_STR);
    public static Guid SHARE_EDIT_EDIT_PATH_STATE_ID = new Guid(SHARE_EDIT_EDIT_PATH_STATE_ID_STR);
    public static Guid SHARE_EDIT_CHOOSE_PATH_STATE_ID = new Guid(SHARE_EDIT_CHOOSE_PATH_STATE_ID_STR);
    public static Guid SHARE_EDIT_EDIT_NAME_STATE_ID = new Guid(SHARE_EDIT_EDIT_NAME_STATE_ID_STR);
    public static Guid SHARE_EDIT_CHOOSE_CATEGORIES_STATE_ID = new Guid(SHARE_EDIT_CHOOSE_CATEGORIES_STATE_ID_STR);

    public static Guid SHARE_EDIT_STATE_ID = new Guid(SHARE_EDIT_STATE_ID_STR);

    #endregion

    #region Protected fields

    protected ItemsList _sharesList;
    protected ShareEditMode _editMode;
    protected ItemsList _allBaseMediaProvidersList;
    protected AbstractProperty _isSharesSelectedProperty;
    protected AbstractProperty _isMediaProviderSelectedProperty;
    protected AbstractProperty _mediaProviderProperty;
    protected AbstractProperty _choosenResourcePathStrProperty;
    protected AbstractProperty _choosenResourcePathProperty;
    protected AbstractProperty _isChoosenPathValidProperty;
    protected AbstractProperty _choosenResourcePathDisplayNameProperty;
    protected ItemsList _mediaProviderPathsTree;
    protected AbstractProperty _shareNameProperty;
    protected AbstractProperty _isShareNameEmptyProperty;
    protected ItemsList _allMediaCategoriesList;
    protected ICollection<string> _mediaCategories = new HashSet<string>();
    protected Guid _currentShareId;

    #endregion

    #region Ctor

    public SharesConfigModel()
    {
      _sharesList = new ItemsList();
      _allBaseMediaProvidersList = new ItemsList();
      _isSharesSelectedProperty = new WProperty(typeof(bool), false);
      _isMediaProviderSelectedProperty = new WProperty(typeof(bool), false);
      _mediaProviderProperty = new WProperty(typeof(IBaseMediaProvider), null);
      _choosenResourcePathStrProperty = new WProperty(typeof(string), string.Empty);
      _choosenResourcePathStrProperty.Attach(OnChoosenResourcePathStrChanged);
      _choosenResourcePathProperty = new WProperty(typeof(ResourcePath), null);
      _choosenResourcePathProperty.Attach(OnChoosenResourcePathChanged);
      _isChoosenPathValidProperty = new WProperty(typeof(bool), false);
      _choosenResourcePathDisplayNameProperty = new WProperty(typeof(string), string.Empty);
      _mediaProviderPathsTree = new ItemsList();
      _shareNameProperty = new WProperty(typeof(string), string.Empty);
      _shareNameProperty.Attach(OnShareNameChanged);
      _isShareNameEmptyProperty = new WProperty(typeof(bool), true);
      _allMediaCategoriesList = new ItemsList();
      _mediaCategories = new HashSet<string>();
    }

    public void Dispose()
    {
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
      get { return _editMode == ShareEditMode.AddShare ? ADD_SHARE_TITLE_RES : EDIT_SHARE_TITLE_RES; }
    }

    /// <summary>
    /// List of all available base media providers. To be used in the GUI.
    /// </summary>
    public ItemsList AllBaseMediaProviders
    {
      get { return _allBaseMediaProvidersList; }
    }

    public AbstractProperty IsSharesSelectedProperty
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

    public AbstractProperty IsMediaProviderSelectedProperty
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

    public AbstractProperty MediaProviderProperty
    {
      get { return _mediaProviderProperty; }
    }

    /// <summary>
    /// Selected base media provider. To be used in the GUI.
    /// </summary>
    public IBaseMediaProvider MediaProvider
    {
      get { return (IBaseMediaProvider) _mediaProviderProperty.GetValue(); }
      set { _mediaProviderProperty.SetValue(value); }
    }

    public AbstractProperty ChoosenResourcePathStrProperty
    {
      get { return _choosenResourcePathStrProperty; }
    }

    /// <summary>
    /// Selected media provider path. To be used in the GUI.
    /// </summary>
    public string ChoosenResourcePathStr
    {
      get { return (string) _choosenResourcePathStrProperty.GetValue(); }
      set { _choosenResourcePathStrProperty.SetValue(value); }
    }

    public AbstractProperty ChoosenResourcePathProperty
    {
      get { return _choosenResourcePathProperty; }
    }

    /// <summary>
    /// Selected media provider path as resource path instance. To be used in the GUI.
    /// </summary>
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
    /// <c>true</c> if the selected media provider path is valid in the media provider.
    /// To be used in the GUI.
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
    /// Human-readable display name of the selected media provider path. To be used in the GUI.
    /// </summary>
    public string ChoosenResourcePathDisplayName
    {
      get { return (string) _choosenResourcePathDisplayNameProperty.GetValue(); }
      set { _choosenResourcePathDisplayNameProperty.SetValue(value); }
    }

    /// <summary>
    /// Paths tree of the selected media provider, if the media provider supports path
    /// navigation. To be used in the GUI.
    /// </summary>
    public ItemsList MediaProviderPathsTree
    {
      get { return _mediaProviderPathsTree; }
    }

    public AbstractProperty ShareNameProperty
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

    public AbstractProperty IsShareNameEmptyProperty
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

    public void RemoveSelectedSharesAndFinish()
    {
      ILocalSharesManagement sharesManagement = ServiceScope.Get<ILocalSharesManagement>();
      foreach (ListItem shareItem in _sharesList)
        if (shareItem.Selected)
        {
          Guid shareId = new Guid(shareItem[ID_KEY]);
          sharesManagement.RemoveShare(shareId);
        }
      ClearAllConfiguredProperties();
      NavigateBackToOverview();
    }

    public void SelectMediaProviderAndContinue()
    {
      IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
      IBaseMediaProvider mediaProvider = null;
      foreach (ListItem mediaProviderItem in _allBaseMediaProvidersList)
      {
        if (mediaProviderItem.Selected)
        {
          Guid mediaProviderId = new Guid(mediaProviderItem[ID_KEY]);
          IMediaProvider mp;
          if (mediaAccessor.LocalMediaProviders.TryGetValue(mediaProviderId, out mp))
          {
            mediaProvider = mp as IBaseMediaProvider;
            break;
          }
        }
      }
      if (mediaProvider == null)
        // Error case: Should not happen
        return;
      IMediaProvider oldMediaProvider = MediaProvider;
      if (oldMediaProvider == null ||
          oldMediaProvider.Metadata.MediaProviderId != mediaProvider.Metadata.MediaProviderId)
        ClearAllConfiguredProperties();
      MediaProvider = mediaProvider;
      // Check if the choosen MP implements a known path navigation interface and go to that screen,
      // if supported
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      IResourceAccessor rootAccessor = mediaProvider.CreateMediaItemAccessor("/");
      if (rootAccessor is IFileSystemResourceAccessor)
        workflowManager.NavigatePush(SHARE_EDIT_CHOOSE_PATH_STATE_ID, null);
      else // If needed, add other path navigation screens here
        // Fallback: Simple TextBox path editor screen
        workflowManager.NavigatePush(SHARE_EDIT_EDIT_PATH_STATE_ID, null);
    }

    public void RefreshOrClearSubPathItems(TreeItem pathItem, bool clearSubItems)
    {
      if (clearSubItems)
      {
        pathItem.SubItems.Clear();
        pathItem.SubItems.FireChange();
      }
      else
        RefreshMediaProviderPathList(pathItem.SubItems, (IFileSystemResourceAccessor) pathItem.AdditionalProperties[RESOURCE_ACCESSOR_KEY]);
    }

    public void FinishShareConfiguration()
    {
      ILocalSharesManagement sharesManagement = ServiceScope.Get<ILocalSharesManagement>();
      if (_editMode == ShareEditMode.AddShare)
      {
        sharesManagement.RegisterShare(ChoosenResourcePath, ShareName, MediaCategories);
        ClearAllConfiguredProperties();
        NavigateBackToOverview();
      }
      else if (_editMode == ShareEditMode.EditShare)
      {
        Share share = sharesManagement.GetShare(CurrentShareId);
        if (share != null)
        {
          if (share.BaseResourcePath != ChoosenResourcePath)
          {
            IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
            screenManager.ShowDialog(SHARES_CONFIG_RELOCATE_DIALOG_SCREEN);
          }
          else
            UpdateShareAndFinish(RelocationMode.ReImport);
        }
      }
      else
        throw new NotImplementedException(string.Format("ShareEditMode '{0}' is not implemented", _editMode));
    }

    public void FinishDoRelocate()
    {
      UpdateShareAndFinish(RelocationMode.Relocate);
    }

    public void FinishDoReImport()
    {
      UpdateShareAndFinish(RelocationMode.ReImport);
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
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(SHARE_EDIT_CHOOSE_MEDIA_PROVIDER_STATE_ID, null);
    }

    public void NavigateBackToOverview()
    {
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePopToState(SHARES_OVERVIEW_STATE_ID, false);
    }

    #endregion

    #region Protected methods

    void OnShareItemSelectionChanged(AbstractProperty shareItem, object oldValue)
    {
      UpdateIsSharesSelected();
    }

    void OnMediaProviderItemSelectionChanged(AbstractProperty shareItem, object oldValue)
    {
      UpdateIsMediaProviderSelected();
    }

    void OnChoosenResourcePathStrChanged(AbstractProperty mediaProviderURL, object oldValue)
    {
      string str = ChoosenResourcePathStr;
      ResourcePath result = null;
      if (!string.IsNullOrEmpty(str))
      {
        IBaseMediaProvider mp = MediaProvider;
        if (mp.IsResource(str))
          result = new ResourcePath(new ProviderPathSegment[]
              {
                new ProviderPathSegment(mp.Metadata.MediaProviderId, str, true), 
              });
        else
          try
          {
            result = ResourcePath.Deserialize(str);
          }
          catch (ArgumentException)
          {
            return;
          }
      }
      // Will trigger the change handler of property ChoosenResourcePath - which evaluates IsChoosenPathValid and ChoosenPathDisplayName
      ChoosenResourcePath = result;
    }

    void OnChoosenResourcePathChanged(AbstractProperty mediaProviderURL, object oldValue)
    {
      // Don't update ChoosenResourcePathStr - the string is the master and can be written in several formats
      UpdateIsChoosenPathValid();
      UpdateChoosenPathDisplayName();
    }

    void OnTreePathSelectionChanged(AbstractProperty property, object oldValue)
    {
      UpdateChoosenResourcePath();
    }

    void OnShareNameChanged(AbstractProperty shareName, object oldValue)
    {
      UpdateIsShareNameEmpty();
    }

    void OnMediaCategoryItemSelectionChanged(AbstractProperty property, object oldValue)
    {
      UpdateMediaCategories();
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
      foreach (ListItem mediaProviderItem in _allBaseMediaProvidersList)
        if (mediaProviderItem.Selected)
        {
          result = true;
          break;
        }
      IsMediaProviderSelected = result;
    }

    protected static IFileSystemResourceAccessor FindChoosenResourcePath(ItemsList items)
    {
      foreach (TreeItem directoryItem in items)
        if (directoryItem.Selected)
          return (IFileSystemResourceAccessor) directoryItem.AdditionalProperties[RESOURCE_ACCESSOR_KEY];
        else
        {
          IFileSystemResourceAccessor childPath = FindChoosenResourcePath(directoryItem.SubItems);
          if (childPath != null)
            return childPath;
        }
      return null;
    }

    protected void UpdateChoosenResourcePath()
    {
      IFileSystemResourceAccessor ra = FindChoosenResourcePath(MediaProviderPathsTree);
      ChoosenResourcePath = ra == null ? null : ra.LocalResourcePath;
    }

    protected void UpdateIsChoosenPathValid()
    {
      try
      {
        ResourcePath rp = ChoosenResourcePath;
        if (rp == null)
        {
          IsChoosenPathValid = false;
          return;
        }
        // Check if we can create an item accessor - if we get an exception, the path is not valid
        IResourceAccessor ra = rp.CreateLocalMediaItemAccessor();
        ra.Dispose();
        IsChoosenPathValid = true;
      }
      catch (Exception)
      {
        IsChoosenPathValid = false;
      }
    }

    protected void UpdateChoosenPathDisplayName()
    {
      try
      {
        ResourcePath rp = ChoosenResourcePath;
        if (rp == null)
        {
          ChoosenResourcePathDisplayName = string.Empty;
          return;
        }
        IResourceAccessor ra = rp.CreateLocalMediaItemAccessor();
        ChoosenResourcePathDisplayName = ra.ResourcePathName;
        ra.Dispose();
      }
      catch (Exception)
      {
        ChoosenResourcePathDisplayName = string.Empty;
      }
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
    }

    protected void UpdateSharesList()
    {
      _sharesList.Clear();
      ILocalSharesManagement sharesManagement = ServiceScope.Get<ILocalSharesManagement>();
      bool selected = false;
      List<Share> shareDescriptors = new List<Share>(sharesManagement.Shares.Values);
      shareDescriptors.Sort((a, b) => a.Name.CompareTo(b.Name));
      foreach (Share share in shareDescriptors)
      {
        ListItem shareItem = new ListItem(NAME_KEY, share.Name);
        shareItem.SetLabel(ID_KEY, share.ShareId.ToString());
        IResourceAccessor resourceAccessor = share.BaseResourcePath.CreateLocalMediaItemAccessor();
        shareItem.AdditionalProperties[RESOURCE_ACCESSOR_KEY] = resourceAccessor;
        shareItem.SetLabel(PATH_KEY, resourceAccessor.ResourcePathName);
        IMediaProvider firstMediaProvider = GetFirstMediaProvider(share);
        shareItem.SetLabel(SHARE_MEDIAPROVIDER_KEY, firstMediaProvider == null ? null : firstMediaProvider.Metadata.Name);
        string categories = StringUtils.Join(", ", share.MediaCategories);
        shareItem.SetLabel(SHARE_CATEGORY_KEY, categories);
        if (shareDescriptors.Count == 1)
        {
          selected = true;
          shareItem.Selected = true;
        }
        shareItem.SelectedProperty.Attach(OnShareItemSelectionChanged);
        _sharesList.Add(shareItem);
      }
      _sharesList.FireChange();
      IsSharesSelected = selected;
    }

    protected IBaseMediaProvider GetFirstMediaProvider(Share share)
    {
      IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
      IEnumerator<ProviderPathSegment> enumer = share.BaseResourcePath.GetEnumerator();
      IMediaProvider result;
      if (!enumer.MoveNext() || !mediaAccessor.LocalMediaProviders.TryGetValue(enumer.Current.ProviderId, out result))
        return null;
      return result as IBaseMediaProvider;
    }

    protected void UpdateMediaProvidersList()
    {
      _allBaseMediaProvidersList.Clear();
      IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
      bool selected = false;
      List<IMediaProvider> mediaProviders = new List<IMediaProvider>();
      foreach (IBaseMediaProvider mediaProvider in mediaAccessor.LocalBaseMediaProviders)
        mediaProviders.Add(mediaProvider);
      mediaProviders.Sort((a, b) => a.Metadata.Name.CompareTo(b.Metadata.Name));
      foreach (IMediaProvider mediaProvider in mediaProviders)
      {
        MediaProviderMetadata metadata = mediaProvider.Metadata;
        ListItem mediaProviderItem = new ListItem(NAME_KEY, metadata.Name);
        mediaProviderItem.SetLabel(ID_KEY, metadata.MediaProviderId.ToString());
        if ((MediaProvider != null && MediaProvider.Metadata.MediaProviderId == metadata.MediaProviderId) ||
            mediaProviders.Count == 1)
        {
          mediaProviderItem.Selected = true;
          selected = true;
        }
        mediaProviderItem.SelectedProperty.Attach(OnMediaProviderItemSelectionChanged);
        _allBaseMediaProvidersList.Add(mediaProviderItem);
      }
      IsMediaProviderSelected = selected;
    }

    protected void RefreshMediaProviderPathList(ItemsList items, IFileSystemResourceAccessor accessor)
    {
      if (accessor == null)
        // Error case - would happen if media provider returned a resource accessor which is no IFileSystemResourceAccessor
        return;
      items.Clear();
      ICollection<IFileSystemResourceAccessor> res = FileSystemResourceNavigator.GetChildDirectories(accessor);
      if (res != null)
      {
        List<IFileSystemResourceAccessor> directories = new List<IFileSystemResourceAccessor>(res);
        CultureInfo culture = ServiceScope.Get<ILocalization>().CurrentCulture;
        directories.Sort((a, b) => string.Compare(a.ResourceName, b.ResourceName, true, culture));
        foreach (IFileSystemResourceAccessor childAccessor in directories)
        {
          TreeItem directoryItem = new TreeItem(NAME_KEY, childAccessor.ResourceName);
          directoryItem.AdditionalProperties[RESOURCE_ACCESSOR_KEY] = childAccessor;
          directoryItem.SetLabel(PATH_KEY, childAccessor.ResourcePathName);
          if (ChoosenResourcePath == childAccessor.LocalResourcePath)
            directoryItem.Selected = true;
          directoryItem.SelectedProperty.Attach(OnTreePathSelectionChanged);
          items.Add(directoryItem);
        }
      }
      items.FireChange();
    }

    protected void UpdateMediaProviderPathTree()
    {
      IBaseMediaProvider mp = MediaProvider;
      if (mp == null)
      { // This happens when the WF-Manager navigates back to the overview screen - all properties have been cleared before
        _mediaProviderPathsTree.Clear();
        _mediaProviderPathsTree.FireChange();
        return;
      }
      IResourceAccessor rootAccessor = mp.CreateMediaItemAccessor("/");
      RefreshMediaProviderPathList(_mediaProviderPathsTree, rootAccessor as IFileSystemResourceAccessor);
    }

    protected static ICollection<string> GetAllAvailableCategories()
    {
      IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
      ICollection<string> result = new HashSet<string>();
      foreach (IMetadataExtractor me in mediaAccessor.LocalMetadataExtractors.Values)
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

    protected void ClearAllConfiguredProperties()
    {
      MediaProvider = null;
      ChoosenResourcePath = null;
      ShareName = string.Empty;
      MediaCategories.Clear();
    }

    protected bool InitializePropertiesWithShare(Guid shareId)
    {
      ILocalSharesManagement sharesManagement = ServiceScope.Get<ILocalSharesManagement>();
      Share share = sharesManagement.GetShare(shareId);
      if (share == null)
        return false;
      MediaProvider = GetFirstMediaProvider(share);
      ChoosenResourcePath = share.BaseResourcePath;
      ShareName = share.Name;
      MediaCategories.Clear();
      CollectionUtils.AddAll(MediaCategories, share.MediaCategories);
      return true;
    }

    protected string SuggestShareName()
    {
      try
      {
        ResourcePath rp = ChoosenResourcePath;
        return rp.CreateLocalMediaItemAccessor().ResourceName;
      }
      catch (Exception)
      {
        return string.Empty;
      }
    }

    protected void UpdateShareAndFinish(RelocationMode relocationMode)
    {
      ILocalSharesManagement sharesManagement = ServiceScope.Get<ILocalSharesManagement>();
      sharesManagement.UpdateShare(CurrentShareId, ChoosenResourcePath, ShareName, MediaCategories, relocationMode);
      ClearAllConfiguredProperties();
      NavigateBackToOverview();
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
      else if (workflowState == SHARE_EDIT_STATE_ID)
      {
        _editMode = ShareEditMode.EditShare;
        UpdateSharesList();
      }
      else if (workflowState == SHARE_EDIT_CHOOSE_MEDIA_PROVIDER_STATE_ID ||
          workflowState == SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID)
      {
        if (workflowState == SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID)
        {
          // This is a bit weird here. The state SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE has
          // the same function like the SHARE_EDIT_CHOOSE_MEDIA_PROVIDER_STATE, it is only
          // necessary to do some additional internal initialization because in the "add" case,
          // the state SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID is the first one after the overview
          _editMode = ShareEditMode.AddShare;
          ClearAllConfiguredProperties();
        }
        // In the "edit" case, the state SHARE_EDIT_STATE_ID is active before SHARE_EDIT_CHOOSE_MEDIA_PROVIDER_STATE_ID,
        // so we do the initialization there
        
        // This could be optimized - we should not update the MPs list every time we are popping a WF state
        UpdateMediaProvidersList();
      }
      else if (workflowState == SHARE_EDIT_EDIT_PATH_STATE_ID)
      {
        // Nothing to prepare
      }
      else if (workflowState == SHARE_EDIT_CHOOSE_PATH_STATE_ID)
      {
        UpdateMediaProviderPathTree();
      }
      else if (workflowState == SHARE_EDIT_EDIT_NAME_STATE_ID)
      {
        if (_editMode == ShareEditMode.AddShare && string.IsNullOrEmpty(ShareName))
          ShareName = SuggestShareName();
      }
      else if (workflowState == SHARE_EDIT_CHOOSE_CATEGORIES_STATE_ID)
      {
        UpdateMediaCategoriesList();
      }
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(SHARESCONFIG_MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      PrepareState(newContext.WorkflowState.StateId);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
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

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Not used yet, currently we don't show any menu during the shares configuration process.
      // Perhaps we'll add menu actions later for different convenience procedures like initializing the
      // shares to their default setting, ...
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
