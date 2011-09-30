<?xml version="1.0" encoding="utf-8"?>
<DockPanel xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    x:Name="ProgramInfoPanel" LastChildFill="true">
  <Grid DockPanel.Dock="Top"
      HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="100"/>
      <ColumnDefinition Width="100"/>
      <ColumnDefinition Width="100"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    <Label x:Name="Channel" Grid.Row="0" Grid.Column="0" Scroll="Auto" FontSize="{ThemeResource SmallFontSize}"
        Content="{Binding ChannelName}" Color="{ThemeResource TextColor}"/>
    
    <Label x:Name="ProgramStart" Grid.Row="0" Grid.Column="1" Scroll="Auto" Margin="5,0,0,0" FontSize="{ThemeResource SmallFontSize}"
        Content="{Binding Path=CurrentProgram.StartTime, Converter={StaticResource DateFormatConverter}}"
        Color="{ThemeResource TextColor}"/>

    <Label x:Name="ProgramEnd" Grid.Row="0" Grid.Column="2" Scroll="Auto" Margin="5,0,0,0" FontSize="{ThemeResource SmallFontSize}"
        Content="{Binding Path=CurrentProgram.EndTime, Converter={StaticResource DateFormatConverter}}"
        Color="{ThemeResource TextColor}"/>

    <Label x:Name="Program" Grid.Row="0" Grid.Column="3" Scroll="Auto" Margin="5,0,0,0"  FontSize="{ThemeResource SmallFontSize}"
        Content="{Binding CurrentProgram.Title}"
        Color="{ThemeResource TextColor}"/>

    <Label Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="4" x:Name="Description" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
           Wrap="True" Scroll="Auto" FontSize="{ThemeResource SmallFontSize}"
           Content="{Binding Path=CurrentProgram.Description}"
           Color="{ThemeResource TextColor}"/>
  </Grid>
</DockPanel>
