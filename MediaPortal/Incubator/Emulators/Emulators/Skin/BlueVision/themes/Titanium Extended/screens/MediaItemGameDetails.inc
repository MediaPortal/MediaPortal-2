<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:emulators="clr-namespace:Emulators.MediaExtensions;assembly=Emulators"
  xmlns:aspect="clr-namespace:Emulators.Common.Games;assembly=Emulators.Common"
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

  <emulators:GameAspectWrapper Grid.Column="1" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
                               MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <emulators:GameAspectWrapper.IsVisible>
      <MultiBinding Converter="{StaticResource MediaItemAspectToBoolConverter}">
        <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem" />
        <Binding Source="{x:Static aspect:GameAspect.ASPECT_ID}" />
      </MultiBinding>
    </emulators:GameAspectWrapper.IsVisible>
    <emulators:GameAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel Margin="20,30,20,15" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Grid>
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
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

            <!--Label x:Name="MovieLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.VideoTitleLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="MovieName" Grid.Row="0" Grid.Column="1"
                Content="{Binding MovieName}" Color="{ThemeResource TextColor}"/-->

            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Margin="0,0,0,5" Grid.Row="1" Grid.ColumnSpan="2" Grid.Column="0" VerticalAlignment="Center">
              <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                                           Maximum="10"
                                           Value="{Binding Rating}">
                <StarRatingPanel.LayoutTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="1.65" ScaleY="1.5"/>
                  </TransformGroup>
                </StarRatingPanel.LayoutTransform>
              </StarRatingPanel>

              <Label x:Name="RatingCountLabel" Color="{ThemeResource EmphasisTextColor}"  Margin="8,0,0,5">
                <Label.Content>
                  <Binding Path="Rating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                </Label.Content>
              </Label>
            </StackPanel>

            <Label x:Name="PlatformLabel" Grid.Row="2" Grid.Column="0"
                Content="[Emulators.Platform]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Emulators.Platform" Grid.Row="2" Grid.Column="1"
                Content="{Binding Platform}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="DeveloperLabel" Grid.Row="3" Grid.Column="0"
                Content="[Emulators.Developer]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Developer" Grid.Row="3" Grid.Column="1"
                Content="{Binding Developer}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="YearLabel" Grid.Row="4" Grid.Column="0"
                Content="[Media.VideoYearLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Year" Grid.Row="4" Grid.Column="1"
                Content="{Binding Year}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="GenreLabel" Grid.Row="5" Grid.Column="0"
                Content="[Media.VideoGenreLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Genre" Grid.Row="5" Grid.Column="1"
                Content="{Binding Genres}" Color="{ThemeResource TextColor}"/>

          </Grid>

          <Label x:Name="StoryPlot" Wrap="True" Scroll="Auto" ScrollSpeed="5" Margin="0,20,0,0" Height="340"
              HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
              Content="{Binding Description}"
              Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>
        </StackPanel>
      </ControlTemplate>
    </emulators:GameAspectWrapper.Template>
  </emulators:GameAspectWrapper>

  <Grid Grid.Column="1" Margin="10,0,-350,-40" HorizontalAlignment="Center" VerticalAlignment="Bottom">
    <emulators:GameAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <emulators:GameAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="GameName" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                Content="{Binding GameName}" Color="Black" FontSize="{ThemeResource NormalFontSize}"
                HorizontalAlignment="Center"/>
        </ControlTemplate>
      </emulators:GameAspectWrapper.Template>
    </emulators:GameAspectWrapper>
  </Grid>

  <Image Grid.Column="0" HorizontalAlignment="Center" VerticalAlignment="Stretch" Stretch="Uniform" Margin="27,-7,17,30"
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
