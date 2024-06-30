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
    private CodeAggregatorGtk.Settings settings;

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

        // Initialize settings
        settings = new CodeAggregatorGtk.Settings();

        // Handle window delete event to exit gracefully
        DeleteEvent += (sender, args) => 
        {
            SaveSettings();
            Application.Quit();
        };

        // Load settings and populate tree view if source folder is set
        LoadSettings();
        if (!string.IsNullOrEmpty(settings.SourceFolder))
        {
            sourceFolder = settings.SourceFolder;
            PopulateTreeView();
        }
    }

    private void OnSelectFolderClicked(object? sender, EventArgs e)
    {
        var folderChooser = new FileChooserDialog("Select Folder", this, FileChooserAction.SelectFolder);
        folderChooser.AddButton("Cancel", ResponseType.Cancel);
        folderChooser.AddButton("Select", ResponseType.Accept);

        if (folderChooser.Run() == (int)ResponseType.Accept)
        {
            sourceFolder = folderChooser.Filename;
            settings.SourceFolder = sourceFolder;
            PopulateTreeView();
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
        }

        fileChooser.Destroy();
    }

    private void PopulateTreeView()
    {
        var store = new TreeStore(typeof(bool), typeof(string), typeof(string));
        folderTreeView.Model = store;

        var toggleRenderer = new CellRendererToggle();
        toggleRenderer.Toggled += OnToggled;

        folderTreeView.AppendColumn("Include", toggleRenderer, "active", 0);
        folderTreeView.AppendColumn("Name", new CellRendererText(), "text", 1);

        PopulateTreeView(store, sourceFolder!, null);

        RestoreCheckedItems(store);
    }

    private void PopulateTreeView(TreeStore store, string path, TreeIter? parent)
    {
        foreach (var dir in Directory.GetDirectories(path))
        {
            TreeIter iter;
            if (parent.HasValue)
            {
                iter = store.AppendValues(parent.Value, false, System.IO.Path.GetFileName(dir), dir);
            }
            else
            {
                iter = store.AppendValues(false, System.IO.Path.GetFileName(dir), dir);
            }
            PopulateTreeView(store, dir, iter);
        }
        foreach (var file in Directory.GetFiles(path))
        {
            if (parent.HasValue)
            {
                store.AppendValues(parent.Value, false, System.IO.Path.GetFileName(file), file);
            }
            else
            {
                store.AppendValues(false, System.IO.Path.GetFileName(file), file);
            }
        }
    }

    private void OnToggled(object sender, ToggledArgs args)
    {
        if (folderTreeView.Model is TreeStore store)
        {
            if (store.GetIterFromString(out TreeIter iter, args.Path))
            {
                bool active = (bool)store.GetValue(iter, 0);
                store.SetValue(iter, 0, !active);

                string path = (string)store.GetValue(iter, 2);
                if (Directory.Exists(path))
                {
                    ToggleChildren(store, iter, !active);
                }
            }
        }
    }

    private void ToggleChildren(TreeStore store, TreeIter parent, bool active)
    {
        if (store.IterChildren(out TreeIter childIter, parent))
        {
            do
            {
                store.SetValue(childIter, 0, active);
                string path = (string)store.GetValue(childIter, 2);
                if (Directory.Exists(path))
                {
                    ToggleChildren(store, childIter, active);
                }
            } while (store.IterNext(ref childIter));
        }
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
                CollectSelectedItems(store, include, exclude);
            }

            AggregateFiles(sourceFolder, outputFilePath, include, exclude);
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

    private void CollectSelectedItems(TreeStore store, List<string> include, List<string> exclude)
    {
        if (store.GetIterFirst(out TreeIter iter))
        {
            CollectSelectedItems(store, iter, include, exclude);
        }
    }

    private void CollectSelectedItems(TreeStore store, TreeIter iter, List<string> include, List<string> exclude)
    {
        bool isSelected = (bool)store.GetValue(iter, 0);
        string path = (string)store.GetValue(iter, 2);

        if (isSelected)
        {
            include.Add(path);
        }
        else
        {
            exclude.Add(path);
        }

        if (store.IterChildren(out TreeIter childIter, iter))
        {
            do
            {
                CollectSelectedItems(store, childIter, include, exclude);
            } while (store.IterNext(ref childIter));
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

    private void SaveSettings()
    {
        // Save settings to file
        string settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        File.WriteAllText(settingsPath, Newtonsoft.Json.JsonConvert.SerializeObject(settings));
    }

    private void LoadSettings()
    {
        // Load settings from file
        string settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        if (File.Exists(settingsPath))
        {
            settings = Newtonsoft.Json.JsonConvert.DeserializeObject<CodeAggregatorGtk.Settings>(File.ReadAllText(settingsPath)) ?? new CodeAggregatorGtk.Settings();
        }
    }

    private void RestoreCheckedItems(TreeStore store)
    {
        foreach (var include in settings.Include)
        {
            SetItemChecked(store, include, true);
        }

        foreach (var exclude in settings.Exclude)
        {
            SetItemChecked(store, exclude, false);
        }
    }

    private void SetItemChecked(TreeStore store, string path, bool isChecked)
    {
        if (store.GetIterFirst(out TreeIter iter))
        {
            SetItemChecked(store, iter, path, isChecked);
        }
    }

    private void SetItemChecked(TreeStore store, TreeIter iter, string path, bool isChecked)
    {
        string itemPath = (string)store.GetValue(iter, 2);
        if (itemPath == path)
        {
            store.SetValue(iter, 0, isChecked);
        }

        if (store.IterChildren(out TreeIter childIter, iter))
        {
            do
            {
                SetItemChecked(store, childIter, path, isChecked);
            } while (store.IterNext(ref childIter));
        }
    }
}
