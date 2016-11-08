<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>

  <media:AudioAlbumAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:AudioAlbumAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="550"/>
            <ColumnDefinition Width="*"/>
          </Grid.ColumnDefinitions>
          
          <StackPanel Orientation="Vertical">    
            <Label Content="{Binding Path=Album}"  Margin="0,0,-500,0" Color="{ThemeResource TextColor}" Wrap="True"
                   FontFamily="SegoeUIBold" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis"/>   
            <Label Content="{Binding Path=Artists}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
            <StackPanel Orientation="Horizontal">
              <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                                        IsVisible="{Binding Path=RecordingTime,Converter={StaticResource ReferenceToBoolConverter}}">
                <media:MediaAspectWrapper.Template>
                  <ControlTemplate>
                    <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
                      <Label x:Name="Year" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}" Opacity="1.2"
                          Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>
                    </Grid>
                  </ControlTemplate>
                </media:MediaAspectWrapper.Template>
              </media:MediaAspectWrapper>
              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,0,0,0"
                          IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                                 Maximum="5" VerticalAlignment="Center"
                                 IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
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
                       FontSize="{ThemeResource SmallerFontSize}" Margin="15,0,5,0" Opacity="1.2">
                  <Label.Content>
                    <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                  </Label.Content>
                </Label>
              </StackPanel>
            </StackPanel>

              <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis" Grid.Row="0" Grid.Column="1"
                     Opacity="1.3" Color="{ThemeResource MenuTextBackground}">
                <Label.Content>
                  <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
                    <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
                    <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
                  </MultiBinding>
                </Label.Content>
              </Label>
            
            <StackPanel Orientation="Horizontal" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}"
                        IsVisible="{Binding Path=Tracks,Converter={StaticResource ReferenceToBoolConverter}}">
              <Label x:Name="NumTracks" Content="{Binding Tracks}" Opacity="1.2"
                     Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}"/>
              <Label Content=" " IsVisible="{Binding Path=Tracks,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Label x:Name="NumTrackLabel" Content="[Media.TrackItemsHeader]" Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}" 
                     IsVisible="{Binding Path=Tracks,Converter={StaticResource ReferenceToBoolConverter}}" Opacity="1.2"/>
            </StackPanel>
          </StackPanel>

          <Label x:Name="Description" Grid.Row="0" Grid.Column="1" Margin="0,45,0,20" TextTrimming="WordEllipsis" 
                 VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Wrap="True" Opacity="1.2"
                 Content="{Binding Path=Description}"  FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}"/>
        </Grid>
      </ControlTemplate>
    </media:AudioAlbumAspectWrapper.Template>
  </media:AudioAlbumAspectWrapper>

</Grid>
