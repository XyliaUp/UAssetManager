using System.IO;
using System.Runtime.CompilerServices;

namespace UAssetManager.Utils;
public static class BinaryExtension
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Read<T>(this BinaryReader reader)
	{
		var size = Unsafe.SizeOf<T>();
		var buffer = reader.ReadBytes(size);
		return Unsafe.ReadUnaligned<T>(ref buffer[0]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Write<T>(this BinaryWriter writer, T value) where T : struct
	{
		var size = Unsafe.SizeOf<T>();
		var data = new byte[size];

		unsafe
		{
			fixed (byte* p = &Unsafe.As<T, byte>(ref value))
			{
				using var ms = new UnmanagedMemoryStream(p, size);
				ms.Read(data, 0, data.Length);
			}
		}

		writer.Write(data);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static async Task SaveAsync(this Stream stream, string path)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);

		await using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
		stream.Seek(0, SeekOrigin.Begin);
		await stream.CopyToAsync(fs);
		await fs.FlushAsync();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GetReadableSize(double size)
	{
		if (size == 0) return "0 B";

		string[] sizes = ["B", "KB", "MB", "GB", "TB"];
		var order = 0;
		while (size >= 1024 && order < sizes.Length - 1)
		{
			order++;
			size /= 1024;
		}

		return $"{size:# ###.##} {sizes[order]}".TrimStart();
	}
}