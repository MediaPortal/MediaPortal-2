<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="2" Grid.Row="0" x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="380"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <StackPanel Grid.Column="1" Margin="20,30,20,15" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MovieAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="180"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!--Label x:Name="MovieLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.VideoTitleLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="MovieName" Grid.Row="0" Grid.Column="1"
                Content="{Binding MovieName}" Color="{ThemeResource TextColor}"/-->

            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Margin="0,0,0,5" Grid.Row="1" Grid.ColumnSpan="2" Grid.Column="0" VerticalAlignment="Center">
              <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                                           Maximum="10"
                                           Value="{Binding TotalRating}">
                <StarRatingPanel.LayoutTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="1.65" ScaleY="1.5"/>
                  </TransformGroup>
                </StarRatingPanel.LayoutTransform>
              </StarRatingPanel>

              <Label x:Name="RatingCountLabel" Color="{ThemeResource EmphasisTextColor}"  Margin="8,0,0,5">
                <Label.Content>
                  <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                </Label.Content>
              </Label>
            </StackPanel>

            <Label x:Name="CollectionLabel" Grid.Row="2" Grid.Column="0"
                Content="[Media.VideoCollectionLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Collection" Grid.Row="2" Grid.Column="1"
                Content="{Binding CollectionName}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="DurationLabel" Grid.Row="3" Grid.Column="0"
                Content="[Media.VideoDurationLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Duration" Grid.Row="3" Grid.Column="1"
                Content="{Binding Path=Runtime, Converter={StaticResource DurationConverter}, ConverterParameter=hh\:mm}" Color="{ThemeResource TextColor}"/>

          </Grid>
        </ControlTemplate>
      </media:MovieAspectWrapper.Template>
    </media:MovieAspectWrapper>

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="180"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Label x:Name="YearLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.VideoYearLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Year" Grid.Row="0" Grid.Column="1"
                Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>

          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:VideoAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:VideoAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="180"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Label x:Name="GenreLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.VideoGenreLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Genre" Grid.Row="0" Grid.Column="1"
                Content="{Binding Genres}" Color="{ThemeResource TextColor}"/>

          </Grid>
        </ControlTemplate>
      </media:VideoAspectWrapper.Template>
    </media:VideoAspectWrapper>

    <Label x:Name="StoryPlot" Wrap="True" Scroll="Auto" ScrollSpeed="5" Margin="0,20,0,0" Height="340"
        HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>
  </StackPanel>

  <Grid Grid.Column="1" Margin="10,0,-350,-40" HorizontalAlignment="Center" VerticalAlignment="Bottom">
    <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MovieAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="MovieName" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                Content="{Binding MovieName}" Color="Black" FontSize="{ThemeResource NormalFontSize}"
                HorizontalAlignment="Center"/>
        </ControlTemplate>
      </media:MovieAspectWrapper.Template>
    </media:MovieAspectWrapper>
  </Grid>

  <Image Grid.Column="0" HorizontalAlignment="Center" VerticalAlignment="Stretch" Stretch="Fill" Margin="27,-7,17,30"
      Width="380" Height="520">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                     Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;520;False}"/>
    </Image.Source>
    <Image.OpacityMask >
      <ImageBrush ImageSource="filmstrip_mask.png"/>
    </Image.OpacityMask>
  </Image>
</Grid>
