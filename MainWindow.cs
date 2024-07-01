using System;
using System.Collections.Generic;
using System.IO;
using Gtk;

public class MainWindow : Window
{
    private TreeView folderTreeView;
    private Entry outputPathEntry;
    private Button selectFolderButton, aggregateButton, selectOutputButton;
    private string? sourceFolder;
    private CodeAggregatorGtk.SettingsHandler settingsHandler;
    private CodeAggregatorGtk.TreeViewHandler? treeViewHandler;
    private ProgressBar progressBar;

    public MainWindow() : base("Code Aggregator")
    {
        SetDefaultSize(800, 600);
        SetPosition(WindowPosition.Center);

        Box vbox = new Box(Gtk.Orientation.Vertical, 5);
        Box hbox = new Box(Gtk.Orientation.Horizontal, 5);

        selectFolderButton = new Button("Select Folder");
        selectFolderButton.Clicked += OnSelectFolderClicked;
        hbox.PackStart(selectFolderButton, false, false, 5);

        outputPathEntry = new Entry() { PlaceholderText = "Output File Path" };
        outputPathEntry.Changed += OnOutputPathChanged;
        hbox.PackStart(outputPathEntry, true, true, 5);

        selectOutputButton = new Button("Select Output");
        selectOutputButton.Clicked += OnSelectOutputClicked;
        hbox.PackStart(selectOutputButton, false, false, 5);

        aggregateButton = new Button("Aggregate");
        aggregateButton.Clicked += OnAggregateClicked;
        aggregateButton.Sensitive = false; // Initially disable the button
        hbox.PackStart(aggregateButton, false, false, 5);

        progressBar = new ProgressBar();
        progressBar.Visible = false;
        vbox.PackStart(progressBar, false, false, 5);

        vbox.PackStart(hbox, false, false, 5);

        folderTreeView = new TreeView();
        folderTreeView.Selection.Changed += OnSelectionChanged;
        vbox.PackStart(folderTreeView, true, true, 5);

        Add(vbox);

        // Initialize settings handler
        settingsHandler = new CodeAggregatorGtk.SettingsHandler();

        // Load settings and populate tree view if source folder is set
        string documentsDirectory = System.IO.Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "/", "Documents");
        if (!string.IsNullOrEmpty(settingsHandler.Settings.SourceFolder) && Directory.Exists(settingsHandler.Settings.SourceFolder))
        {
            sourceFolder = settingsHandler.Settings.SourceFolder;
        }
        else
        {
            sourceFolder = documentsDirectory;
        }

        treeViewHandler = new CodeAggregatorGtk.TreeViewHandler(folderTreeView, sourceFolder);
        treeViewHandler.PopulateTreeView(settingsHandler.Settings.SelectedFiles);

        // Handle window delete event to exit gracefully
        DeleteEvent += (sender, args) =>
        {
            SaveSelectedFiles();
            settingsHandler.SaveSettings();
            Application.Quit();
        };
    }

    public void SetSettings(CodeAggregatorGtk.Settings settings)
    {
        settingsHandler.Settings = settings;
    }

    private void SaveSelectedFiles()
    {
        settingsHandler.Settings.SelectedFiles = treeViewHandler?.GetSelectedFiles() ?? new List<string>();
    }

    private void OnSelectFolderClicked(object? sender, EventArgs e)
    {
        var folderChooser = new FileChooserDialog("Select Folder", this, FileChooserAction.SelectFolder);
        folderChooser.AddButton("Cancel", ResponseType.Cancel);
        folderChooser.AddButton("Select", ResponseType.Accept);

        if (folderChooser.Run() == (int)ResponseType.Accept)
        {
            sourceFolder = folderChooser.Filename;
            settingsHandler.Settings.SourceFolder = sourceFolder;
            treeViewHandler = new CodeAggregatorGtk.TreeViewHandler(folderTreeView, sourceFolder);
            treeViewHandler.PopulateTreeView(settingsHandler.Settings.SelectedFiles);
        }

        folderChooser.Destroy();
    }

    private void OnSelectOutputClicked(object? sender, EventArgs e)
    {
        var fileChooser = new FileChooserDialog("Select Output File", this, FileChooserAction.Save);
        fileChooser.AddButton("Cancel", ResponseType.Cancel);
        fileChooser.AddButton("Select", ResponseType.Accept);

        if (fileChooser.Run() == (int)ResponseType.Accept)
        {
            if (File.Exists(fileChooser.Filename))
            {
                var overwriteDialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, "File already exists. Overwrite?");
                if (overwriteDialog.Run() == (int)ResponseType.Yes)
                {
                    outputPathEntry.Text = fileChooser.Filename;
                    settingsHandler.Settings.OutputFile = fileChooser.Filename;
                }
                overwriteDialog.Destroy();
            }
            else
            {
                outputPathEntry.Text = fileChooser.Filename;
                settingsHandler.Settings.OutputFile = fileChooser.Filename;
            }
        }

        fileChooser.Destroy();
    }

    private void OnOutputPathChanged(object? sender, EventArgs e)
    {
        ValidateAggregateButtonState();
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        ValidateAggregateButtonState();
    }

    private void ValidateAggregateButtonState()
    {
        var outputPathSet = !string.IsNullOrEmpty(outputPathEntry.Text);
        var anyFileSelected = treeViewHandler?.AnyFileSelected() ?? false;

        aggregateButton.Sensitive = outputPathSet && anyFileSelected;
    }

    private void OnAggregateClicked(object? sender, EventArgs e)
    {
        var outputFilePath = outputPathEntry.Text;
        if (!string.IsNullOrEmpty(sourceFolder) && !string.IsNullOrEmpty(outputFilePath))
        {
            var include = new List<string>();
            var exclude = new List<string>();
            if (folderTreeView.Model is TreeStore store)
            {
                treeViewHandler?.CollectSelectedItems(store, include, exclude);
            }

            // Show progress bar
            progressBar.Visible = true;
            progressBar.Fraction = 0;

            // Run aggregation in a separate task to keep UI responsive
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    CodeAggregatorGtk.FileAggregator.AggregateFiles(sourceFolder, outputFilePath, include, exclude, UpdateProgress);
                    Application.Invoke((_, __) =>
                    {
                        progressBar.Visible = false;
                        MessageDialog md = new MessageDialog(this, DialogFlags.DestroyWithParent, MessageType.Info, ButtonsType.Ok, $"Files aggregated successfully into {outputFilePath}");
                        md.Run();
                        md.Destroy();
                    });
                }
                catch (Exception ex)
                {
                    Application.Invoke((_, __) =>
                    {
                        progressBar.Visible = false;
                        MessageDialog md = new MessageDialog(this, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok, $"An error occurred: {ex.Message}");
                        md.Run();
                        md.Destroy();
                    });
                }
            });
        }
        else
        {
            MessageDialog md = new MessageDialog(this, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok, "Please select a folder and specify an output file path.");
            md.Run();
            md.Destroy();
        }
    }

    private void UpdateProgress(double progress)
    {
        Application.Invoke((_, __) =>
        {
            progressBar.Fraction = progress;
        });
    }
}
