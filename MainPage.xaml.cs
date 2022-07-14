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

    //if message to add pins received from MainPageViewModel
    MessagingCenter.Subscribe<MessagingMarker, List<PostcodePosition>>(this, "PinAdded", (sender, arg) =>
    {
      foreach(PostcodePosition p in arg)
      {
        AddPin(p);
      }
    });

    Setup();
  }

  public async void Setup()
  {
    await GetLocation();
    DrawMap();
  }   
  public async Task GetLocation()
  {
    try
    {
      ActIndicator.IsRunning = true;
      location = new();
      location = await geolocation.GetLocationAsync(new GeolocationRequest
      {
        DesiredAccuracy = GeolocationAccuracy.Best,
        Timeout = TimeSpan.FromSeconds(30)
      });
      ActIndicator.IsRunning = false;
    }
    catch (Exception e)
    {
      Debug.WriteLine($"GM: Can't get location: {e.Message}");
    }
  }

  public void DrawMap()
  {
    IsBusy = true;

    var mapControl = new Mapsui.UI.Maui.MapControl();
    var map = mapControl.Map;
    mapControl.Info += MapControl_Info;  //click on map - doesn't work

    map?.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());

    //Navigate to my location
    var smc = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
    map.Home = n => n.NavigateTo(new MPoint(smc.x, smc.y), map.Resolutions[16]);  //0 zoomed out-19 zoomed in

    //link to xaml
    mapViewElement.Map = map;
    IsBusy = false;

    //add a pin
    var myPin = new Pin(mapViewElement)
    {
      Position = new Position(location.Latitude, location.Longitude),
      Type = PinType.Pin,
      Label = "Home",
      Address = "Home",
      Scale = 0.7F,
      Color = Colors.Blue,
    };
    mapViewElement.Pins.Add(myPin);
  }

  public void AddPin(PostcodePosition p)
  {
    var myPin = new Pin(mapViewElement)
    {
      Position = new Position(p.Latitude, p.Longitude),
      Type = PinType.Pin,
      Label = p.Postcode,
      Address = "",
      Scale = 0.7F,
      Color = Colors.Red,
    };
    mapViewElement.Pins.Add(myPin);
  }

  private void MapControl_Info(object sender, Mapsui.UI.MapInfoEventArgs e)
  {

  }
}

