# Code Aggregator

## Description

Code Aggregator is an application that allows you to select a root folder and aggregate all text files within the selected folder and its subfolders into a single text file. This is particularly useful for consolidating code files or other text-based files.

## Features

- Select a root folder for aggregation.
- Include or exclude specific files and folders using checkboxes.
- Save the aggregated text to a specified location with a file chooser dialog.
- Option to open the aggregated file after the operation completes.
- Command line support for running the aggregator without the GUI.
- Saves the selected folders and files for future use.
- Option for recursive inclusion of folders and files.
- Maintains previously selected folder and state of checkboxes.

## How to Use

### Graphical User Interface

1. Click the 'Select Folder' button to choose the root folder for aggregation.
2. Use the checkboxes to include or exclude specific files and folders. By default, all folders and files are included.
3. Click 'Select Output' to choose the output file path.
4. Click 'Aggregate' to begin the aggregation process. You will be prompted to specify the location and name of the output file.
5. Note: It is recommended to exclude folders like .git, .vs, or other dependency folders to avoid including unnecessary files.

### Command Line Interface

- `?` - Show command line syntax and tips.
- `source_folder` - Run the aggregator on this source folder.
- `-o:"New_Output_Folders\New_Output_Filename.ext"` - Change the output file for this run only.
- `-oc:"New_Output_Folders\New_Output_Filename.ext"` - Change the output file and update the JSON settings.
- `-a:"Folder\Folder_or_File_to_Add[.ext]"` - Add this folder or file for this run only.
- `-ac:"Folder\Folder_or_File_to_Add[.ext]"` - Add this folder or file and update the JSON settings.
- `-q` - Quiet mode (no output from the program, don't show windows).

#### Error Codes

- `0` - Worked perfectly.
- `1` - Folder not found (when trying to add a new folder).
- `2` - File not found (when trying to add a new file).
- `3` - Error outputting to the output file.
- `4` - Source folder not found.

## Installation

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- GTK# for .NET

### Building the Executable

1. Clone the repository:

    ```sh
    git clone https://github.com/R3troR0b/CodeAggregatorLinux.git
    cd CodeAggregatorLinux
    ```

2. Restore the project dependencies:

    ```sh
    dotnet restore
    ```

3. Build the project:

    ```sh
    dotnet build --configuration Release
    ```

### Running the Application

1. Run the application:

    ```sh
    dotnet run
    ```

### Creating the Installer

1. Ensure you have [Inno Setup](https://jrsoftware.org/isinfo.php) installed.

2. Open the `Inno Install Compiler Script.iss` file in Inno Setup.

3. Compile the script. This will look for the built executable in the `bin/Release/net8.0-windows` folder and create a `setup.exe` file in the `Output` folder.

### Running the Installer

1. Navigate to the `Output` folder where the `setup.exe` file was created.

2. Run `setup.exe` to install the Code Aggregator application.

3. After installation, you can find the application in the Start Menu under "Code Aggregator."

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any changes.

## License

This project is licensed under the MIT License.
