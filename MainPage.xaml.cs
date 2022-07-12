using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.UI.Maui;

namespace RouteIt;

public partial class MainPage : ContentPage
{
  IGeolocation geolocation;

  public Location location;

  public MainPage(MainPageViewModel viewModel, IGeolocation geolocation)
	{
		InitializeComponent();
    BindingContext = viewModel;

    this.geolocation = geolocation;
    location = new();
    GetLocation();
  }

  public async void GetLocation()
  {
    try
    {
      IsBusy = true;
      location = await geolocation.GetLocationAsync(new GeolocationRequest
      {
        DesiredAccuracy = GeolocationAccuracy.Best,
        Timeout = TimeSpan.FromSeconds(30)
      });

      var smc = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
      
      var mapControl = new Mapsui.UI.Maui.MapControl();
      var map = mapControl.Map;

      map.BackColor = Mapsui.Styles.Color.Black;

      map?.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
      
      map.Widgets.Add(new ScaleBarWidget(map));
      map.Widgets.Add(new ZoomInOutWidget { MarginX = 10, MarginY = 20 });  //adds the +/- zoom widget



      map.Home = n => n.NavigateTo(new MPoint(smc.x, smc.y), map.Resolutions[16]);  //0 zoomed out-19 zoomed in
      IsBusy = false;

      var layer = new GenericCollectionLayer<List<IFeature>>
      {
        Style = SymbolStyles.CreatePinStyle()
      };
      map.Layers.Add(layer);

      layer?.Features.Add(new GeometryFeature
      {

      });

      layer?.DataHasChanged();

      //Add to my xaml
      MapDisplay.Content = mapControl;

    }
    catch (Exception e)
    {
      Debug.WriteLine($"GM: Can't query location: {e.Message}");
      await Application.Current.MainPage.DisplayAlert("GM: Error", e.Message, "OK");
    }
  }

}

