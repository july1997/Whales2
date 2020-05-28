using Unity.Mathematics;
using UnityEngine;

public class Bootstrap : MonoBehaviour 
{   
    public GameObject Prefab;

    public static Bootstrap Instance { get; private set; }
    public static Param Param { get { return Instance.param; } }

    [SerializeField] public int boidCount = 100;
    [SerializeField] float3 boidScale = new float3(0.1f, 0.1f, 0.3f);
    [SerializeField] Param param;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {

    }

    void OnDrawGizmos()
    {
        if (!param) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * param.wallScale);
    }
}