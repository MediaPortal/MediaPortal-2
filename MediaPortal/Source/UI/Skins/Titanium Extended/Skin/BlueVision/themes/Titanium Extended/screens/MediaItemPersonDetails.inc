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
 
  <StackPanel Grid.Column="1" Margin="20,30,20,45" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <media:PersonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:PersonAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="220"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <Label Grid.Row="0" Grid.Column="0"
                   Content="[Media.DateBorn]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label Color="{ThemeResource TextColor}" Grid.Row="0" Grid.Column="1"
                   Content="{Binding Path=BornDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
            
            <Label Grid.Row="1" Grid.Column="0"
                   Content="[Media.DateDied]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label Color="{ThemeResource TextColor}" Grid.Row="1" Grid.Column="1"
                   Content="{Binding Path=DeathDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
            
            <Label Grid.Row="2" Grid.Column="0"
                   Content="[Media.Origin]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label Grid.Row="2" Grid.Column="1" Margin="0,0,65,0" TextTrimming="WordEllipsis"
                   Content="{Binding Origin}" Color="{ThemeResource TextColor}"/>
            
            <Label Grid.Row="3" Grid.ColumnSpan="2" Wrap="True" HorizontalAlignment="Stretch" Height="390"
                   VerticalAlignment="Stretch" TextTrimming="WordEllipsis" Margin="0,20,0,0"
                   Content="{Binding Biography}" Color="{ThemeResource TextColor}" FontSize="{ThemeResource NormalFontSize}"/>
          </Grid>
        </ControlTemplate>
      </media:PersonAspectWrapper.Template>
    </media:PersonAspectWrapper>
  </StackPanel>

  <Grid Grid.Column="1" Margin="10,0,-350,-40" HorizontalAlignment="Center" VerticalAlignment="Bottom">
    <media:PersonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:PersonAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="PersonName" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                Content="{Binding PersonName}" Color="Black" FontSize="{ThemeResource NormalFontSize}" TextTrimming="WordEllipsis"
                HorizontalAlignment="Center"/>
        </ControlTemplate>
      </media:PersonAspectWrapper.Template>
    </media:PersonAspectWrapper>
  </Grid>
  
  <Image Grid.Column="0" HorizontalAlignment="Center" VerticalAlignment="Top" Stretch="Fill" Margin="27,35,17,30"
         Width="350" Height="345">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" />
    </Image.Source>
    <Image.OpacityMask >
      <ImageBrush ImageSource="filmstrip_mask.png"/>
    </Image.OpacityMask>
  </Image>
</Grid>
