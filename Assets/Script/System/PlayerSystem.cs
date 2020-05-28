using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
public class InputComponentSystem : ComponentSystem
{
    private float localAngleV = 0f;
    private float localAngleH = 0f;
    private float defaultspeed = 2.0f;

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<NetworkIdComponent>();
        RequireSingletonForUpdate<EnablewhalesGhostReceiveSystemComponent>();
    }

    protected override void OnUpdate()
    {
        var localInput = GetSingleton<CommandTargetComponent>().targetEntity;
        if (localInput == Entity.Null)
        {
            var localPlayerId = GetSingleton<NetworkIdComponent>().Value;
            Entities.WithNone<InputCommandData>().ForEach((Entity ent, ref PlayerComponent player) =>
            {
                if (player.PlayerId == localPlayerId)
                {
                    PostUpdateCommands.AddBuffer<InputCommandData>(ent);
                    PostUpdateCommands.SetComponent(GetSingletonEntity<CommandTargetComponent>(), new CommandTargetComponent {targetEntity = ent});
                }
            });
            return;
        }
        
        var input = default(InputCommandData);
        input.tick = World.GetExistingSystem<ClientSimulationSystemGroup>().ServerTick;
        localAngleH += Input.GetAxis("Horizontal");
        localAngleV -= Input.GetAxis("Vertical");

        input.angleH = localAngleH;
        input.angleV = localAngleV;
        input.speed = defaultspeed;

        var inputBuffer = EntityManager.GetBuffer<InputCommandData>(localInput);
        inputBuffer.AddCommandData(input);
    }
}

[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
public class PlayerSystem : ComponentSystem
{
    private float3 front;

    protected override void OnCreate()
    {
        // 向いてる方向
        front = new float3 (0, 0, 1);
    }

    protected override void OnUpdate()
    {
        var group = World.GetExistingSystem<GhostPredictionSystemGroup>();
        var tick = group.PredictingTick;
        var deltaTime = Time.DeltaTime;
        Entities.ForEach((DynamicBuffer<InputCommandData> inputBuffer, ref Translation pos, ref Rotation rot, ref PredictedGhostComponent prediction) =>
        {
            if (!GhostPredictionSystemGroup.ShouldPredict(tick, prediction))
                return;
            InputCommandData input;
            inputBuffer.GetDataAtTick(tick, out input);

            // 回転行列を求める
            var rotation = Quaternion.AngleAxis(input.angleH, new float3(0, 1, 0)) * Quaternion.AngleAxis(input.angleV, new float3(1, 0, 0));
            var dir = rotation * front;
            
            pos.Value += new float3(dir) * input.speed * deltaTime; 
            rot.Value = rotation;
        });
    }
}