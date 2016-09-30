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
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="620"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Image Grid.Column="1" HorizontalAlignment="Right" VerticalAlignment="Stretch" Stretch="Fill" Margin="0,10,10,0"
    Width="190" Height="280">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                     Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;520}"/>
    </Image.Source>
    <Image.OpacityMask>
      <VisualBrush>
        <VisualBrush.Visual>
          <Border Name="mask" Background="White" CornerRadius="6" BorderThickness="0" BorderBrush="Black"/>
        </VisualBrush.Visual>
      </VisualBrush>
    </Image.OpacityMask>
  </Image>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="0"  Grid.ColumnSpan="2" Grid.Row="1"  TextTrimming="WordEllipsis"
        HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource MenuTextBackground}" Opacity="1.3"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Margin="0,30,10,10"/>

  <StackPanel Grid.Column="0" Grid.Row="0" Margin="0,0,10,0" VerticalAlignment="Stretch" Orientation="Vertical">

    <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Stretch">
      <media:MovieAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="MediaItemName" Wrap="True" Margin="0,0,0,0" TextTrimming="WordEllipsis"
                    VerticalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold"
                    Content="{Binding MovieName}"
                    Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MovieAspectWrapper.Template>
    </media:MovieAspectWrapper>
    
    <StackPanel Orientation="Horizontal">
      <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Center">
        <media:MovieAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" 
                          Margin="0,2,0,1">
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
                <Label x:Name="RatingCountLabel" Opacity="1.3" Color="{ThemeResource MenuTextBackground}" Margin="15,2,5,1" 
                       FontSize="{ThemeResource SmallerFontSize}" VerticalAlignment="Center"
                       IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Label.Content>
                    <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                  </Label.Content>
                </Label>
              </StackPanel>
            </Grid>
          </ControlTemplate>
        </media:MovieAspectWrapper.Template>
      </media:MovieAspectWrapper>      
    </StackPanel>

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
        <media:MediaAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
              </Grid.RowDefinitions>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
              </Grid.ColumnDefinitions>
              <Label x:Name="Year" HorizontalAlignment="Left" FontSize="{ThemeResource SmallFontSize}"
                  Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>
            </Grid>
          </ControlTemplate>
        </media:MediaAspectWrapper.Template>
      </media:MediaAspectWrapper>

    <media:VideoAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:VideoAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}"
                Content="{Binding Genres}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
          </Grid>
        </ControlTemplate>
      </media:VideoAspectWrapper.Template>
    </media:VideoAspectWrapper>

    <StackPanel Orientation="Horizontal">
      <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:MovieAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <StackPanel Orientation="Horizontal">
                <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"
                       IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}"
                       Content="{Binding Path=Runtime}"/>
                <Label Content=" " IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}"/>
                <Label Content="[Media.Minutes]" FontSize="{ThemeResource SmallFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"
                       IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}"/>
              </StackPanel>
            </Grid>
          </ControlTemplate>
        </media:MovieAspectWrapper.Template>
      </media:MovieAspectWrapper>

      <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
        <media:MediaAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Width="37" Height="24" Grid.Row="0" Grid.Column="1" Margin="0,0,20,0" >
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
    </StackPanel>
  </StackPanel>
</Grid>
