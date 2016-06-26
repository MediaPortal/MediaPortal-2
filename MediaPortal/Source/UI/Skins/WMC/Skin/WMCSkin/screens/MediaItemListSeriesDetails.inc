<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="0,30,20,-200"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="540"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Image Grid.Column="1" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Fill" Margin="0,10,10,0" Height="210">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"/>
    </Image.Source>
    <Image.OpacityMask>
      <VisualBrush>
        <VisualBrush.Visual>
          <Border Name="mask" Background="White" CornerRadius="6" BorderThickness="0" BorderBrush="Black"/>
        </VisualBrush.Visual>
      </VisualBrush>
    </Image.OpacityMask>
  </Image>

  <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" 
                             Grid.Column="0" Grid.Row="0" Margin="0,0,10,0">
    <media:SeriesAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel Orientation="Vertical" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
          <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" Wrap="True"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" TextTrimming="WordEllipsis">
            <Label.Content>
              <Binding Path="SeriesName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0}"/>
            </Label.Content>
          </Label>
          <Label x:Name="SeasonLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label.Content>
              <Binding Path="Season" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}Season {0}"/>
            </Label.Content>
          </Label>

          <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
            <Label Content="Episode " Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" VerticalAlignment="Center"/>
            <Label x:Name="EpisodeLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"
                   DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" VerticalAlignment="Center">
              <Label.Content>
                <Binding Path="Episode"/>
              </Label.Content>
            </Label>
          </StackPanel>

          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="FirstAired" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"
              Content="{Binding Path=FirstAired, Converter={StaticResource DateFormatConverter}, ConverterParameter=d}"/>
          </Grid>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" VerticalAlignment="Center">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,4,0,2">
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
              <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColor}" Margin="15,0,5,0" VerticalAlignment="Center"
                     FontSize="{ThemeResource SmallerFontSize}" Opacity="0.5">
                <Label.Content>
                  <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                </Label.Content>
              </Label>
            </StackPanel>
          </Grid>
        </StackPanel>
      </ControlTemplate>
    </media:SeriesAspectWrapper.Template>
  </media:SeriesAspectWrapper>

  <StackPanel Orientation="Vertical" Grid.Column="0" Grid.ColumnSpan="2" Grid.Row="1" Margin="0,10,0,10">
    <media:VideoAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:VideoAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="Genre" Opacity="0.5" FontSize="{ThemeResource SmallFontSize}"
                   Content="{Binding Genres}" Color="{ThemeResource TextColor}" />
          </Grid>
        </ControlTemplate>
      </media:VideoAspectWrapper.Template>
    </media:VideoAspectWrapper>

    <media:VideoAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:VideoAspectWrapper.Template>
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
              <Label Content=" min." FontSize="{ThemeResource SmallFontSize}" Opacity="0.5" Color="{ThemeResource TextColor}"/>
            </StackPanel>
          </Grid>
        </ControlTemplate>
      </media:VideoAspectWrapper.Template>
    </media:VideoAspectWrapper>
  </StackPanel>
  
  <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" 
                             Grid.Column="0" Grid.ColumnSpan="2" Grid.Row="2" Margin="0,20,0,10">
    <media:SeriesAspectWrapper.Template>
      <ControlTemplate>
        <Grid>
          <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Label x:Name="EpisodeNameLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Grid.Row="0">
            <Label.Content>
              <Binding Path="EpisodeName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0}"/>
            </Label.Content>
          </Label>
          <Label x:Name="MediaItemDescription" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Grid.Row="1"
                   Wrap="True" Opacity="0.5" TextTrimming="WordEllipsis" FontSize="{ThemeResource SmallFontSize}" Margin="0,5,5,0"
                   HorizontalAlignment="Stretch" Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
                   Color="{ThemeResource TextColor}"/>          
        </Grid>
      </ControlTemplate>
    </media:SeriesAspectWrapper.Template>
  </media:SeriesAspectWrapper>

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
      
</Grid>
