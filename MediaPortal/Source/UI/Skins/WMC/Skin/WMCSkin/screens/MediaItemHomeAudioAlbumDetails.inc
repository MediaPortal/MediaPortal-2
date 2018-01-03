<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="700"/>
  </Grid.ColumnDefinitions>
  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>
  
  
  <media:AudioAlbumAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:AudioAlbumAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                    HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

            <Label Content="{Binding Path=Album}" Wrap="True" Color="{ThemeResource TextColor}" 
                   FontFamily="SegoeUIBold" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis"
                   IsVisible="{Binding Path=Album,Converter={StaticResource ReferenceToBoolConverter}}"/>
            
            <Label Content="{Binding Path=Artists}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"
                   TextTrimming="WordEllipsis" Wrap="True" IsVisible="{Binding Path=Artists,Converter={StaticResource ReferenceToBoolConverter}}"/>
            
            <StackPanel Orientation="Horizontal">
              <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
                <media:MediaAspectWrapper.Template>
                  <ControlTemplate>
                    <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                          IsVisible="{Binding Path=RecordingTime,Converter={StaticResource ReferenceToBoolConverter}}">
                      <Label x:Name="Year" FontSize="{ThemeResource SmallFontSize}"
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

            <StackPanel Orientation="Horizontal" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}"
                        IsVisible="{Binding Path=Tracks,Converter={StaticResource ReferenceToBoolConverter}}">
              <Label x:Name="NumTracks" Content="{Binding Tracks}" Opacity="1.3"
                     Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}"/>
              <Label Content=" " IsVisible="{Binding Path=Tracks,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Label x:Name="NumTrackLabel" Content="[Media.TrackItemsHeader]" Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}"
                     IsVisible="{Binding Path=Tracks,Converter={StaticResource ReferenceToBoolConverter}}" Opacity="1.3"/>
            </StackPanel>

            <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:UserDataWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                        IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
                    <Image HorizontalAlignment="Left" VerticalAlignment="Top" Stretch="Fill" Width="85" Height="85" Margin="0,5,30,0">
                      <Image.Source>
                        <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}CircularBar\{0:F0}.png"/>
                      </Image.Source>
                    </Image>
                  </Grid>
                </ControlTemplate>
              </media:UserDataWrapper.Template>
            </media:UserDataWrapper>      

        </StackPanel>
      </ControlTemplate>
    </media:AudioAlbumAspectWrapper.Template>
  </media:AudioAlbumAspectWrapper>

</Grid>
