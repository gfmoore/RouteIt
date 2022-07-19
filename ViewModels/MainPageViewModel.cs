namespace RouteIt.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
  public MainPageViewModel()
  {
    AddTestPostcodes();  //My home ST1 6SS 53.033809f, -2.151793f to SY6 1AX 53.051347f, -2.189165f

    LoadItinero();  //the routing package
  }


  [ObservableProperty]
  private string startPostcode;

  [ObservableProperty]
  private string postcodes;

  [ObservableProperty]
  private string route;

  [ObservableProperty]
  private bool findRouteEnabled;

  [ObservableProperty]
  private string mapItemsLabel = "Map Items";

  [ObservableProperty]
  private string findRouteLabel = "Find Route";

  public bool clearMap = false;   //for controlling the map or clear map button
  public bool clearRoute = false; //for controlling the route it or clear route

  public void AddTestPostcodes()
  {
    FindRouteEnabled = false;  //doesn't work
    StartPostcode = "ST1 6SS";
    //Postcodes = "ST6 1AX\rST6 1AS\rST6 1HS\rST6 4ER\rST6 1HA\rST6 4ER\rST6 7NR\rST6 1BW\rST6 7DG\rST6 7NE\rST6 7QT\rST6 1BD\rST6 1HX\rST6 4HR\rST6 7AL\rST6 7EL\rST6 7QQ\rST6 7DT";
    Postcodes = "ST2 8EF\rST2 8JT\rST2 8HF\rST1 6SL\rST2 8HX\rST2 8EW";
    //get postcodes from Postcodes
  }

  [ObservableProperty]
  private Border mapContent;

  [ObservableProperty]
  private bool isBusy = false;

  List<PostcodePosition> postcodeCoordinates = null;

  public RouterDb routerDb;
  public Router router;
  public Itinero.Profiles.Profile profile;

  public void LoadItinero()
  {
    //Load the Itinero OSM GB database Takes about 10seconds to load
    IsBusy = true;
    using (var stream = new FileInfo(@"C:\Users\Gordon\source\repos\RouteIt\Resources\Raw\gb.routerdb").OpenRead())
    {
      routerDb = RouterDb.Deserialize(stream);
    }

    router = new(routerDb);
    profile = Itinero.Osm.Vehicles.Vehicle.Car.Fastest();
    IsBusy = false;
  }

  [RelayCommand]
  public void Meatballs()
  {
    Debug.WriteLine("Heyupski");
  }

  [RelayCommand]
  public async void MapItems()
  {
    if (!clearMap)
    {
      //??Cannot send a message from the LoadItinero function - because it is part of the constructor???
      IsBusy = true;
      MessagingCenter.Send(new MessagingMarker(), "RouterDbLoaded", routerDb);
      IsBusy = false;

      if (StartPostcode == null)  //no start postocde
      {
        StartPostcode = "Need this!!!";
        return;
      }

      List<string> postcodes = new()
      {
        StartPostcode
      };

      if (Postcodes == String.Empty || Postcodes == null)
      {
        Postcodes = "Need these!!!";
        return;
      }

      Debug.WriteLine("Map items");
      clearMap = true;
      MapItemsLabel = "Clear Map";

      HttpClient _client = new();
      string baseURI = "https://api.postcodes.io/postcodes/";

      postcodes.AddRange(Postcodes.Split("\r", StringSplitOptions.None));

      postcodeCoordinates = new();

      foreach (string postcode in postcodes)
      {

        Uri uri = new(baseURI + postcode);
        try
        {
          HttpResponseMessage response = await _client.GetAsync(uri);
          if (response.IsSuccessStatusCode)
          {
            string content = await response.Content.ReadAsStringAsync();

            PostcodeObject pc = JsonSerializer.Deserialize<PostcodeObject>(content);
            PostcodePosition pcp = new()
            {
              Postcode = pc.result.postcode,
              Latitude = pc.result.latitude,
              Longitude = pc.result.longitude
            };
            postcodeCoordinates.Add(pcp);
            //Debug.WriteLine($"Latitude: {pc.result.latitude} Longitude: {pc.result.longitude}");
          }
        }
        catch (Exception e)
        {
          Debug.WriteLine($"RouteIt: {e.Message}");
        }
      }
      FindRouteEnabled = true;

      //add pins by sending a message to the code behind where map is definded
      MessagingCenter.Send(new MessagingMarker(), "AddPins", postcodeCoordinates);

    }
    else
    {
      MapItemsLabel = "Map Items";
      clearMap = false;
      Postcodes = "";
      Route = "";

      FindRouteLabel = "Find Route";
      clearRoute = false;

      //recentre map on my home pos
      MessagingCenter.Send(new MessagingMarker(), "ClearMap", postcodeCoordinates);

      if (shortestRoute == null) return;
      MessagingCenter.Send(new MessagingMarker(), "ClearRoute", shortestRoute.ToList());
    }
  }

  public PostcodePosition[] shortestRoute;

  [RelayCommand]
  public void FindRoute()
  {
    if (!clearRoute)
    {
      Debug.WriteLine("Find route");
      FindRouteLabel = "Clear Route";
      clearRoute = true;
      
      if (postcodeCoordinates == null) return;  //need to map it
      //clear the route list
      Route = "";

      //ok so let's start at StartPostcode and find the distance to all Postcodes
      //convert postcodeCordinates to an array
      double shortestDistance;
      int shortesti;
      double distance;
      //the starting array and the shortest route array
      PostcodePosition[] postcodePositions = postcodeCoordinates.ToArray();
      shortestRoute = new PostcodePosition[postcodePositions.Length];
      shortestRoute[0] = postcodePositions[0];
      int j = 1;

      while (postcodePositions.Length > 2)
      {
        shortestDistance = 999999;
        shortesti = 0;
        for (int i = 1; i < postcodePositions.Length; i++)
        {
          //distance = CrowFliesDistance(postcodePositions[0], postcodePositions[i]);
          distance = ItineroDistance(postcodePositions[0], postcodePositions[i]);

          if (distance < shortestDistance)
          {
            shortestDistance = distance;
            shortesti = i;
          }
        }
        shortestRoute[j] = postcodePositions[shortesti];
        j += 1;

        //now need to create a new array with this as starting point and not the first element
        PostcodePosition[] newPostcodePositions = new PostcodePosition[postcodePositions.Length - 1];
        newPostcodePositions[0] = postcodePositions[shortesti];
        for (int i = 1; i < postcodePositions.Length; i++)
        {
          if (i < shortesti)
          {
            newPostcodePositions[i] = postcodePositions[i];
          }
          if (i > shortesti)
          {
            newPostcodePositions[i - 1] = postcodePositions[i];
          }
        }
        postcodePositions = newPostcodePositions;
      }
      //grab last postcode
      //shortestRoute[shortestRoute.Length - 1] = postcodePositions[1];
      shortestRoute[^1] = postcodePositions[1];

      for (int i = 1; i < shortestRoute.Length; i++)
      {
        //Debug.WriteLine(shortestRoute[i].Postcode);
        if (i != shortestRoute.Length - 1) Route += shortestRoute[i].Postcode + "\r\n";
        if (i == shortestRoute.Length - 1) Route += shortestRoute[i].Postcode;
      }

      MessagingCenter.Send(new MessagingMarker(), "RouteIt", shortestRoute.ToList());
    }
    else
    {
      FindRouteLabel = "Find Route";
      clearRoute = false;

      //clear the route list
      Route = "";

      MessagingCenter.Send(new MessagingMarker(), "ClearRoute", shortestRoute.ToList());
    }
  }


  //Haversine formula https://www.geeksforgeeks.org/program-distance-two-points-earth/
  public static double CrowFliesDistance(PostcodePosition pp1, PostcodePosition pp2)
{
    double dtor = Math.PI / 180;
    double lat1 = pp1.Latitude * dtor;
    double lat2 = pp2.Latitude * dtor;
    double lng1 = pp1.Longitude * dtor;
    double lng2 = pp2.Longitude * dtor;

    Double d = 3963.0 * Math.Acos( (Math.Sin(lat1) * Math.Sin(lat2)) + (Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lng2 - lng1)) );

    return d;
  }

  public double ItineroDistance(PostcodePosition pp1, PostcodePosition pp2)
  {
    // calculate a route.

    // snaps the given location to the nearest routable edge.My home
    var start = router.Resolve(profile, (float) pp1.Latitude, (float) pp1.Longitude);
    var end = router.Resolve(profile, (float) pp2.Latitude, (float) pp2.Longitude);

    var route = router.Calculate(profile, start, end);

    return route.TotalDistance; //(in meters)
  }
}

