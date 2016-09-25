<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
  <Grid.RowDefinitions>
    <RowDefinition Height="190"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="1650"/>
    <ColumnDefinition Width="100"/>
  </Grid.ColumnDefinitions>

  
  <WrapPanel Orientation="Vertical" Grid.Column="0" Grid.Row="0">

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="TitleLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioTitleLabel]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.2"/>
            <Label x:Name="Title" Grid.Row="0" Grid.Column="1" Scroll="Auto"
                Content="{Binding TrackName}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>

            <Label x:Name="ArtistsLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioArtistsLabel]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.2"/>
            <Label x:Name="Artists" Grid.Row="0" Grid.Column="1"
                Content="{Binding Artists}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="TrackLabel" Grid.Row="0" Grid.Column="0"
                   Content="[Media.AudioTrackLabel]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.2"/>
            <Label x:Name="Track" Grid.Row="0" Grid.Column="1"
                   Content="{Binding Path=Track}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="DurationLabel" Grid.Row="0" Grid.Column="0"
                   Content="[Media.AudioDurationLabel]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.2"/>
            <Label x:Name="Duration" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"
                   Content="{Binding Path=Duration, Converter={StaticResource DurationConverter}, ConverterParameter=mm\:ss}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="AlbumLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioAlbumLabel]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.2"/>
            <Label x:Name="Album" Grid.Row="0" Grid.Column="1"
                Content="{Binding Album}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="AlbumArtistsLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioAlbumArtistsLabel]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.2"/>
            <Label x:Name="AlbumArtists" Grid.Row="0" Grid.Column="1"
                Content="{Binding Path=AlbumArtists}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>
    
    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>            
            <Label x:Name="GenresLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioGenresLabel]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.2"/>
            <Label x:Name="Genres" Grid.Row="0" Grid.Column="1"
                Content="{Binding Genres}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="YearLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioYearLabel]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.2"/>
            <Label x:Name="Year" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"
                Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <!--media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="170"/>
              <ColumnDefinition Width="300"/>
            </Grid.ColumnDefinitions>           
            <Label x:Name="BitrateLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioBitRateLabel]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.2"/>
            <Label x:Name="Bitrate" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}">
              <Label.Content>
                  <Binding Path="BitRate" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0} kBit"/>
              </Label.Content>
            </Label> 
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="170"/>
              <ColumnDefinition Width="300"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="ComposersLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioComposersLabel]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.2"/>
            <Label x:Name="Composers" Grid.Row="0" Grid.Column="1"
                Content="{Binding Path=Composers}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper-->

    <!--media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="170"/>
              <ColumnDefinition Width="300"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="TypeLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioMimeTypeLabel]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.2"/>
            <Label x:Name="MimeType" Grid.Row="0" Grid.Column="1"
                Content="{Binding Path=MimeType}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper-->

  </WrapPanel>

  <!--media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:MediaAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Grid.Row="0" Grid.Column="1">
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
          </Grid.ColumnDefinitions>
          <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Width="37" Height="32" Margin="0,8,0,0" Grid.Row="0" Grid.Column="0"  >
            <Image.Source>
              <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} ? {1} : {2}">
                <Binding Path="PlayCount"/>
                <Binding Source=""/>
                <Binding Source="unheard_icon.png"/>
              </MultiBinding>
            </Image.Source>
          </Image>
         </Grid>
      </ControlTemplate>
    </media:MediaAspectWrapper.Template>
  </media:MediaAspectWrapper-->

</Grid>
