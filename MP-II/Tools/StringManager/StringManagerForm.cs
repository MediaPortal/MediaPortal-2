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
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Globalization;
using MediaPortal.Utilities.Xml;
using MediaPortal.Utilities.Localisation;
using MediaPortal.Utilities.Localisation.Strings;
using MediaPortal.Tools.StringManager.SolutionFiles;

namespace MediaPortal.Tools.StringManager
{
  public partial class StringManagerForm : Form
  {
    #region enum
    private enum Tabs
    {
      Create,
      Translate,
      Manage,
      Settings
    }
    #endregion

    #region Variables
    // Settings
    private string _configFile;
    private StringManagerConfig _config;
    private string _stringPath;

    //Strings
    private LocalisationStrings _strings;

    // Create

    //Manage tab
    private List<StringMatch> _matchedStrings;

    //translate
    private Dictionary<string, List<string>> _missingList;
    private List<LanguageInfo> _languageList;
    private Dictionary<string, StringLocalised> _editList;
    private StringFile _defaultStrings;
    private StringFile _targetStrings;
    private bool _modifiedSection;
    private int _lastSelected;
    private int _currentSection;
    private int _currentLanguage;
    private int _targetSection;
    private Color _lastFore;
    private Color _lastBack;
    #endregion

    #region Constructors/Destructors
    public StringManagerForm()
    {
      InitializeComponent();

      // load config file if it exists
      LoadConfig();

      // Catch closing event - check if un saved changes exist
      this.Closing += new System.ComponentModel.CancelEventHandler(this.Form_Closing);

      // Manage Tab
      lvMatches.Groups.Clear();
      string[] groupNames = Enum.GetNames(typeof(StringMatch.Source));
      foreach (string group in groupNames)
      {
        lvMatches.Groups.Add(new ListViewGroup(group, HorizontalAlignment.Left));
      }

      // Check settings - enable tabs based on settings
      if (tbStringsPath.Text == string.Empty)
      {
        tabsModes.Controls[(int)Tabs.Create].Enabled = false;
        tabsModes.Controls[(int)Tabs.Translate].Enabled = false;
        tabsModes.Controls[(int)Tabs.Manage].Enabled = false;
        tabsModes.SelectedIndex = (int)Tabs.Settings;
      }
      else
      {
        if (tbSolution.Text == string.Empty && tbSkinPath.Text == string.Empty)
          tabsModes.Controls[(int)Tabs.Manage].Enabled = false;
        tabsModes.SelectedIndex = (int)Tabs.Translate;
      }
      btnSave.Enabled = false;
      btnSaveNewStrings.Enabled = false;
      _currentLanguage = 0;
      _currentSection = 0;
      _targetSection = 0;
    }
    #endregion

    #region Private
    #region Save Load Files
    private void LoadConfig()
    {
      _configFile = Path.Combine(Environment.CurrentDirectory, "StringManager.Config.Xml");
      if (File.Exists(_configFile))
      {
        XmlSerializer s = new XmlSerializer(typeof(StringManagerConfig));
        TextReader r = new StreamReader(_configFile);
        _config = (StringManagerConfig)s.Deserialize(r);

        tbStringsPath.Text = _config.stringPath;
        tbSkinPath.Text = _config.skinPath;
        tbSolution.Text = _config.solutionFile;
      }
    }

    private void SaveConfig()
    {
      if (_config == null)
        _config = new StringManagerConfig();

      _config.stringPath = tbStringsPath.Text;
      _config.skinPath = tbSkinPath.Text;
      _config.solutionFile = tbSolution.Text;

      if (File.Exists(_configFile + ".Bak"))
        File.Delete(_configFile + ".Bak");

      if (File.Exists(_configFile))
        File.Move(_configFile, _configFile + ".Bak");

      XmlSerializer s = new XmlSerializer(typeof(StringManagerConfig));
      TextWriter w = new StreamWriter(_configFile);
      s.Serialize(w, _config);

    }

    private bool IsValidLanguagePath()
    {
      if (Directory.Exists(tbStringsPath.Text) && File.Exists(Path.Combine(tbStringsPath.Text, "strings_en.xml")))
        return true;

      return false;
    }

