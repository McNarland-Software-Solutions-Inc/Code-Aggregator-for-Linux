using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CodeAggregatorGtk
{
    public class FileAggregator
    {
        public static void AggregateFiles(string sourceFolder, string outputFile, List<string> selectedNodes, Action<double> progressCallback)
        {
            int totalNodes = selectedNodes.Count;
            int processedNodes = 0;

            using (var output = new StreamWriter(outputFile, false, Encoding.UTF8))
            {
                output.WriteLine($"Source Folder: {sourceFolder}");

                foreach (var node in selectedNodes)
                {
                    string relativePath = Path.GetRelativePath(sourceFolder, node);
                    bool isTextFile = IsTextFile(node);

                    if (isTextFile)
                    {
                        output.WriteLine($"\n--- Start of File: {relativePath} ---\n");
                        foreach (var line in File.ReadLines(node))
                        {
                            output.WriteLine(line);
                        }
                        output.WriteLine($"\n--- End of File: {relativePath} ---\n");
                    }

                    processedNodes++;
                    progressCallback((double)processedNodes / totalNodes);
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
