/* Program			RouteIt
 * Description	A program for providing a route for a list of UK Postcodes
 * Author				Gordon Moore
 * Date					18 July 2022
 * Copyright		For now, NO permission is granted to use this code or program, all rights reserved Gordon F Moore 18 July 2022
 * 
 * Versions
 * 1.0.0.0			18 July 2022	Initial working version
 * 1.0.0.1			18 July 2022	Added some pin info and made them clickable
 * 1.0.0.2			18 July 2022	Added route number index to callouts
 */

namespace RouteIt;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		//dependency injection
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<MainPageViewModel>();

		builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);

		return builder.Build();
	}
}
