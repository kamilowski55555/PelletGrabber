# ML_CODE_STYLE.md
Machine Learning & Reinforcement Learning Code Style Guidelines

These rules define how all ML, RL, Unity ML-Agents, and PyTorch code must be structured to remain reproducible, maintainable, and commercially viable. They complement the general Python code style and override it where ML-specific discipline is required.

---

## 1. Guiding Principles
- **Reproducibility first.** All experiments must be repeatable.
- **Configuration over hardcoding.** No hyperparameters inside code.
- **Clear separation of concerns.** Training ≠ inference ≠ environment logic.
- **Explicitness beats cleverness.** No "magic" tensor operations.
- **Everything must be logged.** If it's not logged, it didn't happen.

---

## 2. ML Project Structure
```
ml/
    configs/
    datasets/
    envs/
    policies/
    trainers/
    models/
    utils/
    notebooks/
    checkpoints/
    experiments/
```

### Rules:
- **configs/** contains YAML/TOML config files for experiments.
- **envs/** holds all Unity ML-Agent environment wrappers and adapters.
- **policies/** contains neural network architectures (e.g., CNNs, MLPs).
- **trainers/** contain training loops, optimizers, schedulers.
- **models/** holds PyTorch model definitions that are not policies.
- **utils/** common helpers (seeding, logging, geometry, replay buffers).
- **checkpoints/** stores model snapshots with metadata.
- **experiments/** stores results, logs, plots, artifacts.
- **notebooks/** for prototyping only, never production code.

---

## 3. Configuration Management
### **Strict requirement:** No hardcoded hyperparameters in code.
All configurable values must be in `configs/`.

Example config (`configs/ppo_labyrinth.yaml`):
```
seed: 1337
learning_rate: 3e-4
batch_size: 64
gamma: 0.99
entropy_coef: 0.01
update_epochs: 4
max_steps: 2_000_000
unity_env_path: "Builds/Labyrinth.x86_64"
```

Training code loads config once:
```
config = load_config("ppo_labyrinth.yaml")
```

---

## 4. Reproducibility & Seeds
Always set seeds for:
- Python
- NumPy
- PyTorch
- Unity ML-Agents (if supported)

Provide a unified seed function:
```
def set_global_seed(seed: int) -> None:
    random.seed(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)
    torch.cuda.manual_seed_all(seed)
```

No experiment may run without calling this.

---

## 5. Logging & Experiment Tracking
### Required logging:
- Training loss curves
- Episode reward curves
- Evaluation metrics
- Learning rate schedules
- Hyperparameters used
- Checkpoints and their metadata

### Use at least one:
- TensorBoard
- WandB
- CSVLogger (custom)

Each experiment must generate a uniquely named directory in `experiments/`:
```
experiments/
    ppo_labyrinth_2025-12-05_21-14-33/
```

Metadata file:
```
metadata.json
```
must include:
- git commit hash
- config file hash
- timestamp
- environment version

---

## 6. Model & Checkpoint Standards
### Required fields inside checkpoint metadata:
- model architecture name
- config used
- training step
- git commit hash
- reward/eval metrics

### Directory structure:
```
checkpoints/
    ppo_v1/
        step_0500000.pt
        step_1000000.pt
        metadata.json
```

Checkpoints may **not** be overwritten.
New files must always be created.

---

## 7. RL Code Architecture
RL introduces specific components that must be kept separate.

### RL Directory Structure:
```
rl/
    runners/
    buffers/
    policies/
    envs/
    rewards/
```

### RL layering rules:
- **Policy**: neural network deciding actions.
- **Runner**: collects rollouts from environment.
- **Buffer**: stores transitions (s, a, r, s').
- **Trainer**: updates policy using collected batch.
- **Reward functions**: must be explicit and documented.

Example reward function file:
```
# rewards/navigation.py
# Reward shaping for red-cube labyrinth.
# Rationale must be documented above each reward component.
```

No reward shaping allowed directly inside environment loop.

---

## 8. Unity ML-Agents Integration Rules
### Python side:
- Wrap Unity environment inside a class in `envs/`.
- No direct ML logic in Unity C# scripts.
- Communication must be explicit and typed.
- Behavior parameters must be versioned.

### Unity side:
- Each agent must have a clear observation spec.
- No hidden heuristics.
- No unlogged changes to environment difficulty.

### Observation rules:
- Document shape and meaning of each input tensor.
- Use comments to describe spatial conventions (e.g., channels-first).

---

## 9. Tensor Discipline
### Forbidden:
- Silent broadcasting
- In-place ops without comments
- Unlabeled tensor dimensions
- Implicit reshapes
- Operations relying on coincidental shapes

### Required:
- Every tensor must have its shape documented.
- Complex tensor transformations must be commented.

Example:
```
# obs: [batch, channels, height, width]
obs = obs.float() / 255.0
```

---

## 10. Training vs Inference Separation
Two separate entrypoints:
```
train.py
inference.py
```

### Rules:
- Training code must never import inference-only modules.
- Inference must load checkpoint + config.
- No training dependencies allowed inside inference.

---

## 11. No Anti-Patterns
Strictly forbidden:
- Hardcoded hyperparameters
- Hidden reward shaping inside env loop
- Mixing Unity logic with RL logic
- Putting training and inference in same script
- Silent tensor reshapes
- Unseeded experiments
- Checkpoint overwriting
- Models created inside loops
- Complex logic inside Jupyter notebooks

---

## 12. Documentation Requirements
Every RL component must document:
- What it computes
- Input/output tensor shapes
- Why it exists (intent)
- Edge cases

Reward functions must include rationales.

Trainers must have full docstrings describing:
- algorithm
- update steps
- limitations

---

## Summary for AI Tools
Generated ML/RL code must:
1. Use configuration files for all hyperparameters.
2. Ensure reproducibility via seeds.
3. Separate training, inference, env, runner, policy, and buffer logic.
4. Log everything.
5. Document tensor shapes and reward shaping.
6. Use strict versioning for checkpoints and environments.
7. Produce deterministic experiments.
8. Avoid magic numbers and implicit transformations.

Treat ML code like scientific software: precise, reproducible, interpretable.