//The JSON received from postcodes.io as class
#pragma warning disable IDE1006 // Naming Styles
public class PostcodeObject
{
  public int status { get; set; }
  public Result result { get; set; }

}

public class Result
{
  public string postcode { get; set; }
  public int quality { get; set; }
  public int eastings { get; set; }
  public int northings { get; set; }
  public string country { get; set; }
  public string nhs_ha { get; set; }
  public double longitude { get; set; }
  public double latitude { get; set; }
  public string european_electoral_region { get; set; }
  public string primary_care_trust { get; set; }
  public string region { get; set; }
  public string lsoa { get; set; }
  public string msoa { get; set; }
  public string incode { get; set; }
  public string outcode { get; set; }
  public string parliamentary_constituency { get; set; }
  public string admin_district { get; set; }
  public string parish { get; set; }
  public string admin_county { get; set; }
  public string admin_ward { get; set; }
  public string ced { get; set; }
  public string ccg { get; set; }
  public string nuts { get; set; }
  public Codes codes { get; set; }

}

public class Codes
{
  public string admin_district { get; set; }
  public string admin_county { get; set; }
  public string admin_ward { get; set; }
  public string parish { get; set; }
  public string parliamentary_constituency { get; set; }
  public string ccg { get; set; }
  public string ccg_id { get; set; }
  public string ced { get; set; }
  public string nuts { get; set; }
  public string lsoa { get; set; }
  public string msoa { get; set; }
  public string lau2 { get; set; }

}
