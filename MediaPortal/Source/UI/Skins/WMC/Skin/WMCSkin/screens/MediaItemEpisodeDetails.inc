<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="2" Grid.Row="2" x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="480"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Grid.Column="0" Grid.ColumnSpan="2" Grid.Row="0" Margin="0,-15,0,0">
    <media:EpisodeAspectWrapper.Template>
      <ControlTemplate> 
          <StackPanel Orientation="Horizontal">
            <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold"
                   DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Label.Content>
                <Binding Path="SeriesName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0}"/>
              </Label.Content>
            </Label>
            <Label x:Name="SeasonLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"
                   DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Label.Content>
                <Binding Path="Season" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{} - S{0} E"/>
              </Label.Content>
            </Label>
            <Label x:Name="EpisodeLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"
                   DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Label.Content>
                <Binding Path="Episode"/>
              </Label.Content>
            </Label>
            <Label x:Name="EpisodeNameLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"
                   DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Label.Content>
                <Binding Path="EpisodeName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}: {0}"/>
              </Label.Content>
            </Label>
          </StackPanel>
      </ControlTemplate>
    </media:EpisodeAspectWrapper.Template>
  </media:EpisodeAspectWrapper>

  <media:VideoAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Grid.Column="1" Grid.Row="1" Margin="10,0,5,35">
    <media:VideoAspectWrapper.Template>
      <ControlTemplate>
        <Label x:Name="Description" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" 
                 Wrap="True" Opacity="0.5" TextTrimming="WordEllipsis" FontSize="{ThemeResource SmallFontSize}"
                 HorizontalAlignment="Stretch" Content="{Binding StoryPlot}"
                 Color="{ThemeResource TextColor}"/>
       </ControlTemplate>
    </media:VideoAspectWrapper.Template>
  </media:VideoAspectWrapper>

  <StackPanel Grid.Column="0" Grid.Row="1" Margin="0,0,30,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <StackPanel Orientation="Horizontal">

      <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:EpisodeAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Label x:Name="FirstAired"
                Content="{Binding Path=FirstAired, Converter={StaticResource DateFormatConverter}, ConverterParameter=d}" Color="{ThemeResource TextColor}"/>
            </Grid>
          </ControlTemplate>
        </media:EpisodeAspectWrapper.Template>
      </media:EpisodeAspectWrapper>

      <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:EpisodeAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
              </Grid.RowDefinitions>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
              </Grid.ColumnDefinitions>
              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center"
                          Grid.Column="0" Grid.Row="4" Margin="0,4,0,3">
                <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                                 Maximum="5" VerticalAlignment="Center"
                                 IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}">
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
                <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColor}" Margin="15,0,5,0" VerticalAlignment="Center"
                       FontSize="{ThemeResource SmallerFontSize}" Opacity="0.5"
                       IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Label.Content>
                    <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                  </Label.Content>
                </Label>
              </StackPanel>
            </Grid>
          </ControlTemplate>
        </media:EpisodeAspectWrapper.Template>
      </media:EpisodeAspectWrapper>
    </StackPanel>

    <!--media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
          <media:MediaAspectWrapper.Template>
            <ControlTemplate>
              <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Width="37" Height="24" Margin="-5,0,15,0" >
                  <Image.Source>
                    <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} ? {1} : {2}">
                      <Binding Path="PlayCount"/>
                      <Binding Source=""/>
                      <Binding Source="unwatched_icon.png"/>
                    </MultiBinding>
                  </Image.Source>
                </Image>
              </Grid>
            </ControlTemplate>
          </media:MediaAspectWrapper.Template>
        </media:MediaAspectWrapper-->

        <media:VideoAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
          <media:VideoAspectWrapper.Template>
            <ControlTemplate>
              <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                <Label x:Name="Genre" Opacity="0.5"
                       Content="{Binding Genres}" Color="{ThemeResource TextColor}" />
              </Grid>
            </ControlTemplate>
          </media:VideoAspectWrapper.Template>
        </media:VideoAspectWrapper>

        <media:VideoStreamAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
          <media:VideoStreamAspectWrapper.Template>
            <ControlTemplate>
              <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                <StackPanel Orientation="Horizontal">
                  <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Opacity="0.5"
                         Color="{ThemeResource TextColor}">
                    <Label.Content>
                      <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                        <Binding Path="Duration"/>
                      </MultiBinding>
                    </Label.Content>
                  </Label>
                  <Label Content=" min." FontSize="{ThemeResource SmallFontSize}" Opacity="0.5" Color="{ThemeResource TextColor}"
                         IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
                </StackPanel>
              </Grid>
            </ControlTemplate>
          </media:VideoStreamAspectWrapper.Template>
        </media:VideoStreamAspectWrapper>
      
  </StackPanel>

</Grid>
