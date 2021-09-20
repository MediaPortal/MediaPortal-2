<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="0,390,20,-60">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <EmptyString2FalseConverter x:Key="EmptyStringToFalseConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="600"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Image Grid.Row="1" Grid.Column="1" HorizontalAlignment="Stretch" VerticalAlignment="Bottom" Stretch="Uniform" Margin="0,10,10,40" Height="300">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;520}"/>
    </Image.Source>
  </Image>

  <StackPanel Orientation="Vertical" Margin="0,0,20,0" VerticalAlignment="Stretch">

    <Label x:Name="MediaItemName" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold"
           DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}" Content="{Binding SimpleTitle}" Margin="0,0,-100,0"
           IsVisible="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem,Converter={StaticResource ReferenceToBoolConverter}}"/>

    <Grid DataContext="{Binding Source={StaticResource FanArtBackgroundModel}}"
          IsVisible="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem,Converter={StaticResource ReferenceToBoolConverter}}">
      <Image Source="HomeContentIcons\category.png" Height="27" Width="27" Stretch="Uniform" Margin="0,-1,5,1"
             IsVisible="{Binding Path=NumItems,Converter={StaticResource ReferenceToBoolConverter}}"
             VerticalAlignment="Center" HorizontalAlignment="Left"/>
      <StackPanel Orientation="Horizontal">
        <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding NumItems}"
               IsVisible="{Binding Path=NumItems,Converter={StaticResource ReferenceToBoolConverter}}"
               Margin="40,0,8,0" Color="{ThemeResource TextColorForeground}"/>
        <Label FontSize="{ThemeResource SmallFontSize}" Content="[Media.MediaItemsHeader]"
               Margin="0,0,8,0" Color="{ThemeResource TextColorForeground}"
               IsVisible="{Binding Path=NumItems,Converter={StaticResource ReferenceToBoolConverter}}"/>
      </StackPanel>
    </Grid>

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding Path=RecordingTime,Converter={StaticResource ReferenceToBoolConverter}}">
            <Image Source="HomeContentIcons\year.png" Height="27" Width="27" Stretch="Uniform" Margin="0,-1,5,1"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="Year" HorizontalAlignment="Left" FontSize="{ThemeResource SmallFontSize}"
                   Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center" Margin="40,0,0,0"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=g}" />
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <Grid IsVisible="{Binding ElementName=Genre, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
      <Image Source="HomeContentIcons\genre.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0"
             VerticalAlignment="Center" HorizontalAlignment="Left"/>
      <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis" Margin="40,0,0,0"
             Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center">
        <Label.Content>
          <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
            <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
            <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}"/>
          </MultiBinding>
        </Label.Content>
      </Label>
    </Grid>

    <media:VideoStreamAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                                    VerticalAlignment="Center">
      <media:VideoStreamAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}">

            <Image Source="HomeContentIcons\duration.png" Height="26" Width="26" Stretch="Uniform" Margin="1,-1,4,1"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <StackPanel Orientation="Horizontal" Grid.Column="1">
              <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}" Margin="40,0,0,0"
                     IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}" VerticalAlignment="Center">
                <Label.Content>
                  <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                    <Binding Path="Duration"/>
                  </MultiBinding>
                </Label.Content>
              </Label>
              <Label Content=" " IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}" VerticalAlignment="Center"/>
              <Label Content="[Media.Minutes]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"
                     IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}" VerticalAlignment="Center"/>
            </StackPanel>
          </Grid>
        </ControlTemplate>
      </media:VideoStreamAspectWrapper.Template>
    </media:VideoStreamAspectWrapper>
    
    <StackPanel Orientation="Horizontal">
      <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:UserDataWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                  IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
              <Image VerticalAlignment="Center" Stretch="Fill" Width="95" Height="95" Margin="0,10,40,0" >
                <Image.Source>
                  <Binding Path="PlayPercentage" Converter="{StaticResource RoundingFormatConverter}" ConverterParameter="{}5;CircularBar\{0}.png"/>
                </Image.Source>
              </Image>
              <Label VerticalAlignment="Center" HorizontalAlignment="Center" FontSize="20" Color="White" FontFamily="SegoeUIBold"
                     Width="95" Height="95" Margin="0,0,40,0" HorizontalContentAlignment="Center" VerticalContentAlignment="Center">
                <Label.Content>
                  <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0:F0}%"/>
                </Label.Content>
              </Label>
            </Grid>
          </ControlTemplate>
        </media:UserDataWrapper.Template>
      </media:UserDataWrapper>
      <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Uniform" Height="140" Margin="60,-30,-280,-40">
        <Image.Source>
          <ImageSourceWrapper Delay="0.5" Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=ClearArt;140;240}"/>
        </Image.Source>
      </Image>
    </StackPanel>
  </StackPanel>

  <media:VideoAspectWrapper Grid.Row="1" Grid.ColumnSpan="2" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:VideoAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel Orientation="Horizontal">
          <Label x:Name="StoryPlot" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"
                 Opacity="0.5" TextTrimming="WordEllipsis" Margin="0,40,5,25"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label.Content>
              <Binding Path="StoryPlot"/>
            </Label.Content>
          </Label>
        </StackPanel>
      </ControlTemplate>
    </media:VideoAspectWrapper.Template>
  </media:VideoAspectWrapper>

</Grid>
