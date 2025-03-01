using UnityEngine;
using Zenject;

public class GameInstaller : MonoInstaller
{
    [SerializeField] private GameObject cellPrefab;
    public const string CELL_PREFAB_ID = "CellPrefab";

    public override void Start()
    {
        base.Start();
    }

    public override void InstallBindings()
    {
        Container.Bind<MazeGenerator>().AsSingle();
        Container.Bind<GameObject>().WithId(CELL_PREFAB_ID).FromInstance(cellPrefab).AsSingle();
        Container.Bind<MazeSpawner>().FromComponentInHierarchy().AsSingle();
        Container.Bind<HintRenderer>().FromComponentInHierarchy().AsSingle();
        Container.Bind<UI>().FromComponentInHierarchy().AsSingle();
        Container.Bind<AgentBackHomeAlgorithm>().FromComponentInHierarchy().AsSingle();
        Container.Bind<AgentAStarAlgorithm>().FromComponentInHierarchy().AsSingle();
        Container.Bind<MazeAgent>().FromComponentInHierarchy().AsSingle();
    }
}