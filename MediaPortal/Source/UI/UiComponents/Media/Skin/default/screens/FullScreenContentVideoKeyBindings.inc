<?xml version="1.0" encoding="utf-8"?>
<!-- DataContext model = VideoPlayerModel -->
<Grid xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    DataContext="{Binding Source={Model 4E2301B4-3C17-4a1d-8DE5-2CEA169A0256}}">
  <Grid.Resources>
    <Model x:Key="ExtendedVideoSkip" Id="8573DBD8-A257-426a-9875-9DB155D32D47"/>
  </Grid.Resources>

  <KeyBinding Key="Info" Command="{Command ToggleOSD}"/>
  <KeyBinding Key="Back" Command="{Command CloseOSD}" IsEnabled="{Binding IsOSDVisible}"/>
  <KeyBinding Key="Escape" Command="{Command CloseOSD}" IsEnabled="{Binding IsOSDVisible}"/>
  <KeyBinding Key="ZoomMode" Command="{Command PlayerUIContributor.ShowZoomModeDialog}"/>
  <KeyBinding Key="Yellow" Command="{Command PlayerUIContributor.OpenChooseSubtitleDialog}" IsEnabled="{Binding PlayerUIContributor.SubtitlesAvailable}"/>
  <KeyBinding Key="Blue" Command="{Command Source={Service WorkflowManager},Path=NavigatePush,Parameters=A3F53310-4D93-4f93-8B09-D53EE8ACD829}" />

  <KeyBinding Key="Up" Command="{Command Source={StaticResource ExtendedVideoSkip},Path=InstantSkipForward}" IsEnabled="{Binding !IsOSDVisible}"/>
  <KeyBinding Key="Down" Command="{Command Source={StaticResource ExtendedVideoSkip},Path=InstantSkipBackward}" IsEnabled="{Binding !IsOSDVisible}"/>

  <KeyBinding Key="Right" Command="{Command Source={StaticResource ExtendedVideoSkip},Path=SkipStepForward}" IsEnabled="{Binding !IsOSDVisible}"/>
  <KeyBinding Key="Left" Command="{Command Source={StaticResource ExtendedVideoSkip},Path=SkipStepBackward}" IsEnabled="{Binding !IsOSDVisible}"/>

  <KeyBinding Key="PageUp" Command="{Command Source={Service PlayerContextManager},Path=NextItem}"/>
  <KeyBinding Key="PageDown" Command="{Command Source={Service PlayerContextManager},Path=PreviousItem}"/>
</Grid>
