<?xml version="1.0" encoding="utf-8"?>
<DockPanel xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
    x:Name="ProgramInfoPanel" LastChildFill="true" Height="200">

  <DockPanel.Resources>
    <Model x:Key="SlimTvClient" Id="8BEC1372-1C76-484c-8A69-C7F3103708EC"/>
  </DockPanel.Resources>

  <Grid DockPanel.Dock="Center" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="0,-30,0,0"
        DataContext="{Binding Source={StaticResource SlimTvClient}}">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="450"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <StackPanel Orientation="Vertical" Grid.Row="0" Grid.RowSpan="2" Grid.Column="0" >
      <Label x:Name="ProgramStart" Margin="0,0,0,0"  Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"
             Content="{Binding Path=SelectedCurrentProgram.StartTime, Converter={StaticResource SlimTvDateFormatConverter}}"/>
      <Label x:Name="ProgramEnd" Content="{Binding Path=SelectedCurrentProgram.EndTime, Converter={StaticResource SlimTvDateFormatConverter}}"
             Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>
      <Label x:Name="Channel" Content="{Binding SelectedChannelName}" Color="{ThemeResource MenuTextBackground}" Opacity="1.2" FontSize="{ThemeResource SmallFontSize}"/>
      <Image x:Name="ChannelLogo" Width="{ThemeResource ChannelLogoLarge}" Margin="0,10,0,0"
             Stretch="Uniform" HorizontalAlignment="Left" VerticalAlignment="Top" >
        <Image.Source>
          <fanart:FanArtImageSource fanart:FanArtMediaType="ChannelTv" fanart:FanArtType="Banner"
                                    fanart:FanArtName="{Binding SelectedChannelName}" fanart:MaxWidth="0" fanart:MaxHeight="0"/>
        </Image.Source>
      </Image>
    </StackPanel>

    <Grid Grid.Row="0" Grid.Column="1" >
      <Grid.RowDefinitions>
        <RowDefinition Height="*"/>
      </Grid.RowDefinitions>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*"/>
      </Grid.ColumnDefinitions>
      <Label x:Name="Program" Scroll="Auto" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" FontFamily="SegoeUIBold"
             Content="{Binding SelectedCurrentProgram.Title}" Margin="10,0,0,0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
             Color="{ThemeResource TextColor}"/>
      <Label x:Name="SeriesLabel" Opacity="1.3" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
             IsVisible="{Binding Path=SelectedCurrentProgram.Series, Converter={StaticResource EmptyStringToFalseConverter}}"
             Color="{ThemeResource MenuTextBackground}" Margin="10,0,10,0" TextTrimming="WordEllipsis">
        <Label.Content>
          <Binding Path="SelectedCurrentProgram.Series" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{} ({0})"/>
        </Label.Content>
      </Label>
    </Grid>

    <Label Grid.Row="1" Grid.Column="1" x:Name="Description" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
           Wrap="True" TextTrimming="WordEllipsis" Opacity="1.3" FontSize="{ThemeResource SmallFontSize}"
           Content="{Binding Path=SelectedCurrentProgram.Description}"
           Color="{ThemeResource MenuTextBackground}" Margin="10,0,10,5" />
  </Grid>
</DockPanel>
