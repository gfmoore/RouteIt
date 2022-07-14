

namespace RouteIt.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
  public MainPageViewModel()
  {
    AddTestPostcodes();
  }

  [ObservableProperty]
  private string startPostcode;

  [ObservableProperty]
  private string postcodes;

  [ObservableProperty]
  private string route;

  [ObservableProperty]
  private bool findRouteEnabled; 

  public void AddTestPostcodes()
  {
    FindRouteEnabled = false;  //doesn't work
    StartPostcode = "ST1 6SS";
    Postcodes = "ST6 1AX\r\nST6 1AS\r\nST6 1HS\r\nST6 4ER\r\nST6 1HA\r\nST6 4ER\r\nST6 7NR\r\nST6 1BW\r\nST6 7DG\r\nST6 7NE\r\nST6 7QT\r\nST6 1BD\r\nST6 1HX\r\nST6 4HR\r\nST6 7AL\r\nST6 7EL\r\nST6 7QQ\r\nST6 7DT";
  }

  [ObservableProperty]
  private Border mapContent;

  [ObservableProperty]
  private bool isBusy = false;

  List<PostcodePosition> postcodeCoordinates = null;

  [RelayCommand]
  public async void MapItems()
  {
    Debug.WriteLine("Map items");
    HttpClient _client = new();
    string baseURI = "https://api.postcodes.io/postcodes/";

    List<string> postcodes = new();
    postcodes.Add(StartPostcode);
    postcodes.AddRange(Postcodes.Split("\r\n", StringSplitOptions.None));
    
    postcodeCoordinates = new();
    
    foreach(string postcode in postcodes)
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
    MessagingCenter.Send(new MessagingMarker(), "PinAdded", postcodeCoordinates);
  }

  [RelayCommand]
  public void FindRoute()
  {
    Debug.WriteLine("Find route");
    if (postcodeCoordinates == null) return;  //need to map it
    //clear the route list
    Route = "";

    //ok so let's start at StartPostcode and find the distance to all Postcodes
    //convert postcodeCordinates to an array
    double shortestDistance = 999999;
    int shortesti = 0;
    double distance;
    //the starting array and the shortest route array
    PostcodePosition[] postcodePositions = postcodeCoordinates.ToArray();
    PostcodePosition[] shortestRoute = new PostcodePosition[postcodePositions.Length];
    shortestRoute[0] = postcodePositions[0];
    int j = 1;

    while (postcodePositions.Length > 2)
    {
      shortestDistance = 999999;
      shortesti = 0;
      for (int i=1; i < postcodePositions.Length; i++)
      {
        distance = CrowFliesDistance(postcodePositions[0], postcodePositions[i]);
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
      for (int i=1; i < postcodePositions.Length; i++)
      {
        if (i < shortesti)
        {
          newPostcodePositions[i] = postcodePositions[i];
        }
        if (i > shortesti)
        {
          newPostcodePositions[i-1] = postcodePositions[i];
        }
      }
      postcodePositions = newPostcodePositions;
    }
    //grab last postcode
    shortestRoute[shortestRoute.Length-1] = postcodePositions[1];
    for (int i=1; i < shortestRoute.Length; i++)
    {
      //Debug.WriteLine(shortestRoute[i].Postcode);
      if (i != shortestRoute.Length -1) Route += shortestRoute[i].Postcode + "\r\n";
      if (i == shortestRoute.Length -1) Route += shortestRoute[i].Postcode;
    }
    
  }


  //Haversine formula https://www.geeksforgeeks.org/program-distance-two-points-earth/
  public double CrowFliesDistance(PostcodePosition pp1, PostcodePosition pp2)
{
    double dtor = Math.PI / 180;
    double lat1 = pp1.Latitude * dtor;
    double lat2 = pp2.Latitude * dtor;
    double lng1 = pp1.Longitude * dtor;
    double lng2 = pp2.Longitude * dtor;

    Double d = 3963.0 * Math.Acos( (Math.Sin(lat1) * Math.Sin(lat2)) + (Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lng2 - lng1)) );

    return d;
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
