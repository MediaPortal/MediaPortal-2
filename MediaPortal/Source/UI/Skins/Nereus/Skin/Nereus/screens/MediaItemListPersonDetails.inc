<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  x:Name="MediaItemDetails" Margin="0,300,20,-10"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>

  <media:PersonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                             HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Height="900">
    <media:PersonAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" VerticalAlignment="Stretch">
          <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="456"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="474"/>
            <ColumnDefinition Width="456"/>
          </Grid.ColumnDefinitions>

          <Grid Grid.Column="1" Grid.Row="1" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Height="456">
            <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Margin="0" Width="456" Height="456">
              <Image.Source>
                <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                                    Source="Square_large.png"/>
              </Image.Source>
            </Image>
            <Image x:Name="Person" HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Margin="8" Width="440" Height="440">
              <Image.Source>
                <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                                    Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"/>
              </Image.Source>
            </Image>
            <Image HorizontalAlignment="Center" VerticalAlignment="Center" Stretch="Uniform" Width="200" Height="200"
                   Source="person.png" IsVisible="{Binding ElementName=Person, Path=!HasImage}"/>
          </Grid>

          <Label x:Name="MediaItemDescription" Grid.Row="1" Margin="0,0,10,30" Height="456"
                 Wrap="True" TextTrimming="WordEllipsis" FontSize="{ThemeResource SmallFontSize}"
                 VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Content="{Binding Biography}"
                 Color="{ThemeResource MenuTextBackground}"/>

          <StackPanel Orientation="Vertical" Grid.Row="0" Grid.Column="0" Margin="0,0,20,0">
            <Label VerticalAlignment="Stretch" Content="{Binding PersonName}" TextTrimming="WordEllipsis" Margin="0,0,-200,0"
                   Color="{ThemeResource TextColor}" FontSize="{ThemeResource NormalFontSize}" FontFamily="SegoeUIBold" Wrap="True"/>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">
              <Image Source="HomeContentIcons\year.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0"
                     IsVisible="{Binding Path=BornDate,Converter={StaticResource ReferenceToBoolConverter}}"
                     VerticalAlignment="Center" HorizontalAlignment="Left"/>
              <Image Source="HomeContentIcons\year.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0"
                     IsVisible="{Binding Path=DeathDate,Converter={StaticResource ReferenceToBoolConverter}}"
                     VerticalAlignment="Center" HorizontalAlignment="Left"/>
              <StackPanel Orientation="Horizontal" Margin="40,0,0,0">
                <Label FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"
                       Content="{Binding Path=BornDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
                <Label FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"
                       Content=" - " IsVisible="{Binding Path=DeathDate,Converter={StaticResource ReferenceToBoolConverter}}"/>
                <Label FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"
                       Content="{Binding Path=DeathDate, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
              </StackPanel>
            </Grid>

            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                  IsVisible="{Binding Path=Origin,Converter={StaticResource ReferenceToBoolConverter}}">
              <Image Source="HomeContentIcons\place.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0"
                     VerticalAlignment="Center" HorizontalAlignment="Left"/>
              <Label Content="{Binding Origin}" FontSize="{ThemeResource SmallFontSize}" Margin="40,0,0,0"
                     Color="{ThemeResource TextColorForeground}" TextTrimming="WordEllipsis"/>
            </Grid>
          </StackPanel>

        </Grid>
      </ControlTemplate>
    </media:PersonAspectWrapper.Template>
</media:PersonAspectWrapper>
      
</Grid>
