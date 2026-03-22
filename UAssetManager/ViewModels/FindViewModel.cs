using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using UAssetManager.Models;
using UAssetManager.Resources;

namespace UAssetManager.ViewModels;

internal partial class FindViewModel : ObservableObject
{
	#region Fields
	public event EventHandler? CloseRequest;

	private ITreeSearchProvider? _host;
	private CancellationTokenSource? _cts;
	#endregion

	#region Properties
	[ObservableProperty] string _searchTerm = string.Empty;
	[ObservableProperty] bool _caseSensitive = false;
	[ObservableProperty] bool _useRegex = false;
	[ObservableProperty] bool _isForward = false;
	[ObservableProperty] bool _isSearching = false;
	[ObservableProperty] bool _isCancelling = false;
	[ObservableProperty] ObservableCollection<object> _searchResults = new();
	[ObservableProperty] object? _selectedResult;
	#endregion

	#region Methods

	[RelayCommand]
	void Close()
	{
		CloseRequest?.Invoke(this, EventArgs.Empty);
	}

	[RelayCommand]
	void Cancel()
	{
		if (_cts == null) return;
		IsCancelling = true;
		_cts.Cancel();
	}

	[RelayCommand]
	void FindAll()
	{
		ArgumentNullException.ThrowIfNull(_host);
		if (string.IsNullOrWhiteSpace(SearchTerm))
		{
			MessageBox.Show(StringHelper.Get("FindWindow_PleaseEnterSearchContent"), StringHelper.Get("Text.Information"), MessageBoxButton.OK, MessageBoxImage.Information);
			return;
		}

		try
		{
			IsSearching = true;
			IsCancelling = false;
			_cts?.Cancel();
			_cts = new CancellationTokenSource();

			Task.Run(() =>
			{
				_host.ClearHighlights();
				var results = _host.FindAll(BuildPredicate(), _cts.Token);

				Application.Current?.Dispatcher.Invoke(() =>
				{
					SearchResults.Clear();
					foreach (var result in results) SearchResults.Add(result);
				});
			});
		}
		catch (Exception ex)
		{
			MessageBox.Show(StringHelper.Get("FindWindow_SearchError", ex.Message), StringHelper.Get("Text.Error"), MessageBoxButton.OK, MessageBoxImage.Information);
		}
		finally
		{
			IsSearching = false;
			IsCancelling = false;
		}
	}

	public void Initialize(ITreeSearchProvider host)
	{
		_host = host;
	}

	partial void OnSelectedResultChanged(object? value)
	{
		if (value is null) return;

		Application.Current.Dispatcher.Invoke(() => _host?.SelectObject(value));
		if (UAGConfig.Data.EnableCloseDialog) CloseRequest?.Invoke(this, EventArgs.Empty);
	}

	public void ClearHighlights()
	{
		_host?.ClearHighlights();
	}

	[RelayCommand]
	private void PerformSearch()
	{
		ArgumentNullException.ThrowIfNull(_host);
		if (string.IsNullOrWhiteSpace(SearchTerm))
		{
			MessageBox.Show(StringHelper.Get("FindWindow_PleaseEnterSearchContent"), StringHelper.Get("Text.Information"), MessageBoxButton.OK, MessageBoxImage.Information);
			return;
		}

		try
		{
			IsSearching = true;
			IsCancelling = false;
			_cts?.Cancel();
			_cts = new CancellationTokenSource();

			Task.Run(() =>
			{
				var found = _host.FindNext(BuildPredicate(), IsForward, _cts.Token, out var selected);
				if (found)
				{
					Application.Current.Dispatcher.Invoke(() => _host.SelectObject(selected));
					return;
				}

				if (!found) MessageBox.Show(StringHelper.Get("FindWindow_NoResultsFound"), StringHelper.Get("Text.Information"), MessageBoxButton.OK, MessageBoxImage.Information);
			});
		}
		catch (Exception ex)
		{
			MessageBox.Show(StringHelper.Get("FindWindow_SearchError", ex.Message), StringHelper.Get("Text.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
		}
		finally
		{
			IsSearching = false;
			IsCancelling = false;
		}
	}

	private Func<object, bool> BuildPredicate()
	{
		string term = SearchTerm;
		bool caseSensitive = CaseSensitive;
		bool useRegex = UseRegex;
		if (useRegex)
		{
			var options = RegexOptions.Compiled | RegexOptions.Multiline;
			if (!caseSensitive) options |= RegexOptions.IgnoreCase;
			var regex = new Regex(term, options);
			return (obj) =>
			{
				if (obj is TreeNodeItem n) return regex.IsMatch(n.ToString());
				var s = obj?.ToString() ?? string.Empty;
				return regex.IsMatch(s);
			};
		}
		var cmp = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
		return (obj) =>
		{
			var s = obj?.ToString() ?? string.Empty;
			return !string.IsNullOrEmpty(s) && s.Contains(term, cmp);
		};
	}

	#endregion
}

public interface ITreeSearchProvider
{
	bool FindNext(Func<object, bool> predicate, bool isForward, CancellationToken token, out object? selected);

	IEnumerable<object> FindAll(Func<object, bool> predicate, CancellationToken token);

	void ClearHighlights();

	void SelectObject(object? found);
}