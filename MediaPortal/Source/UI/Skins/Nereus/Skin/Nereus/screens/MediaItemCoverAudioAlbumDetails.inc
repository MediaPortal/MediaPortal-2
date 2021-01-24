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
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <media:AudioAlbumAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:AudioAlbumAspectWrapper.Template>
      <ControlTemplate>
        <Label x:Name="MediaItemDescription" Wrap="True" TextTrimming="WordEllipsis" Opacity="0.6" Height="260"
               FontSize="32" Color="{ThemeResource TextColor}" HorizontalContentAlignment="Right"
               Content="{Binding Description}" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="60,4,40,2"/>
      </ControlTemplate>
    </media:AudioAlbumAspectWrapper.Template>
  </media:AudioAlbumAspectWrapper>

  <media:AudioAlbumAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                                 Grid.Column="1" VerticalAlignment="Top">
    <media:AudioAlbumAspectWrapper.Template>
      <ControlTemplate>

          <StackPanel Orientation="Vertical" Margin="10,0,10,0" VerticalAlignment="Top" 
                      DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">

            <Label Content="{Binding Path=Album}" Wrap="True" Margin="0,0,0,0" Color="{ThemeResource TextColor}"
                   FontFamily="SegoeUIBold" FontSize="{ThemeResource NormalFontSize}" TextTrimming="WordEllipsis"
                   IsVisible="{Binding Path=Album,Converter={StaticResource ReferenceToBoolConverter}}"/>

            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,0,0,3">
              <Label Content="{Binding Path=Artists}" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColor}" VerticalAlignment="Center"
                     TextTrimming="WordEllipsis" Wrap="True" IsVisible="{Binding Path=Artists,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                               IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}"
                               Maximum="5" VerticalAlignment="Center" Margin="15,0,0,0">
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
              <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center"
                     FontSize="{ThemeResource SmallerFontSize}" Margin="15,3,5,1"
                     IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                <Label.Content>
                  <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                </Label.Content>
              </Label>
            </StackPanel>

            <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
              <media:MediaAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                        IsVisible="{Binding ElementName=Year, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
                    <Image Source="HomeContentIcons\year.png" Height="27" Width="27" Stretch="Uniform" Margin="0,-1,5,1"
                           VerticalAlignment="Center" HorizontalAlignment="Left"/>
                    <Label x:Name="Year" FontSize="{ThemeResource SmallFontSize}" VerticalAlignment="Center" Margin="40,0,0,0"
                           Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColorForeground}"/>
                  </Grid>
                </ControlTemplate>
              </media:MediaAspectWrapper.Template>
            </media:MediaAspectWrapper>

            <Grid IsVisible="{Binding ElementName=Genre, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
              <Image Source="HomeContentIcons\audiogenre.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0"
                     VerticalAlignment="Center" HorizontalAlignment="Left"/>
              <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis"
                     Color="{ThemeResource TextColorForeground}" Margin="40,0,0,0">
                <Label.Content>
                  <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
                    <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
                    <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
                  </MultiBinding>
                </Label.Content>
              </Label>
            </Grid>

            <Grid DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}"
                  IsVisible="{Binding Path=Tracks,Converter={StaticResource ReferenceToBoolConverter}}">
              <Image Source="HomeContentIcons\track.png" Height="29" Width="28" Stretch="Uniform" Margin="0,0,5,0"
                     VerticalAlignment="Center" HorizontalAlignment="Left"/>
              <StackPanel Orientation="Horizontal" Margin="40,0,0,0">
                <Label x:Name="NumTracks" Content="{Binding Tracks}" VerticalAlignment="Center"
                       Color="{ThemeResource TextColorForeground}" FontSize="{ThemeResource SmallFontSize}"/>
                <Label Content=" " IsVisible="{Binding Path=Tracks,Converter={StaticResource ReferenceToBoolConverter}}" VerticalAlignment="Center"/>
                <Label x:Name="NumTrackLabel" Content="[Media.TrackItemsHeader]" Color="{ThemeResource TextColorForeground}" FontSize="{ThemeResource SmallFontSize}"
                       IsVisible="{Binding Path=Tracks,Converter={StaticResource ReferenceToBoolConverter}}" VerticalAlignment="Center"/>
              </StackPanel>
            </Grid>
            
          </StackPanel>

      </ControlTemplate>
    </media:AudioAlbumAspectWrapper.Template>
  </media:AudioAlbumAspectWrapper>

</Grid>
