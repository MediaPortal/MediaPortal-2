#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// Base data class which has two orthogonal jobs:
  /// 1) Collecting all share data during the share add or edit workflow and at the same time,
  /// 2) handling the communication with the local or server shares management.
  /// </summary>
  public abstract class SharesProxy
  {
    #region Enums

    public enum ShareEditMode
    {
      AddShare,
      EditShare,
    }

    #endregion

    #region Protected fields

    protected ShareEditMode? _editMode;
    protected ItemsList _allBaseResourceProvidersList;
    protected AbstractProperty _isResourceProviderSelectedProperty;
    protected AbstractProperty _baseResourceProviderProperty;
    protected AbstractProperty _nativeSystemProperty;
    protected AbstractProperty _choosenResourcePathStrProperty;
    protected AbstractProperty _choosenResourcePathProperty;
    protected AbstractProperty _isChoosenPathValidProperty;
    protected AbstractProperty _choosenResourcePathDisplayNameProperty;
    protected ItemsList _resourceProviderPathsTree;
    protected AbstractProperty _shareNameProperty;
    protected AbstractProperty _isShareNameValidProperty;
    protected AbstractProperty _invalidShareHintProperty;
    protected ItemsList _allMediaCategoriesTree;
    protected ICollection<string> _mediaCategories = new HashSet<string>();
    protected Share _origShare = null;

    #endregion

    protected SharesProxy(ShareEditMode? editMode)
    {
      _editMode = editMode;
      _allBaseResourceProvidersList = new ItemsList();
      _isResourceProviderSelectedProperty = new WProperty(typeof(bool), false);
      _baseResourceProviderProperty = new WProperty(typeof(ResourceProviderMetadata), null);
      _nativeSystemProperty = new WProperty(typeof(string), string.Empty);
      _choosenResourcePathStrProperty = new WProperty(typeof(string), string.Empty);
      _choosenResourcePathStrProperty.Attach(OnChoosenResourcePathStrChanged);
      _choosenResourcePathProperty = new WProperty(typeof(ResourcePath), null);
      _choosenResourcePathProperty.Attach(OnChoosenResourcePathChanged);
      _isChoosenPathValidProperty = new WProperty(typeof(bool), false);
      _choosenResourcePathDisplayNameProperty = new WProperty(typeof(string), string.Empty);
      _resourceProviderPathsTree = new ItemsList();
      _shareNameProperty = new WProperty(typeof(string), string.Empty);
      _shareNameProperty.Attach(OnShareNameChanged);
      _isShareNameValidProperty = new WProperty(typeof(bool), true);
      _invalidShareHintProperty = new WProperty(typeof(string), null);
      _allMediaCategoriesTree = new ItemsList();
      _mediaCategories = new HashSet<string>();
    }

    #region Event handlers

    void OnResourceProviderItemSelectionChanged(AbstractProperty shareItem, object oldValue)
    {
      UpdateIsResourceProviderSelected();
    }

    void OnChoosenResourcePathStrChanged(AbstractProperty resourceProviderURL, object oldValue)
    {
      ChoosenResourcePath = ExpandResourcePathFromString(ChoosenResourcePathStr);
    }

    void OnChoosenResourcePathChanged(AbstractProperty resourceProviderURL, object oldValue)
    {
      // Don't update ChoosenResourcePathStr - the string is the master and can be written in several formats
      UpdateIsChoosenPathValid();
      UpdateChoosenPathDisplayName();
    }

    void OnTreePathSelectionChanged(AbstractProperty property, object oldValue)
    {
      UpdateChoosenResourcePath();
      UpdateIsChoosenPathValid();
    }

    void OnShareNameChanged(AbstractProperty shareName, object oldValue)
    {
      UpdateIsShareNameValid();
    }

    static void SelectMediaCategoryHierarchy(TreeItem item)
    {
      item.Selected = true;
      TreeItem parent = (TreeItem) item.AdditionalProperties[Consts.KEY_PARENT_ITEM];
      if (parent == null)
        return;
      SelectMediaCategoryHierarchy(parent);
    }

    static void DeselectMediaCategoryHierarchy(TreeItem item)
    {
      item.Selected = false;
      foreach (TreeItem childItem in item.SubItems)
        DeselectMediaCategoryHierarchy(childItem);
    }

    void OnMediaCategoryItemSelectionChanged(TreeItem changedItem)
    {
      if (changedItem.Selected)
        SelectMediaCategoryHierarchy(changedItem);
      else
        DeselectMediaCategoryHierarchy(changedItem);
      UpdateMediaCategories();
    }

    #endregion

    #region Public properties (can be used by the GUI)

    public Share OrigShare
    {
      get { return _origShare; }
    }

    public ShareEditMode? EditMode
    {
      get { return _editMode; }
      set { _editMode = value; }
    }

    /// <summary>
    /// Returns the appropriate title for the whole share add or edit workflow, for example
    /// <c>[SharesConfig.AddServerShare]</c>, which could evaluate to the string <c>Add server share</c>, depending
    /// on the configured language.
    /// </summary>
    public abstract string ConfigShareTitle { get; }

    /// <summary>
    /// Returns the information if the selected <see cref="BaseResourceProvider"/> supports a tree navigation through its
    /// structure, i.e. we can use the property <see cref="ResourceProviderPathsTree"/> and the method
    /// <see cref="UpdateResourceProviderPathTree"/>.
    /// </summary>
    public abstract bool ResourceProviderSupportsResourceTreeNavigation { get; }

    /// <summary>
    /// List of all available base resource providers.
    /// </summary>
    public ItemsList AllBaseResourceProviders
    {
      get { return _allBaseResourceProvidersList; }
    }

    public AbstractProperty IsResourceProviderSelectedProperty
    {
      get { return _isResourceProviderSelectedProperty; }
    }

    /// <summary>
    /// <c>true</c> if at least one resource provider is selected.
    /// </summary>
    public bool IsResourceProviderSelected
    {
      get { return (bool) _isResourceProviderSelectedProperty.GetValue(); }
      set { _isResourceProviderSelectedProperty.SetValue(value); }
    }

    public AbstractProperty BaseResourceProviderProperty
    {
      get { return _baseResourceProviderProperty; }
    }

    /// <summary>
    /// Metadata structure of the selected base resource provider.
    /// </summary>
    public ResourceProviderMetadata BaseResourceProvider
    {
      get { return (ResourceProviderMetadata) _baseResourceProviderProperty.GetValue(); }
      set
      {
        ResourceProviderMetadata oldVAlue = (ResourceProviderMetadata) _baseResourceProviderProperty.GetValue();
        _baseResourceProviderProperty.SetValue(value);
        if (value == null || oldVAlue == null || value.ResourceProviderId != oldVAlue.ResourceProviderId)
          ChoosenResourcePath = null;
      }
    }

    public AbstractProperty NativeSystemProperty
    {
      get { return _nativeSystemProperty; }
    }

    /// <summary>
    /// System where the share is located.
    /// </summary>
    public string NativeSystem
    {
      get { return (string) _nativeSystemProperty.GetValue(); }
      set { _nativeSystemProperty.SetValue(value); }
    }

    public AbstractProperty ChoosenResourcePathStrProperty
    {
      get { return _choosenResourcePathStrProperty; }
    }

    /// <summary>
    /// Selected resource provider path.
    /// </summary>
    /// <remarks>
    /// This property will be bound to an input field by the skin. The user can write the desired resource path itself,
    /// if the choosen resource provider is no filesystem resource provider and thus we cannot provide a directory tree.
    /// </remarks>
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
    /// Selected resource provider path as resource path instance.
    /// </summary>
    /// <remarks>
    /// This property will automatically be updated - either from the <see cref="ChoosenResourcePathStr"/>, which was
    /// edited by the user himself, or from the directory tree selection change handler.
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

    public bool IsResourcePathChanged
    {
      get { return ChoosenResourcePath != _origShare.BaseResourcePath; }
    }

    public bool IsCategoriesChanged
    {
      get { return !CollectionUtils.CompareObjectCollections(_mediaCategories, _origShare.MediaCategories); }
    }

    /// <summary>
    /// Paths tree of the selected resource provider, if the resource provider supports path
    /// navigation.
    /// </summary>
    public ItemsList ResourceProviderPathsTree
    {
      get { return _resourceProviderPathsTree; }
    }

    public AbstractProperty ShareNameProperty
    {
      get { return _shareNameProperty; }
    }

    /// <summary>
    /// Edited name for the current share.
    /// </summary>
    public string ShareName
    {
      get { return (string) _shareNameProperty.GetValue(); }
      set { _shareNameProperty.SetValue(value); }
    }

    public AbstractProperty IsShareNameValidProperty
    {
      get { return _isShareNameValidProperty; }
    }

    /// <summary>
    /// <c>true</c> if the edited share name is a valid string for a share name.
    /// </summary>
    public bool IsShareNameValid
    {
      get { return (bool) _isShareNameValidProperty.GetValue(); }
      set { _isShareNameValidProperty.SetValue(value); }
    }

    public AbstractProperty InvalidShareHintProperty
    {
      get { return _invalidShareHintProperty; }
    }

    /// <summary>
    /// Hint which tells the user why the choosen resource path or share name is not valid.
    /// </summary>
    public string InvalidShareHint
    {
      get { return (string) _invalidShareHintProperty.GetValue(); }
      set { _invalidShareHintProperty.SetValue(value); }
    }

    /// <summary>
    /// Tree of all media categories.
    /// </summary>
    public ItemsList AllMediaCategories
    {
      get { return _allMediaCategoriesTree; }
    }

    /// <summary>
    /// Collection of choosen media categories for the current share.
    /// </summary>
    public ICollection<string> MediaCategories
    {
      get { return _mediaCategories; }
    }

    #endregion

    #region Public methods

    public void ClearAllConfiguredProperties()
    {
      BaseResourceProvider = null;
      ChoosenResourcePath = null;
      ShareName = string.Empty;
      MediaCategories.Clear();
      NativeSystem = null;
    }

    protected bool InitializePropertiesWithShare(Share share, string nativeSystem)
    {
      _origShare = share;
      BaseResourceProvider = GetBaseResourceProviderMetadata(share.BaseResourcePath);
      NativeSystem = nativeSystem;
      ChoosenResourcePath = share.BaseResourcePath;
      ShareName = share.Name;
      MediaCategories.Clear();
      CollectionUtils.AddAll(MediaCategories, share.MediaCategories);
      return true;
    }

    protected abstract IEnumerable<ResourceProviderMetadata> GetAvailableBaseResourceProviders();

    public static Guid? GetBaseResourceProviderId(ResourcePath path)
    {
      if (!path.IsAbsolute)
        return null;
      ProviderPathSegment firstProvider = path.FirstOrDefault();
      if (firstProvider == null)
        return null;
      return firstProvider.ProviderId;
    }

    protected ResourceProviderMetadata GetBaseResourceProviderMetadata(ResourcePath path)
    {
      Guid? resourceProviderId = GetBaseResourceProviderId(path);
      return resourceProviderId.HasValue ? GetResourceProviderMetadata(resourceProviderId.Value) : null;
    }

    protected abstract ResourceProviderMetadata GetResourceProviderMetadata(Guid resourceProviderId);

    public void UpdateResourceProvidersList()
    {
      _allBaseResourceProvidersList.Clear();
      bool selected = false;
      List<ResourceProviderMetadata> resourceProviderMDs = new List<ResourceProviderMetadata>(
          GetAvailableBaseResourceProviders().Where(metadata => !metadata.TransientMedia));
      resourceProviderMDs.Sort((a, b) => a.Name.CompareTo(b.Name));
      ResourceProviderMetadata choosenBaseResourceProvider = BaseResourceProvider;
      foreach (ResourceProviderMetadata metadata in resourceProviderMDs)
      {
        ListItem resourceProviderItem = new ListItem(Consts.KEY_NAME, metadata.Name);
        resourceProviderItem.AdditionalProperties[Consts.KEY_RESOURCE_PROVIDER_METADATA] = metadata;
        if ((choosenBaseResourceProvider != null && choosenBaseResourceProvider.ResourceProviderId == metadata.ResourceProviderId) ||
            resourceProviderMDs.Count == 1)
        {
          resourceProviderItem.Selected = true;
          selected = true;
        }
        resourceProviderItem.SelectedProperty.Attach(OnResourceProviderItemSelectionChanged);
        _allBaseResourceProvidersList.Add(resourceProviderItem);
      }
      IsResourceProviderSelected = selected;
    }

    public ResourceProviderMetadata GetSelectedBaseResourceProvider()
    {
      return _allBaseResourceProvidersList.Where(resourceProviderItem => resourceProviderItem.Selected).Select(
          resourceProviderItem => resourceProviderItem.AdditionalProperties[
              Consts.KEY_RESOURCE_PROVIDER_METADATA] as ResourceProviderMetadata).FirstOrDefault();
    }

    public void UpdateIsResourceProviderSelected()
    {
      IsResourceProviderSelected = _allBaseResourceProvidersList.Any(resourceProviderItem => resourceProviderItem.Selected);
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
      ChoosenResourcePath = FindChoosenResourcePath(_resourceProviderPathsTree);
    }

    protected abstract ResourcePath ExpandResourcePathFromString(string path);

    protected abstract bool SharePathExists(ResourcePath sharePath);

    protected internal void UpdateIsChoosenPathValid()
    {
      ResourcePath choosenResourcePath = ChoosenResourcePath;
      ResourcePath path = choosenResourcePath;
      bool result = path != null && GetIsPathValid(choosenResourcePath);
      if (SharePathExists(path))
      {
        InvalidShareHint = Consts.RES_SHARE_PATH_EXISTS;
        result = false;
      }
      else
        InvalidShareHint = null;
      IsChoosenPathValid = result;
    }

    protected abstract bool GetIsPathValid(ResourcePath path);

    protected void UpdateChoosenPathDisplayName()
    {
      ResourcePath path = ChoosenResourcePath;
      ChoosenResourcePathDisplayName = path == null ? string.Empty : GetResourcePathDisplayName(path);
    }

    public abstract string GetResourcePathDisplayName(ResourcePath path);

    protected abstract bool ShareNameExists(string shareName);

    protected void UpdateIsShareNameValid()
    {
      string shareName = ShareName;
      bool result = true;
      string hint = null;
      if (string.IsNullOrEmpty(shareName))
      {
        hint = Consts.RES_SHARE_NAME_EMPTY;
        result = false;
      }
      else if (ShareNameExists(shareName))
      {
        hint = Consts.RES_SHARE_NAME_EXISTS;
        result = false;
      }
      IsShareNameValid = result;
      InvalidShareHint = hint;
    }

    protected ICollection<string> GetSelectedMediaCategories(ItemsList list)
    {
      ICollection<string> result = new List<string>();
      foreach (TreeItem categoryItem in list)
      {
        if (categoryItem.Selected)
          result.Add(categoryItem[Consts.KEY_NAME]);
        CollectionUtils.AddAll(result, GetSelectedMediaCategories(categoryItem.SubItems));
      }
      return result;
    }

    protected void UpdateMediaCategories()
    {
      _mediaCategories.Clear();
      CollectionUtils.AddAll(_mediaCategories, GetSelectedMediaCategories(_allMediaCategoriesTree));
    }

    /// <summary>
    /// We need a class to provide a property <see cref="IsExpanded"/> together with its <see cref="IsExpandedProperty"/>
    /// in order to make the SkinEngine bind to our expansion flag.
    /// </summary>
    protected class ExpansionHelper
    {
      protected AbstractProperty _isExpandedProperty = new WProperty(typeof(bool), false);
      protected SharesProxy _parent;
      protected TreeItem _directoryItem;

      public ExpansionHelper(TreeItem directoryItem, SharesProxy parent)
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
      {
        List<ResourcePathMetadata> directories = new List<ResourcePathMetadata>(res);
        directories.Sort((a, b) => a.ResourceName.CompareTo(b.ResourceName));
        foreach (ResourcePathMetadata childDirectory in directories)
        {
          TreeItem directoryItem = new TreeItem(Consts.KEY_NAME, childDirectory.ResourceName);
          directoryItem.AdditionalProperties[Consts.KEY_RESOURCE_PATH] = childDirectory.ResourcePath;
          directoryItem.SetLabel(Consts.KEY_PATH, childDirectory.HumanReadablePath);
          if (ChoosenResourcePath == childDirectory.ResourcePath)
            directoryItem.Selected = true;
          directoryItem.SelectedProperty.Attach(OnTreePathSelectionChanged);
          directoryItem.AdditionalProperties[Consts.KEY_EXPANSION] = new ExpansionHelper(directoryItem, this);
          items.Add(directoryItem);
        }
      }
      items.FireChange();
    }

    protected abstract IEnumerable<ResourcePathMetadata> GetChildDirectoriesData(ResourcePath path);

    /// <summary>
    /// Updates the data for the resource provider path tree.
    /// </summary>
    public void UpdateResourceProviderPathTree()
    {
      ResourceProviderMetadata rpm = BaseResourceProvider;
      if (rpm == null)
      { // This happens when the WF-Manager navigates back to the overview screen - all properties have been cleared before
        _resourceProviderPathsTree.Clear();
        _resourceProviderPathsTree.FireChange();
        return;
      }
      RefreshResourceProviderPathList(_resourceProviderPathsTree, ResourcePath.BuildBaseProviderPath(rpm.ResourceProviderId, "/"));
      UpdateIsChoosenPathValid();
    }

    protected abstract IDictionary<string, MediaCategory> GetAllAvailableCategories();

    /// <summary>
    /// Builds the data for the media categories tree.
    /// </summary>
    public void UpdateMediaCategoriesTree()
    {
      _allMediaCategoriesTree.Clear();

      IDictionary<string, MediaCategory> allCategories = GetAllAvailableCategories();
      ICollection<string> rootCategories;
      ICollection<string> selectedMediaCategoriesIncludingParents = new List<string>();
      foreach (string mediaCategory in _mediaCategories)
      {
        MediaCategory category;
        if (!allCategories.TryGetValue(mediaCategory, out category))
          continue;
        CollectionUtils.AddAll(selectedMediaCategoriesIncludingParents, GetMediaCategoryIncludingParents(category));
      }
      IDictionary<string, ICollection<string>> categories2Children = BuildCategoriesChildrenMapping(allCategories.Values, out rootCategories);
      BuildCategoriesTree(_allMediaCategoriesTree, null, rootCategories, selectedMediaCategoriesIncludingParents, categories2Children, allCategories);
    }

    /// <summary>
    /// Returns all media category names of the given <paramref name="mediaCategory"/> and all its direct and indirect parent categories.
    /// </summary>
    /// <param name="mediaCategory">Media category to start building the hierarchy.</param>
    /// <returns>Collection of media category names.</returns>
    protected ICollection<string> GetMediaCategoryIncludingParents(MediaCategory mediaCategory)
    {
      ICollection<string> result = new List<string> {mediaCategory.CategoryName};
      foreach (MediaCategory parentCategory in mediaCategory.ParentCategories)
        CollectionUtils.AddAll(result, GetMediaCategoryIncludingParents(parentCategory));
      return result;
    }

    /// <summary>
    /// Takes all media categories from property <see cref="MediaCategories"/> and retains for each path of media categories from all category leaves to the roots only the
    /// category items which are the most far away from the root. All other media categories which are located in a path from one of the resulting media categories
    /// up to one of the roots of the hierarchy are implicit.
    /// </summary>
    /// <returns>Collection of media category names.</returns>
    protected ICollection<string> GetMediaCategoriesCleanedUp()
    {
      IDictionary<string, MediaCategory> allCategories = GetAllAvailableCategories();
      ICollection<string> result = new List<string>(MediaCategories);
      ICollection<string> remove = new List<string>();
      foreach (string mediaCategory in result)
      {
        MediaCategory category;
        if (!allCategories.TryGetValue(mediaCategory, out category))
          continue;
        CollectionUtils.AddAll(remove, GetMediaCategoryIncludingParents(category));
        remove.Remove(mediaCategory);
      }
      CollectionUtils.RemoveAll(result, remove);
      return result;
    }

    protected void BuildCategoriesTree(ItemsList contents, TreeItem parentItem, ICollection<string> categories, ICollection<string> allSelectedCategories,
        IDictionary<string, ICollection<string>> categories2Children, IDictionary<string, MediaCategory> allCategories)
    {
      contents.Clear();
      List<string> categoriesSorted = new List<string>(categories);
      categoriesSorted.Sort();
      foreach (string mediaCategory in categoriesSorted)
      {
        TreeItem categoryItem = new TreeItem(Consts.KEY_NAME, mediaCategory);
        categoryItem.AdditionalProperties[Consts.KEY_MEDIA_CATEGORY] = allCategories[mediaCategory];
        categoryItem.AdditionalProperties[Consts.KEY_PARENT_ITEM] = parentItem;
        if (allSelectedCategories.Contains(mediaCategory))
          categoryItem.Selected = true;
        PropertyChangedHandler handler = (property, oldValue) => OnMediaCategoryItemSelectionChanged(categoryItem);
        categoryItem.SelectedProperty.Attach(handler);
        categoryItem.AdditionalProperties["HandlerStrongReference"] = handler; // SelectedProperty only manages weak references to its handlers -> avoid handler to be removed
        ICollection<string> childCategories;
        if (!categories2Children.TryGetValue(mediaCategory, out childCategories))
          childCategories = new List<string>();
        BuildCategoriesTree(categoryItem.SubItems, categoryItem, childCategories, allSelectedCategories, categories2Children, allCategories);
        contents.Add(categoryItem);
      }
    }

    protected IDictionary<string, ICollection<string>> BuildCategoriesChildrenMapping(ICollection<MediaCategory> allCategories, out ICollection<string> rootCategories)
    {
      rootCategories = new HashSet<string>();
      IDictionary<string, ICollection<string>> result = new Dictionary<string, ICollection<string>>();
      foreach (MediaCategory mediaCategory in allCategories)
      {
        if (mediaCategory.ParentCategories.Count == 0)
          rootCategories.Add(mediaCategory.CategoryName);
        else
          foreach (MediaCategory parentCategory in mediaCategory.ParentCategories)
          {
            ICollection<string> children;
            if (!result.TryGetValue(parentCategory.CategoryName, out children))
              result[parentCategory.CategoryName] = children = new HashSet<string>();
            children.Add(mediaCategory.CategoryName);
          }
      }
      return result;
    }

    protected abstract string SuggestShareName();

    public abstract void AddShare();

    public abstract void UpdateShare(RelocationMode relocationMode);

    public void PrepareShareName()
    {
      if (string.IsNullOrEmpty(ShareName))
        ShareName = SuggestShareName();
      UpdateIsShareNameValid();
    }

    public abstract void ReImportShare();

    #endregion
  }
}
