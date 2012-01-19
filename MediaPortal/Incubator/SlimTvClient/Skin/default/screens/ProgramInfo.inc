<?xml version="1.0" encoding="utf-8"?>
<Grid xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
    x:Name="ProgramInfoPanel" Margin="10">
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="{ThemeResource ProgramInfoHeaderWidth}"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  <Label x:Name="ChannelLabel" Grid.Row="0" Grid.Column="0" Content="[SlimTvClient.ChannelLabel]" Color="{ThemeResource TextColor}"/>
  <Label x:Name="Channel" Grid.Row="0" Grid.Column="1" Scroll="Auto" Content="{Binding ChannelName}" Color="{ThemeResource TextColor}"/>

  <Label x:Name="ProgramLabel" Grid.Row="1" Grid.Column="0" Content="[SlimTvClient.ProgramLabel]" Color="{ThemeResource TextColor}"/>
  <Label x:Name="Program" Grid.Row="1" Grid.Column="1" Scroll="Auto" Content="{Binding CurrentProgram.Title}" Color="{ThemeResource TextColor}"/>

  <Label x:Name="ProgramStartLabel" Grid.Row="2" Grid.Column="0" Content="[SlimTvClient.ProgramStartLabel]" Color="{ThemeResource TextColor}"/>
  <Label x:Name="ProgramStart" Grid.Row="2" Grid.Column="1" Scroll="Auto"
      Content="{Binding Path=CurrentProgram.StartTime, Converter={StaticResource DateFormatConverter}}"
      Color="{ThemeResource TextColor}"/>

  <Label x:Name="ProgramEndLabel" Grid.Row="3" Grid.Column="0" Content="[SlimTvClient.ProgramEndLabel]" Color="{ThemeResource TextColor}"/>
  <Label x:Name="ProgramEnd" Grid.Row="3" Grid.Column="1" Scroll="Auto"
      Content="{Binding Path=CurrentProgram.EndTime, Converter={StaticResource DateFormatConverter}}"
      Color="{ThemeResource TextColor}"/>

  <Label x:Name="GenreLabel" Grid.Row="4" Grid.Column="0" Content="[SlimTvClient.GenreLabel]" Color="{ThemeResource TextColor}"/>
  <Label x:Name="Genre" Grid.Row="4" Grid.Column="1" Content="{Binding Path=CurrentProgram.Genre}" Color="{ThemeResource TextColor}" Scroll="Auto"/>

  <!-- TODO: add all related episode information -->
  <Label x:Name="EpisodeLabel" Grid.Row="5" Grid.Column="0" Content="[SlimTvClient.EpisodeLabel]" Color="{ThemeResource TextColor}"/>
  <Label x:Name="Episode" Grid.Row="5" Grid.Column="1" Content="{Binding Path=CurrentProgram.EpisodeName}" Color="{ThemeResource TextColor}" Scroll="Auto"/>

  <Label x:Name="DescriptionLabel" Grid.Row="6" Grid.Column="0" Grid.ColumnSpan="2" Content="[SlimTvClient.DescriptionLabel]" Color="{ThemeResource TextColor}"/>
  <Label x:Name="Description" Grid.Row="7" Grid.Column="0" Grid.ColumnSpan="2" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" 
      Wrap="True" Scroll="Auto" Content="{Binding Path=CurrentProgram.Description}" Color="{ThemeResource TextColor}"/>
</Grid>
