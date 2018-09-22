<?xml version="1.0" encoding="utf-8"?>
<!-- DataContext model = AudioPlayerModel -->
<Grid xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    DataContext="{Binding Source={Model D8998340-DA2D-42be-A29B-6D7A72AEA2DC}}">

  <KeyBinding Key="Info" Command="{Command ToggleOSD}"/>
  <KeyBinding Key="Back" Command="{Command CloseOSD}" IsEnabled="{Binding IsOSDVisible}"/>
  <KeyBinding Key="Escape" Command="{Command CloseOSD}" IsEnabled="{Binding IsOSDVisible}"/>

  <KeyBinding Key="PageUp" Command="{Command Source={Service PlayerContextManager},Path=NextItem}"/>
  <KeyBinding Key="PageDown" Command="{Command Source={Service PlayerContextManager},Path=PreviousItem}"/>
</Grid>
