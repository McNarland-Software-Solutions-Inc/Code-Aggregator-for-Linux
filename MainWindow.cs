using System;
using System.Collections.Generic;
using System.IO;
using Gtk;

public class MainWindow : Window
{
    private TreeView folderTreeView;
    private Entry outputPathEntry;
    private Button selectFolderButton, aggregateButton;

    private string? sourceFolder;
    private CodeAggregatorGtk.Settings settings;

    public MainWindow() : base("Code Aggregator")
    {
        SetDefaultSize(800, 600);
        SetPosition(WindowPosition.Center);

        Box vbox = new Box(Orientation.Vertical, 5);
        Box hbox = new Box(Orientation.Horizontal, 5);

        selectFolderButton = new Button("Select Folder");
        selectFolderButton.Clicked += OnSelectFolderClicked;
        hbox.PackStart(selectFolderButton, false, false, 5);

        outputPathEntry = new Entry() { PlaceholderText = "Output File Path" };
        hbox.PackStart(outputPathEntry, true, true, 5);

        aggregateButton = new Button("Aggregate");
        aggregateButton.Clicked += OnAggregateClicked;
        hbox.PackStart(aggregateButton, false, false, 5);

        vbox.PackStart(hbox, false, false, 5);

        folderTreeView = new TreeView();
        vbox.PackStart(folderTreeView, true, true, 5);

        Add(vbox);

         // Initialize settings
        settings = new CodeAggregatorGtk.Settings();
    }

    private void OnSelectFolderClicked(object? sender, EventArgs e)
    {
        var folderChooser = new FileChooserDialog("Select Folder", this, FileChooserAction.SelectFolder);
        folderChooser.AddButton("Cancel", ResponseType.Cancel);
        folderChooser.AddButton("Select", ResponseType.Accept);

        if (folderChooser.Run() == (int)ResponseType.Accept)
        {
            sourceFolder = folderChooser.Filename;
            PopulateTreeView();
        }

        folderChooser.Destroy();
    }

    private void PopulateTreeView()
    {
        var store = new TreeStore(typeof(string), typeof(string));
        folderTreeView.Model = store;
        folderTreeView.AppendColumn("Name", new CellRendererText(), "text", 0);

        PopulateTreeView(store, sourceFolder!, null);
    }

    private void PopulateTreeView(TreeStore store, string path, TreeIter? parent)
{
    foreach (var dir in Directory.GetDirectories(path))
    {
        TreeIter iter;
        if (parent.HasValue)
        {
            iter = store.AppendValues(parent.Value, System.IO.Path.GetFileName(dir), dir);
        }
        else
        {
            iter = store.AppendValues(System.IO.Path.GetFileName(dir), dir);
        }
        PopulateTreeView(store, dir, iter);
    }
    foreach (var file in Directory.GetFiles(path))
    {
        if (parent.HasValue)
        {
            store.AppendValues(parent.Value, System.IO.Path.GetFileName(file), file);
        }
        else
        {
            store.AppendValues(System.IO.Path.GetFileName(file), file);
        }
    }
}


    private void OnAggregateClicked(object? sender, EventArgs e)
    {
        var outputFilePath = outputPathEntry.Text;
        if (!string.IsNullOrEmpty(sourceFolder) && !string.IsNullOrEmpty(outputFilePath))
        {
            AggregateFiles(sourceFolder, outputFilePath, settings.Include, settings.Exclude);
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

    private void AggregateFiles(string sourceFolder, string outputFile, List<string> include, List<string> exclude)
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

    public void SetSettings(CodeAggregatorGtk.Settings settings)
    {
        this.settings = settings;
    }
}
