#!/usr/bin/env bash
# ============================================================
# setup.sh
# Bootstrap script: initializes git, creates first commit
# sequence, configures remote, pushes to GitHub.
#
# Usage:
#   ./scripts/setup.sh <github-user>/<repo-name> [<remote-url>]
#
# Example:
#   ./scripts/setup.sh yourname/project-aria
# ============================================================

set -euo pipefail

REPO_SLUG="${1:-}"
REMOTE_URL="${2:-}"

if [[ -z "$REPO_SLUG" ]]; then
    echo "Usage: $0 <github-user>/<repo-name> [<remote-url>]"
    echo ""
    echo "Example:"
    echo "  $0 yourname/project-aria"
    echo "  $0 yourname/project-aria git@github.com:yourname/project-aria.git"
    exit 1
fi

if [[ -z "$REMOTE_URL" ]]; then
    REMOTE_URL="https://github.com/${REPO_SLUG}.git"
fi

# ---- Sanity
if [[ ! -d ".git" ]]; then
    echo "📦 Initializing git repository..."
    git init -b main
    git config user.name "${GIT_USER_NAME:-Project Aria Bot}"
    git config user.email "${GIT_USER_EMAIL:-bot@project-aria.local}"
fi

# ---- Personalize placeholders
echo "🔧 Personalizing placeholders..."
find . -type f \( -name "*.md" -o -name "*.yml" \) -not -path "./.git/*" | while read -r f; do
    sed -i.bak "s|<user>|${REPO_SLUG%%/*}|g" "$f" && rm -f "$f.bak"
done

# ---- Update README badge URLs
echo "🔗 Updating badge URLs..."
sed -i.bak "s|github.com/<user>/project-aria|github.com/${REPO_SLUG}|g" README.md && rm -f README.md.bak
sed -i.bak "s|../../discussions|../../..//discussions|g" README.md && rm -f README.md.bak
sed -i.bak "s|../../issues|../../..//issues|g" README.md && rm -f README.md.bak
sed -i.bak "s|../../wiki|../../..//wiki|g" README.md && rm -f README.md.bak

# ---- Initial commit
echo "📝 Creating initial commit sequence..."
git add .gitignore LICENSE README.md CHANGELOG.md
git commit -m "chore: initial project scaffold

- MIT License
- .gitignore for Unity
- README with badges
- CHANGELOG" 2>/dev/null || echo "  (initial files already committed)"

git add .github/
git commit -m "ci: add GitHub Actions workflows + community templates

- Android build pipeline (game-ci/unity-builder)
- Issue templates (bug, feature, performance)
- PR template
- CODEOWNERS" 2>/dev/null || echo "  (github files already committed)"

git add Architecture.md SetupGuide.md PerformanceGuide.md Networking.md ModuleMap.md
git commit -m "docs: architecture, setup, performance, networking guides" 2>/dev/null || echo "  (docs already committed)"

git add Packages/ ProjectSettings/
git commit -m "build: Unity package manifest (URP, NGO, Input System, Addressables)" 2>/dev/null || echo "  (package files already committed)"

git add BuildTools/ scripts/
git commit -m "build: local build scripts + CI bootstrap" 2>/dev/null || echo "  (build files already committed)"

# ---- Code modules in dependency order
git add Assets/Scripts/Core/
git commit -m "feat(core): GameManager, EventBus, ServiceLocator, ObjectPool

- EventBus: type-safe pub/sub
- ServiceLocator: dependency registry
- ObjectPool: zero-alloc GameObject pool
- TimeSystem: day/night + season
- WeatherSystem: state machine
- SaveSystem: JSON autosave + backup rotation
- GameSettings: 5 graphics tiers + accessibility" 2>/dev/null || echo "  (core already committed)"

git add Assets/Scripts/Player/
git commit -m "feat(player): controller, stats, input, interaction, inventory

- CharacterController-based movement
- HP/Hunger/Stamina/Temperature survival loop
- New Input System integration
- Save/load hooks" 2>/dev/null || echo "  (player already committed)"

git add Assets/Scripts/Controls/
git commit -m "feat(controls): mobile-first touch UI

- VirtualJoystick (dynamic positioning)
- SmartActionButton (context-aware)
- HotbarUI (8 slots + keybinds)
- MobileControlsUI (left-handed + customizable)" 2>/dev/null || echo "  (controls already committed)"

git add Assets/Scripts/World/
git commit -m "feat(world): procedural chunked world + 12 biomes

- 16x128x16 chunks with mesh pooling
- Perlin/FBM noise + seeded RNG
- Biome sampling by climate
- Async generation queue" 2>/dev/null || echo "  (world already committed)"

git add Assets/Scripts/Building/ Assets/Scripts/Farming/
git commit -m "feat(building+farming): grid-snap + crops

- BuildingSystem with ghost preview
- FarmSystem: tilling, watering, seasonal growth" 2>/dev/null || echo "  (building/farming already committed)"

git add Assets/Scripts/Inventory/ Assets/Scripts/Crafting/
git commit -m "feat(inventory+crafting): items, drag-drop, recipe queue

- ItemDatabase (ScriptableObject)
- ItemSlot with touch drag-drop
- CraftingSystem with station tiers" 2>/dev/null || echo "  (inventory/crafting already committed)"

git add Assets/Scripts/Combat/
git commit -m "feat(combat): melee/ranged/magic + enemies + multi-phase bosses

- CombatSystem (dodge, parry, stamina)
- Enemy AI (NavMesh)
- BossController with phase + patterns
- Projectile (homing/straight)" 2>/dev/null || echo "  (combat already committed)"

git add Assets/Scripts/NPC/ Assets/Scripts/Quest/
git commit -m "feat(npc+quest): schedule, dialogue, friendship, quests

- NPCController with hourly schedule
- DialogueSystem (branching trees + rewards)
- QuestSystem (Main/Side/Daily/Event)
- Auto-triggers via EventBus" 2>/dev/null || echo "  (npc/quest already committed)"

git add Assets/Scripts/Multiplayer/
git commit -m "feat(multiplayer): NGO host/client + server-authoritative anti-cheat

- 2-20 players coop
- ServerAuthority: rate limit + distance + sanity checks
- Chat RPCs" 2>/dev/null || echo "  (multiplayer already committed)"

git add Assets/Scripts/Optimization/ Assets/Scripts/UI/ Assets/Scripts/Audio/ Assets/Scripts/Achievements/
git commit -m "feat(optimization+ui+audio+achievements): perf, HUD, adaptive audio

- LODManager, AsyncAssetLoader, PerformanceMonitor
- HUDManager, MinimapUI
- AudioManager with crossfade
- AchievementSystem (persistent)" 2>/dev/null || echo "  (optimization already committed)"

git add Assets/Editor/
git commit -m "build(editor): Unity Editor build automation

- BuildScript.BuildAndroid / BuildIOS
- Keystore generation helper
- Scene list management" 2>/dev/null || echo "  (editor already committed)"

# ---- Add remote + push
echo ""
echo "📡 Adding remote: $REMOTE_URL"
git remote remove origin 2>/dev/null || true
git remote add origin "$REMOTE_URL"

echo ""
echo "📜 Commit log:"
git log --oneline

echo ""
echo "🚀 Ready to push! Run:"
echo "  git push -u origin main"
echo ""
echo "After push, configure these GitHub Secrets:"
echo "  - UNITY_EMAIL"
echo "  - UNITY_PASSWORD"
echo "  - UNITY_LICENSE"
echo ""
echo "Then the CI workflow will auto-build the APK."
