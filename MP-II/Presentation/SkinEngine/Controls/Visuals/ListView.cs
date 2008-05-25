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

using MediaPortal.Presentation.Properties;
using MediaPortal.Control.InputManager;
using Presentation.SkinEngine.Controls.Bindings;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class ListView : ItemsControl
  {
    #region Private fields

    Property _commandParameter;
    Command _command;
    Property _commands;
    Command _contextMenuCommand;
    Property _contextMenuCommandParameterProperty;
    Command _selectionChanged;

    #endregion

    #region Ctor

    public ListView()
    {
      Init();
    }

    void Init()
    {
      _commandParameter = new Property(typeof(object), null);
      _commands = new Property(typeof(CommandGroup), new CommandGroup());
      _command = null;
      _contextMenuCommandParameterProperty = new Property(typeof(object), null);
      _contextMenuCommand = null;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ListView lv = source as ListView;
      Command = copyManager.GetCopy(lv.Command);
      CommandParameter = copyManager.GetCopy(lv._commandParameter);
      SelectionChanged = copyManager.GetCopy(lv.SelectionChanged);

      ContextMenuCommand = copyManager.GetCopy(lv.ContextMenuCommand);
      ContextMenuCommandParameter = copyManager.GetCopy(lv.ContextMenuCommandParameter);
      Commands = copyManager.GetCopy(lv.Commands);
    }

    #endregion

    #region Events

    public Command SelectionChanged
    {
      get { return _selectionChanged; }
      set { _selectionChanged = value; }
    }

    #endregion

    #region Command properties

    public Property CommandsProperty
    {
      get { return _commands; }
    }

    public CommandGroup Commands
    {
      get { return _commands.GetValue() as CommandGroup; }
      set { _commands.SetValue(value); }
    }

    public Command Command
    {
      get { return _command; }
      set { _command = value; }
    }

    public Property CommandParameterProperty
    {
      get { return _commandParameter; }
    }

    public object CommandParameter
    {
      get { return _commandParameter.GetValue(); }
      set { _commandParameter.SetValue(value); }
    }

    public Command ContextMenuCommand
    {
      get { return _contextMenuCommand; }
      set { _contextMenuCommand = value; }
    }

    public Property ContextMenuCommandParameterProperty
    {
      get { return _contextMenuCommandParameterProperty; }
    }

    public object ContextMenuCommandParameter
    {
      get { return _contextMenuCommandParameterProperty.GetValue(); }
      set { _contextMenuCommandParameterProperty.SetValue(value); }
    }

    #endregion

    #region Input handling

    public override void OnMouseMove(float x, float y)
    {
      base.OnMouseMove(x, y);
      UpdateCurrentItem();
    }

    public override void OnKeyPressed(ref Key key)
    {
      UpdateCurrentItem();
      bool executeCmd = (CurrentItem != null && key == MediaPortal.Control.InputManager.Key.Enter);
      bool executeContextCmd = (CurrentItem != null && key == MediaPortal.Control.InputManager.Key.ContextMenu);
      base.OnKeyPressed(ref key);

      if (executeCmd)
      {
        if (Command != null)
        {
          Command.Execute(CommandParameter, false);
        }
        Commands.Execute(this);
      }
      if (executeContextCmd)
      {
        if (ContextMenuCommand != null)
        {
          ContextMenuCommand.Execute(ContextMenuCommandParameter, false);

        }
      }
    }

    void UpdateCurrentItem()
    {
      UIElement element = FindFocusedItem();
      if (element == null)
      {
        CurrentItem = null;
      }
      else
      {
        while (element.Context == null && element.VisualParent != null)
          element = element.VisualParent;
        CurrentItem = element.Context;
      }
      if (SelectionChanged != null)
      {
        SelectionChanged.Execute(CurrentItem, true);
      }

    }

    #endregion

    protected override FrameworkElement PrepareItemContainer(object dataItem)
    {
      ListViewItem container = new ListViewItem();
      container.Style = ItemContainerStyle;
      container.Context = dataItem;
      container.ContentTemplate = ItemTemplate;
      container.ContentTemplateSelector = ItemTemplateSelector;
      container.Content = (FrameworkElement)ItemTemplate.LoadContent();
      container.VisualParent = _itemsHostPanel;
      return container;
    }
  }
}
