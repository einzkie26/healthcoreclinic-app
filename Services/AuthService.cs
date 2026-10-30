using healthcoreclinic_app_demo.Data;
using healthcoreclinic_app_demo.Models;
using healthcoreclinic_app_demo.Utilities;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ClinicResource = healthcoreclinic_app_demo.Models.Resource;

namespace healthcoreclinic_app_demo.Services
{
    public sealed record StaffLimitInfo(string RoleName, int ActiveCount, int? MaxAllowed)
    {
        public int? Remaining => MaxAllowed.HasValue ? Math.Max(0, MaxAllowed.Value - ActiveCount) : null;
        public bool IsAtLimit => MaxAllowed.HasValue && ActiveCount >= MaxAllowed.Value;
        public string LimitText => MaxAllowed.HasValue ? $"{ActiveCount}/{MaxAllowed.Value}" : $"{ActiveCount}/Unlimited";
        public string RemainingText => Remaining.HasValue ? $"{Remaining.Value} remaining" : "Unlimited";
    }

    public class AuthService
    {
        private readonly ClinicDbContext _context;
        private readonly object _activityLock = new();
        private readonly List<AdminActivityItem> _adminActivities = new();
        private int _nextActivityId = 1;

        public string? CurrentRole { get; set; }
        public string? CurrentUser { get; set; }
        public int CurrentUserID { get; set; }
        public User? CurrentUserData { get; set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentRole);

        public AuthService(ClinicDbContext context)
        {
            _context = context;
        }

        public sealed class AdminActivityItem
        {
            public int Id { get; set; }
            public DateTime TimestampUtc { get; set; }
            public int AdminUserId { get; set; }
            public string AdminName { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string EntityType { get; set; } = string.Empty;
            public string EntityName { get; set; } = string.Empty;
            public string Details { get; set; } = string.Empty;
            public bool IsRead { get; set; }
        }

        public List<AdminActivityItem> GetRecentAdminActivities(int take = 30)
        {
            lock (_activityLock)
            {
                return _adminActivities
                    .OrderByDescending(a => a.TimestampUtc)
                    .Take(Math.Max(1, take))
                    .ToList();
            }
        }

        public int GetUnreadAdminActivityCount()
        {
            lock (_activityLock)
            {
                return _adminActivities.Count(a => !a.IsRead);
            }
        }

        public bool MarkAdminActivityAsRead(int activityId)
        {
            lock (_activityLock)
            {
                var item = _adminActivities.FirstOrDefault(a => a.Id == activityId);
                if (item == null)
                    return false;

                item.IsRead = true;
                return true;
            }
        }

        private void LogAdminActivity(int adminUserId, string action, string entityType, string entityName, string details)
        {
            if (adminUserId <= 0)
                return;

            var adminName = _context.Users
                .AsNoTracking()
                .Where(u => u.UserID == adminUserId)
                .Select(u => u.FullName)
                .FirstOrDefault() ?? $"Admin #{adminUserId}";

            var entry = new AdminActivityItem
            {
                Id = _nextActivityId++,
                TimestampUtc = DateTime.UtcNow,
                AdminUserId = adminUserId,
                AdminName = adminName,
                Action = action,
                EntityType = entityType,
                EntityName = entityName,
                Details = details,
                IsRead = false
            };

            lock (_activityLock)
            {
                _adminActivities.Add(entry);
                if (_adminActivities.Count > 500)
                {
                    _adminActivities.RemoveRange(0, _adminActivities.Count - 500);
                }
            }
        }

        #region Validation Methods

        public bool IsValidEmail(string email)
        {
            try
            {
                var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                return Regex.IsMatch(email, pattern);
            }
            catch
            {
                return false;
            }
        }

        public bool IsValidPassword(string password)
        {
            return password != null && password.Length >= 6;
        }

        #endregion

        #region Login

        public async Task<(bool Success, string Message, string? Role)> LoginAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    return (false, "Email and password are required.", null);

                if (!IsValidEmail(email))
                    return (false, "Please enter a valid email address.", null);

                System.Diagnostics.Debug.WriteLine($"🔍 Attempting login for: {email}");
                
