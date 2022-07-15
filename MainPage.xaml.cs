using Mapsui.UI.Maui;
using Mapsui.Utilities;
using Microsoft.Maui.Devices.Sensors;
using System.Text;

namespace RouteIt;

public partial class MainPage : ContentPage
{
  IGeolocation geolocation;

  public Location location;

  public Mapsui.UI.Maui.MapControl mapControl;

  public double minLat, maxLat, minLng, maxLng;

  public MainPage(MainPageViewModel viewModel, IGeolocation geolocation)
	{
		InitializeComponent();
    BindingContext = viewModel;

    this.geolocation = geolocation;

    //----------------------------------------------------------------
    //if message to add pins received from MainPageViewModel
    MessagingCenter.Subscribe<MessagingMarker, List<PostcodePosition>>(this, "PinAdded", (sender, arg) =>
    {
      //get min max coords for a bounding box, set max, min cooordinates to be the first one in the list
      minLat = arg[0].Latitude;
      maxLat = arg[0].Latitude;
      minLng = arg[0].Longitude;
      maxLng = arg[0].Longitude;
      foreach(PostcodePosition p in arg)
      {
        if (p.Latitude < minLat) minLat = p.Latitude;
        if (p.Latitude > maxLat) maxLat = p.Latitude;
        if (p.Longitude < minLng) minLng = p.Longitude;
        if (p.Longitude > maxLng) maxLng = p.Longitude;
        AddPin(p, Colors.Red);
      }
      var bl = SphericalMercator.FromLonLat(minLng, minLat*.99999);
      var tr = SphericalMercator.FromLonLat(maxLng, maxLat*1.00005);
      var smc = SphericalMercator.FromLonLat((minLng+maxLng)/2.0, (minLat+maxLat)/2.0);

      MRect mrect = new(bl.x, bl.y, tr.x, tr.y);

      //mapViewElement.Navigator.NavigateTo(new MPoint(smc.x, smc.y), mapControl.Map.Resolutions[14]);  //0 zoomed out-19 zoomed in
      mapViewElement.Navigator.NavigateTo(mrect, ScaleMethod.Fit);  //0 zoomed out-19 zoomed in
 
    });

    MessagingCenter.Subscribe<MessagingMarker, List<PostcodePosition>>(this, "RouteIt", (sender, arg) =>
    {
      DrawPolyLine(arg, Colors.Green);
    });

    MessagingCenter.Subscribe<MessagingMarker, List<PostcodePosition>>(this, "ClearMap", (sender, arg) =>
    {
      //Clear pins
      mapViewElement.Pins.Clear();

      //Navigate to my location
      var smc = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
      mapViewElement.Navigator.NavigateTo(new MPoint(smc.x, smc.y), mapControl.Map.Resolutions[16]);  //0 zoomed out-19 zoomed in
    });

    MessagingCenter.Subscribe<MessagingMarker, List<PostcodePosition>>(this, "ClearRoute", (sender, arg) =>
    {
      mapViewElement.Drawables.Clear();
    });

      //----------------------------------------------------------------------

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
    ActIndicator.IsRunning = true;

    //setup Mapsui map
    mapControl = new Mapsui.UI.Maui.MapControl();
    mapControl.Map?.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
    
    //link to xaml
    mapViewElement.Map = mapControl.Map;
    ActIndicator.IsRunning = false;
    
    //Navigate to my location
    var smc = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
    mapViewElement.Navigator.NavigateTo(new MPoint(smc.x, smc.y), mapControl.Map.Resolutions[16]);  //0 zoomed out-19 zoomed in

    //Add a home pin
    PostcodePosition p = new()
    {
      Latitude = location.Latitude,
      Longitude = location.Longitude,
      Postcode = "Home"
    };
    AddPin(p, Colors.Blue);

  }

  public void AddPin(PostcodePosition p, Color c)
  {
    var myPin = new Pin(mapViewElement)
    {
      Position = new Position(p.Latitude, p.Longitude),
      Type = PinType.Pin,
      Label = p.Postcode,
      Address = "",
      Scale = 0.7F,
      Color = c,
    };
    mapViewElement.Pins.Add(myPin);
  }

  public void DrawLine(double x1, double y1, double x2, double y2, Color c)
  {

  }

  public void DrawPolyLine(List<PostcodePosition> pp, Color c)
  {

    Polyline pl = new Polyline { StrokeWidth = 4, StrokeColor = c };
    
    foreach (PostcodePosition p in pp)
    {
      pl.Positions.Add(new(p.Latitude, p.Longitude));
    }
    //and return home
    pl.Positions.Add(new(pp[0].Latitude, pp[0].Longitude));

    mapViewElement.Drawables.Add(pl);

  }

  //https://stackoverflow.com/questions/3852268/c-sharp-implementation-of-googles-encoded-polyline-algorithm

}

