using System.Diagnostics;
using System.IO;
using System.Reflection;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Pak.Objects;
public class UObject : NormalExport
{
    public NormalExport? Export;

    public static UObject CreateObject(NormalExport export)
    {
        UObject? instance = null;

        var className = export.GetClassTypeForAncestry(export.Asset, out _);
        var type = ObjectTypeRegistry.GetClass(className.ToString());
        if (type != null)
        {
            try
            {
                instance = Activator.CreateInstance(type) as UObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Construct typed object failed, falling back to base UObject: {ex}");
            }
        }

        var Ar = new AssetBinaryReader(new MemoryStream(export.Extras));
        instance ??= new UObject();
        instance.Asset = export.Asset;
        instance.Data = export.Data;
        instance.Export = export;
        instance.Deserialize(Ar);
        instance.Extras = Ar.ReadBytes((int)(Ar.BaseStream.Length - Ar.BaseStream.Position));
        return instance;
    }

    public virtual void Deserialize(AssetBinaryReader Ar)
    {

    }

    public virtual void Serialize(AssetBinaryWriter Aw)
    {

    }

    public List<PropertyData> GetFields()
    {
        var result = new List<PropertyData>();
        var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var prop in properties)
        {
            if (prop.DeclaringType == typeof(NormalExport) ||
                prop.DeclaringType == typeof(UObject)) continue;

            var fields = CreateFields(prop);
            if (fields != null) result.Add(fields);
        }

        return result;
    }

    public PropertyData? CreateFields(PropertyInfo property)
    {
        var value = property.GetValue(this);
        var fieldName = FName.DefineDummy(Asset, property.Name);

        return property.PropertyType switch
        {
            Type t when t == typeof(bool) => new BoolPropertyData(fieldName) { Value = (bool)value },
            Type t when t == typeof(int) => new IntPropertyData(fieldName) { Value = (int)value },
            Type t when t == typeof(float) => new FloatPropertyData(fieldName) { Value = (float)value },
            Type t when t == typeof(double) => new DoublePropertyData(fieldName) { Value = (double)value },
            Type t when t == typeof(string) => new StrPropertyData(fieldName) { Value = new FString((string)value) },
            Type t when t == typeof(FPackageIndex) => new ObjectPropertyData(fieldName) { Value = (FPackageIndex)value },
            Type t when t == typeof(FPackageIndex[]) => new ArrayPropertyData(fieldName)
            {
                ArrayType = FName.DefineDummy(Asset, "ObjectProperty"),
                Value = ((FPackageIndex[])value).Select(pi => new ObjectPropertyData(FName.DefineDummy(Asset, "")) { Value = pi }).ToArray()
            },
            _ => new StrPropertyData(fieldName) { Value = new FString(value?.ToString() ?? "null") }
        };
    }


    //public string GetFullName(UObject? stopOuter = null, bool includeClassPackage = false)
    //{
    //    var result = new StringBuilder(128);
    //    GetFullName(stopOuter, result, includeClassPackage);
    //    return result.ToString();
    //}

    //public void GetFullName(UObject? stopOuter, StringBuilder resultString, bool includeClassPackage = false)
    //{
    //    resultString.Append(includeClassPackage ? Class?.GetPathName() : ExportType);
    //    resultString.Append('\'');
    //    GetPathName(stopOuter, resultString);
    //    resultString.Append('\'');
    //}

    //public string GetPathName(UObject? stopOuter = null)
    //{
    //    var result = new StringBuilder();
    //    GetPathName(stopOuter, result);
    //    return result.ToString();
    //}

    //public void GetPathName(UObject? stopOuter, StringBuilder resultString)
    //{
    //    if (this != stopOuter)
    //    {
    //        var objOuter = Outer;
    //        if (objOuter != null && objOuter != stopOuter)
    //        {
    //            objOuter.GetPathName(stopOuter, resultString);
    //            // SUBOBJECT_DELIMITER_CHAR is used to indicate that this object's outer is not a UPackage
    //            resultString.Append(objOuter.Outer is IPackage ? ':' : '.');
    //        }

    //        resultString.Append(Name);
    //    }
    //    else
    //    {
    //        resultString.Append("None");
    //    }
    //}
}

internal static class ObjectTypeRegistry
{
    private static readonly Type _baseType = typeof(UObject);
    private static readonly Dictionary<string, Type> _classes = new(StringComparer.OrdinalIgnoreCase);

    static ObjectTypeRegistry()
    {
        RegisterEngine(_baseType.Assembly);
    }

    public static void RegisterEngine(Assembly assembly)
    {
        foreach (var definedType in assembly.DefinedTypes)
        {
            if (definedType.IsAbstract || definedType.IsInterface || !_baseType.IsAssignableFrom(definedType))
            {
                continue;
            }

            RegisterClass(definedType);
        }
    }

    public static void RegisterClass(Type type)
    {
        var name = type.Name;
        if ((name[0] == 'U' || name[0] == 'A') && char.IsUpper(name[1]))
            name = name[1..];
        RegisterClass(name, type);
    }

    public static void RegisterClass(string serializedName, Type type)
    {
        lock (_classes)
        {
            _classes[serializedName] = type;
        }
    }

    public static Type? GetClass(string serializedName)
    {
        lock (_classes)
        {
            if (!_classes.TryGetValue(serializedName, out var type) && serializedName.EndsWith("_C", StringComparison.OrdinalIgnoreCase))
            {
                _classes.TryGetValue(serializedName[..^2], out type);
            }
            return type;
        }
    }
}