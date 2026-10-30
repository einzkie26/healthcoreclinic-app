namespace healthcoreclinic_app_demo.Models
{
    public class Role
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;

        // Navigation
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
    }

    public class RolePermission
    {
        public int RolePermissionID { get; set; }
        public int RoleID { get; set; }
        public string PermissionKey { get; set; } = string.Empty;
        public bool IsAllowed { get; set; }

        // Navigation
        public virtual Role? Role { get; set; }
    }

    public class User
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int RoleID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByAdminID { get; set; }

        // Navigation
        public virtual Role? Role { get; set; }
        public virtual User? CreatedByAdmin { get; set; }
        public virtual ICollection<User> CreatedStaff { get; set; } = new List<User>();
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }

    public class SubscriptionPlan
    {
        public int PlanID { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int? MaxDoctors { get; set; }
        public int? MaxReceptionists { get; set; }
        public bool HasResourceAccess { get; set; }
        public bool HasAnalytics { get; set; }

        // Navigation
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }

    public class Subscription
    {
        public int SubscriptionID { get; set; }
        public int AdminID { get; set; }
        public int PlanID { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "Active";

        // Navigation
        public virtual User? Admin { get; set; }
        public virtual SubscriptionPlan? Plan { get; set; }
    }

    public class Patient
    {
        public int PatientID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? ContactNumber { get; set; }
        public string? Address { get; set; }
        public bool IsArchived { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }

    public class DoctorSchedule
    {
        public int ScheduleID { get; set; }
        public int DoctorID { get; set; }
        public DateTime AvailableDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        // Navigation
        public virtual User? Doctor { get; set; }
    }

    public class Resource
    {
        public int ResourceID { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public int Quantity { get; set; } = 0;
        public string Status { get; set; } = "Available";

        // Navigation
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }

    public class Appointment
    {
        public int AppointmentID { get; set; }
        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public int? ResourceID { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = "Scheduled";
        public int CreatedBy { get; set; }

        // Navigation
        public virtual Patient? Patient { get; set; }
        public virtual User? Doctor { get; set; }
        public virtual Resource? Resource { get; set; }
        public virtual User? CreatedByUser { get; set; }
    }

    public class MedicalRecord
    {
        public int RecordID { get; set; }
        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Patient? Patient { get; set; }
        public virtual User? Doctor { get; set; }
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }

    public class Prescription
    {
        public int PrescriptionID { get; set; }
        public int RecordID { get; set; }
        public string Medication { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;

        // Navigation
        public virtual MedicalRecord? Record { get; set; }
    }

    public class Invoice
    {
        public int InvoiceID { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int PatientID { get; set; }
        public int CreatedBy { get; set; }
        public string ServiceDescription { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Patient? Patient { get; set; }
        public virtual User? CreatedByUser { get; set; }
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class Payment
    {
        public int PaymentID { get; set; }
        public int InvoiceID { get; set; }
        public decimal AmountPaid { get; set; }
        public string Method { get; set; } = "Cash";
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Invoice? Invoice { get; set; }
    }
}
