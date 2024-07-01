using System.Collections.Generic;
using System.IO;
using Gtk;

namespace CodeAggregatorGtk
{
    public class TreeViewHandler
    {
        private readonly TreeView folderTreeView;
        private readonly string sourceFolder;

        public TreeViewHandler(TreeView treeView, string source)
        {
            folderTreeView = treeView;
            sourceFolder = source;
        }

        public void PopulateTreeView(List<string> selectedFiles)
        {
            if (!Directory.Exists(sourceFolder))
            {
                Console.WriteLine($"Directory not found: {sourceFolder}");
                return;
            }

            var store = new TreeStore(typeof(bool), typeof(string), typeof(string));
            folderTreeView.Model = store;

            if (folderTreeView.Columns.Length == 0)
            {
                var toggleRenderer = new CellRendererToggle();
                toggleRenderer.Toggled += OnToggled;

                folderTreeView.AppendColumn("Include", toggleRenderer, "active", 0);
                folderTreeView.AppendColumn("Name", new CellRendererText(), "text", 1);
            }

            PopulateTreeView(store, sourceFolder, null, selectedFiles);
        }

        private void PopulateTreeView(TreeStore store, string path, TreeIter? parent, List<string> selectedFiles)
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                TreeIter iter;
                var relativePath = Path.GetRelativePath(sourceFolder, dir);
                bool isSelected = selectedFiles.Contains(relativePath);

                if (parent.HasValue)
                {
                    iter = store.AppendValues(parent.Value, isSelected, Path.GetFileName(dir), dir);
                }
                else
                {
                    iter = store.AppendValues(isSelected, Path.GetFileName(dir), dir);
                }
                PopulateTreeView(store, dir, iter, selectedFiles);
            }
            foreach (var file in Directory.GetFiles(path))
            {
                var relativePath = Path.GetRelativePath(sourceFolder, file);
                bool isSelected = selectedFiles.Contains(relativePath);

                if (parent.HasValue)
                {
                    store.AppendValues(parent.Value, isSelected, Path.GetFileName(file), file);
                }
                else
                {
                    store.AppendValues(isSelected, Path.GetFileName(file), file);
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

        public void CollectSelectedItems(TreeStore store, List<string> include, List<string> exclude)
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
            string relativePath = Path.GetRelativePath(sourceFolder, path);

            if (isSelected)
            {
                include.Add(relativePath);
            }
            else
            {
                exclude.Add(relativePath);
            }

            if (store.IterChildren(out TreeIter childIter, iter))
            {
                do
                {
                    CollectSelectedItems(store, childIter, include, exclude);
                } while (store.IterNext(ref childIter));
            }
        }

        public bool AnyFileSelected()
        {
            if (folderTreeView.Model is TreeStore store)
            {
                if (store.GetIterFirst(out TreeIter iter))
                {
                    return CheckAnyFileSelected(store, iter);
                }
            }
            return false;
        }

        private bool CheckAnyFileSelected(TreeStore store, TreeIter iter)
        {
            do
            {
                bool isSelected = (bool)store.GetValue(iter, 0);
                if (isSelected)
                {
                    return true;
                }

                if (store.IterChildren(out TreeIter childIter, iter))
                {
                    if (CheckAnyFileSelected(store, childIter))
                    {
                        return true;
                    }
                }
            } while (store.IterNext(ref iter));

            return false;
        }

        public List<string> GetSelectedFiles()
        {
            var selectedFiles = new List<string>();
            if (folderTreeView.Model is TreeStore store)
            {
                if (store.GetIterFirst(out TreeIter iter))
                {
                    CollectSelectedFiles(store, iter, selectedFiles);
                }
            }
            return selectedFiles;
        }

        private void CollectSelectedFiles(TreeStore store, TreeIter iter, List<string> selectedFiles)
        {
            do
            {
                bool isSelected = (bool)store.GetValue(iter, 0);
                string path = (string)store.GetValue(iter, 2);
                string relativePath = Path.GetRelativePath(sourceFolder, path);

                if (isSelected)
                {
                    selectedFiles.Add(relativePath);
                }

                if (store.IterChildren(out TreeIter childIter, iter))
                {
                    CollectSelectedFiles(store, childIter, selectedFiles);
                }
            } while (store.IterNext(ref iter));
        }
    }
}
