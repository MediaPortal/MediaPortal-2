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

using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.Commands;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers
{
  /// <summary>
  /// <see cref="TriggerAction"/> wrapper class for an <see cref="IExecutableCommand"/>.
  /// </summary>
  public class TriggerCommand : TriggerAction
  {
    #region Protected fields

    protected IExecutableCommand _command;

    #endregion

    #region Ctor

    public TriggerCommand()
    {
      Init();
    }

    void Init()
    {
      _command = null;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      TriggerCommand ic = (TriggerCommand) source;
      Command = copyManager.GetCopy(ic.Command);
    }

    public override void Dispose()
    {
      MPF.TryCleanupAndDispose(Command);
      base.Dispose();
    }

    #endregion

    #region Public properties

    public IExecutableCommand Command
    {
      get { return _command; }
      set { _command = value; }
    }

    #endregion

    #region Base overrides

    public override void Execute(UIElement element)
    {
      IExecutableCommand cmd = Command;
      if (cmd == null)
        return;
      if (element.ElementState == ElementState.Running)
        InputManager.Instance.ExecuteCommand(cmd.Execute);
      else
        cmd.Execute();
    }

    #endregion
  }
}