                // Query database for user
                var user = _context.Users
                    .Where(u => u.Email == email)
                    .Include(u => u.Role)
                    .FirstOrDefault();

                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ User not found: {email}");
                    var userCount = _context.Users.Count();
                    System.Diagnostics.Debug.WriteLine($"📊 Total users in database: {userCount}");
                    return (false, "Invalid email or password.", null);
                }

                System.Diagnostics.Debug.WriteLine($"✓ User found: {user.Email}, Role: {user.Role?.RoleName}");

                // Verify password hash
                if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Password verification failed for: {email}");
                    return (false, "Invalid email or password.", null);
                }

                System.Diagnostics.Debug.WriteLine($"✓ Password verified for: {email}");

                if (!user.IsActive)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Deactivated account login blocked for: {email}");
                    return (false, "Your account was deactivated by your administrator, Please go to support office for your concern.", null);
                }

                // Set session data
                CurrentRole = user.Role?.RoleName;
                CurrentUser = user.Email;
                CurrentUserID = user.UserID;
                CurrentUserData = user;

                System.Diagnostics.Debug.WriteLine($"✓ Login successful for: {email}, Role: {CurrentRole}");

                return (true, "Login successful.", CurrentRole);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Login error: {ex.Message}\n{ex.InnerException?.Message}");
                return (false, $"Login error: {ex.Message}", null);
            }
        }

        #endregion

        #region Registration

        public async Task<(bool Success, string Message)> RegisterAdminAsync(
            string clinicName,
            string fullName,
            string email,
            string password,
            string planName)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(clinicName))
                    return (false, "Clinic name is required.");
                if (string.IsNullOrWhiteSpace(fullName))
                    return (false, "Full name is required.");
                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email is required.");
                if (string.IsNullOrWhiteSpace(password))
                    return (false, "Password is required.");
                if (string.IsNullOrWhiteSpace(planName))
                    return (false, "Please select a subscription plan.");

                if (fullName.Length < 3)
                    return (false, "Full name must be at least 3 characters.");

                if (!IsValidEmail(email))
                    return (false, "Please enter a valid email address.");

                if (!IsValidPassword(password))
                    return (false, "Password must be at least 6 characters.");

                // Check if email already exists
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == email);
                if (existingUser != null)
                    return (false, "This email is already registered.");

                // Get Admin role and subscription plan
                var adminRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Admin");
                var plan = _context.SubscriptionPlans.FirstOrDefault(p => p.PlanName == planName);

                if (adminRole == null || plan == null)
                    return (false, "Invalid role or plan.");

                // Hash password
                var passwordHash = PasswordHasher.HashPassword(password);

                // Create user
                var newUser = new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = passwordHash,
                    RoleID = adminRole.RoleID,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Create subscription
                var subscription = new Subscription
                {
                    AdminID = newUser.UserID,
                    PlanID = plan.PlanID,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    Status = "Active"
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                return (true, $"Registration successful! Your account has been created with the {planName} plan. You can now log in with your email address.");
            }
            catch (Exception ex)
            {
                return (false, $"Registration error: {ex.Message}");
            }
        }

        #endregion

        #region Admin/Doctor/Receptionist Operations

        public async Task<List<User>> GetUsersByRoleAsync(string roleName, bool includeInactive = false)
        {
            try
            {
                return await Task.Run(() =>
                    _context.Users
                        .Where(u => u.Role!.RoleName == roleName && (includeInactive || u.IsActive))
                        .Include(u => u.Role)
                        .Include(u => u.CreatedByAdmin)
                        .ToList()
                );
            }
            catch
            {
                return new List<User>();
            }
        }

        public async Task<Subscription?> GetActiveSubscriptionAsync(int adminId)
        {
            try
            {
                return await _context.Subscriptions
                    .AsNoTracking()
                    .Include(s => s.Plan)
                    .Where(s => s.AdminID == adminId)
                    .OrderByDescending(s => s.StartDate)
                    .FirstOrDefaultAsync(s => s.Status == "Active")
                    ?? await _context.Subscriptions
                        .AsNoTracking()
                        .Include(s => s.Plan)
                        .Where(s => s.AdminID == adminId)
                        .OrderByDescending(s => s.StartDate)
                        .FirstOrDefaultAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<SubscriptionPlan?> GetActiveSubscriptionPlanAsync(int adminId)
        {
            var subscription = await GetActiveSubscriptionAsync(adminId);
            return subscription?.Plan;
        }

        public async Task<(bool Allowed, string Message)> CanCreateStaffAsync(int adminId, string roleName)
        {
            try
            {
                var limit = await GetStaffLimitAsync(adminId, roleName);
                if (!limit.MaxAllowed.HasValue)
                    return (true, string.Empty);

                if (limit.ActiveCount >= limit.MaxAllowed.Value)
                    return (false, $"Plan limit reached. {limit.RoleName}s: {limit.ActiveCount}/{limit.MaxAllowed.Value}.");

                return (true, string.Empty);
            }
            catch
            {
                return (true, string.Empty);
            }
        }

        public async Task<StaffLimitInfo> GetStaffLimitAsync(int adminId, string roleName)
        {
            var normalizedRole = roleName.Equals("Doctor", StringComparison.OrdinalIgnoreCase)
                ? "Doctor"
                : roleName.Equals("Receptionist", StringComparison.OrdinalIgnoreCase)
                    ? "Receptionist"
                    : roleName;

            var plan = await GetActiveSubscriptionPlanAsync(adminId);
            var maxAllowed = normalizedRole.Equals("Doctor", StringComparison.OrdinalIgnoreCase)
                ? plan?.MaxDoctors
                : normalizedRole.Equals("Receptionist", StringComparison.OrdinalIgnoreCase)
                    ? plan?.MaxReceptionists
                    : null;

            var activeCount = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .CountAsync(u =>
                    u.IsActive &&
                    u.CreatedByAdminID == adminId &&
                    u.Role != null &&
                    u.Role.RoleName == normalizedRole);

            return new StaffLimitInfo(normalizedRole, activeCount, maxAllowed);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await Task.Run(() =>
                    _context.Users
                        .Where(u => u.UserID == userId)
                        .Include(u => u.Role)
                        .Include(u => u.CreatedByAdmin)
                        .FirstOrDefault()
                );
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string Message)> CreateUserAsync(
            string fullName,
            string email,
            string password,
            string roleName,
            int? createdByAdminId = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 CreateUserAsync called: Name='{fullName}', Email='{email}', Role='{roleName}'");

                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || 
                    string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(roleName))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Missing required fields");
                    return (false, "All fields are required.");
                }

                // Check if email exists
                if (_context.Users.Any(u => u.Email == email))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Email already exists: {email}");
                    return (false, "Email already registered.");
                }

                // Get role
                var role = _context.Roles.FirstOrDefault(r => r.RoleName == roleName);
                if (role == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Role not found: {roleName}. Available roles: {string.Join(", ", _context.Roles.Select(r => r.RoleName))}");
                    return (false, $"Invalid role: {roleName}");
                }

                System.Diagnostics.Debug.WriteLine($"✓ Role found: {role.RoleName} (ID: {role.RoleID})");

                var passwordHash = PasswordHasher.HashPassword(password);

                var newUser = new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = passwordHash,
                    RoleID = role.RoleID,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAdminID = createdByAdminId > 0 ? createdByAdminId : null
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                if (createdByAdminId.HasValue && createdByAdminId.Value > 0)
                {
                    LogAdminActivity(
                        createdByAdminId.Value,
                        "Create",
                        "Staff User",
                        newUser.FullName,
                        $"Created {roleName} account ({newUser.Email}).");
                }

                System.Diagnostics.Debug.WriteLine($"✓ User '{fullName}' created successfully. ID: {newUser.UserID}");
                return (true, $"User '{fullName}' created successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating user: {ex.Message}");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"   Inner: {ex.InnerException.Message}");
                return (false, $"Error creating user: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateUserAsync(
            int userId,
            string fullName,
            string email,
            bool isActive)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.UserID == userId);
                if (user == null)
                    return (false, "User not found.");

                // Check if new email is already taken
                if (user.Email != email && _context.Users.Any(u => u.Email == email))
                    return (false, "Email already in use.");

                user.FullName = fullName;
                user.Email = email;
                user.IsActive = isActive;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return (true, "User updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating user: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                    return (false, "User not found.");

                if (user.IsActive)
                    return (false, "Only deactivated users can be permanently deleted.");

                // Deleting staff can be blocked by FK restrictions.
                // We reassign "created by" references and block deletion only for critical doctor-owned records.
                var hasMedicalRecords = await _context.MedicalRecords.AnyAsync(m => m.DoctorID == userId);
                var hasDoctorAppointments = await _context.Appointments.AnyAsync(a => a.DoctorID == userId);

                if (hasMedicalRecords || hasDoctorAppointments)
                {
                    var roleName = user.Role?.RoleName ?? "Staff";
                    return (false, $"{roleName} cannot be deleted because linked appointments or medical records still exist.");
                }

                // Reassign created-by references to the creator admin when available.
                var fallbackAdminId = user.CreatedByAdminID;
                if (fallbackAdminId.HasValue && fallbackAdminId.Value > 0)
                {
                    var createdAppointments = await _context.Appointments
                        .Where(a => a.CreatedBy == userId)
                        .ToListAsync();
                    foreach (var appointment in createdAppointments)
                    {
                        appointment.CreatedBy = fallbackAdminId.Value;
                    }

                    var createdInvoices = await _context.Invoices
                        .Where(i => i.CreatedBy == userId)
                        .ToListAsync();
                    foreach (var invoice in createdInvoices)
                    {
                        invoice.CreatedBy = fallbackAdminId.Value;
                    }
                }
                else
                {
                    // No reassignment target available.
                    var hasCreatedAppointments = await _context.Appointments.AnyAsync(a => a.CreatedBy == userId);
                    var hasCreatedInvoices = await _context.Invoices.AnyAsync(i => i.CreatedBy == userId);
                    if (hasCreatedAppointments || hasCreatedInvoices)
                    {
                        return (false, "User cannot be deleted because they created appointments or invoices and no reassignment admin is available.");
                    }
                }

                // If this user created other staff, release that reference.
                var createdStaff = await _context.Users
                    .Where(u => u.CreatedByAdminID == userId)
                    .ToListAsync();
                foreach (var staff in createdStaff)
                {
                    staff.CreatedByAdminID = null;
                }

                // Remove schedules owned by this user (if any).
                var schedules = await _context.DoctorSchedules
                    .Where(s => s.DoctorID == userId)
                    .ToListAsync();
                if (schedules.Count > 0)
                {
                    _context.DoctorSchedules.RemoveRange(schedules);
                }

                // Remove subscriptions only when deleting an admin user.
                var subscriptions = await _context.Subscriptions
                    .Where(s => s.AdminID == userId)
                    .ToListAsync();
                if (subscriptions.Count > 0)
                {
                    _context.Subscriptions.RemoveRange(subscriptions);
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                if (user.CreatedByAdminID.HasValue && user.CreatedByAdminID.Value > 0)
                {
                    LogAdminActivity(
                        user.CreatedByAdminID.Value,
                        "Delete",
                        "Staff User",
                        user.FullName,
                        $"Deleted deactivated account ({user.Email}).");
                }

                return (true, "User deleted successfully.");
            }
            catch (DbUpdateException ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return (false, $"Unable to delete user due to related records. {detail}");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting user: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateStaffUserAsync(
            int userId,
            string fullName,
            string email,
            bool isActive,
            int actingAdminId)
        {
            try
            {
                if (actingAdminId <= 0)
                    return (false, "Invalid admin context.");

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user == null)
                    return (false, "User not found.");

                var roleName = user.Role?.RoleName;
                if (roleName != "Doctor" && roleName != "Receptionist")
                    return (false, "Only Doctor and Receptionist accounts can be managed from Admin User Management.");

                if (user.CreatedByAdminID != actingAdminId)
                    return (false, "You can only manage staff accounts created by your admin account.");

                var normalizedEmail = email.Trim();
                if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)
                    && await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
                {
                    return (false, "Email already in use.");
                }

                user.FullName = fullName.Trim();
                user.Email = normalizedEmail;
                user.IsActive = isActive;

                await _context.SaveChangesAsync();

                LogAdminActivity(
                    actingAdminId,
                    isActive ? "Activate" : "Deactivate",
                    "Staff User",
                    user.FullName,
                    $"Set status to {(isActive ? "Active" : "Inactive")} ({user.Email}).");

                return (true, "User updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating user: {ex.Message}");
            }
        }

        #endregion

        #region Subscription Operations

        public async Task<Subscription?> GetAdminSubscriptionAsync(int adminId)
        {
            try
            {
                return await Task.Run(() =>
                    _context.Subscriptions
                        .Where(s => s.AdminID == adminId)
                        .Include(s => s.Plan)
                        .FirstOrDefault()
                );
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<SubscriptionPlan>> GetSubscriptionPlansAsync()
        {
            try
            {
                return await Task.Run(() => _context.SubscriptionPlans.ToList());
            }
            catch
            {
                return new List<SubscriptionPlan>();
            }
        }

        #endregion

        #region Patient Operations

        public async Task<List<Patient>> GetAllPatientsAsync()
        {
            try
            {
                return await _context.Patients
                    .Where(p => !p.IsArchived)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error fetching patients: {ex.Message}");
                return new List<Patient>();
            }
        }

        public async Task<Patient?> GetPatientByIdAsync(int patientId)
        {
            try
            {
                return await _context.Patients
                    .Include(p => p.Appointments)
                    .Include(p => p.MedicalRecords)
                    .FirstOrDefaultAsync(p => p.PatientID == patientId);
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string Message, int PatientID)> CreatePatientAsync(
            string fullName,
            DateTime? birthDate,
            string? gender,
            string? contactNumber,
            string? address)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(fullName))
                    return (false, "Patient name is required.", 0);

                if (fullName.Length < 3)
                    return (false, "Patient name must be at least 3 characters.", 0);

                // Create new patient
                var patient = new Patient
                {
                    FullName = fullName.Trim(),
                    BirthDate = birthDate,
                    Gender = gender,
                    ContactNumber = contactNumber?.Trim(),
                    Address = address?.Trim(),
                    IsArchived = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                if (CurrentRole == "Admin")
                {
                    LogAdminActivity(CurrentUserID, "Create", "Patient", patient.FullName, "Created patient record.");
                }

                System.Diagnostics.Debug.WriteLine($"✓ Patient '{fullName}' created successfully. ID: {patient.PatientID}");
                return (true, $"Patient '{fullName}' created successfully.", patient.PatientID);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating patient: {ex.Message}");
                return (false, $"Error creating patient: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string Message)> UpdatePatientAsync(
            int patientId,
            string fullName,
            DateTime? birthDate,
            string? gender,
            string? contactNumber,
            string? address)
        {
            try
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientID == patientId);
                if (patient == null)
                    return (false, "Patient not found.");

                if (string.IsNullOrWhiteSpace(fullName) || fullName.Length < 3)
                    return (false, "Patient name must be at least 3 characters.");

                patient.FullName = fullName.Trim();
                patient.BirthDate = birthDate;
                patient.Gender = gender;
                patient.ContactNumber = contactNumber?.Trim();
                patient.Address = address?.Trim();

                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                if (CurrentRole == "Admin")
                {
                    LogAdminActivity(CurrentUserID, "Update", "Patient", patient.FullName, "Updated patient record.");
                }

                System.Diagnostics.Debug.WriteLine($"✓ Patient '{fullName}' updated successfully.");
                return (true, "Patient updated successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error updating patient: {ex.Message}");
                return (false, $"Error updating patient: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ArchivePatientAsync(int patientId)
        {
            try
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientID == patientId);
                if (patient == null)
                    return (false, "Patient not found.");

                patient.IsArchived = true;
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                if (CurrentRole == "Admin")
                {
                    LogAdminActivity(CurrentUserID, "Archive", "Patient", patient.FullName, "Archived patient record.");
                }

                System.Diagnostics.Debug.WriteLine($"✓ Patient archived successfully.");
                return (true, "Patient archived successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error archiving patient: {ex.Message}");
                return (false, $"Error archiving patient: {ex.Message}");
            }
        }

        public async Task<List<Patient>> GetArchivedPatientsAsync()
        {
            try
            {
                return await _context.Patients
                    .Where(p => p.IsArchived)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error fetching archived patients: {ex.Message}");
                return new List<Patient>();
            }
        }

        public async Task<(bool Success, string Message)> RestorePatientAsync(int patientId)
        {
            try
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientID == patientId);
                if (patient == null)
                    return (false, "Patient not found.");

                patient.IsArchived = false;
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                if (CurrentRole == "Admin")
                {
                    LogAdminActivity(CurrentUserID, "Restore", "Patient", patient.FullName, "Restored patient record.");
                }

                System.Diagnostics.Debug.WriteLine($"✓ Patient restored successfully.");
                return (true, "Patient restored successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error restoring patient: {ex.Message}");
                return (false, $"Error restoring patient: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeletePatientAsync(int patientId)
        {
            try
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientID == patientId);
                if (patient == null)
                    return (false, "Patient not found.");

                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();

                if (CurrentRole == "Admin")
                {
                    LogAdminActivity(CurrentUserID, "Delete", "Patient", patient.FullName, "Permanently deleted patient record.");
                }

                System.Diagnostics.Debug.WriteLine($"✓ Patient permanently deleted.");
                return (true, "Patient permanently deleted.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error deleting patient: {ex.Message}");
                return (false, $"Error deleting patient: {ex.Message}");
            }
        }

        #endregion

        #region Appointment and Schedule Operations

        public async Task<List<Appointment>> GetAppointmentsAsync(int? doctorId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Include(a => a.Resource)
                    .AsQueryable();

                if (doctorId.HasValue && doctorId.Value > 0)
                    query = query.Where(a => a.DoctorID == doctorId.Value);

                if (startDate.HasValue)
                    query = query.Where(a => a.AppointmentDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.AppointmentDate <= endDate.Value);

                return await query
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching appointments: {ex.Message}");
                return new List<Appointment>();
            }
        }

        public async Task<Appointment?> GetAppointmentByIdAsync(int appointmentId)
        {
            try
            {
                return await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Include(a => a.Resource)
                    .FirstOrDefaultAsync(a => a.AppointmentID == appointmentId);
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string Message, int AppointmentID)> CreateAppointmentAsync(
            int patientId,
            int doctorId,
            DateTime appointmentDate,
            int? resourceId,
            string status,
            int createdBy)
        {
            try
            {
                if (patientId <= 0)
                    return (false, "Please select a patient.", 0);
                if (doctorId <= 0)
                    return (false, "Please select a doctor.", 0);
                if (appointmentDate == default)
                    return (false, "Please select an appointment date and time.", 0);

                var patientExists = await _context.Patients.AnyAsync(p => p.PatientID == patientId && !p.IsArchived);
                if (!patientExists)
                    return (false, "Selected patient was not found.", 0);

                var doctorExists = await _context.Users.AnyAsync(u => u.UserID == doctorId && u.IsActive && u.Role != null && u.Role.RoleName == "Doctor");
                if (!doctorExists)
                    return (false, "Selected doctor was not found.", 0);

                var hasConflict = await _context.Appointments.AnyAsync(a =>
                    a.DoctorID == doctorId &&
                    a.AppointmentDate == appointmentDate &&
                    a.Status != "Cancelled");

                if (hasConflict)
                    return (false, "This doctor already has an appointment at that time.", 0);

                var creatorId = createdBy > 0 ? createdBy : doctorId;
                if (!await _context.Users.AnyAsync(u => u.UserID == creatorId))
                    creatorId = doctorId;

                var appointment = new Appointment
                {
                    PatientID = patientId,
                    DoctorID = doctorId,
                    ResourceID = resourceId > 0 ? resourceId : null,
                    AppointmentDate = appointmentDate,
                    Status = string.IsNullOrWhiteSpace(status) ? "Scheduled" : status,
                    CreatedBy = creatorId
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                return (true, "Appointment scheduled successfully.", appointment.AppointmentID);
            }
            catch (Exception ex)
            {
                return (false, $"Error scheduling appointment: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string Message)> UpdateAppointmentAsync(
            int appointmentId,
            int patientId,
            int doctorId,
            DateTime appointmentDate,
            int? resourceId,
            string status)
        {
            try
            {
                var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentID == appointmentId);
                if (appointment == null)
                    return (false, "Appointment not found.");

                if (patientId <= 0 || doctorId <= 0)
                    return (false, "Patient and doctor are required.");

                var hasConflict = await _context.Appointments.AnyAsync(a =>
                    a.AppointmentID != appointmentId &&
                    a.DoctorID == doctorId &&
                    a.AppointmentDate == appointmentDate &&
                    a.Status != "Cancelled");

                if (hasConflict)
                    return (false, "This doctor already has an appointment at that time.");

                appointment.PatientID = patientId;
                appointment.DoctorID = doctorId;
                appointment.ResourceID = resourceId > 0 ? resourceId : null;
                appointment.AppointmentDate = appointmentDate;
                appointment.Status = string.IsNullOrWhiteSpace(status) ? "Scheduled" : status;

                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();

                return (true, "Appointment updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating appointment: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateAppointmentStatusAsync(int appointmentId, string status)
        {
            try
            {
                var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentID == appointmentId);
                if (appointment == null)
                    return (false, "Appointment not found.");

                appointment.Status = string.IsNullOrWhiteSpace(status) ? appointment.Status : status;
                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();

                return (true, "Appointment status updated.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating appointment status: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAppointmentAsync(int appointmentId)
        {
            try
            {
                var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentID == appointmentId);
                if (appointment == null)
                    return (false, "Appointment not found.");

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                return (true, "Appointment deleted successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting appointment: {ex.Message}");
            }
        }

        public async Task<List<ClinicResource>> GetResourcesAsync()
        {
            try
            {
                return await _context.Resources
                    .OrderBy(r => r.ResourceName)
                    .ToListAsync();
            }
            catch
            {
                return new List<ClinicResource>();
            }
        }

        public async Task<List<DoctorSchedule>> GetDoctorSchedulesAsync(int? doctorId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.DoctorSchedules
                    .Include(s => s.Doctor)
                    .AsQueryable();

                if (doctorId.HasValue && doctorId.Value > 0)
                    query = query.Where(s => s.DoctorID == doctorId.Value);
                if (startDate.HasValue)
                    query = query.Where(s => s.AvailableDate >= startDate.Value.Date);
                if (endDate.HasValue)
                    query = query.Where(s => s.AvailableDate <= endDate.Value.Date);

                return await query
                    .OrderBy(s => s.AvailableDate)
                    .ThenBy(s => s.StartTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching schedules: {ex.Message}");
                return new List<DoctorSchedule>();
            }
        }

        public async Task<(bool Success, string Message, int ScheduleID)> CreateDoctorScheduleAsync(
            int doctorId,
            DateTime availableDate,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            try
            {
                if (doctorId <= 0)
                    return (false, "Please select a doctor.", 0);
                if (endTime <= startTime)
                    return (false, "End time must be after start time.", 0);

                var hasOverlap = await _context.DoctorSchedules.AnyAsync(s =>
                    s.DoctorID == doctorId &&
                    s.AvailableDate.Date == availableDate.Date &&
                    startTime < s.EndTime &&
                    endTime > s.StartTime);

                if (hasOverlap)
                    return (false, "This schedule overlaps an existing doctor schedule.", 0);

                var schedule = new DoctorSchedule
                {
                    DoctorID = doctorId,
                    AvailableDate = availableDate.Date,
                    StartTime = startTime,
                    EndTime = endTime
                };

                _context.DoctorSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                return (true, "Schedule slot added successfully.", schedule.ScheduleID);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating schedule: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string Message)> DeleteDoctorScheduleAsync(int scheduleId)
        {
            try
            {
                var schedule = await _context.DoctorSchedules.FirstOrDefaultAsync(s => s.ScheduleID == scheduleId);
                if (schedule == null)
                    return (false, "Schedule slot not found.");

                _context.DoctorSchedules.Remove(schedule);
                await _context.SaveChangesAsync();

                return (true, "Schedule slot removed.");
            }
            catch (Exception ex)
            {
                return (false, $"Error removing schedule: {ex.Message}");
            }
        }

        #endregion

        #region Logout

        public void Logout()
        {
            CurrentRole = null;
            CurrentUser = null;
            CurrentUserID = 0;
            CurrentUserData = null;
        }

        #endregion
    }
}
