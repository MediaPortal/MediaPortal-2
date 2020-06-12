using Emulators.GoodMerge;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Emulators.Models
{
  public class GoodMergeSelectModel
  {
    public static Guid MODEL_ID = new Guid("E5D445E1-1F1E-4604-90CC-A63B098A5FFE");
    protected const string DIALOG_GOODMERGE_SELECT = "dialog_goodmerge_select";
    protected const string DIALOG_GOODMERGE_EXTRACT = "dialog_goodmerge_extract";
    protected const string KEY_ARCHIVE_FILE = "GOODMERGE_ARCHIVE_FILE";

    protected AbstractProperty _progressProperty = new WProperty(typeof(int), 0);
    protected ItemsList _items = new ItemsList();
    protected string _selectedItem;
    protected GoodMergeExtractor _goodMergeExtractor;
    protected Guid? _extractionDialogHandle;

    public static GoodMergeSelectModel Instance()
    {
      return (GoodMergeSelectModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(MODEL_ID);
    }

    public ItemsList Items
    {
      get { return _items; }
    }

    public AbstractProperty ProgressProperty
    {
      get { return _progressProperty; }
    }

    public int Progress
    {
      get { return (int)_progressProperty.GetValue(); }
      set { _progressProperty.SetValue(value); }
    }

    public void Extract(ILocalFsResourceAccessor accessor, IEnumerable<string> archiveItems, string selectedItem, Action<ExtractionCompletedEventArgs> completedDlgt)
    {
      _selectedItem = selectedItem;
      if (string.IsNullOrEmpty(selectedItem))
      {
        CreateListItems(archiveItems, accessor.LocalFileSystemPath);
        ServiceRegistration.Get<IScreenManager>().ShowDialog(DIALOG_GOODMERGE_SELECT, (a, b) => Extract(accessor, _selectedItem, completedDlgt));
      }
      else
      {
        Extract(accessor, selectedItem, completedDlgt);
      }
    }

    public void SelectItem(string archivePath, IEnumerable<string> archiveItems, string selectedItem, Action<string> completedDlgt)
    {
      CreateListItems(archiveItems, archivePath);
      ServiceRegistration.Get<IScreenManager>().ShowDialog(DIALOG_GOODMERGE_SELECT, (a, b) =>
      {
        if (completedDlgt != null)
          completedDlgt(_selectedItem);
      });
    }

    public void Select(ListItem item)
    {
      object selectedItemOb;
      if (item == null || !item.AdditionalProperties.TryGetValue(KEY_ARCHIVE_FILE, out selectedItemOb))
        return;
      _selectedItem = selectedItemOb as string;
    }

    protected void CreateListItems(IEnumerable<string> archiveFiles, string archivePath)
    {
      _items.Clear();
      if (archiveFiles != null)
      {
        List<string> sortedFiles = new List<string>(archiveFiles);
        sortedFiles.Sort();
        string prefix = DosPathHelper.GetFileNameWithoutExtension(archivePath);
        foreach (string file in sortedFiles)
        {
          string name = GetDisplayName(file, prefix);
          ListItem item = new ListItem(Consts.KEY_NAME, name);
          item.AdditionalProperties[KEY_ARCHIVE_FILE] = file;
          _items.Add(item);
        }
      }
      _items.FireChange();
    }

    protected void Extract(ILocalFsResourceAccessor accessor, string selectedItem, Action<ExtractionCompletedEventArgs> completedDlgt)
    {
      string extractedPath = null;
      if (string.IsNullOrEmpty(selectedItem) || GoodMergeExtractor.IsExtracted(accessor, selectedItem, out extractedPath))
      {
        if (completedDlgt != null)
          completedDlgt(new ExtractionCompletedEventArgs(selectedItem, extractedPath, !string.IsNullOrEmpty(selectedItem)));
        return;
      }

      Progress = 0;
      _goodMergeExtractor = new GoodMergeExtractor();
      _goodMergeExtractor.ExtractionProgress += (o, e) => Progress = e.Percent;
      _goodMergeExtractor.ExtractionCompleted += (o, e) =>
      {
        CloseDialog();
        if (completedDlgt != null)
          completedDlgt(e);
      };

      _extractionDialogHandle = ServiceRegistration.Get<IScreenManager>().ShowDialog(DIALOG_GOODMERGE_EXTRACT);
      _goodMergeExtractor.Extract(accessor, selectedItem);
    }

    protected void CloseDialog()
    {
      Guid? dialogHandle = _extractionDialogHandle;
      if (dialogHandle.HasValue)
        ServiceRegistration.Get<IScreenManager>().CloseDialog(dialogHandle.Value);
    }

    protected string GetDisplayName(string fileName, string baseFileName)
    {
      if (string.IsNullOrEmpty(baseFileName) || fileName == baseFileName || !fileName.StartsWith(baseFileName))
        return fileName;
      return fileName.Substring(baseFileName.Length);
    }
  }
}