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

namespace MediaPortal.Tools.StringManager
{
  partial class StringManagerForm
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.tabsModes = new System.Windows.Forms.TabControl();
      this.tabCreate = new System.Windows.Forms.TabPage();
      this.btnSaveNewStrings = new System.Windows.Forms.Button();
      this.btnAddString = new System.Windows.Forms.Button();
      this.btnAddSection = new System.Windows.Forms.Button();
      this.tbNewString = new System.Windows.Forms.TextBox();
      this.tbNewSection = new System.Windows.Forms.TextBox();
      this.label4 = new System.Windows.Forms.Label();
      this.lvCreateStrings = new System.Windows.Forms.ListView();
      this.columnHeader9 = new System.Windows.Forms.ColumnHeader();
      this.columnHeader10 = new System.Windows.Forms.ColumnHeader();
      this.columnHeader11 = new System.Windows.Forms.ColumnHeader();
      this.label5 = new System.Windows.Forms.Label();
      this.tvCreateSections = new System.Windows.Forms.TreeView();
      this.tabTranslate = new System.Windows.Forms.TabPage();
      this.btnNew = new System.Windows.Forms.Button();
      this.btnSave = new System.Windows.Forms.Button();
      this.listTranslateStrings = new System.Windows.Forms.ListView();
      this.columnText2 = new System.Windows.Forms.ColumnHeader();
      this.cbLanguages = new System.Windows.Forms.ComboBox();
      this.lStrings = new System.Windows.Forms.Label();
      this.listDefaultStrings = new System.Windows.Forms.ListView();
      this.columnName = new System.Windows.Forms.ColumnHeader();
      this.columnText = new System.Windows.Forms.ColumnHeader();
      this.columnDescription = new System.Windows.Forms.ColumnHeader();
      this.lSections = new System.Windows.Forms.Label();
      this.treeSections = new System.Windows.Forms.TreeView();
      this.tabManage = new System.Windows.Forms.TabPage();
      this.lTotal = new System.Windows.Forms.Label();
      this.tbTotal = new System.Windows.Forms.MaskedTextBox();
      this.lMatches = new System.Windows.Forms.Label();
      this.lvMatches = new System.Windows.Forms.ListView();
      this.columnHeader8 = new System.Windows.Forms.ColumnHeader();
      this.columnHeader4 = new System.Windows.Forms.ColumnHeader();
      this.label1 = new System.Windows.Forms.Label();
      this.lvStrings = new System.Windows.Forms.ListView();
      this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
      this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
      this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
      this.label2 = new System.Windows.Forms.Label();
      this.tvSections = new System.Windows.Forms.TreeView();
      this.tabSettings = new System.Windows.Forms.TabPage();
      this.gbCreate = new System.Windows.Forms.GroupBox();
      this.label3 = new System.Windows.Forms.Label();
      this.btnNewStrings = new System.Windows.Forms.Button();
      this.btnSaveSettings = new System.Windows.Forms.Button();
      this.gbManage = new System.Windows.Forms.GroupBox();
      this.tbSolution = new System.Windows.Forms.MaskedTextBox();
      this.lSolution = new System.Windows.Forms.Label();
      this.btnSkinPath = new System.Windows.Forms.Button();
      this.btnSolution = new System.Windows.Forms.Button();
      this.lSkinPath = new System.Windows.Forms.Label();
      this.tbSkinPath = new System.Windows.Forms.MaskedTextBox();
      this.gbRequired = new System.Windows.Forms.GroupBox();
      this.tbStringsPath = new System.Windows.Forms.MaskedTextBox();
      this.lStringsPath = new System.Windows.Forms.Label();
      this.btnPath = new System.Windows.Forms.Button();
      this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
      this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
      this.columnHeader5 = new System.Windows.Forms.ColumnHeader();
      this.columnHeader6 = new System.Windows.Forms.ColumnHeader();
      this.columnHeader7 = new System.Windows.Forms.ColumnHeader();
      this.tabsModes.SuspendLayout();
      this.tabCreate.SuspendLayout();
      this.tabTranslate.SuspendLayout();
      this.tabManage.SuspendLayout();
      this.tabSettings.SuspendLayout();
      this.gbCreate.SuspendLayout();
      this.gbManage.SuspendLayout();
      this.gbRequired.SuspendLayout();
      this.SuspendLayout();
      // 
      // tabsModes
      // 
      this.tabsModes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.tabsModes.Controls.Add(this.tabCreate);
      this.tabsModes.Controls.Add(this.tabTranslate);
      this.tabsModes.Controls.Add(this.tabManage);
      this.tabsModes.Controls.Add(this.tabSettings);
      this.tabsModes.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.tabsModes.Location = new System.Drawing.Point(13, 13);
      this.tabsModes.Name = "tabsModes";
      this.tabsModes.SelectedIndex = 0;
      this.tabsModes.Size = new System.Drawing.Size(668, 464);
      this.tabsModes.TabIndex = 0;
      this.tabsModes.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabsModes_Selecting);
      this.tabsModes.TabIndexChanged += new System.EventHandler(this.tabsModes_TabIndexChanged);
      // 
      // tabCreate
      // 
      this.tabCreate.Controls.Add(this.btnSaveNewStrings);
      this.tabCreate.Controls.Add(this.btnAddString);
      this.tabCreate.Controls.Add(this.btnAddSection);
      this.tabCreate.Controls.Add(this.tbNewString);
      this.tabCreate.Controls.Add(this.tbNewSection);
      this.tabCreate.Controls.Add(this.label4);
      this.tabCreate.Controls.Add(this.lvCreateStrings);
      this.tabCreate.Controls.Add(this.label5);
      this.tabCreate.Controls.Add(this.tvCreateSections);
      this.tabCreate.Location = new System.Drawing.Point(4, 22);
      this.tabCreate.Name = "tabCreate";
      this.tabCreate.Padding = new System.Windows.Forms.Padding(3);
      this.tabCreate.Size = new System.Drawing.Size(660, 438);
      this.tabCreate.TabIndex = 3;
      this.tabCreate.Text = "Create";
      this.tabCreate.UseVisualStyleBackColor = true;
      // 
      // btnSaveNewStrings
      // 
      this.btnSaveNewStrings.Location = new System.Drawing.Point(579, 409);
      this.btnSaveNewStrings.Name = "btnSaveNewStrings";
      this.btnSaveNewStrings.Size = new System.Drawing.Size(75, 23);
      this.btnSaveNewStrings.TabIndex = 13;
      this.btnSaveNewStrings.Text = "Save";
      this.btnSaveNewStrings.UseVisualStyleBackColor = true;
      this.btnSaveNewStrings.Click += new System.EventHandler(this.btnSaveNewStrings_Click);
      // 
      // btnAddString
      // 
      this.btnAddString.Location = new System.Drawing.Point(337, 34);
      this.btnAddString.Name = "btnAddString";
      this.btnAddString.Size = new System.Drawing.Size(61, 20);
      this.btnAddString.TabIndex = 12;
      this.btnAddString.Text = "Add";
      this.btnAddString.UseVisualStyleBackColor = true;
      this.btnAddString.Click += new System.EventHandler(this.btnAddString_Click);
      // 
      // btnAddSection
      // 
      this.btnAddSection.Location = new System.Drawing.Point(134, 34);
      this.btnAddSection.Name = "btnAddSection";
      this.btnAddSection.Size = new System.Drawing.Size(58, 20);
      this.btnAddSection.TabIndex = 11;
      this.btnAddSection.Text = "Add";
      this.btnAddSection.UseVisualStyleBackColor = true;
      this.btnAddSection.Click += new System.EventHandler(this.btnAddSection_Click);
      // 
      // tbNewString
      // 
      this.tbNewString.Location = new System.Drawing.Point(212, 34);
      this.tbNewString.Name = "tbNewString";
      this.tbNewString.Size = new System.Drawing.Size(119, 20);
      this.tbNewString.TabIndex = 10;
      // 
      // tbNewSection
      // 
      this.tbNewSection.Location = new System.Drawing.Point(6, 34);
      this.tbNewSection.Name = "tbNewSection";
      this.tbNewSection.Size = new System.Drawing.Size(122, 20);
      this.tbNewSection.TabIndex = 8;
      // 
      // label4
      // 
      this.label4.AutoSize = true;
      this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.label4.Location = new System.Drawing.Point(209, 17);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(91, 13);
      this.label4.TabIndex = 7;
      this.label4.Text = "English Strings";
      // 
      // lvCreateStrings
      // 
      this.lvCreateStrings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)));
      this.lvCreateStrings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader9,
            this.columnHeader10,
            this.columnHeader11});
      this.lvCreateStrings.LabelEdit = true;
      this.lvCreateStrings.Location = new System.Drawing.Point(212, 60);
      this.lvCreateStrings.MultiSelect = false;
      this.lvCreateStrings.Name = "lvCreateStrings";
      this.lvCreateStrings.Size = new System.Drawing.Size(442, 343);
      this.lvCreateStrings.TabIndex = 6;
      this.lvCreateStrings.UseCompatibleStateImageBehavior = false;
      this.lvCreateStrings.View = System.Windows.Forms.View.Details;
      // 
      // columnHeader9
      // 
      this.columnHeader9.Text = "Name";
      this.columnHeader9.Width = 122;
      // 
      // columnHeader10
      // 
      this.columnHeader10.Text = "Text";
      this.columnHeader10.Width = 180;
      // 
      // columnHeader11
      // 
      this.columnHeader11.Text = "Description";
      this.columnHeader11.Width = 250;
      // 
      // label5
      // 
      this.label5.AutoSize = true;
      this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.label5.Location = new System.Drawing.Point(6, 17);
      this.label5.Name = "label5";
      this.label5.Size = new System.Drawing.Size(56, 13);
      this.label5.TabIndex = 5;
      this.label5.Text = "Sections";
      // 
      // tvCreateSections
      // 
      this.tvCreateSections.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)));
      this.tvCreateSections.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.tvCreateSections.FullRowSelect = true;
      this.tvCreateSections.Location = new System.Drawing.Point(6, 60);
      this.tvCreateSections.Name = "tvCreateSections";
      this.tvCreateSections.Size = new System.Drawing.Size(186, 343);
      this.tvCreateSections.TabIndex = 4;
      this.tvCreateSections.VisibleChanged += new System.EventHandler(this.tvCreateSections_VisibleChanged);
      // 
      // tabTranslate
      // 
      this.tabTranslate.Controls.Add(this.btnNew);
      this.tabTranslate.Controls.Add(this.btnSave);
      this.tabTranslate.Controls.Add(this.listTranslateStrings);
      this.tabTranslate.Controls.Add(this.cbLanguages);
      this.tabTranslate.Controls.Add(this.lStrings);
      this.tabTranslate.Controls.Add(this.listDefaultStrings);
      this.tabTranslate.Controls.Add(this.lSections);
      this.tabTranslate.Controls.Add(this.treeSections);
      this.tabTranslate.Location = new System.Drawing.Point(4, 22);
      this.tabTranslate.Name = "tabTranslate";
      this.tabTranslate.Padding = new System.Windows.Forms.Padding(3);
      this.tabTranslate.Size = new System.Drawing.Size(660, 438);
      this.tabTranslate.TabIndex = 0;
      this.tabTranslate.Text = "Translate";
      this.tabTranslate.UseVisualStyleBackColor = true;
      // 
      // btnNew
      // 
      this.btnNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnNew.Location = new System.Drawing.Point(520, 408);
      this.btnNew.Name = "btnNew";
      this.btnNew.Size = new System.Drawing.Size(47, 20);
      this.btnNew.TabIndex = 7;
      this.btnNew.Text = "New";
      this.btnNew.UseVisualStyleBackColor = true;
      this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
      // 
      // btnSave
      // 
      this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnSave.Location = new System.Drawing.Point(607, 408);
      this.btnSave.Name = "btnSave";
      this.btnSave.Size = new System.Drawing.Size(47, 20);
      this.btnSave.TabIndex = 6;
      this.btnSave.Text = "Save";
      this.btnSave.UseVisualStyleBackColor = true;
      this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
      // 
      // listTranslateStrings
      // 
      this.listTranslateStrings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.listTranslateStrings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnText2});
      this.listTranslateStrings.LabelEdit = true;
      this.listTranslateStrings.Location = new System.Drawing.Point(520, 32);
      this.listTranslateStrings.MultiSelect = false;
      this.listTranslateStrings.Name = "listTranslateStrings";
      this.listTranslateStrings.Size = new System.Drawing.Size(134, 370);
      this.listTranslateStrings.TabIndex = 5;
      this.listTranslateStrings.UseCompatibleStateImageBehavior = false;
      this.listTranslateStrings.View = System.Windows.Forms.View.Details;
      this.listTranslateStrings.Enter += new System.EventHandler(this.listTranslateStrings_Enter);
      this.listTranslateStrings.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listTranslateStrings_ItemSelectionChanged);
      this.listTranslateStrings.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.listTranslateStrings_AfterLabelEdit);
      this.listTranslateStrings.Leave += new System.EventHandler(this.listTranslateStrings_Leave);
      // 
      // columnText2
      // 
      this.columnText2.Text = "Text";
      this.columnText2.Width = 200;
      // 
      // cbLanguages
      // 
      this.cbLanguages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.cbLanguages.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cbLanguages.FormattingEnabled = true;
      this.cbLanguages.Location = new System.Drawing.Point(520, 8);
      this.cbLanguages.Name = "cbLanguages";
      this.cbLanguages.Size = new System.Drawing.Size(134, 21);
      this.cbLanguages.TabIndex = 4;
      this.cbLanguages.SelectedIndexChanged += new System.EventHandler(this.cbLanguages_SelectedIndexChanged);
      // 
      // lStrings
      // 
      this.lStrings.AutoSize = true;
      this.lStrings.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.lStrings.Location = new System.Drawing.Point(162, 16);
      this.lStrings.Name = "lStrings";
      this.lStrings.Size = new System.Drawing.Size(144, 13);
      this.lStrings.TabIndex = 3;
      this.lStrings.Text = "Default Strings (English)";
      // 
      // listDefaultStrings
      // 
      this.listDefaultStrings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)));
      this.listDefaultStrings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnName,
            this.columnText,
            this.columnDescription});
      this.listDefaultStrings.FullRowSelect = true;
      this.listDefaultStrings.Location = new System.Drawing.Point(165, 32);
      this.listDefaultStrings.MultiSelect = false;
      this.listDefaultStrings.Name = "listDefaultStrings";
      this.listDefaultStrings.Size = new System.Drawing.Size(349, 370);
      this.listDefaultStrings.TabIndex = 2;
      this.listDefaultStrings.UseCompatibleStateImageBehavior = false;
      this.listDefaultStrings.View = System.Windows.Forms.View.Details;
      this.listDefaultStrings.Enter += new System.EventHandler(this.listDefaultStrings_Enter);
      this.listDefaultStrings.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listDefaultStrings_ItemSelectionChanged);
      this.listDefaultStrings.Leave += new System.EventHandler(this.listDefaultStrings_Leave);
      // 
      // columnName
      // 
      this.columnName.Text = "Name";
      this.columnName.Width = 80;
      // 
      // columnText
      // 
      this.columnText.Text = "Text";
      this.columnText.Width = 180;
      // 
      // columnDescription
      // 
      this.columnDescription.Text = "Description";
      this.columnDescription.Width = 250;
      // 
      // lSections
      // 
      this.lSections.AutoSize = true;
      this.lSections.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.lSections.Location = new System.Drawing.Point(6, 16);
      this.lSections.Name = "lSections";
      this.lSections.Size = new System.Drawing.Size(56, 13);
      this.lSections.TabIndex = 1;
      this.lSections.Text = "Sections";
      // 
      // treeSections
      // 
      this.treeSections.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)));
      this.treeSections.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.treeSections.FullRowSelect = true;
      this.treeSections.Location = new System.Drawing.Point(6, 32);
      this.treeSections.Name = "treeSections";
      this.treeSections.Size = new System.Drawing.Size(153, 370);
      this.treeSections.TabIndex = 0;
      this.treeSections.Enter += new System.EventHandler(this.treeSections_Enter);
      this.treeSections.VisibleChanged += new System.EventHandler(this.treeSections_VisibleChanged);
      this.treeSections.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeSections_AfterSelect);
      this.treeSections.Leave += new System.EventHandler(this.treeSections_Leave);
      // 
      // tabManage
      // 
      this.tabManage.Controls.Add(this.lTotal);
      this.tabManage.Controls.Add(this.tbTotal);
      this.tabManage.Controls.Add(this.lMatches);
      this.tabManage.Controls.Add(this.lvMatches);
      this.tabManage.Controls.Add(this.label1);
      this.tabManage.Controls.Add(this.lvStrings);
      this.tabManage.Controls.Add(this.label2);
      this.tabManage.Controls.Add(this.tvSections);
      this.tabManage.ForeColor = System.Drawing.SystemColors.ControlText;
      this.tabManage.Location = new System.Drawing.Point(4, 22);
      this.tabManage.Name = "tabManage";
      this.tabManage.Padding = new System.Windows.Forms.Padding(3);
      this.tabManage.Size = new System.Drawing.Size(660, 438);
      this.tabManage.TabIndex = 1;
      this.tabManage.Text = "Manage";
      this.tabManage.UseVisualStyleBackColor = true;
      // 
      // lTotal
      // 
      this.lTotal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.lTotal.AutoSize = true;
      this.lTotal.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.lTotal.Location = new System.Drawing.Point(564, 411);
      this.lTotal.Name = "lTotal";
      this.lTotal.Size = new System.Drawing.Size(36, 13);
      this.lTotal.TabIndex = 17;
      this.lTotal.Text = "Total";
      // 
      // tbTotal
      // 
      this.tbTotal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.tbTotal.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.tbTotal.ForeColor = System.Drawing.SystemColors.WindowText;
      this.tbTotal.Location = new System.Drawing.Point(606, 408);
      this.tbTotal.Name = "tbTotal";
      this.tbTotal.ReadOnly = true;
      this.tbTotal.Size = new System.Drawing.Size(44, 20);
      this.tbTotal.TabIndex = 16;
      this.tbTotal.Text = "0";
      this.tbTotal.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
      // 
      // lMatches
      // 
      this.lMatches.AutoSize = true;
      this.lMatches.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.lMatches.Location = new System.Drawing.Point(421, 17);
      this.lMatches.Name = "lMatches";
      this.lMatches.Size = new System.Drawing.Size(55, 13);
      this.lMatches.TabIndex = 9;
      this.lMatches.Text = "Matches";
      // 
      // lvMatches
      // 
      this.lvMatches.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.lvMatches.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader8,
            this.columnHeader4});
      this.lvMatches.Location = new System.Drawing.Point(420, 33);
      this.lvMatches.Name = "lvMatches";
      this.lvMatches.Size = new System.Drawing.Size(230, 370);
      this.lvMatches.TabIndex = 8;
      this.lvMatches.UseCompatibleStateImageBehavior = false;
      this.lvMatches.View = System.Windows.Forms.View.Details;
      // 
      // columnHeader8
      // 
      this.columnHeader8.DisplayIndex = 1;
      this.columnHeader8.Text = "File";
      this.columnHeader8.Width = 180;
      // 
      // columnHeader4
      // 
      this.columnHeader4.DisplayIndex = 0;
      this.columnHeader4.Text = "Project";
      this.columnHeader4.Width = 69;
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.label1.Location = new System.Drawing.Point(163, 17);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(144, 13);
      this.label1.TabIndex = 7;
      this.label1.Text = "Default Strings (English)";
      // 
      // lvStrings
      // 
      this.lvStrings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)));
      this.lvStrings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
      this.lvStrings.FullRowSelect = true;
      this.lvStrings.Location = new System.Drawing.Point(166, 33);
      this.lvStrings.MultiSelect = false;
      this.lvStrings.Name = "lvStrings";
      this.lvStrings.Size = new System.Drawing.Size(249, 370);
      this.lvStrings.TabIndex = 6;
      this.lvStrings.UseCompatibleStateImageBehavior = false;
      this.lvStrings.View = System.Windows.Forms.View.Details;
      this.lvStrings.SelectedIndexChanged += new System.EventHandler(this.lvStrings_SelectedIndexChanged);
      // 
      // columnHeader1
      // 
      this.columnHeader1.Text = "Name";
      this.columnHeader1.Width = 80;
      // 
      // columnHeader2
      // 
      this.columnHeader2.Text = "Text";
      this.columnHeader2.Width = 180;
      // 
      // columnHeader3
      // 
      this.columnHeader3.Text = "Description";
      this.columnHeader3.Width = 250;
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.label2.Location = new System.Drawing.Point(7, 17);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(56, 13);
      this.label2.TabIndex = 5;
      this.label2.Text = "Sections";
      // 
      // tvSections
      // 
      this.tvSections.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)));
      this.tvSections.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.tvSections.FullRowSelect = true;
      this.tvSections.Location = new System.Drawing.Point(7, 33);
      this.tvSections.Name = "tvSections";
      this.tvSections.Size = new System.Drawing.Size(153, 370);
      this.tvSections.TabIndex = 4;
      this.tvSections.Enter += new System.EventHandler(this.tvSections_Enter);
      this.tvSections.VisibleChanged += new System.EventHandler(this.tvSections_VisibleChanged);
      this.tvSections.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvSections_AfterSelect);
      this.tvSections.Leave += new System.EventHandler(this.tvSections_Leave);
      // 
      // tabSettings
      // 
      this.tabSettings.Controls.Add(this.gbCreate);
      this.tabSettings.Controls.Add(this.btnSaveSettings);
      this.tabSettings.Controls.Add(this.gbManage);
      this.tabSettings.Controls.Add(this.gbRequired);
      this.tabSettings.Location = new System.Drawing.Point(4, 22);
      this.tabSettings.Name = "tabSettings";
      this.tabSettings.Padding = new System.Windows.Forms.Padding(3);
      this.tabSettings.Size = new System.Drawing.Size(660, 438);
      this.tabSettings.TabIndex = 2;
      this.tabSettings.Text = "Settings";
      this.tabSettings.UseVisualStyleBackColor = true;
      // 
      // gbCreate
      // 
      this.gbCreate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.gbCreate.Controls.Add(this.label3);
      this.gbCreate.Controls.Add(this.btnNewStrings);
      this.gbCreate.Location = new System.Drawing.Point(14, 98);
      this.gbCreate.Name = "gbCreate";
      this.gbCreate.Size = new System.Drawing.Size(629, 59);
      this.gbCreate.TabIndex = 12;
      this.gbCreate.TabStop = false;
      this.gbCreate.Text = "Create";
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(17, 26);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(124, 13);
      this.label3.TabIndex = 4;
      this.label3.Text = "Start New Language Set";
      // 
      // btnNewStrings
      // 
      this.btnNewStrings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnNewStrings.Enabled = false;
      this.btnNewStrings.Location = new System.Drawing.Point(154, 21);
      this.btnNewStrings.Name = "btnNewStrings";
      this.btnNewStrings.Size = new System.Drawing.Size(54, 23);
      this.btnNewStrings.TabIndex = 3;
      this.btnNewStrings.Text = "New";
      this.btnNewStrings.UseVisualStyleBackColor = true;
      this.btnNewStrings.Click += new System.EventHandler(this.btnNewStrings_Click);
      // 
      // btnSaveSettings
      // 
      this.btnSaveSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnSaveSettings.Location = new System.Drawing.Point(568, 409);
      this.btnSaveSettings.Name = "btnSaveSettings";
      this.btnSaveSettings.Size = new System.Drawing.Size(75, 23);
      this.btnSaveSettings.TabIndex = 11;
      this.btnSaveSettings.Text = "Save";
      this.btnSaveSettings.UseVisualStyleBackColor = true;
      this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);
      // 
      // gbManage
      // 
      this.gbManage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.gbManage.Controls.Add(this.tbSolution);
      this.gbManage.Controls.Add(this.lSolution);
      this.gbManage.Controls.Add(this.btnSkinPath);
      this.gbManage.Controls.Add(this.btnSolution);
      this.gbManage.Controls.Add(this.lSkinPath);
      this.gbManage.Controls.Add(this.tbSkinPath);
      this.gbManage.Location = new System.Drawing.Point(14, 163);
      this.gbManage.Name = "gbManage";
      this.gbManage.Size = new System.Drawing.Size(629, 108);
      this.gbManage.TabIndex = 10;
      this.gbManage.TabStop = false;
      this.gbManage.Text = "Manage";
      // 
      // tbSolution
      // 
      this.tbSolution.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.tbSolution.Location = new System.Drawing.Point(154, 33);
      this.tbSolution.Name = "tbSolution";
      this.tbSolution.Size = new System.Drawing.Size(396, 20);
      this.tbSolution.TabIndex = 3;
      this.tbSolution.TextChanged += new System.EventHandler(this.tbSolution_TextChanged);
      // 
      // lSolution
      // 
      this.lSolution.AutoSize = true;
      this.lSolution.Location = new System.Drawing.Point(17, 40);
      this.lSolution.Name = "lSolution";
      this.lSolution.Size = new System.Drawing.Size(45, 13);
      this.lSolution.TabIndex = 4;
      this.lSolution.Text = "Solution";
      // 
      // btnSkinPath
      // 
      this.btnSkinPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnSkinPath.Location = new System.Drawing.Point(581, 60);
      this.btnSkinPath.Name = "btnSkinPath";
      this.btnSkinPath.Size = new System.Drawing.Size(27, 23);
      this.btnSkinPath.TabIndex = 8;
      this.btnSkinPath.Text = "...";
      this.btnSkinPath.UseVisualStyleBackColor = true;
      this.btnSkinPath.Click += new System.EventHandler(this.btnSkinPath_Click);
      // 
      // btnSolution
      // 
      this.btnSolution.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnSolution.Location = new System.Drawing.Point(581, 34);
      this.btnSolution.Name = "btnSolution";
      this.btnSolution.Size = new System.Drawing.Size(27, 23);
      this.btnSolution.TabIndex = 5;
      this.btnSolution.Text = "...";
      this.btnSolution.UseVisualStyleBackColor = true;
      this.btnSolution.Click += new System.EventHandler(this.btnSolution_Click);
      // 
      // lSkinPath
      // 
      this.lSkinPath.AutoSize = true;
      this.lSkinPath.Location = new System.Drawing.Point(17, 65);
      this.lSkinPath.Name = "lSkinPath";
      this.lSkinPath.Size = new System.Drawing.Size(53, 13);
      this.lSkinPath.TabIndex = 7;
      this.lSkinPath.Text = "Skin Path";
      // 
      // tbSkinPath
      // 
      this.tbSkinPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.tbSkinPath.Location = new System.Drawing.Point(154, 63);
      this.tbSkinPath.Name = "tbSkinPath";
      this.tbSkinPath.Size = new System.Drawing.Size(396, 20);
      this.tbSkinPath.TabIndex = 6;
      // 
      // gbRequired
      // 
      this.gbRequired.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.gbRequired.Controls.Add(this.tbStringsPath);
      this.gbRequired.Controls.Add(this.lStringsPath);
      this.gbRequired.Controls.Add(this.btnPath);
      this.gbRequired.Location = new System.Drawing.Point(14, 6);
      this.gbRequired.Name = "gbRequired";
      this.gbRequired.Size = new System.Drawing.Size(629, 86);
      this.gbRequired.TabIndex = 9;
      this.gbRequired.TabStop = false;
      this.gbRequired.Text = "Required";
      // 
      // tbStringsPath
      // 
      this.tbStringsPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.tbStringsPath.Location = new System.Drawing.Point(154, 37);
      this.tbStringsPath.Name = "tbStringsPath";
      this.tbStringsPath.Size = new System.Drawing.Size(396, 20);
      this.tbStringsPath.TabIndex = 0;
      this.tbStringsPath.TextChanged += new System.EventHandler(this.tbStringsPath_TextChanged);
      // 
      // lStringsPath
      // 
      this.lStringsPath.AutoSize = true;
      this.lStringsPath.Location = new System.Drawing.Point(17, 40);
      this.lStringsPath.Name = "lStringsPath";
      this.lStringsPath.Size = new System.Drawing.Size(115, 13);
      this.lStringsPath.TabIndex = 1;
      this.lStringsPath.Text = "Language Strings Path";
      // 
      // btnPath
      // 
      this.btnPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnPath.Location = new System.Drawing.Point(581, 34);
      this.btnPath.Name = "btnPath";
      this.btnPath.Size = new System.Drawing.Size(27, 23);
      this.btnPath.TabIndex = 2;
      this.btnPath.Text = "...";
      this.btnPath.UseVisualStyleBackColor = true;
      this.btnPath.Click += new System.EventHandler(this.btnPath_Click);
      // 
      // folderBrowserDialog1
      // 
      this.folderBrowserDialog1.ShowNewFolderButton = false;
      // 
      // openFileDialog1
      // 
      this.openFileDialog1.FileName = "openFileDialog1";
      // 
      // columnHeader5
      // 
      this.columnHeader5.Text = "Name";
      // 
      // columnHeader6
      // 
      this.columnHeader6.Text = "Text";
      // 
      // columnHeader7
      // 
      this.columnHeader7.Text = "Description";
      // 
      // StringManagerForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(693, 489);
      this.Controls.Add(this.tabsModes);
      this.Name = "StringManagerForm";
      this.Text = "StringManager";
      this.tabsModes.ResumeLayout(false);
      this.tabCreate.ResumeLayout(false);
      this.tabCreate.PerformLayout();
      this.tabTranslate.ResumeLayout(false);
      this.tabTranslate.PerformLayout();
      this.tabManage.ResumeLayout(false);
      this.tabManage.PerformLayout();
      this.tabSettings.ResumeLayout(false);
      this.gbCreate.ResumeLayout(false);
      this.gbCreate.PerformLayout();
      this.gbManage.ResumeLayout(false);
      this.gbManage.PerformLayout();
      this.gbRequired.ResumeLayout(false);
      this.gbRequired.PerformLayout();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.TabControl tabsModes;
    private System.Windows.Forms.TabPage tabTranslate;
    private System.Windows.Forms.Label lSections;
    private System.Windows.Forms.TreeView treeSections;
    private System.Windows.Forms.TabPage tabManage;
    private System.Windows.Forms.ComboBox cbLanguages;
    private System.Windows.Forms.Label lStrings;
    private System.Windows.Forms.ListView listDefaultStrings;
    private System.Windows.Forms.ColumnHeader columnName;
    private System.Windows.Forms.ColumnHeader columnText;
    private System.Windows.Forms.ColumnHeader columnDescription;
    private System.Windows.Forms.ListView listTranslateStrings;
    private System.Windows.Forms.ColumnHeader columnText2;
    private System.Windows.Forms.TabPage tabSettings;
    private System.Windows.Forms.Button btnSkinPath;
    private System.Windows.Forms.Label lSkinPath;
    private System.Windows.Forms.MaskedTextBox tbSkinPath;
    private System.Windows.Forms.Button btnSolution;
    private System.Windows.Forms.Label lSolution;
    private System.Windows.Forms.MaskedTextBox tbSolution;
    private System.Windows.Forms.Button btnPath;
    private System.Windows.Forms.Label lStringsPath;
    private System.Windows.Forms.MaskedTextBox tbStringsPath;
    private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
    private System.Windows.Forms.OpenFileDialog openFileDialog1;
    private System.Windows.Forms.GroupBox gbManage;
    private System.Windows.Forms.GroupBox gbRequired;
    private System.Windows.Forms.ColumnHeader columnHeader5;
    private System.Windows.Forms.ColumnHeader columnHeader6;
    private System.Windows.Forms.ColumnHeader columnHeader7;
    private System.Windows.Forms.Button btnNew;
    private System.Windows.Forms.Button btnSave;
    private System.Windows.Forms.Button btnSaveSettings;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.ListView lvStrings;
    private System.Windows.Forms.ColumnHeader columnHeader1;
    private System.Windows.Forms.ColumnHeader columnHeader2;
    private System.Windows.Forms.ColumnHeader columnHeader3;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.TreeView tvSections;
    private System.Windows.Forms.Label lMatches;
    private System.Windows.Forms.ListView lvMatches;
    private System.Windows.Forms.ColumnHeader columnHeader8;
    private System.Windows.Forms.Label lTotal;
    private System.Windows.Forms.MaskedTextBox tbTotal;
    private System.Windows.Forms.ColumnHeader columnHeader4;
    private System.Windows.Forms.TabPage tabCreate;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Button btnNewStrings;
    private System.Windows.Forms.GroupBox gbCreate;
    private System.Windows.Forms.Button btnAddSection;
    private System.Windows.Forms.TextBox tbNewString;
    private System.Windows.Forms.TextBox tbNewSection;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.ListView lvCreateStrings;
    private System.Windows.Forms.ColumnHeader columnHeader9;
    private System.Windows.Forms.ColumnHeader columnHeader10;
    private System.Windows.Forms.ColumnHeader columnHeader11;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.TreeView tvCreateSections;
    private System.Windows.Forms.Button btnSaveNewStrings;
    private System.Windows.Forms.Button btnAddString;
  }
}