    private StringFile LoadStrings(string directory, string language)
    {
      string filename = "strings_" + language + ".xml";

      string path = Path.Combine(directory, filename);
      if (File.Exists(path))
      {
        StringFile strings;
        try
        {
          XmlSerializer s = new XmlSerializer(typeof(StringFile));
          TextReader r = new StreamReader(path);
          strings = (StringFile)s.Deserialize(r);
          return strings;
        }
        catch (Exception)
        {
        }
      }
      return null;
    }

    private void SaveStrings(StringFile strings, string directory, string language)
    {
      string filename = "strings_" + language + ".xml";

      string path = Path.Combine(directory, filename);
      if (File.Exists(path + ".bak"))
        File.Delete(path + ".bak");

      if (File.Exists(path))
        File.Move(path, path + ".bak");

      try
      {
        XmlSerializer s = new XmlSerializer(typeof(StringFile));
        TextWriter w = new StreamWriter(path);
        XmlTextWriter writer = new XmlNoNamespaceWriter(w);
        writer.Formatting = Formatting.Indented;
        s.Serialize(writer, strings);
        writer.Close();
      }
      catch (Exception)
      {
      }
    }

    private void LoadDefaultStrings()
    {
      _strings = new LocalisationStrings(_stringPath, "en");
      _defaultStrings = LoadStrings(_stringPath, "en");

      // Get Available language list
      _languageList = new List<LanguageInfo>();
      foreach (CultureInfo language in _strings.AvailableLanguages())
      {
        if (language.Name != "en")
        {
          LanguageInfo langInfo = new LanguageInfo(language);
          _languageList.Add(langInfo);

        }
      }

      DrawLanguageList(null);

      // Build section trees
      treeSections.Nodes.Clear();
      tvSections.Nodes.Clear();
      foreach (StringSection section in _defaultStrings.sections)
      {
        treeSections.Nodes.Add(section.name);
        tvSections.Nodes.Add(section.name);
      }
    }
    #endregion

    private void LoadSolution()
    {
      Cursor.Current = Cursors.WaitCursor;
      Solution solution = new Solution(tbSolution.Text);

      _matchedStrings = new List<StringMatch>();
      foreach (CSProject project in solution.Projects)
      {
        foreach (string file in project.csList)
        {
          TextReader reader = new StreamReader(Path.Combine(project.dir, file));
          string source = reader.ReadToEnd();
          Regex stringSearch = new Regex("(StringId|Get<ILocalisation>\\(\\)\\.ToString)\\(\"(?<section>[a-z^\\.]+)\", \"(?<name>[a-z0-9\\.]+)\"\\)");

          MatchCollection matches = stringSearch.Matches(source);
          foreach (Match stringId in matches)
          {
            StringMatch stringInfo = new StringMatch();
            stringInfo.stringId = stringId.Groups["section"].Value + "." + stringId.Groups["name"].Value;
            stringInfo.file = file;
            stringInfo.source = StringMatch.Source.Code;
            stringInfo.project = project.name;
            _matchedStrings.Add(stringInfo);
          }
        }
      }
      Cursor.Current = Cursors.Default;
    }

    private void LoadSkin()
    {
      DirectoryInfo skinDir = new DirectoryInfo(tbSkinPath.Text);

      if (skinDir.Exists)
      {
        Regex stringSearch = new Regex("\\[(?<section>[a-z^\\.]+)\\.(?<name>[a-z0-9\\.]+)\\]");

        foreach (FileInfo file in skinDir.GetFiles("*.xml"))
        {
          TextReader reader = new StreamReader(file.FullName);
          string source = reader.ReadToEnd();

          MatchCollection matches = stringSearch.Matches(source);
          foreach (Match stringId in matches)
          {
            StringMatch stringInfo = new StringMatch();
            stringInfo.stringId = stringId.Groups["section"].Value + "." + stringId.Groups["name"].Value;
            stringInfo.file = file.Name;
            stringInfo.source = StringMatch.Source.Skin;
            _matchedStrings.Add(stringInfo);
          }
        }
      }
    }

    #region Create
    private void DrawCreateSectionsTreeView()
    {
      // Build section trees
      tvCreateSections.Nodes.Clear();
      foreach (StringSection section in _defaultStrings.sections)
      {
        TreeNode node = new TreeNode();
        node.Text = section.name;
        if (section.isNew)
          node.ForeColor = System.Drawing.Color.Blue;
        tvCreateSections.Nodes.Add(node);
      }
    }
    #endregion

