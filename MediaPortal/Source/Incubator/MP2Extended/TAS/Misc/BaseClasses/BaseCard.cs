using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.TAS.Misc.BaseClasses
{
  class BaseCard
  {
    internal static WebCard Card(ICard card)
    {
      return new WebCard
      {
        Id = card.CardId,
        Name = card.Name,
        Priority = card.Priority,
        PreloadCard = card.PreloadCard,
        CAM = card.HasCam,
        CamType = (int)card.CamType,
        DecryptLimit = card.DecryptLimit,
        DevicePath = card.DevicePath,
        Enabled = card.Enabled,
        GrabEPG = card.EpgIsGrabbing,
        //LastEpgGrab = card,
        RecordingFolder = card.RecordingFolder,
        RecordingFormat = card.RecordingFormat,
        TimeShiftFolder = card.TimeshiftFolder,
        SupportSubChannels = card.SupportSubChannels
      };
    }
  }
}
