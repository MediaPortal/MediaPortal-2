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
  
  <Grid Grid.Column="1" Margin="20,30,20,15">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="180"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <Label x:Name="CollectionLabel" Grid.Row="1" Grid.Column="0"
        Content="[Media.VideoCollectionLabel]" Color="{ThemeResource EmphasisTextColor}"/>
    <Label x:Name="Collection" Grid.Row="1" Grid.Column="1"
        Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=FanArtName}" Color="{ThemeResource TextColor}"/>
  </Grid>

  <Grid Grid.Column="1" Margin="10,0,-350,-40" HorizontalAlignment="Center" VerticalAlignment="Bottom">
    <media:MovieAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MovieAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="MovieName" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=FanArtName}" Color="Black" FontSize="{ThemeResource NormalFontSize}"
                HorizontalAlignment="Center"/>
        </ControlTemplate>
      </media:MovieAspectWrapper.Template>
    </media:MovieAspectWrapper>
  </Grid>

  <Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Fill" Margin="27,-7,17,30" 
         Width="380" Height="520">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;560}"/>
    </Image.Source>
    <Image.OpacityMask >
      <ImageBrush ImageSource="filmstrip_mask.png"/>
    </Image.OpacityMask>
  </Image>
</Grid>
