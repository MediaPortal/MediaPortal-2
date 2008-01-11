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
using CSScriptLibrary;
using MediaPortal.Core;
using MediaPortal.Core.Logging;

namespace SkinEngine.Scripts
{
  public class ScriptManager
  {
    #region variables

    private Assembly _scripts;
    private List<string> _scriptNames = new List<string>();

    private static ScriptManager _instance;

    #endregion

    /// <summary>
    /// Initializes the <see cref="ScriptManager"/> class.
    /// </summary>
    private ScriptManager()
    {
      ComposeTotalScript();
    }

    /// <summary>
    /// Gets the ScriptManager instance.
    /// </summary>
    /// <value>The ScriptManager.</value>
    public static ScriptManager Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = new ScriptManager();
        }
        return _instance;
      }
    }

    /// <summary>
    /// Reloads all scripts.
    /// </summary>
    public void Reload()
    {
      _scripts = null;
      _scriptNames.Clear();
      ComposeTotalScript();
    }

    private void ComposeTotalScript()
    {
      ServiceScope.Get<ILogger>().Info("ScriptManager:compose");
      string totalFile = String.Format(@"skin\{0}\scripts\allscripts.txt", SkinContext.SkinName);
      string finalName = String.Format(@"skin\{0}\scripts\Precompiled\{1}.dll", SkinContext.SkinName, Path.GetFileNameWithoutExtension(totalFile));

      try
      {
        string folder = String.Format(@"skin\{0}\scripts", SkinContext.SkinName);
        string[] files = Directory.GetFiles(folder, "*.cs");
        for (int i = 0; i < files.Length; ++i)
        {
          string name = Path.GetFileNameWithoutExtension(files[i]);
          _scriptNames.Add(name);
        }
        if (!File.Exists(totalFile) || !File.Exists(finalName))
        {
          string totalScript = "";
          List<string> usings = new List<string>();
          for (int i = 0; i < files.Length; ++i)
          {
            List<string> classes = new List<string>();
            using (Stream stream = new FileStream(files[i], FileMode.Open, FileAccess.ReadWrite))
            {
              string name = Path.GetFileNameWithoutExtension(files[i]);
              ServiceScope.Get<ILogger>().Info("ScriptManager:  {0}", name);
              string script = "";
              using (StreamReader reader = new StreamReader(stream))
              {
                while (true)
                {
                  string line = reader.ReadLine();
                  if (line == null)
                  {
                    break;
                  }
                  if (line.StartsWith("using"))
                  {
                    bool exists = false;
                    foreach (string us in usings)
                    {
                      if (us == line)
                      {
                        exists = true;
                      }
                    }
                    if (!exists)
                    {
                      usings.Add(line);
                    }
                  }
                  else
                  {
                    if (line.Contains(" class "))
                    {
                      string className;
                      int pos = line.IndexOf(" class ");
                      int posEnd = pos + " class ".Length;
                      int pos2 = line.IndexOf(" ", posEnd);
                      if (pos2 < 0)
                      {
                        className = line.Substring(posEnd);
                      }
                      else
                      {
                        className = line.Substring(posEnd, pos2 - posEnd);
                      }
                      classes.Add(className);
                    }
                    line += "\r\n";
                    script += line;
                  }
                }
              }
              foreach (string c in classes)
              {
                script = script.Replace(c, String.Format("{0}{1}", c, name));
              }
              totalScript += script;
            }
          }

          try
          {
            if (File.Exists(totalFile))
            {
              File.Delete(totalFile);
            }
          }
          catch
          {
          }
          using (Stream stream = new FileStream(totalFile, FileMode.OpenOrCreate, FileAccess.Write))
          {
            using (StreamWriter writer = new StreamWriter(stream))
            {
              foreach (string us in usings)
              {
                writer.WriteLine(us);
              }
              writer.Write(totalScript);
            }
          }
        }
      }
      catch (Exception)
      {
      }

      try
      {
        ServiceScope.Get<ILogger>().Info("ScriptManager:loading script:{0}", totalFile);

        // skinengine assembly no longer in root directory ... this needs to come from somewhere else
        string rootAsemblyDir = Path.GetDirectoryName(GetLoadedAssemblyLocation("MediaPortal"));
        string skinAsemblyDir = Path.GetDirectoryName(GetLoadedAssemblyLocation("skinengine"));
        try
        {
          File.Copy(rootAsemblyDir + @"\mediaportal.core.dll", skinAsemblyDir + @"\mediaportal.core.dll", true);
        }
        catch (Exception) { }
        finalName = Path.Combine(rootAsemblyDir, finalName);
        string name = String.Format(@"{0}.dll", Path.GetFileNameWithoutExtension(totalFile));
        string scriptCopy = Path.Combine(skinAsemblyDir, name);
        File.Copy(totalFile, scriptCopy, true);
#if DEBUG
        bool debug = true;
#else
        bool debug = false;
#endif
        Assembly assembly = CSScript.Load(scriptCopy, null, debug);
        try
        {
          File.Delete(scriptCopy);
        }
        catch (Exception) { }
        try
        {
          File.Delete(skinAsemblyDir + @"\mediaportal.core.dll");
        }
        catch (Exception) { }

        _scripts = assembly;
        ServiceScope.Get<ILogger>().Info("ScriptManager:scripts loaded");
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("ScriptManager:Errror while compiling script:{0}", totalFile);
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

    /// <summary>
    /// Gets the loaded assembly location.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    private static string GetLoadedAssemblyLocation(string name)
    {
      foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
      {
        string asmName = asm.FullName.Split(",".ToCharArray())[0];
        if (string.Compare(name, asmName, true) == 0)
        {
          return asm.Location;
        }
      }
      return "";
    }


    /// <summary>
    /// Determines whether the scriptmanager contains the script
    /// </summary>
    /// <param name="script">The scriptname.</param>
    /// <returns>
    /// 	<c>true</c> if scriptmanager contains the specified script; otherwise, <c>false</c>.
    /// </returns>
    public bool Contains(string script)
    {
      return _scriptNames.Contains(script);
    }

    /// <summary>
    /// Gets the script.
    /// </summary>
    /// <param name="key">The scriptname.</param>
    /// <returns></returns>
    public object GetScript(string key)
    {
      return _scripts.CreateInstance("Scriptlet" + key);
    }
  }
}
