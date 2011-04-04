using MediaPortal.Core.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.Plugins.SlimTvClient.Helpers
{
  /// <summary>
  /// Holds a GUI item which represents a Program item.
  /// </summary>
  public class ProgramListItem : ListItem
  {
    protected AbstractProperty _programProperty = null;

    /// <summary>
    /// Exposes the program.
    /// </summary>
    public ProgramProperties Program
    {
      get { return (ProgramProperties)_programProperty.GetValue(); }
      set { _programProperty.SetValue(value); }
    }
    /// <summary>
    /// Exposes the program.
    /// </summary>
    public AbstractProperty ProgramProperty
    {
      get { return _programProperty; }
    }

    public ProgramListItem(ProgramProperties program)
    {
      _programProperty = new WProperty(typeof (ProgramProperties), program);
      SetLabel(Consts.KEY_NAME, program.Title);
      SetLabel("Title", program.Title);
      SetLabel("StartTime", FormatHelper.FormatProgramTime(program.StartTime));
      SetLabel("EndTime", FormatHelper.FormatProgramTime(program.EndTime));
    }
  }
}