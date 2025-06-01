using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Forms;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Diagnostics;
using Wpf.Ui.Controls;
using Microsoft.ML;
using Microsoft.ML.Data;
using PeNet;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Data.Sqlite;

namespace ZASHITA_APP.Dialogs
{
    public partial class SelectiveScanDialog : FluentWindow
    {
        private readonly MLContext _mlContext;
        private readonly ITransformer? _model;
        private readonly HashSet<string> _malwareHashes;
        private CancellationTokenSource _cancellationTokenSource;
        private List<string> _selectedFiles;
        private List<(string FilePath, string DetectionMethod)> _malwareFiles;
        private readonly string _databasePath;
        private readonly string _quarantinePath = @"C:\zashita-malwares";

        public SelectiveScanDialog(string datasetPath)
        {
            InitializeComponent();
            FileEnScan.Content = "Waiting...";
            Progress.Value = 0;
            resultssss.Text = "";
            viruussss.Text = "0";
            nonsignedfileeee.Text = "0";
            hashshavirusss.Text = "0";
            nonvirusss.Text = "0";
            _mlContext = new MLContext();
            _selectedFiles = new List<string>();
            _malwareFiles = new List<(string FilePath, string DetectionMethod)>();
            _cancellationTokenSource = new CancellationTokenSource();
            _malwareHashes = LoadMalwareHashes(datasetPath);
            Debug.WriteLine($"Loaded {_malwareHashes.Count} malware hashes");
            _databasePath = @"C:\Users\WORK\source\repos\Hamza19C\HAMZA_PFE\HAMZA_PFE\ZASHITA.sql";

            CreateDatabaseAndTable();

            try
            {
                string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MLModel1new.mlnet");
                if (!File.Exists(modelPath))
                    throw new FileNotFoundException($"ML model file not found at: {modelPath}");
                _model = _mlContext.Model.Load(modelPath, out var modelSchema);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading ML model: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                _model = null;
            }

            try
            {
                Directory.CreateDirectory(_quarantinePath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error creating quarantine directory: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void CreateDatabaseAndTable()
        {
            try
            {
                if (!File.Exists(_databasePath))
                {
                }

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
                Debug.WriteLine($"Loading malware hashes from: {datasetPath}");
                using (var reader = new StreamReader(datasetPath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    BadDataFound = null,
                    MissingFieldFound = null
                }))
                {
                    int rowCount = 0;
                    while (csv.Read())
                    {
                        rowCount++;
                        var hash = csv.GetField<string>(1)?.Trim().ToLowerInvariant();
                        if (!string.IsNullOrEmpty(hash))
                        {
                            malwareHashes.Add(hash);
                            Debug.WriteLine($"Loaded hash: {hash}");
                        }
                        else
                        {
                            Debug.WriteLine($"Row {rowCount}: Empty or invalid hash");
                        }
                    }
                    Debug.WriteLine($"Total rows processed: {rowCount}, Unique hashes loaded: {malwareHashes.Count}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading malware dataset: {ex.Message}");
                System.Windows.MessageBox.Show($"Error loading malware dataset: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            return malwareHashes;
        }

        private async void ChooseFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var customMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Pick Files or Folder",
                Content = "Wanna select files or a whole folder?",
                PrimaryButtonText = "File",
                SecondaryButtonText = "Folder",
                CloseButtonText = "Cancel"
            };

            var result = await customMessageBox.ShowDialogAsync();
            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                var openFileDialog = new System.Windows.Forms.OpenFileDialog
                {
                    Multiselect = true,
                    Filter = "All Files (*.*)|*.*",
                    Title = "Select Files"
                };
                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _selectedFiles = openFileDialog.FileNames.ToList();
                    SelectedFilesListBox.ItemsSource = _selectedFiles;
                }
            }
            else if (result == Wpf.Ui.Controls.MessageBoxResult.Secondary)
            {
                var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Pick a folder to scan",
                    UseDescriptionForTitle = true
                };

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string folderPath = folderBrowserDialog.SelectedPath;
                    _selectedFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories).ToList();
                    SelectedFilesListBox.ItemsSource = _selectedFiles;
                }
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFiles.Count == 0)
            {
                System.Windows.MessageBox.Show("Yo, pick at least one file or folder to scan!", "No Files Selected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            StartButton.IsEnabled = false;
            ChooseFilesButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
            FileEnScan.Content = "Scanning...";
            Progress.Value = 0;
            resultssss.Text = "";
            viruussss.Text = "0";
            nonsignedfileeee.Text = "0";
            hashshavirusss.Text = "0";
            nonvirusss.Text = "0";
            _malwareFiles.Clear();
            MalwareListBox.ItemsSource = null;

            try
            {
                var (hashMalwareCount, mlMalwareCount, nonCertifiedCount, totalFilesScanned, f1Score) = await RunScanAsync(_selectedFiles, _cancellationTokenSource.Token);

                if (hashMalwareCount == 0 && mlMalwareCount == 0)
                {
                    resultssss.Text = $"✅ EVERYTHING WELL! Non-certified PE files: {nonCertifiedCount}";
                    resultssss.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    resultssss.Text = $"MALWARES FOUND IN YOUR DEVICE";
                    resultssss.Foreground = System.Windows.Media.Brushes.Red;
                }

                viruussss.Text = (hashMalwareCount + mlMalwareCount).ToString();
                nonsignedfileeee.Text = nonCertifiedCount.ToString();
                hashshavirusss.Text = hashMalwareCount.ToString();
                nonvirusss.Text = (totalFilesScanned - (hashMalwareCount + mlMalwareCount)).ToString();

                MalwareListBox.ItemsSource = _malwareFiles.Select(m => $"{m.FilePath} This malawre was detected using {m.DetectionMethod}").ToList();

                SaveToMainScan(hashMalwareCount + mlMalwareCount, totalFilesScanned);
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
                ChooseFilesButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
                Progress.Value = 100;
            }
        }

        private async Task<(int HashMalwareCount, int MLMalwareCount, int NonCertifiedCount, int TotalFilesScanned, double F1Score)> RunScanAsync(List<string> files, CancellationToken cancellationToken)
        {
            int hashMalwareCount = 0;
            int mlMalwareCount = 0;
            int nonCertifiedCount = 0;
            int totalFiles = files.Count;
            int scannedFiles = 0;
            int totalFilesScanned = 0;
            int truePositives = 0;
            int falsePositives = 0;
            int falseNegatives = 0;

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
                        string fileHash = ComputeSHA256(filePath);
                        bool isActuallyMalware = _malwareHashes.Contains(fileHash);
                        bool isPeFile = IsPeFile(filePath);
                        bool detectedAsMalware = false;

                        Debug.WriteLine($"File: {filePath}, Hash: {fileHash}, IsActuallyMalware: {isActuallyMalware}, IsPeFile: {isPeFile}");

                        if (isPeFile)
                        {
                            bool isCertified = HasValidCertificate(filePath);
                            Debug.WriteLine($"  PE File - IsCertified: {isCertified}");
                            if (!isCertified)
                            {
                                nonCertifiedCount++;
                                if (_model != null)
                                {
                                    var (score, _) = ScanExeFileWithML(filePath);
                                    Debug.WriteLine($"  ML Score: {score}");
                                    if (score > 0.3f)
                                    {
                                        mlMalwareCount++;
                                        _malwareFiles.Add((filePath, "ML Model"));
                                        CopyToQuarantine(filePath);
                                        detectedAsMalware = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (isActuallyMalware)
                            {
                                hashMalwareCount++;
                                _malwareFiles.Add((filePath, "SHA256"));
                                CopyToQuarantine(filePath);
                                detectedAsMalware = true;
                            }
                        }

                        Debug.WriteLine($"  DetectedAsMalware: {detectedAsMalware}");

                        if (isActuallyMalware && detectedAsMalware)
                        {
                            truePositives++;
                        }
                        else if (!isActuallyMalware && detectedAsMalware)
                        {
                            falsePositives++;
                        }
                        else if (isActuallyMalware && !detectedAsMalware)
                        {
                            falseNegatives++;
                        }

                        Debug.WriteLine($"  TP: {truePositives}, FP: {falsePositives}, FN: {falseNegatives}");

                        scannedFiles++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error scanning {filePath}: {ex.Message}");
                    }
                    totalFilesScanned++;
                }
            }, cancellationToken);

            double precision = truePositives > 0 ? (double)truePositives / (truePositives + falsePositives) : 0;
            double recall = truePositives > 0 ? (double)truePositives / (truePositives + falseNegatives) : 0;
            double f1Score = (precision + recall) > 0 ? 2 * (precision * recall) / (precision + recall) : 0;

            Debug.WriteLine($"Final - TP: {truePositives}, FP: {falsePositives}, FN: {falseNegatives}, Precision: {precision:F2}, Recall: {recall:F2}, F1: {f1Score:F2}");

            return (hashMalwareCount, mlMalwareCount, nonCertifiedCount, totalFilesScanned, f1Score);
        }