    #region Translate
    private void SetTargetSection()
    {
      string name = _defaultStrings.sections[_currentSection].name;
      if (_targetStrings != null)
      {
        int index = 0;
        foreach (StringSection section in _targetStrings.sections)
        {
          if (section.name == name)
          {
            _targetSection = index;
            return;
          }
          index++;
        }
      }
      _targetSection = -1;
    }

    private void DrawLanguageList(string selected)
    {
      _languageList.Sort();
      cbLanguages.Items.Clear();
      cbLanguages.Items.Add("Target Language");
      _currentLanguage = 0;
      int index = 1;
      foreach (LanguageInfo language in _languageList)
      {
        if (language.Name != "en")
        {
          cbLanguages.Items.Add(language.ToString());
          if (language.Name == selected)
          {
            _currentLanguage = index;
          }
          index++;
        }
      }
      cbLanguages.SelectedIndex = _currentLanguage;
    }

    private void StoreEditList()
    {
      if (treeSections.SelectedNode != null && _targetStrings != null && _modifiedSection)
      {
        if (_targetSection >= 0)
        {
          StringSection section = _targetStrings.sections[_targetSection];
          section.localisedStrings = new List<StringLocalised>();
          foreach (StringLocalised newString in _editList.Values)
          {
            section.localisedStrings.Add(newString);
          }
        }
        else
        {
          StringSection section = new StringSection();
          section.name = _defaultStrings.sections[_currentSection].name;
          section.localisedStrings = new List<StringLocalised>();
          foreach (StringLocalised newString in _editList.Values)
          {
            section.localisedStrings.Add(newString);
          }
          _targetStrings.sections.Add(section);
        }
        _modifiedSection = false;
      }
    }

    private void BuildEditList()
    {
      if (treeSections.SelectedNode != null && _targetStrings != null)
      {
        _editList = new Dictionary<string, StringLocalised>();
        if (_targetSection >= 0)
        {
          StringSection section = _targetStrings.sections[_targetSection];
          listTranslateStrings.Items.Clear();

          foreach (StringLocalised targetString in section.localisedStrings)
          {
            if (!_editList.ContainsKey(targetString.name))
              _editList.Add(targetString.name, targetString);
          }
        }
      }
    }

    private void DrawTargetList()
    {
      if (treeSections.SelectedNode != null && _targetStrings != null)
      {
        listTranslateStrings.Items.Clear();
        StringSection section = _defaultStrings.sections[_currentSection];
        int index = 0;
        foreach (StringLocalised defaultString in section.localisedStrings)
        {
          ListViewItem stringItem;
          if (_editList.ContainsKey(defaultString.name))
          {
            stringItem = new ListViewItem(_editList[defaultString.name].text);
          }
          else
          {
            stringItem = new ListViewItem(string.Empty);
            listDefaultStrings.Items[index].ForeColor = System.Drawing.Color.Red;
          }
          index++;
          listTranslateStrings.Items.Add(stringItem);
        }
      }
    }

