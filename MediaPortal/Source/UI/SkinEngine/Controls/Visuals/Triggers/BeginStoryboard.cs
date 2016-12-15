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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.Controls.Animations;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers
{
  public class BeginStoryboard : TriggerAction, IAddChild<Storyboard>
  {
    #region Protected fields

    protected AbstractProperty _storyBoardProperty;
    protected AbstractProperty _handoffBehaviorProperty;

    protected string _name;

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
      _handoffBehaviorProperty = new SProperty(typeof(HandoffBehavior), HandoffBehavior.SnapshotAndReplace);
    }

    void Attach()
    {
      _storyBoardProperty.Attach(OnStoryboardChanged);
    }

    void Detach()
    {
      _storyBoardProperty.Detach(OnStoryboardChanged);
    }

    private void OnStoryboardChanged(AbstractProperty property, object oldvalue)
    {
      var oldStoryboard = oldvalue as Storyboard;
      if (oldStoryboard != null && ReferenceEquals(oldStoryboard.LogicalParent, this))
      {
        oldStoryboard.LogicalParent = null;
      }
      Storyboard.LogicalParent = this;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      BeginStoryboard s = (BeginStoryboard) source;
      Storyboard = copyManager.GetCopy(s.Storyboard);
      HandoffBehavior = s.HandoffBehavior;
      Name = s.Name;
      Attach();
    }

    public override void Dispose()
    {
      MPF.TryCleanupAndDispose(Storyboard);
      base.Dispose();
    }

    #endregion

    protected void RegisterName()
    {
      INameScope ns = FindNameScope();
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

    public string Name
    {
      get { return _name; }
      set { _name = value; }
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

    public override double DurationInMilliseconds
    {
      get { return Storyboard.ActualDurationInMilliseconds; }
    }

    #endregion

    public override void Execute(UIElement element)
    {
      if (Storyboard != null)
        element.StartStoryboard(Storyboard, HandoffBehavior);
    }

    public override void Setup(UIElement element)
    {
      RegisterName();
    }

    public override void Reset(UIElement element)
    {
      base.Reset(element);
      if (Storyboard != null && element != null)
        element.StopStoryboard(Storyboard);
    }

    #region IAddChild<Storyboard> implementation

    public void AddChild(Storyboard s)
    {
      Storyboard = s;
    }

    #endregion
  }
}
