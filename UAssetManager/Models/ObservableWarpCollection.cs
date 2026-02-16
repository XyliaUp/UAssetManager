using System.Collections.ObjectModel;

namespace UAssetManager.Models;
public class ObservableWarpCollection<T> : ObservableCollection<WarpItem<T>>
{
	public void Add(T item) => base.Add(new WarpItem<T>(item));

	public void Remove(T item)
	{
		var warpItem = new WarpItem<T>(item);
		if (Contains(warpItem)) base.Remove(warpItem);
	}
}

public record class WarpItem<T>(T Value)
{
	public static implicit operator WarpItem<T>(T value) => new(value);
	public static implicit operator T(WarpItem<T> item) => item.Value;
}