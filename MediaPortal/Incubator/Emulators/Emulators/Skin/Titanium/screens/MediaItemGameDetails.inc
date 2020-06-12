<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:emulators="clr-namespace:Emulators.MediaExtensions;assembly=Emulators"
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

  <StackPanel Grid.Column="1" Margin="10" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <emulators:GameAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <emulators:GameAspectWrapper.Template>
        <ControlTemplate>
          <StackPanel DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
            <Grid>
              <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
              </Grid.RowDefinitions>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="180"/>
                <ColumnDefinition Width="*"/>
              </Grid.ColumnDefinitions>

              <Label x:Name="GameLabel" Grid.Row="0" Grid.Column="0"
                  Content="[Media.VideoTitleLabel]" Color="{ThemeResource EmphasisTextColor}"/>
              <Label x:Name="GameName" Grid.Row="0" Grid.Column="1"
                  Content="{Binding GameName}" Color="{ThemeResource TextColor}"/>

              <Label x:Name="RatingLabel" Grid.Row="1" Grid.Column="0"
                  Content="[Media.Rating]" Color="{ThemeResource EmphasisTextColor}"/>

              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Grid.Row="1" Grid.Column="1">
                <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                                             Maximum="10"
                                             Value="{Binding Rating}">
                  <StarRatingPanel.LayoutTransform>
                    <TransformGroup>
                      <ScaleTransform ScaleX="1.5" ScaleY="1.5"/>
                    </TransformGroup>
                  </StarRatingPanel.LayoutTransform>
                </StarRatingPanel>
              </StackPanel>

              <Label x:Name="PlatformLabel" Grid.Row="2" Grid.Column="0"
                  Content="[Emulators.Platform]" Color="{ThemeResource EmphasisTextColor}"/>
              <Label x:Name="Developer" Grid.Row="2" Grid.Column="1"
                  Content="{Binding Platform}" Color="{ThemeResource TextColor}"/>

              <Label x:Name="DeveloperLabel" Grid.Row="3" Grid.Column="0"
                  Content="[Emulators.Developer]" Color="{ThemeResource EmphasisTextColor}"/>
              <Label x:Name="Duration" Grid.Row="3" Grid.Column="1"
                  Content="{Binding Developer}" Color="{ThemeResource TextColor}"/>

              <Label x:Name="YearLabel" Grid.Row="4" Grid.Column="0"
                  Content="[Media.VideoYearLabel]" Color="{ThemeResource EmphasisTextColor}"/>
              <Label x:Name="Year" Grid.Row="4" Grid.Column="1"
                  Content="{Binding Year}" Color="{ThemeResource TextColor}"/>

            </Grid>

            <Label x:Name="Description" Wrap="True" Scroll="Auto" ScrollSpeed="5" Margin="0,10,0,0" Height="320"
                HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
                Content="{Binding Description}"
                Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>
          </StackPanel>
        </ControlTemplate>
      </emulators:GameAspectWrapper.Template>
    </emulators:GameAspectWrapper>
  </StackPanel>


  <Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Uniform" Margin="5"
      Width="380" Height="560"
      Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;560}">
  </Image>
</Grid>
