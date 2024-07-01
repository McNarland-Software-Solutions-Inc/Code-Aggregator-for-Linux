using System;
using System.Collections.Generic;
using Gtk;

public class MainWindow : Window
{
    private TreeView folderTreeView;
    private Entry outputPathEntry;
    private Button selectFolderButton, aggregateButton, selectOutputButton;
    private string? sourceFolder;
    private CodeAggregatorGtk.SettingsHandler settingsHandler;
    private CodeAggregatorGtk.TreeViewHandler? treeViewHandler;

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
        hbox.PackStart(outputPathEntry, true, true, 5);

        selectOutputButton = new Button("Select Output");
        selectOutputButton.Clicked += OnSelectOutputClicked;
        hbox.PackStart(selectOutputButton, false, false, 5);

        aggregateButton = new Button("Aggregate");
        aggregateButton.Clicked += OnAggregateClicked;
        hbox.PackStart(aggregateButton, false, false, 5);

        vbox.PackStart(hbox, false, false, 5);

        folderTreeView = new TreeView();
        vbox.PackStart(folderTreeView, true, true, 5);

        Add(vbox);

        // Initialize settings handler
        settingsHandler = new CodeAggregatorGtk.SettingsHandler();

        // Initialize tree view handler if source folder is set
        if (!string.IsNullOrEmpty(settingsHandler.Settings.SourceFolder))
        {
            sourceFolder = settingsHandler.Settings.SourceFolder;
            treeViewHandler = new CodeAggregatorGtk.TreeViewHandler(folderTreeView, sourceFolder);
            treeViewHandler.PopulateTreeView();
        }

        // Handle window delete event to exit gracefully
        DeleteEvent += (sender, args) => 
        {
            settingsHandler.SaveSettings();
            Application.Quit();
        };

        // Load settings and populate tree view if source folder is set
        if (!string.IsNullOrEmpty(settingsHandler.Settings.SourceFolder))
        {
            sourceFolder = settingsHandler.Settings.SourceFolder;
            treeViewHandler = new CodeAggregatorGtk.TreeViewHandler(folderTreeView, sourceFolder);
            treeViewHandler.PopulateTreeView();
        }
    }

    public void SetSettings(CodeAggregatorGtk.Settings settings)
    {
        settingsHandler.Settings = settings;
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
            treeViewHandler.PopulateTreeView();
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
            outputPathEntry.Text = fileChooser.Filename;
            settingsHandler.Settings.OutputFile = fileChooser.Filename;
        }

        fileChooser.Destroy();
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

            CodeAggregatorGtk.FileAggregator.AggregateFiles(sourceFolder, outputFilePath, include, exclude);
            MessageDialog md = new MessageDialog(this, DialogFlags.DestroyWithParent, MessageType.Info, ButtonsType.Ok, $"Files aggregated successfully into {outputFilePath}");
            md.Run();
            md.Destroy();
        }
        else
        {
            MessageDialog md = new MessageDialog(this, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok, "Please select a folder and specify an output file path.");
            md.Run();
            md.Destroy();
        }
    }
}
