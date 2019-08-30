<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto"/>
  </Grid.ColumnDefinitions>

  <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:MediaAspectWrapper.Template>
      <ControlTemplate>
        <Label x:Name="Title" Scroll="Auto" FontSize="{ThemeResource SmallerFontSize}" FontFamily="SegoeUIBold"
               DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
               Content="{Binding Title}" Color="{ThemeResource TextColor}" TextTrimming="CharacterEllipsis"/>
      </ControlTemplate>
    </media:MediaAspectWrapper.Template>
  </media:MediaAspectWrapper>

  <StackPanel Orientation="Horizontal" Grid.Row="1">
    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding ElementName=Year, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Image Source="HomeContentIcons\year.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="0,-1,0,1"/>
            <Label x:Name="Year" FontSize="{ThemeResource SmallerFontSize}" Grid.Column="1" Margin="5,0,0,0" Opacity="0.6"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=g}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>
            
    <!--Resolution-->
    <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:ImageAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding ElementName=Height, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Image Source="HomeContentIcons\resolution.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="20,-1,5,1"/>
            <Label Content="{Binding AspectWidth}" Grid.Column="1" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallerFontSize}" Opacity="0.6"/>
            <Label Content=" x " Grid.Column="2" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" Opacity="0.6"/>    
            <Label Content="{Binding AspectHeight}" Grid.Column="3" Color="{ThemeResource TextColor}" 
                   x:Name="Height" FontSize="{ThemeResource SmallerFontSize}" Opacity="0.6"/>
          </Grid>
        </ControlTemplate>
      </media:ImageAspectWrapper.Template>
    </media:ImageAspectWrapper>    

  </StackPanel>
</Grid>
