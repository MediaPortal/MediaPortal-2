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
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CodeCleanup
{
  public partial class MainWindow : Window
  {
    private const string Filename_old_Header = "header.old.txt";
    private const string Filename_new_Header = "header.new.txt";
    private const string Filename_BlackList = "Blacklist.txt";

    private const string Filename_AssemblyInfo_Header = "AssemblyInfo.Header.*.txt";
    private const string Filename_AssemblyInfo_Footer = "AssemblyInfo.Footer.*.txt";

    public MainWindow()
    {
      InitializeComponent();
      LoadSettings();
    }

    private void LoadSettings()
    {
      string appDir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

      #region source directory settings

      // set source dir
      try
      {
        DirectoryInfo dirInfo = new DirectoryInfo(appDir);

        while (!Directory.Exists(dirInfo.Parent.FullName + "\\.git"))
          dirInfo = dirInfo.Parent;

        rootTextBox.Text = dirInfo.FullName;
      }
      catch (Exception)
      {
      }


      // load blacklist
      blackListBox.Items.Clear();
      try
      {
        string[] blackList = File.ReadAllLines(Filename_BlackList);
        foreach (string s in blackList)
        {
          blackListBox.Items.Add(s);
        }
      }
      catch (Exception ex)
      {
      }

      #endregion

      #region UpdateHeader settings

      try
      {
        oldHeaderTextBox.Text = File.ReadAllText(Filename_old_Header);
      }
      catch (Exception ex)
      {
        oldHeaderTextBox.Text = string.Empty;
      }

      try
      {
        newHeaderTextBox.Text = File.ReadAllText(Filename_new_Header);
      }
      catch (Exception ex)
      {
        newHeaderTextBox.Text = string.Empty;
      }

      #endregion

      #region AssemblyInfo settings

      foreach (var fi in new DirectoryInfo(appDir).GetFiles(Filename_AssemblyInfo_Header))
      {
        TextBox tb = new TextBox();
        tb.Text = File.ReadAllText(fi.FullName);
        stackHeader.Children.Add(tb);
      }

      foreach (var fi in new DirectoryInfo(appDir).GetFiles(Filename_AssemblyInfo_Footer))
      {
        TextBox tb = new TextBox();
        tb.Text = File.ReadAllText(fi.FullName);
        stackFooter.Children.Add(tb);
      }

      #endregion
    }

    private bool ValideCommonSettings()
    {
      bool checkSettings = true;

      logListBox.Items.Clear();

      if (string.IsNullOrEmpty(rootTextBox.Text))
      {
        logListBox.Items.Add("No path given as argument.");
        checkSettings = false;
      }
      else if (!Directory.Exists(rootTextBox.Text))
      {
        logListBox.Items.Add("Directory not found: " + rootTextBox.Text);
        checkSettings = false;
      }

      return checkSettings;
    }

    private void UpdateHeader(object sender, RoutedEventArgs e)
    {
      bool checkSettings = ValideCommonSettings();

      if (string.IsNullOrEmpty(oldHeaderTextBox.Text))
      {
        logListBox.Items.Add("Old header is empty");
        checkSettings = false;
      }

      if (string.IsNullOrEmpty(newHeaderTextBox.Text))
      {
        logListBox.Items.Add("New header is empty");
        checkSettings = false;
      }

      if (!checkSettings)
        return;

      UpdateHeader(new DirectoryInfo(rootTextBox.Text));
    }

    private void UpdateHeader(DirectoryInfo directory)
    {
      foreach (string s in blackListBox.Items)
      {
        if (directory.FullName.EndsWith(s))
          return;
      }

      foreach (var di in directory.GetDirectories())
        UpdateHeader(di);

      foreach (var fi in directory.GetFiles("*.cs"))
        UpdateHeader(fi);
    }

    private void UpdateHeader(FileInfo file)
    {
      if (file.Name.ToLower().Equals("assemblyinfo.cs"))
        return;
      if (file.Name.ToLower().EndsWith(".designer.cs"))
        return;

      string content = File.ReadAllText(file.FullName);

      if (content.StartsWith(newHeaderTextBox.Text))
      {
        // nothing to do, file is already up to date
      }
      else if (content.StartsWith(oldHeaderTextBox.Text))
      {
        content = content.Replace(oldHeaderTextBox.Text, newHeaderTextBox.Text);
        File.WriteAllText(file.FullName, content);
      }
      else
      {
        logListBox.Items.Add(new FileBasedLogEntry(file.FullName, "File with unknown header found: "));
      }
    }

    private void CheckAssemblyInfo(object sender, RoutedEventArgs e)
    {
      bool checkSettings = ValideCommonSettings();

      if (!checkSettings)
        return;

      CheckAssemblyInfo(new DirectoryInfo(rootTextBox.Text));
    }

    private void CheckAssemblyInfo(DirectoryInfo directory)
    {
      foreach (string s in blackListBox.Items)
      {
        if (directory.FullName.EndsWith(s))
          return;
      }

      foreach (var di in directory.GetDirectories())
        CheckAssemblyInfo(di);

      foreach (var fi in directory.GetFiles("AssemblyInfo.cs"))
        CheckAssemblyInfo(fi);
    }

    private void CheckAssemblyInfo(FileInfo file)
    {
      string content = File.ReadAllText(file.FullName);

      bool isHeaderOK = false;
      foreach (var child in stackHeader.Children)
      {
        TextBox tb = child as TextBox;
        if (ReferenceEquals(tb, null)) continue;

        if (content.StartsWith(tb.Text))
          isHeaderOK = true;
      }
      if (!isHeaderOK)
        logListBox.Items.Add(new FileBasedLogEntry(file.FullName, "AssemblyInfo.cs with unknown header found: "));

      bool isFooterOK = false;
      foreach (var child in stackFooter.Children)
      {
        TextBox tb = child as TextBox;
        if (ReferenceEquals(tb, null)) continue;

        if (content.EndsWith(tb.Text))
          isFooterOK = true;
      }
      if (!isFooterOK)
        logListBox.Items.Add(new FileBasedLogEntry(file.FullName, "AssemblyInfo.cs with unknown footer found: "));
    }

    private void logListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (logListBox.SelectedItems.Count != 1) return;

      FileBasedLogEntry item = logListBox.SelectedItems[0] as FileBasedLogEntry;
      if (ReferenceEquals(item, null)) return;

      item.OpenExplorerFileSelected();
    }
  }
}
