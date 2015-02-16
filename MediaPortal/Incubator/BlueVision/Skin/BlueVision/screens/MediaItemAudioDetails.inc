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

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="130"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="TitleLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioTitleLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Title" Grid.Row="0" Grid.Column="1" Scroll="Auto"
                Content="{Binding Title}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>
    
    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="130"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="YearLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioYearLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Year" Grid.Row="0" Grid.Column="1"
                Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="130"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>

            <Label x:Name="ArtistsLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioArtistsLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Artists" Grid.Row="0" Grid.Column="1"
                Content="{Binding Artists}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="130"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>            
            <Label x:Name="AlbumLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioAlbumLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Album" Grid.Row="0" Grid.Column="1"
                Content="{Binding Album}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="130"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>            
            <Label x:Name="GenresLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioGenresLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Genres" Grid.Row="0" Grid.Column="1"
                Content="{Binding Genres}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="130"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>            
            <Label x:Name="DurationLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioDurationLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Duration" Grid.Row="0" Grid.Column="1"
                Content="{Binding Path=Duration, Converter={StaticResource DurationConverter}}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="130"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>           
            <Label x:Name="BitrateLabel" Grid.Row="0" Grid.Column="0"
                Content="Bitrate" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Bitrate" Grid.Row="0" Grid.Column="1"
                Content="t.b.d." Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>
</WrapPanel>

  <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:MediaAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Grid.Row="0" Grid.Column="1">
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
          </Grid.ColumnDefinitions>
          <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Width="37" Height="24" Margin="0,8,0,0" Grid.Row="0" Grid.Column="0"  >
            <Image.Source>
              <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} ? {1} : {2}">
                <Binding Path="PlayCount"/>
                <Binding Source="watched_icon.png"/>
                <Binding Source="unwatched_icon.png"/>
              </MultiBinding>
            </Image.Source>
          </Image>
         </Grid>
      </ControlTemplate>
    </media:MediaAspectWrapper.Template>
  </media:MediaAspectWrapper>

          

  <!--Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Uniform" Margin="5"
      Width="380" Height="560"
      Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"/-->



</Grid>
