using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.Common.Commands;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.Screens;

namespace Emulators.LibRetro
{
  public class LibRetroPlayerUIContributor : DefaultVideoPlayerUIContributor
  {
    protected const int STATE_INDEX_COUNT = 10;

    protected LibRetroPlayer _retroPlayer;
    protected ItemsList _contextMenuItems;
    protected ItemsList _stateIndexItems;

    public override ItemsList ChapterMenuItems
    {
      get { return RetroMenuItems; }
    }

    public ItemsList RetroMenuItems
    {
      get
      {
        _contextMenuItems.Clear();
        ListItem item = new ListItem(Consts.KEY_NAME, "[Emulators.LibRetro.StateIndex]");
        item.Command = new MethodDelegateCommand(OpenChooseStateDialog);
        _contextMenuItems.Add(item);
        item = new ListItem(Consts.KEY_NAME, "[Emulators.LibRetro.LoadState]");
        item.Command = new MethodDelegateCommand(LoadState);
        _contextMenuItems.Add(item);
        item = new ListItem(Consts.KEY_NAME, "[Emulators.LibRetro.SaveState]");
        item.Command = new MethodDelegateCommand(SaveState);
        _contextMenuItems.Add(item);
        return _contextMenuItems;
      }
    }

    public ItemsList StateIndexItems
    {
      get
      {
        _stateIndexItems.Clear();
        for (int i = 0; i < STATE_INDEX_COUNT; i++)
        {
          int index = i;
          ListItem item = new ListItem(Consts.KEY_NAME, index.ToString());
          item.Command = new MethodDelegateCommand(() => SetStateIndex(index));
          _stateIndexItems.Add(item);
        }
        return _stateIndexItems;
      }
    }

    public override void Initialize(MediaWorkflowStateType stateType, IPlayer player)
    {
      base.Initialize(stateType, player);
      _retroPlayer = player as LibRetroPlayer;
      _contextMenuItems = new ItemsList();
      _stateIndexItems = new ItemsList();
    }

    protected override void Update()
    {
      base.Update();
      ChaptersAvailable = true;
    }

    public override void PrevChapter()
    {

    }

    public override void NextChapter()
    {

    }

    public override void OpenChooseChapterDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("dialog_retro_context_menu");
    }

    public void OpenChooseStateDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("dialog_retro_choose_state");
    }

    protected void SetStateIndex(int index)
    {
      if (_retroPlayer == null)
        return;
      LibRetroFrontend retro = _retroPlayer.LibRetro;
      if (retro != null)
        retro.SetStateIndex(index);
    }

    protected void SaveState()
    {
      if (_retroPlayer == null)
        return;
      LibRetroFrontend retro = _retroPlayer.LibRetro;
      if (retro != null)
        retro.SaveState();
    }

    protected void LoadState()
    {
      if (_retroPlayer == null)
        return;
      LibRetroFrontend retro = _retroPlayer.LibRetro;
      if (retro != null)
        retro.LoadState();
    }
  }
}
