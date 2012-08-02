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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Xml.XPath;
using MP2_PluginWizard.Model;
using MP2_PluginWizard.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;


namespace MP2_PluginWizard.View
{
  /// <summary>
  /// Interaction logic for WelcomePage.xaml
  /// </summary>
  public partial class WelcomePage
  {

    #region Ctor/Dtor
    public WelcomePage()
    {
      ServiceRegistration.Get<ILogger>().Debug("WelcomePage: Init");
      InitializeComponent();
      EnableNextButton = false;
    }
    #endregion 
    
    #region Private Methods
    #region mp2 region
    private void OnMp2UpdateClick(object sender, RoutedEventArgs e)
    {
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      var settings = settingsManager.Load<PluginsSettings>();


      var dialog = new FolderBrowserDialog
                     {
                       SelectedPath = settings.Mp2Path,
                       RootFolder = Environment.SpecialFolder.Desktop,
                       Description = "Please select your MediaPortal2 directory."
                     };
      if (dialog.ShowDialog() != DialogResult.OK) 
      	return;
      settings.Mp2Path = dialog.SelectedPath;
      settingsManager.Save(settings);

      ViewModel.PluginList.Clear();
      ViewModel.Mp2PluginsAvailable = false;

      var bgWorker = new BackgroundWorker();
      bgWorker.DoWork += LoadPluginList;
	    bgWorker.RunWorkerCompleted += BackgroundWorkerCompleted;
      bgWorker.RunWorkerAsync(dialog.SelectedPath);
    }

    private void LoadPluginList(object sender, DoWorkEventArgs e)
    {
      var selectedPath = (string) e.Argument;
      ServiceRegistration.Get<ILogger>().Debug("Updating plugin data from [{0}]", selectedPath);

      var pluginList = new List<PluginNameId>();

      // Store a stack of our directories.
      var stack = new Stack<string>();
      // Add initial directory.
      stack.Push(selectedPath);

      // Continue while there are directories to process
      while (stack.Count > 0)
      {
        // Get top directory
        var dir = stack.Pop();
        try
        {
          var pluginPathFile = Path.Combine(dir, "plugin.xml");
          if (File.Exists(pluginPathFile))
          {
            var pluginNameId = ReadPluginNameId(pluginPathFile);
            if (pluginNameId != null)
            {
              if (!pluginList.Any(x => x.Id.Equals(pluginNameId.Id)))
                pluginList.Add(pluginNameId);
            }
          }
          else
          {
            // Add all subdirectories from current directory.
            foreach (var newDir in Directory.GetDirectories(dir).Where(newDir => !newDir.EndsWith("Resources", StringComparison.OrdinalIgnoreCase)))
              stack.Push(newDir);
          }
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error loading plugins from [" + selectedPath + "]", ex);
          break;
        }
      }
      e.Result = pluginList;
    }

    private void BackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
      var pluginList = (List<PluginNameId>) e.Result;
     
      foreach (var plugin in pluginList)
        ViewModel.PluginList.Add(plugin);
      
      ViewModel.PluginList.Sort(x => x.Name, ListSortDirection.Ascending);
      ViewModel.Mp2PluginsAvailable = true;


      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      var settings = settingsManager.Load<PluginsSettings>();

      if (settings.Plugins == null)
        settings.Plugins = new List<PluginNameId>();
      settings.Plugins.Clear();

      foreach (var plugin in ViewModel.PluginList)
        settings.Plugins.Add(plugin);

      settingsManager.Save(settings);
      CheckEnableNextButton();
    }

    private PluginNameId ReadPluginNameId(string pluginPathFile)
    {
      try
      {
        using (Stream pluginFileStream = File.OpenRead(pluginPathFile))
        {
          var doc = new XPathDocument(pluginFileStream);
          var nav = doc.CreateNavigator();
          nav.MoveToChild(XPathNodeType.Element);
          if (nav.LocalName != "Plugin")
            return null;

          var name = "";
          var id ="";
          var nameOk = false;
          var pluginIdOk = false;
          var attrNav = nav.Clone();
          if (attrNav.MoveToFirstAttribute())
            do
            {
              switch (attrNav.Name)
              {
                case "Name":
                  name = attrNav.Value;
                  nameOk = true;
                  break;
                case "PluginId":
                  id = attrNav.Value;
                  pluginIdOk = true;
                  break;
              }
            } while (attrNav.MoveToNextAttribute() && !(nameOk && pluginIdOk));
          if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(id))
            return new PluginNameId(name, new Guid(id));
        }
      }
      catch(Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error loading plugin name and id from [" + pluginPathFile + "]", e);
      }
      return null;
    }

    #endregion

    #region plugin region
    private void PluginDirButtonClick(object sender, RoutedEventArgs e)
    {
      var dialog = new FolderBrowserDialog();
      if (dialog.ShowDialog() != DialogResult.OK) 
      	return;

      ViewModel.AddNewPluginPathName(dialog.SelectedPath);
    }
    
    private void PluginPathSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
      // ensure that we have a valid MP2 directory to read in all plugin.xml files
      if (string.IsNullOrEmpty(ViewModel.PluginPathName))
        return;

      if (!Directory.Exists(ViewModel.PluginPathName))
        Directory.CreateDirectory(ViewModel.PluginPathName);

      // Init the static Plugin model for the wizard
      var pluginFile = Path.Combine(ViewModel.PluginPathName, "plugin.xml");
      if (File.Exists(pluginFile))
        ViewModel.Load(ViewModel.PluginPathName);
      else 
      	ViewModel.Reset();
      CheckEnableNextButton();
    }
    
    private void CheckEnableNextButton()
    {
    	EnableNextButton = ((ViewModel.Mp2PluginsAvailable == true) &&
    	                    (!string.IsNullOrEmpty(ViewModel.PluginPathName)));
    }
    
    #endregion

    #endregion

  }
}
