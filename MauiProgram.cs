using healthcoreclinic_app_demo.Services;
using healthcoreclinic_app_demo.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace healthcoreclinic_app_demo
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				});

			builder.Services.AddMauiBlazorWebView();

			// Configuration
			var config = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					{ "ConnectionStrings:DefaultConnection", "Server=desktop-5i67rfl\\sqlexpress;Database=HealthCoreDB;Trusted_Connection=True;TrustServerCertificate=True;" }
				})
				.Build();

			builder.Services.AddSingleton(config);

			// Database Context
			builder.Services.AddDbContext<ClinicDbContext>(options =>
				options.UseSqlServer(
					config.GetConnectionString("DefaultConnection"),
					sqlOptions => sqlOptions.EnableRetryOnFailure(
						maxRetryCount: 5,
						maxRetryDelay: TimeSpan.FromSeconds(30),
						errorNumbersToAdd: null
					)
				)
			);

			// Services
			builder.Services.AddSingleton<AuthService>();
			builder.Services.AddSingleton<LoginService>();

#if DEBUG
			builder.Services.AddBlazorWebViewDeveloperTools();
			builder.Logging.AddDebug();
#endif

			return builder.Build();
		}
	}
}
