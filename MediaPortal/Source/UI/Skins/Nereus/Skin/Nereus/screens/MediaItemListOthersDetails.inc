<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="0,330,20,0">

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
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Image Source="HomeContentIcons\year.png" Height="27" Width="27" Stretch="Uniform" Margin="0,-1,5,1"
                   VerticalAlignment="Center" HorizontalAlignment="Left" IsVisible="{Binding Path=RecordingTime,Converter={StaticResource ReferenceToBoolConverter}}"/>
            <Label x:Name="Year" Grid.Column="1" HorizontalAlignment="Left" FontSize="{ThemeResource SmallFontSize}"
                   Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center" Margin="5,0,0,0" IsVisible="{Binding Path=RecordingTime,Converter={StaticResource ReferenceToBoolConverter}}"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=g}" />
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <Grid IsVisible="{Binding ElementName=Genre, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
      <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
      </Grid.RowDefinitions>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <Image Source="HomeContentIcons\genre.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0"
             VerticalAlignment="Center" HorizontalAlignment="Left"/>
      <Label x:Name="Genre" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis" Margin="5,0,0,0"
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
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <Image Source="HomeContentIcons\duration.png" Height="26" Width="26" Stretch="Uniform" Margin="1,-1,4,1"
                   VerticalAlignment="Center" HorizontalAlignment="Left" IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
            <StackPanel Orientation="Horizontal" Grid.Column="1" IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}" >
              <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}" Margin="5,0,0,0" VerticalAlignment="Center">
                <Label.Content>
                  <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                    <Binding Path="Duration"/>
                  </MultiBinding>
                </Label.Content>
              </Label>
              <Label Content=" " VerticalAlignment="Center"/>
              <Label Content="[Media.Minutes]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center"/>
            </StackPanel>
          </Grid>
        </ControlTemplate>
      </media:VideoStreamAspectWrapper.Template>
    </media:VideoStreamAspectWrapper>
    
      <StackPanel Orientation="Horizontal">
        <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Margin="30,-5,0,0">
          <media:UserDataWrapper.Template>
            <ControlTemplate>
              <Grid RenderTransformOrigin="0.5,0.5" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
                <Grid.RenderTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="0.7" ScaleY="0.7"/>
                  </TransformGroup>
                </Grid.RenderTransform>
                <Control Style="{ThemeResource CircularBackgroundStyle_0}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_18}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_36}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_54}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_72}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_90}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_108}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_126}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_144}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_162}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_180}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_198}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_216}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_234}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_252}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_270}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_288}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_306}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_324}"/>
                <Control Style="{ThemeResource CircularBackgroundStyle_342}"/>

                <Control Style="{ThemeResource CircularIndicatorStyle_0}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>0"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_18}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>5"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_36}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>10"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_54}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>15"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_72}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>20"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_90}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>25"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_108}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>30"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_126}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>35"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_144}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>40"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_162}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>45"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_180}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>50"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_198}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>55"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_216}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>60"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_234}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>65"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_252}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>70"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_270}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>75"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_288}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>80"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_306}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>85"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_324}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>90"/>
                  </Control.IsVisible>
                </Control>
                <Control Style="{ThemeResource CircularIndicatorStyle_342}">
                  <Control.IsVisible>
                    <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>95"/>
                  </Control.IsVisible>
                </Control>
                <Grid Margin="-70,-6,70,0">
                  <Grid.RowDefinitions>
                    <RowDefinition Height="*"/>
                  </Grid.RowDefinitions>
                  <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                  </Grid.ColumnDefinitions>
                  <Label VerticalAlignment="Center" HorizontalAlignment="Center" FontSize="28" Color="White" FontFamily="SegoeUIBold"
                         HorizontalContentAlignment="Center" VerticalContentAlignment="Center">
                    <Label.Content>
                      <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0:F0}%"/>
                    </Label.Content>
                  </Label>
                </Grid>
              </Grid>
            </ControlTemplate>
          </media:UserDataWrapper.Template>
        </media:UserDataWrapper>
        <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Uniform" Height="140" Margin="60,-40,-280,-40">
          <Image.Source>
            <ImageSourceWrapper Delay="0.5" Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=ClearArt;140;240}"/>
          </Image.Source>
        </Image>
      </StackPanel>

    </StackPanel>

  <media:VideoAspectWrapper Grid.Row="1" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:VideoAspectWrapper.Template>
      <ControlTemplate>
          <Label x:Name="StoryPlot" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"
                 Opacity="0.5" TextTrimming="WordEllipsis" Margin="0,0,15,25" Wrap="True" Content="{Binding Path=StoryPlot}"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"/>
      </ControlTemplate>
    </media:VideoAspectWrapper.Template>
  </media:VideoAspectWrapper>

  <Image x:Name="thumb" Stretch="Uniform" Grid.Row="1" Grid.Column="1" Margin="0,-120,10,20" VerticalAlignment="Bottom">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5" VerticalTextureAlignment="Bottom"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;520}"/>
    </Image.Source>
  </Image>
  <Image x:Name="fallback_thumb" Stretch="Uniform" Grid.Row="1" Grid.Column="1" Margin="0,-120,10,20" IsVisible="{Binding ElementName=thumb, Path=!HasImage}" VerticalAlignment="Bottom">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5" VerticalTextureAlignment="Bottom"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=MediaItem}"/>
    </Image.Source>
  </Image>

</Grid>
