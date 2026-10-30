# Validation and Error Handling Documentation - Healthcare Clinic System

## COMPLETE OVERVIEW - BULLET FORM

### 🎯 VALIDATION LAYERS

#### **INPUT VALIDATION (UI Level)**
• **Email Validation** (AuthService.IsValidEmail)
  - Pattern: `^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$`
  - Checks: Valid format, domain extension min 2 chars
  - Try-catch wrapper for regex errors
  - Returns: `bool` (true = valid)

• **Password Validation** (AuthService.IsValidPassword)
  - Minimum 6 characters required
  - Cannot be null or whitespace
  - Returns: `bool` (true = valid)

• **Field-Level Validation** (Register.razor.cs)
  - Clinic name: Required, min 2 chars
  - Full name: Required, min 3 chars
  - Email: Required + valid format
  - Password: Required, min 6 chars
  - Confirm password: Must match password field
  - Subscription plan: Must be selected

---

#### **BUSINESS LOGIC VALIDATION (Service Layer)**

• **Login Validation** (LoginService.LoginAsync → AuthService.LoginAsync)
  - Check: Email not empty → Message: "Please enter your email."
  - Check: Password not empty → Message: "Please enter your password."
  - Check: Email contains @ symbol → Message: "Please enter a valid email address."
  - Check: Email format valid (regex) → Message: "Please enter a valid email address."
  - Check: User exists in database → Message: "Invalid email or password."
  - Check: Password matches hash → Message: "Invalid email or password."
  - Check: User account active → Message: "User account is inactive."
  - Success: Return redirect route based on user role

• **Registration Validation** (AuthService.RegisterAdminAsync)
  - Clinic name: Required, non-empty, min 2 chars
  - Full name: Required, min 3 chars
  - Email: Required, valid format, non-empty
  - Password: Required, min 6 chars
  - Plan: Required, must exist in database
  - Email unique: Check no duplicates in database
  - Plan exists: Verify subscription plan is valid
  - Returns: `(bool Success, string Message)`

• **User Creation Validation** (AuthService.CreateUserAsync - Admin only)
  - Name: Required, min 3 chars
  - Email: Valid + unique (no duplicates allowed)
  - Role: Valid role enum (Super Admin, Admin, Doctor, Receptionist)
  - Staff limits enforced: MaxDoctors, MaxReceptionists from plan
  - Returns: `(bool Success, string Message)`

• **Patient Creation/Update Validation**
  - Full name: Required, min 3 chars
  - Birth date: Valid date, not future date
  - Gender: Valid enum value
  - Contact: Valid format if provided
  - Address: Non-empty if provided
  - Returns: `(bool Success, string Message)`

• **Staff Limit Validation** (AuthService.CanCreateStaffAsync)
  - Check: Current doctor count < plan.MaxDoctors
  - Check: Current receptionist count < plan.MaxReceptionists
  - Message if limit exceeded: "Cannot add more [role]. Upgrade your subscription."
  - Returns: `(bool CanCreate, string Message, int CurrentCount)`

• **Schedule Conflict Validation** (Doctor/Receptionist appointment booking)
  - Check: Doctor not double-booked at same time
  - Check: Patient appointment times don't overlap
  - Check: Appointment date not in past
  - Message if conflict: "Time slot already booked."
  - Returns: `(bool Valid, string Message)`

• **Subscription Feature Access Validation**
  - Check: plan.HasResourceAccess before viewing Resources page
  - Check: plan.HasAnalytics before viewing Reports & Analytics
  - Message if denied: "This feature is not included in your subscription."
  - Returns: `bool` (true = has access)

---

### 🔒 SECURITY VALIDATION

#### **Password Hashing & Verification** (PasswordHasher.cs)
• **Hash Method**
  - Library: BCrypt.NET-Next
  - One-way hashing (cannot be reversed)
  - Automatic salt generation per password
  - Work factor: 12 iterations (slow intentional)
  - Returns: `string` (hash value)

