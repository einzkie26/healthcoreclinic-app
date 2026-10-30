namespace healthcoreclinic_app_demo
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();
			InitializeDatabase();
		}

		private void InitializeDatabase()
		{
			try
			{
				var dbContext = IPlatformApplication.Current?.Services?.GetService<healthcoreclinic_app_demo.Data.ClinicDbContext>();
				if (dbContext != null)
				{
					// Create database if it doesn't exist
					dbContext.Database.EnsureCreated();
					System.Diagnostics.Debug.WriteLine("Database ensured created");
					
					// Ensure roles exist
					if (!dbContext.Roles.Any())
					{
						dbContext.Roles.AddRange(
							new healthcoreclinic_app_demo.Models.Role { RoleID = 1, RoleName = "Super Admin" },
							new healthcoreclinic_app_demo.Models.Role { RoleID = 2, RoleName = "Admin" },
							new healthcoreclinic_app_demo.Models.Role { RoleID = 3, RoleName = "Doctor" },
							new healthcoreclinic_app_demo.Models.Role { RoleID = 4, RoleName = "Receptionist" }
						);
						dbContext.SaveChanges();
						System.Diagnostics.Debug.WriteLine("Roles created");
					}
					
					// Ensure subscription plans exist
					if (!dbContext.SubscriptionPlans.Any())
					{
						dbContext.SubscriptionPlans.AddRange(
							new healthcoreclinic_app_demo.Models.SubscriptionPlan { PlanID = 1, PlanName = "Basic", Price = 144, MaxDoctors = 1, MaxReceptionists = 1, HasResourceAccess = false, HasAnalytics = false },
							new healthcoreclinic_app_demo.Models.SubscriptionPlan { PlanID = 2, PlanName = "Standard", Price = 599, MaxDoctors = 3, MaxReceptionists = 3, HasResourceAccess = false, HasAnalytics = true },
							new healthcoreclinic_app_demo.Models.SubscriptionPlan { PlanID = 3, PlanName = "Premium", Price = 1599, MaxDoctors = null, MaxReceptionists = null, HasResourceAccess = true, HasAnalytics = true }
						);
						dbContext.SaveChanges();
						System.Diagnostics.Debug.WriteLine("Subscription plans created");
					}
					
					// Ensure super admin exists
					var superAdminUser = dbContext.Users.FirstOrDefault(u => u.Email == "admin@clinic.com");
					
					if (superAdminUser == null)
					{
						var superAdminRole = dbContext.Roles.FirstOrDefault(r => r.RoleName == "Super Admin");
						if (superAdminRole != null)
						{
							var hashedPassword = healthcoreclinic_app_demo.Utilities.PasswordHasher.HashPassword("SuperAdmin123");
							var adminUser = new healthcoreclinic_app_demo.Models.User
							{
								FullName = "System Administrator",
								Email = "admin@clinic.com",
								PasswordHash = hashedPassword,
								RoleID = superAdminRole.RoleID,
								IsActive = true,
								CreatedAt = DateTime.UtcNow
							};
							
							dbContext.Users.Add(adminUser);
							dbContext.SaveChanges();
							System.Diagnostics.Debug.WriteLine("Super admin user created");
						}
					}
					else
					{
						System.Diagnostics.Debug.WriteLine("Super admin user already exists");
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}\n{ex.InnerException?.Message}");
			}
		}

		protected override Window CreateWindow(IActivationState? activationState)
		{
			return new Window(new MainPage()) { Title = "healthcoreclinic-app-demo" };
		}
	}
}
