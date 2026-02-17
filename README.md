
# The Long Mood

This mod is an implementation of Project Zomboid’s boredom and depression system into The Long Dark.

## Overview

The Long Mood introduces a dynamic psychological layer to survival.

Boredom and Depression evolve continuously based on player activity, environment, and physical condition.

The progression of Boredom and Depression is represented in the game by afflictions.
These afflictions act as a symbolic adaptation of Project Zomboid’s moodles.  

Only one boredom-related affliction and one depression-related affliction can be active at any given time.

Following Project Zomboid’s structure:

Boredom progresses as:  
Bored → Very Bored → Extremely Bored

Depression progresses as:  
Sad → Weepy → Miserable → Hopeless

The mod is currently about 95% complete.  
The only planned feature still missing is visual color desaturation tied to depression severity.

## How It Works

Boredom and Depression do **not** update while sleeping.

### Boredom

Boredom is influenced by activity, light sources, and proximity to fire.

It does **not** increase immediately — a delay applies before inactivity begins to raise it (customizable).

| Condition | Effect on Boredom |
|------------|------------------|
| Player is active | Decreases over time (Recovery × game hours) |
| Holding active light source | Decreases over time (Recovery × game hours) |
| Within 5m of a fire | Decreases over time (Recovery × game hours) |
| Inactive below "Boredom Delay" | No change |
| Inactive beyond "Boredom Delay" | Increases over time (Rate × game hours) |

### Depression

Depression reacts to afflictions, boredom level, and player activity.

| Condition | Effect on Depression |
|------------|---------------------|
| Player has affliction | Increases (Rate × 2) |
| Player is reading | Decreases (Recovery) |
| Boredom ≥ 90 | Increases (Rate × 2.96) |
| Boredom ≥ 75 | Increases (Rate × 2.22) |
| Boredom ≥ 50 | Increases (Rate × 1.48) |
| Boredom < 50 | Decreases (Rate × 0.5) |
| Finishing a proper meal (cooldown based) | Flat depression reduction bonus |

Depression is therefore:

- Directly amplified by high boredom
- Strongly affected by physical suffering
- Temporarily relieved by reading
- Boosted downward by food consumption

### Struggle Penalty (Aftershock System)

Wildlife struggles apply a Depression penalty. You can choose how it is applied:

- **Instant mode**: the full penalty is applied immediately.
- **Aftershock mode**: the penalty is stored as a pool and applied gradually over time.

In aftershock mode, the penalty is applied linearly over the configured duration (1–12 in-game hours).  
While an aftershock is active, **passive depression recovery is disabled** so the penalty isn’t silently canceled out by baseline recovery.

This makes attacks leave a meaningful psychological impact even if the player returns to safe / comfortable conditions afterwards.

### Eating Bonus Logic

If the player eats long enough:

- A flat depression reduction is applied
- A cooldown prevents immediate repetition
- Partial eating does not trigger the bonus

## For Developers
<details>
<summary><strong>Click to Expand</strong></summary>
  
### Visual Debug

Boredom and Depression values can be displayed directly in-game for debugging purposes.

When enabled in settings, values are shown numerically and update in real time.  
Color thresholds reflect severity stages, allowing quick visual verification of progression logic.

This mode is intended strictly for testing and balancing.

### ML Logging

Optional MelonLoader logging can be enabled via settings.

When active, the system logs:

- Inactivity detection triggers
- Save/load operations

Logging is intended for debugging purposes and produce frequent output.

### Console Commands

The following debug commands are available via the [Developer Console](https://github.com/DigitalzombieTLD/TLD-Developer-Console/):

**set_boredom value**  
Sets boredom to a specific value.  
Example: `set_boredom 69`

**set_depression value**  
Sets depression to a specific value.  
Example: `set_depression 69`

**reset_boredom**  
Resets boredom to 0.

**reset_depression**  
Resets depression to 0.

</details>

## Installation

1. Install MelonLoader
2. Install [AfflictionComponent](https://github.com/TLD-Mods/AfflictionComponent), [ModSettings](https://github.com/DigitalzombieTLD/ModSettings/) and [ModData](https://github.com/dommrogers/ModData)
3. Place `TheLongMood.dll` inside the Mods folder

## AI Notice

This mod was partially developed with the assistance of AI tools (approximately 20%).

AI support was used for structural guidance, debugging assistance, and documentation refinement.  
Coding, core design decisions, system architecture, balancing, and implementation logic remain fully human-driven.