• **Verify Method**
  - Compares input password to stored hash
  - Try-catch for corrupted hash handling
  - Returns: `bool` (true = match)

• **What's NEVER stored**
  - ❌ Plain-text passwords
  - ❌ Encrypted passwords (can be reversed)
  - ❌ Password derivatives

---

### 🛡️ ERROR HANDLING

#### **TRY-CATCH PATTERNS**

• **Service Layer Error Handling** (AuthService, LoginService)
  - Wraps all database operations
  - Wraps all business logic
  - Catches: DbException, ArgumentException, Exception
  - Returns: User-friendly message (never exposes internal details)
  - Logs: Error to Debug output for developer debugging
  - Example: "An error occurred during login. Please try again."

• **Component-Level Error Handling** (Register.razor, Admin pages)
  - Wraps async service calls
  - Wraps data loading operations
  - Displays: Success/error messages in modal dialogs
  - Auto-close: Success messages after 800ms
  - Stay-open: Error messages for user correction
  - Clears: Password fields on auth failures

• **Database Operation Safety** (ClinicDbContext, services)
  - Try-catch around `SaveChangesAsync()`
  - Try-catch around `FirstOrDefaultAsync()`, `ToListAsync()`
  - Try-catch around LINQ queries
  - Handles: Constraint violations, connection errors

---

### 📱 UI ERROR DISPLAY

#### **Visual Feedback**

