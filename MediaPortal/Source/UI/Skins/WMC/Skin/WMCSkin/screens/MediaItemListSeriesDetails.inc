<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
    xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="0,30,20,-200"
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
    <ColumnDefinition Width="590"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Image Grid.Column="1" HorizontalAlignment="Right" Stretch="Fill" 
         Margin="0,10,10,0" Width="270" Height="390">
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

  <StackPanel Grid.Column="0" Grid.Row="0" Margin="0,0,10,0" VerticalAlignment="Stretch">
    <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Stretch">
      <media:SeriesAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold" Wrap="True"
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
              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,4,0,2">
                <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                                 Maximum="5" VerticalAlignment="Center"
                                 IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
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
                <Label x:Name="RatingCountLabel" Margin="15,0,5,0" VerticalAlignment="Center"
                       FontSize="{ThemeResource SmallerFontSize}" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"
                       IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
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

    <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis"
           Opacity="1.3" Color="{ThemeResource MenuTextBackground}">
      <Label.Content>
        <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
          <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
          <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
        </MultiBinding>
      </Label.Content>
    </Label>

    <StackPanel Orientation="Vertical" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">
      <StackPanel Orientation="Horizontal">
        <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding Seasons}"
               Margin="0,0,8,0" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
        <Label FontSize="{ThemeResource SmallFontSize}" Content="[Media.SeasonItemsHeader]"
               IsVisible="{Binding Path=Seasons,Converter={StaticResource ReferenceToBoolConverter}}"
               Margin="0,0,8,0" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
      </StackPanel>
      <StackPanel Orientation="Horizontal">
        <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding Episodes}"
               Margin="0,0,8,0" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
        <Label FontSize="{ThemeResource SmallFontSize}" Content="[Media.EpisodeItemsHeader]"
               IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"
               Margin="0,0,8,0" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
      </StackPanel>
    </StackPanel>

    <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:UserDataWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
            <Image HorizontalAlignment="Left" VerticalAlignment="Center" Stretch="Fill" Width="95" Height="95" Margin="0,10,10,10" >
              <Image.Source>
                <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}CircularBar\{0:F0}.png"/>
              </Image.Source>
            </Image>
          </Grid>
        </ControlTemplate>
      </media:UserDataWrapper.Template>
    </media:UserDataWrapper>
    
  </StackPanel>
  
  <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Stretch"
                             Grid.Column="0" Grid.ColumnSpan="2" Grid.Row="1" Margin="0,30,10,10">
    <media:SeriesAspectWrapper.Template>
      <ControlTemplate>
        <Grid VerticalAlignment="Stretch">
          <Grid.RowDefinitions>
            <RowDefinition Height="440"/>
          </Grid.RowDefinitions>
          <Label x:Name="MediaItemDescription" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Grid.Row="1"
                   Wrap="True" TextTrimming="WordEllipsis" FontSize="{ThemeResource SmallFontSize}" Margin="0,5,5,0"
                   VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Content="{Binding Description}"
                   Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>          
        </Grid>
      </ControlTemplate>
    </media:SeriesAspectWrapper.Template>
  </media:SeriesAspectWrapper>
      
</Grid>
