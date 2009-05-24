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
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.Effects;
using MediaPortal.SkinEngine.Fonts;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.ContentManagement
{
  // FIXME Albert: make a real signleton object, implement a disposal method to unsubscribe from queue
  public class ContentManager
  {
    static public int TextureReferences = 0;
    static public int VertexReferences = 0;

    #region Private variables

    private static Dictionary<string, IAsset> _assetsNormal = new Dictionary<string, IAsset>();
    private static Dictionary<string, IAsset> _assetsHigh = new Dictionary<string, IAsset>();
    private static List<IAsset> _vertexBuffers = new List<IAsset>();
    private static List<IAsset> _unnamedAssets = new List<IAsset>();
    private static DateTime _timer = SkinContext.Now;

    #endregion

    static ContentManager()
    {
      IMessageBroker msgBroker = ServiceScope.Get<IMessageBroker>();
      msgBroker.Register_Async("contentmanager", queue_OnMessageReceived);
    }

    static void queue_OnMessageReceived(QueueMessage message)
    {
      if (message.MessageData.ContainsKey("action") && message.MessageData.ContainsKey("fullpath"))
      {
        string action = (string)message.MessageData["action"];
        if (action == "changed")
        {
          string fileName = (string)message.MessageData["fullpath"];
          lock (_assetsNormal)
          {
            if (_assetsNormal.ContainsKey(fileName))
            {
              TextureAsset asset = (TextureAsset)_assetsNormal[fileName];
              asset.Free(true);
            }
          }
          lock (_assetsHigh)
          {
            if (_assetsHigh.ContainsKey(fileName))
            {
              TextureAsset asset = (TextureAsset)_assetsHigh[fileName];
              asset.Free(true);
            }
          }
        }
      }
    }

    /// <summary>
    /// Adds an asset to the un-named asset collection
    /// </summary>
    /// <param name="unknownAsset">The unknown asset.</param>
    public static void Add(IAsset unknownAsset)
    {
      lock (_unnamedAssets)
      {
        _unnamedAssets.Add(unknownAsset);
      }
    }

    /// <summary>
    /// Removes the specified  asset.
    /// </summary>
    /// <param name="unknownAsset">The unknown asset.</param>
    public static void Remove(IAsset unknownAsset)
    {
      lock (_unnamedAssets)
      {
        if (_unnamedAssets.Remove(unknownAsset)) return;
      }
      lock (_vertexBuffers)
      {
        if (_vertexBuffers.Remove(unknownAsset)) return;
      }
      lock (_assetsNormal)
      {
        Dictionary<string, IAsset>.Enumerator enumer = _assetsNormal.GetEnumerator();
        while (enumer.MoveNext())
        {
          if (enumer.Current.Value == unknownAsset)
          {
            _assetsNormal.Remove(enumer.Current.Key);
            break;
          }
        }
      }
      lock (_assetsHigh)
      {
        Dictionary<string, IAsset>.Enumerator enumer = _assetsHigh.GetEnumerator();
        while (enumer.MoveNext())
        {
          if (enumer.Current.Value == unknownAsset)
          {
            _assetsHigh.Remove(enumer.Current.Key);
            break;
          }
        }
      }
    }

    /// <summary>
    /// returns a fontbuffer asset for the specified font
    /// </summary>
    /// <param name="font">The font.</param>
    /// <returns></returns>
    public static FontBufferAsset GetFont(Font font)
    {
      lock (_vertexBuffers)
      {
        FontBufferAsset vertex = new FontBufferAsset(font);
        _vertexBuffers.Add(vertex);
        return vertex;
      }
    }

    /// <summary>
    /// returns a vertex buffer asset for the specified graphic file
    /// </summary>
    /// <param name="fileName">Name of the file (.jpg, .png).</param>
    /// <returns></returns>
    public static VertextBufferAsset Load(string fileName, bool normal)
    {
      TextureAsset texture = GetTexture(fileName, normal);
      lock (_vertexBuffers)
      {
        VertextBufferAsset vertex = new VertextBufferAsset(texture);
        _vertexBuffers.Add(vertex);
        return vertex;
      }
    }

    public static EffectAsset GetEffect(string effectName)
    {
      lock (_assetsNormal)
      {
        if (_assetsNormal.ContainsKey(effectName))
        {
          return (EffectAsset)_assetsNormal[effectName];
        }
        EffectAsset newEffect = new EffectAsset(effectName);
        _assetsNormal[effectName] = newEffect;
        return newEffect;
      }
    }

    /// <summary>
    /// returns a texture asset for the specified graphic file
    /// </summary>
    /// <param name="fileName">Name of the file (.jpg, .png).</param>
    /// <returns></returns>
    public static TextureAsset GetTexture(string fileName, bool thumb)
    {
      if (thumb)
      {
        lock (_assetsNormal)
        {
          if (_assetsNormal.ContainsKey(fileName))
          {
            return (TextureAsset)_assetsNormal[fileName];
          }
          TextureAsset newImage = new TextureAsset(fileName);
          _assetsNormal[fileName] = newImage;
          return newImage;
        }
      }

      lock (_assetsHigh)
      {
        if (_assetsHigh.ContainsKey(fileName))
        {
          return (TextureAsset)_assetsHigh[fileName];
        }
        TextureAsset newImage = new TextureAsset(fileName);
        _assetsHigh[fileName] = newImage;
        return newImage;
      }
    }

    /// <summary>
    /// Frees any un-used assets
    /// </summary>
    public static void Clean()
    {
      TimeSpan ts = SkinContext.Now - _timer;
      if (ts.TotalSeconds < 1)
      {
        return;
      }
      _timer = SkinContext.Now;

      Free(true, false);
      //Trace.WriteLine(String.Format("Mgr: normal:{0} high:{1} unnamed:{2} vertex:{3}",
      //_assetsNormal.Count, _assetsHigh.Count, _unnamedAssets.Count, _vertexBuffers.Count));
    }

    protected static void Free(ICollection<IAsset> assets, bool checkIfCanBeDeleted, bool force)
    {
      lock (assets)
      {
        foreach (IAsset asset in assets)
        {
          if (asset.IsAllocated && (!checkIfCanBeDeleted || asset.CanBeDeleted))
          {
            asset.Free(force);
          }
        }
      }
    }

    protected static void Free(IDictionary<string, IAsset> assets, bool checkIfCanBeDeleted, bool force)
    {
      lock (assets)
      {
        Free(assets.Values, checkIfCanBeDeleted, force);
      }
    }

    public static void Free()
    {
      Free(false, true);
    }

    /// <summary>
    /// Frees all resources
    /// </summary>
    protected static void Free(bool checkIfCanBeDeleted, bool force)
    {
      Free(_assetsNormal, checkIfCanBeDeleted, force);
      Free(_assetsHigh, checkIfCanBeDeleted, force);
      Free(_unnamedAssets, checkIfCanBeDeleted, force);
      Free(_vertexBuffers, checkIfCanBeDeleted, force);
    }

    public static void Clear()
    {
      Free();

      _vertexBuffers.Clear();
      _unnamedAssets.Clear();
      _assetsHigh.Clear();
      _assetsNormal.Clear();
    }

  }
}