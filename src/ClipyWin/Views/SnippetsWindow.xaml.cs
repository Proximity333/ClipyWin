using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using ClipyWin.Environments;
using ClipyWin.Models;
using ClipyWin.Storage;
using ClipyWin.Utilities;

namespace ClipyWin.Views;

public partial class SnippetsWindow : Window
{
    private static SnippetsWindow? _instance;

    private readonly ClipyDb _db;
    private bool _suppressEditorSync;

    public SnippetsWindow()
    {
        InitializeComponent();
        ApplyLocalization();
        _db = AppEnvironment.Current.Db;
        Loaded += (_, _) => Reload(selectFirst: true);
    }

    private void ApplyLocalization()
    {
        Title = Loc.T("window.snippets");
        AddFolderBtn.Content = "+ " + Loc.T("snippets.addFolder");
        AddSnippetBtn.Content = "+ " + Loc.T("snippets.addSnippet");
        DeleteBtn.Content = Loc.T("snippets.delete");
        TitleFieldLabel.Text = Loc.T("snippets.title.field");
        ImportBtn.Content = Loc.T("snippets.import");
        ExportBtn.Content = Loc.T("snippets.export");
        CloseBtn.Content = Loc.T("prefs.close");
    }

    public static void ShowSingleton()
    {
        if (_instance == null || !_instance.IsLoaded)
        {
            _instance = new SnippetsWindow();
            _instance.Closed += (_, _) => _instance = null;
            _instance.Show();
        }
        else
        {
            if (_instance.WindowState == WindowState.Minimized) _instance.WindowState = WindowState.Normal;
            _instance.Activate();
        }
    }

    public static void RefreshLocalization()
    {
        if (_instance == null || !_instance.IsLoaded) return;
        _instance.ApplyLocalization();
        _instance.Reload();
    }

    private void Reload(bool selectFirst = false)
    {
        Tree.Items.Clear();
        var folders = _db.Folders.FindAll().OrderBy(f => f.Index).ToList();
        TreeViewItem? first = null;
        foreach (var folder in folders)
        {
            var folderNode = new TreeViewItem
            {
                Header = string.IsNullOrWhiteSpace(folder.Title) ? Loc.T("snippets.untitled") : folder.Title,
                Tag = folder,
                IsExpanded = true
            };
            foreach (var snippet in folder.Snippets.OrderBy(s => s.Index))
            {
                var snippetNode = new TreeViewItem
                {
                    Header = string.IsNullOrWhiteSpace(snippet.Title) ? Loc.T("snippets.untitled") : snippet.Title,
                    Tag = snippet
                };
                folderNode.Items.Add(snippetNode);
            }
            Tree.Items.Add(folderNode);
            first ??= folderNode;
        }

        if (selectFirst && first != null)
        {
            first.IsSelected = true;
        }
        else
        {
            UpdateEditor(null);
        }
    }

    private object? _selected;

