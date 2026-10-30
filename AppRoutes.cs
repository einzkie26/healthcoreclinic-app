namespace healthcoreclinic_app_demo
{
    public static class AppRoutes
    {
        public const string Login = "/";
        public const string Register = "/register";
        public const string Dashboard = "/dashboard";
        public const string NotFound = "/not-found";

        public const string SuperAdmin = "/superadmin";
        public const string SuperAdminUsers = "/superadmin/users";
        public const string SuperAdminSubscriptions = "/superadmin/subscriptions";
        public const string SuperAdminPlans = "/superadmin/plans";
        public const string SuperAdminAccessControl = "/superadmin/access-control";
        public const string SuperAdminResources = "/superadmin/resources";
        public const string SuperAdminReports = "/superadmin/reports";

        public const string Admin = "/admin";
        public const string AdminUsers = "/admin/users";
        public const string AdminSubscription = "/admin/subscription";
        public const string AdminResources = "/admin/resources";
        public const string AdminArchive = "/admin/archive";
        public const string AdminReports = "/admin/reports";
        public const string AdminPatients = "/admin/patients";

        public const string Doctor = "/doctor";
        public const string DoctorAppointments = "/doctor/appointments";
        public const string DoctorPatients = "/doctor/patients";
        public const string DoctorMedicalRecords = "/doctor/medical-records";
        public const string DoctorPrescriptions = "/doctor/prescriptions";
        public const string DoctorSchedule = "/doctor/schedule";

        public const string Receptionist = "/receptionist";
        public const string ReceptionistPatients = "/receptionist/patients";
        public const string ReceptionistAppointments = "/receptionist/appointments";
        public const string ReceptionistBilling = "/receptionist/billing";
        public const string ReceptionistInvoices = "/receptionist/invoices";
    }
}
