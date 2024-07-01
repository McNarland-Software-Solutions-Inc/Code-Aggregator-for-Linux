using System;
using System.Collections.Generic;
using System.IO;
using Gtk;
using CodeAggregatorGtk;

public class MainWindow : Window
{
    private TreeView folderTreeView;
    private Entry outputPathEntry;
    private Button selectFolderButton, aggregateButton, selectOutputButton;
    private string? sourceFolder;
    private SettingsHandler settingsHandler;
    private TreeViewHandler? treeViewHandler;
    private ProgressBar progressBar;
    private CodeAggregatorGtk.TreeNode rootNode;

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
        settingsHandler = new SettingsHandler();

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

        if (settingsHandler.Settings.OutputPaths.ContainsKey(sourceFolder))
        {
            outputPathEntry.Text = settingsHandler.Settings.OutputPaths[sourceFolder];
        }

        rootNode = BuildTreeNode(sourceFolder);
        treeViewHandler = new TreeViewHandler(folderTreeView, rootNode);
        treeViewHandler.PopulateTreeView(settingsHandler.Settings.SelectedNodes);

        // Handle window delete event to exit gracefully
        DeleteEvent += (sender, args) =>
        {
            SaveSelectedNodes();
            settingsHandler.SaveSettings();
            Application.Quit();
        };
    }

    public void SetSettings(CodeAggregatorGtk.Settings settings)
    {
        settingsHandler.Settings = settings;
    }

    private void SaveSelectedNodes()
    {
        settingsHandler.Settings.SelectedNodes = treeViewHandler?.GetSelectedNodes() ?? new List<string>();
        settingsHandler.Settings.OutputPaths[sourceFolder!] = outputPathEntry.Text;
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
            rootNode = BuildTreeNode(sourceFolder);
            treeViewHandler = new TreeViewHandler(folderTreeView, rootNode);
            treeViewHandler.PopulateTreeView(settingsHandler.Settings.SelectedNodes);

            if (settingsHandler.Settings.OutputPaths.ContainsKey(sourceFolder))
            {
                outputPathEntry.Text = settingsHandler.Settings.OutputPaths[sourceFolder];
            }
            else
            {
                outputPathEntry.Text = string.Empty;
            }
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
                    settingsHandler.Settings.OutputPaths[sourceFolder!] = fileChooser.Filename;
                }
                overwriteDialog.Destroy();
            }
            else
            {
                outputPathEntry.Text = fileChooser.Filename;
                settingsHandler.Settings.OutputFile = fileChooser.Filename;
                settingsHandler.Settings.OutputPaths[sourceFolder!] = fileChooser.Filename;
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
            var selectedNodes = treeViewHandler?.GetSelectedNodes() ?? new List<string>();

            // Show progress bar
            progressBar.Visible = true;
            progressBar.Fraction = 0;

            // Run aggregation in a separate task to keep UI responsive
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    CodeAggregatorGtk.FileAggregator.AggregateFiles(sourceFolder, outputFilePath, selectedNodes, UpdateProgress);
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

    private CodeAggregatorGtk.TreeNode BuildTreeNode(string path)
    {
        var node = new CodeAggregatorGtk.TreeNode(path);
        if (Directory.Exists(path))
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                var childNode = BuildTreeNode(dir);
                node.AddChild(childNode);
            }

            foreach (var file in Directory.GetFiles(path))
            {
                var childNode = new CodeAggregatorGtk.TreeNode(file) { IsSelected = true };
                node.AddChild(childNode);
            }
        }
        return node;
    }
}
