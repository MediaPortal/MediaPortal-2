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
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Configuration;
using MediaPortal.Configuration.Settings;


namespace MediaPortal.Manager
{
  /// <summary>
  /// Calculates the location of controls, based on an instance of ConfigBase.
  /// </summary>
  /// <remarks>
  /// Set the properties to link events to the constructed controls.
  /// </remarks>
  public class FormDesigner
  {
    #region Variables

    /// <summary>
    /// Location of the configuration in the ConfigurationTree.
    /// </summary>
    private string _configLocation;

    /// <summary>
    /// IConfigurationManager to load settings from.
    /// </summary>
    private IConfigurationManager _manager;
    
    /// <summary>
    /// ToolTip to add all help string to.
    /// Use SetHelp() to add help.
    /// </summary>
    private ToolTip _help;
    private ConfigChangedEventHandler _configChangedHandler;
    private EventHandler _yesnochange;
    private EventHandler _singleSelectionListChange;
    private EventHandler _multiSelectionListChange;
    private EventHandler _entryLeave;
    private EventHandler _listboxSelectionChanged; // can be used to disable/enable buttons
    private EventHandler _buttonClicked;
    private EventHandler _numUpDownChanged;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the ToolTip to add help text to.
    /// </summary>
    /// <remarks>
    /// The ToolTip should always be set with FormDesigner.SetHelp()
    /// </remarks>
    public ToolTip Help
    {
      get { return _help; }
      set { _help = value; }
    }

    /// <summary>
    /// Gets or sets the eventhandler for a configuration change.
    /// </summary>
    public ConfigChangedEventHandler ConfigChangedHandler
    {
      get { return _configChangedHandler; }
      set { _configChangedHandler = value; }
    }

    /// <summary>
    /// Gets or sets the EventHandler used to report YesNo changes.
    /// </summary>
    /// <remarks>
    /// Linked events:
    ///   CheckBox.CheckedChanged event
    /// 
    /// Possible CheckBox Tags:
    ///   YesNo
    /// </remarks>
    public EventHandler YesNoChange
    {
      get { return _yesnochange; }
      set { _yesnochange = value; }
    }

    /// <summary>
    /// Gets or sets the EventHandler used to report SingleSelectionList changes.
    /// </summary>
    /// <remarks>
    /// Linked events:
    ///   ComboBox.SelectedIndexChanged
    ///   RadioButton.CheckedChanged
    /// 
    /// Possible ComboBox Tags:
    ///   SingleSelectionList
    /// Possible RadioButton Tags:
    ///   int
    /// Possible RadioButton.Parent Tags:
    ///   SingleSelectionList
    /// </remarks>
    public EventHandler SingleSelectionListChange
    {
      get { return _singleSelectionListChange; }
      set { _singleSelectionListChange = value; }
    }

    /// <summary>
    /// Gets or sets the EventHandler used to report MultiSelectionList changes.
    /// (CheckedListBox.Click)
    /// </summary>
    /// <remarks>
    /// Linked events:
    ///   CheckedListBox.Click
    /// 
    /// Possible CheckedListBox Tags:
    ///   MultipleSelectionList
    /// </remarks>
    public EventHandler MultiSelectionListChange
    {
      get { return _multiSelectionListChange; }
      set { _multiSelectionListChange = value; }
    }

    /// <summary>
    /// Gets or sets the EventHandler used to report Entry changes.
    /// </summary>
    /// <remarks>
    /// Linked events:
    ///   TextBox.Leave
    /// 
    /// Possible TextBox Tags:
    ///   Entry
    /// </remarks>
    public EventHandler EntryLeave
    {
      get { return _entryLeave; }
      set { _entryLeave = value; }
    }

    /// <summary>
    /// Gets or sets the EventHandler used to report ListBoxSelection changes.
    /// A ListBox will always be part of a more advanced SettingType, check the Tag to get the linked data.
    /// </summary>
    /// <remarks>
    /// Linked events:
    ///   ListBox.SelectedIndexChanged
    /// 
    /// Possible ListBox Tags:
    ///   PreferenceListDetails
    /// </remarks>
    public EventHandler ListBoxSelectionChange
    {
      get { return _listboxSelectionChanged; }
      set { _listboxSelectionChanged = value; }
    }

