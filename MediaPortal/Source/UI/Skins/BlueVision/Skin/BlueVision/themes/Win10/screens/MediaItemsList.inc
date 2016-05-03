<?xml version="1.0" encoding="utf-8"?>
<Grid
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    DataContext="{Binding Source={Model 4CDD601F-E280-43b9-AD0A-6D7B2403C856},Path=NavigationData.CurrentScreenData}"
    > <!-- Model = MediaNavigationModel -->
  <Grid.Resources>
    <Model x:Key="MediaModel" Id="4CDD601F-E280-43b9-AD0A-6D7B2403C856"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  <Grid HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding IsItemsValid}" Margin="-50,5,20,44">
    <Grid.RowDefinitions>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <!-- All KeyBindings are included from shared resource -->
    <Include Source="screens\MediaItemsFilterKeyBindings.inc"/>

    <Image Source="win10_bg.png" Opacity="0.7" Margin="-35,-35,456,20" Stretch="Fill">
      <Image.IsVisible>
        <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} != {LayoutType}.{CoverLayout}"/>
      </Image.IsVisible>
    </Image>
    
    <!--Border Grid.Row="0" Margin="-35,-80,455,-20" >
      <Border.Background>
        <LinearGradientBrush StartPoint="0.5,0" EndPoint="0.5,1">
          <LinearGradientBrush.GradientStops>
            <GradientStop Color="#00444444" Offset="0.1"  />
            <GradientStop Color="#CC444444" Offset="0.2"  />
            <GradientStop Color="#CC444444" Offset="0.8"  />
            <GradientStop Color="#00444444" Offset="1"  />
          </LinearGradientBrush.GradientStops>
        </LinearGradientBrush>
      </Border.Background>
      <Border.IsVisible>
        <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} != {LayoutType}.{CoverLayout}"/>
      </Border.IsVisible>
    </Border-->

    <!--Rectangle Grid.Row="0" Fill="Black" Opacity="0.5" Margin="-37,13,41,-1">
      <Rectangle.Effect>
        <ZoomBlurEffect BlurAmount="0.02" CenterX="0.25" CenterY="0.5" />
      </Rectangle.Effect>
      <Rectangle.IsVisible>
        <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} != {LayoutType}.{CoverLayout}"/>
      </Rectangle.IsVisible>
    </Rectangle-->

    <!--Border Grid.Row="0" Margin="-30,20,50,6" >
      <Border.Background>
        <SolidColorBrush Color="{StaticResource GrayBackgroundWithBlurBehind}"/>
      </Border.Background>
      <Border.IsVisible>
        <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} != {LayoutType}.{CoverLayout}"/>
      </Border.IsVisible>
    </Border-->

    <ListView x:Name="MediaItemsList" Style="{ThemeResource MediaItemsListListViewStyle}" ItemsSource="{Binding Items}"
              HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding ShowList}" >
      <ListView.Resources>
        <Command x:Key="Menu_Command" Source="{StaticResource MediaModel}" Path="Select"
            Parameters="{LateBoundValue BindingValue={Binding}}"/>
      </ListView.Resources>
      <ListView.Margin>
        <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} == {LayoutType}.{ListLayout} ? {1} : ({0} == {LayoutType}.{GridLayout} ? {2} : {3})">
          <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType"/>
          <Binding Source="-29,22,-110,40"/>
          <Binding Source="-29,22,-110,-3"/>
          <Binding Source="-38,22,-120,-3"/>
          
        </MultiBinding>
      </ListView.Margin>
    </ListView>

    <Label IsVisible="{Binding ShowListHint}"
        HorizontalAlignment="Center" VerticalAlignment="Center" Wrap="true"
        Content="{Binding ListHint}" Color="{ThemeResource HintTextColor}"/>
  </Grid>
  <Label IsVisible="{Binding !IsItemsValid}"
      HorizontalAlignment="Center" VerticalAlignment="Center"
      Content="[Media.ViewInvalid]" Color="{ThemeResource ErrorTextColor}"/>
</Grid>
