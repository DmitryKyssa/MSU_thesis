using UnityEngine;

public class UI : Singleton<UI>
{
    private HintRenderer _hintRenderer;
    private MazeSpawner _mazeSpawner;
    private MazeAgentRecursivePathfinding _mazeAgentBackHome;
    private MazeAgentAStar _mazeAgentAStar;
    private MazeAgentML _mazeAgent;

    private void Awake()
    {
        _mazeAgentBackHome = MazeAgentRecursivePathfinding.Instance;
        _mazeAgentAStar = MazeAgentAStar.Instance;
        _mazeAgent = FindFirstObjectByType<MazeAgentML>();
        _hintRenderer = HintRenderer.Instance;
        _mazeSpawner = MazeSpawner.Instance;
    }

    public void ActivateAgentBackHome()
    {
        _mazeAgent.SetCircleColliderEnableStatus(false);
        _hintRenderer.ComponentEdgeCollider.enabled = false;
        _mazeAgentBackHome.ActivateAgent();
    }

    public void ActivateAgentAStar()
    {
        _mazeAgent.SetCircleColliderEnableStatus(false);
        _hintRenderer.ComponentEdgeCollider.enabled = false;
        _mazeAgentAStar.ActivateAgent();
    }

    public void ActivateAgent()
    {
        _hintRenderer.ComponentEdgeCollider.enabled = true;
        _mazeAgent.enabled = true;
    }

    public void RegenerateMaze()
    {
        _mazeAgentBackHome.ResetPosition();
        _hintRenderer.PathIsDrawn = false;
        _hintRenderer.ResetLineRendererPositions();
        foreach (Transform child in _mazeSpawner.transform)
        {
            Destroy(child.gameObject);
        }
        _mazeSpawner.GenerateMaze();
    }

    public void DrawPath()
    {
        if (!_hintRenderer.PathIsDrawn)
        {
            _hintRenderer.DrawPath();
        }
        else
        {
            _hintRenderer.PathIsDrawn = false;
            _hintRenderer.ResetLineRendererPositions();
        }
    }
}