using MediaPortal.Core;
using MediaPortal.Presentation.Screens;

namespace MediaPortal.Builders
{
  public class StaticBackgroundManager : IBackgroundManager
  {
    #region Protected fields

    protected string _backgroundScreenName;

    #endregion

    public StaticBackgroundManager()
    {
      _backgroundScreenName = null;
    }

    public StaticBackgroundManager(string screenName)
    {
      _backgroundScreenName = screenName;
    }

    #region IBackgroundManager implementation

    public void Install()
    {
      if (!string.IsNullOrEmpty(_backgroundScreenName))
      {
        IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
        screenManager.SetBackgroundLayer(_backgroundScreenName);
      }
    }

    public void Uninstall()
    {
    }

    #endregion
  }
}