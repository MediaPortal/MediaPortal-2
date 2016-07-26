<?xml version="1.0" encoding="utf-8"?>
<Grid xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Context="{Model Id=8BEC1372-1C76-484c-8A69-C7F3103708EC}" Grid.ZIndex="-100">
  <!-- ZIndex is required to place this dummy grid in background to avoid un-clickable controls -->
  <Grid.Resources>
    <Model x:Key="ExtendedVideoSkip" Id="8573DBD8-A257-426a-9875-9DB155D32D47"/>
    <Model x:Key="ChannelZapModel" Id="1C7DCFFE-E34E-41FD-9104-9AA594E49375"/>
  </Grid.Resources>

  <KeyBinding Key="Info" Command="{Command ShowVideoInfo}"/>
  <KeyBinding Key="ZoomMode" Command="{Command PlayerUIContributor.ShowZoomModeDialog}"/>
  <KeyBinding Key="Yellow" Command="{Command PlayerUIContributor.OpenChooseSubtitleDialog}" IsEnabled="{Binding PlayerUIContributor.SubtitlesAvailable}"/>
  <KeyBinding Key="Blue" Command="{Command Source={Service WorkflowManager},Path=NavigatePush,Parameters=A3F53310-4D93-4f93-8B09-D53EE8ACD829}" />

  <KeyBinding Key="Up" Command="{Command Source={StaticResource ExtendedVideoSkip},Path=InstantSkipForward}" IsEnabled="{Binding !IsOSDVisible}"/>
  <KeyBinding Key="Down" Command="{Command Source={StaticResource ExtendedVideoSkip},Path=InstantSkipBackward}" IsEnabled="{Binding !IsOSDVisible}"/>

  <KeyBinding Key="Right" Command="{Command Source={StaticResource ExtendedVideoSkip},Path=SkipStepForward}" IsEnabled="{Binding !IsOSDVisible}"/>
  <KeyBinding Key="Left" Command="{Command Source={StaticResource ExtendedVideoSkip},Path=SkipStepBackward}" IsEnabled="{Binding !IsOSDVisible}"/>

  <KeyBinding Key="Escape" Command="{Command CloseOSD}" IsEnabled="{Binding IsOSDVisible}"/>

  <KeyBinding Key="PageUp" Command="{Command ZapNextChannel}"/>
  <KeyBinding Key="PageDown" Command="{Command ZapPrevChannel}"/>

  <KeyBinding Key="Record" Command="{Command RecordDialog}"/>
  <KeyBinding Key="R" Command="{Command RecordDialog}"/>

  <KeyBinding Key="Ok" Command="{Command Source={Service ScreenManager},Path=ShowDialog,Parameters=SlimTVClient-miniguide}" IsEnabled="{Binding !IsOSDVisible}"/>

  <!-- Channel zapping by numbers -->
  <KeyBinding Key="0" Command="{Command Source={StaticResource ChannelZapModel},Path=ZapByNumber,Parameters=0}" IsEnabled="{Binding !IsOSDVisible}"/>
  <KeyBinding Key="1" Command="{Command Source={StaticResource ChannelZapModel},Path=ZapByNumber,Parameters=1}" IsEnabled="{Binding !IsOSDVisible}"/>
  <KeyBinding Key="2" Command="{Command Source={StaticResource ChannelZapModel},Path=ZapByNumber,Parameters=2}" IsEnabled="{Binding !IsOSDVisible}"/>
  <KeyBinding Key="3" Command="{Command Source={StaticResource ChannelZapModel},Path=ZapByNumber,Parameters=3}" IsEnabled="{Binding !IsOSDVisible}"/>
  <KeyBinding Key="4" Command="{Command Source={StaticResource ChannelZapModel},Path=ZapByNumber,Parameters=4}" IsEnabled="{Binding !IsOSDVisible}"/>
  <KeyBinding Key="5" Command="{Command Source={StaticResource ChannelZapModel},Path=ZapByNumber,Parameters=5}" IsEnabled="{Binding !IsOSDVisible}"/>
  <KeyBinding Key="6" Command="{Command Source={StaticResource ChannelZapModel},Path=ZapByNumber,Parameters=6}" IsEnabled="{Binding !IsOSDVisible}"/>
  <KeyBinding Key="7" Command="{Command Source={StaticResource ChannelZapModel},Path=ZapByNumber,Parameters=7}" IsEnabled="{Binding !IsOSDVisible}"/>
  <KeyBinding Key="8" Command="{Command Source={StaticResource ChannelZapModel},Path=ZapByNumber,Parameters=8}" IsEnabled="{Binding !IsOSDVisible}"/>
  <KeyBinding Key="9" Command="{Command Source={StaticResource ChannelZapModel},Path=ZapByNumber,Parameters=9}" IsEnabled="{Binding !IsOSDVisible}"/>

</Grid>
