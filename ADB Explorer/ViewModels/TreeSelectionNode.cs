using ADB_Explorer.Models;
using static ADB_Explorer.Models.AbstractFile;

namespace ADB_Explorer.ViewModels;

public class TreeSelectionNode : ViewModelBase
{
    private bool? isChecked = false;
    public bool? IsChecked
    {
        get => isChecked;
        set
        {
            // Prevent manual forcing of an indeterminate value from UI toggles.
            // Indeterminate should reflect child state only.
            if (!isUpdatingChildren && value is null)
            {
                UpdateFromChildren();
                return;
            }

            if (!Set(ref isChecked, value))
                return;

            if (!isUpdatingChildren)
            {
                SetChildrenChecked(value);
                Parent?.UpdateFromChildren();
            }
        }
    }

    public string Name { get; }
    public string FullPath { get; }
    public FileType Type { get; }
    public bool IsDirectory => Type is FileType.Folder;

    public ObservableCollection<TreeSelectionNode> Children { get; } = [];
    public TreeSelectionNode Parent { get; }

    private bool isLoaded = false;
    public bool IsLoaded
    {
        get => isLoaded;
        set => Set(ref isLoaded, value);
    }

    private bool isUpdatingChildren;

    public TreeSelectionNode(FileClass file, TreeSelectionNode parent = null)
    {
        Name = file.DisplayName;
        FullPath = file.FullPath;
        Type = file.Type;
        Parent = parent;
    }

    public void SetChildren(IEnumerable<TreeSelectionNode> children)
    {
        Children.Clear();
        foreach (var child in children)
        {
            Children.Add(child);
        }
    }

    private void SetChildrenChecked(bool? value)
    {
        if (value is null)
            return;

        isUpdatingChildren = true;
        foreach (var child in Children)
        {
            child.IsChecked = value;
        }
        isUpdatingChildren = false;
    }

    public void UpdateFromChildren()
    {
        if (Children.Count == 0)
            return;

        var allChecked = Children.All(c => c.IsChecked == true);
        var allUnchecked = Children.All(c => c.IsChecked == false);

        isUpdatingChildren = true;
        IsChecked = allChecked ? true : allUnchecked ? false : null;
        isUpdatingChildren = false;

        Parent?.UpdateFromChildren();
    }

    public IEnumerable<TreeSelectionNode> EnumerateCheckedLeavesAndFolders(bool hasCheckedAncestor = false)
    {
        var selfChecked = IsChecked == true;

        if (selfChecked && !hasCheckedAncestor)
        {
            yield return this;
            yield break;
        }

        foreach (var child in Children)
        {
            foreach (var selected in child.EnumerateCheckedLeavesAndFolders(hasCheckedAncestor || selfChecked))
            {
                yield return selected;
            }
        }
    }
}