        private void CopyToQuarantine(string filePath)
        {
            try
            {
                Directory.CreateDirectory(_quarantinePath);
                string fileName = Path.GetFileName(filePath);
                string destPath = Path.Combine(_quarantinePath, $"{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}");
                File.Copy(filePath, destPath, false);
                Debug.WriteLine($"Quarantined: {filePath} to {destPath}");
                LogToQuarantineTable(filePath, destPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error quarantining {filePath}: {ex.Message}");
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
                Debug.WriteLine($"Error logging to quarantin table for {originalPath}: {ex.Message}");
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
                throw new ArgumentException("Gotta be an .exe file, fam!");

            var peFile = new PeFile(filePath);
            if (!peFile.IsExe)
                throw new ArgumentException("This ain't a valid PE executable!");

            var dosHeaderData = ParseDosHeader(filePath);
            var fileHeader = peFile.ImageNtHeaders?.FileHeader ?? throw new InvalidOperationException("FileHeader's missing!");
            var optHeader = peFile.ImageNtHeaders?.OptionalHeader ?? throw new InvalidOperationException("OptionalHeader's AWOL!");

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
                MinorImageVersion = optHeader.MajorImageVersion,
                MajorSubsystemVersion = optHeader.MajorSubsystemVersion,
                MinorSubsystemVersion = optHeader.MajorSubsystemVersion,
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

        private void SaveToMainScan(int numOfMalwares, int numOfFilesScanned)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_databasePath};"))
                {
                    connection.Open();
                    string insertQuery = @"
                        INSERT INTO MainScan (NumOfMalwares, NumOfFilesScaned, LastScan, StateOfSys)
                        VALUES (@NumOfMalwares, @NumOfFilesScaned, @LastScan, @StateOfSys)";
                    using (var command = new SqliteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@NumOfMalwares", numOfMalwares);
                        command.Parameters.AddWithValue("@NumOfFilesScaned", numOfFilesScanned);
                        command.Parameters.AddWithValue("@LastScan", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                        command.Parameters.AddWithValue("@StateOfSys", numOfMalwares > 0 ? 1 : 0);
                        command.ExecuteNonQuery();
                    }
                    string selectQuery = "SELECT * FROM MainScan ORDER BY LastScan DESC LIMIT 1";
                    using (var command = new SqliteCommand(selectQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string result = $"Saved to MainScan:\n" +
                                               $"NumOfMalwares: {reader["NumOfMalwares"]}\n" +
                                               $"NumOfFilesScaned: {reader["NumOfFilesScaned"]}\n" +
                                               $"LastScan: {reader["LastScan"]} ({DateTimeOffset.FromUnixTimeSeconds((long)reader["LastScan"]).ToString("yyyy-MM-dd HH:mm:ss")})\n" +
                                               $"StateOfSys: {reader["StateOfSys"]}";
                                System.Windows.MessageBox.Show(result, "Database Save Confirmation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving to MainScan table: {ex.Message}", "Database Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
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

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
        }

        private void TextBox_TextChanged_1(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
        }
    }
}