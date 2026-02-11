# Infinite Match-3 Engine 💎
> A high-performance, mobile-optimized Match-3 framework built in Unity (C#).

[![Play in Browser](https://img.shields.io/badge/Play_in_Browser-itch.io-fa5c5c?style=for-the-badge&logo=itch.io)](https://aciros.itch.io/infinite-match-3-engine)
[![Unity](https://img.shields.io/badge/Made_with-Unity-000000?style=for-the-badge&logo=unity)](https://unity.com/)

## 🎮 Overview
This project is a technical case study implementing the core mechanics of top-grossing puzzle games. It features a custom grid management system designed for **mobile performance**, utilizing **Object Pooling** and **Non-Recursive Algorithms** to ensure 60 FPS on low-end devices.

## 🔧 Technical Highlights

### 1. Non-Recursive Flood Fill (BFS)
Instead of standard recursion (which risks Stack Overflow on large boards), I implemented an iterative **Breadth-First Search (BFS)** for match detection.
* **Code:** [`Scripts/BoardManager.cs`](./Assets/Scripts/BoardManager.cs) (See `FindMatches` method)
* **Why:** Ensures $O(N)$ complexity without memory spikes.

### 2. Object Pooling System
To prevent Garbage Collection (GC) stutters during gameplay, tiles are never destroyed. They are deactivated and returned to a managed queue.
* **Code:** [`Scripts/TilePool.cs`](./Assets/Scripts/TilePool.cs)
* **Impact:** Zero runtime allocation for tile spawning after initialization.

### 3. Logical vs. Visual Separation
The game state (2D Array) updates instantly, while the visual layer (Unity GameObjects) interpolates smoothly.
* **Logic:** The data model shifts pointers immediately.
* **Visuals:** `StartCoroutine(SlideToPosition)` handles the Lerp animation.
* **Result:** Deterministic gameplay that is independent of frame rate or physics engine quirks.

### 4. Proactive Deadlock Detection
The board automatically shuffles if no moves are possible.
* **Algorithm:** Simulates every possible swap ($N \times M$ operations) in a virtual grid to check for potential matches before the user can input.

## 📂 Project Structure
* `Assets/Scripts/Managers/`: Core game logic (Board, Game, Audio).
* `Assets/Scripts/Controllers/`: Visual components (Tile animation, Input handling).
* `Assets/Prefabs/`: Pre-configured GameObjects for the pooling system.

## 🚀 Setup & Installation
1.  Clone the repo: `git clone https://github.com/melihgurlek/match-3-engine.git`
2.  Open in **Unity 2021.3 LTS** (or newer).
3.  Open `Scenes/MainGame.unity`.
4.  Press **Play**.

---
*Developed by [Melih Gürlek](https://linkedin.com/in/melihgurlek)*
