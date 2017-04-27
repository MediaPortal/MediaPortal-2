<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Margin="-10,-15,0,0">

  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Label x:Name="MediaItemName" Grid.Row="0"
         IsVisible="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem,Converter={StaticResource ReferenceToBoolConverter}}"
         DataContext="{Binding Source={StaticResource FanArtBackgroundModel}}" Content="{Binding SimpleTitle}"
         HorizontalAlignment="Stretch" Color="{ThemeResource TextColor}" FontFamily="DefaultBold"/>

  <StackPanel Orientation="Vertical" Grid.Row="1" Margin="0,0,0,5" DataContext="{Binding Source={StaticResource FanArtBackgroundModel}}"
         IsVisible="{Binding Path=NumItems,Converter={StaticResource ReferenceToBoolConverter}}">
    <StackPanel Orientation="Horizontal">
      <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding NumItems}"
             Margin="0,0,8,0" Color="{ThemeResource TextColor}" Opacity="0.6"/>
      <Label FontSize="{ThemeResource SmallFontSize}" Content="[Media.MediaItemsHeader]"
             Margin="0,0,8,0" Color="{ThemeResource TextColor}" Opacity="0.6"/>
    </StackPanel>
  </StackPanel>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Row="1"
         TextTrimming="WordEllipsis" HorizontalAlignment="Stretch"
         Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
         Margin="0,0,35,25" Color="{ThemeResource TextColor}"/>
</Grid>
