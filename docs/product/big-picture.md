## Birdie: Game Design Strategy & Vision

## High-Level Concep
- Idle Bird is a "quasi-idle," cozy desktop companion game for Windows. It runs as a transparent overlay sitting just above the user's taskbar, transforming the bottom of the screen into a virtual window sill.
- The game is designed to be non-intrusive. It exists in the player's peripheral vision while they work or browse, offering a "digital naturalist" experience where birds visit in real-time.

## Core Design Pillars

- Non-Intrusive Companionship: The game respects the user's workspace. It does not demand constant attention but rewards observation.
- Cozy & Safe: The aesthetic is warm, illustrated, and relaxing. There is no "fail state," no death, and no stress.
- Real-World Connection: The game synchronizes with the player's local time. Nighttime in the real world means nocturnal birds (like owls) appear in the game.
- Deep Individual Bonds: Players don't just collect species; they build specific friendship levels with individual bird entities.

## The "Quasi-Idle" Gameplay Loop

- Unlike standard idle games that play themselves entirely, Idle Bird relies on "Active Bursts" of interaction.
- Wait (Idle): The player works on their PC. Birds spawn on the taskbar sill based on time-of-day logic.
- Interact (Active): The player notices a bird, clicks it, and plays a short, mouse-only minigame (e.g., feeding, singing back).
- Reward (Economy): Success yields Golden Seeds (Currency) and Friendship Points (Relationship XP).
- Invest (Progression): * Seeds are spent on Habitat Upgrades (e.g., Bird Bath) to attract new species.
- Friendship Points unlock Diary Entries (lore/facts) for that specific bird.

## Economy & Progression Strategy

The game utilizes a Dual-Resource System to separate "Horizontal Progression" (collecting more birds) from "Vertical Progression" (knowing birds better).

Resource A: Golden Seeds (The Universal Currency)

Source: Earned by playing minigames.

Sink: Used in the Shop.

Strategic Purpose: To control the pacing of new species discovery. Buying a "Feeder" or "Bird Bath" is the trigger that allows rare birds to spawn.

Resource B: Friendship Points (The Individual Metric)

Source: Earned by interacting with a specific bird.

Sink: Automatic thresholds (Level 1 to Level 4).

Strategic Purpose: To create emotional attachment.

Level 1 (Contact): Name/Photo added to Diary.

Level 2 (Acquaintance): Diet/Habitat info unlocked.

Level 3 (Friend): Fun facts/Trivia unlocked.

Level 4 (Best Friend): The bird leaves physical gifts (rare feathers/seeds).

## Mechanics & Systems Overview

Spawning Logic (The "Living" World)

Time-Gated: Birds have specific active hours (e.g., Robins: 08:00–18:00; Owls: 22:00–04:00).

Rarity Weights: Common birds appear often; rare birds require specific "Lures" or Habitat Upgrades to spawn.

Pity System: If a specific bird hasn't visited in a long time, its spawn probability slowly increases to prevent bad RNG frustration.

The Minigames

Minigames are short (10-30 seconds), mouse-driven, and low-stakes. They are the primary method of interaction.

Examples: Rhythm games (mimic the song), Sorting games (pick the right seeds), Precision clicks (cleaning feathers).

The Bird Diary (The Trophy Room)

The Diary is the ultimate long-term goal. It is an album that fills up not just with stamps of birds, but with detailed knowledge gained through friendship.

## Technical & Aesthetic Constraints

Window Management: The game must handle transparency and "click-through" logic so it doesn't block the user from clicking their actual desktop wallpaper when no bird is present.

Setting: The birds are native to Spain, grounding the game in a specific, realistic ecological niche.

## Top Features (USP)

### Characters & World
- **Cute Animations**: Immersive toon fantasy world with vibrant, delightful destruction

### Accessibility
- **Relaxing Gameplay**: Perfect for unwinding anytime, anywhere

## Project Information

- **Project Code**: Birdie
- **Platforms**: Windoows

