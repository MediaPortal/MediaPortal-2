using System;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.PluginManager;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.Players.Video
{
  public class BDHandlerPlugin : IPluginStateTracker, IPlayerBuilder
  {

    public BDHandlerPlugin()
    {
      //Assembly assy = Assembly.GetExecutingAssembly();
      //foreach (Attribute attr in Attribute.GetCustomAttributes(assy))
      //{
      //  if (attr.GetType() == typeof(AssemblyTitleAttribute))
      //    _pluginName = ((AssemblyTitleAttribute)attr).Title;
      //  else if (attr.GetType() == typeof(AssemblyDescriptionAttribute))
      //    _pluginDesc = ((AssemblyDescriptionAttribute)attr).Description;
      //  else if (attr.GetType() == typeof(AssemblyCompanyAttribute))
      //    _pluginAuthor = ((AssemblyCompanyAttribute)attr).Company;
      //}
    }

    //private void OnMessage(GUIMessage message) {
    //    if (message.Message != GUIMessage.MessageType.GUI_MSG_BLURAY_DISK_INSERTED || g_Player.Playing)
    //        return;

    //    BDHandlerCore.PlayDisc(message.Label);
    //}

    #region IPlugin Members

    public void Start()
    {
      if (!BDHandlerCore.Enabled)
      {
        if (BDHandlerCore.Init())
        {
          BDHandlerCore.Enabled = true;
          //GUIWindowManager.Receivers += new SendMessageHandler(this.OnMessage);
          BDHandlerCore.LogInfo("Player handling is activated.");
        }
        else
        {
          BDHandlerCore.LogInfo("Plugin is disabled because no suitable splitter was detected.");
        }
      }
    }


    #endregion

    //#region ISetupForm Members

    //public string PluginName()
    //{
    //  return _pluginName;
    //} private string _pluginName;

    //public string Description()
    //{
    //  return _pluginDesc;
    //} private string _pluginDesc;

    //public string Author()
    //{
    //  return _pluginAuthor;
    //}  private string _pluginAuthor;

    //public void ShowPlugin()
    //{
    //  return;
    //}

    //public bool CanEnable()
    //{
    //  return true;
    //}

    //public int GetWindowId()
    //{
    //  return 19801015;
    //}

    //public bool DefaultEnabled()
    //{
    //  return true;
    //}

    //public bool HasSetup()
    //{
    //  return false;
    //}

    //public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
    //{
    //  strButtonText = string.Empty;
    //  strButtonImage = string.Empty;
    //  strButtonImageFocus = string.Empty;
    //  strPictureImage = string.Empty;
    //  return false;
    //}

    //#endregion
    #region Protected fields

    protected string _pluginDirectory = null;

    #endregion

    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      _pluginDirectory = pluginRuntime.Metadata.GetAbsolutePath(string.Empty);
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      if (BDHandlerCore.Enabled)
      {
        //GUIWindowManager.Receivers -= OnMessage;
        BDHandlerCore.Enabled = false;
        BDHandlerCore.LogInfo("Player handling is deactivated.");
      }
    }

    public void Continue() { }

    void IPluginStateTracker.Shutdown() { }

    #endregion

    #region IPlayerBuilder implementation

    public IPlayer GetPlayer(IResourceLocator locator, string mimeType)
    {
      if (mimeType == "video/bluray")
      {
        BDPlayer player = new BDPlayer();
        try
        {
          player.SetMediaItemLocator(locator);
        }
        catch (Exception e)
        {
          BDHandlerCore.LogError("Error playing media item '{0}'", locator);
          player.Dispose();
          return null;
        }
        return player;
      }
      return null;
    }

    #endregion
  }

}