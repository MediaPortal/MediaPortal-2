using System;
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;

namespace WifiRemote
{
  public class NowPlayingTv : IAdditionalNowPlayingInfo
  {
    private string mediaType = "tv";

    public string MediaType
    {
      get { return mediaType; }
    }

    public string MpExtId
    {
      get { return ChannelId.ToString(); }
    }

    public int MpExtMediaType
    {
      get { return (int)MpExtendedMediaTypes.Tv; }
    }

    public int MpExtProviderId
    {
      get { return 0; } //no tv providers yet
    }

    /// <summary>
    /// ID of the current channel
    /// </summary>
    public int ChannelId { get; set; }

    /// <summary>
    /// Name of the current channel
    /// </summary>
    public string ChannelName { get; set; }

    /// <summary>
    /// Id of current program
    /// </summary>
    public int CurrentProgramId { get; set; }

    /// <summary>
    /// Name of current program
    /// </summary>
    public string CurrentProgramName { get; set; }

    /// <summary>
    /// Description of current program
    /// </summary>
    public string CurrentProgramDescription { get; set; }

    /// <summary>
    /// Start date of current program
    /// </summary>
    public DateTime CurrentProgramBegin { get; set; }

    /// <summary>
    /// End date of current program
    /// </summary>
    public DateTime CurrentProgramEnd { get; set; }

    /// <summary>
    /// Id of next program
    /// </summary>
    public int NextProgramId { get; set; }

    /// <summary>
    /// Name of next program
    /// </summary>
    public string NextProgramName { get; set; }

    /// <summary>
    /// Description of next program
    /// </summary>
    public string NextProgramDescription { get; set; }

    /// <summary>
    /// Start date of next program
    /// </summary>
    public DateTime NextProgramBegin { get; set; }

    /// <summary>
    /// End date of next program
    /// </summary>
    public DateTime NextProgramEnd { get; set; }

    // TODO: reimplement
    /// <summary>
    /// Constructor
    /// </summary>
    public NowPlayingTv()
    {
      /*TvDatabase.Channel current = MpTvServerHelper.GetCurrentTimeShiftingTVChannel();
      if (current != null)
      {
        ChannelId = current.IdChannel;
        ChannelName = current.DisplayName;

        if (current.CurrentProgram != null)
        {
          CurrentProgramId = current.CurrentProgram.IdProgram;
          CurrentProgramName = current.CurrentProgram.Title;
          CurrentProgramDescription = current.CurrentProgram.Description;
          CurrentProgramBegin = current.CurrentProgram.StartTime;
          CurrentProgramEnd = current.CurrentProgram.EndTime;
        }

        if (current.NextProgram != null)
        {
          NextProgramId = current.NextProgram.IdProgram;
          NextProgramName = current.NextProgram.Title;
          NextProgramDescription = current.NextProgram.Description;
          NextProgramBegin = current.NextProgram.StartTime;
          NextProgramEnd = current.NextProgram.EndTime;
        }
      }*/
    }
  }
}