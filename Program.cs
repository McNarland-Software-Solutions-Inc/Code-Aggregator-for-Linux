using System;
using Gtk;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

namespace CodeAggregatorGtk
{
    class Program
    {
        static string settingsFile = "settings.json";
        static Settings settings = new Settings();

        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--help")
            {
                ShowHelp();
                return;
            }

            LoadSettings();

            if (args.Length > 0)
            {
                HandleCommandLineArgs(args);
            }
            else
            {
                Application.Init();
                var settingsHandler = new SettingsHandler();
                var win = new MainWindow();
                win.SetDefaultSize(800, 600);
                win.SetPosition(WindowPosition.Center);
                win.ShowAll();
                win.SetSettings(settingsHandler.Settings);
                Application.Run();
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run <source_folder> [-o:<output_file>] [-q]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -o:<output_file>    Specify the output file.");
            Console.WriteLine("  -q                  Quiet mode (no output).");
        }

        static void HandleCommandLineArgs(string[] args)
        {
            string sourceFolder = args[0];
            string outputFile = "aggregated.txt";
            bool quietMode = false;

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].StartsWith("-o:"))
                {
                    outputFile = args[i].Substring(3);
                }
                else if (args[i] == "-q")
                {
                    quietMode = true;
                }
            }

            try
            {
                AggregateFiles(sourceFolder, outputFile);
                if (!quietMode)
                {
                    Console.WriteLine($"Files aggregated successfully into {outputFile}");
                }
            }
            catch (Exception ex)
            {
                if (!quietMode)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        static void AggregateFiles(string sourceFolder, string outputFile)
        {
            var selectedNodes = settings.SelectedNodes;
            using (var output = new StreamWriter(outputFile))
            {
                foreach (var file in selectedNodes)
                {
                    var content = File.ReadAllText(file);
                    output.WriteLine(content);
                }
            }
        }

        static void LoadSettings()
        {
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                settings = JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
            }
        }

        static void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(settingsFile, json);
        }
    }
}