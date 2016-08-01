<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="0,30,20,-200"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
  
  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="540"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Image Grid.Column="1" Grid.Row="0" HorizontalAlignment="Right" VerticalAlignment="Top" 
         Stretch="Fill" Margin="0,10,10,0" Width="210" Height="210">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"/>
    </Image.Source>
    <Image.OpacityMask>
      <VisualBrush>
        <VisualBrush.Visual>
          <Border Name="mask" Background="White" CornerRadius="6" BorderThickness="0" BorderBrush="Black"/>
        </VisualBrush.Visual>
      </VisualBrush>
    </Image.OpacityMask>
  </Image>
  
  <WrapPanel Orientation="Vertical" Grid.Column="0" Grid.Row="0">

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="TitleLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                Content="[Media.AudioTitleLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Title" Grid.Row="0" Grid.Column="1" Scroll="Auto" FontSize="{ThemeResource SmallFontSize}"
                Content="{Binding Title}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>

            <Label x:Name="ArtistsLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                Content="[Media.AudioArtistsLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Artists" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
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
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>            
            <Label x:Name="AlbumLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                Content="[Media.AudioAlbumLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Album" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
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
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="AlbumArtistsLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                Content="[Media.AudioAlbumArtistsLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="AlbumArtists" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
                Content="{Binding Path=AlbumArtists}" Color="{ThemeResource TextColor}"/>
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
            <Label x:Name="TrackLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                Content="[Media.AudioTrackLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Track" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
                Content="{Binding Path=Track}" Color="{ThemeResource TextColor}"/>
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
            <Label x:Name="DurationLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                Content="[Media.AudioDurationLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Duration" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
                Content="{Binding Path=Duration, Converter={StaticResource DurationConverter}, ConverterParameter=hh\:mm\:ss}" Color="{ThemeResource TextColor}"/>
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
            <Label x:Name="GenresLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                Content="[Media.AudioGenresLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Genres" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
                Content="{Binding Genres}" Color="{ThemeResource TextColor}"/>
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
            <Label x:Name="YearLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                Content="[Media.AudioYearLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Year" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
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
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>           
            <Label x:Name="BitrateLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                Content="[Media.AudioBitRateLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Bitrate" Grid.Row="0" Grid.Column="1" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}">
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
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="ComposersLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                Content="[Media.AudioComposersLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="Composers" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
                Content="{Binding Path=Composers}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="450"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="TypeLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                Content="[Media.AudioMimeTypeLabel]" Color="{ThemeResource TextColor}" Opacity="0.6"/>
            <Label x:Name="MimeType" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
                Content="{Binding Path=MimeType}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

  </WrapPanel>

  <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
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
  </media:MediaAspectWrapper>

          

  <!--Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Uniform" Margin="5"
      Width="380" Height="560"
      Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"/-->



</Grid>