    /// <summary>
    /// Gets or sets the EventHandler used to report a clicked button.
    /// </summary>
    /// <remarks>
    /// Linked events:
    ///   Button.Click
    /// 
    /// Possible Button Tags:
    ///   PreferenceListDetails
    /// </remarks>
    public EventHandler ButtonClick
    {
      get { return _buttonClicked; }
      set { _buttonClicked = value; }
    }

    /// <summary>
    /// Gets or sets the EventHandler used to report a change in a NumericUpDown.ValueChanged.
    /// </summary>
    /// <remarks>
    /// Linked events:
    ///   NumericUpDown
    /// </remarks>
    public EventHandler NumUpDownChanged
    {
      get { return _numUpDownChanged; }
      set { _numUpDownChanged = value; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of FormDesigner, which can be used to build an instance of SettingBase (and all its subsettings) to a Panel.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// An ArgumentException is thrown if the configLocation isn't a valid path.
    /// </exception>
    /// <param name="configLocation">Location of the configuration item in the ConfigurationTree.</param>
    public FormDesigner(string configLocation)
    {
      _manager = ServiceScope.Get<IConfigurationManager>();
      _configLocation = configLocation;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Builds the configuration items to a Panel.
    /// All built instances of SettingBase will be added to the property SubSettings.
    /// </summary>
    /// <param name="rightToLeft">Build for a right to left language?</param>
    /// <param name="width">Width of the panel to return.</param>
    /// <returns></returns>
    public Panel BuildToPanel(bool rightToLeft, int width)
    {
      return BuildToPanel(_manager.GetNode(_configLocation), new Position(rightToLeft, width));
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Builds the specified node to a Panel.
    /// </summary>
    /// <param name="node">Node to build to the Panel.</param>
    /// <param name="position">Defines the positioning parameters.</param>
    /// <returns></returns>
    private Panel BuildToPanel(IConfigurationNode node, Position position)
    {
      Panel panel = new Panel();
      panel.Tag = node == null ? null : node.ConfigObj;
      panel.AutoSize = false;
      panel.Height = 0;
      panel.Width = position.FullWidth;
      panel.Padding = new Padding(0, 0, 0, 0);
      panel.Margin = new Padding(0, 0, 0, 0);
      if (node == null || node.ConfigObj == null)
        return panel; // Return empty panel
      panel.Name = string.Format("{0}_{1}", node.Location, node.ConfigObj);
      // Add heading
      if (node.ConfigObj is ConfigGroup)
      {
        panel.Controls.Add(CreateHeading(position, node.ConfigObj.Text.Evaluate()));
        position.LinePosition += position.LineHeight;
      }
      // Add subcontrols
      foreach (IConfigurationNode subNode in node.ChildNodes)
      {
        if (subNode.ConfigObj.Hidden) continue;
        if (subNode.ConfigObj is ConfigGroup)
        {
          Position pos = (Position) position.Clone();
          pos.StartColumnOne += pos.Indent; // indent the first column
          pos.WidthColumnOne -= pos.Indent; // shorten the width, so it doesn't overlap with column two
          pos.LinePosition = 0; // reset linePosition, this is relative to the new control
          // Make a recursive call to process the group to a Panel
          Panel subPanel = BuildToPanel(subNode, pos);
          subPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
          subPanel.Location = new Point(0, position.LinePosition);
          subPanel.Enabled = !subNode.ConfigObj.Disabled;
          panel.Controls.Add(subPanel);
          position.LinePosition += pos.LinePosition;
        }
        else if (subNode.ConfigObj is ConfigSetting)
        {
          ConfigSetting setting = (ConfigSetting) subNode.ConfigObj;
          setting.OnChangeEvent += _configChangedHandler;
          if (setting is Entry)
          {
            panel.Controls.Add(CreateLabel(position, setting.Text.Evaluate()));
            TextBox txt = CreateTextBox(position, setting.Columns);
            txt.Text = ((Entry) setting).Value;
            txt.Tag = setting;
            SetHelp(txt, setting.Help.Evaluate());
            txt.Enabled = !setting.Disabled;
            panel.Controls.Add(txt);
            position.LinePosition += position.LineHeight + position.Margin;
          }
          else if (setting is LimitedNumberSelect)
          {
            int height;
            panel.Controls.Add(CreateLabel(position, setting.Text.Evaluate(), position.Width, out height));
            panel.Controls.Add(CreateLimitedNumberSelect(position, (LimitedNumberSelect) setting));
            position.LinePosition += height + position.Margin;
          }
          else if (setting is MultipleEntryList)
          {
            panel.Controls.Add(CreateLabel(position, setting.Text.Evaluate()));
            position.LinePosition += position.LineHeight;
            TextBox txt = CreateMultiLineTextBox(position, setting.Rows);
            txt.Tag = setting;
            MultipleEntryList entryList = (MultipleEntryList) setting;
            txt.Lines = new string[entryList.Lines.Count];
            for (int i = 0; i < txt.Lines.Length; i++)
              txt.Lines[i] = entryList.Lines[i];
            SetHelp(txt, setting.Help.Evaluate());
            txt.Enabled = !setting.Disabled;
            panel.Controls.Add(txt);
            position.LinePosition += txt.Height + position.Margin;
          }
          else if (setting is MultipleSelectionList)
          {
            int lblHeight;
            panel.Controls.Add(CreateLabel(position, setting.Text.Evaluate(), position.WidthColumnOne,
                                           out lblHeight));
            position.LinePosition += lblHeight + position.Margin;
            CheckedListBox chk = CreateCheckedListBox(position);
            MultipleSelectionList list = ((MultipleSelectionList) setting);
            for (int i = 0; i < list.Items.Count; i++)
              chk.Items.Add(list.Items[i], list.SelectedIndices.Contains(i));
            chk.Enabled = !setting.Disabled;
            panel.Controls.Add(chk);
            position.LinePosition += chk.Height + position.Margin;
          }
          else if (setting is NumberSelect)
          {
            int height;
            panel.Controls.Add(CreateLabel(position, setting.Text.Evaluate(), position.Width, out height));
            panel.Controls.Add(CreateNumberSelect(position, (NumberSelect) setting));
            position.LinePosition += height + position.Margin;
          }
          else if (setting is Path)
          {
            int height;
            panel.Controls.Add(CreateLabel(position, setting.Text.Evaluate(), position.Width, out height));
            position.LinePosition += height + position.Margin;
            Panel browse = CreateBrowseEntry(position, (Path) setting);
            panel.Controls.Add(browse);
            position.LinePosition += browse.Height + position.Margin;
          }
          else if (setting is PreferenceList)
          {
            int height;
            panel.Controls.Add(CreateLabel(position, setting.Text.Evaluate(), position.Width, out height));
            position.LinePosition += height + position.Margin;
            Panel list = CreatePreferenceList(position, (PreferenceList) setting);
            panel.Controls.Add(list);
            position.LinePosition += list.Height + position.Margin;
          }
          else if (setting is SingleSelectionList)
          {
            int lblHeight;
            panel.Controls.Add(CreateLabel(position, setting.Text.Evaluate(), position.WidthColumnOne,
                                           out lblHeight));
            lblHeight += position.Margin;
            if (((SingleSelectionList) setting).Items.Count > 3) // ComboBox
            {
              ComboBox cmb = CreateComboBox(position);
              foreach (IResourceString item in ((SingleSelectionList)setting).Items)
                cmb.Items.Add(item.Evaluate());
              cmb.SelectedIndex = ((SingleSelectionList) setting).Selected;
              cmb.Tag = setting;
              SetHelp(cmb, setting.Help.Evaluate());
              cmb.Enabled = !setting.Disabled;
              panel.Controls.Add(cmb);
              if (lblHeight > position.LineHeight)
                position.LinePosition += (position.ItemHeight*(lblHeight/(position.ItemHeight*2)));
              position.LinePosition += position.LineHeight;
            }
            else // 3 or less items:            Radiobuttons
            {
              Panel radioPanel = CreateRadioPanel(position, (SingleSelectionList) setting);
              panel.Enabled = !setting.Disabled;
              panel.Controls.Add(radioPanel);
              position.LinePosition += radioPanel.Height + position.Margin;
            }
          }
          else if (setting is YesNo)
          {
            int lblHeight;
            panel.Controls.Add(CreateLabel(position, setting.Text.Evaluate(), position.WidthColumnOne, out lblHeight));
            lblHeight += position.Margin;
            CheckBox chk = CreateCheckBox(position);
            chk.Checked = ((YesNo) setting).Yes;
            chk.Tag = setting;
            SetHelp(chk, setting.Help.Evaluate());
            chk.Enabled = !setting.Disabled;
            panel.Controls.Add(chk);
            if (lblHeight > position.LineHeight)
              position.LinePosition += (position.ItemHeight*(lblHeight/(position.ItemHeight*2)));
            position.LinePosition += position.LineHeight;
          }
        }
      }
      panel.Height = position.LinePosition;
      return panel;
    }

    /// <summary>
    /// Sets the help for a Control.
    /// </summary>
    /// <param name="owner">Control to set help to.</param>
    /// <param name="text">Text to display as tooltip.</param>
    private void SetHelp(System.Windows.Forms.Control owner, string text)
    {
      if (_help != null)
        _help.SetToolTip(owner, text);
    }

    #region Default Creators

    /// <summary>
    /// Returns a default heading.
    /// Headings are, by default, positioned in the first column.
    /// </summary>
    /// <param name="position">Position of the heading.</param>
    /// <param name="text">Text to display.</param>
    /// <returns></returns>
    private Label CreateHeading(Position position, string text)
    {
      Label heading = new Label();
      heading.Margin = new Padding(position.Margin, 3, position.Margin, 3);
      heading.AutoSize = false;
      heading.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
      heading.Location = new Point(position.StartColumnOne - position.Indent * 2, position.LinePosition);
      heading.Size = new Size(position.Width, position.ItemHeight);
      heading.TabIndex = position.NextTabIndex;
      heading.Name = "heading" + position.TabIndex.ToString();
      heading.Text = text;
      heading.TextAlign = ContentAlignment.TopLeft;
      return heading;
    }

    /// <summary>
    /// Returns a default heading.
    /// Headings are, by default, positioned in the first column.
    /// </summary>
    /// <param name="position">Position of the heading.</param>
    /// <param name="text">Text to display.</param>
    /// <param name="height">Expected height</param>
    /// <returns></returns>
    private Label CreateHeading(Position position, string text, out int height)
    {
      Label heading = CreateHeading(position, text);
      SizeF size = heading.CreateGraphics().MeasureString(heading.Text, heading.Font);
      height = (int)(size.Height * size.Width / position.WidthColumnOne);
      heading.AutoSize = true;
      heading.MaximumSize = new Size(position.Width, height + position.Margin);
      heading.MinimumSize = new Size(position.Width, height);
      return heading;
    }

    /// <summary>
    /// Returns a default Label.
    /// Labels are, by default, positioned in the first column.
    /// </summary>
    /// <param name="position">Position of the label.</param>
    /// <param name="text">Text to display.</param>
    /// <returns></returns>
    private Label CreateLabel(Position position, string text)
    {
      Label label = new Label();
      label.Location = new Point(position.StartColumnOne, position.LinePosition - 2);
      label.Margin = new Padding(position.Margin, 3, position.Margin, 3);
      label.TabIndex = position.NextTabIndex;
      label.Name = "label" + position.TabIndex.ToString();
      label.Text = text;
      label.AutoSize = false;
      label.Width = position.WidthColumnOne;
      return label;
    }

    /// <summary>
    /// Returns a default Label.
    /// Labels are, by default, positioned in the first column.
    /// </summary>
    /// <param name="position">Position of the label.</param>
    /// <param name="text">Text to display.</param>
    /// <param name="maxWidth">Maximum width of the label.</param>
    /// <param name="height">Expected height, based on the maximum width.</param>
    /// <returns></returns>
    private Label CreateLabel(Position position, string text, int maxWidth, out int height)
    {
      Label label = CreateLabel(position, text);
      // We'll have to guess the height...
      SizeF size = label.CreateGraphics().MeasureString(label.Text, label.Font);
      height = (int)(size.Height * (double)size.Width / maxWidth);
      label.AutoSize = true;
      label.MaximumSize = new Size(maxWidth, height + position.Margin);
      label.MinimumSize = new Size(maxWidth, height);
      return label;
    }

    /// <summary>
    /// Returns a default TextBox.
    /// Its position is based on both parameters.
    /// </summary>
    /// <param name="position">Position of the TextBox.</param>
    /// <param name="reqWidth">Requested width, as a number of characters.</param>
    /// <returns></returns>
    private TextBox CreateTextBox(Position position, int reqWidth)
    {
      TextBox txt = new TextBox();
      txt.Multiline = false;
      StringBuilder sb = new StringBuilder(reqWidth < 0 ? 0 : reqWidth);
      for (int i = 0; i < reqWidth; i++)
        sb.Append("o"); // take 'o' as the character to measure
      int width = (int)(txt.CreateGraphics().MeasureString(sb.ToString(), txt.Font).Width);
      if (width < position.WidthColumnTwo)
      {
        txt.Width = position.WidthColumnTwo;
        txt.Location = new Point(position.StartColumnTwo, position.LinePosition - 5);
      }
      else
      {
        txt.Width = position.Width;
        position.LinePosition += position.LineHeight;
        txt.Location = new Point(position.StartColumnOne + position.Margin, position.LinePosition);
      }
      txt.TabIndex = position.NextTabIndex;
      txt.Name = "textBox" + position.TabIndex.ToString();
      txt.Leave += _entryLeave;
      return txt;
    }

    /// <summary>
    /// Returns a default multiline TextBox.
    /// A multiline TextBox is, by default spread over the whole width.
    /// Use the Height property to avoid overlapping controls.
    /// </summary>
    /// <param name="position">Position of the TextBox.</param>
    /// <param name="reqHeight">Requested height, as a number of lines.</param>
    /// <returns></returns>
    private TextBox CreateMultiLineTextBox(Position position, int reqHeight)
    {
      TextBox txt = new TextBox();
      txt.Multiline = true;
      txt.Location = new Point(position.StartColumnOne + position.Margin, position.LinePosition);
      txt.Size = new Size(position.Width, (int)(position.LineHeight * 3.5));  // set the maximum size
      if (reqHeight > 0 && reqHeight * position.ItemHeight < txt.Height)      // alter height if less than maximum
        txt.Height = reqHeight * position.ItemHeight;
      txt.TabIndex = position.NextTabIndex;
      txt.Name = "multilineTextBox" + position.TabIndex.ToString();
      txt.Leave += _entryLeave;
      return txt;
    }

    /// <summary>
    /// Returns a default CheckBox. This is a 15x14 CheckBox without any text.
    /// Checkboxes are, by default, positioned in the second column.
    /// </summary>
    /// <param name="position">Position of the checkbox.</param>
    /// <returns></returns>
    private CheckBox CreateCheckBox(Position position)
    {
      CheckBox chk = new CheckBox();
      chk.AutoSize = true;
      chk.Location = new Point(position.StartColumnTwo, position.LinePosition - 2);
      chk.Size = new Size(15, 14);
      chk.TabIndex = position.NextTabIndex;
      chk.Name = "checkBox" + position.TabIndex.ToString();
      chk.UseVisualStyleBackColor = true;
      chk.CheckedChanged += _yesnochange;
      return chk;
    }

    /// <summary>
    /// Returns a default ComboBox.
    /// ComboBoxes are, by default, fitted in the second column.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private ComboBox CreateComboBox(Position position)
    {
      ComboBox cmb = new ComboBox();
      cmb.DropDownStyle = ComboBoxStyle.DropDownList;
      cmb.Location = new Point(position.StartColumnTwo, position.LinePosition - 5);
      cmb.Size = new Size(position.WidthColumnTwo, 21);
      cmb.TabIndex = position.NextTabIndex;
      cmb.Name = "comboBox" + position.TabIndex.ToString();
      cmb.SelectedIndexChanged += _singleSelectionListChange;
      return cmb;
    }

    /// <summary>
    /// Returns a default CheckedListBox.
    /// A CheckedListBox is, by default, spread over the whole width.
    /// Use the Height property to avoid overlapping controls.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private CheckedListBox CreateCheckedListBox(Position position)
    {
      CheckedListBox chk = new CheckedListBox();
      //chk.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right)));
      chk.CheckOnClick = true;
      chk.FormattingEnabled = false;
      chk.Location = new Point(position.StartColumnOne + position.Margin, position.LinePosition);
      chk.Size = new Size(position.Width, (int)(position.LineHeight * 3.5));
      chk.TabIndex = position.NextTabIndex;
      chk.Name = "checkedListBox" + position.TabIndex.ToString();
      chk.Click += _multiSelectionListChange;
      return chk;
    }

    /// <summary>
    /// Returns a default radiopanel.
    /// This panel will try to fit into the second column,
    /// if not it will take a new line and spread over the whole width.
    /// </summary>
    /// <param name="position">Position of the Panel.</param>
    /// <param name="tag">Tag for the panel, containing data about the RadioButtons.</param>
    /// <returns></returns>
    private Panel CreateRadioPanel(Position position, SingleSelectionList tag)
    {
      Panel panel = new Panel();
      panel.AutoSize = false;
      panel.Tag = tag;
      panel.TabIndex = position.NextTabIndex;
      panel.Name = "radioPanel" + position.TabIndex.ToString();
      bool takeNewLine = false; // needed to know the columnwidth when calculating the expected height
      List<RadioButton> items = new List<RadioButton>(tag.Items.Count);
      for (int i = 0; i < tag.Items.Count; i++)
      {
        RadioButton btn = new RadioButton();
        btn.AutoSize = false;
        btn.TabIndex = position.NextTabIndex;
        btn.Name = "radioButton" + position.TabIndex.ToString();
        btn.Text = tag.Items[i].Evaluate();
        btn.CheckAlign = ContentAlignment.TopLeft;
        btn.Checked = (i == tag.Selected);
        btn.CheckedChanged += _singleSelectionListChange;
        btn.Tag = i;
        SetHelp(btn, tag.Help.Evaluate());
        items.Add(btn);
        // see if we should take a new line (add 30 for the radiobutton and to cover too small measurements)
        btn.Width = (int)(btn.CreateGraphics().MeasureString(btn.Text, btn.Font).Width + 30);
        if (btn.Width > position.WidthColumnTwo)
          takeNewLine = true;
      }
      // we can't calculate the height before, because we don't know the available width (depends on takeNewLine)
      // => second loop is needed
      Position nPos = (Position)position.Clone();
      nPos.LinePosition = 0;
      int width; // values depend on the new line, we can't use Position
      if (takeNewLine)
      {
        width = position.Width - position.Margin;
      }
      else
      {
        width = position.WidthColumnTwo;
      }
      foreach (RadioButton btn in items)
      {
        btn.Location = new Point(0, nPos.LinePosition);
        if (btn.Width > width)  // doesn't fit in the column, we'll have to calculate the height to avoid overlapping controls
        {
          btn.MaximumSize = new Size(width, (int)((double)btn.Width / width * btn.Height + position.LineHeight - btn.Height));
          btn.MinimumSize = btn.MaximumSize;
          btn.AutoSize = true;
          nPos.LinePosition += btn.MaximumSize.Height;
        }
        else
          nPos.LinePosition += nPos.LineHeight;
        panel.Controls.Add(btn);
      }
      panel.Size = new Size(width, nPos.LinePosition);
      // Locate and return the panel
      if (!takeNewLine)
        panel.Location = new Point(position.StartColumnTwo, position.LinePosition);
      else
      {
        position.LinePosition += position.ItemHeight;
        panel.Location = new Point(position.StartColumnOne + position.Margin, position.LinePosition);
      }
      return panel;
    }

    /// <summary>
    /// Creates a default PreferenceList.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    private Panel CreatePreferenceList(Position position, PreferenceList tag)
    {
      // List containing the items
      ListBox list = new ListBox();
      list.TabIndex = position.NextTabIndex;
      list.Name = "preferenceList" + position.TabIndex.ToString();
      //list.Size = new Size(position.Width - position.Margin, position.LineHeight * 4);
      list.Size = new Size(position.Width, position.LineHeight * 4);
      list.Location = new Point(0, 0);
      foreach (string item in tag.SortedItems)
        list.Items.Add(item);
      // Button to move items up
      Button up = new Button();
      up.TabIndex = position.NextTabIndex;
      up.Name = "preferenceListUp" + position.TabIndex.ToString();
      up.Location = new Point(position.Margin, list.Height);
      up.Size = new Size((int)(list.Width / 2 - position.Margin * 1.5), position.LineHeight);
      up.Text = "+";
      up.Enabled = false;
      // Button to move items down
      Button down = new Button();
      down.TabIndex = position.NextTabIndex;
      down.Name = "preferenceListDown" + position.TabIndex.ToString();
      down.Location = new Point(up.Location.X + up.Size.Width + position.Margin, up.Location.Y);
      down.Size = up.Size;
      down.Text = "-";
      down.Enabled = false;
      // Add events
      list.SelectedIndexChanged += _listboxSelectionChanged;
      up.Click += _buttonClicked;
      down.Click += _buttonClicked;
      // Create PreferenceListDetails
      PreferenceListDetails details = new PreferenceListDetails(tag, list, up, down);
      // Set tags
      list.Tag = details;
      up.Tag = details;
      down.Tag = details;
      // Create and return panel
      Panel panel = details.GetAsPanel(new Size(list.Width, position.LineHeight * 5), new Point(position.StartColumnOne, position.LinePosition));
      panel.TabIndex = position.NextTabIndex;
      panel.Name = "preferenceListHolder" + position.TabIndex;
      return panel;
    }

    /// <summary>
    /// Creates a default Path.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    private Panel CreateBrowseEntry(Position position, Path tag)
    {
      // TextBox
      TextBox txt = new TextBox();
      txt.TabIndex = position.NextTabIndex;
      txt.Name = "pathTextBox" + position.TabIndex.ToString();
      txt.Width = position.WidthColumnOne - position.Margin;
      txt.Location = new Point(0, 2); // position this 2 pixels lower than the button
      txt.Text = tag.SelectedPath;
      // Button
      Button btn = new Button();
      btn.TabIndex = position.NextTabIndex;
      btn.Name = "pathButton" + position.TabIndex.ToString();
      btn.Width = position.WidthColumnTwo;
      btn.Location = new Point(position.StartColumnTwo - position.StartColumnOne, 0); // needs to be relative to the parent
      btn.Text = LocalizationHelper.CreateLabelProperty("[configuration.browse]").Evaluate();
      // Add events
      txt.Leave += _entryLeave;
      btn.Click += _buttonClicked;
      // Create PathDetails
      PathDetails details = new PathDetails(tag, txt, btn);
      // Set tags
      txt.Tag = details;
      btn.Tag = details;
      // Create and return panel
      Panel panel = details.GetAsPanel(new Size(position.Width, position.LineHeight), new Point(position.StartColumnOne, position.LinePosition));
      panel.TabIndex = position.NextTabIndex;
      panel.Name = "pathHolder" + position.TabIndex;
      panel.Margin = new Padding(0);
      return panel;
    }

    /// <summary>
    /// Creates a number select.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    private NumericUpDown CreateNumberSelect(Position position, NumberSelect tag)
    {
      NumericUpDown num = new NumericUpDown();
      num.Tag = tag;
      num.TabIndex = position.NextTabIndex;
      num.Name = "numberSelect" + position.TabIndex.ToString();
      num.Width = position.WidthColumnTwo;
      num.Location = new Point(position.StartColumnTwo, position.LinePosition);
      if (tag.ValueType != NumberSelect.NumberType.FixedPoint)
      {
        num.DecimalPlaces = 3;
        num.Increment = (decimal)0.001;
      }
      num.Maximum = 999999999999999;
      num.Minimum = -999999999999999;
      num.Value = (decimal)tag.Value;
      num.ValueChanged += _numUpDownChanged;
      return num;
    }

    /// <summary>
    /// Creates a default number select.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    private NumericUpDown CreateLimitedNumberSelect(Position position, LimitedNumberSelect tag)
    {
      NumericUpDown num = CreateNumberSelect(position, tag);
      num.Maximum = (decimal)tag.UpperLimit;
      num.Minimum = (decimal)tag.LowerLimit;
      return num;
    }

    #endregion

    #endregion
  }
}
