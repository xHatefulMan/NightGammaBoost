using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace NightGammaBoost
{
    public static class GameDetector
    {
        public static readonly Dictionary<string, string> GameList = new Dictionary<string, string>
        {
            { "Rust",                  "RustClient"       },
            { "DayZ",                  "DayZ"             },
            { "Minecraft",             "javaw"            },
            { "The Forest",            "TheForest"        },
            { "Sons of The Forest",    "SonsOfTheForest"  },
            { "Valheim",               "valheim"          },
            { "Subnautica",            "Subnautica"       },
            { "Green Hell",            "GreenHell"        },
            { "Escape from Tarkov",    "EscapeFromTarkov" },
            { "Hunt Showdown",         "HuntGame"         },
            { "PUBG",                  "TslGame"          },
            { "Arma 3",                "arma3"            },
            { "Arma Reforger",         "ArmaReforger"     },
            { "Phasmophobia",          "Phasmophobia"     },
            { "Lethal Company",        "Lethal Company"   },
            { "The Long Dark",         "TheLongDark"      },
            { "7 Days to Die",         "7DaysToDie"       },
            { "Dying Light",           "DyingLightGame"   },
            { "Dying Light 2",         "DyingLight2"      },
            { "Red Dead Redemption 2", "RDR2"             },
            { "GTA V",                 "GTA5"             },
            { "Cyberpunk 2077",        "Cyberpunk2077"    },
            { "The Witcher 3",         "witcher3"         },
            { "Elden Ring",            "eldenring"        },
            { "Dark Souls 3",          "DarkSoulsIII"     },
            { "Sekiro",                "sekiro"           },
            { "Resident Evil 4",       "re4"              },
            { "Resident Evil 8",       "re8"              },
            { "Dead Space",            "Dead Space"       },
            { "Stranded Deep",         "Stranded Deep"    },
            { "Raft",                  "Raft"             },
            { "Conan Exiles",          "ConanSandbox"     },
        };

        public static List<string> GetInstalledGames()
        {
            var installedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ScanSteam(installedNames);
            ScanEpic(installedNames);
            ScanUbisoft(installedNames);
            ScanEA(installedNames);
            ScanGOG(installedNames);

            var result = new List<string>();
            foreach (var kvp in GameList)
                if (installedNames.Any(n =>
                    n.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    kvp.Key.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0))
                    result.Add(kvp.Key);

            return result;
        }

        private static void ScanSteam(HashSet<string> names)
        {
            try
            {
                string steamPath = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam",
                    "InstallPath", null) as string;

                if (steamPath == null)
                    steamPath = Registry.GetValue(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam",
                        "InstallPath", null) as string;

                if (steamPath == null) return;

                var folders = new List<string> { Path.Combine(steamPath, "steamapps") };

                string vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                if (File.Exists(vdf))
                    foreach (var line in File.ReadAllLines(vdf))
                        if (line.Contains("\"path\""))
                        {
                            var parts = line.Split('"');
                            if (parts.Length >= 4)
                            {
                                string apps = Path.Combine(
                                    parts[3].Replace("\\\\", "\\"), "steamapps");
                                if (Directory.Exists(apps)) folders.Add(apps);
                            }
                        }

                foreach (var folder in folders)
                {
                    if (!Directory.Exists(folder)) continue;
                    foreach (var acf in Directory.GetFiles(folder, "*.acf"))
                        foreach (var line in File.ReadAllLines(acf))
                            if (line.Contains("\"name\""))
                            {
                                var parts = line.Split('"');
                                if (parts.Length >= 4) names.Add(parts[3]);
                            }
                }
            }
            catch { }
        }

        private static void ScanEpic(HashSet<string> names)
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Epic", "EpicGamesLauncher", "Data", "Manifests");
                if (!Directory.Exists(path)) return;
                foreach (var file in Directory.GetFiles(path, "*.item"))
                    foreach (var line in File.ReadAllLines(file))
                        if (line.Contains("\"DisplayName\""))
                        {
                            var parts = line.Split('"');
                            if (parts.Length >= 4) names.Add(parts[3]);
                        }
            }
            catch { }
        }

        private static void ScanUbisoft(HashSet<string> names)
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Ubisoft Game Launcher", "games");
                if (!Directory.Exists(path)) return;
                foreach (var dir in Directory.GetDirectories(path))
                    names.Add(Path.GetFileName(dir));
            }
            catch { }
        }

        private static void ScanEA(HashSet<string> names)
        {
            try
            {
                string[] paths = {
                    @"C:\Program Files\EA Games",
                    @"C:\Program Files (x86)\Origin Games"
                };
                foreach (var p in paths)
                {
                    if (!Directory.Exists(p)) continue;
                    foreach (var dir in Directory.GetDirectories(p))
                        names.Add(Path.GetFileName(dir));
                }
            }
            catch { }
        }

        private static void ScanGOG(HashSet<string> names)
        {
            try
            {
                string gog = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient",
                    "clientExecutable", null) as string;
                if (gog == null) return;
                string games = Path.Combine(Path.GetDirectoryName(gog), "Games");
                if (!Directory.Exists(games)) return;
                foreach (var dir in Directory.GetDirectories(games))
                    names.Add(Path.GetFileName(dir));
            }
            catch { }
        }

        public static string GetRunningGame(IEnumerable<string> gamesToCheck)
        {
            var running = Process.GetProcesses()
                .Select(p => { try { return p.ProcessName; } catch { return ""; } })
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var game in gamesToCheck)
                if (GameList.TryGetValue(game, out string proc))
                    if (running.Contains(proc))
                        return game;

            return null;
        }
    }
}