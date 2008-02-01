using System;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MyXaml.Core;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using SkinEngine.Controls.Animations;
using SkinEngine.Controls.Brushes;
using SkinEngine.Controls.Panels;
using SkinEngine.Controls.Transforms;
using SkinEngine.Controls.Visuals;
using SkinEngine.Controls.Visuals.Triggers;
using SkinEngine.Controls.Bindings;

using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
namespace SkinEngine.Skin
{
  public class ResourceDictionaryCache
  {
    static ResourceDictionaryCache _instance;
    Dictionary<string, ResourceDictionary> _dictionaries;
    protected ResourceDictionaryCache()
    {
      _dictionaries = new Dictionary<string, ResourceDictionary>();
    }

    static public ResourceDictionaryCache Instance
    {
      get
      {
        if (_instance == null)
          _instance = new ResourceDictionaryCache();
        return _instance;
      }
    }

    public ResourceDictionary Get(string resourceName)
    {
      if (!_dictionaries.ContainsKey(resourceName))
      {
        XamlLoader loader = new XamlLoader();
        _dictionaries[resourceName] = (loader.Load(resourceName) as ResourceDictionary);
      }
      return _dictionaries[resourceName];
    }
  }
}
