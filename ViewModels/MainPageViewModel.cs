namespace RouteIt.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
  public MainPageViewModel()
  {

  }

  [ObservableProperty]
  private Border mapContent;

  [ObservableProperty]
  private bool isBusy = false;

  [RelayCommand]
  public void MapItems()
  {
    Debug.WriteLine("Map items");

  }

  [RelayCommand]
  public void FindRoute()
  {
    Debug.WriteLine("Find route");
  }
}
