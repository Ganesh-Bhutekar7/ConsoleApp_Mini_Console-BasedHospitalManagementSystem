using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ------------------------------------------
// Utility class for Console UI formatting
// ------------------------------------------
public static class UiHelper
{
    public static void TitleBanner(string title)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Magenta;
        int width = Console.WindowWidth;
        string banner = $"🏥 {title.ToUpper()} 🏥";
        int left = Math.Max(0, (width - banner.Length) / 2);
        Console.SetCursorPosition(left, 1);
        Console.WriteLine(banner);
        Console.WriteLine(new string('═', width));
        Console.ResetColor();
    }

    public static void Header(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n═══════════════════════════════════");
        Console.WriteLine($" {title.ToUpper()} ");
        Console.WriteLine("═══════════════════════════════════");
        Console.ResetColor();
    }

    public static void Info(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✅ {message}");
        Console.ResetColor();
    }

    public static void Warn(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  ⚠️  {message}");
        Console.ResetColor();
    }

    public static void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ❌ {message}");
        Console.ResetColor();
    }

    public static string Ask(string prompt)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"  => {prompt}: ");
        Console.ResetColor();
        return Console.ReadLine() ?? "";
    }

    public static void DisplayTable<T>(IEnumerable<T> items, string[] headers, Func<T, string[]> getRow)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n" + new string('-', Console.WindowWidth));
        Console.WriteLine(string.Join(" | ", headers));
        Console.WriteLine(new string('-', Console.WindowWidth));
        Console.ResetColor();
        foreach (var item in items)
        {
            Console.WriteLine(string.Join(" | ", getRow(item)));
        }
        Console.WriteLine(new string('-', Console.WindowWidth));
    }
}

// ------------------------------------------
// Models
// ------------------------------------------
public abstract class Person
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "";
}

public class Patient : Person
{
    public string Condition { get; set; } = "";
    public string RoomNumber { get; set; } = "";
    public bool IsAdmitted { get; set; }
    public List<string> Prescriptions { get; } = new List<string>();
}

public class Doctor : Person
{
    public string Specialty { get; set; } = "";
}

public class Appointment
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public DateTime When { get; set; }
}

public class Charge
{
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
}

public class Bill
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public List<Charge> Charges { get; } = new List<Charge>();
    public decimal Total => Charges.Sum(c => c.Amount);

    public static Bill operator +(Bill b, Charge c)
    {
        b.Charges.Add(c);
        return b;
    }

    public string TotalFormatted => Total.ToString("C", new System.Globalization.CultureInfo("en-IN"));
}

// ------------------------------------------
// Services
// ------------------------------------------
public class AppointmentScheduler
{
    private readonly List<Appointment> _appointments = new();

    public async Task ScheduleAsync(Appointment appt)
    {
        await Task.Delay(100); // simulate DB delay
        if (_appointments.Any(a => a.PatientId == appt.PatientId &&
                                   a.DoctorId == appt.DoctorId &&
                                   a.When == appt.When))
        {
            throw new DuplicateAppointmentException("Duplicate appointment found.");
        }
        _appointments.Add(appt);
    }

    public bool DeleteAppointment(Guid id)
    {
        var appt = _appointments.FirstOrDefault(a => a.Id == id);
        if (appt != null)
        {
            _appointments.Remove(appt);
            return true;
        }
        return false;
    }

    public IEnumerable<Appointment> All => _appointments;
}

public class DuplicateAppointmentException : Exception
{
    public DuplicateAppointmentException(string msg) : base(msg) { }
}

public class RoomManager
{
    private readonly List<string> _availableRooms = new() { "101", "102", "103", "201", "202" };
    private readonly Dictionary<Guid, string> _patientRooms = new();

    public bool AssignRoom(Patient patient, string room)
    {
        if (!_availableRooms.Contains(room)) return false;
        if (_patientRooms.ContainsValue(room)) return false;
        _patientRooms[patient.Id] = room;
        patient.RoomNumber = room;
        patient.IsAdmitted = true;
        return true;
    }

    public bool DischargePatient(Patient patient)
    {
        if (_patientRooms.ContainsKey(patient.Id))
        {
            _patientRooms.Remove(patient.Id);
            patient.RoomNumber = "";
            patient.IsAdmitted = false;
            return true;
        }
        return false;
    }

    public IEnumerable<string> AvailableRooms => _availableRooms.Except(_patientRooms.Values);
}

public class PrescriptionService
{
    public void AddPrescription(Patient patient, string medication)
    {
        patient.Prescriptions.Add(medication);
    }

    public IEnumerable<string> GetPrescriptions(Patient patient)
    {
        return patient.Prescriptions;
    }
}

public class BillingService
{
    public event Action<Bill>? BillPaid;
    public void Pay(Bill bill) => BillPaid?.Invoke(bill);
}