    private bool CheckSave()
    {
      if (btnSave.Enabled)
      {
        DialogResult result = MessageBox.Show("World you like to save the changes?", "Warning: Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

        switch (result)
        {
          case DialogResult.Yes:
            SaveTargetStrings();
            break;
          case DialogResult.Cancel:
            return false;
          default:
            break;
        }
      }
      return true;
    }

    private void SaveTargetStrings()
    {
      if (cbLanguages.SelectedIndex > 0)
      {
        StoreEditList();
        SaveStrings(_targetStrings, _stringPath, _languageList[cbLanguages.SelectedIndex - 1].Name);
        btnSave.Enabled = false;
      }
    }

    private void BuildMissingList()
    {
      Dictionary<string, Dictionary<string, StringLocalised>> searchStrings = new Dictionary<string, Dictionary<string, StringLocalised>>();

      foreach (StringSection section in _targetStrings.sections)
      {
        Dictionary<string, StringLocalised> sectionList = new Dictionary<string, StringLocalised>();

        foreach (StringLocalised str in section.localisedStrings)
        {
          if (!sectionList.ContainsKey(str.name))
            sectionList.Add(str.name, str);
        }
        searchStrings.Add(section.name, sectionList);
      }

      _missingList = new Dictionary<string, List<string>>();
      foreach (StringSection section in _defaultStrings.sections)
      {
        if (searchStrings.ContainsKey(section.name))
        {
          List<string> missingStrings = new List<string>();
          foreach (StringLocalised str in section.localisedStrings)
          {
            if (!searchStrings[section.name].ContainsKey(str.name))
              missingStrings.Add(str.name);
          }
          if (missingStrings.Count > 0)
            _missingList.Add(section.name, missingStrings);
        }
        else
        {
          List<string> missingStrings = new List<string>();
          foreach (StringLocalised str in section.localisedStrings)
            missingStrings.Add(str.name);
          _missingList.Add(section.name, missingStrings);
        }
      }
    }

    private void ColourMissingSections()
    {
      foreach (TreeNode node in treeSections.Nodes)
      {
        if (_missingList.ContainsKey(node.Text))
          node.ForeColor = System.Drawing.Color.Red;
        else
          node.ForeColor = System.Drawing.Color.Black;
      }

      foreach (ListViewItem item in listDefaultStrings.Items)
      {
        if (_missingList.ContainsKey(treeSections.SelectedNode.Text)
          && _missingList[treeSections.SelectedNode.Text].Contains(item.Text))
          item.ForeColor = System.Drawing.Color.Red;
        else
          item.ForeColor = System.Drawing.Color.Black;
      }
    }
    #endregion
    #endregion

    #region Events
    #region Form
    private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (!CheckSave())
        e.Cancel = true;
    }
    #endregion

