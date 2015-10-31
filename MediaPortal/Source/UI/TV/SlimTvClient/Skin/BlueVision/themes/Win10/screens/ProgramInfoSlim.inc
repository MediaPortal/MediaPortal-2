<?xml version="1.0" encoding="utf-8"?>
<DockPanel xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
    x:Name="ProgramInfoPanel" LastChildFill="true" Height="170">
  
  <Grid DockPanel.Dock="Top" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="-60,-45,-55,0"
        DataContext="{Binding Source={StaticResource SlimTvMultiChannelGuide}}">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*"/>
      <ColumnDefinition Width="320"/>
    </Grid.ColumnDefinitions>

    <StackPanel Orientation="Vertical" HorizontalAlignment="Right" Grid.Row="0" Grid.RowSpan="2" Grid.Column="1">
      <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" >
        <Label x:Name="ProgramStart" Margin="0,0,0,0"  Color="{ThemeResource TextColor}"
               Content="{Binding Path=CurrentProgram.StartTime, Converter={StaticResource SlimTvDateFormatConverter}}"/>
        <Label Margin="5,0,5,0" Content="—" Color="{ThemeResource TextColor}"/>
        <Label x:Name="ProgramEnd" Content="{Binding Path=CurrentProgram.EndTime, Converter={StaticResource SlimTvDateFormatConverter}}"
            Color="{ThemeResource TextColor}" Wrap="True" Scroll="Auto"/>
      </StackPanel>
      <Label x:Name="Channel" Scroll="Auto" Content="{Binding ChannelName}" Color="{ThemeResource TextColor}" 
             HorizontalAlignment="Right"/>
      <Image x:Name="ChannelLogo" Width="{ThemeResource ChannelLogoLarge}"
             Stretch="Uniform" HorizontalAlignment="Right" VerticalAlignment="Top" Margin="0,10,0,0">
        <Image.Source>
          <fanart:FanArtImageSource fanart:FanArtMediaType="ChannelTv" fanart:FanArtType="Banner" 
                                    fanart:FanArtName="{Binding ChannelName}" fanart:MaxWidth="0" fanart:MaxHeight="0"/>
        </Image.Source>
      </Image>
    </StackPanel>
    
    <Label x:Name="Program" Grid.Row="0" Grid.Column="0" Scroll="Auto" FontFamily="DefaultBold"
              Content="{Binding CurrentProgram.Title}" Margin="0,0,0,0"
              Color="{ThemeResource TextColor}"/>
    
    <Label Grid.Row="1" Grid.Column="0" x:Name="Description" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
           Wrap="True" Scroll="Auto"
           Content="{Binding Path=CurrentProgram.Description}"
           Color="{ThemeResource TextColor}" Margin="0,0,0,5" />
  </Grid>
</DockPanel>
