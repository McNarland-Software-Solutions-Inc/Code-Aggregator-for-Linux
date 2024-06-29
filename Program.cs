using System;
using System.Collections.Generic;
using System.IO;
using Gtk;
using Newtonsoft.Json;

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

            settings = LoadSettings();

            if (args.Length > 0)
            {
                HandleCommandLineArgs(args);
            }
            else
            {
                Application.Init();
                var win = new MainWindow();
                win.SetDefaultSize(800, 600);
                win.SetPosition(WindowPosition.Center);
                win.ShowAll();
                win.SetSettings(settings);
                Application.Run();
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run <source_folder> [-o:<output_file>] [-q] [-a:<file_or_folder_to_include>] [-r:<file_or_folder_to_exclude>]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -o:<output_file>    Specify the output file.");
            Console.WriteLine("  -q                  Quiet mode (no output).");
            Console.WriteLine("  -a:<file_or_folder_to_include>    Add file or folder to include list.");
            Console.WriteLine("  -r:<file_or_folder_to_exclude>    Add file or folder to exclude list.");
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
                else if (args[i].StartsWith("-a:"))
                {
                    string item = args[i].Substring(3);
                    settings.Include.Add(item);
                    SaveSettings(settings);
                }
                else if (args[i].StartsWith("-r:"))
                {
                    string item = args[i].Substring(3);
                    settings.Exclude.Add(item);
                    SaveSettings(settings);
                }
            }

            try
            {
                AggregateFiles(sourceFolder, outputFile, settings.Include, settings.Exclude);
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

        static void AggregateFiles(string sourceFolder, string outputFile, List<string> include, List<string> exclude)
        {
            using (var output = new StreamWriter(outputFile))
            {
                foreach (var file in Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories))
                {
                    if (exclude.Contains(file))
                    {
                        continue;
                    }

                    if (include.Count == 0 || include.Contains(file))
                    {
                        var content = File.ReadAllText(file);
                        output.WriteLine(content);
                    }
                }
            }
        }

        static Settings LoadSettings()
        {
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                return JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
            }
            return new Settings();
        }

        static void SaveSettings(Settings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(settingsFile, json);
        }
    }
}
