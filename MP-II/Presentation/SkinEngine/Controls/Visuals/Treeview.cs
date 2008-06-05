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
using MediaPortal.Presentation.Collections;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class TreeView : ItemsControl
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

    public TreeView()
    {
      Init();
    }

    void Init()
    {
      _commandParameter = new Property(typeof(object), null);
      _commands = new Property(typeof(CommandGroup), new CommandGroup(this));
      _command = null;
      _contextMenuCommandParameterProperty = new Property(typeof(object), null);
      _contextMenuCommand = null;

    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      TreeView tv = source as TreeView;
      Command = copyManager.GetCopy(tv.Command);
      CommandParameter = copyManager.GetCopy(tv._commandParameter);
      SelectionChanged = copyManager.GetCopy(tv.SelectionChanged);

      ContextMenuCommand = copyManager.GetCopy(tv.ContextMenuCommand);
      ContextMenuCommandParameter = copyManager.GetCopy(tv.ContextMenuCommandParameter);
      foreach (InvokeCommand command in tv.Commands)
        Commands.AddChild(copyManager.GetCopy(command));
    }

    #endregion

    #region Events

    public Command SelectionChanged
    {
      get { return _selectionChanged; }
      set { _selectionChanged = value; }
    }

    #endregion

    #region Public properties

    public Property CommandsProperty
    {
      get { return _commands; }
    }

    public CommandGroup Commands
    {
      get { return _commands.GetValue() as CommandGroup; }
      set
      {
        _commands.SetValue(value);
        ((CommandGroup) _commands.GetValue()).Owner = this;
      }
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
      UIElement element = FindElement(FocusFinder.Instance);
      if (element == null)
      {
        CurrentItem = null;
      }
      else
      {
        while (element.Context == null && element.VisualParent != null)
          element = element.VisualParent as UIElement;
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
      TreeViewItem container = new TreeViewItem();
      container.Style = ItemContainerStyle;
      container.ItemContainerStyle = ItemContainerStyle; // TreeItems also have to build containers...
      container.Context = dataItem;
      container.TemplateControl = new ItemsPresenter();
      container.TemplateControl.Margin = new SlimDX.Vector4(64, 0, 0, 0);
      container.TemplateControl.VisualParent = container;
      container.ItemsPanel = ItemsPanel;
      if (dataItem is ListItem)
      {
        ListItem listItem = (ListItem) dataItem;
        container.ItemsSource = listItem.SubItems;
      }

      container.HeaderTemplateSelector = ItemTemplateSelector;
      container.HeaderTemplate = ItemTemplate;
      FrameworkElement containerTemplateControl = ItemContainerStyle.Get();
      containerTemplateControl.Context = dataItem;
      ContentPresenter headerContentPresenter = containerTemplateControl.FindElement(new TypeFinder(typeof(ContentPresenter))) as ContentPresenter;
      headerContentPresenter.Content = (FrameworkElement)container.HeaderTemplate.LoadContent();

      container.Header = containerTemplateControl;

      ItemsPresenter p = container.Header.FindElement(new TypeFinder(typeof(ItemsPresenter))) as ItemsPresenter;
      if (p != null) p.IsVisible = false;
      return container;
    }
  }
}
