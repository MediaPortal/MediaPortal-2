<?xml version="1.0" encoding="utf-8"?>
<DockPanel xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
    x:Name="ProgramInfoPanel" LastChildFill="true" Height="170">
  
  <Grid DockPanel.Dock="Top" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
        DataContext="{Binding Source={StaticResource SlimTvMultiChannelGuide}}">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="250"/>
      <ColumnDefinition Width="Auto"/>
      <ColumnDefinition Width="Auto"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    <Label x:Name="Channel" Grid.Row="0" Grid.Column="0" Scroll="Auto" Content="{Binding ChannelName}" Color="{ThemeResource TextColor}"/>

    <Image x:Name="ChannelLogo" Grid.Column="0" Grid.Row="1" Width="{ThemeResource ChannelLogoLarge}" Height="{ThemeResource ChannelLogoLarge}" Stretch="Uniform">
      <Image.Source>
        <fanart:FanArtImageSource fanart:FanArtMediaType="ChannelTv" fanart:FanArtType="Banner" fanart:FanArtName="{Binding ChannelName}" fanart:MaxWidth="0" fanart:MaxHeight="0"/>
      </Image.Source>
    </Image>

    <Label x:Name="ProgramStart" Grid.Row="0" Grid.Column="1" Scroll="Auto" Margin="5,0,0,0"
        Content="{Binding Path=CurrentProgram.StartTime, Converter={StaticResource SlimTvDateFormatConverter}}"
        Color="{ThemeResource TextColor}"/>

    <Label x:Name="ProgramEnd" Grid.Row="0" Grid.Column="2" Scroll="Auto" Margin="5,0,0,0"
        Content="{Binding Path=CurrentProgram.EndTime, Converter={StaticResource SlimTvDateFormatConverter}}"
        Color="{ThemeResource TextColor}"/>

    <Label x:Name="Program" Grid.Row="0" Grid.Column="3" Scroll="Auto" Margin="5,0,0,0"
        Content="{Binding CurrentProgram.Title}"
        Color="{ThemeResource EmphasisTextColor}"/>

    <Label Grid.Row="1" Grid.Column="1" Grid.ColumnSpan="3" x:Name="Description" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
           Wrap="True" Scroll="Auto"
           Content="{Binding Path=CurrentProgram.Description}"
           Color="{ThemeResource TextColor}" Margin="0,0,0,25" />
  </Grid>
</DockPanel>
