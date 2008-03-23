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
  public class ModelManager
  {
    #region variables

    private static ModelManager _instance;
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
    /// returns the ModelManager
    /// </summary>
    /// <value>The instance.</value>
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
    /// Determines whether the modelmanager contains the specified model
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="className">Name of the class.</param>
    /// <returns>
    /// 	<c>true</c> if [contains] [the specified model name]; otherwise, <c>false</c>.
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
    /// returns the specified model
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="className">Name of the class.</param>
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
    public Model GetModelByName(string name)
    {
      for (int i = 0; i < _models.Count; ++i)
      {
        Model m = _models[i];
        string mName = String.Format("{0}:{1}", m.Assembly, m.ClassName);
        if (mName == name)
        {
          return m;
        }
      }
      return null;
    }

    /// <summary>
    /// Loads the model from disk
    /// </summary>
    /// <param name="assemblyName">Name of the assembly.</param>
    /// <param name="className">Name of the class.</param>
    public void Load(string assemblyName, string className)
    {
      ServiceScope.Get<ILogger>().Debug("ModelManager: Load model plugin: {0} class:{1}", assemblyName, className);

      
      try
      {
        object model = ServiceScope.Get<IPluginManager>().GetPluginItem<IPlugin>("/Models/" + assemblyName, className);
        Type exportedType = model.GetType();
        if (exportedType.IsClass && exportedType.Name == className)
        {
          _models.Add(new Model(assemblyName, className, exportedType, model));
          return;
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Debug("ModelManager: failed to load model plugin:{0} class:{1}", assemblyName,
                                          className);
        ServiceScope.Get<ILogger>().Error(ex);
      }
      
      /*
      try
      {
        string assemblyFileName = String.Format(@"{0}\models\{1}.dll", Directory.GetCurrentDirectory(), assemblyName);
        Assembly assembly = Assembly.LoadFile(assemblyFileName);
        Type[] exportedTypes = assembly.GetExportedTypes();
        for (int i = 0; i < exportedTypes.Length; ++i)
        {
          if (exportedTypes[i].IsClass && exportedTypes[i].Name == className)
          {
            object model = Activator.CreateInstance(exportedTypes[i]);
            _models.Add(new Model(assemblyName, className, exportedTypes[i], model));
            return;
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Debug("ModelManager: failed to load model assembly:{0} class:{1}", assemblyName,
                                          className);
        ServiceScope.Get<ILogger>().Error(ex);
      }
      */
    }
  }
}
