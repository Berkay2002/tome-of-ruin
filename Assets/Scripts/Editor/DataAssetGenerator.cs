#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class DataAssetGenerator
{
    [MenuItem("Tools/Generate Data Assets")]
    public static void GenerateAll()
    {
        CreateDirectories();
        CreateHarmonyTable();
        CreateAttacks();
        CreateComboBooks();
        CreateEnemyData();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All data assets generated!");
    }

    private static void CreateDirectories()
    {
        EnsureDirectory("Assets/Data");
        EnsureDirectory("Assets/Data/Attacks");
        EnsureDirectory("Assets/Data/ComboBooks");
        EnsureDirectory("Assets/Data/Enemies");
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static T CreateAsset<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void CreateHarmonyTable()
    {
        var table = CreateAsset<HarmonyTable>("Assets/Data/HarmonyTable.asset");
        table.entries = new HarmonyEntry[]
        {
            new HarmonyEntry { fromTag = AttackTag.Rising, toTag = AttackTag.Slam, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Slam, toTag = AttackTag.Sweep, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Sweep, toTag = AttackTag.Overhead, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Overhead, toTag = AttackTag.Thrust, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Thrust, toTag = AttackTag.Rising, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Spinning, toTag = AttackTag.Sweep, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Sweep, toTag = AttackTag.Spinning, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Slam, toTag = AttackTag.Rising, level = HarmonyLevel.Dissonant },
            new HarmonyEntry { fromTag = AttackTag.Rising, toTag = AttackTag.Rising, level = HarmonyLevel.Dissonant },
            new HarmonyEntry { fromTag = AttackTag.Slam, toTag = AttackTag.Slam, level = HarmonyLevel.Dissonant },
            new HarmonyEntry { fromTag = AttackTag.Overhead, toTag = AttackTag.Overhead, level = HarmonyLevel.Dissonant },
            new HarmonyEntry { fromTag = AttackTag.Spinning, toTag = AttackTag.Spinning, level = HarmonyLevel.Dissonant },
        };
        EditorUtility.SetDirty(table);
    }

    private static AttackData CreateAttack(string name, AttackTag primary, AttackTag secondary,
        float damage, AttackSpeed speed, MovementPattern movement)
    {
        string safeName = name.Replace(" ", "");
        var attack = CreateAsset<AttackData>($"Assets/Data/Attacks/{safeName}.asset");
        attack.attackName = name;
        attack.primaryTag = primary;
        attack.secondaryTag = secondary;
        attack.baseDamage = damage;
        attack.speed = speed;
        attack.movementPattern = movement;
        EditorUtility.SetDirty(attack);
        return attack;
    }

    private static void CreateAttacks()
    {
        CreateAttack("Quick Slash", AttackTag.Sweep, AttackTag.None, 8, AttackSpeed.Fast, MovementPattern.HoldPosition);
        CreateAttack("Lunging Thrust", AttackTag.Thrust, AttackTag.None, 12, AttackSpeed.Medium, MovementPattern.LungeForward);
        CreateAttack("Overhead Chop", AttackTag.Overhead, AttackTag.None, 15, AttackSpeed.Slow, MovementPattern.HoldPosition);
        CreateAttack("Rising Cut", AttackTag.Rising, AttackTag.None, 10, AttackSpeed.Medium, MovementPattern.HoldPosition);
        CreateAttack("Ground Slam", AttackTag.Slam, AttackTag.None, 18, AttackSpeed.Slow, MovementPattern.HoldPosition);
        CreateAttack("Spinning Slash", AttackTag.Spinning, AttackTag.None, 14, AttackSpeed.Medium, MovementPattern.CircleArc);
        CreateAttack("Viper Strike", AttackTag.Thrust, AttackTag.Rising, 11, AttackSpeed.Fast, MovementPattern.LungeForward);
        CreateAttack("Reaping Arc", AttackTag.Sweep, AttackTag.Slam, 16, AttackSpeed.Slow, MovementPattern.CircleArc);
        CreateAttack("Skyward Rend", AttackTag.Rising, AttackTag.Overhead, 13, AttackSpeed.Medium, MovementPattern.HoldPosition);
        CreateAttack("Whirlwind", AttackTag.Spinning, AttackTag.Sweep, 12, AttackSpeed.Medium, MovementPattern.CircleArc);
        CreateAttack("Executioner", AttackTag.Overhead, AttackTag.Slam, 20, AttackSpeed.Slow, MovementPattern.LungeForward);
        CreateAttack("Shadow Step", AttackTag.Thrust, AttackTag.Spinning, 9, AttackSpeed.Fast, MovementPattern.PullBack);
    }

    private static void CreateComboBooks()
    {
        // Load attacks for pre-configured books
        var quickSlash = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/Data/Attacks/QuickSlash.asset");
        var lungingThrust = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/Data/Attacks/LungingThrust.asset");
        var risingCut = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/Data/Attacks/RisingCut.asset");
        var groundSlam = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/Data/Attacks/GroundSlam.asset");
        var overheadChop = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/Data/Attacks/OverheadChop.asset");

        // Tattered Manual (Common, pre-configured)
        var tatteredManual = CreateAsset<ComboBookData>("Assets/Data/ComboBooks/TatteredManual.asset");
        tatteredManual.bookName = "Tattered Manual";
        tatteredManual.rarity = ComboBookRarity.Common;
        tatteredManual.ForceInitSlots();
        tatteredManual.SetAttack(0, quickSlash);
        tatteredManual.SetAttack(1, lungingThrust);
        EditorUtility.SetDirty(tatteredManual);

        // Blank Tome (Common, empty)
        var blankTome = CreateAsset<ComboBookData>("Assets/Data/ComboBooks/BlankTome.asset");
        blankTome.bookName = "Blank Tome";
        blankTome.rarity = ComboBookRarity.Common;
        blankTome.ForceInitSlots();
        EditorUtility.SetDirty(blankTome);

        // Knight's Codex (Rare, pre-configured)
        var knightsCodex = CreateAsset<ComboBookData>("Assets/Data/ComboBooks/KnightsCodex.asset");
        knightsCodex.bookName = "Knight's Codex";
        knightsCodex.rarity = ComboBookRarity.Rare;
        knightsCodex.ForceInitSlots();
        knightsCodex.SetAttack(0, risingCut);
        knightsCodex.SetAttack(1, groundSlam);
        knightsCodex.SetAttack(2, quickSlash);
        EditorUtility.SetDirty(knightsCodex);

        // Wanderer's Journal (Rare, empty)
        var wanderersJournal = CreateAsset<ComboBookData>("Assets/Data/ComboBooks/WanderersJournal.asset");
        wanderersJournal.bookName = "Wanderer's Journal";
        wanderersJournal.rarity = ComboBookRarity.Rare;
        wanderersJournal.ForceInitSlots();
        EditorUtility.SetDirty(wanderersJournal);

        // Forgotten Manuscript (Rare, empty)
        var forgottenManuscript = CreateAsset<ComboBookData>("Assets/Data/ComboBooks/ForgottenManuscript.asset");
        forgottenManuscript.bookName = "Forgotten Manuscript";
        forgottenManuscript.rarity = ComboBookRarity.Rare;
        forgottenManuscript.ForceInitSlots();
        EditorUtility.SetDirty(forgottenManuscript);

        // Legendary Swordmaster's Opus (Legendary, pre-configured)
        var opus = CreateAsset<ComboBookData>("Assets/Data/ComboBooks/SwordmastersOpus.asset");
        opus.bookName = "Swordmaster's Opus";
        opus.rarity = ComboBookRarity.Legendary;
        opus.ForceInitSlots();
        opus.SetAttack(0, risingCut);
        opus.SetAttack(1, groundSlam);
        opus.SetAttack(2, quickSlash);
        opus.SetAttack(3, overheadChop);
        EditorUtility.SetDirty(opus);
    }

    private static void CreateEnemyData()
    {
        CreateEnemy("Hollow", 30, 1.5f, 8, 1.0f, 2.0f, 5, 15, 0.5f, 0, false, 0.4f);
        CreateEnemy("Wraith", 20, 4.0f, 12, 1.2f, 1.0f, 7, 10, 0.5f, 0, false, 0.3f);
        CreateEnemy("Knight", 80, 1.0f, 15, 1.5f, 2.5f, 5, 40, 0.5f, 0.5f, true, 0.6f);
        CreateEnemy("Caster", 25, 1.5f, 10, 6.0f, 2.0f, 8, 12, 0.5f, 0, false, 0.3f);
        CreateEnemy("Boss", 200, 2.0f, 20, 2.0f, 3.0f, 10, 80, 0.5f, 0.3f, true, 0.7f);
    }

    private static void CreateEnemy(string name, float hp, float speed, float damage,
        float range, float cooldown, float detection, float staggerThreshold,
        float staggerDuration, float armor, bool armorOffStagger, float navRadius = 0.4f)
    {
        var enemy = CreateAsset<EnemyData>($"Assets/Data/Enemies/{name}.asset");
        enemy.enemyName = name;
        enemy.maxHealth = hp;
        enemy.moveSpeed = speed;
        enemy.attackDamage = damage;
        enemy.attackRange = range;
        enemy.attackCooldown = cooldown;
        enemy.detectionRange = detection;
        enemy.staggerThreshold = staggerThreshold;
        enemy.staggerDuration = staggerDuration;
        enemy.armorReduction = armor;
        enemy.armorDisabledDuringStagger = armorOffStagger;
        enemy.navMeshRadius = navRadius;
        EditorUtility.SetDirty(enemy);
    }
}
#endif
