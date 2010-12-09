#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.UI.SkinEngine.Controls.Animations;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers
{
  public class BeginStoryboard : TriggerAction
  {
    #region Private fields

    protected AbstractProperty _storyBoardProperty;
    protected AbstractProperty _nameProperty;
    protected AbstractProperty _handoffBehaviorProperty;
    private string _tempName = null;

    #endregion

    #region Ctor

    public BeginStoryboard()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _storyBoardProperty = new SProperty(typeof(Storyboard), null);
      _nameProperty = new SProperty(typeof(string), string.Empty);
      _handoffBehaviorProperty = new SProperty(typeof(HandoffBehavior), HandoffBehavior.SnapshotAndReplace);
    }

    void Attach()
    {
      _nameProperty.Attach(OnNameChanged);
    }

    void Detach()
    {
      _nameProperty.Detach(OnNameChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      BeginStoryboard s = (BeginStoryboard) source;
      Storyboard = copyManager.GetCopy(s.Storyboard);
      _tempName = s.Name;
      HandoffBehavior = s.HandoffBehavior;
      Attach();

      copyManager.CopyCompleted += OnCopyCompleted;
    }

    #endregion

    void OnCopyCompleted(ICopyManager copymanager)
    {
      Name = _tempName;
      _tempName = null;
    }

    void OnNameChanged(AbstractProperty prop, object oldVal)
    {
      string oldName = (string) oldVal;
      INameScope ns = FindNameScope();
      if (ns != null && !string.IsNullOrEmpty(oldName))
        ns.UnregisterName(oldName);
      if (ns != null)
        try
        {
          ns.RegisterName(Name, this);
        }
        catch (ArgumentException)
        {
          ServiceRegistration.Get<ILogger>().Warn("Name '"+Name+"' was registered twice in namescope '"+ns+"'");
        }
    }

    #region Public properties

    public AbstractProperty StoryboardProperty
    {
      get { return _storyBoardProperty; }
    }

    public Storyboard Storyboard
    {
      get { return (Storyboard) _storyBoardProperty.GetValue(); }
      set { _storyBoardProperty.SetValue(value); }
    }


    public AbstractProperty NameProperty
    {
      get { return _nameProperty; }
    }

    public string Name
    {
      get { return _nameProperty.GetValue() as string; }
      set { _nameProperty.SetValue(value); }
    }

    public AbstractProperty HandoffBehaviorProperty
    {
      get { return _handoffBehaviorProperty; }
    }

    public HandoffBehavior HandoffBehavior
    {
      get { return (HandoffBehavior) _handoffBehaviorProperty.GetValue(); }
      set { _handoffBehaviorProperty.SetValue(value); }
    }

    #endregion

    public override void Execute(UIElement element)
    {
      if (Storyboard != null)
        element.StartStoryboard(Storyboard, HandoffBehavior);
    }
  }
}
