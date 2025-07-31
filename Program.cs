using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ConsoleApp1
{
    public class Patient
    {
        public int patientId { get; set; }
        public string patientName { get; set; }
        public DateTime visitDate { get; set; }
        public string visitType { get; set; }
        public string description { get; set; }
        public string doctorName { get; set; }

        public Patient(int patientId, string patientName, DateTime visitDate, string visitType, string description, string doctorName = "")
        {
            this.patientId = patientId;
            this.patientName = patientName;
            this.visitDate = visitDate;
            this.visitType = visitType;
            this.description = description;
            this.doctorName = doctorName;
        }

        public Patient(Patient other)
        {
            this.patientId = other.patientId;
            this.patientName = other.patientName;
            this.visitDate = other.visitDate;
            this.visitType = other.visitType;
            this.description = other.description;
            this.doctorName = other.doctorName;
        }

        public override string ToString()
        {
            return $"ID: {patientId}, Name: {patientName}, Date: {visitDate:dd/MM/yyyy}, Type: {visitType}, Doctor: {doctorName}, Description: {description}";
        }

        public string ToCsvString()
        {
            return $"{patientId},{EscapeCsv(patientName)},{visitDate:yyyy-MM-dd},{EscapeCsv(visitType)},{EscapeCsv(description)},{EscapeCsv(doctorName)}";
        }

        public static Patient FromCsvString(string csvLine)
        {
            var parts = ParseCsvLine(csvLine);
            if (parts.Length >= 6)
            {
                return new Patient(
                    int.Parse(parts[0]),
                    parts[1],
                    DateTime.Parse(parts[2]),
                    parts[3],
                    parts[4],
                    parts[5]
                );
            }
            throw new ArgumentException("Invalid CSV format");
        }

        private static string EscapeCsv(string field)
        {
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return '"' + field.Replace("\"", "\"\"") + '"';
            }
            return field;
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }
    }


    public abstract class Command
    {
        public abstract void Execute();
        public abstract void Undo();
    }

    public class AddPatientCommand : Command
    {
        private Hospital hospital;
        private Patient patient;

        public AddPatientCommand(Hospital hospital, Patient patient)
        {
            this.hospital = hospital;
            this.patient = patient;
        }

        public override void Execute()
        {
            hospital.AddPatientDirect(patient);
        }

        public override void Undo()
        {
            hospital.DeletePatientDirect(patient.patientId);
        }
    }

    public class UpdatePatientCommand : Command
    {
        private Hospital hospital;
        private Patient oldPatient;
        private Patient newPatient;

        public UpdatePatientCommand(Hospital hospital, Patient oldPatient, Patient newPatient)
        {
            this.hospital = hospital;
            this.oldPatient = new Patient(oldPatient);
            this.newPatient = new Patient(newPatient);
        }

        public override void Execute()
        {
            hospital.UpdatePatientDirect(newPatient);
        }

        public override void Undo()
        {
            hospital.UpdatePatientDirect(oldPatient);
        }
    }

    public class DeletePatientCommand : Command
    {
        private Hospital hospital;
        private Patient patient;

        public DeletePatientCommand(Hospital hospital, Patient patient)
        {
            this.hospital = hospital;
            this.patient = new Patient(patient);
        }

        public override void Execute()
        {
            hospital.DeletePatientDirect(patient.patientId);
        }

        public override void Undo()
        {
            hospital.AddPatientDirect(patient);
        }
    }


    public class Hospital
    {
        private List<Patient> patients;
        private Stack<Command> undoStack;
        private Stack<Command> redoStack;
        private const string dataFileName = "..\\..\\..\\patient_visits.csv";  //path where code exists
        //private const string dataFileName = "patient_visits.csv";    //path where binary exists
        private const int maxUndoOperations = 10;
        private int nextPatientId;

        public Hospital()
        {
            patients = new List<Patient>();
            undoStack = new Stack<Command>();
            redoStack = new Stack<Command>();
            nextPatientId = 1;
            LoadDataFromFile();
        }

        public static void Main(string[] args)
        {
            Hospital hospital = new Hospital();
            hospital.GenerateMockData();
            hospital.ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== PATIENT VISIT MANAGER ===");
                Console.WriteLine("1. Add New Patient Visit");
                Console.WriteLine("2. Update Patient Visit");
                Console.WriteLine("3. Delete Patient Visit");
                Console.WriteLine("4. Search Patient Visits");
                Console.WriteLine("5. Generate Reports");
                Console.WriteLine("6. Undo Last Operation");
                Console.WriteLine("7. Redo Last Undone Operation");
                Console.WriteLine("8. Save Data");
                Console.WriteLine("9. Exit");
                Console.Write("Choose an option (1-9): ");

                string choice = Console.ReadLine();
                Console.Clear();

                switch (choice)
                {
                    case "1":
                        AddNewPatientVisit();
                        break;
                    case "2":
                        UpdatePatientVisit();
                        break;
                    case "3":
                        DeletePatientVisit();
                        break;
                    case "4":
                        SearchPatientVisits();
                        break;
                    case "5":
                        GenerateReports();
                        break;
                    case "6":
                        UndoLastOperation();
                        break;
                    case "7":
                        RedoLastOperation();
                        break;
                    case "8":
                        SaveDataToFile();
                        ShowNotification("Data saved successfully!");
                        break;
                    case "9":
                        SaveDataToFile();
                        Console.WriteLine("Thank you for using Patient Visit Manager!");
                        return;
                    default:
                        ShowNotification("Invalid choice. Please try again.");
                        break;
                }

                if (choice != "9")
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        private void AddNewPatientVisit()
        {
            Console.WriteLine("=== ADD NEW PATIENT VISIT ===");

            try
            {
                Console.Write("Enter patient name: ");
                string patientName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(patientName))
                {
                    ShowNotification("Patient name cannot be empty!");
                    return;
                }

                Console.Write("Enter visit date (DD/MM/YYYY) or press Enter for today: ");
                string dateInput = Console.ReadLine();
                DateTime visitDate;
                if (string.IsNullOrWhiteSpace(dateInput))
                {
                    visitDate = DateTime.Today;
                }
                else if (!DateTime.TryParseExact(dateInput, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out visitDate))
                {
                    ShowNotification("Invalid date format! Please use DD/MM/YYYY");
                    return;
                }

                Console.WriteLine("Select visit type:");
                Console.WriteLine("1. Consultation");
                Console.WriteLine("2. Follow-up");
                Console.WriteLine("3. Emergency");
                Console.WriteLine("4. Routine Check-up");
                Console.Write("Enter choice (1-4): ");
                string typeChoice = Console.ReadLine();


                string visitType = typeChoice switch
                {
                    "1" => "Consultation",
                    "2" => "Follow-up",
                    "3" => "Emergency",
                    "4" => "Routine Check-up",
                    _ => "Consultation"
                };

                Console.Write("Enter description/notes: ");
                string description = Console.ReadLine() ?? "";

                Console.Write("Enter doctor name (optional): ");
                string doctorName = Console.ReadLine() ?? "";

                Patient newPatient = new Patient(nextPatientId++, patientName, visitDate, visitType, description, doctorName);
                AddPatient(newPatient);
                ShowNotification($"Patient visit added successfully! ID: {newPatient.patientId}");
            }
            catch (Exception ex)
            {
                ShowNotification($"Error adding patient visit: {ex.Message}");
            }
        }

        private void UpdatePatientVisit()
        {
            Console.WriteLine("=== UPDATE PATIENT VISIT ===");

            Console.Write("Enter patient ID to update: ");
            if (!int.TryParse(Console.ReadLine(), out int patientId))
            {
                ShowNotification("Invalid patient ID!");
                return;
            }

            Patient existingPatient = patients.FirstOrDefault(p => p.patientId == patientId);
            if (existingPatient == null)
            {
                ShowNotification("Patient not found!");
                return;
            }

            Console.WriteLine($"Current details: {existingPatient}");
            Console.WriteLine();

            try
            {
                Console.Write($"Enter new patient name (current: {existingPatient.patientName}): ");
                string newName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(newName))
                    newName = existingPatient.patientName;

                Console.Write($"Enter new visit date (DD/MM/YYYY) (current: {existingPatient.visitDate:dd/MM/yyyy}): ");
                string dateInput = Console.ReadLine();
                DateTime newDate = existingPatient.visitDate;
                if (!string.IsNullOrWhiteSpace(dateInput))
                {
                    if (!DateTime.TryParseExact(dateInput, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out newDate))
                    {
                        ShowNotification("Invalid date format! Keeping current date.");
                        newDate = existingPatient.visitDate;
                    }
                }

                Console.WriteLine("Select new visit type:");
                Console.WriteLine("1. Consultation");
                Console.WriteLine("2. Follow-up");
                Console.WriteLine("3. Emergency");
                Console.WriteLine("4. Routine Check-up");
                Console.Write($"Enter choice (1-4) (current: {existingPatient.visitType}): ");
                string typeChoice = Console.ReadLine();

                string newType = typeChoice switch
                {
                    "1" => "Consultation",
                    "2" => "Follow-up",
                    "3" => "Emergency",
                    "4" => "Routine Check-up",
                    _ => existingPatient.visitType
                };

                Console.Write($"Enter new description (current: {existingPatient.description}): ");
                string newDescription = Console.ReadLine();
                if (string.IsNullOrEmpty(newDescription))
                    newDescription = existingPatient.description;

                Console.Write($"Enter new doctor name (current: {existingPatient.doctorName}): ");
                string newDoctor = Console.ReadLine();
                if (string.IsNullOrEmpty(newDoctor))
                    newDoctor = existingPatient.doctorName;

                Patient updatedPatient = new Patient(patientId, newName, newDate, newType, newDescription, newDoctor);
                UpdatePatient(existingPatient, updatedPatient);
                ShowNotification("Patient visit updated successfully!");
            }
            catch (Exception ex)
            {
                ShowNotification($"Error updating patient visit: {ex.Message}");
            }
        }

        private void DeletePatientVisit()
        {
            Console.WriteLine("=== DELETE PATIENT VISIT ===");

            Console.Write("Enter patient ID to delete: ");
            if (!int.TryParse(Console.ReadLine(), out int patientId))
            {
                ShowNotification("Invalid patient ID!");
                return;
            }

            Patient patientToDelete = patients.FirstOrDefault(p => p.patientId == patientId);
            if (patientToDelete == null)
            {
                ShowNotification("Patient not found!");
                return;
            }

            Console.WriteLine($"Patient details: {patientToDelete}");
            Console.Write("Are you sure you want to delete this patient visit? (y/n): ");
            string confirmation = Console.ReadLine()?.ToLower();

            if (confirmation == "y" || confirmation == "yes")
            {
                DeletePatient(patientToDelete);
                ShowNotification("Patient visit deleted successfully!");
            }
            else
            {
                ShowNotification("Delete operation cancelled.");
            }
        }

        private void SearchPatientVisits()
        {
            Console.WriteLine("=== SEARCH PATIENT VISITS ===");
            Console.WriteLine("1. Search by Patient Name");
            Console.WriteLine("2. Search by Doctor Name");
            Console.WriteLine("3. Search by Visit Type");
            Console.WriteLine("4. Search by Date");
            Console.WriteLine("5. View All Patients");
            Console.Write("Choose search option (1-5): ");

            string choice = Console.ReadLine();
            List<Patient> searchResults = new List<Patient>();

            switch (choice)
            {
                case "1":
                    Console.Write("Enter patient name (partial match): ");
                    string patientName = Console.ReadLine()?.ToLower();
                    searchResults = patients.Where(p => p.patientName.ToLower().Contains(patientName ?? "")).ToList();
                    break;
                case "2":
                    Console.Write("Enter doctor name (partial match): ");
                    string doctorName = Console.ReadLine()?.ToLower();
                    searchResults = patients.Where(p => p.doctorName.ToLower().Contains(doctorName ?? "")).ToList();
                    break;
                case "3":
                    Console.WriteLine("Enter visit type: ");
                    Console.WriteLine("1. Consultation");
                    Console.WriteLine("2. Follow-up");
                    Console.WriteLine("3. Emergency");
                    Console.WriteLine("4. Routine Check-up");
                    Console.Write("Enter choice (1-4): ");
                    string typeChoice = Console.ReadLine();

                    string visitType = typeChoice switch
                    {
                        "1" => "Consultation",
                        "2" => "Follow-up",
                        "3" => "Emergency",
                        "4" => "Routine Check-up",
                        _ => "Consultation"
                    };
                    visitType = visitType.ToLower();
                    searchResults = patients.Where(p => p.visitType.ToLower().Contains(visitType ?? "")).ToList();
                    break;
                case "4":
                    Console.Write("Enter date (DD/MM/YYYY): ");
                    string dateInput = Console.ReadLine();
                    if (DateTime.TryParseExact(dateInput, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime searchDate))
                    {
                        searchResults = patients.Where(p => p.visitDate.Date == searchDate.Date).ToList();
                    }
                    else
                    {
                        ShowNotification("Invalid date format!");
                        return;
                    }
                    break;
                case "5":
                    searchResults = patients.ToList();
                    break;
                default:
                    ShowNotification("Invalid choice!");
                    return;
            }

            DisplaySearchResults(searchResults);
        }

        private void DisplaySearchResults(List<Patient> results)
        {
            if (results.Count == 0)
            {
                Console.WriteLine("No patients found matching the search criteria.");
                return;
            }

            Console.WriteLine($"\nFound {results.Count} patient(s):");
            Console.WriteLine(new string('-', 100));

            foreach (var patient in results.OrderBy(p => p.visitDate))
            {
                Console.WriteLine(patient);
            }
        }

        private void GenerateReports()
        {
            Console.WriteLine("=== REPORTS AND STATISTICS ===");
            Console.WriteLine("1. Individual Visit Summary");
            Console.WriteLine("2. Visit Count by Type");
            Console.WriteLine("3. Weekly Visit Summary");
            Console.WriteLine("4. Monthly Statistics");
            Console.Write("Choose report option (1-4): ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    GenerateIndividualVisitSummary();
                    break;
                case "2":
                    GenerateVisitCountByType();
                    break;
                case "3":
                    GenerateWeeklyVisitSummary();
                    break;
                case "4":
                    GenerateMonthlyStatistics();
                    break;
                default:
                    ShowNotification("Invalid choice!");
                    break;
            }
        }

        private void GenerateIndividualVisitSummary()
        {
            Console.Write("Enter patient ID for summary: ");
            if (!int.TryParse(Console.ReadLine(), out int patientId))
            {
                ShowNotification("Invalid patient ID!");
                return;
            }

            Patient patient = patients.FirstOrDefault(p => p.patientId == patientId);
            if (patient == null)
            {
                ShowNotification("Patient not found!");
                return;
            }

            Console.WriteLine("\n=== INDIVIDUAL VISIT SUMMARY ===");
            Console.WriteLine($"Patient ID: {patient.patientId}");
            Console.WriteLine($"Patient Name: {patient.patientName}");
            Console.WriteLine($"Visit Date: {patient.visitDate:dd/MM/yyyy}");
            Console.WriteLine($"Visit Type: {patient.visitType}");
            Console.WriteLine($"Doctor: {(string.IsNullOrEmpty(patient.doctorName) ? "Not specified" : patient.doctorName)}");
            Console.WriteLine($"Description: {patient.description}");
        }

        private void GenerateVisitCountByType()
        {
            Console.WriteLine("\n=== VISIT COUNT BY TYPE ===");
            var typeGroups = patients.GroupBy(p => p.visitType)
                                   .OrderByDescending(g => g.Count());

            foreach (var group in typeGroups)
            {
                Console.WriteLine($"{group.Key}: {group.Count()} visits");
            }

            Console.WriteLine($"\nTotal Visits: {patients.Count}");
        }

        private void GenerateWeeklyVisitSummary()
        {
            Console.Write("Enter week start date (DD/MM/YYYY): ");
            if (!DateTime.TryParseExact(Console.ReadLine(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime weekStart))
            {
                ShowNotification("Invalid date format!");
                return;
            }

            DateTime weekEnd = weekStart.AddDays(7);
            var weeklyVisits = patients.Where(p => p.visitDate >= weekStart && p.visitDate < weekEnd)
                                     .OrderBy(p => p.visitDate)
                                     .ToList();

            Console.WriteLine($"\n=== WEEKLY VISIT SUMMARY ({weekStart:dd/MM/yyyy} - {weekEnd.AddDays(-1):dd/MM/yyyy}) ===");
            Console.WriteLine($"Total visits this week: {weeklyVisits.Count}");

            if (weeklyVisits.Any())
            {
                var dailyGroups = weeklyVisits.GroupBy(p => p.visitDate.Date)
                                            .OrderBy(g => g.Key);

                foreach (var day in dailyGroups)
                {
                    Console.WriteLine($"{day.Key:dd/MM/yyyy}: {day.Count()} visits");
                }
            }
        }

        private void GenerateMonthlyStatistics()
        {
            Console.Write("Enter month (MM/YYYY): ");
            string monthInput = Console.ReadLine();
            if (!DateTime.TryParseExact($"01/{monthInput}", "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime monthStart))
            {
                ShowNotification("Invalid month format! Use MM/YYYY");
                return;
            }

            DateTime monthEnd = monthStart.AddMonths(1);
            var monthlyVisits = patients.Where(p => p.visitDate >= monthStart && p.visitDate < monthEnd)
                                      .ToList();

            Console.WriteLine($"\n=== MONTHLY STATISTICS ({monthStart:MM/yyyy}) ===");
            Console.WriteLine($"Total visits: {monthlyVisits.Count}");

            if (monthlyVisits.Any())
            {
                var typeStats = monthlyVisits.GroupBy(p => p.visitType)
                                           .OrderByDescending(g => g.Count());

                Console.WriteLine("\nVisits by type:");
                foreach (var group in typeStats)
                {
                    Console.WriteLine($"  {group.Key}: {group.Count()}");
                }

                var doctorStats = monthlyVisits.Where(p => !string.IsNullOrEmpty(p.doctorName))
                                             .GroupBy(p => p.doctorName)
                                             .OrderByDescending(g => g.Count());

                if (doctorStats.Any())
                {
                    Console.WriteLine("\nVisits by doctor:");
                    foreach (var group in doctorStats)
                    {
                        Console.WriteLine($"  {group.Key}: {group.Count()}");
                    }
                }
            }
        }

        public void AddPatient(Patient patient)
        {
            var command = new AddPatientCommand(this, patient);
            ExecuteCommand(command);
        }

        public void UpdatePatient(Patient oldPatient, Patient newPatient)
        {
            var command = new UpdatePatientCommand(this, oldPatient, newPatient);
            ExecuteCommand(command);
        }

        public void DeletePatient(Patient patient)
        {
            var command = new DeletePatientCommand(this, patient);
            ExecuteCommand(command);
        }

        private void ExecuteCommand(Command command)
        {
            command.Execute();
            undoStack.Push(command);


            if (undoStack.Count > maxUndoOperations)
            {
                var commands = undoStack.ToArray();
                undoStack.Clear();
                for (int i = commands.Length - maxUndoOperations; i < commands.Length; i++)
                {
                    undoStack.Push(commands[i]);
                }
            }

            redoStack.Clear();
        }

        public void AddPatientDirect(Patient patient)
        {
            patients.Add(patient);
            if (patient.patientId >= nextPatientId)
                nextPatientId = patient.patientId + 1;
        }

        public void UpdatePatientDirect(Patient updatedPatient)
        {
            var existingPatient = patients.FirstOrDefault(p => p.patientId == updatedPatient.patientId);
            if (existingPatient != null)
            {
                int index = patients.IndexOf(existingPatient);
                patients[index] = updatedPatient;
            }
        }

        public void DeletePatientDirect(int patientId)
        {
            patients.RemoveAll(p => p.patientId == patientId);
        }

        private void UndoLastOperation()
        {
            if (undoStack.Count == 0)
            {
                ShowNotification("No operations to undo!");
                return;
            }

            var command = undoStack.Pop();
            command.Undo();
            redoStack.Push(command);
            ShowNotification("Last operation undone successfully!");
        }

        private void RedoLastOperation()
        {
            if (redoStack.Count == 0)
            {
                ShowNotification("No operations to redo!");
                return;
            }

            var command = redoStack.Pop();
            command.Execute();
            undoStack.Push(command);
            ShowNotification("Operation redone successfully!");
        }

        private void LoadDataFromFile()
        {
            try
            {
                if (File.Exists(dataFileName))
                {
                    string[] lines = File.ReadAllLines(dataFileName);
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            try
                            {
                                Patient patient = Patient.FromCsvString(line);
                                patients.Add(patient);
                                if (patient.patientId >= nextPatientId)
                                    nextPatientId = patient.patientId + 1;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing line: {line}. Error: {ex.Message}");
                            }
                        }
                    }
                    Console.WriteLine($"Loaded {patients.Count} patient records from file.");
                }
                else
                {
                    Console.WriteLine("No existing data file found. Starting with empty database.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data from file: {ex.Message}");
            }
        }

        private void SaveDataToFile()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(dataFileName))
                {
                    foreach (Patient patient in patients)
                    {
                        writer.WriteLine(patient.ToCsvString());
                    }
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"Error saving data to file: {ex.Message}");
            }
        }

        private void GenerateMockData()
        {
            if (patients.Count > 0) return; // Don't generate if data already exists

            Random random = new Random();
            string[] firstNames = { "John", "Jane", "Michael", "Sarah", "David", "Emma", "James", "Lisa", "Robert", "Maria", "William", "Jennifer", "Richard", "Patricia", "Joseph", "Linda", "Thomas", "Elizabeth", "Charles", "Barbara", "Christopher", "Jessica", "Daniel", "Susan", "Matthew", "Karen", "Anthony", "Nancy", "Mark", "Betty" };
            string[] lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson" };
            string[] visitTypes = { "Consultation", "Follow-up", "Emergency", "Routine Check-up" };
            string[] doctors = { "Dr. Smith", "Dr. Johnson", "Dr. Williams", "Dr. Brown", "Dr. Davis", "Dr. Miller", "Dr. Wilson", "Dr. Moore", "Dr. Taylor", "Dr. Anderson" };
            string[] descriptions = { "Regular checkup", "Follow-up appointment", "Urgent medical attention needed", "Routine examination", "Consultation for symptoms", "Preventive care visit", "Health screening", "Medical evaluation", "Treatment follow-up", "Annual physical exam" };

            int numberOfRecords = random.Next(300, 501); // Generate 300-500 records

            for (int i = 0; i < numberOfRecords; i++)
            {
                string patientName = $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
                DateTime visitDate = DateTime.Today.AddDays(-random.Next(0, 365)); // Random date within last year
                string visitType = visitTypes[random.Next(visitTypes.Length)];
                string description = descriptions[random.Next(descriptions.Length)];
                string doctorName = random.Next(10) < 8 ? doctors[random.Next(doctors.Length)] : ""; // 80% chance of having a doctor

                Patient mockPatient = new Patient(nextPatientId++, patientName, visitDate, visitType, description, doctorName);
                patients.Add(mockPatient);
            }

            SaveDataToFile();
            Console.WriteLine($"Generated {numberOfRecords} mock patient records for testing.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private void ShowNotification(string message)
        {
            Console.WriteLine();
            Console.WriteLine($"*** {message} ***");
            Console.WriteLine();
        }
    }
}