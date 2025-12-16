# Pellet Grabber

A simple Unity project demonstrating the basics of **Reinforcement Learning** using [Unity ML-Agents](https://github.com/Unity-Technologies/ml-agents).

## Overview

This project is a beginner-friendly example of training an AI agent to learn a simple task: moving to collect a pellet while avoiding walls. It's a great starting point to understand how reinforcement learning works in practice.

## How It Works

The agent learns through trial and error:
- **Positive reward (+2)**: When the agent reaches the pellet
- **Negative reward (-1)**: When the agent hits a wall

The agent observes its own position and the target position, then learns to move left or right to maximize its reward.

## Requirements

- Unity 2022.3 or later (LTS recommended)
- [Unity ML-Agents Package](https://docs.unity3d.com/Packages/com.unity.ml-agents@2.0/manual/index.html) (v2.0.2)
- Python 3.8+ with `mlagents` package (for training)
