<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" MinHeight="630"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="400"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <StackPanel Grid.Column="1" Margin="20,30,20,15">
    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="250"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Label x:Name="YearLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.VideoYearLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Year" Grid.Row="0" Grid.Column="1"
                Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="TitleLabel" Grid.Row="1" Grid.Column="0"
                Content="[Media.ImageTitleLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Title" Grid.Row="1" Grid.Column="1" Scroll="Auto"
                Content="{Binding Title}" Color="{ThemeResource TextColor}"/>

          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:ImageAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:ImageAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="250"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>


            <Label x:Name="CameraMakeLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.CameraMake]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="CameraMake" Grid.Row="0" Grid.Column="1" Scroll="Auto"
                Content="{Binding EquipmentMake}"
                Color="{ThemeResource TextColor}"/>

            <Label x:Name="CameraModelLabel" Grid.Row="1" Grid.Column="0"
                Content="[Media.CameraModel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="CameraModel" Grid.Row="1" Grid.Column="1"
                Content="{Binding EquipmentModel}"
                Color="{ThemeResource TextColor}"/>

            <Label x:Name="ImageISOSpeedLabel" Grid.Row="2" Grid.Column="0"
                Content="[Media.ImageISOSpeedLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="ImageISOSpeed" Grid.Row="2" Grid.Column="1"
                Content="{Binding ISOSpeedRating}"
                Color="{ThemeResource TextColor}"/>

            <Label x:Name="ImageExposureTimeLabel" Grid.Row="3" Grid.Column="0"
                Content="[Media.ImageExposureTimeLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="ImageExposureTime" Grid.Row="3" Grid.Column="1"
                Content="{Binding ExposureTime}"
                Color="{ThemeResource TextColor}"/>

            <Label x:Name="ImageFNumberLabel" Grid.Row="4" Grid.Column="0"
                Content="[Media.ImageFNumberLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="ImageFNumber" Grid.Row="4" Grid.Column="1"
                Content="{Binding FNumber}"
                Color="{ThemeResource TextColor}"/>

            <Label x:Name="ImageMeteringModeLabel" Grid.Row="5" Grid.Column="0"
                Content="[Media.ImageMeteringModeLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="ImageMeteringMode" Grid.Row="5" Grid.Column="1"
                Content="{Binding MeteringMode}"
                Color="{ThemeResource TextColor}"/>

            <Label x:Name="ImageCountryLabel" Grid.Row="6" Grid.Column="0"
                Content="[Media.ImageCountryLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="ImageCountry" Grid.Row="6" Grid.Column="1"
                Content="{Binding Country}"
                Color="{ThemeResource TextColor}"/>

            <Label x:Name="ImageStateLabel" Grid.Row="7" Grid.Column="0"
                Content="[Media.ImageStateLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="ImageState" Grid.Row="7" Grid.Column="1"
                Content="{Binding State}"
                Color="{ThemeResource TextColor}"/>

            <Label x:Name="ImageCityLabel" Grid.Row="8" Grid.Column="0"
                Content="[Media.ImageCityLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="ImageCity" Grid.Row="8" Grid.Column="1"
                Content="{Binding City}"
                Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:ImageAspectWrapper.Template>
    </media:ImageAspectWrapper>
  </StackPanel>

  <Grid Grid.Column="1" Margin="10,0,-350,-40" HorizontalAlignment="Center" VerticalAlignment="Bottom">
    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <StackPanel Orientation="Horizontal" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="Imagetitle"
                  Content="{Binding Title}" Color="Black" FontSize="{ThemeResource NormalFontSize}"
                  HorizontalAlignment="Center"/>
          </StackPanel>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>
  </Grid>

  <Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Uniform" Margin="5"
      Width="330" Height="550">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5" VerticalTextureAlignment="Top"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"/>
    </Image.Source>
  </Image>
</Grid>
