<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" MinHeight="630"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>
  
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="380"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Grid.Column="1" Margin="20,30,20,15" >
    <media:SeasonAspectWrapper.Template>
      <ControlTemplate>
        <Grid HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
          <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="190"/>
            <ColumnDefinition Width="500"/>
          </Grid.ColumnDefinitions>

          <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                     Grid.Column="1" Grid.Row="0" Grid.RowSpan="3" HorizontalAlignment="Right" VerticalAlignment="Top">
            <media:UserDataWrapper.Template>
              <ControlTemplate>
                <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                      IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Width="95" Height="95" Margin="0,0,0,0" >
                    <Image.Source>
                      <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}CircularBar\{0:F0}.png"/>
                    </Image.Source>
                  </Image>
                </Grid>
              </ControlTemplate>
            </media:UserDataWrapper.Template>
          </media:UserDataWrapper>

          <Label x:Name="SeriesLabel" Grid.Row="0" Grid.Column="0" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Content="[Media.SeriesTitleLabel]" Color="{ThemeResource EmphasisTextColor}"/>
          <Label x:Name="Series" Grid.Row="0" Grid.Column="1" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Content="{Binding SeriesName}" Color="{ThemeResource TextColor}" TextTrimming="WordEllipsis" Margin="0,0,65,0"/>

          <StackPanel Orientation="Horizontal" Grid.Row="1" Grid.Column="0">
            <Label x:Name="SeasonLabel" Content="[Media.SeasonRootViewName]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label Content=":" Color="{ThemeResource EmphasisTextColor}"/>
          </StackPanel>
          <Label x:Name="Season" Grid.Row="1" Grid.Column="1" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Content="{Binding Season}" Color="{ThemeResource TextColor}"/>

          <StackPanel Orientation="Horizontal" Grid.Row="2" Grid.Column="0">
            <Label x:Name="EpisodeLabel" Content="[Media.EpisodeItemsHeader]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label Content=":" Color="{ThemeResource EmphasisTextColor}"/>
          </StackPanel>
          <Label x:Name="Episode" Grid.Row="2" Grid.Column="1" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}"
              Content="{Binding Episodes}" Color="{ThemeResource TextColor}"/>
         
          <Label x:Name="MediaItemDescription" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Grid.Row="3" Grid.ColumnSpan="2"
                   Wrap="True" TextTrimming="WordEllipsis" Margin="0,30,0,0" Color="{ThemeResource TextColor}"
                   VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Content="{Binding Description}"/>
        </Grid>
      </ControlTemplate>
    </media:SeasonAspectWrapper.Template>
  </media:SeasonAspectWrapper>

  <Grid Grid.Column="1" Margin="10,0,-350,-40" HorizontalAlignment="Center" VerticalAlignment="Bottom">
    <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:SeasonAspectWrapper.Template>
        <ControlTemplate>
          <StackPanel Orientation="Horizontal" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="SeasonLabel" Content="[Media.SeasonRootViewName]" Color="Black"
                   FontSize="{ThemeResource NormalFontSize}"/>
            <Label Content=" " Color="Black" FontSize="{ThemeResource NormalFontSize}"/>
            <Label x:Name="Season" Content="{Binding Season}" Color="Black" FontSize="{ThemeResource NormalFontSize}"/>
          </StackPanel>
        </ControlTemplate>
      </media:SeasonAspectWrapper.Template>
    </media:SeasonAspectWrapper>
  </Grid>

  <Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Fill" Margin="27,-7,17,30"
      Width="380" Height="520">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;520}">
      </ImageSourceWrapper>
    </Image.Source>
    <Image.OpacityMask >
      <ImageBrush ImageSource="filmstrip_mask.png"/>
    </Image.OpacityMask>
  </Image>
</Grid>
