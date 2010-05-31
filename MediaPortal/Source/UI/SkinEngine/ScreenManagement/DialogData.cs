using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.UI.SkinEngine.ScreenManagement
{
  public class DialogData
  {
    protected Screen _dialogScreen;
    protected DialogCloseCallbackDlgt _closeCallback;

    public DialogData(Screen dialogScreen, DialogCloseCallbackDlgt closeCallback)
    {
      _dialogScreen = dialogScreen;
      _closeCallback = closeCallback;
    }

    public Screen DialogScreen
    {
      get { return _dialogScreen; }
    }

    public DialogCloseCallbackDlgt CloseCallback
    {
      get { return _closeCallback; }
    }
  }
}