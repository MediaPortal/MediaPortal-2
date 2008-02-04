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

using System.Collections.Generic;
using MediaPortal.Core.Commands;
using MediaPortal.Core.Properties;
using MediaPortal.Core.Localisation;

namespace MediaPortal.Core.Collections
{
  /// <summary>
  /// class for a single item
  /// </summary>
  public delegate void ListItemChangedHandler(ListItem item);
  public class ListItem
  {
    #region variables

    private ICommand _command;
    private ICommandParameter _parameter;
    private Dictionary<string, ILabelProperty> _labels;
    protected ItemsCollection _subItems;
    public event ListItemChangedHandler OnChanged;
    Property _selectedProperty;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ListItem"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="stringvalue">The stringvalue.</param>
    public ListItem(string name, string stringvalue)
    {
      _subItems = new ItemsCollection();
      _labels = new Dictionary<string, ILabelProperty>();
      _labels[name] = new SimpleLabelProperty(stringvalue);
      _selectedProperty = new Property(false);
    }
    public ListItem(string name, StringId stringvalue)
    {
      _subItems = new ItemsCollection();
      _labels = new Dictionary<string, ILabelProperty>();
      _labels[name] = new LocalizedLabelProperty(stringvalue);
      _selectedProperty = new Property(false);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListItem"/> class.
    /// </summary>
    public ListItem()
    {
      _subItems = new ItemsCollection();
      _labels = new Dictionary<string, ILabelProperty>();
      _selectedProperty = new Property(false);
    }

    /// <summary>
    /// Determines whether listitem contains a label with the specified name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>
    /// 	<c>true</c> if [contains] [the specified name]; otherwise, <c>false</c>.
    /// </returns>
    public bool Contains(string name)
    {
      return _labels.ContainsKey(name);
    }

    /// <summary>
    /// returns the label property for the label with the name specified
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public ILabelProperty Label(string name)
    {
      if (_labels.ContainsKey(name))
      {
        return _labels[name];
      }
      return new SimpleLabelProperty("");
    }


    /// <summary>
    /// Adds a label to the listitem
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="stringvalue">The stringvalue.</param>
    public void Add(string name, string stringvalue)
    {
      _labels[name] = new SimpleLabelProperty(stringvalue);
    }

    /// <summary>
    /// Gets or sets the labels.
    /// </summary>
    /// <value>The labels.</value>
    public Dictionary<string, ILabelProperty> Labels
    {
      get { return _labels; }
      set { _labels = value; }
    }

    /// <summary>
    /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
    /// </returns>
    /// <filterPriority>2</filterPriority>
    public override string ToString()
    {
      Dictionary<string, ILabelProperty>.Enumerator enumer = _labels.GetEnumerator();
      if (enumer.MoveNext())
      {
        return enumer.Current.Value.Evaluate(null, null);
      }
      return "";
    }

    /// <summary>
    /// Executes the command associated with the listitem
    /// </summary>
    public virtual void Execute()
    {
      if (Command != null)
      {
        Command.Execute(CommandParameter);
      }
    }

    /// <summary>
    /// Gets or sets the command to execute when listitem has been selected.
    /// </summary>
    /// <value>The command.</value>
    public ICommand Command
    {
      get { return _command; }
      set { _command = value; }
    }

    /// <summary>
    /// Gets or sets the command parameter.
    /// </summary>
    /// <value>The command parameter.</value>
    public ICommandParameter CommandParameter
    {
      get { return _parameter; }
      set { _parameter = value; }
    }

    public bool Selected
    {
      get { return (bool)_selectedProperty.GetValue(); }
      set { _selectedProperty.SetValue(value); }
    }
    public Property SelectedProperty
    {
      get { return _selectedProperty; }
      set { _selectedProperty = value; }
    }

    public ItemsCollection SubItems
    {
      get { return _subItems; }
    }

    public void FireChange()
    {
      if (OnChanged != null)
        OnChanged(this);
    }
  }
}
