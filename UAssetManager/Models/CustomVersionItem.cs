using CommunityToolkit.Mvvm.ComponentModel;

namespace UAssetManager.Models;
public class CustomVersionItem : ObservableObject
{
	private string _name = string.Empty;
	public string Name
	{
		get => _name;
		set
		{
			_name = value;
			OnPropertyChanged();
		}
	}

	private int _version;
	public int Version
	{
		get => _version;
		set
		{
			_version = value;
			OnPropertyChanged();
		}
	}
}