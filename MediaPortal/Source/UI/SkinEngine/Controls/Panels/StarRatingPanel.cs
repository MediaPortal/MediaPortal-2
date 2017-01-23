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
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  public class StarRatingPanel : StackPanel
  {
    protected readonly AbstractProperty _valueProperty;
    protected readonly AbstractProperty _maximumProperty;
    protected readonly AbstractProperty _isReadOnlyProperty;
    protected readonly AbstractProperty _starTemplateProperty;
    protected readonly AbstractProperty _starReadOnlyTemplateProperty;
    protected bool _itemsCreated = false;
    protected bool _clicked = false;

    public StarRatingPanel()
    {
      _valueProperty = new SProperty(typeof(Double), 0d);
      _maximumProperty = new SProperty(typeof(int), 5);
      _isReadOnlyProperty = new SProperty(typeof(bool), false);
      _starTemplateProperty = new SProperty(typeof(ControlTemplate), null);
      _starReadOnlyTemplateProperty = new SProperty(typeof(ControlTemplate), null);
      Orientation = Orientation.Horizontal;
      Attach();
    }

    public override void Dispose()
    {
      Detach();
      MPF.TryCleanupAndDispose(StarTemplate);
      MPF.TryCleanupAndDispose(StarReadOnlyTemplate);
      base.Dispose();
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      StarRatingPanel p = (StarRatingPanel)source;
      StarTemplate = copyManager.GetCopy(p.StarTemplate);
      StarReadOnlyTemplate = copyManager.GetCopy(p.StarReadOnlyTemplate);
      Maximum = p.Maximum;
      Value = p.Value;
      IsReadOnly = p.IsReadOnly;
      Attach();
    }

    private void Attach()
    {
      ValueProperty.Attach(OnValueChanged);
      MaximumProperty.Attach(OnMaximumChanged);
    }

    private void Detach()
    {
      ValueProperty.Detach(OnValueChanged);
      MaximumProperty.Detach(OnMaximumChanged);
    }

    public AbstractProperty ValueProperty
    {
      get { return _valueProperty; }
    }

    public Double Value
    {
      get { return (Double)_valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }

    public AbstractProperty MaximumProperty
    {
      get { return _maximumProperty; }
    }

    public int Maximum
    {
      get { return (int)_maximumProperty.GetValue(); }
      set { _maximumProperty.SetValue(value); }
    }

    public AbstractProperty IsReadOnlyProperty
    {
      get { return _isReadOnlyProperty; }
    }

    public bool IsReadOnly
    {
      get { return (bool)_isReadOnlyProperty.GetValue(); }
      set { _isReadOnlyProperty.SetValue(value); }
    }

    public AbstractProperty StarTemplateProperty
    {
      get { return _starTemplateProperty; }
    }

    public ControlTemplate StarTemplate
    {
      get { return (ControlTemplate)_starTemplateProperty.GetValue(); }
      set { _starTemplateProperty.SetValue(value); }
    }

    public AbstractProperty StarReadOnlyTemplateProperty
    {
      get { return _starReadOnlyTemplateProperty; }
    }

    public ControlTemplate StarReadOnlyTemplate
    {
      get { return (ControlTemplate)_starReadOnlyTemplateProperty.GetValue(); }
      set { _starReadOnlyTemplateProperty.SetValue(value); }
    }

    #region Layout overrides

    protected override void ArrangeOverride()
    {
      PrepareItems();
      base.ArrangeOverride();
    }

    private void PrepareItems()
    {
      if (_itemsCreated)
        return;
      _itemsCreated = true;

      Children.Clear();
      for (int i = 0; i < Maximum; i++)
      {
        int index = i;
        Visuals.Control cb = CreateControl(index);
        Children.Add(cb);
      }

      SetChildValues();
    }

    private Visuals.Control CreateControl(int index)
    {
      if (IsReadOnly)
      {
        return new Star
        {
          /*Star*/
          Template = MpfCopyManager.DeepCopyCutLVPs(StarReadOnlyTemplate),
          Focusable = false
        };
      }
      else
      {
        return new CheckBox
        {
          Template = MpfCopyManager.DeepCopyCutLVPs(StarTemplate),
          Checked = new CommandBridge(new MethodDelegateCommand(() => Toggle(index, true))),
          Unchecked = new CommandBridge(new MethodDelegateCommand(() => Toggle(index, false)))
        };
      }
    }

    private void Toggle(int toggledIndex, bool targetState)
    {
      SetStarValue(toggledIndex, targetState);
      _clicked = true;
      try
      {
        Value = targetState ? toggledIndex + 1 : 0;
      }
      finally
      {
        _clicked = false;
      }
    }

    private void SetStarValue(int toggledIndex, bool targetState)
    {
      var controlIndex = toggledIndex;
      var maxIndex = Maximum;
      if (maxIndex > Children.Count)
        return;
      for (int i = 0; i < Maximum; i++)
      {
        bool setChecked = targetState && i <= controlIndex;
        ((CheckBox)Children[i]).IsChecked = setChecked;
      }
    }

    private void SetStarValue(double value)
    {
      for (int i = 0; i < Children.Count; i++)
      {
        double valueGreaterIndex = value - i;
        if (valueGreaterIndex < 0)
          valueGreaterIndex = 0;
        ((Star)Children[i]).Value = valueGreaterIndex > 1.0 ? 1.0 : valueGreaterIndex;
      }
    }

    private void OnMaximumChanged(AbstractProperty property, object oldvalue)
    {
      // Force recreation
      _itemsCreated = false;
      InvalidateLayout(true, true);
    }

    private void OnValueChanged(AbstractProperty property, object oldvalue)
    {
      // Don't change value if user just toggled stars
      if (_clicked)
        return;

      // Make sure stars are created
      PrepareItems();

      SetChildValues();
    }

    private void SetChildValues()
    {
      if (IsReadOnly)
      {
        double value = Value;
        if (value < 0)
          value = 0;
        if (value >= Maximum)
          value = Maximum;
        SetStarValue(value);
      }
      else
      {
        double value = Value - 1;
        bool targetState = true;
        if (value < 0)
        {
          value = 0;
          targetState = false;
        }
        if (value > Maximum)
          value = Maximum;

        SetStarValue((int)value, targetState);
      }
    }

    #endregion
  }
}
