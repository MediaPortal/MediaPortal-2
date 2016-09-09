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

    <DockPanel DataContext="{Binding Source={Model 4CDD601F-E280-43b9-AD0A-6D7B2403C856},Path=NavigationData.CurrentScreenData}"
           VerticalAlignment="Top" HorizontalAlignment="Left" LastChildFill="false" Margin="70,-130,0,0"
           IsVisible="{Binding IsItemsValid}">
      <!---Grid+CoverView-->
      <StackPanel DockPanel.Dock="Left" Orientation="Horizontal">
        <Label HorizontalAlignment="Center" VerticalAlignment="Center" Margin="0,0,10,0" FontFamily="DefaultBold"
               Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}">
          <Label.Content>
            <MultiBinding Converter="{StaticResource GetIndexMultiConverter}" ConverterParameter="1">
              <Binding Source="{StaticResource FanArtBackgroundModel}" Path="SelectedItem"/>
              <Binding Source="{Binding ElementName=MediaItemsList}" Path="ItemsSource"/>
            </MultiBinding>
          </Label.Content>
        </Label>
        <Label Color="{ThemeResource TextColor}" Content="| " FontSize="{ThemeResource SmallFontSize}"
               Opacity="1"/>
        <Label Color="{ThemeResource TextColor}" Content="{Binding NumItems}" FontSize="{ThemeResource SmallFontSize}"
               Opacity="1"/>
      </StackPanel>
    </DockPanel>

    <Label IsVisible="{Binding ShowListHint}"
        HorizontalAlignment="Center" VerticalAlignment="Center" Wrap="true"
        Content="{Binding ListHint}" Color="{ThemeResource HintTextColor}"/>
  </Grid>
  <Label IsVisible="{Binding !IsItemsValid}"
      HorizontalAlignment="Center" VerticalAlignment="Center"
      Content="[Media.ViewInvalid]" Color="{ThemeResource ErrorTextColor}"/>
</Grid>
