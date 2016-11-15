<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="0,30,20,-200"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
  
  <media:AudioAlbumAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:AudioAlbumAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Height="800">
          <Grid.Resources>
            <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
            <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
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
                   Stretch="Fill" Margin="0,10,10,0" Height="320" Width="320">
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

          <Grid VerticalAlignment="Stretch" Grid.Column="0" Grid.Row="0" Margin="0,0,10,0">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <Label Grid.Row="0" Content="{Binding Path=Album}" Wrap="True" Margin="0,0,0,0" Color="{ThemeResource TextColor}" 
                   FontFamily="SegoeUIBold" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis"
                   IsVisible="{Binding Path=Album,Converter={StaticResource ReferenceToBoolConverter}}"/>
            
            <Label Grid.Row="1" Content="{Binding Path=Artists}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"
                   TextTrimming="WordEllipsis" Wrap="True" IsVisible="{Binding Path=Artists,Converter={StaticResource ReferenceToBoolConverter}}"/>
            
            <StackPanel Grid.Row="2" Orientation="Horizontal">
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

            <Label x:Name="Genre" Grid.Row="3" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis"
                   Opacity="1.3" Color="{ThemeResource MenuTextBackground}">
              <Label.Content>
                <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
                  <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
                  <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
                </MultiBinding>
              </Label.Content>
            </Label>

            <StackPanel Grid.Row="4" Orientation="Horizontal" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}"
                        IsVisible="{Binding Path=Tracks,Converter={StaticResource ReferenceToBoolConverter}}">
              <Label x:Name="NumTracks" Content="{Binding Tracks}" Opacity="1.3"
                     Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}"/>
              <Label Content=" " IsVisible="{Binding Path=Tracks,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Label x:Name="NumTrackLabel" Content="[Media.TrackItemsHeader]" Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}"
                     IsVisible="{Binding Path=Tracks,Converter={StaticResource ReferenceToBoolConverter}}" Opacity="1.3"/>
            </StackPanel>

            <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Grid.Row="5">
              <media:UserDataWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                        IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
                    <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Width="95" Height="95" Margin="0,10,10,10" >
                      <Image.Source>
                        <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}CircularBar\{0:F0}.png"/>
                      </Image.Source>
                    </Image>
                  </Grid>
                </ControlTemplate>
              </media:UserDataWrapper.Template>
            </media:UserDataWrapper>      
          </Grid>
          
          <Label x:Name="Description" Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2" Margin="0,40,0,20" TextTrimming="WordEllipsis"
                 HorizontalAlignment="Stretch" Wrap="True" Opacity="1.3"
                 Content="{Binding Path=Description}"  FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}"/>
        </Grid>
      </ControlTemplate>
    </media:AudioAlbumAspectWrapper.Template>
  </media:AudioAlbumAspectWrapper>


</Grid>
