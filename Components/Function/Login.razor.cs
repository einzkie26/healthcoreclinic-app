using healthcoreclinic_app_demo.Services;
using Microsoft.AspNetCore.Components;

namespace healthcoreclinic_app_demo.Components.Pages
{
    public partial class Login
    {
        [Inject]
        private LoginService LoginService { get; set; } = default!;

        [Inject]
        private NavigationManager Nav { get; set; } = default!;

        private string? username;
        private string? password;
        private string? message;
        private bool isSuccess;
        private bool isLoading;

        protected override void OnInitialized()
        {
            if (LoginService.IsAuthenticated)
            {
                Nav.NavigateTo(LoginService.GetAuthenticatedUserRoute());
            }
        }

        private async Task HandleLogin()
        {
            isLoading = true;
            message = null;

            var result = await LoginService.LoginAsync(username, password);

            isSuccess = result.Success;
            message = result.Message;

            if (result.ClearPassword)
            {
                password = string.Empty;
            }

            if (result.Success && result.RedirectRoute != null)
            {
                await Task.Delay(1000);
                Nav.NavigateTo(result.RedirectRoute);
            }

            isLoading = false;
        }

        private void ClearMessage()
        {
            message = null;
        }

        private void NavigateToRegister()
        {
            Nav.NavigateTo(AppRoutes.Register);
        }
    }
}
