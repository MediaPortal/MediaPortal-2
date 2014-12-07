<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media">

  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Label x:Name="MediaItemName" Grid.Row="0"
          HorizontalAlignment="Stretch"
          Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}"
          Color="{ThemeResource TextColor}"/>

  <Label x:Name="MediaItemDescription" Wrap="True" Scroll="Auto" ScrollSpeed="5" Grid.Row="1"
        HorizontalAlignment="Stretch"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>

</Grid>
