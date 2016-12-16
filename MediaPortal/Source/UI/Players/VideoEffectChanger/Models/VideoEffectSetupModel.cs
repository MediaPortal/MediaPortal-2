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
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.VideoEffectChanger.Settings;

namespace MediaPortal.UiComponents.VideoEffectChanger.Models
{
  public class VideoEffectSetupModel : IWorkflowModel
  {
    #region Consts

    public const string VIDEOEFFECT_SETUP_MODEL_ID_STR = "10A1ABE0-2EF6-4DF1-822B-DEBC7DDB676A";
    public const string WF_STATE_CHOOSE_EFFECT_MENU_DIALOG = "D1722690-1B10-4EA1-B0A4-938A861F53E5";
    public const string RES_CHOOSE_LOWER_EFFECT = "[VEC.LowerEffect]";
    public const string RES_CHOOSE_HIGHER_EFFECT = "[VEC.HigherEffect]";

    public readonly static Guid VIDEOEFFECT_SETUP_MODEL_ID = new Guid(VIDEOEFFECT_SETUP_MODEL_ID_STR);
    public readonly static Guid WF_STATE_ID_CHOOSE_EFFECT_MENU_DIALOG = new Guid(WF_STATE_CHOOSE_EFFECT_MENU_DIALOG);

    private delegate void SimpleStringDelegate(string title, string file);

    #endregion

    #region Protected fields

    protected readonly AbstractProperty _isEnabledProperty = new WProperty(typeof(bool), false);
    protected readonly AbstractProperty _lowerEffectProperty = new WProperty(typeof(string), null);
    protected readonly AbstractProperty _higherEffectProperty = new WProperty(typeof(string), null);
    protected readonly AbstractProperty _resolutionLimitProperty = new WProperty(typeof(int), 0);
    protected string _lowerEffectFile;
    protected string _higherEffectFile;

    #endregion

    #region Constructor

    public VideoEffectSetupModel()
    {
      AvailableEffects = new ItemsList();
    }

    #endregion

    #region Public properties - Bindable Data

    public AbstractProperty IsEnabledProperty
    {
      get { return _isEnabledProperty; }
    }

    public bool IsEnabled
    {
      get { return (bool)_isEnabledProperty.GetValue(); }
      set { _isEnabledProperty.SetValue(value); }
    }

    public AbstractProperty LowerEffectProperty
    {
      get { return _lowerEffectProperty; }
    }

    public string LowerEffect
    {
      get { return (string)_lowerEffectProperty.GetValue(); }
      set { _lowerEffectProperty.SetValue(value); }
    }

    public AbstractProperty HigherEffectProperty
    {
      get { return _higherEffectProperty; }
    }

    public string HigherEffect
    {
      get { return (string)_higherEffectProperty.GetValue(); }
      set { _higherEffectProperty.SetValue(value); }
    }

    public AbstractProperty ResolutionLimitProperty
    {
      get { return _resolutionLimitProperty; }
    }

    public int ResolutionLimit
    {
      get { return (int)_resolutionLimitProperty.GetValue(); }
      set { _resolutionLimitProperty.SetValue(value); }
    }

    public ItemsList AvailableEffects { get; private set; }

    public string ChooseEffectDialogHeader { get; private set; }

    #endregion

    #region Public methods - Commands

    public void Select(ListItem item)
    {
      if (item == null)
        return;
      ICommand command = item.Command;
      if (command != null)
        command.Execute();
    }

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      VideoEffectChangerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoEffectChangerSettings>();
      settings.IsEnabled = IsEnabled;
      settings.LowerResolutionEffect = _lowerEffectFile;
      settings.HigherResolutionEffect = _higherEffectFile;
      settings.ResolutionLimit = ResolutionLimit;
      settingsManager.Save(settings);
    }

    public void SelectLowerEffect()
    {
      InitList(AvailableEffects, _lowerEffectFile, (title, file) =>
      {
        LowerEffect = title;
        _lowerEffectFile = file;
      });
      ChooseEffectDialogHeader = LocalizationHelper.CreateResourceString(RES_CHOOSE_LOWER_EFFECT).Evaluate();
      OpenChooseEffectDialog();
    }

    public void SelectHigherEffect()
    {
      InitList(AvailableEffects, _higherEffectFile, (title, file) =>
      {
        HigherEffect = title;
        _higherEffectFile = file;
      });
      ChooseEffectDialogHeader = LocalizationHelper.CreateResourceString(RES_CHOOSE_HIGHER_EFFECT).Evaluate();
      OpenChooseEffectDialog();
    }

    public static void OpenChooseEffectDialog()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(WF_STATE_ID_CHOOSE_EFFECT_MENU_DIALOG, new NavigationContextConfig());
    }

    #endregion

    #region Private members

    private void InitModel()
    {
      // Load settings
      VideoEffectChangerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoEffectChangerSettings>();
      IsEnabled = settings.IsEnabled;
      LowerEffect = GetEffectName(settings.LowerResolutionEffect);
      HigherEffect = GetEffectName(settings.HigherResolutionEffect);
      ResolutionLimit = settings.ResolutionLimit;
    }

    private string GetEffectName(string effectFile)
    {
      IGeometryManager geometryManager = ServiceRegistration.Get<IGeometryManager>();
      string name;
      if (effectFile != null && geometryManager.AvailableEffects.TryGetValue(effectFile, out name))
        return name;

      return GetEffectName(geometryManager.StandardEffectFile);
    }

    private void InitList(ItemsList targetList, string selectedEffect, SimpleStringDelegate command)
    {
      targetList.Clear();
      IGeometryManager geometryManager = ServiceRegistration.Get<IGeometryManager>();
      string standardEffectFile = geometryManager.StandardEffectFile;
      foreach (KeyValuePair<string, string> nameToEffect in geometryManager.AvailableEffects)
      {
        string file = nameToEffect.Key;
        string effectFile = selectedEffect ?? standardEffectFile;
        string effectName = nameToEffect.Value;
        ListItem item = new ListItem(Consts.KEY_NAME, effectName)
        {
          Command = new MethodDelegateCommand(() => command(effectName, file)),
          Selected = file == effectFile,
        };
        targetList.Add(item);
      }
      targetList.FireChange();
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return VIDEOEFFECT_SETUP_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      InitModel();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // Nothing to do here
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do here
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