public class Reporting
{
    public static void PrintSnapshot(IEnumerable<Appointment> appts, IEnumerable<Bill> bills, IEnumerable<Person> people, RoomManager roomManager)
    {
        UiHelper.Header("Hospital Reports");

        // Doctors Report
        UiHelper.DisplayTable(
            people.OfType<Doctor>(),
            new[] { "Doctor Name", "Specialty", "Appointments" },
            d => new[] { d.Name, d.Specialty, appts.Count(a => a.DoctorId == d.Id).ToString() }
        );

        // Patients Report
        UiHelper.DisplayTable(
            people.OfType<Patient>(),
            new[] { "Patient Name", "Condition", "Room", "Status" },
            p => new[] { p.Name, p.Condition, p.RoomNumber, p.IsAdmitted ? "Admitted" : "Discharged" }
        );

        // Billing Report
        var top = bills.GroupBy(b => b.PatientId)
                       .Select(g => new { PatientId = g.Key, Total = g.Sum(b => b.Total) })
                       .OrderByDescending(x => x.Total)
                       .FirstOrDefault();

        if (top != null)
        {
            var patient = people.FirstOrDefault(p => p.Id == top.PatientId)?.Name ?? "Unknown";
            UiHelper.Info($"Top Payer: {patient} -> {top.Total.ToString("C", new System.Globalization.CultureInfo("en-IN"))}");
        }

        UiHelper.Info($"Total Appointments: {appts.Count()}");
        UiHelper.Info($"Total Bills: {bills.Count()}");
        UiHelper.Info($"Available Rooms: {string.Join(", ", roomManager.AvailableRooms)}");
    }
}

// ------------------------------------------
// Main Program
// ------------------------------------------
public class Program
{
    private static List<Person> People = new();
    private static AppointmentScheduler Scheduler = new();
    private static BillingService Billing = new();
    private static RoomManager RoomManager = new();
    private static PrescriptionService PrescriptionService = new();
    private static List<Bill> Bills = new();
    private static string AdminUser = "admin";
    private static string AdminPass = "1234";

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        InitializeDemoData();

        while (!Login()) { }

        Billing.BillPaid += bill =>
        {
            var patient = People.FirstOrDefault(p => p.Id == bill.PatientId)?.Name ?? "Unknown";
            UiHelper.Info($"Bill {bill.Id} paid {bill.TotalFormatted} by {patient}");
        };

        bool running = true;
        while (running)
        {
            UiHelper.TitleBanner("Smart Hospital Management System");
            Console.WriteLine("1. Add Patient");
            Console.WriteLine("2. Add Doctor");
            Console.WriteLine("3. Schedule Appointment");
            Console.WriteLine("4. Delete Patient");
            Console.WriteLine("5. Delete Appointment");
            Console.WriteLine("6. Generate Bill");
            Console.WriteLine("7. View Reports");
            Console.WriteLine("8. Change Login Credentials");
            Console.WriteLine("9. Assign Room");
            Console.WriteLine("10. Discharge Patient");
            Console.WriteLine("11. Manage Prescriptions");
            Console.WriteLine("12. Logout");
            Console.WriteLine("13. Exit");

            string choice = UiHelper.Ask("Enter choice");
            switch (choice)
            {
                case "1": AddPatient(); break;
                case "2": AddDoctor(); break;
                case "3": await AddAppointment(); break;
                case "4": DeletePatient(); break;
                case "5": DeleteAppointment(); break;
                case "6": GenerateBill(); break;
                case "7": Reporting.PrintSnapshot(Scheduler.All, Bills, People, RoomManager); break;
                case "8": ChangeCredentials(); break;
                case "9": AssignRoom(); break;
                case "10": DischargePatient(); break;
                case "11": ManagePrescriptions(); break;
                case "12": Login(); break;
                case "13": running = false;break;
                default: UiHelper.Warn("Invalid choice"); break;
            }
            Console.WriteLine("\nPress Enter to continue...");
            Console.ReadLine();
        }

