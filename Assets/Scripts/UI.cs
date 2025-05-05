using UnityEngine;

public class UI : Singleton<UI>
{
    private HintRenderer _hintRenderer;
    private MazeSpawner _mazeSpawner;
    private AgentBackHomeAlgorithm _mazeAgentBackHome;
    private AgentAStarAlgorithm _mazeAgentAStar;
    private MazeAgent _mazeAgent;

    private void Awake()
    {
        _mazeAgentBackHome = AgentBackHomeAlgorithm.Instance;
        _mazeAgentAStar = AgentAStarAlgorithm.Instance;
        _mazeAgent = FindFirstObjectByType<MazeAgent>();
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