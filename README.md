# Training an AI Model in Unity with ML-Agents

This guide explains the steps required to set up and train an AI model in Unity using ML-Agents.

## Prerequisites

Before you begin, ensure you have the following installed:

- **Unity 6000.0.32f1** 
- **Python 3.10.11**
- **ML-Agents (in Unity) 3.0.0.**
- **ML-Agents (from PyPi): 1.1.0**
- **Communicator API 1.5.0**
- **PyTorch: 2.6.0+cpu**

## Installation

### 1. Install ML-Agents

Open a terminal or command prompt and run:

```bash
pip install mlagents
```

### 2. Install ML-Agents in Unity (If is not installed. Check: **Window → Package Manager → In project**)

1. Open Unity and go to **Window → Package Manager**.
2. Click on **Add package from git URL** and enter:
   ```
   com.unity.ml-agents
   ```
3. Click **Add** to install the package.

### 3. Verify Installation

Run the following command to check the installation:

```bash
mlagents-learn --help
```

If the command executes without errors, the setup is complete.

## Training Process

### 1. Modify a Training Configuration File (`config.yaml`)

File `config.yaml`:

```yaml
behaviors:
  MazeAgent: #Named as in Behaviour Parameters component attached to Player gameobject in Scenes/Maze2D
    trainer_type: ppo
    hyperparameters:
      batch_size: 64
      buffer_size: 2048
      learning_rate: 3.0e-4
    network_settings:
      normalize: false
      hidden_units: 128
      num_layers: 2
    reward_signals:
      extrinsic:
        strength: 1.0
        gamma: 0.99
    max_steps: 100000
    time_horizon: 64
```

### 3. Start Training

Run the training process with:

```bash
mlagents-learn config.yaml --run-id=MazeAgentTraining
```
NOTE: MazeAgentTraining is unique identificator

### 4. Using the Trained Model

Once training is complete, a model file (`.onnx`) will be saved in the `results` directory. To use it in Unity:

1. Drag and drop the trained model into your Unity project.
2. Assign it to the **Behavior Parameters** component of the agent.