    #region Tab Selection
    private void tabsModes_Selecting(object sender, TabControlCancelEventArgs e)
    {
      // check if tab is enables
      if (tabsModes.Controls[tabsModes.SelectedIndex].Enabled == false)
      {
        // If tab is not enabled don't switch to it -> cancel event, displa message to user
        e.Cancel = true;
        MessageBox.Show("Settings missing for this option", "Missing Settings", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
      }
    }

    private void tabsModes_TabIndexChanged(object sender, EventArgs e)
    {
      switch (tabsModes.SelectedIndex)
      {
        case (int)Tabs.Create:
          DrawCreateSectionsTreeView();
          break;
      }
    }
    #endregion

    #region Settings Tab
    private void tbStringsPath_TextChanged(object sender, EventArgs e)
    {
      if (tbStringsPath.Text == string.Empty)
      {
        tabsModes.Controls[0].Enabled = false;
        tbStringsPath.ForeColor = System.Drawing.Color.Red;
        return;
      }

      if (tbStringsPath.Text != string.Empty && IsValidLanguagePath())
      {
        tabsModes.Controls[0].Enabled = true;
        tbStringsPath.ForeColor = System.Drawing.Color.Black;

        if (tbStringsPath.Text != _stringPath)
        {
          //Save current path
          _stringPath = tbStringsPath.Text;

          Cursor.Current = Cursors.WaitCursor;

          LoadDefaultStrings();

          Cursor.Current = Cursors.Default;
        }
      }
      else
      {
        if (Directory.Exists(tbStringsPath.Text))
          btnNewStrings.Enabled = true;
        tbStringsPath.ForeColor = System.Drawing.Color.Red;
      }
    }

    private void tbSolution_TextChanged(object sender, EventArgs e)
    {
      if (tbSolution.Text == string.Empty)
      {
        tabsModes.Controls[1].Enabled = false;
        tbSolution.ForeColor = System.Drawing.Color.Red;
      }

      if (tbSolution.Text != string.Empty && File.Exists(tbSolution.Text))
      {
        tabsModes.Controls[1].Enabled = true;
        tbSolution.ForeColor = System.Drawing.Color.Black;
      }
      else
      {
        tbSolution.ForeColor = System.Drawing.Color.Red;
      }
    }

    private void btnSaveSettings_Click(object sender, EventArgs e)
    {
      SaveConfig();
    }

    private void btnPath_Click(object sender, EventArgs e)
    {
      if (tbStringsPath.Text != string.Empty)
        folderBrowserDialog1.SelectedPath = tbStringsPath.Text;
      else
        folderBrowserDialog1.SelectedPath = Environment.CurrentDirectory;
      DialogResult result = folderBrowserDialog1.ShowDialog();
      if (result == DialogResult.OK)
      {
        tbStringsPath.Text = folderBrowserDialog1.SelectedPath;
      }
    }

    private void btnSkinPath_Click(object sender, EventArgs e)
    {
      if (tbSkinPath.Text != string.Empty)
        folderBrowserDialog1.SelectedPath = tbSkinPath.Text;
      else
        folderBrowserDialog1.SelectedPath = Environment.CurrentDirectory;
      DialogResult result = folderBrowserDialog1.ShowDialog();
      if (result == DialogResult.OK)
      {
        tbSkinPath.Text = folderBrowserDialog1.SelectedPath;
      }
    }

    private void btnSolution_Click(object sender, EventArgs e)
    {
      openFileDialog1.InitialDirectory = Environment.CurrentDirectory;
      DialogResult result = openFileDialog1.ShowDialog();
      if (result == DialogResult.OK)
      {
        tbSolution.Text = openFileDialog1.FileName;
      }
    }

    private void btnNewStrings_Click(object sender, EventArgs e)
    {
      _defaultStrings = new StringFile();
      _defaultStrings.languageName = "en";
      tabsModes.Controls[(int)Tabs.Create].Enabled = true;
      tabsModes.SelectedIndex = (int)Tabs.Create;
    }
    #endregion

    #region Translate Tab
    #region Tree
    private void treeSections_AfterSelect(object sender, TreeViewEventArgs e)
    {
      _lastSelected = 0;
      _lastFore = Color.FromName("WindowText");
      _lastBack = Color.FromName("Window");

      StoreEditList();

      _currentSection = treeSections.SelectedNode.Index;
      StringSection section = _defaultStrings.sections[_currentSection];
      listDefaultStrings.Items.Clear();
      foreach (StringLocalised defaultString in section.localisedStrings)
      {
        ListViewItem stringItem = new ListViewItem(defaultString.name);
        stringItem.SubItems.Add(defaultString.text);
        listDefaultStrings.Items.Add(stringItem);
      }
      SetTargetSection();
      BuildEditList();
      DrawTargetList();
    }

    private void treeSections_Leave(object sender, EventArgs e)
    {
      treeSections.Nodes[_currentSection].NodeFont = treeSections.Font;
    }

    private void treeSections_Enter(object sender, EventArgs e)
    {
      treeSections.Nodes[_currentSection].NodeFont = new Font(treeSections.Font, FontStyle.Regular);
    }

    private void treeSections_VisibleChanged(object sender, EventArgs e)
    {
      // Required because of bug in .NET
      foreach (TreeNode node in treeSections.Nodes)
      {
        if (node.NodeFont == null || (node.NodeFont.Bold && !node.IsSelected))
          node.NodeFont = new Font(treeSections.Font, FontStyle.Regular);
      }
    }
    #endregion

    private void cbLanguages_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (cbLanguages.SelectedIndex != 0 && cbLanguages.SelectedIndex != _currentLanguage)
      {
        if (CheckSave())
        {
          LanguageInfo info = _languageList[cbLanguages.SelectedIndex - 1];
          _targetStrings = LoadStrings(_stringPath, info.Name);

          BuildMissingList();
          ColourMissingSections();
          SetTargetSection();
          BuildEditList();
          DrawTargetList();
          btnSave.Enabled = false;
          _modifiedSection = false;
          _currentLanguage = cbLanguages.SelectedIndex;
        }
      }
      cbLanguages.SelectedIndex = _currentLanguage;
    }

    private void listTranslateStrings_AfterLabelEdit(object sender, LabelEditEventArgs e)
    {
      if (e.Label != null)
      {
        string name = listDefaultStrings.Items[e.Item].Text;
        if (_editList.ContainsKey(name))
        {
          _editList[name].text = e.Label;
        }
        else
        {
          StringLocalised newString = new StringLocalised();
          newString.name = name;
          newString.text = e.Label;
          _editList.Add(name, newString);
        }
        btnSave.Enabled = true;
        _modifiedSection = true;
        DrawTargetList();

        // Hi James - please add something like that but matching to your concept how it should work ;)
        //if (listTranslateStrings.Items[_lastSelected].Index == _editList.Count - 2)
        //{
        //  if (listDefaultStrings.Items.Count >= _editList.Count)
        //  {
        //    listTranslateStrings.Items[_editList.Count - 1].Text = listDefaultStrings.Items[_lastSelected + 1].Text;
        //    listTranslateStrings.Items[_lastSelected + 1].BeginEdit();
        //  }
        //}
      }
    }

