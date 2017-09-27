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
  </Grid.ColumnDefinitions>
  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>

  <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Grid.ColumnSpan="2">
    <media:AudioAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
              HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
          </Grid.ColumnDefinitions>
          <Label x:Name="Title" TextTrimming="WordEllipsis" FontFamily="SegoeUIBold"
              Content="{Binding TrackName}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}"/>
        </Grid>
      </ControlTemplate>
    </media:AudioAspectWrapper.Template>
  </media:AudioAspectWrapper>

  <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Grid.Row="1"
                            HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <media:AudioAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel Orientation="Vertical" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                    HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

          <Label x:Name="Album" FontSize="{ThemeResource SmallFontSize}"
                 Content="{Binding Album}" Color="{ThemeResource TextColor}"/>
          <Label x:Name="Artists" FontSize="{ThemeResource SmallFontSize}" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
                 Content="{Binding Artists}" Color="{ThemeResource TextColor}"/>

          <StackPanel Orientation="Horizontal" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
            <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:MediaAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
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

          <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"
                 IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}" HorizontalAlignment="Stretch"
                 Content="{Binding Path=Duration, Converter={StaticResource DurationConverter}, ConverterParameter=mm\:ss}"/>

          <StackPanel Orientation="Horizontal">
            <Label Content="[Media.TrackMenuItem]" Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}"
                   IsVisible="{Binding Path=Track,Converter={StaticResource ReferenceToBoolConverter}}" Opacity="1.2"/>
            <Label Content="{Binding Track}" Color="{ThemeResource MenuTextBackground}" FontSize="{ThemeResource SmallFontSize}" Opacity="1.2"/>
          </StackPanel>

        </StackPanel>
      </ControlTemplate>
    </media:AudioAspectWrapper.Template>
  </media:AudioAspectWrapper>
  


</Grid>
