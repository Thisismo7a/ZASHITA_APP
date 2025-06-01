using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Wpf.Ui.Controls;
using Microsoft.ML;
using Microsoft.ML.Data;
using PeNet;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Data.Sqlite;

namespace ZASHITA_APP.Dialogs
{
    public partial class QuickScanDialog : FluentWindow
    {
        private readonly MLContext _mlContext;
        private readonly ITransformer? _model;
        private readonly HashSet<string> _malwareHashes;
        private CancellationTokenSource _cancellationTokenSource;
        private List<string> _selectedFiles;
        private List<(string FilePath, string DetectionMethod)> _malwareFiles;
        private readonly string _databasePath = @"C:\Users\WORK\source\repos\Hamza19C\HAMZA_PFE\HAMZA_PFE\ZASHITA.sql";
        private readonly string _quarantinePath = @"C:\zashita-malwares";

        public QuickScanDialog(string datasetPath)
        {
            InitializeComponent();
            FileEnScan.Content = "Waiting...";
            Progress.Value = 0;
            resultssss.Text = "Results ...";
            viruussss.Text = "0";
            nonsignedfileeee.Text = "0";
            hashshavirusss.Text = "0";
            nonvirusss.Text = "0";
            _mlContext = new MLContext();
            _selectedFiles = new List<string>();
            _malwareFiles = new List<(string FilePath, string DetectionMethod)>();
            _cancellationTokenSource = new CancellationTokenSource();
            _malwareHashes = LoadMalwareHashes(datasetPath);

            try
            {
                Directory.CreateDirectory(_quarantinePath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error creating quarantine directory: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            CreateDatabaseAndTable();

            try
            {
                string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MLModel1new.mlnet");
                if (!File.Exists(modelPath))
                    throw new FileNotFoundException();
                _model = _mlContext.Model.Load(modelPath, out var modelSchema);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading ML model: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                _model = null;
            }
        }

        private void CreateDatabaseAndTable()
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_databasePath};"))
                {
                    connection.Open();
                    string createMainScanQuery = @"
                        CREATE TABLE IF NOT EXISTS MainScan (
                            NumOfMalwares INTEGER,
                            NumOfFilesScaned INTEGER,
                            LastScan INTEGER,
                            StateOfSys INTEGER
                        )";
                    using (var command = new SqliteCommand(createMainScanQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    string createQuarantineQuery = @"
                        CREATE TABLE IF NOT EXISTS quarantin (
                            FilePath TEXT,
                            ScanDate INTEGER,
                            Action INTEGER
                        )";
                    using (var command = new SqliteCommand(createQuarantineQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error creating database or tables: {ex.Message}", "Database Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private HashSet<string> LoadMalwareHashes(string datasetPath)
        {
            var malwareHashes = new HashSet<string>();
            try
            {
                using (var reader = new StreamReader(datasetPath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    BadDataFound = null,
                    MissingFieldFound = null
                }))
                {
                    while (csv.Read())
                    {
                        var hash = csv.GetField<string>(1)?.Trim().ToLowerInvariant();
                        if (!string.IsNullOrEmpty(hash))
                            malwareHashes.Add(hash);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading malware dataset: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            return malwareHashes;
        }

        private async void StartScan(object sender, RoutedEventArgs e)
        {
            _selectedFiles.Clear();
            _selectedFiles.AddRange(GetQuickScanFiles());

            if (_selectedFiles.Count == 0)
            {
                System.Windows.MessageBox.Show("No files found to scan!", "No Files Found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            StartButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
            FileEnScan.Content = "Scanning...";
            Progress.Value = 0;
            resultssss.Text = "Scanning in progress...";
            viruussss.Text = "0";
            nonsignedfileeee.Text = "0";
            hashshavirusss.Text = "0";
            nonvirusss.Text = "0";
            _malwareFiles.Clear();
            MalwareListBox.ItemsSource = null;

            try
            {
                var (hashMalwareCount, mlMalwareCount, nonCertifiedCount, totalFilesScanned) = await RunScanAsync(_selectedFiles, _cancellationTokenSource.Token);

                await SaveScanResultsToDatabase(hashMalwareCount, mlMalwareCount, totalFilesScanned);

                if (hashMalwareCount == 0 && mlMalwareCount == 0)
                {
                    resultssss.Text = $"✅ EVERYTHING WELL! Non-certified PE files: {nonCertifiedCount}";
                    resultssss.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    resultssss.Text = $"MALWARES FOUND IN YOUR DEVICE :";
                    resultssss.Foreground = System.Windows.Media.Brushes.Red;
                }

                viruussss.Text = (hashMalwareCount + mlMalwareCount).ToString();
                nonsignedfileeee.Text = nonCertifiedCount.ToString();
                hashshavirusss.Text = hashMalwareCount.ToString();
                nonvirusss.Text = (totalFilesScanned - (hashMalwareCount + mlMalwareCount)).ToString();
                MalwareListBox.ItemsSource = _malwareFiles.Select(m => $"{m.FilePath} This malware was detected using {m.DetectionMethod}").ToList();
            }
            catch (OperationCanceledException)
            {
                FileEnScan.Content = "Scan Cancelled!";
                resultssss.Text = "Scan was cancelled!";
            }
            catch (Exception ex)
            {
                FileEnScan.Content = "Error during scan!";
                resultssss.Text = $"Error: {ex.Message}";
            }
            finally
            {
                StartButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
                Progress.Value = 100;
            }
        }

        private async Task SaveScanResultsToDatabase(int hashMalwareCount, int mlMalwareCount, int totalFilesScanned)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_databasePath};"))
                {
                    await connection.OpenAsync();

                    string createTableQuery = @"
                        CREATE TABLE IF NOT EXISTS MainScan (
                            NumOfMalwares INTEGER,
                            NumOfFilesScaned INTEGER,
                            LastScan INTEGER,
                            StateOfSys INTEGER
                        )";
                    using (var createCommand = new SqliteCommand(createTableQuery, connection))
                    {
                        await createCommand.ExecuteNonQueryAsync();
                    }

                    int numOfMalwares = hashMalwareCount + mlMalwareCount;
                    long lastScanUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    int stateOfSys = numOfMalwares == 0 ? 0 : 1;

                    string insertQuery = @"
                        INSERT INTO MainScan (NumOfMalwares, NumOfFilesScaned, LastScan, StateOfSys)
                        VALUES (@NumOfMalwares, @NumOfFilesScaned, @LastScan, @StateOfSys)";
                    using (var insertCommand = new SqliteCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@NumOfMalwares", numOfMalwares);
                        insertCommand.Parameters.AddWithValue("@NumOfFilesScaned", totalFilesScanned);
                        insertCommand.Parameters.AddWithValue("@LastScan", lastScanUnix);
                        insertCommand.Parameters.AddWithValue("@StateOfSys", stateOfSys);
                        await insertCommand.ExecuteNonQueryAsync();
                    }

                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving scan results to database: {ex.Message}", "Database Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private List<string> GetQuickScanFiles()
        {
            var files = new List<string>();
            var folders = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"
            };

            foreach (var folder in folders)
            {
                try
                {
                    if (Directory.Exists(folder))
                        files.AddRange(Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories));
                }
                catch { }
            }
            return files;
        }

        private async Task<(int HashMalwareCount, int MLMalwareCount, int NonCertifiedCount, int TotalFilesScanned)> RunScanAsync(List<string> files, CancellationToken cancellationToken)
        {
            int hashMalwareCount = 0;
            int mlMalwareCount = 0;
            int nonCertifiedCount = 0;
            int totalFiles = files.Count;
            int scannedFiles = 0;
            int totalFilesScanned = 0;

            var startTime = DateTime.Now;
            await Task.Run(() =>
            {
                foreach (string filePath in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Dispatcher.Invoke(() =>
                    {
                        FileEnScan.Content = filePath;
                        Progress.Value = (double)scannedFiles / totalFiles * 100;

                        var elapsedTime = DateTime.Now - startTime;
                        var estimatedTotalTime = elapsedTime.TotalSeconds / (scannedFiles + 1) * totalFiles;
                        var estimatedTimeRemaining = TimeSpan.FromSeconds(estimatedTotalTime - elapsedTime.TotalSeconds);
                        TimeEstimation.Content = $"{(int)estimatedTimeRemaining.TotalMinutes}m {estimatedTimeRemaining.Seconds}s remaining";
                    });

                    try
                    {
                        bool isPeFile = IsPeFile(filePath);
                        if (isPeFile)
                        {
                            bool isCertified = HasValidCertificate(filePath);
                            if (!isCertified)
                            {
                                nonCertifiedCount++;
                                if (_model != null)
                                {
                                    var (score, _) = ScanExeFileWithML(filePath);
                                    if (score > 0.5f)
                                    {
                                        mlMalwareCount++;
                                        _malwareFiles.Add((filePath, "ML Model"));
                                        CopyToQuarantine(filePath);
                                    }
                                }
                            }
                        }
                        else
                        {
                            string fileHash = ComputeSHA256(filePath);
                            if (_malwareHashes.Contains(fileHash))
                            {
                                hashMalwareCount++;
                                _malwareFiles.Add((filePath, "SHA256"));
                                CopyToQuarantine(filePath);
                            }
                        }
                        scannedFiles++;
                    }
                    catch { }
                    totalFilesScanned++;
                }
            }, cancellationToken);

            return (hashMalwareCount, mlMalwareCount, nonCertifiedCount, totalFilesScanned);
        }

        private void CopyToQuarantine(string filePath)
        {
            try
            {
                Directory.CreateDirectory(_quarantinePath);
                string fileName = Path.GetFileName(filePath);
                string destPath = Path.Combine(_quarantinePath, $"{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}");
                File.Move(filePath, destPath);
                LogToQuarantineTable(filePath, destPath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error moving file to quarantine: {ex.Message}", "Quarantine Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void LogToQuarantineTable(string originalPath, string quarantinePath)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_databasePath};"))
                {
                    connection.Open();
                    string insertQuery = @"
                        INSERT INTO quarantin (FilePath, ScanDate, Action)
                        VALUES (@FilePath, @ScanDate, @Action)";
                    using (var command = new SqliteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@FilePath", originalPath);
                        command.Parameters.AddWithValue("@ScanDate", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                        command.Parameters.AddWithValue("@Action", 0);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error logging to quarantin table: {ex.Message}", "Database Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool IsPeFile(string filePath)
        {
            try
            {
                var peFile = new PeFile(filePath);
                return peFile.IsExe;
            }
            catch
            {
                return false;
            }
        }

        private bool HasValidCertificate(string filePath)
        {
            try
            {
                var certificate = X509Certificate.CreateFromSignedFile(filePath);
                if (certificate != null)
                {
                    X509Certificate2 cert2 = new X509Certificate2(certificate);
                    return DateTime.Now >= cert2.NotBefore && DateTime.Now <= cert2.NotAfter;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private (float Score, float Confidence) ScanExeFileWithML(string filePath)
        {
            if (Path.GetExtension(filePath).ToLower() != ".exe")
                throw new ArgumentException();
            var peFile = new PeFile(filePath);
            if (!peFile.IsExe)
                throw new ArgumentException();
            var dosHeaderData = ParseDosHeader(filePath);
            var fileHeader = peFile.ImageNtHeaders?.FileHeader ?? throw new InvalidOperationException();
            var optHeader = peFile.ImageNtHeaders?.OptionalHeader ?? throw new InvalidOperationException();
            var sampleData = new ModelInput
            {
                Type = 1f,
                SHA256 = ComputeSHA256(filePath),
                e_magic = dosHeaderData.e_magic,
                e_cblp = dosHeaderData.e_cblp,
                e_cp = dosHeaderData.e_cp,
                e_crlc = dosHeaderData.e_crlc,
                e_cparhdr = dosHeaderData.e_cparhdr,
                e_minalloc = dosHeaderData.e_minalloc,
                e_maxalloc = dosHeaderData.e_maxalloc,
                e_ss = dosHeaderData.e_ss,
                e_sp = dosHeaderData.e_sp,
                e_csum = dosHeaderData.e_csum,
                e_ip = dosHeaderData.e_ip,
                e_cs = dosHeaderData.e_cs,
                e_lfarlc = dosHeaderData.e_lfarlc,
                e_ovno = dosHeaderData.e_ovno,
                e_oemid = dosHeaderData.e_oemid,
                e_oeminfo = 0f,
                e_lfanew = dosHeaderData.e_lfanew,
                Machine = (float)fileHeader.Machine,
                NumberOfSections = fileHeader.NumberOfSections,
                TimeDateStamp = fileHeader.TimeDateStamp,
                PointerToSymbolTable = fileHeader.PointerToSymbolTable,
                NumberOfSymbols = fileHeader.NumberOfSymbols,
                SizeOfOptionalHeader = fileHeader.SizeOfOptionalHeader,
                Characteristics = (float)fileHeader.Characteristics,
                Magic = (float)optHeader.Magic,
                MajorLinkerVersion = optHeader.MajorLinkerVersion,
                MinorLinkerVersion = optHeader.MinorLinkerVersion,
                SizeOfCode = optHeader.SizeOfCode,
                SizeOfInitializedData = optHeader.SizeOfInitializedData,
                SizeOfUninitializedData = optHeader.SizeOfUninitializedData,
                AddressOfEntryPoint = optHeader.AddressOfEntryPoint,
                BaseOfCode = optHeader.BaseOfCode,
                ImageBase = optHeader.ImageBase,
                SectionAlignment = optHeader.SectionAlignment,
                FileAlignment = optHeader.FileAlignment,
                MajorOperatingSystemVersion = optHeader.MajorOperatingSystemVersion,
                MinorOperatingSystemVersion = optHeader.MinorOperatingSystemVersion,
                MajorImageVersion = optHeader.MajorImageVersion,
                MinorImageVersion = optHeader.MinorImageVersion,
                MajorSubsystemVersion = optHeader.MajorSubsystemVersion,
                MinorSubsystemVersion = optHeader.MinorSubsystemVersion,
                Reserved1 = optHeader.Win32VersionValue,
                SizeOfImage = optHeader.SizeOfImage,
                SizeOfHeaders = optHeader.SizeOfHeaders,
                CheckSum = optHeader.CheckSum,
                Subsystem = (float)optHeader.Subsystem,
                DllCharacteristics = (float)optHeader.DllCharacteristics,
                SizeOfStackReserve = optHeader.SizeOfStackReserve,
                SizeOfHeapReserve = optHeader.SizeOfHeapReserve,
                SizeOfHeapCommit = optHeader.SizeOfHeapCommit,
                LoaderFlags = optHeader.LoaderFlags,
                NumberOfRvaAndSizes = optHeader.NumberOfRvaAndSizes
            };
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_model);
            var result = predictionEngine.Predict(sampleData);
            return (result.PredictedLabel, result.Score?.Max() ?? 0f);
        }

        private (ushort e_magic, ushort e_cblp, ushort e_cp, ushort e_crlc, ushort e_cparhdr, ushort e_minalloc, ushort e_maxalloc, ushort e_ss, ushort e_sp, ushort e_csum, ushort e_ip, ushort e_cs, ushort e_lfarlc, ushort e_ovno, ushort e_oemid, uint e_lfanew) ParseDosHeader(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);
            return (
                e_magic: reader.ReadUInt16(),
                e_cblp: reader.ReadUInt16(),
                e_cp: reader.ReadUInt16(),
                e_crlc: reader.ReadUInt16(),
                e_cparhdr: reader.ReadUInt16(),
                e_minalloc: reader.ReadUInt16(),
                e_maxalloc: reader.ReadUInt16(),
                e_ss: reader.ReadUInt16(),
                e_sp: reader.ReadUInt16(),
                e_csum: reader.ReadUInt16(),
                e_ip: reader.ReadUInt16(),
                e_cs: reader.ReadUInt16(),
                e_lfarlc: reader.ReadUInt16(),
                e_ovno: reader.ReadUInt16(),
                e_oemid: reader.ReadUInt16(),
                e_lfanew: reader.ReadUInt32()
            );
        }

        private string ComputeSHA256(string filePath)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private void CloseDialog(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Cancel();
            this.Close();
        }

        public class ModelInput
        {
            public float Type { get; set; }
            public string? SHA256 { get; set; }
            public float e_magic { get; set; }
            public float e_cblp { get; set; }
            public float e_cp { get; set; }
            public float e_crlc { get; set; }
            public float e_cparhdr { get; set; }
            public float e_minalloc { get; set; }
            public float e_maxalloc { get; set; }
            public float e_ss { get; set; }
            public float e_sp { get; set; }
            public float e_csum { get; set; }
            public float e_ip { get; set; }
            public float e_cs { get; set; }
            public float e_lfarlc { get; set; }
            public float e_ovno { get; set; }
            public float e_oemid { get; set; }
            public float e_oeminfo { get; set; }
            public float e_lfanew { get; set; }
            public float Machine { get; set; }
            public float NumberOfSections { get; set; }
            public float TimeDateStamp { get; set; }
            public float PointerToSymbolTable { get; set; }
            public float NumberOfSymbols { get; set; }
            public float SizeOfOptionalHeader { get; set; }
            public float Characteristics { get; set; }
            public float Magic { get; set; }
            public float MajorLinkerVersion { get; set; }
            public float MinorLinkerVersion { get; set; }
            public float SizeOfCode { get; set; }
            public float SizeOfInitializedData { get; set; }
            public float SizeOfUninitializedData { get; set; }
            public float AddressOfEntryPoint { get; set; }
            public float BaseOfCode { get; set; }
            public float ImageBase { get; set; }
            public float SectionAlignment { get; set; }
            public float FileAlignment { get; set; }
            public float MajorOperatingSystemVersion { get; set; }
            public float MinorOperatingSystemVersion { get; set; }
            public float MajorImageVersion { get; set; }
            public float MinorImageVersion { get; set; }
            public float MajorSubsystemVersion { get; set; }
            public float MinorSubsystemVersion { get; set; }
            public float Reserved1 { get; set; }
            public float SizeOfImage { get; set; }
            public float SizeOfHeaders { get; set; }
            public float CheckSum { get; set; }
            public float Subsystem { get; set; }
            public float DllCharacteristics { get; set; }
            public float SizeOfStackReserve { get; set; }
            public float SizeOfHeapReserve { get; set; }
            public float SizeOfHeapCommit { get; set; }
            public float LoaderFlags { get; set; }
            public float NumberOfRvaAndSizes { get; set; }
        }

        public class ModelOutput
        {
            [ColumnName("PredictedLabel")]
            public float PredictedLabel { get; set; }
            public float[]? Score { get; set; }
        }
    }
}