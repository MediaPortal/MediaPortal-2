#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using HidInput.Inputs;
using InputDevices.Common.Mapping;
using MediaPortal.UI.Control.InputManager;
using SharpLib.Hid.Usage;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HidInput.DefaultMappings
{
  internal class WindowsMediaCenterRemote
  {
    protected static readonly IEnumerable<MappedAction> _defaultMapping = new List<MappedAction>
    {
      new MappedAction(InputAction.CreateKeyAction(Key.Power), new GenericInput(WindowsMediaCenterRemoteControl.TvPower)),
      new MappedAction(InputAction.CreateKeyAction(Key.Escape), new KeyboardInput(Keys.BrowserBack)),
      new MappedAction(InputAction.CreateKeyAction(Key.Start), new GenericInput(WindowsMediaCenterRemoteControl.GreenStart)),
      new MappedAction(InputAction.CreateKeyAction(Key.RecordedTV), new GenericInput(WindowsMediaCenterRemoteControl.RecordedTv)),
      new MappedAction(InputAction.CreateKeyAction(Key.Guide), new GenericInput(ConsumerControl.MediaSelectProgramGuide)),
      new MappedAction(InputAction.CreateKeyAction(Key.LiveTV), new GenericInput(WindowsMediaCenterRemoteControl.LiveTv)),
      new MappedAction(InputAction.CreateKeyAction(Key.DVDMenu), new GenericInput(WindowsMediaCenterRemoteControl.DvdMenu)),
      new MappedAction(InputAction.CreateKeyAction(Key.VolumeUp), new KeyboardInput(Keys.VolumeUp)),
      new MappedAction(InputAction.CreateKeyAction(Key.VolumeDown), new KeyboardInput(Keys.VolumeDown)),
      new MappedAction(InputAction.CreateKeyAction(Key.Mute), new KeyboardInput(Keys.VolumeMute)),
      new MappedAction(InputAction.CreateKeyAction(Key.PageUp), new GenericInput(ConsumerControl.ChannelIncrement)),
      new MappedAction(InputAction.CreateKeyAction(Key.PageDown), new GenericInput(ConsumerControl.ChannelDecrement)),
      new MappedAction(InputAction.CreateKeyAction(Key.Info), new GenericInput(ConsumerControl.AppCtrlProperties)),
      new MappedAction(InputAction.CreateKeyAction(Key.Stop), new KeyboardInput(Keys.MediaStop)),
      new MappedAction(InputAction.CreateKeyAction(Key.Pause), new GenericInput(ConsumerControl.Pause)),
      new MappedAction(InputAction.CreateKeyAction(Key.Record), new GenericInput(ConsumerControl.Record)),
      new MappedAction(InputAction.CreateKeyAction(Key.Play), new GenericInput(ConsumerControl.Play)),
      new MappedAction(InputAction.CreateKeyAction(Key.Rew), new GenericInput(ConsumerControl.Rewind)),
      new MappedAction(InputAction.CreateKeyAction(Key.Fwd), new GenericInput(ConsumerControl.FastForward)),
      new MappedAction(InputAction.CreateKeyAction(Key.Previous), new KeyboardInput(Keys.MediaPreviousTrack)),
      new MappedAction(InputAction.CreateKeyAction(Key.Next), new KeyboardInput(Keys.MediaNextTrack)),
      new MappedAction(InputAction.CreateKeyAction(Key.TeleText), new GenericInput(WindowsMediaCenterRemoteControl.Teletext)),
      new MappedAction(InputAction.CreateKeyAction(Key.Red), new GenericInput(WindowsMediaCenterRemoteControl.TeletextRed)),
      new MappedAction(InputAction.CreateKeyAction(Key.Green), new GenericInput(WindowsMediaCenterRemoteControl.TeletextGreen)),
      new MappedAction(InputAction.CreateKeyAction(Key.Yellow), new GenericInput(WindowsMediaCenterRemoteControl.TeletextYellow)),
      new MappedAction(InputAction.CreateKeyAction(Key.Blue), new GenericInput(WindowsMediaCenterRemoteControl.TeletextBlue)),
    }.AsReadOnly();

    public static IEnumerable<MappedAction> DefaultMapping
    {
      get { return _defaultMapping; }
    }
  }
}
