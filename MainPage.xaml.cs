namespace RouteIt;

public partial class MainPage : ContentPage
{
  IGeolocation geolocation;

  public Location location;

  public MapControl mapControl;

  public double minLat, maxLat, minLng, maxLng;

  public RouterDb routerDb;
  public Router router;
  public Route route;
  public Profile profile;
  public RouterPoint start;
  public RouterPoint end;

  public MainPage(MainPageViewModel viewModel, IGeolocation geolocation)
	{
		InitializeComponent();
    BindingContext = viewModel;

    this.geolocation = geolocation;

    //----------------------------------------------------------------

    //pass a copy? reference? of routeDb from ViewModel where it is defined
    MessagingCenter.Subscribe<MessagingMarker, RouterDb>(this, "RouterDbLoaded", (sender, arg) =>
    {
      Debug.WriteLine("Router data loaded");
      if (routerDb == null)
      {
        ActIndicator.IsRunning = true;
        routerDb = arg;
        router = new(routerDb);
        profile = Itinero.Osm.Vehicles.Vehicle.Car.Fastest();
        ActIndicator.IsRunning = false;
      }
    });

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

      mapViewElement.Navigator.NavigateTo(mrect, ScaleMethod.Fit);  //0 zoomed out-19 zoomed in
 
    });

    MessagingCenter.Subscribe<MessagingMarker, List<PostcodePosition>>(this, "RouteIt", (sender, arg) =>
    {
      Debug.WriteLine("Route it");
      DrawPolyLine(arg, Colors.Green);
    });

    MessagingCenter.Subscribe<MessagingMarker, List<PostcodePosition>>(this, "ClearMap", (sender, arg) =>
    {
      //Clear pins
      Debug.WriteLine("Clear map");
      mapViewElement.Pins.Clear();

      //Navigate to my location
      var smc = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
      mapViewElement.Navigator.NavigateTo(new MPoint(smc.x, smc.y), mapControl.Map.Resolutions[16]);  //0 zoomed out-19 zoomed in
    });

    MessagingCenter.Subscribe<MessagingMarker, List<PostcodePosition>>(this, "ClearRoute", (sender, arg) =>
    {
      Debug.WriteLine("Clear route");
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

  public void DrawPolyLine(List<PostcodePosition> pp, Color c)
  {
    Polyline pl = new Polyline { StrokeWidth = 4, StrokeColor = c };

    for (int i = 0; i < pp.Count-1; i++)
    {
      start = router.Resolve(profile, (float)pp[i].Latitude, (float)pp[i].Longitude);
      end = router.Resolve(profile, (float)pp[i+1].Latitude, (float)pp[i+1].Longitude);

      route = router.Calculate(profile, start, end); 

      for (int j = 0; j<route.Shape.Length; j++)
      {
        pl.Positions.Add(new((float)route.Shape[j].Latitude, (float)route.Shape[j].Longitude));
      }
    }
    //do return home
    start = router.Resolve(profile, (float)pp[pp.Count - 1].Latitude, (float)pp[pp.Count - 1].Longitude);
    end = router.Resolve(profile, (float)pp[0].Latitude, (float)pp[0].Longitude);

    route = router.Calculate(profile, start, end);
    //add all the locations for that route
    for (int j = 0; j < route.Shape.Length; j++)
    {
      pl.Positions.Add(new((float)route.Shape[j].Latitude, (float)route.Shape[j].Longitude));
    }

    //add polyline to mapViewElement
    mapViewElement.Drawables.Add(pl);

  }

}

