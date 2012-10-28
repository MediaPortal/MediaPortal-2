using System;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  /// <summary>
  /// Holds a GUI item which represents a Program item.
  /// </summary>
  public class ProgramListItem : ListItem
  {
    protected AbstractProperty _programProperty = null;
    protected AbstractProperty _isRunningProperty = null;

    /// <summary>
    /// Exposes the program.
    /// </summary>
    public ProgramProperties Program
    {
      get { return (ProgramProperties) _programProperty.GetValue(); }
      set { _programProperty.SetValue(value); }
    }
    /// <summary>
    /// Exposes the program.
    /// </summary>
    public AbstractProperty ProgramProperty
    {
      get { return _programProperty; }
    }

    /// <summary>
    /// Exposes a flag if the program is currently running.
    /// </summary>
    public bool IsRunning
    {
      get { return (bool) _isRunningProperty.GetValue(); }
      set { _isRunningProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes a flag if the program is currently running.
    /// </summary>
    public AbstractProperty IsRunningProperty
    {
      get { return _isRunningProperty; }
    }

    public ProgramListItem(ProgramProperties program)
    {
      _programProperty = new WProperty(typeof(ProgramProperties), program);
      _isRunningProperty = new WProperty(typeof(bool), false);
      SetLabel(Consts.KEY_NAME, program.Title);
      SetLabel("Title", program.Title);
      SetLabel("StartTime", FormatHelper.FormatProgramTime(program.StartTime));
      SetLabel("EndTime", FormatHelper.FormatProgramTime(program.EndTime));
      Update();
    }

    /// <summary>
    /// Updates Program IsRunning status
    /// </summary>
    public void Update()
    {
      DateTime now = DateTime.Now;
      IsRunning = Program.StartTime <= now && Program.EndTime > now;
    }
  }

  public class PlaceholderListItem : ProgramListItem
  {
    public PlaceholderListItem(ProgramProperties program)
      : base(program)
    { }
  }
}