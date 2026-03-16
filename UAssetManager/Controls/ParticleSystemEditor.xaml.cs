using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Pak.Objects;
using UAssetAPI.Pak.Utils;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Controls;
public partial class ParticleSystemEditor
{
	#region Constructor

	private static ParticleSystemEditor? _instance;
	public static ParticleSystemEditor Instance => _instance ??= new ParticleSystemEditor();

	private ParticleSystemEditor()
	{
		InitializeComponent();
	}
	#endregion

	#region Methods
	public void Show(NormalExport current, UAsset asset)
	{
		var Emitters = current.Find<ArrayPropertyData>("Emitters")!;

		foreach (var emitterprop in Emitters.Value)
		{
			var emitter = emitterprop.GetObject<FPackageIndex>().ToExport(asset);

			var LodLevels = emitter.Find<ArrayPropertyData>("lodlevels");
			foreach (var loadlevelprop in LodLevels.Value)
			{
				var loadlevel = loadlevelprop.GetObject<FPackageIndex>().ToExport(asset);
				var Modules = loadlevel.Find<ArrayPropertyData>("modules");

				foreach (var moduleprop in Modules.Value)
				{
					var module = moduleprop.GetObject<FPackageIndex>().ToExport(asset);
					var t0 = UObject.CreateObject(module as NormalExport);
				}
			}



			//foreach (var emitter in emitters.Value)
			//{

			//}
		}
	}

	public static void Initialize(UAsset asset)
	{
		asset.Exports.Any(x => x.GetExportClassType().Value == "particlesystem");

		foreach (NormalExport item in asset.Exports)
		{
			var name = item.GetExportClassType().Value;
			//if (name == "bnsparticlemodulerequired")
			//{
			//	foreach (var p in item.Data)
			//	{
			//		if (p.Name.Value == "emitterduration" ||
			//			p.Name.Value == "emitterdelay")
			//		{
			//			var value = p.GetObject<float>();
			//			p.SetObject((float)(value * 0.3));
			//		}
			//	}
			//}

			if (name == "bnsparticlemodulelifetime")
			{
				foreach (var p in item.Data)
				{

				}
			}
		}
	}
	#endregion
}