    #region Buttons
    private void btnSave_Click(object sender, EventArgs e)
    {
      SaveTargetStrings();
    }

    private void btnNew_Click(object sender, EventArgs e)
    {
      if (CheckSave())
      {
        NewLanguageDialog newlanguage = new NewLanguageDialog(_languageList);
        if (newlanguage.ShowDialog() == DialogResult.OK)
        {
          _languageList.Add(newlanguage.Selected);
          DrawLanguageList(newlanguage.Selected.Name);
          _targetStrings = new StringFile();
          _targetStrings.languageName = newlanguage.Selected.Name;
          _targetStrings.sections = new List<StringSection>();
        }
      }
    }
    #endregion

    #region Synchronise List Selection
    // These events keep the selection synchronised between the two lists Default and Translate
    private void listDefaultStrings_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
    {
      if (listTranslateStrings.Items.Count > 0)
      {
        listTranslateStrings.TopItem = listTranslateStrings.Items[listDefaultStrings.TopItem.Index];
        listTranslateStrings.Items[_lastSelected].ForeColor = _lastFore;
        listTranslateStrings.Items[_lastSelected].BackColor = _lastBack;
        if (listDefaultStrings.SelectedItems.Count > 0)
        {
          _lastSelected = listDefaultStrings.SelectedItems[0].Index;
          _lastFore = listTranslateStrings.Items[_lastSelected].ForeColor;
          _lastBack = listTranslateStrings.Items[_lastSelected].BackColor;
          listTranslateStrings.Items[_lastSelected].ForeColor = Color.FromName("HighlightText");
          listTranslateStrings.Items[_lastSelected].BackColor = Color.FromName("Highlight");
        }
      }
    }

    private void listTranslateStrings_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
    {
      listDefaultStrings.TopItem = listDefaultStrings.Items[listTranslateStrings.TopItem.Index];
      listDefaultStrings.Items[_lastSelected].ForeColor = _lastFore;
      listDefaultStrings.Items[_lastSelected].BackColor = _lastBack;
      if (listTranslateStrings.SelectedItems.Count > 0)
      {
        _lastSelected = listTranslateStrings.SelectedItems[0].Index;
        _lastFore = listDefaultStrings.Items[_lastSelected].ForeColor;
        _lastBack = listDefaultStrings.Items[_lastSelected].BackColor;
        listDefaultStrings.Items[_lastSelected].ForeColor = System.Drawing.Color.FromName("HighlightText");
        listDefaultStrings.Items[_lastSelected].BackColor = System.Drawing.Color.FromName("Highlight");
      }
    }

    private void listDefaultStrings_Leave(object sender, EventArgs e)
    {
      if (listTranslateStrings.Items.Count > 0)
      {
        listTranslateStrings.Items[_lastSelected].ForeColor = _lastFore;
        listTranslateStrings.Items[_lastSelected].BackColor = _lastBack;
      }
    }

    private void listTranslateStrings_Leave(object sender, EventArgs e)
    {
      if (listDefaultStrings.Items.Count > 0)
      {
        listDefaultStrings.Items[_lastSelected].ForeColor = _lastFore;
        listDefaultStrings.Items[_lastSelected].BackColor = _lastBack;
      }
    }

    private void listDefaultStrings_Enter(object sender, EventArgs e)
    {
      if (listTranslateStrings.Items.Count > 0)
      {
        listTranslateStrings.Items[_lastSelected].ForeColor = System.Drawing.Color.FromName("HighlightText");
        listTranslateStrings.Items[_lastSelected].BackColor = System.Drawing.Color.FromName("Highlight");
      }
    }

    private void listTranslateStrings_Enter(object sender, EventArgs e)
    {
      if (listDefaultStrings.Items.Count > 0)
      {
        listDefaultStrings.Items[_lastSelected].ForeColor = System.Drawing.Color.FromName("HighlightText");
        listDefaultStrings.Items[_lastSelected].BackColor = System.Drawing.Color.FromName("Highlight");
      }
    }
    #endregion
    #endregion

