<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="0,30,20,-200"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
  
  <media:AudioAlbumAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:AudioAlbumAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Height="800">
          <Grid.Resources>
            <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
          </Grid.Resources>
          <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="540"/>
            <ColumnDefinition Width="350"/>
          </Grid.ColumnDefinitions>

            <Image VerticalAlignment="Top" HorizontalAlignment="Right" Grid.Column="1"
                   Stretch="Fill" Margin="0,10,0,0" Height="210" Width="210">
              <Image.Source>
                <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                                    Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                                    VerticalTextureAlignment="Top" HorizontalTextureAlignment="Right"/>
              </Image.Source>
              <Image.OpacityMask>
                <VisualBrush>
                  <VisualBrush.Visual>
                    <Border Name="mask" Background="White" CornerRadius="6" BorderThickness="0" BorderBrush="Black"/>
                  </VisualBrush.Visual>
                </VisualBrush>
              </Image.OpacityMask>
            </Image>

          <StackPanel Orientation="Vertical" VerticalAlignment="Stretch" Grid.Column="0" Grid.Row="0">
            <Label Content="{Binding Path=Album}"  Margin="0,0,0,0" Color="{ThemeResource TextColor}" FontFamily="SegoeUIBold" FontSize="{ThemeResource SmallFontSize}"/>
            <Label Content="{Binding Path=Artists}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
            <StackPanel Orientation="Horizontal">
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
              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,0,0,0">
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
                       FontSize="{ThemeResource SmallerFontSize}" Margin="15,0,5,0" Opacity="1.3"
                       IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Label.Content>
                    <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                  </Label.Content>
                </Label>
              </StackPanel>
            </StackPanel>

            <Label x:Name="Genres" Grid.Row="0" Grid.Column="1" Content="{Binding Genres}" Opacity="1.3"
                   Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}"/>

            <StackPanel Orientation="Horizontal">
              <Label x:Name="NumTracks" Content="{Binding NumTracks}" Opacity="1.3"
                     Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}"/>
              <Label x:Name="NumTrackLabel" Content=" Track(s)" Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}"
                     IsVisible="{Binding Path=NumTracks,Converter={StaticResource ReferenceToBoolConverter}}" Opacity="1.2"/>
            </StackPanel>
          </StackPanel>
          
          <Label x:Name="Description" Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2" Margin="0,40,0,20" TextTrimming="WordEllipsis"
                 HorizontalAlignment="Stretch" Wrap="True" Opacity="1.3"
                 Content="{Binding Path=Description}"  FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}"/>

        </Grid>
      </ControlTemplate>
    </media:AudioAlbumAspectWrapper.Template>
  </media:AudioAlbumAspectWrapper>


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
