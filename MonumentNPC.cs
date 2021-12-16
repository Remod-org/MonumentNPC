using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Monument NPC Control", "RFC1920", "1.0.3")]
    [Description("Autokill NPCs spawned by Rust to protect monument puzzles, etc.")]
    internal class MonumentNPC : RustPlugin
    {
        [PluginReference]
        private readonly Plugin MonBots, BotReSpawn;

        private ConfigData configData;
        public List<string> monNames = new List<string>();
        public SortedDictionary<string, Vector3> monPos  = new SortedDictionary<string, Vector3>();
        public SortedDictionary<string, Vector3> monSize = new SortedDictionary<string, Vector3>();

        private void OnServerInitialized()
        {
            FindMonuments();

            if (configData.killOnStartup)
            {
                foreach (KeyValuePair<string, Vector3> mondata in monPos)
                {
                    if (mondata.Key == null) continue;
                    if (configData.debug) Puts($"Checking monument {mondata.Key} with size {monSize[mondata.Key].z.ToString()}");

                    List<ScientistNPC> localsci = new List<ScientistNPC>();
                    Vis.Entities(mondata.Value, monSize[mondata.Key].z, localsci);
                    foreach (ScientistNPC sci in localsci)
                    {
                        CheckAndKill(sci);
                    }
                }
            }
        }

        private void Init()
        {
            LoadConfigVariables();
            AdjustBanditProtections();
        }

        private void AdjustBanditProtections()
        {
            if (!configData.BanditGuardDamage) return;
            foreach (BanditGuard sci in UnityEngine.Object.FindObjectsOfType<BanditGuard>())
            {
                if (configData.debug) Puts("Found BanditGuard.  Adjusting protections...");
                SetProtection(sci);
            }
        }

        private void SetProtection(BanditGuard sci)
        {
            Dictionary<DamageType, float> protections = new Dictionary<DamageType, float>
            {
                { DamageType.Bite, 1f },
                { DamageType.Cold, 1f },
                { DamageType.ColdExposure, 1f },
                { DamageType.Decay, 1f },
                { DamageType.ElectricShock, 1f },
                { DamageType.Hunger, 1f },
                { DamageType.Radiation, 1f },
                { DamageType.RadiationExposure, 1f }
            };
            sci.baseProtection.Clear();
            foreach (var protection in protections)
            {
                sci.baseProtection.Add(protection.Key, protection.Value);
            }
            sci.startHealth = 100f;
            sci.health = 100f;
            sci.SendNetworkUpdateImmediate();
        }

        private void OnEntitySpawned(BanditGuard sci)
        {
            if (!configData.BanditGuardDamage) return;
            if (configData.debug) Puts("BanditGuard spawning.  Adjusting protections...");
            SetProtection(sci);
        }

        private bool CheckBotPlugins(ScientistNPC sci)
        {
            bool monbot = false;
            bool botrespawn = false;

            if (MonBots)
            {
                monbot = (bool)MonBots?.Call("IsMonBot", sci);
            }
            if (BotReSpawn)
            {
                // No idea if this exists
                botrespawn = (bool)BotReSpawn?.Call("IsBotReSpawn", sci);
            }
            return monbot || botrespawn;
        }

        private void CheckAndKill(ScientistNPC sci)
        {
            foreach (KeyValuePair<string, Vector3> mondata in monPos)
            {
                //if (configData.debug) Puts($"Checking monument {mondata.Key}");
                if (mondata.Key == null) continue;
                if (!configData.killAtAllMonuments && !configData.killMonuments.Contains(mondata.Key))
                {
                    continue;
                }
                if (Vector3.Distance(sci.transform.position, mondata.Value) < monSize[mondata.Key].z)
                {
                    if (!CheckBotPlugins(sci))
                    {
                        if (configData.debug) Puts($"Too close to {mondata.Key}.  Killing...");
                        sci.Kill();
                        return;
                    }
                }
            }
            if (configData.debug) Puts("Not in range of any monuments.");
        }

        private void OnEntitySpawned(ScientistNPC sci)
        {
            if (configData.debug) Puts("ScientistNPCNew Spawned");
            timer.Once(1, () => CheckAndKill(sci));
        }

        private void FindMonuments()
        {
            Vector3 extents = Vector3.zero;
            float realWidth = 0f;
            string name = null;
            bool ishapis =  ConVar.Server.level.Contains("Hapis");

            foreach (MonumentInfo monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                if (monument.name.Contains("power_sub")) continue;// || monument.name.Contains("cave")) continue;
                realWidth = 0f;
                name = null;

                if (monument.name == "OilrigAI")
                {
                    name = "Small Oilrig";
                }
                else if (monument.name == "OilrigAI2")
                {
                    name = "Large Oilrig";
                }
                else
                {
                    if (ishapis)
                    {
                        foreach (Match e in Regex.Matches(monument.name, @"\w{4,}|\d{1,}"))
                        {
                            if (e.Value.Equals("MONUMENT")) continue;
                            if (e.Value.Contains("Label")) continue;
                            name += e.Value + " ";
                        }
                        name = name.Trim();
                    }
                    else
                    {
                        name = Regex.Match(monument.name, @"\w{6}\/(.+\/)(.+)\.(.+)").Groups[2].Value.Replace("_", " ").Replace(" 1", "").Titleize();
                    }
                }
                if (monPos.ContainsKey(name)) continue;

                extents = monument.Bounds.extents;

                if (realWidth > 0f)
                {
                    extents.z = realWidth;
                }

                if (extents.z < 1)
                {
                    extents.z = 100f;
                }
                if (!configData.allMonuments.Contains(name) || configData.allMonuments == null)
                {
                    configData.allMonuments.Add(name);
                }
                monNames.Add(name);
                monPos.Add(name, monument.transform.position);
                monSize.Add(name, extents);
            }
            monPos.OrderBy(x => x.Key);
            monSize.OrderBy(x => x.Key);
            SaveConfig(configData);
        }

        #region config
        public class ConfigData
        {
            public bool debug;
            public bool killAtAllMonuments;
            public bool killOnStartup;
            public bool BanditGuardDamage;
            public List<string> killMonuments = new List<string>();
            public List<string> allMonuments = new List<string>();
            public VersionNumber Version;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                debug = false,
                BanditGuardDamage = true,
                killAtAllMonuments = false,
                killMonuments = new List<string>() { "Airfield", "Trainyard" },
                Version = Version
            };
            SaveConfig(config);
        }

        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();
            configData.Version = Version;
            SaveConfig(configData);
        }

        private void SaveConfig(ConfigData config)
        {
            config.allMonuments.Sort();
            config.killMonuments.Sort();
            Config.WriteObject(config, true);
        }
        #endregion
    }
}