• **Success Messages**
  - Background: Green (#4CAF50)
  - Icon: Check mark
  - Display: 800ms auto-dismiss
  - Closes modal after delay
  - Example: "Registration successful! Redirecting..."

• **Error Messages**
  - Background: Red (#F44336)
  - Icon: Alert icon
  - Display: Stays until dismissed
  - User can fix and retry
  - Example: "Please enter a valid email address."

• **Field-Level Errors**
  - Red error text below input field
  - Appears on blur or change event
  - Clears when field becomes valid
  - Real-time validation feedback

• **Form Validation**
  - Button disabled if form invalid
  - Multiple error messages for different issues
  - Clear all errors on modal close
  - Re-validate all fields on submit

---

### 📊 VALIDATION METHODS BY LOCATION

| Method | Location | Validates | Returns |
|--------|----------|-----------|---------|
| **IsValidEmail** | AuthService | Email format | bool |
| **IsValidPassword** | AuthService | Password length (6+) | bool |
| **LoginAsync** | LoginService → AuthService | Credentials + user active | (bool, string, role) |
| **RegisterAdminAsync** | AuthService | 8 registration fields | (bool, string) |
| **CreateUserAsync** | AuthService | Name, email, role, limits | (bool, string) |
| **CanCreateStaffAsync** | AuthService | Doctor/receptionist limits | (bool, string, count) |
| **CreatePatientAsync** | AuthService | Name, birth date | (bool, string) |
| **HashPassword** | PasswordHasher | N/A (hashing) | string |
| **VerifyPassword** | PasswordHasher | Hash vs password | bool |
| **ValidateField** | Register.razor.cs | Individual form fields | None (sets error string) |
| **ValidateAllFields** | Register.razor.cs | All form fields | bool |

---

### ✅ VALIDATION FLOW EXAMPLES

#### **Login Flow**
```
User enters email/password
    ↓
LoginService.LoginAsync() called
    ↓ ValidateInput(email, password)
    ├─ Empty email? → Return error message
    ├─ Empty password? → Return error message
    ├─ No @ in email? → Return error message
    ↓ (if passes)
AuthService.LoginAsync(email, password)
    ├─ Email/password empty? → Return error
    ├─ Invalid email format? → Return error
    ├─ User not found? → Return "Invalid email or password."
    ├─ Password doesn't match? → Return "Invalid email or password."
    ├─ User inactive? → Return "User account is inactive."
    ↓ (if all pass)
Return success + role
    ↓
Redirect to role dashboard
```

#### **Registration Flow**
```
User fills registration form
    ↓
ValidateAllFields() called on submit
    ├─ Clinic name < 2 chars? → Show error below field
    ├─ Full name < 3 chars? → Show error below field
    ├─ Email invalid? → Show error below field
    ├─ Password < 6 chars? → Show error below field
    ├─ Password ≠ confirm? → Show error below field
    ├─ Plan not selected? → Show error below field
    ↓ (if any error, stop)
Form invalid → Cannot submit
    ↓ (all fields pass)
AuthService.RegisterAdminAsync() called
    ├─ Email already exists? → Show error message
    ├─ Plan doesn't exist? → Show error message
    ├─ Database error? → Show generic error message
    ↓ (if all pass)
Return success
    ↓
Show "Registration successful! Redirecting..."
    ↓
Redirect to Login page after 1.5s
```

#### **Staff Creation Flow (Admin)**
```
Admin clicks "Create User"
    ↓
Modal opens with form
    ↓
Admin fills name, email, role
    ↓
Admin clicks "Create"
    ↓
Validation checks:
    ├─ Name required? ✓
    ├─ Email valid? ✓
    ├─ Email unique? ✓
    ├─ Role valid? ✓
    ├─ Can add more [role]? → Check CanCreateStaffAsync()
    │   ├─ If limit reached → Show "Cannot add more. Upgrade plan."
    │   ├─ If within limit → Continue
    ↓ (if all pass)
CreateUserAsync() called
    ├─ Try-catch wraps database insert
    ├─ On success → Show green "User created!" message
    ├─ On error → Show red error message
    ↓
Modal closes on success (800ms delay)
    ↓
Reload user list
```

---

### 🔍 ERROR MESSAGE INVENTORY

| Error | Trigger | Source | Message |
|-------|---------|--------|---------|
| **Empty Email** | Input null/empty | LoginService.ValidateInput | "Please enter your email." |
| **Empty Password** | Input null/empty | LoginService.ValidateInput | "Please enter your password." |
| **Invalid Email Format** | No @ symbol | LoginService.ValidateInput | "Please enter a valid email address." |
| **Invalid Email Format** | Regex fails | AuthService.IsValidEmail | "Please enter a valid email address." |
| **User Not Found** | Email not in DB | AuthService.LoginAsync | "Invalid email or password." |
| **Password Mismatch** | Hash verify fails | AuthService.LoginAsync | "Invalid email or password." |
| **Inactive User** | user.IsActive == false | AuthService.LoginAsync | "User account is inactive." |
| **Duplicate Email** | Email exists in DB | AuthService.RegisterAdminAsync | "Email already registered." |
| **Invalid Plan** | Plan not in DB | AuthService.RegisterAdminAsync | "Selected plan does not exist." |
| **Password Too Short** | Length < 6 | AuthService.IsValidPassword | "Password must be at least 6 characters." |
| **Passwords Mismatch** | Input ≠ confirm | Register.razor.cs | "Passwords do not match." |
| **Staff Limit Exceeded** | Current >= max | AuthService.CanCreateStaffAsync | "Cannot add more [role]. Upgrade subscription." |
| **Schedule Conflict** | Time overlap | Appointment validation | "Time slot already booked." |
| **Feature Denied** | No plan access | Subscription validation | "This feature is not included in your subscription." |
| **Database Error** | Exception thrown | Services catch block | "An error occurred. Please try again." |
| **Form Invalid** | Multiple field errors | Register.razor.cs | "Please fix the errors above." |

---

### 🎯 BEST PRACTICES IMPLEMENTED

✅ **Multi-layer validation** - Input, business logic, database  
✅ **User-friendly messages** - No technical jargon exposed  
✅ **Try-catch error safety** - All critical operations wrapped  
✅ **Secure password hashing** - BCrypt with salt + work factor  
✅ **Email format validation** - Regex + real format check  
✅ **Duplicate prevention** - Unique email constraint  
✅ **Referential integrity** - Foreign key checks  
✅ **Subscription limit enforcement** - Staff creation limits checked  
✅ **Activity logging** - Debug output for developer review  
✅ **Graceful error recovery** - Form stays open for correction  
✅ **No sensitive data in errors** - Generic messages for security  
✅ **Field-level feedback** - Real-time validation display  
✅ **Modal validation** - All fields checked before submission  
✅ **Password field clearing** - Cleared on auth failures  

---

### 🚀 FUTURE IMPROVEMENTS RECOMMENDED

📋 **Input Sanitization**
  - Add SQL injection prevention (edge cases)
  - Add XSS prevention (sanitize special characters)

📋 **Rate Limiting**
  - Limit login attempts (e.g., 5 attempts/5 minutes)
  - Lockout account after N failed attempts
  - Show countdown timer

📋 **Password Reset**
  - Send reset link via email
  - Link expires after 30 minutes
  - Set new password with validation

📋 **Two-Factor Authentication (2FA)**
  - Email verification code
  - SMS verification (optional)
  - Backup codes for account recovery

📋 **Enhanced Logging**
  - Use Serilog for structured logging
  - Log validation failures with user ID
  - Alert admin on suspicious patterns

📋 **Async Validation**
  - Validate email uniqueness as user types
  - Check password strength in real-time
  - Show validation progress

📋 **Batch Operation Safety**
  - Transaction rollback on any error
  - Atomic all-or-nothing operations
  - Detailed batch error reports

---

## Overview

The Healthcare Clinic System implements multi-layer validation and error handling to ensure data integrity, security, and user experience.

---

## **1. INPUT VALIDATION LAYER**

### **1.1 Email Validation**

• **Method**: `AuthService.IsValidEmail(string email)`
  - Location: [Services/AuthService.cs](Services/AuthService.cs#L115)
  - Pattern: `^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$`
  - Validates: Format, domain extension (minimum 2 chars)
  - Return: `bool` (true = valid, false = invalid)
  - Exception Handling: Try-catch wraps regex validation

```csharp
public bool IsValidEmail(string email)
{
    try
    {
        var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }
    catch { return false; }
}
```

### **1.2 Password Validation**

• **Method**: `AuthService.IsValidPassword(string password)`
  - Location: [Services/AuthService.cs](Services/AuthService.cs#L125)
  - Rules:
    - Minimum 6 characters
    - Cannot be null
  - Return: `bool`

```csharp
public bool IsValidPassword(string password)
{
    return password != null && password.Length >= 6;
}
```

### **1.3 Field-Level Validation**

**Components with field validation:**

• **Login.razor**
  - Email: Non-empty + valid format (handled by LoginService.ValidateInput)
  - Password: Non-empty

• **Register.razor**
  - Clinic Name: Non-empty + min 3 chars
  - Full Name: Non-empty + min 3 chars
  - Email: Non-empty + valid format
  - Password: Min 6 chars
  - Confirm Password: Must match password
  - Plan: Must select one

• **Admin/UserManagement.razor**
  - Name: Required + min 3 chars
  - Email: Required + valid format (must be unique)
  - Role: Required (Doctor/Receptionist)
  - Password: Min 6 chars (auto-generated if empty)

• **Patient Forms (Admin & Receptionist)**
  - Full Name: Required + min 3 chars
  - Birth Date: Valid date format + reasonable age (not future)
  - Gender: Required
  - Contact: Valid phone format
  - Address: Optional

---

## **2. BUSINESS LOGIC VALIDATION**

### **2.1 Login Validation**

**Service Chain**: `LoginService` → `AuthService`

**Validation Flow**:

```
LoginService.LoginAsync(email, password)
    ↓
ValidateInput(email, password)
    ├─ Email empty? → "Please enter your email."
    ├─ Password empty? → "Please enter your password."
    ├─ No @ symbol? → "Please enter a valid email address."
    ↓ (if passes)
AuthService.LoginAsync(email, password)
    ├─ Email/password empty? → "Email and password are required."
    ├─ Invalid email format? → "Please enter a valid email address."
    ├─ User not found? → "Invalid email or password."
    ├─ Password mismatch? → "Invalid email or password."
    ├─ User inactive? → "User account is inactive."
    ↓ (if passes)
Login successful → Redirect to role dashboard
```

**Return Type**: `LoginResult` (custom result object)
- `Success: bool` - True if login successful
- `Message: string` - User-friendly message
- `RedirectUrl: string?` - Navigation URL on success
- `ClearPassword: bool` - Whether to clear password field on error

### **2.2 Registration Validation**

**Method**: `AuthService.RegisterAdminAsync(clinicName, fullName, email, password, planName)`
  - Location: [Services/AuthService.cs](Services/AuthService.cs#L200+)

**Validation Checks**:

```
✓ Clinic name: Required, non-empty
✓ Full name: Required, min 3 chars, non-empty
✓ Email: Required, non-empty, valid format
✓ Password: Required, non-empty, min 6 chars
✓ Plan: Required, non-empty, exists in database
✓ Email unique: Checks if email already registered
✓ Plan exists: Verifies subscription plan exists
```

**Return**: `(bool Success, string Message)`

### **2.3 User Creation Validation (Admin)**

**Method**: `AuthService.CreateUserAsync(...)`

**Checks**:
- Name: Non-empty, min 3 chars
- Email: Valid + unique (no duplicates)
- Role: Valid role enum
- Staff limits enforced (see section 2.5)

### **2.4 Patient Creation/Update Validation**

**Methods**:
- `AuthService.CreatePatientAsync(...)`
- `AuthService.UpdatePatientAsync(...)`

**Validation**:
- Full name: Required, min 3 chars
- Birth date: Valid date, not future
- Gender: Valid enum value
- Contact: Valid format
- Address: Optional but if provided, non-empty

### **2.5 Staff Limit Validation**

**Method**: `AuthService.CanCreateStaffAsync(userId, roleType)`
  - Location: [Services/AuthService.cs](Services/AuthService.cs#L~1000+)

**Logic**:
```csharp
// Check current active staff count by role
var currentDoctors = await Db.Users
    .CountAsync(u => u.Role == "Doctor" && u.IsActive);

var plan = await GetActiveSubscriptionPlanAsync(userId);

if (plan.MaxDoctors != null && currentDoctors >= plan.MaxDoctors)
    return (false, "Doctor limit reached. Upgrade subscription.", 0);
```

**Returns**: `(bool CanCreate, string Message, int Remaining)`

**Subscription Plan Limits**:

| Plan | Max Doctors | Max Receptionists | Has Resources | Has Analytics |
|------|-------------|------------------|---------------|---------------|
| Free | 1 | 1 | ❌ | ❌ |
| Professional | 5 | 3 | ✅ | ❌ |
| Enterprise | Unlimited | Unlimited | ✅ | ✅ |

### **2.6 Schedule Conflict Validation**

**Method**: `AuthService.CreateDoctorScheduleAsync(doctorId, date, startTime, endTime)`

**Checks**:
```csharp
if (doctorId <= 0)
    return (false, "Please select a doctor.", 0);

if (endTime <= startTime)
    return (false, "End time must be after start time.", 0);

var hasOverlap = await Db.DoctorSchedules.AnyAsync(s =>
    s.DoctorID == doctorId &&
    s.AvailableDate.Date == availableDate.Date &&
    startTime < s.EndTime &&
    endTime > s.StartTime);

if (hasOverlap)
    return (false, "This schedule overlaps an existing doctor schedule.", 0);
```

### **2.7 Subscription Feature Access Validation**

**Methods**:
- `AuthService.GetActiveSubscriptionAsync(int userId)`
- `AuthService.GetActiveSubscriptionPlanAsync(int userId)`

**Validation Gates**:

```
Resource Access:
    ├─ Component: Admin/Resources.razor
    ├─ Check: plan.HasResourceAccess
    └─ Action: Hide "Add Resource" button if false

Analytics Access:
    ├─ Component: Admin/ReportsAnalytics.razor
    ├─ Check: plan.HasAnalytics
    └─ Action: Show "Upgrade" message if false

Staff Limits:
    ├─ Component: Admin/UserManagement.razor
    ├─ Check: CanCreateStaffAsync()
    └─ Action: Disable "Create" button if limit reached
```

---

## **3. PASSWORD SECURITY**

### **3.1 Password Hashing**

**Library**: BCrypt.NET
  - Location: [Utilities/PasswordHasher.cs](Utilities/PasswordHasher.cs)

**Methods**:

```csharp
// Hash password for storage
public static string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password);
}

// Verify login password
public static bool VerifyPassword(string password, string hash)
{
    try
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
    catch { return false; }
}
```

**Security Features**:
- ✅ One-way hashing (cannot reverse)
- ✅ Salt included automatically
- ✅ Work factor: 12 iterations (slow intentional)
- ✅ Exception handling for corrupted hashes

**Never Store**:
- ❌ Plain-text passwords
- ❌ Encrypted passwords (can be reversed)
- ❌ Password derivatives

---

## **4. TRY-CATCH ERROR HANDLING**

### **4.1 Service Layer Error Handling**

**Pattern**: Graceful degradation with user messages

```csharp
try
{
    // Validation checks
    if (string.IsNullOrWhiteSpace(email))
        return (false, "Email is required.", null);
    
    // Database operations
    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == email);
    
    if (user == null)
        return (false, "Invalid email or password.", null);
}
catch (Exception ex)
{
    // Log error for debugging
    System.Diagnostics.Debug.WriteLine($"❌ Error: {ex.Message}");
    
    // Return safe user message (no internal details exposed)
    return (false, "An error occurred during login. Please try again.", null);
}
```

### **4.2 Component-Level Error Handling**

**Pattern**: Modal dialogs + validation messages

```razor
@code {
    private async Task CreateUserAsync()
    {
        isSubmitting = true;
        modalMessage = null;
        
        try
        {
            // Call service
            var result = await Auth.CreateUserAsync(...);
            
            // Handle result
            modalSuccess = result.Success;
            modalMessage = result.Message;
            
            if (result.Success)
            {
                // Reload data
                await LoadUsers();
                // Close modal after delay
                await Task.Delay(800);
                CloseModal();
            }
        }
        catch (Exception ex)
        {
            modalSuccess = false;
            modalMessage = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"❌ {ex}");
        }
    }
}
```

**User Feedback**:
- Success: Green alert with message + auto-close modal
- Error: Red alert with message + stay in modal for correction

### **4.3 Database Operation Safety**

**EF Core Exception Handling**:

```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateException ex)
{
    // Constraint violation, duplicate key, etc.
    return (false, "Unable to save. Please check your input.", null);
}
catch (DbUpdateConcurrencyException ex)
{
    // Record changed between read and write
    return (false, "Record was modified. Please refresh and try again.", null);
}
catch (Exception ex)
{
    // Other database errors
    return (false, "Database error occurred. Please try again.", null);
}
```

---

## **5. UI ERROR DISPLAY**

### **5.1 Validation Messages (Inline)**

**Used in**: Forms with multiple fields

```razor
@if (errors.ContainsKey("email") && !string.IsNullOrEmpty(errors["email"]))
{
    <span class="error-text">@errors["email"]</span>
}
```

**Styling** ([wwwroot/app.css](wwwroot/app.css#L49)):
```css
.validation-message {
    color: #d32f2f;
    font-size: 0.85em;
    margin-top: 4px;
}

.error-text {
    color: #d32f2f;
    font-weight: 500;
}
```

### **5.2 Alert Messages (Page/Modal)**

**Success Alert**:
```html
<div class="alert alert-success">
    Operation successful. User created.
</div>
```

**Error Alert**:
```html
<div class="alert alert-error">
    @modalMessage
</div>
```

**Styling** ([wwwroot/app.css](wwwroot/app.css#L672)):
```css
.alert-success {
    background: #4caf50;
    color: white;
    border-radius: 8px;
    padding: 16px;
}

.alert-error {
    background: #f44336;
    color: white;
    border-radius: 8px;
    padding: 16px;
}
```

### **5.3 Global Error UI**

**Location**: [wwwroot/index.html](wwwroot/index.html#L19)

```html
<div id="blazor-error-ui" data-nosnippet>
    <environment include="Staging,Production">
        <p>An unhandled error has occurred.</p>
        <p><a href="" class="reload">Reload</a></p>
    </environment>
    <environment include="Development">
        <p>An unhandled exception has occurred. See browser console for details.</p>
        <p><a class="dismiss">🗙</a></p>
    </environment>
</div>
```

**Styling** ([wwwroot/app.css](wwwroot/app.css#L72)):
```css
#blazor-error-ui {
    background: #ffebee;
    bottom: 0;
    box-shadow: 0 -1px 2px rgba(0,0,0,0.05);
    display: none;
    left: 0;
    padding: 20px;
    position: fixed;
    right: 0;
    z-index: 1000;
}
```

---

## **6. VALIDATION RESULT PATTERNS**

### **6.1 Service Return Types**

**Pattern 1: Tuple Response**
```csharp
public async Task<(bool Success, string Message)> CreatePatientAsync(...)
{
    return (true, "Patient created successfully.");
}
```

**Pattern 2: Tuple with Additional Data**
```csharp
public async Task<(bool Success, string Message, int ScheduleID)> CreateDoctorScheduleAsync(...)
{
    return (true, "Schedule created.", scheduleId);
}
```

**Pattern 3: Custom Result Object**
```csharp
public async Task<LoginResult> LoginAsync(string email, string password)
{
    return LoginResult.Succeeded("Login successful!", redirectUrl);
    // or
    return LoginResult.Failed("Invalid credentials", clearPassword: true);
}
```

### **6.2 Result Object Properties**

**LoginResult**:
- `Success: bool` - Operation succeeded
- `Message: string` - User message
- `RedirectUrl: string?` - Navigation target
- `ClearPassword: bool` - Clear password field

**StaffLimitInfo** (readonly record):
```csharp
public record StaffLimitInfo(
    string RoleName,
    int ActiveCount,
    int? MaxAllowed
)
{
    public int? Remaining => MaxAllowed.HasValue ? 
        Math.Max(0, MaxAllowed.Value - ActiveCount) : null;
    
    public bool IsAtLimit => MaxAllowed.HasValue && 
        ActiveCount >= MaxAllowed.Value;
    
    public string LimitText => MaxAllowed.HasValue ? 
        $"{ActiveCount}/{MaxAllowed.Value}" : 
        $"{ActiveCount}/Unlimited";
}
```

---

## **7. ACTIVITY LOGGING**

### **7.1 Admin Activity Tracking**

**Method**: `AuthService.LogAdminActivity(...)`

**Logged Events**:
- User creation/deletion
- Patient creation/updates
- Schedule changes
- Prescription changes
- Invoice updates
- Resource modifications

**Data Captured**:
- Timestamp (UTC)
- Admin user ID & name
- Action (Create, Update, Delete, etc.)
- Entity type (Patient, Doctor, etc.)
- Entity name/ID
- Additional details

**Example**:
```csharp
LogAdminActivity(
    adminUserId: 1,
    action: "Create",
    entityType: "Doctor",
    entityName: "Dr. John Smith",
    details: "Assigned to clinic, created schedule"
);
```

**Retrieval Methods**:
- `GetRecentAdminActivities(take: 30)` - Last 30 activities
- `GetUnreadAdminActivityCount()` - Unread notifications
- `MarkAdminActivityAsRead(activityId)` - Mark as read

---

## **8. DATA INTEGRITY CHECKS**

### **8.1 Null Reference Prevention**

**Pattern**:
```csharp
// Always check before use
var patient = await GetPatientAsync(patientId);
if (patient == null)
    return (false, "Patient not found.");

// Or use null-coalescing
var name = patient?.FullName ?? "Unknown";
```

### **8.2 Unique Constraint Handling**

**Email Uniqueness**:
```csharp
var existingUser = await _context.Users
    .FirstOrDefaultAsync(u => u.Email == email);

if (existingUser != null)
    return (false, "Email already registered.");
```

### **8.3 Referential Integrity**

**Foreign Key Checks**:
```csharp
// Verify doctor exists before creating appointment
var doctor = await _context.Doctors
    .FirstOrDefaultAsync(d => d.DoctorID == doctorId);

if (doctor == null)
    return (false, "Selected doctor not found.");
```

---

## **9. ERROR MESSAGES GUIDE**

### **User-Friendly Messages**

✅ **Good**: "Email address already registered. Please use a different email."  
❌ **Bad**: "Unique constraint violation on Users.Email"

✅ **Good**: "Doctor limit reached. Upgrade to add more doctors."  
❌ **Bad**: "Staff limit constraint failed"

✅ **Good**: "Please enter a password with at least 6 characters."  
❌ **Bad**: "Password validation failed"

✅ **Good**: "An error occurred while saving. Please try again."  
❌ **Bad**: "DbUpdateException: Inner exception..."

### **Security Considerations**

- **Never expose**: Database schema, query details, system paths
- **Always sanitize**: User input before display
- **Log internally**: Full exception details for debugging
- **Show users**: Simple, actionable messages

---

## **10. SUMMARY TABLE**

| Layer | Method | Validation | Error Handling | Return |
|-------|--------|-----------|----------------|--------|
| **Input** | IsValidEmail | Regex pattern | Try-catch | bool |
| **Input** | IsValidPassword | Length >= 6 | None | bool |
| **Business** | LoginAsync | Email, password | Try-catch | (bool, string, role?) |
| **Business** | RegisterAdminAsync | 8 fields checked | Try-catch | (bool, string) |
| **Business** | CreateUserAsync | Email unique, role valid | Try-catch | (bool, string) |
| **Business** | CanCreateStaffAsync | Staff limits | Direct check | (bool, string, count) |
| **Business** | CreatePatientAsync | Name, birth date | Try-catch | (bool, string) |
| **Security** | HashPassword | None | Internal BCrypt | string (hash) |
| **Security** | VerifyPassword | Hash format | Try-catch | bool |
| **UI** | Component forms | Field validation | State update | Dictionary<string> |
| **Logging** | LogAdminActivity | Activity valid | Locking mechanism | void |

---

## **11. BEST PRACTICES IMPLEMENTED**

✅ Multi-layer validation (input, business, database)  
✅ User-friendly error messages  
✅ Try-catch for exception safety  
✅ Secure password hashing (BCrypt)  
✅ Email format validation  
✅ Duplicate prevention (email, schedules)  
✅ Referential integrity checks  
✅ Subscription limit enforcement  
✅ Activity logging for audit trail  
✅ Graceful error recovery  
✅ No sensitive data in error messages  

---

## **Future Improvements**

📋 **Recommended Enhancements**:
- Implement input sanitization (prevent SQL injection in edge cases)
- Add rate limiting on login attempts
- Implement password reset with token expiration
- Add two-factor authentication (2FA)
- Enhance logging with structured logging (Serilog)
- Implement form-level async validation
- Add batch operation error rollback
