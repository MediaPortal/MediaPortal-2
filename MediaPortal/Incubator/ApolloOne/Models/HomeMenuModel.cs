using System;
using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.SkinBase.Models;

namespace MediaPortal.UiComponents.ApolloOne.Models
{
  public class HomeMenuModel : MenuModel
  {
    #region Consts

    public const string STR_HOMEMENU_MODEL_ID = "EBA16B93-B669-4162-9CA2-CB1D5E267EC3";
    public static readonly Guid HOMEMENU_MODEL_ID = new Guid(STR_HOMEMENU_MODEL_ID);

    #endregion

    #region Fields

    readonly Dictionary<Guid, GridPosition> _positions = new Dictionary<Guid, GridPosition>();
    readonly ItemsList _positionedItems = new ItemsList();

    #endregion

    #region Internal class

    public class GridPosition
    {
      public int Row { get; set; }
      public int RowSpan { get; set; }
      public int Column { get; set; }
      public int ColumnSpan { get; set; }

      public GridPosition()
      {
        RowSpan = 1;
        ColumnSpan = 1;
      }
    }

    #endregion

    public HomeMenuModel()
    {
      ReadPositions();
      CreatePositionedItems();
      MenuItems.ObjectChanged += MenuItemsOnObjectChanged;
    }

    private void MenuItemsOnObjectChanged(IObservable observable)
    {
      CreatePositionedItems();
    }

    public void CreatePositionedItems()
    {
      _positionedItems.Clear();
      foreach (var menuItem in MenuItems)
      {
        object action;
        if (!menuItem.AdditionalProperties.TryGetValue(Consts.KEY_ITEM_ACTION, out action))
          continue;
        WorkflowAction wfAction = action as WorkflowAction;
        if (wfAction == null)
          continue;

        GridPosition gridPosition;
        if (_positions.TryGetValue(wfAction.ActionId, out gridPosition))
        {
          GridListItem gridItem = new GridListItem(menuItem)
          {
            GridRow = gridPosition.Row,
            GridColumn = gridPosition.Column,
            GridRowSpan = gridPosition.RowSpan,
            GridColumnSpan = gridPosition.ColumnSpan,
          };
          _positionedItems.Add(gridItem);
        }
      }
      _positionedItems.FireChange();
    }

    public ItemsList PositionedMenuItems
    {
      get
      {
        return _positionedItems;
      }
    }

    /// <summary>
    /// Reads actions/positon from settings.
    /// </summary>
    private void ReadPositions()
    {
      _positions[new Guid("A4DF2DF6-8D66-479a-9930-D7106525EB07")] = new GridPosition { Column = 0, ColumnSpan = 5, Row = 0, RowSpan = 3 }; // Videos
      _positions[new Guid("80D2E2CC-BAAA-4750-807B-F37714153751")] = new GridPosition { Column = 0, ColumnSpan = 5, Row = 3, RowSpan = 3 }; // Movies
      _positions[new Guid("30F57CBA-459C-4202-A587-09FFF5098251")] = new GridPosition { Column = 5, ColumnSpan = 5, Row = 0, RowSpan = 3 }; // Series

      _positions[new Guid("93442DF7-186D-42e5-A0F5-CF1493E68F49")] = new GridPosition { Column = 10, ColumnSpan = 6, Row = 0, RowSpan = 6 }; // Browse Media

      _positions[new Guid("BB49A591-7705-408F-8177-45D633FDFAD0")] = new GridPosition { Column = 5, ColumnSpan = 2, Row = 3, RowSpan = 2 };

      _positions[new Guid("17D2390E-5B05-4fbd-89F6-24D60CEB427F")] = new GridPosition { Column = 5, ColumnSpan = 5, Row = 0, RowSpan = 3 };
    }
  }
}
