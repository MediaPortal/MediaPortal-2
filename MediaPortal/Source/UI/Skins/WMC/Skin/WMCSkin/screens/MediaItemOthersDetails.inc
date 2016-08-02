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

  <media:MediaAspectWrapper Grid.Row="0" Margin="-10,-15,0,0" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:MediaAspectWrapper.Template>
      <ControlTemplate>
        <Label x:Name="MediaItemName" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold"
               DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label.Content>
            <Binding Path="Title"/>
          </Label.Content>
        </Label>
      </ControlTemplate>
    </media:MediaAspectWrapper.Template>
  </media:MediaAspectWrapper>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Row="1"
        Scroll="Auto" ScrollSpeed="5" ScrollDelay="5"
        HorizontalAlignment="Stretch"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Margin="-10,0,5,25"
        Color="{ThemeResource TextColor}"/>

</Grid>
