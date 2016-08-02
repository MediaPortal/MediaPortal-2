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

  <Label x:Name="MediaItemName" Wrap="True" Grid.Column="0" Grid.ColumnSpan="2" Grid.Row="0" Margin="0,-15,0,0"
    HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold"
    Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}"
    Color="{ThemeResource TextColor}"/>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="1" Grid.Row="1" Opacity="0.5" TextTrimming="WordEllipsis"
        HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Color="{ThemeResource TextColor}" Margin="10,0,5,35"/>

  <StackPanel Grid.Column="0" Grid.Row="1" Margin="0,0,30,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <StackPanel Orientation="Horizontal">
      <!--media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
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
      </media:MediaAspectWrapper-->

      <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Center" >
        <media:MovieAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" 
                          Grid.Column="0" Grid.Row="4" Margin="0,0,0,0">
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
                <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColor}"  Margin="15,0,5,0" VerticalAlignment="Center"
                       FontSize="{ThemeResource SmallerFontSize}" Opacity="0.5"
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
    
    <media:VideoAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:VideoAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}"
                Content="{Binding Genres}" Color="{ThemeResource TextColor}" Opacity="0.5"/>
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
                <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Opacity="0.5"
                       Content="{Binding Path=Runtime}" Color="{ThemeResource TextColor}"/>
                <Label Content=" min." FontSize="{ThemeResource SmallFontSize}" Opacity="0.5" Color="{ThemeResource TextColor}"
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
