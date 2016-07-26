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
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="350"/>
  </Grid.ColumnDefinitions>

  <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:SeriesAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Grid.Row="0" Grid.Column="0">
          <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
          </Grid.ColumnDefinitions>

          <StackPanel Orientation="Horizontal" Grid.Column="0" Grid.Row="0" Margin="-10,-15,0,0">
            <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontFamily="DefaultBold">
              <Label.Content>
                <Binding Path="SeriesName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0}"/>
              </Label.Content>
            </Label>

            <Label x:Name="SeasonLabel" Color="{ThemeResource TextColor}">
              <Label.Content>
                <Binding Path="Season" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{} - S{0} E"/>
              </Label.Content>
            </Label>

            <Label x:Name="EpisodeLabel" Color="{ThemeResource TextColor}">
              <Label.Content>
                <Binding Path="Episode"/>
              </Label.Content>
            </Label>

            <Label x:Name="EpisodeNameLabel" Color="{ThemeResource TextColor}">
              <Label.Content>
                <Binding Path="EpisodeName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}: {0}"/>
              </Label.Content>
            </Label>

          </StackPanel>

          <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="0" Grid.Row="1" 
                Scroll="Auto" ScrollSpeed="5" ScrollDelay="5" HorizontalAlignment="Stretch"
                Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
                Color="{ThemeResource TextColor}" Margin="-10,0,5,25"/>
        </Grid>
        </ControlTemplate>
    </media:SeriesAspectWrapper.Template>
  </media:SeriesAspectWrapper>
          
  <StackPanel Grid.Column="1" Grid.Row="0"  Margin="15,24,-20,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <Grid>
      <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
      </Grid.RowDefinitions>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="310"/>
        <ColumnDefinition Width="*"/>
      </Grid.ColumnDefinitions>

      <Grid Grid.Row="0" Grid.Column="0" HorizontalAlignment="Left" VerticalAlignment="Center">
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
      </Grid>

      <Grid Grid.Row="0" Grid.Column="1" HorizontalAlignment="Left" VerticalAlignment="Center">
        <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
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
        </media:MediaAspectWrapper>
      </Grid>

      <Grid Grid.Row="1" Grid.Column="0" VerticalAlignment="Center">
        <media:VideoAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
          <media:VideoAspectWrapper.Template>
            <ControlTemplate>
              <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                <Label x:Name="Genre"  Margin="0,0,-10,0"
                       Content="{Binding Genres}" Color="{ThemeResource TextColor}" />
              </Grid>
            </ControlTemplate>
          </media:VideoAspectWrapper.Template>
        </media:VideoAspectWrapper>
      </Grid>

      <Grid Grid.Row="2" Grid.Column="0" VerticalAlignment="Center">
        <media:VideoAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
          <media:VideoAspectWrapper.Template>
            <ControlTemplate>
              <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                <Label x:Name="Duration" 
                       Content="{Binding Path=Duration, Converter={StaticResource DurationConverter}, ConverterParameter=hh\:mm\:ss}" Color="{ThemeResource TextColor}"/>
              </Grid>
            </ControlTemplate>
          </media:VideoAspectWrapper.Template>
        </media:VideoAspectWrapper>
      </Grid>

      <Grid HorizontalAlignment="Left" Grid.Column="0" Grid.Row="3" VerticalAlignment="Center">
        <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
          <media:EpisodeAspectWrapper.Template>
            <ControlTemplate>
              <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                <StackPanel Orientation="Horizontal"  Margin="0,6,0,5">
                  <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                                               Maximum="10"
                                               Value="{Binding TotalRating}">
                    <StarRatingPanel.LayoutTransform>
                      <TransformGroup>
                        <ScaleTransform ScaleX="1.7" ScaleY="1.5"/>
                      </TransformGroup>
                    </StarRatingPanel.LayoutTransform>
                  </StarRatingPanel>
                </StackPanel>
              </Grid>
            </ControlTemplate>
          </media:EpisodeAspectWrapper.Template>
        </media:EpisodeAspectWrapper>
      </Grid>

      <Grid Grid.Row="4" Grid.Column="0">
        <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
          <media:UserDataWrapper.Template>
            <ControlTemplate>
              <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                <Label Color="{ThemeResource TextColor}">
                  <Label.Content>
                    <Binding Path="PlayPercentage"/>
                  </Label.Content>
                </Label>
              </Grid>
            </ControlTemplate>
          </media:UserDataWrapper.Template>
        </media:UserDataWrapper>
      </Grid>


    </Grid>
    
  </StackPanel>
  
</Grid>
