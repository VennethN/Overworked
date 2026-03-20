# OVERWORKED

**OVERWORKED** is an immersive "Email & Work Simulator" developed in Unity 6 by **Sleep Deprivation Studio**, specifically built for **BGDJam 2026**. 

In this game, players step into the shoes of a new employee at Globodyne, tasked with managing a never-ending influx of emails, completing urgent tasks, and navigating office politics through dynamic dialogues.

---

## Core Features

- **Email Management Engine**: A robust system for categorizing, replying to, and completing tasks from an overflowing inbox.
- **Story Mode**: A 7-day narrative challenge where your decisions and efficiency impact your progression.
- **Arcade Mode**: An endless high-score mode for competitive email sorting.
- **Identity & Profile**: Dedicated player name input that persists across sessions and reflects in the game's dialogue.
- **Interactive Dialogues**: Branching narrative sequences with character avatars and custom name injection.
- **Task System**: Integrated minigames and timers for high-pressure work tasks.

## Technology Stack

- **Unity Version**: 6000.3.11f1 (URP 2D)
- **UI Framework**: UI Toolkit (UXML/USS) - Modern, CSS-like UI styling.
- **Input System**: New Unity Input System.
- **Data Driven**: All emails, story beats, and configurations are handled via JSON for easy modding and expansion.
- **Persistence**: Custom binary serialization for fast and reliable cross-version save data.

## Project Structure

- `Assets/Scripts`: The brain of the game, divided into `Core`, `Email`, `Story`, and `UI`.
- `Assets/Resources/Data`: The JSON-driven heart of the game (Emails, Story, Config).
- `Assets/UI`: UXML layouts and USS stylesheets.
- `Assets/Scenes`: The main game loops and menu scenes.

## Getting Started

1. Open the project in **Unity 6**.
2. Navigate to `Assets/Scenes` and open `SampleScene`.
3. Press **Play** and enter your employee name to begin your first day at Globodyne!

---

*Part of BGDJam 2026. Developed by Sleep Deprivation Studio.*

© 2026 Sleep Deprivation Studio. All rights reserved.