    private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        _selected = (e.NewValue as TreeViewItem)?.Tag;
        UpdateEditor(_selected);
    }

    private void UpdateEditor(object? selected)
    {
        _suppressEditorSync = true;
        try
        {
            switch (selected)
            {
                case Folder f:
                    TitleBox.Text = f.Title;
                    ContentBox.Text = string.Empty;
                    ContentBox.IsEnabled = false;
                    AddSnippetBtn.IsEnabled = true;
                    DeleteBtn.IsEnabled = true;
                    break;
                case Snippet s:
                    TitleBox.Text = s.Title;
                    ContentBox.Text = s.Content;
                    ContentBox.IsEnabled = true;
                    AddSnippetBtn.IsEnabled = false;
                    DeleteBtn.IsEnabled = true;
                    break;
                default:
                    TitleBox.Text = string.Empty;
                    ContentBox.Text = string.Empty;
                    ContentBox.IsEnabled = false;
                    AddSnippetBtn.IsEnabled = false;
                    DeleteBtn.IsEnabled = false;
                    break;
            }
        }
        finally { _suppressEditorSync = false; }
    }

    private void Title_Changed(object sender, TextChangedEventArgs e)
    {
        if (_suppressEditorSync) return;
        var tvi = Tree.SelectedItem as TreeViewItem;
        if (tvi == null) return;

        switch (tvi.Tag)
        {
            case Folder f:
                f.Title = TitleBox.Text;
                tvi.Header = string.IsNullOrWhiteSpace(f.Title) ? Loc.T("snippets.untitled") : f.Title;
                _db.Folders.Update(f);
                break;
            case Snippet s:
                s.Title = TitleBox.Text;
                tvi.Header = string.IsNullOrWhiteSpace(s.Title) ? Loc.T("snippets.untitled") : s.Title;
                SaveParentFolderOf(tvi);
                break;
        }
    }

    private void Content_Changed(object sender, TextChangedEventArgs e)
    {
        if (_suppressEditorSync) return;
        var tvi = Tree.SelectedItem as TreeViewItem;
        if (tvi?.Tag is not Snippet s) return;
        s.Content = ContentBox.Text;
        SaveParentFolderOf(tvi);
    }

    private void SaveParentFolderOf(TreeViewItem snippetNode)
    {
        var parent = ItemsControl.ItemsControlFromItemContainer(snippetNode) as TreeViewItem;
        if (parent?.Tag is Folder f) _db.Folders.Update(f);
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = new Folder
        {
            Title = Loc.T("snippets.newFolder"),
            Index = Tree.Items.Count
        };
        _db.Folders.Insert(folder);
        Reload();
        SelectFolder(folder);
    }

    private void AddSnippet_Click(object sender, RoutedEventArgs e)
    {
        if (Tree.SelectedItem is not TreeViewItem tvi) return;
        if (tvi.Tag is not Folder folder) return;

        var snippet = new Snippet
        {
            Title = Loc.T("snippets.newSnippet"),
            Content = string.Empty,
            Index = folder.Snippets.Count
        };
        folder.Snippets.Add(snippet);
        _db.Folders.Update(folder);

        Reload();
        SelectSnippet(folder, snippet);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Tree.SelectedItem is not TreeViewItem tvi) return;

        switch (tvi.Tag)
        {
            case Folder f:
                if (MessageBox.Show(string.Format(Loc.T("snippets.deleteFolderConfirm"), f.Title), "Clipy", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
                _db.Folders.Delete(f.Identifier);
                break;
            case Snippet s:
                var parent = ItemsControl.ItemsControlFromItemContainer(tvi) as TreeViewItem;
                if (parent?.Tag is not Folder pf) return;
                pf.Snippets.RemoveAll(x => x.Identifier == s.Identifier);
                _db.Folders.Update(pf);
                break;
        }

        Reload();
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Clipy snippet XML (*.xml)|*.xml|All files (*.*)|*.*",
            Title = Loc.T("snippets.importTitle")
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var xml = XDocument.Load(dlg.FileName);
            var root = xml.Root ?? throw new InvalidOperationException("Empty XML");
            int folderIndex = _db.Folders.Count();
            foreach (var fElem in root.Elements("folder"))
            {
                var folder = new Folder
                {
                    Title = (string?)fElem.Element("title") ?? "Imported",
                    Index = folderIndex++,
                    Enable = ((string?)fElem.Element("enabled") ?? "true") != "false"
                };
                int snippetIndex = 0;
                foreach (var sElem in fElem.Elements("snippet"))
                {
                    folder.Snippets.Add(new Snippet
                    {
                        Title = (string?)sElem.Element("title") ?? string.Empty,
                        Content = (string?)sElem.Element("content") ?? string.Empty,
                        Enable = ((string?)sElem.Element("enabled") ?? "true") != "false",
                        Index = snippetIndex++
                    });
                }
                _db.Folders.Insert(folder);
            }
            Reload();
             MessageBox.Show(Loc.T("snippets.importDone"), "Clipy", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(Loc.T("snippets.importFailed"), ex.Message), "Clipy", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Clipy snippet XML (*.xml)|*.xml",
            Title = Loc.T("snippets.exportTitle"),
            FileName = Loc.T("snippets.exportFileName")
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var root = new XElement("snippets");
            foreach (var f in _db.Folders.FindAll().OrderBy(x => x.Index))
            {
                var fe = new XElement("folder",
                    new XElement("title", f.Title),
                    new XElement("enabled", f.Enable.ToString().ToLowerInvariant()));
                foreach (var s in f.Snippets.OrderBy(x => x.Index))
                {
                    fe.Add(new XElement("snippet",
                        new XElement("title", s.Title),
                        new XElement("enabled", s.Enable.ToString().ToLowerInvariant()),
                        new XElement("content", s.Content)));
                }
                root.Add(fe);
            }
            new XDocument(root).Save(dlg.FileName);
            MessageBox.Show(Loc.T("snippets.exportDone"), "Clipy", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(Loc.T("snippets.exportFailed"), ex.Message), "Clipy", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SelectFolder(Folder target)
    {
        foreach (TreeViewItem node in Tree.Items)
        {
            if (node.Tag is Folder f && f.Identifier == target.Identifier)
            {
                node.IsSelected = true;
                return;
            }
        }
    }

    private void SelectSnippet(Folder folder, Snippet target)
    {
        foreach (TreeViewItem parent in Tree.Items)
        {
            if (parent.Tag is Folder f && f.Identifier == folder.Identifier)
            {
                foreach (TreeViewItem child in parent.Items)
                {
                    if (child.Tag is Snippet s && s.Identifier == target.Identifier)
                    {
                        parent.IsExpanded = true;
                        child.IsSelected = true;
                        return;
                    }
                }
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
