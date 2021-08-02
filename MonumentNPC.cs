using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Monument NPC Control", "RFC1920", "1.0.1")]
    [Description("Autokill NPCs spawned by Rust to protect monument puzzles.")]
    class MonumentNPC : RustPlugin
    {
        private ConfigData configData;
        public List<string> monNames = new List<string>();
        public SortedDictionary<string, Vector3> monPos  = new SortedDictionary<string, Vector3>();
        public SortedDictionary<string, Vector3> monSize = new SortedDictionary<string, Vector3>();

        private void OnServerInitialized()
        {
            FindMonuments();
        }

        private void Init()
        {
            LoadConfigVariables();
        }

        private void OnEntitySpawned(ScientistNPCNew sci)
        {
            if (configData.debug) Puts("ScientistNPCNew Spawned");
            foreach (KeyValuePair<string, Vector3> mondata in monPos)
            {
                if (mondata.Key == null) continue;
                if (!configData.killAtAllMonuments)
                {
                    if (!configData.killMonuments.Contains(mondata.Key))
                    {
                        continue;
                    }
                }
                if (Vector3.Distance(sci.transform.position, mondata.Value) < monSize[mondata.Key].z)
                {
                    if (configData.debug) Puts($"Too close to {mondata.Key}.  Killing...");
                    sci.Kill();
                    return;
                }
            }
            if (configData.debug) Puts("Not in range of any monuments.");
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
                        MatchCollection elem = Regex.Matches(monument.name, @"\w{4,}|\d{1,}");
                        foreach (Match e in elem)
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
