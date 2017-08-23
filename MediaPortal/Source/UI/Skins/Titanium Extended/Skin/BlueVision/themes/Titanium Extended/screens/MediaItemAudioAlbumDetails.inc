<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  x:Name="MediaItemDetails" MinHeight="630"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
  
  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <media:AudioAlbumAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                                 HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <media:AudioAlbumAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
          <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="400"/>
            <ColumnDefinition Width="*"/>
          </Grid.ColumnDefinitions>

          <Grid Grid.Column="1" Margin="30,30,20,45" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Grid.ColumnSpan="2" Margin="0,0,0,5">
              <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                               Maximum="10" VerticalAlignment="Center">
                <StarRatingPanel.Value>
                  <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}">
                    <Binding Path="TotalRating"/>
                  </MultiBinding>
                </StarRatingPanel.Value>
                <StarRatingPanel.LayoutTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="1.6" ScaleY="1.5"/>
                  </TransformGroup>
                </StarRatingPanel.LayoutTransform>
              </StarRatingPanel>
              <Label x:Name="RatingCountLabel" Color="{ThemeResource EmphasisTextColor}" VerticalAlignment="Center" Margin="15,0,5,0"
                     IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                <Label.Content>
                  <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                </Label.Content>
              </Label>
            </StackPanel>

            <Label x:Name="ArtistsLabel" Grid.Row="1" Grid.Column="0"
                   Content="[Media.AudioArtistsLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Artists" Grid.Row="1" Grid.Column="1" Margin="0,0,65,0" TextTrimming="WordEllipsis"
                   Content="{Binding Artists}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="YearLabel" Grid.Row="2" Grid.Column="0"
                   Content="[Media.AudioYearLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Grid.Row="2" Grid.Column="1">
              <media:MediaAspectWrapper.Template>
                <ControlTemplate>
                  <Label x:Name="Year" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                      Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>
                </ControlTemplate>
              </media:MediaAspectWrapper.Template>
            </media:MediaAspectWrapper>

            <Label x:Name="GenreLabel" Grid.Row="3" Grid.Column="0"
                   Content="[Media.AudioGenresLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Genre" TextTrimming="WordEllipsis" Color="{ThemeResource TextColor}"
                   Grid.Row="3" Grid.Column="1">
              <Label.Content>
                <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
                  <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
                  <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
                </MultiBinding>
              </Label.Content>
            </Label>

            <StackPanel Orientation="Horizontal" Grid.Row="4" Grid.Column="0">
              <Label x:Name="NumTrackLabel" Content="[Media.TrackItemsHeader]" Color="{ThemeResource EmphasisTextColor}"/>
              <Label Content=": " Color="{ThemeResource EmphasisTextColor}"/>
            </StackPanel>
            <Label x:Name="NumTracks" Grid.Row="4" Grid.Column="1" TextTrimming="WordEllipsis" Color="{ThemeResource TextColor}"
                   DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}" Content="{Binding Tracks}">
            </Label>

            <Label x:Name="Description" Grid.Row="5" Grid.Column="0" Grid.ColumnSpan="2" Margin="0,40,0,20" TextTrimming="WordEllipsis"
                   HorizontalAlignment="Stretch" Wrap="True" Content="{Binding Path=Description}" Color="{ThemeResource TextColor}"/>
          </Grid>

          <Grid Grid.Column="1" Grid.Row="0" Margin="10,0,-350,-40" HorizontalAlignment="Center" VerticalAlignment="Bottom">
            <Label x:Name="AlbumLabel" HorizontalAlignment="Center"
                   Content="{Binding Album}" Color="Black" FontSize="{ThemeResource NormalFontSize}" TextTrimming="WordEllipsis"/>
          </Grid>
        
          <Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Top" Stretch="Fill" Margin="20,40,5,5"
              Width="380" Height="380">
            <Image.Source>
              <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                                  FallbackSource="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                                  Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource}"/>
            </Image.Source>
            <Image.OpacityMask >
              <ImageBrush ImageSource="music_filmstrip_mask.png"/>
            </Image.OpacityMask>
          </Image>
        </Grid>
      </ControlTemplate>
    </media:AudioAlbumAspectWrapper.Template>
  </media:AudioAlbumAspectWrapper>
</Grid>
