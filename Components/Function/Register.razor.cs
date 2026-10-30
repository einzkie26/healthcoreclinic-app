using healthcoreclinic_app_demo.Models;
using healthcoreclinic_app_demo.Services;
using Microsoft.AspNetCore.Components;

namespace healthcoreclinic_app_demo.Components.Pages
{
    public partial class Register
    {
        [Inject]
        private AuthService Auth { get; set; } = default!;

        [Inject]
        private NavigationManager Nav { get; set; } = default!;

        private SubscriptionPlan? selectedPlan;
        private List<SubscriptionPlan>? subscriptionPlans;
        private string? clinicName;
        private string? fullName;
        private string? email;
        private string? password;
        private string? confirmPassword;
        private string? message;
        private bool isSuccess;
        private bool isLoading;

        private string? clinicError;
        private string? nameError;
        private string? emailError;
        private string? passwordError;
        private string? confirmError;
        private string? planError;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                subscriptionPlans = await Auth.GetSubscriptionPlansAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading subscription plans: {ex.Message}");
                message = "Failed to load subscription plans.";
                isSuccess = false;
            }
        }

        private void SelectPlan(SubscriptionPlan plan)
        {
            selectedPlan = plan;
            planError = null;
        }

        private void ClearErrors()
        {
            clinicError = nameError = emailError = passwordError = confirmError = planError = null;
        }

        private void ValidateField(string fieldName)
        {
            switch (fieldName)
            {
                case "clinic":
                    if (string.IsNullOrWhiteSpace(clinicName))
                    {
                        clinicError = "Clinic name is required.";
                    }
                    else if (clinicName.Length < 2)
                    {
                        clinicError = "Clinic name must be at least 2 characters.";
                    }
                    else
                    {
                        clinicError = null;
                    }
                    break;

                case "name":
                    if (string.IsNullOrWhiteSpace(fullName))
                    {
                        nameError = "Full name is required.";
                    }
                    else if (fullName.Length < 3)
                    {
                        nameError = "Full name must be at least 3 characters.";
                    }
                    else
                    {
                        nameError = null;
                    }
                    break;

                case "email":
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        emailError = "Email is required.";
                    }
                    else if (!Auth.IsValidEmail(email))
                    {
                        emailError = "Please enter a valid email address.";
                    }
                    else
                    {
                        emailError = null;
                    }
                    break;

                case "password":
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        passwordError = "Password is required.";
                    }
                    else if (!Auth.IsValidPassword(password))
                    {
                        passwordError = "Password must be at least 6 characters.";
                    }
                    else
                    {
                        passwordError = null;
                    }
                    break;

                case "confirm":
                    if (string.IsNullOrWhiteSpace(confirmPassword))
                    {
                        confirmError = "Please confirm your password.";
                    }
                    else if (password != confirmPassword)
                    {
                        confirmError = "Passwords do not match.";
                    }
                    else
                    {
                        confirmError = null;
                    }
                    break;

                case "plan":
                    if (selectedPlan == null)
                    {
                        planError = "Please select a subscription plan.";
                    }
                    else
                    {
                        planError = null;
                    }
                    break;
            }
        }

        private bool ValidateAllFields()
        {
            ClearErrors();
            ValidateField("clinic");
            ValidateField("name");
            ValidateField("email");
            ValidateField("password");
            ValidateField("confirm");
            ValidateField("plan");

            return string.IsNullOrEmpty(clinicError) &&
                   string.IsNullOrEmpty(nameError) &&
                   string.IsNullOrEmpty(emailError) &&
                   string.IsNullOrEmpty(passwordError) &&
                   string.IsNullOrEmpty(confirmError) &&
                   string.IsNullOrEmpty(planError);
        }

        private async Task HandleRegister()
        {
            if (!ValidateAllFields())
            {
                isSuccess = false;
                message = "Please fix the errors above.";
                return;
            }

            isLoading = true;
            message = null;

            try
            {
                await Task.Delay(600);

                var result = await Auth.RegisterAdminAsync(
                    clinicName ?? string.Empty,
                    fullName ?? string.Empty,
                    email ?? string.Empty,
                    password ?? string.Empty,
                    selectedPlan?.PlanName ?? string.Empty);

                isSuccess = result.Success;
                message = result.Message;

                if (result.Success)
                {
                    await Task.Delay(1500);
                    Nav.NavigateTo(AppRoutes.Login);
                }
                else
                {
                    password = string.Empty;
                    confirmPassword = string.Empty;
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                message = $"Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Registration error: {ex}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private void BackToLogin()
        {
            Nav.NavigateTo(AppRoutes.Login);
        }
    }
}
