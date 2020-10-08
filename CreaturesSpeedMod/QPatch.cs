using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CreaturesSpeedMod
{
    public class CreatureSpeedConfig
    {
        public SwimRandom SwimRandom;
        public long TechTypeHash;
        public float OriginVelocity;

        private CreatureSpeedConfig() { }

        public CreatureSpeedConfig(SwimRandom swimRandom, long techTypeHash, float originVelocity)
        {
            this.SwimRandom = swimRandom;
            this.TechTypeHash = techTypeHash;
            this.OriginVelocity = originVelocity;
        }
    }

    public class QPatch
    {
        #region Attributes

        private static bool _success = true;
        private static string _configFilePath = null;

        private const long FallbackSpeedHash = 42L;

        private static readonly List<CreatureSpeedConfig> _creaturesSpeeds = new List<CreatureSpeedConfig>();
        private static readonly Dictionary<long, float> _speedMultipliers = new Dictionary<long, float>();
        private static readonly Dictionary<long, string> _speedMultipliersConfigs = new Dictionary<long, string>()
        {
            { FallbackSpeedHash, "FallbackMultiplier=" },
            { -9222895153685889375, "BoneSharkMultiplier=" },
            { -8650692634683516217, "GhostLeviathanMultiplier=" },
            { -860861341102293204, "JellyrayMultiplier=" },
            { -8603840260252117598, "LavaBoomerangMultiplier=" },
            { -7353250366602161040, "CrabsnakeMultiplier=" },
            { -6821065272940014353, "ShockerMultiplier=" },
            { -6396303725056045888, "ReginaldMultiplier=" },
            { -5586742206258613929, "SeaEmperorJuvenileMultiplier=" },
            { -5087581445772717815, "SpineEelMultiplier=" },
            { -5002774883622721815, "SkyrayMultiplier=" },
            { -4894773863602338016, "HoopfishMultiplier=" },
            { -4864942431443757508, "BladderfishMultiplier=" },
            { -4013101004226671710, "BoomerangMultiplier=" },
            { -3027371778130962826, "GhostRayRedMultiplier=" },
            { -2951106947520167353, "BleederMultiplier=" },
            { -2750372656681514868, "StalkerMultiplier=" },
            { -2666548167821737098, "LavaLizardMultiplier=" },
            { -2411026174175892643, "RockgrubMultiplier=" },
            { -2339152633218566536, "HoleFishMultiplier=" },
            { -2088596011540014035, "PeeperMultiplier=" },
            { -1752164186392880645, "SpinefishMultiplier=" },
            { -1552686018404844735, "BlighterMultiplier=" },
            { -1173831925965465219, "SpadeFishMultiplier=" },
            { -1283742229020934604, "HoopfishSchoolMultiplier=" },
            { 8821251374207114274, "CrabSquidMultiplier=" },
            { 8749912432322200212, "HoverfishMultiplier=" },
            { 8651845265590541008, "RabbitRayMultiplier=" },
            { 8261883654424644223, "SeaEmperorBabyMultiplier=" },
            { 7915722001263501851, "ReefbackMultiplier=" },
            { 783499821329814287, "GasopodMultiplier=" },
            { 782781889069386653, "LavaEyeyeMultiplier=" },
            { 6076058333300221525, "GhostLeviathanJuvenileMultiplier=" },
            { 5841473255195585986, "SeaDragonMultiplier=" },
            { 5742264671429085883, "JumperMultiplier=" },
            { 5355236626084479911, "ReaperLeviathanMultiplier=" },
            { 5080907374028123189, "GhostRayBlueMultiplier=" },
            { 5038110139519585993, "OculusMultiplier=" },
            { 4935505986072052125, "EyeyeMultiplier=" },
            { 4791461876223233767, "CrashMultiplier=" },
            { 4717887330382265434, "BiterMultiplier=" },
            { 2528890417390748535, "GarryFishMultiplier=" },
            { 2121366249118753429, "WarperMultiplier=" },
            { 1310774392623391521, "SandsharkMultiplier=" },
            { 1273245325188782984, "LavaLarvaMultiplier=" },
            { 1261384871725249631, "MesmerMultiplier=" }
        };

        #endregion

        #region Private functions

        private static string ConfigFilePath { get => _configFilePath ?? (_configFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Config.txt")); }

        private static void Log(string str) => Console.WriteLine("[CreaturesSpeedMod] " + str);

        private static float GetSpeedMultiplierFromTechTypeHash(long techTypeHash)
        {
#if DEBUG
            Log("DEBUG: GetSpeedMultiplierFromTechTypeHash: techType Hash=[" + techTypeHash.ToString(CultureInfo.InvariantCulture) + "] Name=[" + (_allTechTypes != null && _allTechTypes.ContainsKey(techTypeHash) ? _allTechTypes[techTypeHash].AsString(false) : "?") + "]");
#endif
            return _speedMultipliers.ContainsKey(techTypeHash) ? _speedMultipliers[techTypeHash] : _speedMultipliers[FallbackSpeedHash];
        }

#if DEBUG
        private static Dictionary<long, TechType> _allTechTypes = null;
        private static void DebugInit()
        {
            if (_allTechTypes == null)
            {
                Log("DEBUG: seaemperorjuvenile tt=[" + TechType.SeaEmperorJuvenile.AsString(false) + "] hash=[" + UWE.Utils.SDBMHash(TechType.SeaEmperorJuvenile.AsString(false)).ToString(CultureInfo.InvariantCulture) + "]");
                Log("DEBUG: Init techtypes dictionary");
                _allTechTypes = new Dictionary<long, TechType>();
                IEnumerable<TechType> tts = Enum.GetValues(typeof(TechType)).Cast<TechType>();
                if (tts != null)
                    foreach (TechType tt in tts)
                    {
                        string ttName = tt.AsString(false);
                        long ttHash = UWE.Utils.SDBMHash(ttName);
                        Log("DEBUG: TechType=[" + ttName + "] Hash=[" + ttHash.ToString(CultureInfo.InvariantCulture) + "]");
                        _allTechTypes.Add(ttHash, tt);
                    }
            }
        }
#endif

        private static void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    _speedMultipliers.Clear();
                    string[] lines = File.ReadAllLines(ConfigFilePath, Encoding.UTF8);
                    if (lines != null && lines.Length > 0)
                        foreach (string line in lines)
                            if (!string.IsNullOrWhiteSpace(line))
                                foreach (KeyValuePair<long, string> e in _speedMultipliersConfigs)
                                    if (line.StartsWith(e.Value))
                                    {
                                        string valStr = line.Substring(e.Value.Length);
                                        string creatureName = e.Value.Substring(0, e.Value.Length - 11);
                                        if (line.Length > e.Value.Length && float.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float speedMultiplier) && speedMultiplier > 0.0f && speedMultiplier < 20.0f)
                                        {
                                            _speedMultipliers.Add(e.Key, speedMultiplier);
                                            Log("INFO: Loaded " + creatureName + " speed multiplier: " + speedMultiplier.ToString(CultureInfo.InvariantCulture));
                                        }
                                        else
                                            Log("WARNING: Incorrect value [" + valStr + "] for " + creatureName + " speed multiplier.");
                                        break;
                                    }
                }
            }
            catch (Exception e)
            {
                Log(string.Format("ERROR: Exception caught while loading creatures speed multiplier! Message=[{0}] StackTrace=[{1}]", e.Message, e.StackTrace));
            }
        }

        #endregion

        #region Harmony patching

        public static void SwimToInternal_Prefix(SwimBehaviour __instance, Vector3 targetPosition, Vector3 targetDirection, ref float velocity, bool overshoot) //protected virtual void SwimToInternal(Vector3 targetPosition, Vector3 targetDirection, float velocity, bool overshoot)
        {
            TechType tt = CraftData.GetTechType(__instance.gameObject);
            if (tt != TechType.None)
            {
                long ttHash = UWE.Utils.SDBMHash(tt.AsString(false));
                velocity *= GetSpeedMultiplierFromTechTypeHash(ttHash);
#if DEBUG
                Log("DEBUG: SwimToInternal: name=[" + __instance.name + "] techtypeName=[" + tt.AsString(false) + "] techtypeHash=[" + ttHash.ToString(CultureInfo.InvariantCulture) + "]");
#endif
            }
            else
            {
                velocity *= GetSpeedMultiplierFromTechTypeHash(FallbackSpeedHash);
#if DEBUG
                Log("DEBUG: SwimToInternal: name=[" + __instance.name + "]");
#endif
            }
        }

        public static void Perform_Prefix(SwimRandom __instance, Creature creature, float deltaTime) //public override void Perform(Creature creature, float deltaTime)
        {
            long techTypeHash = creature.GetTechTypeHash();
            foreach (CreatureSpeedConfig c in _creaturesSpeeds)
                if (c.SwimRandom == __instance && c.TechTypeHash == techTypeHash)
                    return;
            _creaturesSpeeds.Add(new CreatureSpeedConfig(__instance, techTypeHash, __instance.swimVelocity));
            __instance.swimVelocity *= GetSpeedMultiplierFromTechTypeHash(techTypeHash); //_speedMultiplier;
        }

        #endregion

        public static void Patch()
        {
            Log("INFO: Initializing Creatures Speed mod...");
            try
            {
                Harmony HarmonyInstance = new Harmony("com.osubmarin.creaturesspeedmod");
                if (HarmonyInstance == null)
                {
                    Log("ERROR: Unable to initialize Harmony!");
                    return;
                }
                LoadConfig();
#if DEBUG
                DebugInit();
#endif
                var swimToInternalMethod = typeof(SwimBehaviour).GetMethod("SwimToInternal", BindingFlags.NonPublic | BindingFlags.Instance);
                var swimToInternalPrefix = typeof(QPatch).GetMethod("SwimToInternal_Prefix", BindingFlags.Public | BindingFlags.Static);
                HarmonyInstance.Patch(swimToInternalMethod, new HarmonyMethod(swimToInternalPrefix), null);
                var performMethod = typeof(SwimRandom).GetMethod("Perform", BindingFlags.Public | BindingFlags.Instance);
                var performPrefix = typeof(QPatch).GetMethod("Perform_Prefix", BindingFlags.Public | BindingFlags.Static);
                HarmonyInstance.Patch(performMethod, new HarmonyMethod(performPrefix), null);
            }
            catch (Exception e)
            {
                _success = false;
                Log(string.Format("ERROR: Exception caught! Message=[{0}] StackTrace=[{1}]", e.Message, e.StackTrace));
                if (e.InnerException != null)
                    Log(string.Format("ERROR: Inner exception => Message=[{0}] StackTrace=[{1}]", e.InnerException.Message, e.InnerException.StackTrace));
            }
            Log(_success ? "INFO: Creatures Speed mod initialized successfully." : "ERROR: Creatures Speed mod initialization failed.");
        }
    }
}
