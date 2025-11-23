using UAssetAPI;

namespace UAssetManager.Models;
public class SerializationFlagItem
{
    public string Name { get; set; } = string.Empty;

    public CustomSerializationFlags Flag { get; set; }

    public bool IsChecked { get; set; }
}