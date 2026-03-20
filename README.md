# Feature: Player Name Input

This feature adds a dedicated login screen to the game's startup flow, allowing players to set their own name and stores it persistently in the save data for use in dialogues.

## Key Technical Changes

### 1. Persistent Save System (`SaveManager.cs`)
- **Version Upgrade**: The save format has been upgraded from `1` to `2`.
- **String Support**: Now includes a `playerName` field in the binary serialization logic.
- **Backward Compatibility**: If the game detects an older (Version 1) save file, it gracefully defaults the name to "Pegawai Baru" (New Employee) instead of wiping the save.

### 2. UI Login Flow (`ModeSelectController.cs`)
- **Dedicated Screen**: Extracted the name input into its own `_nameInputView` that appears before the game mode selection.
- **Dark Theme Styling**: The `TextField` has been custom-styled with a dark-blue aesthetic and a light-blue border to match the game's "Email/Work Simulator" theme.
- **Immediate Feedback**: The "Lanjut" (Continue) button saves the name and transitions to the Mode select (Arcade / Story) menu without any screen flickering.

### 3. Dialogue Integration (`DialogueController.cs`)
- **Dynamic Injection**: The dialogue system now scans for any lines where the speaker is "Kamu" (You) and automatically replaces it with the player's custom name.
- **Token Replacement**: Introduced `{PlayerName}` token support in the dialogue body, allowing story characters to address the player by their chosen name.

## How to Test
1. Load the `SampleScene.unity`.
2. Press **Play**.
3. You will be greeted by the **Profil Pegawai** screen.
4. Type in your name and click **Lanjut**.
5. Once in a story mode dialogue, verify that your chosen name appears as the speaker for your lines!