        UiHelper.Header("Goodbye!");
    }

    private static void InitializeDemoData()
    {
        // Demo Patients
        People.Add(new Patient { Name = "Rohit Sharma", Condition = "Fever", IsAdmitted = false });
        People.Add(new Patient { Name = "Virat Kohli", Condition = "Fractured Arm", IsAdmitted = true, RoomNumber = "101" });

        // Demo Doctors
        People.Add(new Doctor { Name = "Dr. Ms Dhoni", Specialty = "General Medicine" });
        People.Add(new Doctor { Name = "Dr. Gambhir", Specialty = "Orthopedics" });

        // Demo Appointments
        var patient1 = People.OfType<Patient>().First();
        var doctor1 = People.OfType<Doctor>().First();
        Scheduler.ScheduleAsync(new Appointment
        {
            PatientId = patient1.Id,
            DoctorId = doctor1.Id,
            When = DateTime.Now.AddDays(1)
        }).GetAwaiter().GetResult();

        // Demo Bill
        var bill = new Bill { PatientId = patient1.Id };
        bill += new Charge { Description = "Consultation Fee", Amount = 500 };
        bill += new Charge { Description = "Lab Test", Amount = 1200 };
        Bills.Add(bill);

        // Demo Prescription
        PrescriptionService.AddPrescription(patient1, "Paracetamol 500mg");
    }

    private static bool Login()
    {
        UiHelper.TitleBanner("Smart Hospital Management System - Login");
        string user = UiHelper.Ask("Username");
        string pass = UiHelper.Ask("Password");

        if (user == AdminUser && pass == AdminPass)
        {
            UiHelper.Info("Login Successful!");
            return true;
        }
        else
        {
            UiHelper.Error("Invalid credentials. Try again.");
            return false;
        }
    }

    private static void ChangeCredentials()
    {
        UiHelper.Header("Change Login Credentials");
        string newUser = UiHelper.Ask("Enter New Username");
        string newPass = UiHelper.Ask("Enter New Password");

        AdminUser = newUser;
        AdminPass = newPass;
        UiHelper.Info("Login credentials updated successfully.");
    }

    private static void AddPatient()
    {
        UiHelper.Header("Add Patient");
        string name = UiHelper.Ask("Enter Patient Name");
        string condition = UiHelper.Ask("Enter Condition");

        var patient = new Patient { Name = name, Condition = condition };
        People.Add(patient);
        UiHelper.Info($"Patient {patient.Name} added.");
    }

    private static void DeletePatient()
    {
        UiHelper.Header("Delete Patient");
        var patients = People.OfType<Patient>().ToList();
        if (!patients.Any())
        {
            UiHelper.Error("No patients available.");
            return;
        }

        UiHelper.DisplayTable(
            patients,
            new[] { "Number", "Name", "Condition", "Status" },
            p => new[] { (patients.IndexOf(p) + 1).ToString(), p.Name, p.Condition, p.IsAdmitted ? "Admitted" : "Discharged" }
        );

        int index = int.Parse(UiHelper.Ask("Choose Patient (number)")) - 1;
        var patient = patients[index];
        People.Remove(patient);
        UiHelper.Info($"Patient {patient.Name} deleted.");
    }

    private static void AddDoctor()
    {
        UiHelper.Header("Add Doctor");
        string name = UiHelper.Ask("Enter Doctor Name");
        string specialty = UiHelper.Ask("Enter Specialty");

        var doctor = new Doctor { Name = name, Specialty = specialty };
        People.Add(doctor);
        UiHelper.Info($"Doctor {doctor.Name} ({doctor.Specialty}) added.");
    }

    private static async Task AddAppointment()
    {
        UiHelper.Header("Schedule Appointment");

        var patients = People.OfType<Patient>().ToList();
        var doctors = People.OfType<Doctor>().ToList();

        if (!patients.Any() || !doctors.Any())
        {
            UiHelper.Error("Need at least 1 patient and 1 doctor.");
            return;
        }

        UiHelper.DisplayTable(
            patients,
            new[] { "Number", "Name", "Condition" },
            p => new[] { (patients.IndexOf(p) + 1).ToString(), p.Name, p.Condition }
        );

        int pIndex = int.Parse(UiHelper.Ask("Choose Patient (number)")) - 1;
        var patient = patients[pIndex];

        UiHelper.DisplayTable(
            doctors,
            new[] { "Number", "Name", "Specialty" },
            d => new[] { (doctors.IndexOf(d) + 1).ToString(), d.Name, d.Specialty }
        );

        int dIndex = int.Parse(UiHelper.Ask("Choose Doctor (number)")) - 1;
        var doctor = doctors[dIndex];

        DateTime when = DateTime.Parse(UiHelper.Ask("Enter Date & Time (yyyy-mm-dd HH:mm)"));

        var appt = new Appointment { PatientId = patient.Id, DoctorId = doctor.Id, When = when };
        try
        {
            await Scheduler.ScheduleAsync(appt);
            UiHelper.Info($"Appointment booked for {patient.Name} with {doctor.Name} on {when}");
        }
        catch (DuplicateAppointmentException ex)
        {
            UiHelper.Error(ex.Message);
        }
    }

    private static void DeleteAppointment()
    {
        UiHelper.Header("Delete Appointment");
        var appts = Scheduler.All.ToList();
        if (!appts.Any())
        {
            UiHelper.Error("No appointments available.");
            return;
        }

        UiHelper.DisplayTable(
            appts,
            new[] { "Number", "Patient", "Doctor", "DateTime" },
            a => new[]
            {
                (appts.IndexOf(a) + 1).ToString(),
                People.FirstOrDefault(p => p.Id == a.PatientId)?.Name ?? "Unknown",
                People.FirstOrDefault(p => p.Id == a.DoctorId)?.Name ?? "Unknown",
                a.When.ToString()
            }
        );

        int index = int.Parse(UiHelper.Ask("Choose Appointment (number)")) - 1;
        var appt = appts[index];
        if (Scheduler.DeleteAppointment(appt.Id))
            UiHelper.Info("Appointment deleted.");
        else
            UiHelper.Error("Could not delete appointment.");
    }

    private static void GenerateBill()
    {
        UiHelper.Header("Generate Bill");

        var patients = People.OfType<Patient>().ToList();
        if (!patients.Any())
        {
            UiHelper.Error("No patients available.");
            return;
        }

        UiHelper.DisplayTable(
            patients,
            new[] { "Number", "Name", "Condition" },
            p => new[] { (patients.IndexOf(p) + 1).ToString(), p.Name, p.Condition }
        );

        int pIndex = int.Parse(UiHelper.Ask("Choose Patient (number)")) - 1;
        var patient = patients[pIndex];

        var bill = new Bill { PatientId = patient.Id };

        bool adding = true;
        while (adding)
        {
            string desc = UiHelper.Ask("Enter Service/Charge Description");
            decimal amt = decimal.Parse(UiHelper.Ask("Enter Amount (₹)"));
            bill += new Charge { Description = desc, Amount = amt };

            string more = UiHelper.Ask("Add more charges? (y/n)");
            adding = more.ToLower() == "y";
        }

        Bills.Add(bill);
        Billing.Pay(bill);
        UiHelper.Info($"Bill total: {bill.TotalFormatted}");
    }

    private static void AssignRoom()
    {
        UiHelper.Header("Assign Room");
        var patients = People.OfType<Patient>().Where(p => !p.IsAdmitted).ToList();
        if (!patients.Any())
        {
            UiHelper.Error("No available patients to assign rooms.");
            return;
        }

        UiHelper.DisplayTable(
            patients,
            new[] { "Number", "Name", "Condition" },
            p => new[] { (patients.IndexOf(p) + 1).ToString(), p.Name, p.Condition }
        );

        int pIndex = int.Parse(UiHelper.Ask("Choose Patient (number)")) - 1;
        var patient = patients[pIndex];

        var availableRooms = RoomManager.AvailableRooms.ToList();
        if (!availableRooms.Any())
        {
            UiHelper.Error("No rooms available.");
            return;
        }

        UiHelper.Info("Available Rooms: " + string.Join(", ", availableRooms));
        string room = UiHelper.Ask("Enter Room Number");
        if (RoomManager.AssignRoom(patient, room))
            UiHelper.Info($"Room {room} assigned to {patient.Name}.");
        else
            UiHelper.Error("Room assignment failed. Room may be occupied or invalid.");
    }

    private static void DischargePatient()
    {
        UiHelper.Header("Discharge Patient");
        var patients = People.OfType<Patient>().Where(p => p.IsAdmitted).ToList();
        if (!patients.Any())
        {
            UiHelper.Error("No admitted patients available.");
            return;
        }

        UiHelper.DisplayTable(
            patients,
            new[] { "Number", "Name", "Room" },
            p => new[] { (patients.IndexOf(p) + 1).ToString(), p.Name, p.RoomNumber }
        );

        int pIndex = int.Parse(UiHelper.Ask("Choose Patient (number)")) - 1;
        var patient = patients[pIndex];
        if (RoomManager.DischargePatient(patient))
            UiHelper.Info($"Patient {patient.Name} discharged.");
        else
            UiHelper.Error("Discharge failed.");
    }

    private static void ManagePrescriptions()
    {
        UiHelper.Header("Manage Prescriptions");
        var patients = People.OfType<Patient>().ToList();
        if (!patients.Any())
        {
            UiHelper.Error("No patients available.");
            return;
        }

        UiHelper.DisplayTable(
            patients,
            new[] { "Number", "Name", "Condition" },
            p => new[] { (patients.IndexOf(p) + 1).ToString(), p.Name, p.Condition }
        );

        int pIndex = int.Parse(UiHelper.Ask("Choose Patient (number)")) - 1;
        var patient = patients[pIndex];

        UiHelper.Info("Current Prescriptions: " + (patient.Prescriptions.Any() ? string.Join(", ", patient.Prescriptions) : "None"));
        string medication = UiHelper.Ask("Enter Medication to Add (or leave empty to skip)");
        if (!string.IsNullOrEmpty(medication))
        {
            PrescriptionService.AddPrescription(patient, medication);
            UiHelper.Info($"Added {medication} to {patient.Name}'s prescriptions.");
        }
    }
}
