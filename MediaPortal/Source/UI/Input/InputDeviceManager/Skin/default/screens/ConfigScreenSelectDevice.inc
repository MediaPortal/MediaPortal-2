<?xml version="1.0" encoding="utf-8"?>
<StackPanel Orientation="Vertical" VerticalAlignment="Center"
            xmlns="www.team-mediaportal.com/2008/mpf/directx"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            IsVisible="{Binding ShowInputDeviceSelection}">

  <Label Content="[InputDeviceManager.SelectDevice.Header]"
         Wrap="True" Color="{ThemeResource TextColor}"
         HorizontalAlignment="Stretch" VerticalAlignment="Stretch" 
         HorizontalContentAlignment="Center" VerticalContentAlignment="Center"/>
  <Image Source="press.png" Height="200" Stretch="Uniform" Margin="0,20,0,0"/>

</StackPanel>
