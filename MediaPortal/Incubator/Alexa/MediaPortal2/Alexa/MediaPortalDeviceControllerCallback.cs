namespace MediaPortal2.Alexa
{
    using MediaPortal.Alexa.Contracts;
    using MediaPortal.Common.Logging;
    using MediaPortal.UI.Presentation.Players;
    using System;

    public class MediaPortalDeviceControllerCallback : IMediaPortalDeviceControllerCallback
    {
        private readonly ILogger log;
        private readonly IPlayerContextManager playerCtxMng;
        private readonly IPlayerManager playerManager;
        private readonly TvChannelHelper tvChannelHelper;

        public MediaPortalDeviceControllerCallback(ILogger log, IPlayerContextManager playerCtxMng, IPlayerManager playerManager)
        {
            this.log = log;
            this.playerCtxMng = playerCtxMng;
            this.playerManager = playerManager;
            this.tvChannelHelper = new TvChannelHelper(log);
        }

        public int AdjustVolume(int change, bool explicitly)
        {
            if (explicitly)
            {
                this.log.Info($"Alexa adjust volume by {change}", Array.Empty<object>());
                this.playerManager.Volume += change;
            }
            else
            {
                this.log.Info("Alexa " + ((change > 0) ? "raise" : "lower") + " volume", Array.Empty<object>());
                if (change > 0)
                {
                    this.playerManager.VolumeUp();
                }
                else
                {
                    this.playerManager.VolumeDown();
                }
            }
            return this.playerManager.Volume;
        }

        public string ChangeChannel(string channelName)
        {
            this.log.Info("Alexa change channel to " + channelName, Array.Empty<object>());
            return this.tvChannelHelper.ChangeChannel(channelName);
        }

        public bool FastForward()
        {
            this.log.Info("Alexa fast forward", Array.Empty<object>());
            this.playerCtxMng.SeekForward();
            return true;
        }

        public bool? GetPlaybackState()
        {
            bool? nullable;
            bool flag1;
            bool flag3;
            this.log.Info("Alexa get playback state", Array.Empty<object>());
            IPlayerContext primaryPlayerContext = this.playerCtxMng.PrimaryPlayerContext;
            if (primaryPlayerContext != null)
            {
                flag1 = primaryPlayerContext.PlaybackState == PlaybackState.Playing;
            }
            else
            {
                IPlayerContext local1 = primaryPlayerContext;
                flag1 = false;
            }
            if (flag1)
            {
                flag3 = true;
            }
            else
            {
                IPlayerContext secondaryPlayerContext = this.playerCtxMng.SecondaryPlayerContext;
                if (secondaryPlayerContext != null)
                {
                    flag3 = secondaryPlayerContext.PlaybackState == PlaybackState.Playing;
                }
                else
                {
                    IPlayerContext local2 = secondaryPlayerContext;
                    flag3 = false;
                }
            }
            if (flag3)
            {
                nullable = true;
            }
            else
            {
                bool flag4;
                bool flag5;
                IPlayerContext context3 = this.playerCtxMng.PrimaryPlayerContext;
                if (context3 != null)
                {
                    flag4 = context3.PlaybackState == PlaybackState.Paused;
                }
                else
                {
                    IPlayerContext local3 = context3;
                    flag4 = false;
                }
                if (flag4)
                {
                    flag5 = true;
                }
                else
                {
                    IPlayerContext secondaryPlayerContext = this.playerCtxMng.SecondaryPlayerContext;
                    if (secondaryPlayerContext != null)
                    {
                        flag5 = secondaryPlayerContext.PlaybackState == PlaybackState.Paused;
                    }
                    else
                    {
                        IPlayerContext local4 = secondaryPlayerContext;
                        flag5 = false;
                    }
                }
                if (flag5)
                {
                    nullable = false;
                }
                else
                {
                    nullable = null;
                }
            }
            return nullable;
        }

        public bool Next()
        {
            this.log.Info("Alexa skip to next", Array.Empty<object>());
            this.playerCtxMng.NextItem();
            return true;
        }

        public bool Pause()
        {
            this.log.Info("Alexa pause", Array.Empty<object>());
            this.playerCtxMng.TogglePlayPause();
            return true;
        }

        public bool Play()
        {
            this.log.Info("Alexa play", Array.Empty<object>());
            this.playerCtxMng.TogglePlayPause();
            return true;
        }

        public bool Previous()
        {
            this.log.Info("Alexa go to previous", Array.Empty<object>());
            this.playerCtxMng.PreviousItem();
            return true;
        }

        public bool Rewind()
        {
            this.log.Info("Alexa rewind", Array.Empty<object>());
            this.playerCtxMng.SeekBackward();
            return true;
        }

        public bool SetMute(bool mute)
        {
            this.log.Info("Alexa " + (mute ? "mute" : "unmute") + " volume", Array.Empty<object>());
            this.playerManager.Muted = mute;
            return true;
        }

        public bool SetVolume(int level)
        {
            this.log.Info($"Alexa set volume to {level}", Array.Empty<object>());
            this.playerManager.Volume += level;
            return true;
        }

        public bool Stop()
        {
            this.log.Info("Alexa stop", Array.Empty<object>());
            this.playerCtxMng.Stop();
            return true;
        }
    }
}

