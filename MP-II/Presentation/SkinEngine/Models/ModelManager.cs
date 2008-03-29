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
using System.Reflection;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;

namespace Presentation.SkinEngine
{
  /// <summary>
  /// Singleton class to manage loading and caching model instances.
  /// </summary>
  public class ModelManager
  {
    #region Variables

    /// <summary>
    /// Singleton instance variable.
    /// </summary>
    private static ModelManager _instance;

    /// <summary>
    /// Model cache.
    /// </summary>
    private readonly List<Model> _models;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelManager"/> class.
    /// </summary>
    private ModelManager()
    {
      _models = new List<Model>();
    }

    /// <summary>
    /// Returns the Singleton ModelManager instance.
    /// </summary>
    /// <value>The Singleton ModelManager instance.</value>
    public static ModelManager Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = new ModelManager();
        }
        return _instance;
      }
    }

    /// <summary>
    /// Determines whether the modelmanager did already load the specified model.
    /// </summary>
    /// <param name="assembly">Assembly name of the model to check.</param>
    /// <param name="className">Class name of the model to check.</param>
    /// <returns>
    /// 	<c>true</c> if the specified model was already load into the model cache,
    ///   else <c>false</c>.
    /// </returns>
    public bool Contains(string assembly, string className)
    {
      for (int i = 0; i < _models.Count; ++i)
      {
        Model m = _models[i];
        if (m.Assembly == assembly && m.ClassName == className)
        {
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Returns the specified model. The model will be load from the model
    /// cache, if it was already load. Else, it will be load and stored in the
    /// model cache.
    /// </summary>
    /// <param name="assembly">Assembly name of the model to get or load.</param>
    /// <param name="className">Class name of the model to get or load.</param>
    /// <returns></returns>
    public Model GetOrLoadModel(string assemblyName, string className)
    {
      Model result = GetModel(assemblyName, className);
      if (result == null)
      {
        result = Load(assemblyName, className);
        if (result != null)
          _models.Add(result);
      }
      return result;
    }

    /// <summary>
    /// Returns the specified model from the model cache.
    /// </summary>
    /// <param name="assembly">Assembly name of the model to retrieve.</param>
    /// <param name="className">Class name of the model to retrieve.</param>
    /// <returns></returns>
    public Model GetModel(string assembly, string className)
    {
      for (int i = 0; i < _models.Count; ++i)
      {
        Model m = _models[i];
        if (m.Assembly == assembly && m.ClassName == className)
        {
          return m;
        }
      }
      return null;
    }

    /// <summary>
    /// Returns a model by its composed name. The composed name is in the form:
    /// <code>
    /// [model.Assembly]:[model.ClassName]
    /// </code>
    /// This method is a convenience method for method
    /// <see cref="GetModel(string assembly, string className)"/>.
    /// </summary>
    /// <param name="name">Name of the model to retrieve. The model name is of
    /// the form <c>[model.Assembly]:[model.ClassName]</c>.</param>
    /// <returns></returns>
    public Model GetModelByName(string name)
    {
      int index = name.IndexOf(':');
      if (index == -1)
        return null;
      string assembly = name.Substring(0, index);
      string className = name.Substring(index + 1);
      return GetModel(assembly, className);
    }

    /// <summary>
    /// Loads the specified model from disk. This method doesn't store the returned method in
    /// the internal model cache. To get a model instance from outside this class, use
    /// method <see cref="GetOrLoadModel(string assemblyName, string className)"/>.
    /// </summary>
    /// <param name="assemblyName">Assembly name of the model to load.</param>
    /// <param name="className">Class name of the model to load.</param>
    /// <returns>The model requested to load, else <code>null</code> if the model was not
    /// registered as expected and therefore it could not be load.</returns>
    protected static Model Load(string assemblyName, string className)
    {
      ServiceScope.Get<ILogger>().Debug("ModelManager: Load model assemblyName: {0} class:{1}", assemblyName, className);
      try
      {
        object model = ServiceScope.Get<IPluginManager>().GetPluginItem<IPlugin>("/Models/" + assemblyName, className);
        if (model != null)
        {
          Type exportedType = model.GetType();
          if (exportedType.IsClass)
          {
            return new Model(assemblyName, className, exportedType, model);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("ModelManager: failed to load model assemblyName: {0} class:{1}", ex, assemblyName, className);
      }
      return null;
    }
  }
}