    #region Manage Tab
    private void tvSections_VisibleChanged(object sender, EventArgs e)
    {
      // Require because of bug in .NET
      foreach (TreeNode node in tvSections.Nodes)
      {
        if (node.NodeFont == null || (node.NodeFont.Bold && !node.IsSelected))
          node.NodeFont = new Font(tvSections.Font, FontStyle.Regular);
      }
    }

    private void tvSections_Enter(object sender, EventArgs e)
    {
      tvSections.Nodes[_currentSection].NodeFont = new Font(tvSections.Font, FontStyle.Regular);
    }

    private void tvSections_Leave(object sender, EventArgs e)
    {
      tvSections.Nodes[_currentSection].NodeFont = tvSections.Font;
    }

    private void tvSections_AfterSelect(object sender, TreeViewEventArgs e)
    {
      _currentSection = tvSections.SelectedNode.Index;
      StringSection section = _defaultStrings.sections[_currentSection];
      lvStrings.Items.Clear();
      foreach (StringLocalised defaultString in section.localisedStrings)
      {
        ListViewItem stringItem = new ListViewItem(defaultString.name);
        stringItem.SubItems.Add(defaultString.text);
        lvStrings.Items.Add(stringItem);
      }
    }

    private void lvStrings_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (lvStrings.SelectedItems.Count > 0)
      {
        string stringId = _defaultStrings.sections[_currentSection].name + "." + lvStrings.SelectedItems[0].Text;

        if (_matchedStrings == null)
        {
          LoadSolution();
          LoadSkin();
        }

        lvMatches.Items.Clear();
        foreach (StringMatch stringMatch in _matchedStrings)
        {
          if (stringMatch.stringId == stringId)
          {
            ListViewItem stringItem = new ListViewItem(stringMatch.file);
            stringItem.SubItems.Add(stringMatch.project);
            stringItem.Group = lvMatches.Groups[(int)stringMatch.source];
            lvMatches.Items.Add(stringItem);
          }
        }
        if (lvMatches.Items.Count == 0)
        {
          tbTotal.ForeColor = System.Drawing.Color.Red;
          tbTotal.BackColor = Color.FromKnownColor(KnownColor.Window);
          tbTotal.Text = "0";
        }
        else
        {
          tbTotal.ForeColor = System.Drawing.Color.Black;
          tbTotal.Text = lvMatches.Items.Count.ToString();
        }

      }
    }
    #endregion

    #region Create Tab
    private void btnAddSection_Click(object sender, EventArgs e)
    {
      if (!_defaultStrings.IsSection(tbNewSection.Text))
      {
        StringSection section = new StringSection();
        section.name = tbNewSection.Text;
        section.isNew = true;
        _defaultStrings.sections.Add(section);
        tbNewSection.Text = string.Empty;
        btnSaveNewStrings.Enabled = true;
        DrawCreateSectionsTreeView();
      }
      else
      {
        MessageBox.Show("A Section with the same name already exists", "Section Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
      }
    }

    private void btnAddString_Click(object sender, EventArgs e)
    {
      if (tvCreateSections.SelectedNode != null)
      {
        if (!_defaultStrings.sections[tvCreateSections.SelectedNode.Index].IsString(tbNewString.Text))
        {
          StringLocalised str = new StringLocalised();
          str.name = tbNewString.Text;
          str.isNew = true;
          _defaultStrings.sections[tvCreateSections.SelectedNode.Index].localisedStrings.Add(str);
          tbNewString.Text = string.Empty;
          btnSaveNewStrings.Enabled = true;
        }
        else
        {
          MessageBox.Show("A String with the same name already exists", "String Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
      }
      else
      {
        MessageBox.Show("No Section selected", "String Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
      }
    }

    private void btnSaveNewStrings_Click(object sender, EventArgs e)
    {
      btnSaveNewStrings.Enabled = false;
    }

    private void tvCreateSections_VisibleChanged(object sender, EventArgs e)
    {
      // Required because of bug in .NET
      foreach (TreeNode node in tvCreateSections.Nodes)
      {
        if (node.NodeFont == null || (node.NodeFont.Bold && !node.IsSelected))
          node.NodeFont = new Font(treeSections.Font, FontStyle.Regular);
      }
    }
    #endregion
    #endregion
  }
}
