// ============================================================
// GameBootstrap.cs
// Drop this on a GameObject in the Bootstrap scene. It instantiates
// the GameManager, registers services, and triggers the main loop.
// ============================================================
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.World;
using ProjectAria.Optimization;
using ProjectAria.Audio;
using ProjectAria.Quest;
using ProjectAria.Farming;
using ProjectAria.Crafting;
using ProjectAria.Multiplayer;
using ProjectAria.Player;

namespace ProjectAria.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Refs")]
        public GameManager GameManagerPrefab;
        public NetworkGameManager NetworkPrefab;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // 1. GameManager
            if (GameManager.Instance == null)
            {
                var go = GameManagerPrefab != null ? Instantiate(GameManagerPrefab) : new GameObject("GameManager");
                if (GameManagerPrefab == null) go.AddComponent<GameManager>();
            }

            // 2. Network
            if (NetworkPrefab != null) Instantiate(NetworkPrefab);

            // 3. Audio
            var audioGo = new GameObject("AudioManager");
            var am = audioGo.AddComponent<Audio.MusicController>();
            DontDestroyOnLoad(audioGo);

            // 4. World (created when Game scene loads)
            // 5. Other services
            if (ServiceLocator.Get<FarmSystem>() == null)
                ServiceLocator.Register<FarmSystem>(new FarmSystem());
            if (ServiceLocator.Get<CraftingSystem>() == null)
                ServiceLocator.Register<CraftingSystem>(new CraftingSystem());
            if (ServiceLocator.Get<QuestSystem>() == null)
                ServiceLocator.Register<QuestSystem>(new QuestSystem());
            if (ServiceLocator.Get<LODManager>() == null)
                ServiceLocator.Register<LODManager>(new LODManager { MainCamera = Camera.main });
            if (ServiceLocator.Get<AsyncAssetLoader>() == null)
                ServiceLocator.Register<AsyncAssetLoader>(new AsyncAssetLoader());
            if (ServiceLocator.Get<WorldEventSystem>() == null)
                ServiceLocator.Register<WorldEventSystem>(new WorldEventSystem());
        }

        private void Start()
        {
            // Register sample data (in production, this is loaded from Resources/Addressables)
            RegisterDefaultContent();
        }

        private void Update()
        {
            // Tick services that need it
            ServiceLocator.Get<LODManager>()?.Tick(Time.deltaTime);
            ServiceLocator.Get<WorldEventSystem>()?.Tick(Time.deltaTime);
            ServiceLocator.Get<FarmSystem>()?.Tick(Time.deltaTime, ServiceLocator.Get<TimeSystem>());
            ServiceLocator.Get<CraftingSystem>()?.Tick(Time.deltaTime, FindObjectOfType<PlayerInventory>());
            Audio.LeanTweenHelper.Tick(Time.deltaTime);
        }

        private void RegisterDefaultContent()
        {
            // Sample blocks
            var air = ScriptableObject.CreateInstance<BlockDefinition>();
            air.Id = 0; air.DisplayName = "Air"; air.Type = BlockType.Air; air.Solid = false;
            BlockDatabase.Register(air);

            var grass = ScriptableObject.CreateInstance<BlockDefinition>();
            grass.Id = 1; grass.DisplayName = "Grass"; grass.Solid = true; grass.Hardness = 1;
            BlockDatabase.Register(grass);

            var dirt = ScriptableObject.CreateInstance<BlockDefinition>();
            dirt.Id = 2; dirt.DisplayName = "Dirt"; dirt.Solid = true; dirt.Hardness = 1;
            BlockDatabase.Register(dirt);

            var stone = ScriptableObject.CreateInstance<BlockDefinition>();
            stone.Id = 3; stone.DisplayName = "Stone"; stone.Solid = true; stone.Hardness = 3;
            BlockDatabase.Register(stone);

            var bedrock = ScriptableObject.CreateInstance<BlockDefinition>();
            bedrock.Id = 4; bedrock.DisplayName = "Bedrock"; bedrock.Solid = true; bedrock.Mineable = false;
            BlockDatabase.Register(bedrock);

            // Sample items
            var wood = ScriptableObject.CreateInstance<ItemDefinition>();
            wood.Id = 100; wood.DisplayName = "Wood"; wood.Category = ItemCategory.Material;
            ItemDatabase.Register(wood);

            var stoneItem = ScriptableObject.CreateInstance<ItemDefinition>();
            stoneItem.Id = 101; stoneItem.DisplayName = "Stone"; stoneItem.Category = ItemCategory.Material;
            ItemDatabase.Register(stoneItem);

            var apple = ScriptableObject.CreateInstance<ItemDefinition>();
            apple.Id = 102; apple.DisplayName = "Apple"; apple.Category = ItemCategory.Food;
            apple.HealAmount = 5; apple.HungerRestored = 10;
            ItemDatabase.Register(apple);

            var sword = ScriptableObject.CreateInstance<ItemDefinition>();
            sword.Id = 200; sword.DisplayName = "Iron Sword"; sword.Category = ItemCategory.Weapon;
            sword.Damage = 15; sword.ToolType = ToolType.Sword;
            ItemDatabase.Register(sword);

            // Sample biomes
            var plains = ScriptableObject.CreateInstance<BiomeDefinition>();
            plains.Type = BiomeType.Plains; plains.DisplayName = "Plains";
            plains.SurfaceBlockId = 1; plains.SubSurfaceBlockId = 2; plains.FillerBlockId = 3;
            BiomeDB.Register(plains);

            var forest = ScriptableObject.CreateInstance<BiomeDefinition>();
            forest.Type = BiomeType.Forest; forest.DisplayName = "Forest";
            forest.TreeDensity = 0.1f;
            BiomeDB.Register(forest);

            // Sample recipe
            var woodPlank = ScriptableObject.CreateInstance<RecipeDefinition>();
            woodPlank.Id = 1; woodPlank.DisplayName = "Wood Plank";
            woodPlank.ResultItemId = 100; woodPlank.ResultAmount = 4;
            woodPlank.Ingredients = new[] { new Ingredient { itemId = 100, amount = 1 } }; // wood → planks
            RecipeDatabase.Register(woodPlank);

            // Sample quest
            var q = ScriptableObject.CreateInstance<QuestDefinition>();
            q.Id = "welcome_1"; q.DisplayName = "Welcome to Aria";
            q.Type = QuestType.Main;
            q.Description = "Gather 5 wood.";
            var obj = ScriptableObject.CreateInstance<QuestObjectiveDef>();
            obj.Id = "obj_wood"; obj.Type = ObjectiveType.Mine; obj.RequiredAmount = 5;
            obj.Description = "Gather 5 wood";
            q.Objectives = new[] { obj };
            q.XpReward = 50; q.MoneyReward = 10;
            QuestDatabase.Register(q);
        }
    }
}
