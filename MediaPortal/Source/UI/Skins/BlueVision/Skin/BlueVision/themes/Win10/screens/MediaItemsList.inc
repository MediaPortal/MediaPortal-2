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
  <Grid HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding IsItemsValid}" Margin="-50,5,20,35">
    <Grid.RowDefinitions>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <!--Border Grid.Row="0" Margin="-85,-35,20,5" >
      <Border.Background>
        <SolidColorBrush Color="Black" Opacity="0.6"/>
      </Border.Background>
      <Border.IsVisible>
        <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} != {LayoutType}.{CoverLayout}"/>
      </Border.IsVisible>
    </Border-->

    <Border Grid.Row="0" Margin="-92,-32,-710,8" BorderThickness="0">
      <Border.Background>
        <LinearGradientBrush StartPoint="0,0" EndPoint="1,0">
          <LinearGradientBrush.GradientStops>
            <GradientStop Color="#00000000" Offset="3"  />
            <GradientStop Color="#CC000000" Offset="0.55"  />
            <GradientStop Color="#CC000000" Offset="0"  />
          </LinearGradientBrush.GradientStops>
        </LinearGradientBrush>
      </Border.Background>
      <Border.IsVisible>
        <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} != {LayoutType}.{CoverLayout}"/>
      </Border.IsVisible>
    </Border>

    <Border Grid.Row="0" Margin="-90,-33,-155,7" >
      <Border.Background>
        <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
          <LinearGradientBrush.GradientStops>
            <GradientStop Color="#00000000" Offset="0"  />
            <GradientStop Color="#CC000000" Offset="1"  />
          </LinearGradientBrush.GradientStops>
        </LinearGradientBrush>
      </Border.Background>
      <Border.IsVisible>
        <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} == {LayoutType}.{CoverLayout}"/>
      </Border.IsVisible>
    </Border>

    <ListView x:Name="MediaItemsList" Style="{ThemeResource MediaItemsListListViewStyle}" ItemsSource="{Binding Items}"
        HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding ShowList}" Margin="-35,0,-110,20">
      <ListView.Resources>
        <!--<ResourceWrapper x:Key="VerticalScrollbarRenderTransform">
          <TranslateTransform X="40" />
        </ResourceWrapper>-->
        <Command x:Key="Menu_Command" Source="{StaticResource MediaModel}" Path="Select"
            Parameters="{LateBoundValue BindingValue={Binding}}"/>
      </ListView.Resources>
    </ListView>
    <Label IsVisible="{Binding ShowListHint}"
        HorizontalAlignment="Center" VerticalAlignment="Center" Wrap="true"
        Content="{Binding ListHint}" Color="{ThemeResource HintTextColor}"/>
  </Grid>
  <Label IsVisible="{Binding !IsItemsValid}"
      HorizontalAlignment="Center" VerticalAlignment="Center"
      Content="[Media.ViewInvalid]" Color="{ThemeResource ErrorTextColor}"/>
  <KeyBinding Key="F5" Command="{Command Reload}"/>
</Grid>
