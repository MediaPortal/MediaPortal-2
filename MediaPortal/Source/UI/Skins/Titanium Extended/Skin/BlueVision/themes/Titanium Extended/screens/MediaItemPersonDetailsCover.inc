<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="2" Grid.Row="0" x:Name="MediaItemDetails"
  Margin="0,630,-1755,-40" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>

    <media:PersonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:PersonAspectWrapper.Template>
        <ControlTemplate>
          <Grid HorizontalAlignment="Stretch" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="80"/>
              <ColumnDefinition Width="1670"/>
              <ColumnDefinition Width="200"/>
            </Grid.ColumnDefinitions>         
            <Label Grid.Row="0" Grid.Column="1" x:Name="PersonName" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                  Content="{Binding PersonName}" Color="Black" FontSize="{ThemeResource NormalFontSize}" Margin="-20,0,20,0"
                  HorizontalAlignment="Center" VerticalAlignment="Bottom"/>          
          </Grid>  
        </ControlTemplate>
      </media:PersonAspectWrapper.Template>
    </media:PersonAspectWrapper>

</Grid>
