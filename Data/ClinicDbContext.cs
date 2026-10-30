using Microsoft.EntityFrameworkCore;
using healthcoreclinic_app_demo.Models;

namespace healthcoreclinic_app_demo.Data
{
    public class ClinicDbContext : DbContext
    {
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure primary keys
            modelBuilder.Entity<Role>().HasKey(r => r.RoleID);
            modelBuilder.Entity<RolePermission>().HasKey(rp => rp.RolePermissionID);
            modelBuilder.Entity<User>().HasKey(u => u.UserID);
            modelBuilder.Entity<SubscriptionPlan>().HasKey(p => p.PlanID);
            modelBuilder.Entity<Subscription>().HasKey(s => s.SubscriptionID);
            modelBuilder.Entity<Patient>().HasKey(p => p.PatientID);
            modelBuilder.Entity<DoctorSchedule>().HasKey(ds => ds.ScheduleID);
            modelBuilder.Entity<Resource>().HasKey(r => r.ResourceID);
            modelBuilder.Entity<Appointment>().HasKey(a => a.AppointmentID);
            modelBuilder.Entity<MedicalRecord>().HasKey(m => m.RecordID);
            modelBuilder.Entity<Prescription>().HasKey(p => p.PrescriptionID);
            modelBuilder.Entity<Invoice>().HasKey(i => i.InvoiceID);
            modelBuilder.Entity<Payment>().HasKey(p => p.PaymentID);

            // Configure table names
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<RolePermission>().ToTable("RolePermissions");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<SubscriptionPlan>().ToTable("SubscriptionPlans");
            modelBuilder.Entity<Subscription>().ToTable("Subscriptions");
            modelBuilder.Entity<Patient>().ToTable("Patients");
            modelBuilder.Entity<DoctorSchedule>().ToTable("DoctorSchedules");
            modelBuilder.Entity<Resource>().ToTable("Resources");
            modelBuilder.Entity<Appointment>().ToTable("Appointments");
            modelBuilder.Entity<MedicalRecord>().ToTable("MedicalRecords");
            modelBuilder.Entity<Prescription>().ToTable("Prescriptions");
            modelBuilder.Entity<Invoice>().ToTable("Invoices");
            modelBuilder.Entity<Payment>().ToTable("Payments");

            // Configure User-Role relationship
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleID);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(rp => rp.RoleID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Subscription relationships
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Admin)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(s => s.AdminID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PlanID);

            // Configure Appointment relationships
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure MedicalRecord relationships
            modelBuilder.Entity<MedicalRecord>()
                .HasOne(m => m.Doctor)
                .WithMany()
                .HasForeignKey(m => m.DoctorID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Invoice relationships
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.CreatedByUser)
                .WithMany()
                .HasForeignKey(i => i.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure DoctorSchedule relationship
            modelBuilder.Entity<DoctorSchedule>()
                .HasOne(ds => ds.Doctor)
                .WithMany()
                .HasForeignKey(ds => ds.DoctorID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.CreatedByAdmin)
                .WithMany(u => u.CreatedStaff)
                .HasForeignKey(u => u.CreatedByAdminID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
