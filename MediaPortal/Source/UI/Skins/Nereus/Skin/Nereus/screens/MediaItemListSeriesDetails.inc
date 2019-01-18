<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="0,260,20,-100"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="600"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Image Grid.Row="1" Grid.Column="1" HorizontalAlignment="Right" VerticalAlignment="Stretch" Stretch="Fill"
         Margin="0" Width="294" Height="428">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          Source="Poster_large.png"/>
    </Image.Source>
  </Image>
  <Image Grid.Row="1" Grid.Column="1" HorizontalAlignment="Right" VerticalAlignment="Stretch" Stretch="Fill"
         Margin="8" Width="278" Height="412">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;278;412}"/>
    </Image.Source>
  </Image>

  <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Grid.Column="1"
                             IsVisible="{Binding Source={StaticResource SkinSettingsModel}, Path=[WMC].EnableAgeCertificationLogos}">
    <media:SeriesAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              IsVisible="{Binding Path=Certification,Converter={StaticResource ReferenceToBoolConverter}}">
          <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
          </Grid.ColumnDefinitions>
          <Image HorizontalAlignment="Right" VerticalAlignment="Bottom" Stretch="Uniform" Height="80" MaxWidth="150" Margin="0,0,10,0">
            <Image.Source>
              <ImageSourceWrapper Delay="0" DelayInOut="false" VerticalTextureAlignment="Bottom" HorizontalTextureAlignment="Right">
                <ImageSourceWrapper.Source>
                  <Binding Path="Certification" Converter="{StaticResource SafeFilenameConverter}" ConverterParameter="{}Certification\{0}.png"/>
                </ImageSourceWrapper.Source>
              </ImageSourceWrapper>
            </Image.Source>
          </Image>
        </Grid>
      </ControlTemplate>
    </media:SeriesAspectWrapper.Template>
  </media:SeriesAspectWrapper>

  <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Stretch"
                             Grid.Column="0" Grid.Row="1" Margin="0,40,10,80">
    <media:SeriesAspectWrapper.Template>
      <ControlTemplate>
        <Grid VerticalAlignment="Stretch">
          <Label x:Name="MediaItemDescription" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Wrap="True" TextTrimming="WordEllipsis" FontSize="{ThemeResource SmallFontSize}" Opacity="0.6"
                 VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Content="{Binding Description}" Color="{ThemeResource TextColor}"/>
        </Grid>
      </ControlTemplate>
    </media:SeriesAspectWrapper.Template>
  </media:SeriesAspectWrapper>

  <StackPanel Grid.ColumnSpan="2" Grid.Row="0" Margin="0,0,10,0" VerticalAlignment="Stretch">
    
    <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Stretch">
      <media:SeriesAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource NormalFontSize}" FontFamily="SegoeUIBold" Wrap="True"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" VerticalAlignment="Stretch">
            <Label.Content>
              <Binding Path="SeriesName"/>
            </Label.Content>
          </Label>
        </ControlTemplate>
      </media:SeriesAspectWrapper.Template>
    </media:SeriesAspectWrapper>
    
    <StackPanel Orientation="Horizontal">
      <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:SeriesAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" VerticalAlignment="Center">
              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,2,0,1">
                <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                                 Maximum="5" VerticalAlignment="Center" IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
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
                <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColorForeground}" Margin="15,2,5,1"
                       FontSize="{ThemeResource SmallerFontSize}" IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Label.Content>
                    <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                  </Label.Content>
                </Label>
              </StackPanel>
            </Grid>
          </ControlTemplate>
        </media:SeriesAspectWrapper.Template>
      </media:SeriesAspectWrapper>
    </StackPanel>

    <StackPanel Orientation="Horizontal" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">
      <Image Source="HomeContentIcons\series.png" Height="25" Width="25" Stretch="Uniform" Margin="2,-1,5,1" VerticalAlignment="Center"/>
      <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding Seasons}"
             Margin="0,0,8,0" Color="{ThemeResource TextColorForeground}"/>
      <Label FontSize="{ThemeResource SmallFontSize}" Content="[Media.SeasonItemsHeader]"
             IsVisible="{Binding Path=Seasons,Converter={StaticResource ReferenceToBoolConverter}}"
             Margin="0,0,8,0" Color="{ThemeResource TextColorForeground}"/>
      <Label FontSize="{ThemeResource SmallerFontSize}" Content=" | " VerticalAlignment="Center"
             IsVisible="{Binding Path=Seasons,Converter={StaticResource ReferenceToBoolConverter}}"
             Margin="0,0,8,0" Color="{ThemeResource TextColorForeground}"/>
      <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding Episodes}"
             Margin="0,0,8,0" Color="{ThemeResource TextColorForeground}"/>
      <Label FontSize="{ThemeResource SmallFontSize}" Content="[Media.EpisodeItemsHeader]"
             IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"
             Margin="0,0,8,0" Color="{ThemeResource TextColorForeground}"/>
    </StackPanel>

    <StackPanel Orientation="Horizontal">
      <Image Source="HomeContentIcons\genre.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0" VerticalAlignment="Center"/>
      <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis" Color="{ThemeResource TextColorForeground}">
        <Label.Content>
          <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
            <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
            <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
          </MultiBinding>
        </Label.Content>
      </Label>
    </StackPanel>

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <StackPanel Orientation="Horizontal" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Image Source="HomeContentIcons\year.png" Height="27" Width="27" Stretch="Uniform" Margin="0,-1,5,1" VerticalAlignment="Center"/>
            <Label x:Name="Year" HorizontalAlignment="Left" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" />
          </StackPanel>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <StackPanel Orientation="Horizontal">
      <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:UserDataWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                  IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
              <Image VerticalAlignment="Center" Stretch="Fill" Width="95" Height="95" Margin="0,10,40,0" >
                <Image.Source>
                  <Binding Path="PlayPercentage" Converter="{StaticResource RoundingFormatConverter}" ConverterParameter="{}5;CircularBar\{0}.png"/>
                </Image.Source>
              </Image>
              <Label VerticalAlignment="Center" HorizontalAlignment="Center" FontSize="20" Color="White" FontFamily="SegoeUIBold"
                     Width="95" Height="95" Margin="0,0,40,0" HorizontalContentAlignment="Center" VerticalContentAlignment="Center">
                <Label.Content>
                  <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0:F0}%"/>
                </Label.Content>
              </Label>
            </Grid>
          </ControlTemplate>
        </media:UserDataWrapper.Template>
      </media:UserDataWrapper>

      <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                                 VerticalAlignment="Center" Margin="30,0,0,0">
        <media:SeriesAspectWrapper.Template>
          <ControlTemplate>
            <StackPanel Orientation="Vertical" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                        IsVisible="{Binding !IsEnded}">
              <StackPanel Orientation="Horizontal">
                <Label Content="Next Aired: " Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"
                       IsVisible="{Binding Path=NextAirDate,Converter={StaticResource ReferenceToBoolConverter}}"/>
                <Label VerticalAlignment="Stretch"
                       x:Name="NextAiredDate" Color="{ThemeResource TextColorForeground}" FontSize="{ThemeResource SmallFontSize}"
                       Content="{Binding Path=NextAirDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=d}">
                </Label>
              </StackPanel>
              <Label x:Name="NextEpisode" Color="{ThemeResource TextColorForeground}" FontSize="{ThemeResource SmallFontSize}"
                     Wrap="True" Content="{Binding NextEpisodeName}" VerticalAlignment="Stretch">
              </Label>             
            </StackPanel>

          </ControlTemplate>
        </media:SeriesAspectWrapper.Template>
      </media:SeriesAspectWrapper>
      <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Uniform" Height="140" Margin="60,-30,-280,-40">
        <Image.Source>
          <ImageSourceWrapper Delay="0.5" Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=ClearArt;140;240}"/>
        </Image.Source>
      </Image>
    </StackPanel>

  </StackPanel>
      
</Grid>
