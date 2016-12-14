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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml;
using System.Xml.XPath;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SkinDesigner
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow
  {
    public const string NS_MEDIAPORTAL_MPF_URI = "www.team-mediaportal.com/2008/mpf/directx";
    public const string NS_XAML_URI = "http://schemas.microsoft.com/winfx/2006/xaml";

    public static XmlWriterSettings DEFAULT_XML_WRITER_SETTINGS = new XmlWriterSettings
      {
          NewLineHandling = NewLineHandling.Entitize,
          CheckCharacters = false,
          Encoding = Encoding.UTF8,
          Indent = true,
          CloseOutput = true,
      };

    public static XmlReaderSettings DEFAULT_XML_READER_SETTINGS = new XmlReaderSettings
      {
          CloseInput = true,
          IgnoreComments = false,
          IgnoreProcessingInstructions = false,
          IgnoreWhitespace = false,
      };

    public class ColorResourceFileDescriptor
    {
      protected string _fileName;
      protected XmlDocument _colorResource;
      protected IDictionary<string, Color> _colors = new Dictionary<string, Color>(100);
      protected IDictionary<string, Color> _modifiedColors = new Dictionary<string, Color>(100);

      public ColorResourceFileDescriptor(string colorResourceFileName) : this(colorResourceFileName, Load(colorResourceFileName)) { }

      public ColorResourceFileDescriptor(string fileName, XmlDocument colorResource)
      {
        _fileName = fileName;
        _colorResource = colorResource;
        ReadXML();
      }

      protected static XmlDocument Load(string fileName)
      {
        Log("Reading color file '{0}'", fileName);
        using (FileStream stream = new FileStream(fileName, FileMode.Open))
        using (XmlReader reader = XmlReader.Create(stream, DEFAULT_XML_READER_SETTINGS))
        {
          XmlDocument doc = new XmlDocument {PreserveWhitespace = true};
          doc.Load(reader);
          return doc;
        }
      }

      public void Save()
      {
        WriteXML();
        Log("Saving color file '{0}'", _fileName);
        using (FileStream stream = new FileStream(_fileName, FileMode.Create))
        using (XmlWriter writer = XmlWriter.Create(stream, DEFAULT_XML_WRITER_SETTINGS))
          _colorResource.WriteTo(writer);
      }

      public XmlDocument ColorResource
      {
        get { return _colorResource; }
      }

      public IDictionary<string, Color> Colors
      {
        get { return _colors; }
      }

      public bool IsDirty
      {
        get { return _modifiedColors.Count > 0; }
      }

      public string ThemeName
      {
        get { return Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(_fileName))); }
      }

      protected void ReadXML()
      {
        XPathNavigator rootNavigator = _colorResource.CreateNavigator();
        XmlNameTable nameTable = rootNavigator.NameTable;
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(nameTable);
        nsmgr.AddNamespace("mpf", NS_MEDIAPORTAL_MPF_URI);
        nsmgr.AddNamespace("x", NS_XAML_URI);
        TypeConverter tc = TypeDescriptor.GetConverter(typeof(Color));
        if (tc == null)
          return;
        for (XPathNodeIterator resourcesIterator = rootNavigator.Select("mpf:ResourceDictionary/mpf:ResourceWrapper", nsmgr); resourcesIterator.MoveNext(); )
        {
          XPathNavigator currentResource = resourcesIterator.Current;
          if (currentResource == null)
            return;
          string key = currentResource.GetAttribute("Key", NS_XAML_URI);
          string value = currentResource.GetAttribute("Resource", string.Empty);
          if (string.IsNullOrEmpty(value))
            value = currentResource.GetAttribute("Value", string.Empty);
          try
          {
            Color? c = (Color?) tc.ConvertFromString(value);
            if (c.HasValue)
              _colors[key] = c.Value;
          } catch (Exception) {}
        }
        _modifiedColors.Clear();
        Log("{0} colors read from XML document", _colors.Count);
      }

      protected void WriteXML()
      {
        XPathNavigator rootNavigator = _colorResource.CreateNavigator();
        XmlNameTable nameTable = rootNavigator.NameTable;
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(nameTable);
        nsmgr.AddNamespace("mpf", NS_MEDIAPORTAL_MPF_URI);
        nsmgr.AddNamespace("x", NS_XAML_URI);
        TypeConverter tc = TypeDescriptor.GetConverter(typeof(Color));
        if (tc == null)
          return;
        for (XPathNodeIterator resourcesIterator = rootNavigator.Select("mpf:ResourceDictionary/mpf:ResourceWrapper", nsmgr); resourcesIterator.MoveNext(); )
        {
          XPathNavigator currentResource = resourcesIterator.Current;
          if (currentResource == null)
            return;
          string key = currentResource.GetAttribute("Key", NS_XAML_URI);
            Color newColor;
          if (_modifiedColors.TryGetValue(key, out newColor))
          {
            XmlElement colorResource = (XmlElement) currentResource.UnderlyingObject;
            if (colorResource == null)
              continue;
            if (!string.IsNullOrEmpty(currentResource.GetAttribute("Value", string.Empty)))
              colorResource.RemoveAttribute("Value", string.Empty);
            colorResource.SetAttribute("Resource", string.Empty, newColor.ToString());
          }
        }
        _modifiedColors.Clear();
        Log("{0} colors saved to XML document", _colors.Count);
      }

      public void SetColor(string color, Color value)
      {
        _colors[color] = value;
        _modifiedColors[color] = value;
      }
    }

    public class ColorRow : INotifyPropertyChanged
    {
      protected MainWindow _parent;
      protected string _color;
      protected int _rowIndex;
      
      public ColorRow(MainWindow parent, string color, int rowIndex)
      {
        _parent = parent;
        _color = color;
        _rowIndex = rowIndex;
      }

      public string Name
      {
        get { return _color; }
      }

      public Color? this[int index]
      {
        get
        {
          Color result;
          return _parent.ColorResourceFiles[index].Colors.TryGetValue(_color, out result) ? result : new Color?();
        }
        set
        {
          if (!value.HasValue)
            return;
          _parent.ColorResourceFiles[index].SetColor(_color, value.Value);
          OnPropertyChanged();
        }
      }

      public int RowIndex
      {
        get { return _rowIndex; }
      }

      public event PropertyChangedEventHandler PropertyChanged;

      protected void OnPropertyChanged()
      {
        PropertyChangedEventHandler dlgt = PropertyChanged;
        if (dlgt != null)
        {
          PropertyChangedEventArgs e = new PropertyChangedEventArgs("Item[]");
          dlgt(this, e);
        }
      }
    }

    protected IList<ColorResourceFileDescriptor> _colorResourceFiles = new List<ColorResourceFileDescriptor>(10);
    protected ObservableCollection<ColorRow> _colors = new ObservableCollection<ColorRow>();
    protected ColorDialog _colorDialog = null;

    public MainWindow()
    {
      InitializeComponent();
      _colorDialog = new ColorDialog
        {
            AllowFullOpen = true, // Keeps the user from selecting a custom color.
            FullOpen = true,
            AnyColor = true,
            ShowHelp = false, // Allows the user to get help. (The default is false.)
        };
    }

    public IList<ColorResourceFileDescriptor> ColorResourceFiles
    {
      get { return _colorResourceFiles; }
    }

    public IList<ColorRow> Colors
    {
      get { return _colors; }
    }

    private void AddColorResourceFileButton_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog dlg = new OpenFileDialog
        {
            FileName = "Colors.xaml",
            DefaultExt = ".xaml",
            Filter = "XAML files (.xaml)|*.xaml|All files (*.*)|*.*"
        };

      bool? result = dlg.ShowDialog();

      if (result == true)
        AddColorResourceFile(dlg.FileName);
    }

    private void RemoveColorResourceButton_Click(object sender, RoutedEventArgs e)
    {
      int numFiles = _colorResourceFiles.Count;
      if (numFiles > 0)
        _colorResourceFiles.RemoveAt(numFiles - 1);
      ReloadFiles();
    }

    private void Color0Click(object sender, RoutedEventArgs e)
    {
      ColorRow row = (ColorRow) ((Button) sender).DataContext;
      ChangeColor(row, 0);
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
      bool dirty = _colorResourceFiles.Any(crf => crf.IsDirty);
      if (dirty)
      {
        MessageBoxResult result = MessageBox.Show("Do you want to save the changes?",
            "Colors were changed", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

        switch (result)
        {
          case MessageBoxResult.Yes:
            Save();
            break;
          case MessageBoxResult.No:
            break;
          case MessageBoxResult.Cancel:
            e.Cancel = true;
            break;
        }
      }
    }

    protected void ChangeColor(ColorRow row, int index)
    {
      Color? c = row[index];
      if (!c.HasValue)
        return;
      Color currentColor = c.Value;
      // Set the initial color select to the current color
      _colorDialog.Color = System.Drawing.Color.FromArgb(currentColor.A, currentColor.R, currentColor.G, currentColor.B);


      // Update the text box color if the user clicks OK 
      if (_colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        System.Drawing.Color newColor = _colorDialog.Color;
        row[index] = Color.FromArgb(newColor.A, newColor.R, newColor.G, newColor.B);
      }
    }

    protected void AddColorResourceFile(string fileName)
    {
      ColorResourceFileDescriptor td = new ColorResourceFileDescriptor(fileName);
      _colorResourceFiles.Add(td);
      ReloadFiles();
    }

    protected void ReloadFiles()
    {
      _colors.Clear();
      ISet<string> colors = new HashSet<string>();
      foreach (string color in _colorResourceFiles.SelectMany(crd => crd.Colors.Keys))
        colors.Add(color);
      int index = 0;
      foreach (string color in colors.OrderBy(c => c))
        _colors.Add(new ColorRow(this, color, index++));
      GridViewColumnCollection gvcc = ((GridView) ColorsList.View).Columns;
      for (int i = gvcc.Count; i > _colorResourceFiles.Count + 1; i--)
        gvcc.RemoveAt(i - 1);
      for (int i = gvcc.Count; i < _colorResourceFiles.Count + 1; i++)
      {
        DataTemplate colorTemplate = ColorsList.FindResource("ColorTemplate" + (i - 1)) as DataTemplate;
        gvcc.Add(new GridViewColumn
          {
              Width = 150,
              Header = _colorResourceFiles[i - 1].ThemeName,
              CellTemplate = colorTemplate,
          });
      }
    }

    protected void Save()
    {
      foreach (ColorResourceFileDescriptor crfd in _colorResourceFiles)
        if (crfd.IsDirty)
          crfd.Save();
    }

    protected static void Log(string formatString, params object[] p)
    {
      Console.Out.WriteLine(formatString, p);
    }

    private void SaveColorResourcesButton_Click(object sender, RoutedEventArgs e)
    {
      Save();
    }

    private void ReloadColorResourcesButton_Click(object sender, RoutedEventArgs e)
    {
      ReloadFiles();
    }
  }
}
