# UObject系统使用说明

## 概述

UAssetManager现在支持通过UObject系统解析和编辑Unreal Engine资产中的非UPROPERTY类型字段。这个系统可以自动识别并构建UObject派生类（如UWorld）的树节点，让你能够查看和编辑这些字段。

## 架构说明

### 1. UObject基类

所有自定义的UObject类都应该继承自`UAssetManager.Pak.Objects.UObject`基类：

```csharp
public abstract class UObject
{
    public virtual void Deserialize(AssetBinaryReader Ar) { }
    public virtual void Serialize(AssetBinaryWriter Aw) { }
}
```

### 2. 自动注册机制

`ObjectTypeRegistry`会自动注册所有继承自`UObject`的类，命名规则：
- 如果类名以`U`或`A`开头且第二个字符是大写，则去掉第一个字符
- 例如：`UWorld` -> 注册为 `World`

### 3. UWorld示例

```csharp
public class UWorld : UObject
{
    public FPackageIndex PersistentLevel { get; private set; }
    public FPackageIndex[] ExtraReferencedObjects { get; private set; }
    public FPackageIndex[] StreamingLevels { get; private set; }

    public override void Deserialize(AssetBinaryReader Ar)
    {
        base.Deserialize(Ar);
        PersistentLevel = new FPackageIndex(Ar);
        ExtraReferencedObjects = Ar.ReadArray(() => new FPackageIndex(Ar));
        StreamingLevels = Ar.ReadArray(() => new FPackageIndex(Ar));
    }

    public override void Serialize(AssetBinaryWriter Aw)
    {
        base.Serialize(Aw);
        PersistentLevel.Write(Aw);
        Aw.WriteArray(ExtraReferencedObjects, (o) => o.Write(Aw));
        Aw.WriteArray(StreamingLevels, (o) => o.Write(Aw));
    }
}
```

## 树节点构建

### PointingTreeNodeItem自动构建

当`NormalExport.Extras`存在时，系统会：

1. **自动构造UObject**：
   ```csharp
   var obj = UObject.ConstructObject(normalExport, reader);
   ```

2. **构建树节点**：
   ```csharp
   var uobjectNode = BuildUObjectNodes(obj, normalExport);
   ```

3. **使用反射遍历属性**：
   - 支持`FPackageIndex`字段
   - 支持`FPackageIndex[]`数组
   - 支持其他`IList`集合
   - 支持基本类型

### 节点类型

- `TreeNodeType.UObjectData` - UObject根节点
- `TreeNodeType.UObjectField` - UObject字段节点

## 字段编辑

### 直接复用现有编辑器

UObject字段现在直接使用标准的PropertyData和现有编辑器：

1. **GetPropertyData()** - 获取标准PropertyData
2. **SetPropertyValue()** - 设置属性值
3. **GetAllPropertyData()** - 获取所有属性的PropertyData列表

### 属性方法

```csharp
public abstract class UObject
{
    /// <summary>
    /// 获取指定属性的PropertyData，用于绑定到现有编辑器
    /// </summary>
    public PropertyData? GetPropertyData(string propertyName)
    
    /// <summary>
    /// 设置指定属性的值
    /// </summary>
    public bool SetPropertyValue(string propertyName, object value)
    
    /// <summary>
    /// 获取所有可编辑属性的PropertyData列表
    /// </summary>
    public List<PropertyData> GetAllPropertyData()
}
```

### 自动编辑器选择

PropertyItemsControl会自动为UObject字段选择合适的现有编辑器：

- `BoolPropertyData` → `BoolPropertyEditor`
- `IntPropertyData` → `IntPropertyEditor`
- `FloatPropertyData` → `FloatPropertyEditor`
- `ObjectPropertyData` → `ObjectPropertyEditor`
- `ArrayPropertyData` → `ArrayPropertyEditor`
- 等等...

## 使用示例

### 1. 创建新的UObject类

```csharp
namespace UAssetManager.Pak.Objects.Engine
{
    public class ULevelStreaming : UObject
    {
        public FPackageIndex WorldAsset { get; private set; }
        public bool bShouldBeVisible { get; private set; }
        public bool bShouldBeLoaded { get; private set; }

        public override void Deserialize(AssetBinaryReader Ar)
        {
            base.Deserialize(Ar);
            WorldAsset = new FPackageIndex(Ar);
            bShouldBeVisible = Ar.ReadBoolean();
            bShouldBeLoaded = Ar.ReadBoolean();
        }

        public override void Serialize(AssetBinaryWriter Aw)
        {
            base.Serialize(Aw);
            WorldAsset.Write(Aw);
            Aw.Write(bShouldBeVisible);
            Aw.Write(bShouldBeLoaded);
        }
    }
}
```

### 2. 在UI中查看

1. 加载包含UWorld的资产
2. 在树视图中展开Export节点
3. 找到"UObject Data (UWorld)"节点
4. 展开查看字段：
   - PersistentLevel: 1234
   - StreamingLevels (5)
     - [0]: 5678
     - [1]: 5679
     - ...

### 3. 编辑字段

现在UObject字段直接使用现有的PropertyEditor系统：

1. **在树视图中选择UObject字段节点**
2. **PropertyEditor会自动显示对应的编辑器**：
   - `bool` → 复选框编辑器
   - `int` → 数字输入框
   - `float/double` → 浮点数输入框
   - `string` → 文本输入框
   - `FPackageIndex` → 对象引用编辑器
   - `FPackageIndex[]` → 数组编辑器

3. **编辑支持**：
   - ✅ 完全支持所有现有编辑器的功能
   - ✅ 自动类型转换和验证
   - ✅ 支持撤销/重做
   - ✅ 支持复制/粘贴
   - ✅ 支持键盘导航

### 4. 数据绑定

UObject属性通过标准PropertyData自动绑定到编辑器：

```csharp
// 获取属性数据
var propertyData = uWorld.GetPropertyData("StreamingLevels");

// 设置属性值
uWorld.SetPropertyValue("StreamingLevels", newValue);

// 获取所有属性
var allProperties = uWorld.GetAllPropertyData();
```

## PPV季节性场景控制

使用UWorld对象控制季节性PPV：

```csharp
// 获取UWorld对象
if (treeNode.Data is UWorld world)
{
    // 获取所有LevelStreamingDynamic
    var levelStreamings = world.GetLevelStreamingDynamics(asset);
    
    // 设置可见性
    world.SetStreamingLevelVisibility(0, true, asset);
}
```

## 注意事项

1. **属性必须是public**：只有public的属性才会被反射识别
2. **支持setter**：如果需要编辑，属性必须有setter（可以是private set）
3. **类型限制**：目前支持FPackageIndex、数组、IList和基本类型
4. **序列化顺序**：Deserialize和Serialize中的字段顺序必须一致

## 扩展建议

如果需要添加更多UObject类型：

1. 在`UAssetManager/Pak/Objects/Engine/`目录下创建新文件
2. 继承`UObject`基类
3. 实现`Deserialize`和`Serialize`方法
4. 系统会自动注册并支持树节点构建

