using System.ComponentModel;

namespace RouteIt;

public partial class AppShell : Shell
{
  public AppShell()
  {
    InitializeComponent();
    PropertyChanged += Shell_PropertyChanged;

  }

  private void Shell_PropertyChanged(object sender, PropertyChangedEventArgs e)
  {
    Debug.WriteLine(e.PropertyName.ToString()); //sender FlyoutIcon: null

    //if (e.PropertyName.Equals("FlyoutIsPresented"))
    //  if (FlyoutIsPresented)
    //    Debug.WriteLine("opened");      //you will execute your code here
    //  else
    //    Debug.WriteLine("closed");
  }
}