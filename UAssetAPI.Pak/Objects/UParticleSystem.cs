namespace UAssetAPI.Pak.Objects;

public class UParticleSystem : UObject
{


}

public class UParticleSpriteEmitter : UObject
{


}

public class UParticleLODLevel : UObject
{


}


public abstract class UParticleModule : UObject
{
	public float LODValidity;
}

public class UParticleModuleRequired : UParticleModule
{
	public float EmitterDelay;
	public float EmitterDuration;
	public int EmitterLoops;
}

public class UParticleModuleAccelerationOverlifetime : UParticleModule
{

}

public class UParticleModuleBeamSource : UParticleModule
{

}

public class UParticleModuleBeamTarget : UParticleModule
{

}

public class UParticleModuleColor : UParticleModule
{
	//StartColor;
	//StartAlpha;
}

public class UParticleModuleColorOverlife : UParticleModule
{

}

public class UParticleModuleColorScaleOverlife : UParticleModule
{
	//ColorScaleOverlife;
	//AlphaScaleOverlife;
}

public class UParticleModuleDataDecal : UParticleModule
{
	public bool bNorotation;
}

public class UParticleModuleLifeTime : UParticleModule
{
	//LifeTime
}

public class UParticleModuleLocation : UParticleModule
{


}

public class UParticleModuleLocationEmitter : UParticleModule
{

}

public class UParticleModuleLocationPrimitiveCylinder : UParticleModule
{

}


public class UParticleModuleLocationPrimitiveSphere : UParticleModule
{

}

public class UParticleModuleMeshMaterial : UParticleModule
{


}

public class UParticleModuleMeshRotation : UParticleModule
{


}

public class UParticleModuleOrbit : UParticleModule
{

}

public class UParticleModuleParameterDynamic : UParticleModule
{

}

public class UParticleModuleParameterMeshDynamicParam : UParticleModule
{

}

public class UParticleModuleRotation : UParticleModule
{

}

public class UParticleModuleRotationRate : UParticleModule
{

}

public class UParticleModuleSize : UParticleModule
{


}

public class UParticleModuleSizeScale : UParticleModule
{


}

public class UParticleModuleSizeMultiplyLife : UParticleModule
{

}

public class UParticleModuleSizeMultiplyVelocity : UParticleModule
{

}

public class UParticleModuleSpawn : UParticleModule
{


}

public class UParticleModuleSubUV : UParticleModule
{

}

public class UParticleModuleTypedataDecal : UParticleModule
{


}

public class UParticleModuleTypedataMesh : UParticleModule
{


}

public class UParticleModuleVelocity : UParticleModule
{


}
