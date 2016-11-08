<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="0,30,20,-230"
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

  <Image Grid.Column="1" HorizontalAlignment="Right" VerticalAlignment="Stretch" Stretch="Fill"
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

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="0"  Grid.ColumnSpan="2" Grid.Row="1" TextTrimming="WordEllipsis"
        HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}" Opacity="1.2"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
        Color="{ThemeResource MenuTextBackground}" Margin="0,30,10,10"/>

  <StackPanel Grid.Column="0" Grid.Row="0" Margin="0,0,10,0" VerticalAlignment="Stretch" Orientation="Vertical">

    <media:MovieCollectionAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Stretch" >
      <media:MovieCollectionAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="MediaItemName" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold"
                   Content="{Binding CollectionName}" Color="{ThemeResource TextColor}" TextTrimming="WordEllipsis"/>
          </Grid>
        </ControlTemplate>
      </media:MovieCollectionAspectWrapper.Template>
    </media:MovieCollectionAspectWrapper>

    <StackPanel HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
      <StackPanel Orientation="Horizontal" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">
        <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding Movies}"
               Margin="0,0,8,0" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
        <Label FontSize="{ThemeResource SmallFontSize}" Content="[Media.MoviesItemsHeader]"
               IsVisible="{Binding Path=Movies,Converter={StaticResource ReferenceToBoolConverter}}"
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

</Grid>
