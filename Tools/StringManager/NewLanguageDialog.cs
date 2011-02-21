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
using System.Globalization;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MediaPortal.Tools.StringManager
{
  public partial class NewLanguageDialog : Form
  {
    private Dictionary<string, LanguageInfo> _existingLanguages;
    private List<LanguageInfo> _languages;
    private LanguageInfo _selected;

    public NewLanguageDialog(List<LanguageInfo> existingLanguages)
    {
      InitializeComponent();

      _existingLanguages = new Dictionary<string,LanguageInfo>();
      _existingLanguages.Add("en", null);
      foreach (LanguageInfo language in existingLanguages)
      {
        _existingLanguages.Add(language.Name, language);
      }

      LoadLanguages();
    }

    public LanguageInfo Selected
    {
      get { return _selected; }
    }

    private void LoadLanguages()
    {
     CultureTypes type;
      if(cbRegional.Checked)
        type = CultureTypes.AllCultures;
      else
        type = CultureTypes.NeutralCultures;

      _languages = new List<LanguageInfo>();
      foreach (CultureInfo culture in CultureInfo.GetCultures(type))
      {
        if (!_existingLanguages.ContainsKey(culture.Name))
        {
          LanguageInfo info = new LanguageInfo(culture);
          _languages.Add(info);
        }
      }
      
      _languages.Sort();
      lbLanguageList.Items.Clear();
      foreach (LanguageInfo info in _languages)
      {
        lbLanguageList.Items.Add(info.ToString());
      }
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
      this.DialogResult = DialogResult.Cancel;
    }

    private void btnOk_Click(object sender, EventArgs e)
    {
      if (lbLanguageList.SelectedItem == null)
      {
        this.DialogResult = DialogResult.Cancel;
      }
      else
      {
        _selected = _languages[lbLanguageList.SelectedIndex];
        this.DialogResult = DialogResult.OK;
      }
    }

    private void cbRegional_CheckStateChanged(object sender, EventArgs e)
    {
      LoadLanguages();
    }
  }
}
