using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using UAssetManager.Models;

namespace UAssetManager.Utils;
public static class UAGUtils
{
    public static T TryGetElement<T>(this T[] array, int index)
    {
        if (array != null && index < array.Length)
        {
            return array[index];
        }
        return default(T);
    }

    public static object ArbitraryTryParse(this string input, Type type)
    {
        try
        {
            var converter = TypeDescriptor.GetConverter(type);
            if (converter != null)
            {
                return converter.ConvertFromString(input);
            }
        }
        catch (NotSupportedException) { }
        return null;
    }

    public static void InvokeUI(Action action)
    {
        if (Application.Current?.Dispatcher != null)
        {
            Application.Current.Dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }

    public static T[] StripNullsFromArray<T>(this T[] usArr)
    {
        int c = 0;
        for (int num = 0; num < usArr.Length; num++)
        {
            if (usArr[num] != null) c++;
        }

        var newData = new T[c];
        int indexAdded = 0;
        for (int num = 0; num < usArr.Length; num++)
        {
            if (usArr[num] != null) newData[indexAdded++] = usArr[num];
        }
        return newData;
    }

    public static List<T> StripNullsFromList<T>(this List<T> usList)
    {
        for (int num = 0; num < usList.Count; num++)
        {
            if (usList[num] == null)
            {
                usList.RemoveAt(num);
                num--;
            }
        }
        return usList;
    }

    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"打开URL时出错: {ex.Message}");
        }
    }

    public static void OpenFolder(string folderPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"打开文件夹时出错: {ex.Message}");
        }
    }

    public static void ExpandAllTreeViewItems(IEnumerable items)
    {
        foreach (TreeNodeItem item in items)
        {
            item.IsExpanded = true;
            ExpandAllTreeViewItems(item.Children);
        }
    }

    public static void CollapseAllTreeViewItems(IEnumerable items)
    {
        foreach (TreeNodeItem item in items)
        {
            item.IsExpanded = false;
            CollapseAllTreeViewItems(item.Children);
        }
    }
}