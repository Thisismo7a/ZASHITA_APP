# Zashita 🛡️

Zashita is a free antivirus for Windows — a powerful file scanner that detects threats using a hybrid approach combining machine learning and SHA256 signature matching.

Built with C#, WPF, ML.NET, and PeNet, Zashita provides intelligent, fast, and customizable scans to help secure your system from malware and suspicious files.

Whether you're a casual user or a developer, Zashita offers reliable protection with a modern and lightweight design all at no cost.



## Features ✨

- **Three Scanning Modes:**
  - **Full Scan**: Scans the entire system or directory tree
  - **Quick Scan**: Targets key system paths for faster results
  - **Selective Scan**: User-defined files/folders for scanning

- **Machine Learning:**
  - Scans .exe, .dll, and .sys files using a trained ML.NET model
  - PE header features extracted using PeNet 5.0.0

- **SHA256 Hash Signature Detection:**
  - Used for all other file types
  - Matches against a custom malware hash database

- **Quarantine System:**
  - Detected malware is moved (not copied) to: `C:\zashita-malwares`
  - Change made via: `File.Move(sourcePath, quarantinePath);`

- **Custom Dataset Training:**
  - Add your own CSV datasets and retrain your detection model using ML.NET

## How Zashita Works 🛠️

```
Input Files
    ↓
Scan Mode (Full / Quick / Selective)
    ↓
File Type Check
┌───────────────┬──────────────────────┐
│ PE Files      │ Other File Types     │
│ (.exe/.dll)   │ (.txt/.jpg/.zip etc) │
└───────────────┴──────────────────────┘
    ↓                   ↓
ML.NET Model       SHA256 Signature Check
    ↓                   ↓
    ─────────> Detection Result
                ↓
         Quarantine (if malicious)
```

## Installation 📥

### Prerequisites

- Windows OS
- .NET 6+ SDK
- Visual Studio 2022
- Git (optional)

### Step-by-Step Installation

1. **Create a Datasets Folder**

   Create a new folder in your C: drive:

   ```bash
   mkdir C:\Datasets
   ```

   Put your datasets there. Required file:
   - `PE_Header.csv`

   ```csharp
   string datasetPath = @"C:\Datasets\PE_Header.csv";
   public const string RetrainFilePath = @"C:\Datasets\PE_Header.csv";
   ```

   > **Note**: Copy the `PE_Header.csv` file from the `dataset` folder in the project and paste it into the `C:\Datasets` folder.  
   > — mo7aaaaa

2. **Clone the Repository**

   ```bash
   git clone https://github.com/your-username/zashita.git
   ```

3. **Build and Run**

   Open the solution in Visual Studio 2022 and run the app.

   Or run via CLI:

   ```bash
   cd zashita
   dotnet run
   ```

## Train Your Own Model

Want to customize the detection engine? Train a model using your own dataset!

1. **Dataset Format**

   CSV file with features + label column (1 = malware, 0 = clean)

   Example:

   ```csv
   SizeOfCode,Entropy,Imports,Label
   5120,7.32,15,1
   2048,6.28,5,0
   ```

2. **Add Dataset to Disk**

   Save your dataset here:

   ```csharp
   string datasetPath = @"C:\Datasets\PE_Header.csv";
   ```

   > **Note**: Add ur dataset file in the dataset folder , then rename to `PE_Header.csv`.  
   > — mo7aaaaa 🔥

3. **Train the Model**

   **Option A: ML.NET Model Builder**
   - Right-click your project → Add → Machine Learning
   - Load your CSV → Set `Label` as output
   - Train & export as `MLModel.zip`

   **Option B: ML.NET CLI**

   ```bash
   dotnet tool install -g mlnet
   mlnet auto-train --task binary-classification --label-column-name Label --dataset "C:\Datasets\PE_Header.csv" --output "MLModel"
   ```

4. **Replace Model in App**

   - Copy the new `MLModel.zip` to the `MLModel/` folder
   - Update the model path if needed:

     ```csharp
     string modelPath = "MLModel/MLModel.zip";
     ```

   - Ensure `ModelInput.cs` and `ModelOutput.cs` match the new structure

## Command Reference

```bash
mkdir C:\Datasets
git clone https://github.com/your-username/zashita.git
copy zashita\dataset\PE_Header.csv C:\Datasets\
```

## Contributing

1. Fork the repo
2. Create your branch: `git checkout -b feature/my-feature`
3. Commit your changes
4. Push and open a pull request

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.


 
— mo7aaaaa
