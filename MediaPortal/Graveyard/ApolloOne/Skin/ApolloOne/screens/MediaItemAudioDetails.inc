<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="2" Grid.Row="0" x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="400"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <StackPanel Grid.Column="1" Margin="10">
    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="160"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Label x:Name="YearLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioYearLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Year" Grid.Row="0" Grid.Column="1"
                Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="TitleLabel" Grid.Row="1" Grid.Column="0"
                Content="[Media.AudioTitleLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Title" Grid.Row="1" Grid.Column="1" Scroll="Auto"
                Content="{Binding Title}" Color="{ThemeResource TextColor}"/>

          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="160"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Label x:Name="ArtistsLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioArtistsLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Artists" Grid.Row="0" Grid.Column="1"
                Content="{Binding Artists}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="AlbumLabel" Grid.Row="1" Grid.Column="0"
                Content="[Media.AudioAlbumLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Album" Grid.Row="1" Grid.Column="1"
                Content="{Binding Album}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="GenresLabel" Grid.Row="2" Grid.Column="0"
                Content="[Media.AudioGenresLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Genres" Grid.Row="2" Grid.Column="1"
                Content="{Binding Genres}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="DurationLabel" Grid.Row="3" Grid.Column="0"
                Content="[Media.AudioDurationLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Duration" Grid.Row="3" Grid.Column="1"
                Content="{Binding Path=Duration, Converter={StaticResource DurationConverter}}" Color="{ThemeResource TextColor}"/>

          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>
  </StackPanel>
  <Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Uniform" Margin="5"
      Width="380" Height="560"
      Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"/>



</Grid>
