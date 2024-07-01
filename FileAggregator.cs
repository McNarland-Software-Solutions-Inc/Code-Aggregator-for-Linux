using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CodeAggregatorGtk
{
    public class FileAggregator
    {
        public static void AggregateFiles(string sourceFolder, string outputFile, List<string> include, List<string> exclude, Action<double> progressCallback)
        {
            var files = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);
            int totalFiles = files.Length;
            int processedFiles = 0;

            using (var output = new StreamWriter(outputFile, false, Encoding.UTF8))
            {
                output.WriteLine($"Source Folder: {sourceFolder}");

                foreach (var file in files)
                {
                    string relativePath = Path.GetRelativePath(sourceFolder, file);
                    if (exclude.Contains(file) || exclude.Contains(relativePath))
                    {
                        continue;
                    }

                    if (include.Count == 0 || include.Contains(file) || include.Contains(relativePath))
                    {
                        bool isTextFile = IsTextFile(file);
                        if (isTextFile)
                        {
                            output.WriteLine($"\n--- Start of File: {relativePath} ---\n");
                            foreach (var line in File.ReadLines(file))
                            {
                                output.WriteLine(line);
                            }
                            output.WriteLine($"\n--- End of File: {relativePath} ---\n");
                        }
                    }

                    processedFiles++;
                    progressCallback((double)processedFiles / totalFiles);
                }
            }
        }

        private static bool IsTextFile(string filePath)
        {
            try
            {
                using (var stream = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true))
                {
                    char[] buffer = new char[512];
                    int charsRead = stream.Read(buffer, 0, buffer.Length);
                    if (charsRead == 0)
                        return false;

                    for (int i = 0; i < charsRead; i++)
                    {
                        if (char.IsControl(buffer[i]) && buffer[i] != '\r' && buffer[i] != '\n' && buffer[i] != '\t')
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
