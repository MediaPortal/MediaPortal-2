<?xml version="1.0" encoding="utf-8"?>
<DockPanel xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
    x:Name="ProgramInfoPanel" LastChildFill="true" Height="170">

  <Grid DockPanel.Dock="Top" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="8,10,0,-30">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="1500"/>
      <ColumnDefinition Width="320"/>
    </Grid.ColumnDefinitions>

      <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" Margin="-100,0,0,0" >
        <Label x:Name="ProgramStart" Margin="0,0,0,0"  Color="{ThemeResource TextColor}"
               Content="{Binding Path=CurrentProgram.StartTime, Converter={StaticResource SlimTvDateFormatConverter}}"/>
        <Label Margin="5,0,5,0" Content="—" Color="{ThemeResource TextColor}"/>
        <Label x:Name="ProgramEnd" Content="{Binding Path=CurrentProgram.EndTime, Converter={StaticResource SlimTvDateFormatConverter}}"
            Color="{ThemeResource TextColor}"/>
      </StackPanel>
    
      <Label x:Name="Channel" Scroll="Auto" Content="{Binding ChannelName}" Color="{ThemeResource TextColor}"
             HorizontalAlignment="Right" Grid.Row="1" Grid.Column="1" />
    
      <Image x:Name="ChannelLogo" Width="{ThemeResource ChannelLogoLarge}" Grid.Row="2" Grid.Column="1" Margin="0,10,0,0"
             Stretch="Uniform" HorizontalAlignment="Right" VerticalAlignment="Top" >
        <Image.Source>
          <fanart:FanArtImageSource fanart:FanArtMediaType="ChannelTv" fanart:FanArtType="Banner"
                                    fanart:FanArtName="{Binding ChannelName}" fanart:MaxWidth="0" fanart:MaxHeight="0"/>
        </Image.Source>
      </Image>

    <StackPanel Orientation="Horizontal" Grid.Row="0" Grid.Column="0" >
      <Label x:Name="Program" Grid.Row="0" Grid.Column="0" Scroll="Auto" FontFamily="DefaultBold" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
             Content="{Binding CurrentProgram.Title}" Margin="0,0,0,0"
             Color="{ThemeResource TextColor}"/>
      <Label x:Name="SeriesLabel" Grid.Column="0" Grid.Row="1" Content="{Binding [Series]}" 
             Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" Margin="5,0,0,0"/>
    </StackPanel>

    <Label Grid.Row="1" Grid.Column="0" Grid.RowSpan="2" x:Name="Description" HorizontalAlignment="Stretch" VerticalAlignment="Top"
           Wrap="True" Scroll="Auto" Height="175"
           Content="{Binding Path=CurrentProgram.Description}"
           Color="{ThemeResource TextColor}" Margin="0,0,10,-10" />
  </Grid>
</DockPanel>
