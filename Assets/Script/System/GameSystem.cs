using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
public class GoInGameClientSystem : ComponentSystem
{
    private ScoreMonoBehavior _counter;
    protected override void OnCreate()
    {
        _counter = GameObject.FindObjectOfType<ScoreMonoBehavior>();
    }

    protected override void OnUpdate()
    {
        Entities.WithNone<NetworkStreamInGame>().ForEach((Entity ent, ref NetworkIdComponent id) =>
        {
            PostUpdateCommands.AddComponent<NetworkStreamInGame>(ent);
            var req = PostUpdateCommands.CreateEntity();
            PostUpdateCommands.AddComponent<GoInGameRequest>(req);
            PostUpdateCommands.AddComponent(req, new SendRpcCommandRequestComponent { TargetConnection = ent });
        });

        Entities.WithNone<SendRpcCommandRequestComponent>().ForEach((Entity reqEnt, ref ScoreRequest req, ref ReceiveRpcCommandRequestComponent reqSrc) =>
        {
            var score = GetSingleton<ScoreCompoment>();

            score.value += req.addPoints;
            _counter.SetScore(score.value);

            SetSingleton<ScoreCompoment>(score);
            PostUpdateCommands.DestroyEntity(reqEnt);
        });
    }
}

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class GameServerSystem : ComponentSystem
{
    private bool init = false;

    protected override void OnCreate()
    {
    }
    protected override void OnUpdate()
    {
        if(!init)
        {
            var random = new Unity.Mathematics.Random(853);
            var ghostCollection = GetSingleton<GhostPrefabCollectionComponent>();
            var ghostId = whalesGhostSerializerCollection.FindGhostType<BoidSnapshotData>();
            var prefab = EntityManager.GetBuffer<GhostPrefabBuffer>(ghostCollection.serverPrefabs)[ghostId].Value;

            for (int i = 0; i < Bootstrap.Instance.boidCount; ++i)
            {
                var boid = EntityManager.Instantiate(prefab);

                EntityManager.SetComponentData(boid, new Translation {Value = random.NextFloat3(1f)});
                EntityManager.SetComponentData(boid, new Rotation { Value = quaternion.identity });
                EntityManager.SetComponentData(boid, new Velocity { Value = random.NextFloat3Direction() * Bootstrap.Param.initSpeed });
                EntityManager.SetComponentData(boid, new Acceleration { Value = float3.zero });
                EntityManager.AddBuffer<NeighborsEntityBuffer>(boid);
            }
            init = true;
        }

        Entities.WithNone<SendRpcCommandRequestComponent>().ForEach((Entity reqEnt, ref GoInGameRequest req, ref ReceiveRpcCommandRequestComponent reqSrc) =>
        {
            PostUpdateCommands.AddComponent<NetworkStreamInGame>(reqSrc.SourceConnection);
            UnityEngine.Debug.Log(string.Format("Server setting connection {0} to in game", EntityManager.GetComponentData<NetworkIdComponent>(reqSrc.SourceConnection).Value));
            var ghostCollection = GetSingleton<GhostPrefabCollectionComponent>();

            var playerGhostId = whalesGhostSerializerCollection.FindGhostType<WhaleSnapshotData>();
            var playerPrefab = EntityManager.GetBuffer<GhostPrefabBuffer>(ghostCollection.serverPrefabs)[playerGhostId].Value;
            var player = EntityManager.Instantiate(playerPrefab);
            EntityManager.SetComponentData(player, new PlayerComponent { PlayerId = EntityManager.GetComponentData<NetworkIdComponent>(reqSrc.SourceConnection).Value});
            PostUpdateCommands.AddBuffer<InputCommandData>(player);

            PostUpdateCommands.SetComponent(reqSrc.SourceConnection, new CommandTargetComponent {targetEntity = player});
            PostUpdateCommands.DestroyEntity(reqEnt);
        });
    }
}