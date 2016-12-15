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
using System.IO;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.ContentManagement.AssetCore;
using MediaPortal.UI.SkinEngine.Fonts;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX;
using FontFamily = MediaPortal.UI.SkinEngine.Fonts.FontFamily;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  /// <summary>
  /// Main class of the DirectX content management subsystem. The content management system is responsible to maintain all
  /// DirectX resources which are currently in use, called "assets".
  /// </summary>
  /// <remarks>
  /// <para>
  /// For each asset type (like fonts, effects, render target surfaces and render textures), we provide a wrapper class. Those classes
  /// are called "asset core classes" and they are located in namespace <see cref="MediaPortal.UI.SkinEngine.ContentManagement.AssetCore"/>.
  /// </para>
  /// <para>
  /// Managed assets are re-used by many client objects in parallel. When an asset is not used any more, the content manager releases
  /// it after some time.
  /// </para>
  /// <para>
  /// To simplify the lifetime management of assets, we manage two instances for each asset: One "asset" and one "asset core", which are both
  /// together referenced by a <see cref="AssetInstance"/> object. Each asset instance object which is alive is referenced by the
  /// ContentManager. Furthermore, each asset references its asset core object. The asset object is only referenced by the asset instance
  /// using a <see cref="WeakReference"/>.
  /// </para>
  /// <para>
  /// Each client object (typically SkinEngine controls) holds references to each asset it needs. After the last client object drops
  /// its asset reference or is gc'ed, the asset is free to be catched by the garbage collector. 
  /// </para>
  /// <para>
  /// This class is multithreading-safe.
  /// </para>
  /// </remarks>
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
      /// This core object holds the actual (unmanaged) resource handle, and is only used internally in the ContentManager
      /// </summary>
      public IAssetCore core;

      /// <summary>
      /// The asset member is a <see cref="WeakReference"/> to the asset wrapper that is used by client classes.
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
      RenderTexture = 4,
      RenderTarget = 5,
    }

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

    #region Private variables

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
        Name = "ContMgrGC", //garbage collector thread
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
    /// <param name="thumb"><c>true</c> to load the image as a thumbnail, <c>false</c> otherwise.</param>
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
    /// Retrieves a <see cref="TextureAsset"/> (creating it if necessary) filled with image data from the given stream.
    /// </summary>
    /// <param name="stream">Stream to read the image data to create the texture from.</param>
    /// <param name="key">Key which is unique for the given image <paramref name="stream"/>.</param>
    /// <param name="useSyncLoading">If <c>true</c> the stream gets read synchronously.</param>
    /// <returns>A texture asset containing the image given by the <paramref name="stream"/>.</returns>
    public TextureAsset GetTexture(Stream stream, string key, bool useSyncLoading = false)
    {
      return GetCreateAsset(AssetType.Thumbnail, key,
          assetCore => new TextureAsset(assetCore as TextureAssetCore),
          () => useSyncLoading ?
            new SynchronousStreamTextureAssetCore(stream, key) as TextureAssetCore :
            new StreamTextureAssetCore(stream, key)
            ) as TextureAsset ;
    }

    /// <summary>
    /// Retrieves a <see cref="TextureAsset"/> (creating it if necessary) filled with the given binary image data.
    /// </summary>
    /// <param name="binaryData">The binary image data to create the texture from.</param>
    /// <param name="key">Key which is unique for the given image <paramref name="binaryData"/>.</param>
    /// <returns>A texture asset containing the image given by the <paramref name="binaryData"/>.</returns>
    public TextureAsset GetTexture(byte[] binaryData, string key)
    {
      return GetCreateAsset(AssetType.Thumbnail, key,
          assetCore => new TextureAsset(assetCore as TextureAssetCore),
          () => new BinaryTextureAssetCore(binaryData, key)) as TextureAsset;
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
    /// Retrieves a <see cref="TextureAsset"/> (creating it if necessary) filled with the given color.
    /// </summary>
    /// <param name="color">The color to fill the texture with.</param>
    /// <returns>A texture asset filled with the given color.</returns>
    public TextureAsset GetColorTexture(Color color)
    {
      string key = ':' + color.ToString(); // The ':' is to make absolutely sure that the key isn't a valid filename.
      return GetCreateAsset(AssetType.Texture, key,
          assetCore => new TextureAsset(assetCore as TextureAssetCore),
          () => new ColorTextureAssetCore(color)) as TextureAsset;
    }

    /// <summary>
    /// Retrieves an <see cref="EffectAsset"/> (creating it if necessary) from the specified file.
    /// </summary>
    /// <param name="effectName">Name of the effect file (.fx).</param>
    /// <returns>An <see cref="EffectAsset"/> object.</returns>
    public EffectAsset GetEffect(string effectName)
    {
      return GetCreateAsset(AssetType.Effect, effectName,
          assetCore => new EffectAsset(assetCore as EffectAssetCore),
          () => new EffectAssetCore(effectName)) as EffectAsset;
    }

    /// <summary>
    /// Retrieves a <see cref="FontAsset"/>, creating it if necessary.
    /// </summary>
    /// <param name="fontFamily">The font family to use for the text.</param>
    /// <param name="fontSize">The size of the desired font.</param>
    /// <returns>A <see cref="FontAsset"/> object.</returns>
    public FontAsset GetFont(string fontFamily, float fontSize)
    {
      // Get the actual font file resource for this family
      FontFamily family = FontManager.GetFontFamily(fontFamily);
      if (family == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("ContentManager: Could not get FontFamily '{0}', using default", fontFamily);
        family = FontManager.GetFontFamily(FontManager.DefaultFontFamily);
        if (family == null)
          return null;
      }

      // Round down font size
      int baseSize = (int) Math.Ceiling(fontSize * SkinContext.MaxZoomHeight);
      // If this function is called before the window is openned we get 0
      if (baseSize == 0)
        baseSize = (int) fontSize;
      // Generate the asset key we'll use to store this font
      string key = family.Name + "::" + baseSize;

      return GetCreateAsset(AssetType.Font, key,
          assetCore => new FontAsset(assetCore as FontAssetCore),
          () => new FontAssetCore(family, baseSize, FontManager.DefaultDPI)) as FontAsset;
    }

    /// <summary>
    /// Retrieves a <see cref="RenderTextureAsset"/>, creating it if necessary.
    /// </summary>
    /// <param name="key">The name/key to use for storing the asset.</param>
    /// <returns>A <see cref="RenderTextureAsset"/> object.</returns>
    public RenderTextureAsset GetRenderTexture(string key)
    {
      return GetCreateAsset(AssetType.RenderTexture, key,
          assetCore => new RenderTextureAsset(assetCore as RenderTextureAssetCore),
          () => new RenderTextureAssetCore()) as RenderTextureAsset;
    }

    /// <summary>
    /// Retrieves a <see cref="RenderTargetAsset"/>, creating it if necessary.
    /// </summary>
    /// <param name="key">The name/key to use for storing the asset.</param>
    /// <returns>A <see cref="RenderTargetAsset"/> object.</returns>
    public RenderTargetAsset GetRenderTarget(string key)
    {
      return GetCreateAsset(AssetType.RenderTarget, key,
          assetCore => new RenderTargetAsset(assetCore as RenderTargetAssetCore),
          () => new RenderTargetAssetCore()) as RenderTargetAsset;
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
    /// Frees any assets that haven't been recently used. This must be done synchronously because pre-DirectX 11
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

    #region Private asset management methods

    private TextureAsset GetCreateTexture(string fileName, string key, int width, int height, bool thumb)
    {
      AssetType type = thumb ? AssetType.Thumbnail : AssetType.Texture;

      return GetCreateAsset(type, key,
          assetCore => new TextureAsset(assetCore as TextureAssetCore),
          () => new TextureAssetCore(fileName, width, height) {UseThumbnail = thumb}) as TextureAsset;
    }


    /// <summary>
    /// This function is run asynchronously to remove de-referenced and de-allocated assets from the list.
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

    private delegate IAsset CreateAssetDlgt(IAssetCore core);
    private delegate IAssetCore CreateAssetCoreDlgt();

    private IAsset GetCreateAsset(AssetType type, string key, CreateAssetDlgt createAsset, CreateAssetCoreDlgt createAssetCore)
    {
      AssetInstance assetInstance;
      IAsset result = null;
      IDictionary<string, AssetInstance> assetTypeDict = _assets[(int) type];
      lock (assetTypeDict)
      {
        if (assetTypeDict.TryGetValue(key, out assetInstance))
          result = assetInstance.asset.Target as IAsset;
        else
          assetInstance = NewAssetInstance(key, type, createAssetCore());
        
        // If the asset instance was just created, create asset wrapper. If the asset wrapper has been garbage collected, re-allocate it.
        if (result == null)
        {
          result = createAsset(assetInstance.core);
          assetInstance.asset = new WeakReference(result);
        }
      }
      return result;
    }

    private AssetInstance NewAssetInstance(string key, AssetType type, IAssetCore newcore)
    {
      AssetInstance inst = new AssetInstance { core = newcore };
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
        if (enumer.Current.Value.core.IsAllocated && (!checkIfCanBeDeleted || enumer.Current.Value.core.CanBeDeleted))
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
