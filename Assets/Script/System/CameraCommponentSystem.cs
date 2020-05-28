using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
public class CameraCommponentSystem : ComponentSystem
{
    private EntityQuery localPlayer;
    protected override void OnCreate()
    {
        localPlayer = GetEntityQuery(typeof(Translation), typeof(InputCommandData)); 
    }

    protected override void OnUpdate()
    { 
        Entities.ForEach((ref Translation camPosition, ref Rotation camRotation, ref CameraComponent c2) =>
        {
            var targetPos = localPlayer.ToComponentDataArray<Translation>(Allocator.TempJob);
            var targetImput = GetBufferFromEntity<InputCommandData>(true);
            var targetEntitie = localPlayer.ToEntityArray(Allocator.TempJob);
            if(targetPos.Length >= 1)
            {
                float3 lookVector = targetPos[0].Value - camPosition.Value;
                Quaternion rotation = Quaternion.LookRotation(lookVector);
                camRotation.Value = rotation;
            }
            targetPos.Dispose();
            targetEntitie.Dispose();
        });
    }
}