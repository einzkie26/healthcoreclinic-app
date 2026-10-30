namespace healthcoreclinic_app_demo.Services
{
    public class LoginService
    {
        private readonly AuthService _auth;

        public LoginService(AuthService auth)
        {
            _auth = auth;
        }

        public bool IsAuthenticated => _auth.IsAuthenticated;

        public async Task<LoginResult> LoginAsync(string? email, string? password)
        {
            var validationMessage = ValidateInput(email, password);
            if (validationMessage != null)
            {
                return LoginResult.Failed(validationMessage, clearPassword: false);
            }

            try
            {
                await Task.Delay(400);

                var (success, message, role) = await _auth.LoginAsync(email!, password!);
                if (!success)
                {
                    return LoginResult.Failed(message, clearPassword: true);
                }

                return LoginResult.Succeeded("Login successful! Redirecting...", GetRedirectRoute(role));
            }
            catch (Exception ex)
            {
                return LoginResult.Failed($"Login error: {ex.Message}", clearPassword: false);
            }
        }

        public string GetAuthenticatedUserRoute()
        {
            return GetRedirectRoute(_auth.CurrentRole);
        }

        private static string? ValidateInput(string? email, string? password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "Please enter your email.";
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return "Please enter your password.";
            }

            if (!email.Contains("@"))
            {
                return "Please enter a valid email address.";
            }

            return null;
        }

        private static string GetRedirectRoute(string? role)
        {
            return role switch
            {
                "Super Admin" or "SuperAdmin" => AppRoutes.SuperAdmin,
                "Admin" => AppRoutes.Admin,
                "Doctor" => AppRoutes.Doctor,
                "Receptionist" or "Reception" => AppRoutes.Receptionist,
                _ => AppRoutes.Dashboard
            };
        }
    }

    public sealed record LoginResult(
        bool Success,
        string Message,
        string? RedirectRoute,
        bool ClearPassword)
    {
        public static LoginResult Succeeded(string message, string redirectRoute)
        {
            return new LoginResult(true, message, redirectRoute, ClearPassword: false);
        }

        public static LoginResult Failed(string message, bool clearPassword)
        {
            return new LoginResult(false, message, RedirectRoute: null, clearPassword);
        }
    }
}
