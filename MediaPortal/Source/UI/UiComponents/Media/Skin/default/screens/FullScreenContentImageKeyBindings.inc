<?xml version="1.0" encoding="utf-8"?>
<!-- DataContext model = VideoPlayerModel -->
<Grid xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    DataContext="{Binding Source={Model 4E2301B4-3C17-4a1d-8DE5-2CEA169A0256}}">

  <KeyBinding Key="Info" Command="{Command ToggleOSD}"/>
  <KeyBinding Key="Back" Command="{Command CloseOSD}" IsEnabled="{Binding IsOSDVisible}"/>
  <KeyBinding Key="Escape" Command="{Command CloseOSD}" IsEnabled="{Binding IsOSDVisible}"/>

  <KeyBinding Key="PageUp" Command="{Command Source={Service PlayerContextManager},Path=NextItem}"/>
  <KeyBinding Key="PageDown" Command="{Command Source={Service PlayerContextManager},Path=PreviousItem}"/>
</Grid>
