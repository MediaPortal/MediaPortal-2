#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PathManager;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Configuration;
using MediaPortal.Configuration.Settings;

// System.Windows.Forms.Control gets mixed up with MediaPortal.Control
using FormControl = System.Windows.Forms.Control;


namespace MediaPortal.Manager
{

  public partial class SettingsControl : UserControl
  {

    #region Variables

    private bool _languageChange = false;
    private bool _rightToLeft = false;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of SettingsControl, which is used to dynamically build all configuration options.
    /// </summary>
    public SettingsControl()
    {
      InitializeComponent();

      // localise buttons
      StringId save = new StringId("configuration", "settings.button.save");
      this._btnSave.Tag = save;
      this._btnSave.Text = save.ToString();
      this._btnSave.Enabled = false;

      StringId apply = new StringId("configuration", "settings.button.apply");
      this._btnApply.Tag = apply;
      this._btnApply.Text = apply.ToString();
      this._btnApply.Enabled = false;

      _treeSections.ImageList = new ImageList();
      _treeSections.ImageList.ColorDepth = ColorDepth.Depth32Bit;
      _treeSections.ImageList.TransparentColor = Color.Transparent;
      _treeSections.ImageList.ImageSize = new Size(22, 22);

      LoadSections();

      ServiceScope.Get<ILocalisation>().LanguageChange += new LanguageChangeHandler(LanguageChange);
      CheckRightToLeft();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Helper for MainWindow_FormClosing event.
    /// </summary>
    /// <returns></returns>
    public bool Closing()
    {
      if (this._btnSave.Enabled)
      {
        StringId message = new StringId("configuration", "save_on_exit");
        StringId title = new StringId("configuration", "unsaved_warning");
        DialogResult result = MessageBox.Show(message.ToString(), title.ToString(), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

        switch (result)
        {
          case DialogResult.Yes:
            ServiceScope.Get<IConfigurationManager>().Save();
            break;
          case DialogResult.Cancel:
            return false;
        }
      }
      return true;
    }

    /// <summary>
    /// Selects the search field.
    /// </summary>
    public void FocusSearch()
    {
      this.Focus();
      _txtSearch.Select();
    }

    #endregion

    #region Private Methods

    #region Language

    private void LanguageChange(object o)
    {
      _languageChange = true;
      CheckRightToLeft();
    }

    /// <summary>
    /// Checks if the specified language is a right to left one and commits the necessairy actions.
    /// </summary>
    private void CheckRightToLeft()
    {
      _rightToLeft = ServiceScope.Get<ILocalisation>().CurrentCulture.TextInfo.IsRightToLeft;
      if (_rightToLeft)
      {

        base.RightToLeft = System.Windows.Forms.RightToLeft.Yes;

        this._treeSections.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
        this._treeSections.RightToLeftLayout = true;

        this._picSectionIcon.Location = new System.Drawing.Point(5, 5);
        this._lblSectionTitle.Size = new System.Drawing.Size(_sectionHeader.Size.Width - _picSectionIcon.Size.Width - 5, 16);
        this._lblSectionTitle.Location = new System.Drawing.Point(_picSectionIcon.Size.Width + 5, 22);
      }
      else
      {

        this.RightToLeft = System.Windows.Forms.RightToLeft.No;

        this._treeSections.RightToLeft = System.Windows.Forms.RightToLeft.No;
        this._treeSections.RightToLeftLayout = false;

        this._picSectionIcon.Location = new System.Drawing.Point(_sectionHeader.Size.Width - _picSectionIcon.Size.Width - 5, 5);
        this._lblSectionTitle.Size = new System.Drawing.Size(_sectionHeader.Size.Width - _picSectionIcon.Size.Width - 5, 16);
        this._lblSectionTitle.Location = new System.Drawing.Point(0, 22);
      }
    }

    #endregion

    #region Apply/Save

    /// <summary>
    /// Applies all settings.
    /// </summary>
    private void ApplyAll()
    {
      // Apply configuration
      ServiceScope.Get<IConfigurationManager>().Apply();
      // Apply language
      if (_languageChange)
      {
        // Update text on buttons
        if (this._btnSave.Tag is StringId)
          this._btnSave.Text = ((StringId)this._btnSave.Tag).ToString();

        if (this._btnApply.Tag is StringId)
          this._btnApply.Text = ((StringId)this._btnApply.Tag).ToString();
        // Update text in section tree,
        // all section details controls need to be redrawn.
        foreach (TreeNode node in _treeSections.Nodes)
        {
          if (node.Tag is SectionDetails)
          {
            SectionDetails details = (SectionDetails)node.Tag;
            node.Text = details.Section.Text.ToString();
            details.Designed = false;
          }
          foreach (TreeNode subnode in node.Nodes)
          {
            if (subnode.Tag is SectionDetails)
            {
              SectionDetails details = (SectionDetails)subnode.Tag;
              subnode.Text = details.Section.Text.ToString();
              details.Designed = false;
            }
          }
        }
        // Clear all help text -> needs to be localized again
        _help.RemoveAll();
        // Redraw currently selected section details
        DrawSettings();
        _languageChange = false;
      }
    }

    #endregion

    #region Treeview treeSections

    /// <summary>
    /// Loads all configuration data to the treeview "sections".
    /// </summary>
    private void LoadSections()
    {
      IConfigurationManager manager = ServiceScope.Get<IConfigurationManager>();
      IEnumerable<ConfigSection> sections = manager.GetSections();
      foreach (ConfigSection section in sections)
      {
        TreeNode node = CreateTreeNode(section);
        LoadSections(manager, section.Id, node.Nodes);
        _treeSections.Nodes.Add(node);
      }
    }

    /// <summary>
    /// Loads configuration data from a specified path for the ConfigurationTree to a TreeNodeCollection.
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="parentPath">Path for the ConfigurationTree.</param>
    /// <param name="destination">Collection to add nodes to.</param>
    private void LoadSections(IConfigurationManager manager, string parentPath, TreeNodeCollection destination)
    {
      IEnumerable<ConfigSection> sections = manager.GetSections(parentPath);
      foreach (ConfigSection section in sections)
      {
        TreeNode node = CreateTreeNode(section);
        LoadSections(manager, parentPath + "/" + section.Id, node.Nodes);
        destination.Add(node);
      }
    }

    /// <summary>
    /// Loads an enumeration of ConfigurationNodes to a TreeNodeCollection.
    /// </summary>
    /// <param name="nodes">Collection to get nodes from.</param>
    /// <param name="selectedNode"></param>
    /// <param name="destination">Collection to add nodes to.</param>
    private void LoadSections(IEnumerable<IConfigurationNode> nodes, IConfigurationNode selectedNode, TreeNodeCollection destination)
    {
      foreach (IConfigurationNode node in nodes)
      {
        if (node.Setting is ConfigSection)
        {
          TreeNode treeNode = CreateTreeNode((ConfigSection)node.Setting);
          LoadSections(node.Nodes, selectedNode, treeNode.Nodes);
          destination.Add(treeNode);
          if (treeNode.Parent != null)
            treeNode.Parent.Expand();
        }
      }
    }

    /// <summary>
    /// Returns a TreeNode created from a SettingBase.
    /// </summary>
    /// <param name="section">Setting to create TreeNode from.</param>
    /// <returns></returns>
    private TreeNode CreateTreeNode(ConfigSection section)
    {
      TreeNode node = new TreeNode();
      SectionDetails sectionTag = new SectionDetails();
      sectionTag.Section = section;
      node.Tag = sectionTag;
      node.Text = section.Text.ToString();
      node.ImageIndex = _treeSections.ImageList.Images.Count;
      node.SelectedImageIndex = _treeSections.ImageList.Images.Count;
      AddPNGToImageList(_treeSections.ImageList, section.IconSmall);
      return node;
    }

    /// <summary>
    /// Expands the section holding the specified IConfigurationNode.
    /// The expanded TreeNode is returned.
    /// </summary>
    /// <param name="configNode">IConfigurationNode to expand section from.</param>
    /// <param name="maySelect">May the section be selected?</param>
    /// <returns>The expanded TreeNode.</returns>
    private TreeNode ExpandNodeSection(IConfigurationNode configNode, bool maySelect)
    {
      configNode = (configNode.Setting is ConfigSection ? configNode : configNode.Section);
      TreeNodeCollection collection;  // Collection which will contain the node to select
      if (configNode.Section != null) // Find the parentnode and use its childnodes
        collection = ExpandNodeSection(configNode.Section, false).Nodes;
      else                            // Rootnode, so use the childnodes of the tree
        collection = _treeSections.Nodes;
      // Find the node in the collection
      foreach (TreeNode node in collection)
      {
        if (node.Text == configNode.Setting.Text.ToString())
        {
          node.Expand();
          TreeNode returnNode = node;
          if (returnNode == null)
            return new TreeNode();
          if (maySelect)
            _treeSections.SelectedNode = returnNode;
          return returnNode;
        }
      }
      return new TreeNode();  // Nothing found: return empty node
    }

    #endregion

    #region Panel panelSectionSettings

    /// <summary>
    /// Draws the setting to the panel.
    /// </summary>
    /// <seealso cref="BuildSettings"/>
    private void DrawSettings()
    {
      if (_treeSections.SelectedNode == null)
        return;
      _lblSectionTitle.Text = _treeSections.SelectedNode.Text;
      _picSectionIcon.Image = GetImage(((SectionDetails)_treeSections.SelectedNode.Tag).Section.IconLarge, ImageSize.L48);
      if (_treeSections.SelectedNode.Tag != null && _treeSections.SelectedNode.Tag is SectionDetails)
      {
        if (!((SectionDetails)_treeSections.SelectedNode.Tag).Designed                              // no settings built yet
          || ((SectionDetails)_treeSections.SelectedNode.Tag).RightToLeft != _rightToLeft           // rebuild to fit righ-to-left boolean
          || ((SectionDetails)_treeSections.SelectedNode.Tag).Width != _panelSectionSettings.Width) // rebuild to fit new width
        {
          BuildSettings(_treeSections.SelectedNode);
        }
        try
        {
          _panelSectionSettings.Controls.Clear();
          _panelSectionSettings.Controls.Add(((SectionDetails)_treeSections.SelectedNode.Tag).Control);
          _panelSectionSettings.Refresh();
        }
        catch (Win32Exception)  // No solution!
        {
          // Can throw exceptions when it can't handle the resize (too fast)
          //
          // ErrorCode: -2147467259
          // - CPU goes to 100%
          // - memory ussage is stable
          //
          // How to reproduce: just keep resizing the window real fast for about 20 seconds
          // can't show a messagebox without Vista closing this down first
        }
      }
      else
      {
        _lblSectionTitle.Text = "MediaPortal II";
        _panelSectionSettings.Controls.Clear();
      }
    }

    /// <summary>
    /// Builds the settings for a TreeNode and adds them to the Tag as SectionDetails.
    /// </summary>
    private void BuildSettings(TreeNode node)
    {
      if (node.Tag == null || !(node.Tag is SectionDetails))
        return;
      SectionDetails sectionTag = (SectionDetails)node.Tag;
      sectionTag.RightToLeft = _rightToLeft;
      sectionTag.Width = _panelSectionSettings.Width;
      // Create controls
      FormDesigner designer = new FormDesigner(GetSectionPath(node));
      designer.Help = this._help;
      designer.ConfigChangedHandler = new ConfigChangedEventHandler(UpdateConfigItem);
      designer.YesNoChange = new EventHandler(YesNoChange);
      designer.MultiSelectionListChange = new EventHandler(MultiSelectionListChange);
      designer.SingleSelectionListChange = new EventHandler(SingleSelectionListChange);
      designer.ListBoxSelectionChange = new EventHandler(ListSelectionChanged);
      designer.ButtonClick = new EventHandler(ButtonClicked);
      designer.EntryLeave = new EventHandler(EntryChange);
      designer.NumUpDownChanged = new EventHandler(NumUpDownChanged);
      sectionTag.Control = designer.BuildToPanel(_rightToLeft, _panelSectionSettings.Width);
      sectionTag.Designed = true;
      node.Tag = sectionTag;
    }

    /// <summary>
    /// Gets the TreeNode linked to the specified path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private TreeNode GetTreeNode(string path)
    {
      string[] loc = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
      TreeNode parent = null;
      TreeNodeCollection nodes = _treeSections.Nodes;
      foreach (string l in loc)
      {
        bool found = false;
        foreach (TreeNode node in nodes)
        {
          if (((SectionDetails)node.Tag).Section.Id == l)
          {
            parent = node;
            nodes = node.Nodes;
            found = true;
            break;
          }
        }
        if (!found) // Not found => next part of the path wasn't a section
          break;
      }
      return parent;
    }

    /// <summary>
    /// Gets the IConfigurationManager path for the IConfigurationNode related to the specified TreeNode.
    /// </summary>
    /// <param name="node"></param>
    private string GetSectionPath(TreeNode node)
    {
      if (node.Tag == null || !(node.Tag is SectionDetails))
        return "";
      SectionDetails sectionTag = (SectionDetails)node.Tag;
      if (sectionTag.Section == null)
        return "";
      StringBuilder location = new StringBuilder(sectionTag.Section.Id + "/");
      while (node.Parent != null
            && node.Parent.Tag != null
            && node.Parent.Tag is SectionDetails)
      {
        location.Insert(0, ((SectionDetails)node.Parent.Tag).Section.Id + "/");
        node = node.Parent;
      }
      return location.ToString();
    }

    /// <summary>
    /// Gets a control from the current panel.
    /// </summary>
    /// <exception cref="NullReferenceException">
    /// A NullReferenceException is thrown if the specified control can't be found.
    /// </exception>
    /// <param name="path">Path to the control, should be the same as IConfigurationNode.ToString()</param>
    /// <returns></returns>
    private FormControl GetFormControl(string path)
    {
      string[] location = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
      ControlCollection controls = _panelSectionSettings.Controls[0].Controls;
      foreach (string loc in location)
      {
        bool found = false;
        foreach (FormControl control in controls)
        {
          if (control.Tag != null && ((ConfigBase)control.Tag).Id == loc)
          {
            found = true;
            controls = control.Controls;
            break;
          }
        }
        if (!found)
          throw new NullReferenceException(String.Format("Can't find control \"{0}\" on panel \"{1}\"", path, _panelSectionSettings.Name));
      }
      return controls.Owner;
    }

    /// <summary>
    /// Focuses on the specified control.
    /// </summary>
    /// <param name="path">Path to the control, should be the same as IConfigurationNode.ToString()</param>
    private void FocusControl(string path)
    {
      string section = GetSectionPath(_treeSections.SelectedNode);
      if (section.Length >= path.Length)
        return;
      path = path.Substring(section.Length);
      FormControl control = GetFormControl(path);
      if (control.CanSelect)
        control.Select();
    }

    #endregion

    #region ImageResources

    /// <summary>
    /// Specifies the image size to get.
    /// </summary>
    private enum ImageSize
    {
      /// <summary>
      /// Undefined image.
      /// </summary>
      Undefined,
      /// <summary>
      /// Small image, 16x16 pixels.
      /// </summary>
      S16,
      /// <summary>
      /// Medium image, 22x22 pixels.
      /// </summary>
      M22,
      /// <summary>
      /// Large image, 48x48 pixels.
      /// </summary>
      L48
    }

    /// <summary>
    /// Gets an image. If image can't be found, a default is returned.
    /// </summary>
    /// <param name="location">Path to the image to get.</param>
    /// <param name="type">Type of image to get.</param>
    /// <returns>Requested image.</returns>
    private Image GetImage(string location, ImageSize type)
    {
      // Try to load the requested image
      if (System.IO.File.Exists(location))
      {
        try
        {
          return Image.FromFile(location);
        }
        catch (OutOfMemoryException ex)
        {
          ServiceScope.Get<ILogger>().Error("OutOfMemoryException while loading image: {0}", ex, location);
          return new Bitmap(1, 1);  // No use to try to load a default image -> out of memory
        }
      }
      else
      {
        ServiceScope.Get<ILogger>().Info("Image not found: {0}", location);
      }
      // Try to load a default image
      string path = ServiceScope.Get<IPathManager>().GetPath("<APPLICATION_ROOT>") + @"\Plugins\Configuration.Framework\Images\";
      try
      {
        switch (type)
        {
          case ImageSize.L48:
            return Image.FromFile(path + "default-48.png");
          case ImageSize.M22:
            return Image.FromFile(path + "default-22.png");
          case ImageSize.S16:
            return Image.FromFile(path + "default-16.png");
          default:
            return new Bitmap(1, 1);
        }
      }
      catch (Exception)
      {
        return new Bitmap(1, 1);
      }
    }

    #endregion

    #region ImageListBugFix

    [DllImport("comctl32.dll")]
    static extern bool ImageList_Add(IntPtr hImageList, IntPtr hBitmap, IntPtr hMask);
    [DllImport("kernel32.dll")]
    static extern bool RtlMoveMemory(IntPtr dest, IntPtr source, int dwcount);
    [DllImport("gdi32.dll")]
    static extern IntPtr CreateDIBSection(IntPtr hdc, [In, MarshalAs(UnmanagedType.LPStruct)] BitmapInfo pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

    [StructLayout(LayoutKind.Explicit)]
    private class BitmapInfo
    {
      [FieldOffset(0)]
      public Int32 biSize;
      [FieldOffset(4)]
      public Int32 biWidth;
      [FieldOffset(8)]
      public Int32 biHeight;
      [FieldOffset(12)]
      public Int16 biPlanes;
      [FieldOffset(14)]
      public Int16 biBitCount;
      [FieldOffset(16)]
      public Int32 biCompression;
      [FieldOffset(20)]
      public Int32 biSizeImage;
      [FieldOffset(24)]
      public Int32 biXPelsPerMeter;
      [FieldOffset(28)]
      public Int32 biYPelsPerMeter;
      [FieldOffset(32)]
      public Int32 biClrUsed;
      [FieldOffset(36)]
      public Int32 biClrImportant;
      [FieldOffset(40)]
      public Int32 colors;
    };

    private void AddPNGToImageList(ImageList imageList, string fileName)
    {
      Bitmap bitmap = new Bitmap(GetImage(fileName, ImageSize.M22));
      IntPtr hBitmap, ppvBits;
      BitmapInfo bitmapInfo = new BitmapInfo();
      bitmapInfo.biSize = 40;
      bitmapInfo.biBitCount = 32;
      bitmapInfo.biPlanes = 1;
      bitmapInfo.biWidth = bitmap.Width;
      bitmapInfo.biHeight = bitmap.Height;
      bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
      hBitmap = CreateDIBSection(new IntPtr(0), bitmapInfo, 0, out ppvBits, new IntPtr(0), 0);
      Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
      BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
      IntPtr pixels = bitmapData.Scan0;
      RtlMoveMemory(ppvBits, pixels, bitmap.Height * bitmapData.Stride);
      bitmap.UnlockBits(bitmapData);
      ImageList_Add(imageList.Handle, hBitmap, new IntPtr(0));
    }
    #endregion

    #endregion

    #region EventHandlers

    #region Static Controls

    #region Area

    private void sections_AfterSelect(object sender, TreeViewEventArgs e)
    {
      DrawSettings();
    }

    private void buttonSave_Click(object sender, EventArgs e)
    {
      ServiceScope.Get<IConfigurationManager>().Save();
      this._btnSave.Enabled = false;
    }

    private void buttonApply_Click(object sender, EventArgs e)
    {
      ApplyAll();
      this._btnApply.Enabled = false;
    }

    private void panelSectionSettings_SizeChanged(object sender, EventArgs e)
    {
      DrawSettings();
    }

    #endregion

    #region SearchField

    private void txtSearch_TextChanged(object sender, EventArgs e)
    {
      _treeSections.Nodes.Clear();
      if (_txtSearch.Text == "")
      {
        LoadSections();
        _txtSearch.Tag = null;
        _btnSearch.Tag = null;
      }
      else
      {
        IConfigurationNode bestNode;
        IEnumerable<IConfigurationNode> result = ServiceScope.Get<IConfigurationManager>().Search(_txtSearch.Text, out bestNode);
        _txtSearch.Tag = bestNode;
        _btnSearch.Tag = bestNode;
        LoadSections(result, bestNode, _treeSections.Nodes);
        if (bestNode != null)
          ExpandNodeSection(bestNode, true);
        else
        {
          _panelSectionSettings.Controls.Clear();  // Clear the old bestNode
          _lblSectionTitle.Text = "MediaPortal II";
        }
      }
    }

    private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
    {
      if ((int)e.KeyChar == 13)     // focus on the best match if user presses enter
        btnSearch_Click(sender, e);
    }

    private void btnSearch_Click(object sender, EventArgs e)
    {
      if (((FormControl)sender).Tag != null)
        FocusControl(((FormControl)sender).Tag.ToString());
    }

    #endregion

    #endregion

    #region Dynamic Controls - Settings

    private void UpdateConfigItem(ConfigBase sender, string senderLocation)
    {
      // Setting is on the visible page
      if (senderLocation.StartsWith(GetSectionPath(_treeSections.SelectedNode), true, new System.Globalization.CultureInfo("en")))
      {
        ((SectionDetails)_treeSections.SelectedNode.Tag).Designed = false;
        DrawSettings();
      }
      // Setting is on a different page
      else
      {
        ((SectionDetails)GetTreeNode(senderLocation).Tag).Designed = false;
      }
    }

    private void YesNoChange(object sender, EventArgs e)
    {
      if (sender == null) return;
      if (sender is CheckBox)
      {
        CheckBox chk = (CheckBox)sender;
        if (chk.Tag != null
          && chk.Tag is YesNo)
        {
          ((YesNo)chk.Tag).Yes = chk.Checked;
          this._btnSave.Enabled = true;
          this._btnApply.Enabled = true;
        }
      }
    }

    private void SingleSelectionListChange(object sender, EventArgs e)
    {
      if (sender == null) return;
      if (sender is ComboBox)
      {
        ComboBox cmb = (ComboBox)sender;
        if (cmb.Tag != null
          && cmb.Tag is SingleSelectionList)
        {
          ((SingleSelectionList)cmb.Tag).Selected = cmb.SelectedIndex;
          this._btnSave.Enabled = true;
          this._btnApply.Enabled = true;
        }
      }
      else if (sender is RadioButton)
      {
        RadioButton radio = (RadioButton)sender;
        if (radio.Enabled == true
          && radio.Tag != null
          && radio.Tag is int
          && radio.Parent != null
          && radio.Parent.Tag != null
          && radio.Parent.Tag is SingleSelectionList)
        {
          ((SingleSelectionList)radio.Parent.Tag).Selected = (int)radio.Tag;
          this._btnSave.Enabled = true;
          this._btnApply.Enabled = true;
        }
      }
    }

    private void MultiSelectionListChange(object sender, EventArgs e)
    {
      if (sender == null) return;
      if (sender is CheckedListBox)
      {
        CheckedListBox chk = (CheckedListBox)sender;
        if (chk.Tag != null
          && chk.Tag is MultipleSelectionList)
        {
          ((MultipleSelectionList)chk.Tag).SelectedIndices = (IList<int>)chk.SelectedIndices;
          this._btnSave.Enabled = true;
          this._btnApply.Enabled = true;
        }
      }
    }

    private void EntryChange(object sender, EventArgs e)
    {
      if (sender == null) return;
      if (sender is TextBox)
      {
        TextBox txt = (TextBox)sender;
        if (txt.Tag == null) return;
        if (txt.Tag is Entry)
        {
          ((Entry)txt.Tag).Value = txt.Text;
        }
        else if (txt.Tag is PathDetails)
        {
          ((PathDetails)txt.Tag).Path.SelectedPath = txt.Text;
        }
        else if (txt.Tag is MultipleEntryList)
        {
          MultipleEntryList list = (MultipleEntryList)txt.Tag;
          list.Lines.Clear();
          foreach (string s in txt.Lines)
            list.Lines.Add(s);
        }
        else
        {
          return; // don't enable the buttons
        }
        this._btnSave.Enabled = true;
        this._btnApply.Enabled = true;
      }
    }

    private void ListSelectionChanged(object sender, EventArgs e)
    {
      if (sender == null) return;
      if (sender is ListBox)
      {
        ListBox list = (ListBox)sender;
        // Handle a PreferenceList
        if (list.Tag != null && list.Tag is PreferenceListDetails)
        {
          PreferenceListDetails details = (PreferenceListDetails)list.Tag;
          details.ButtonUp.Enabled = (list.SelectedIndex > 0);
          details.ButtonDown.Enabled = (list.SelectedIndex != -1 && list.SelectedIndex != list.Items.Count - 1);
        }
      }
    }

    private void ButtonClicked(object sender, EventArgs e)
    {
      if (sender == null) return;
      if (sender is Button)
      {
        Button button = (Button)sender;
        if (button.Tag == null) return;
        // Handle a PreferenceList
        if (button.Tag is PreferenceListDetails)
        {
          PreferenceListDetails details = (PreferenceListDetails)button.Tag;
          if (details.ListBox.SelectedIndex != -1)
          {
            sbyte indexChange;
            if (button == details.ButtonUp)
              indexChange = -1; // one up
            else if (button == details.ButtonDown)
              indexChange = 1;  // one down
            else
              return;           // wrong button?
            int selectedIndex = details.ListBox.SelectedIndex;
            int itemIndex = details.PreferenceList.Ranking[selectedIndex];
            details.PreferenceList.Ranking.RemoveAt(selectedIndex);
            details.PreferenceList.Ranking.Insert(selectedIndex + indexChange, itemIndex);
            string itemString = details.ListBox.SelectedItem.ToString();
            details.ListBox.Items.RemoveAt(selectedIndex);
            details.ListBox.Items.Insert(selectedIndex + indexChange, itemString);
            details.ListBox.SelectedIndex = selectedIndex + indexChange;
            this._btnSave.Enabled = true;
            this._btnApply.Enabled = true;
          }
        }
        // Handle a Path
        else if (button.Tag is PathDetails)
        {
          PathDetails details = (PathDetails)button.Tag;
          switch (details.Path.SelectedPathType)
          {
            case Path.PathType.FILE:
              OpenFileDialog ofd = new OpenFileDialog();
              ofd.FileName = details.Path.SelectedPath;
              if (System.IO.File.Exists(ofd.FileName))
                ofd.InitialDirectory = System.IO.Path.GetDirectoryName(ofd.FileName);
              if (ofd.ShowDialog() == DialogResult.OK)
              {
                details.TextBox.Text = ofd.FileName;
                details.Path.SelectedPath = ofd.FileName;
                this._btnSave.Enabled = true;
                this._btnApply.Enabled = true;
              }
              break;
            case Path.PathType.FOLDER:
              FolderBrowserDialog fbd = new FolderBrowserDialog();
              fbd.SelectedPath = details.Path.SelectedPath;
              if (fbd.ShowDialog() == DialogResult.OK)
              {
                details.TextBox.Text = fbd.SelectedPath;
                details.Path.SelectedPath = fbd.SelectedPath;
                this._btnSave.Enabled = true;
                this._btnApply.Enabled = true;
              }
              break;
            default:
              return;
          }
        }
      }
    }

    private void NumUpDownChanged(object sender, EventArgs e)
    {
      if (sender == null) return;
      if (sender is NumericUpDown)
      {
        NumericUpDown num = (NumericUpDown)sender;
        if (num.Tag == null) return;
        // Handle a PreferenceList
        if (num.Tag is NumberSelect)
        {
          ((NumberSelect)num.Tag).Value = (double)num.Value;
          this._btnSave.Enabled = true;
          this._btnApply.Enabled = true;
        }
      }
    }

    #endregion

    #endregion

  }
}
