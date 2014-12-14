<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  HorizontalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto"/>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="Auto"/>
  </Grid.ColumnDefinitions>

  <Label x:Name="MediaItemName" Wrap="True" Grid.Column="0"
      HorizontalAlignment="Stretch"
      Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}"
      Color="{ThemeResource TextColor}"
      FontFamily="DefaultBold"/>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Row="1" Grid.ColumnSpan="3"
        Scroll="Auto" ScrollSpeed="5" ScrollDelay="5"
        HorizontalAlignment="Stretch"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Color="{ThemeResource TextColor}"/>

  <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Grid.Row="0" Grid.Column="1" Margin="15,-5,0,0">
    <media:MediaAspectWrapper.Template>
      <ControlTemplate>
        <Label x:Name="Year" FontSize="{ThemeResource SmallFontSize}"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>
      </ControlTemplate>
    </media:MediaAspectWrapper.Template>
  </media:MediaAspectWrapper>

</Grid>
