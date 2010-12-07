#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Drawing;
using System.Threading;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.UI.SkinEngine.ContentManagement.AssetCore;
using MediaPortal.UI.SkinEngine.Fonts;
using MediaPortal.UI.SkinEngine.SkinManagement;
using FontFamily = MediaPortal.UI.SkinEngine.Fonts.FontFamily;

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  public sealed class ContentManager
  {
    #region Internal structure

    /// <summary>
    /// This struct stores the pair of objects that together make-up a managed asset.
    /// The separation of the two objects allows us to use a <see cref="WeakReference"/>
    /// to determine when clients are still holding references to the asset.
    /// </summary>
    private class AssetInstance
    {
      /// <summary>
      /// This core object holds the actual (unmanaged) resource handle, and is only 
      /// used internally in the ContentManager
      /// </summary>
      public IAssetCore core;
      /// <summary>
      /// The asset member is a <see cref="WeakReference"/> to the asset wrapper that
      /// is used by client classes.
      /// </summary>
      public WeakReference asset;
    }

    #endregion

    #region Consts

    private enum AssetType
    {
      Texture = 0,
      Thumbnail = 1,
      Effect = 2,
      Font = 3,
      RenderTexture = 4
    };
    // Values for less agressive resource management.
    private const int LOW_CLEANUP_THRESHOLD = 70 * 1024 * 1024; // 70 MB
    private const int LOW_DEALLOCATION_LIMIT = 10;
    private const int LOW_SCAN_LIMIT = 40;
    // Values for aggressive resource management.
    private const int HIGH_CLEANUP_THRESHOLD = 100 * 1024 * 1024; // 100 MB
    private const int HIGH_DEALLOCATION_LIMIT = 30;
    private const int HIGH_SCAN_LIMIT = 100;
    // Intervals between cleanups. Which one is used depends on whether the deallocation limit was reached
    // in the last sweep.
    private const double SHORT_CLEANUP_INTERVAL = 1.0;
    private const double LONG_CLEANUP_INTERVAL = 10.0;
    // Maximum time between dictionary garbage collections (in milliseconds)
    private const int DICTIONARY_CLEANUP_INTERVAL = 60 * 1000;
    #endregion

    #region private variables

    private static readonly ContentManager _instance = new ContentManager();
    private readonly Dictionary<string, AssetInstance>[] _assets = null;
    private DateTime _timerA = SkinContext.FrameRenderingStartTime;
    private DateTime _timerB = SkinContext.FrameRenderingStartTime;
    private int _totalAllocation = 0;

    private int _lastCleanedAssetType = 0;
    private double _nextCleanupInterval = LONG_CLEANUP_INTERVAL;
    private readonly Thread _garbageCollectorThread;

    #endregion

    #region Ctor

    private ContentManager()
    {
      int asset_count = Enum.GetNames(typeof(AssetType)).Length;
      _assets = new Dictionary<string, AssetInstance>[asset_count];
      for (int i = 0; i < asset_count; ++i)
        _assets[i] = new Dictionary<string, AssetInstance>();

      _garbageCollectorThread = new Thread(DoGarbageCollection)
      {
        Name = typeof(ContentManager).Name + " garbage collector thread",
        Priority = ThreadPriority.Lowest,
        IsBackground = true
      };
      _garbageCollectorThread.Start();
    }

    #endregion

    #region Public asset methods

    /// <summary>
    /// Retrieves a <see cref="TextureAsset"/> (creating it if necessary) from the specified file.
    /// </summary>
    /// <param name="fileName">Name of the file (.jpg, .png).</param>
    /// <param name="thumb"><c>true</c> to load the image as a thumbnail, false otherwise.</param>
    /// <returns>A <see cref="TextureAsset"/> object.</returns>
    public TextureAsset GetTexture(string fileName, bool thumb)
    {
      return GetCreateTexture(fileName, fileName, 0, 0, thumb);
    }

    public TextureAsset GetTexture(string fileName)
    {
      return GetTexture(fileName, false);
    }

    /// <summary>
    /// Retrieves a <see cref="TextureAsset"/> (creating it if necessary) from the specified file
    /// at a specified size.
    /// </summary>
    /// <param name="fileName">Name of the file (.jpg, .png).</param>
    /// <param name="width">Restricts the size to the given width.</param>
    /// <param name="height">Restricts the size to the given height.</param>
    /// <param name="thumb"><c>true</c> to load the image as a thumbnail, false otherwise.</param>
    /// <returns>A texture asset with the given parameters.</returns>
    public TextureAsset GetTexture(string fileName, int width, int height, bool thumb)
    {
      if (width == 0 && height == 0)
        return GetTexture(fileName, thumb);

      string key = String.Format("{0}:[{1},{2}]", fileName, width, height);
      return GetCreateTexture(fileName, key, width, height, thumb);
    }

    /// <summary>
    /// Retrieves a <see cref="TextureAsset"/> (creating it if necessary) fiklled with the given color.
    /// </summary>
    /// <param name="color">The color to fill the texture with.</param>
    /// <returns>A texture asset filled with the given color.</returns>
    public TextureAsset GetColorTexture(Color color)
    {
      AssetInstance texture;
      TextureAsset asset = null;
      string key = ':' + color.ToString(); // The ':' is to make absolutely sure that the key isn't a valid filename.
      lock (_assets[(int) AssetType.Texture])
      {
        if (!_assets[(int) AssetType.Texture].TryGetValue(key, out texture))
        {
          texture = NewAssetInstance(key, AssetType.Texture, new ColorTextureAssetCore(color));
        }
        else
          asset = texture.asset.Target as TextureAsset;
      }
      // If the asset wrapper has been garbage collected then re-allocate it
      if (asset == null)
      {
        asset = new TextureAsset(texture.core as TextureAssetCore);
        if (texture.asset == null)
          texture.asset = new WeakReference(asset);
      }
      return asset;
    }

    /// <summary>
    /// Retrieves an <see cref="EffectAsset"/> (creating it if necessary) from the specified file.
    /// </summary>
    /// <param name="effectName">Name of the effect file (.fx).</param>
    /// <returns>An <see cref="EffectAsset"/> object.</returns>
    public EffectAsset GetEffect(string effectName)
    {
      AssetInstance effect;
      EffectAsset asset = null;
      lock (_assets[(int) AssetType.Effect])
      {
        if (!_assets[(int) AssetType.Effect].TryGetValue(effectName, out effect))
          effect = NewAssetInstance(effectName, AssetType.Effect, new EffectAssetCore(effectName));
        else
          asset = effect.asset.Target as EffectAsset;
      }
      // If the asset wrapper has been garbage collected then re-allocate it
      if (asset == null)
      {
        asset = new EffectAsset(effect.core as EffectAssetCore);
        if (effect.asset == null)
          effect.asset = new WeakReference(asset);
      }
      return asset;
    }

    /// <summary>
    /// Retrieves a <see cref="FontAsset"/>, creating it if necessary.
    /// </summary>
    /// <param name="fontFamily">The font family to use for the text.</param>
    /// <param name="fontSize">The size of the desired font.</param>
    /// <returns>A <see cref="FontAsset"/> object.</returns>
    public FontAsset GetFont(string fontFamily, float fontSize)
    {
      AssetInstance font;
      FontAsset asset = null;

      // Get the actual font file resource for this family
      FontFamily family = FontManager.GetFontFamily(fontFamily);
      if (family == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("SkinEngine.ContentManager: Could not get FontFamily '{0}', using default", fontFamily);
        family = FontManager.GetFontFamily(FontManager.DefaultFontFamily);
        if (family == null)
          return null;
      }

      // Round down font size
      int baseSize = (int) Math.Ceiling(fontSize * SkinContext.MaxZoomHeight);
      // Generate the asset key we'll use to store this font
      string key = family.Name + "::" + baseSize;

      // Get / Create AssetInstance
      lock (_assets[(int) AssetType.Font])
      {
        if (!_assets[(int) AssetType.Font].TryGetValue(key, out font))
          font = NewAssetInstance(key, AssetType.Font, new FontAssetCore(family, baseSize, FontManager.DefaultDPI));
        else
          asset = font.asset.Target as FontAsset;
      }
      // If the asset wrapper has been garbage collected then re-allocate it
      if (asset == null)
      {
        asset = new FontAsset(font.core as FontAssetCore);
        if (font.asset == null)
          font.asset = new WeakReference(asset);
      }
      return asset;
    }

    /// <summary>
    /// Retrieves a <see cref="RenderTextureAsset"/>, creating it if necessary.
    /// </summary>
    /// <param name="key">The name/key to use for storing the asset.</param>
    /// <returns>A <see cref="RenderTextureAsset"/> object.</returns>
    public RenderTextureAsset GetRenderTexture(string key)
    {
      AssetInstance texture;
      RenderTextureAsset asset = null;
      lock (_assets[(int) AssetType.RenderTexture])
      {
        if (!_assets[(int) AssetType.RenderTexture].TryGetValue(key, out texture))
          texture = NewAssetInstance(key, AssetType.RenderTexture, new RenderTextureAssetCore());
        else
          asset = texture.asset.Target as RenderTextureAsset;
      }
      // If the asset wrapper has been garbage collected then re-allocate it
      if (asset == null) 
      {
        asset = new RenderTextureAsset(texture.core as RenderTextureAssetCore);
        if (texture.asset == null)
          texture.asset = new WeakReference(asset);
      }
      return asset;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Gets the usable instance for this singleton class.
    /// </summary>
    public static ContentManager Instance
    {
      get { return _instance; }
    }

    /// <summary>
    /// Gets a value that is an estimation of the total VRAM usage of assets managed by this class.
    /// </summary>
    public int TotalAllocationSize
    {
      get { return _totalAllocation; }
    }

    /// <summary>
    /// Frees any assets that haven't been recently used. This must be done syncronously because pre-DirectX 11
    /// all calls must be made from the main rendering thread.
    /// </summary>
    public void Clean()
    {
      // If we are over our chosen allocation limit we will start freeing resources at regular intervals
      if (_totalAllocation > LOW_CLEANUP_THRESHOLD &&
        (SkinContext.FrameRenderingStartTime - _timerA).TotalSeconds > _nextCleanupInterval)
      {
        // Choose limits based on allocation threshhold
        int dealloc_limit = LOW_DEALLOCATION_LIMIT;
        int scan_limit = LOW_SCAN_LIMIT; 
        if (_totalAllocation > HIGH_CLEANUP_THRESHOLD)
        {
          dealloc_limit = HIGH_DEALLOCATION_LIMIT;
          scan_limit = HIGH_SCAN_LIMIT; 
        }
        // Loop though assets types until scan limit or deallocation limit is reached
        // This algorthim could use improvement
        int dealloc_remaining = dealloc_limit;
        int type = _lastCleanedAssetType;
        while (type < _assets.Length) 
        {
          lock (_assets[type])
          {
            dealloc_remaining -= Free(_assets[type], true, dealloc_remaining);
            scan_limit -= _assets[type].Count;
          }

          ++type;
          if (type >= _assets.Length)
            type = 0;
          if (dealloc_remaining <= 0 || scan_limit <= 0 || type == _lastCleanedAssetType)
            break;
        }
        _lastCleanedAssetType = type;
        _nextCleanupInterval = (dealloc_remaining > 0) ? LONG_CLEANUP_INTERVAL : SHORT_CLEANUP_INTERVAL;

        _timerA = SkinContext.FrameRenderingStartTime;

        ServiceRegistration.Get<ILogger>().Debug("ContentManager: {0} resources deallocated, next cleanup in {1} seconds. {2}/{3} MB", dealloc_limit - dealloc_remaining, _nextCleanupInterval, _totalAllocation / (1024.0 * 1024.0), HIGH_CLEANUP_THRESHOLD / (1024.0 * 1024.0));
      }

      if ((SkinContext.FrameRenderingStartTime - _timerB).TotalSeconds > 60.0) 
      {
        _timerB = SkinContext.FrameRenderingStartTime;
        ServiceRegistration.Get<ILogger>().Debug("ContentManager: Allocation {0}/{1} MB", _totalAllocation / (1024.0 * 1024.0), HIGH_CLEANUP_THRESHOLD / (1024.0 * 1024.0));
      }
    }

    /// <summary>
    /// Free all resources but retain containers for re-allocation.
    /// </summary>
    public void Free()
    {
      ServiceRegistration.Get<ILogger>().Debug("ContentManager: Freeing all assets", _totalAllocation / (1024.0 * 1024.0), HIGH_CLEANUP_THRESHOLD / (1024.0 * 1024.0));
      Free(false);
      ServiceRegistration.Get<ILogger>().Debug("ContentManager: All assets freed", _totalAllocation / (1024.0 * 1024.0), HIGH_CLEANUP_THRESHOLD / (1024.0 * 1024.0));
    }

    /// <summary>
    /// Free all recources and remove all <see cref="IAsset"/>s.
    /// </summary>
    public void Clear()
    {
      Free();
      foreach (Dictionary<string, AssetInstance> type in _assets)
        lock (type)
          type.Clear();
    }

    #endregion

    #region private asset management methods

    private TextureAsset GetCreateTexture(string fileName, string key, int width, int height, bool thumb)
    {
      int type = (int)(thumb ? AssetType.Thumbnail : AssetType.Texture);
      AssetInstance texture;
      TextureAsset asset = null;

      lock (_assets[type])
      {
        if (!_assets[type].TryGetValue(fileName, out texture))
        {
          texture = NewAssetInstance(key, thumb ? AssetType.Thumbnail : AssetType.Texture,
              new TextureAssetCore(fileName, width, height));
          ((TextureAssetCore)texture.core).UseThumbnail = thumb;
        }
        else
          asset = texture.asset.Target as TextureAsset;
      }
      // If the asset wrapper has been garbage collected then re-allocate it
      if (asset == null)
      {
        asset = new TextureAsset(texture.core as TextureAssetCore);
        if (texture.asset == null)
          texture.asset = new WeakReference(asset);
      }
      return asset;
    }

    /// <summary>
    /// This function is run asyncronously to remove de-referenced and de-allocated assets from the list.
    /// </summary>
    private void DoGarbageCollection()
    {
      while (true)
      {
        if (_totalAllocation > LOW_CLEANUP_THRESHOLD)
        {
          // Clear out expired but unallocated assets
          foreach (Dictionary<string, AssetInstance> type in _assets)
            RemoveUnallocated(type);
        }
        // Wait for next run
        Thread.Sleep(DICTIONARY_CLEANUP_INTERVAL);
      }
    }

    private AssetInstance NewAssetInstance(string key, AssetType type, IAssetCore newcore)
    {
      AssetInstance inst = new AssetInstance { core = newcore };
      // Albert, 2010-11-16: The following line produces too many messages in log
      //ServiceRegistration.Get<ILogger>().Debug("ContentManager: Creating new {0} for '{1}'", type.ToString(), key);
      newcore.AllocationChanged += OnAssetAllocationChanged;
      _assets[(int) type].Add(key, inst);
      return inst;
    }

    /// <summary>
    /// Frees all resources.
    /// </summary>
    private void Free(bool checkIfCanBeDeleted)
    {
      foreach (Dictionary<string, AssetInstance> type in _assets)
        lock(type)
          Free(type, checkIfCanBeDeleted, int.MaxValue);
    }

    /// <summary>
    /// Frees resources of a particular type.
    /// </summary>
    /// <param name="assets">The asset <see cref="Dictionary{TKey,TValue}"/> to search.</param>
    /// <param name="checkIfCanBeDeleted"><c>true</c> if resources should be asked if they are still needed,
    /// <c>false</c> to just deallocate everything.</param>
    /// <param name="limit">The maximum number of resources to de-allocate. Defaults value is <c>Int32.MaxValue</c>.</param>
    /// <returns>The number of resources de-allocated</returns>
    private static int Free(Dictionary<string, AssetInstance> assets, bool checkIfCanBeDeleted, int limit)
    {
      int count = 0;
      Dictionary<string, AssetInstance>.Enumerator enumer = assets.GetEnumerator();
      while (enumer.MoveNext())
        if (enumer.Current.Value.core.IsAllocated && 
          (!checkIfCanBeDeleted || enumer.Current.Value.core.CanBeDeleted))
        {
          enumer.Current.Value.core.Free();
          ++count;
          if (count == limit)
            return count;
        }
      return count;
    }

    /// <summary>
    /// Removes unallocated resources from the given asset <see cref="IDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <param name="assets">The asset <see cref="IDictionary{TKey,TValue}"/> to search.</param>
    private static void RemoveUnallocated(Dictionary<string, AssetInstance> assets)
    {
      lock (assets)
      {
        List<string> items = new List<string>();
        Dictionary<string, AssetInstance>.Enumerator enumer = assets.GetEnumerator();
        while (enumer.MoveNext())
          if (!enumer.Current.Value.asset.IsAlive && !enumer.Current.Value.core.IsAllocated)
            items.Add(enumer.Current.Key);
        foreach (string key in items)
          assets.Remove(key);
      }
    }

    private void OnAssetAllocationChanged(int amount)
    {
      _totalAllocation += amount;
    }

    #endregion
  }
}
