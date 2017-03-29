<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
  
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="480"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>

  <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:AudioAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
          </Grid.ColumnDefinitions>
          <Label x:Name="Title" TextTrimming="WordEllipsis" FontFamily="SegoeUIBold"
              Content="{Binding TrackName}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
        </Grid>
      </ControlTemplate>
    </media:AudioAspectWrapper.Template>
  </media:AudioAspectWrapper>

  <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Grid.Row="1">
    <media:AudioAspectWrapper.Template>
      <ControlTemplate>
        <WrapPanel Orientation="Vertical" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
          <Label x:Name="Artists" FontSize="{ThemeResource SmallFontSize}"
                 Content="{Binding Artists}" Color="{ThemeResource TextColor}"/>
          <StackPanel Grid.Row="2" Orientation="Horizontal">
            <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:MediaAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                    <Label x:Name="Year" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
                           Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:MediaAspectWrapper.Template>
            </media:MediaAspectWrapper>
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center"
                        IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
              <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                               Maximum="5" VerticalAlignment="Center">
                <StarRatingPanel.Value>
                  <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/2">
                    <Binding Path="TotalRating"/>
                  </MultiBinding>
                </StarRatingPanel.Value>
                <StarRatingPanel.LayoutTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="1.6" ScaleY="1.5"/>
                  </TransformGroup>
                </StarRatingPanel.LayoutTransform>
              </StarRatingPanel>
              <Label x:Name="RatingCountLabel" Color="{ThemeResource MenuTextBackground}" VerticalAlignment="Center"
                     FontSize="{ThemeResource SmallerFontSize}" Margin="15,0,5,0" Opacity="1.3"
                     IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                <Label.Content>
                  <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                </Label.Content>
              </Label>
            </StackPanel>
          </StackPanel>

          <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis"
                 IsVisible="{Binding Source={StaticResource FanArtBackgroundModel}, Path=MediaItem, Converter={StaticResource ReferenceToBoolConverter}}"
                 Opacity="1.3" Color="{ThemeResource MenuTextBackground}">
            <Label.Content>
              <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
                <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
                <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
              </MultiBinding>
            </Label.Content>
          </Label>

          <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"
                 IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"
                 Content="{Binding Path=Duration, Converter={StaticResource DurationConverter}, ConverterParameter=mm\:ss}"/>
        </WrapPanel>
      </ControlTemplate>
    </media:AudioAspectWrapper.Template>
  </media:AudioAspectWrapper>
  
  <WrapPanel Orientation="Vertical" Grid.Column="1" Grid.Row="1">
      <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:AudioAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="270"/>
                <ColumnDefinition Width="480"/>
              </Grid.ColumnDefinitions>
              <Label x:Name="AlbumLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                  Content="[Media.AudioAlbumLabel]" Color="{ThemeResource MenuTextBackground}" Opacity="1.3"/>
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
              <ColumnDefinition Width="270"/>
              <ColumnDefinition Width="480"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="ComposersLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                   IsVisible="{Binding Path=Composers,Converter={StaticResource ReferenceToBoolConverter}}"
                   Content="[Media.AudioComposersLabel]" Color="{ThemeResource MenuTextBackground}" Opacity="1.3"/>
            <Label x:Name="Composers" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
                Content="{Binding Path=Composers}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="270"/>
              <ColumnDefinition Width="350"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="AlbumArtistsLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                Content="[Media.AudioAlbumArtistsLabel]" Color="{ThemeResource MenuTextBackground}" Opacity="1.3"/>
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
              <ColumnDefinition Width="270"/>
              <ColumnDefinition Width="350"/>
            </Grid.ColumnDefinitions>
            <Label Grid.Row="0" Grid.Column="0" Content="[Media.AudioTrackLabel]" Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}"
                   IsVisible="{Binding Path=Track,Converter={StaticResource ReferenceToBoolConverter}}" Opacity="1.3"/>
            <Label Grid.Row="0" Grid.Column="1" Content="{Binding Track}" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>
    
    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding Path=NumDiscs,Converter={StaticResource ReferenceToBoolConverter}}">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="270"/>
              <ColumnDefinition Width="350"/>
            </Grid.ColumnDefinitions>
            <Label Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                   Content="[Media.AudioDiscLabel]" Color="{ThemeResource MenuTextBackground}" Opacity="1.3"/>
            <StackPanel Orientation="Horizontal" Grid.Row="0" Grid.Column="1">
              <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding Path=DiscId}" Color="{ThemeResource TextColor}"/>
              <Label FontSize="{ThemeResource SmallFontSize}" Content="/" Color="{ThemeResource TextColor}"
                     IsVisible="{Binding Path=NumDiscs,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding Path=NumDiscs}" Color="{ThemeResource TextColor}"/>
            </StackPanel>
          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>

      <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:AudioAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="270"/>
                <ColumnDefinition Width="350"/>
              </Grid.ColumnDefinitions>
              <Label x:Name="BitrateLabel" Grid.Row="0" Grid.Column="0" FontSize="{ThemeResource SmallFontSize}"
                  Content="[Media.AudioBitRateLabel]" Color="{ThemeResource MenuTextBackground}" Opacity="1.3"/>
              <Label x:Name="Bitrate" Grid.Row="0" Grid.Column="1" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}">
                <Label.Content>
                  <Binding Path="BitRate" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0} kBit"/>
                </Label.Content>
              </Label>
            </Grid>
          </ControlTemplate>
        </media:AudioAspectWrapper.Template>
      </media:AudioAspectWrapper>

  </WrapPanel>

</Grid>
