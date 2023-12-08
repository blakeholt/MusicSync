using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace AMusicSync {
    [BepInPlugin(modGUID, modName, modVersion)]
    public class MusicSyncBase : BaseUnityPlugin {
        // Mods are loaded Alphabetically, so thats why it's 'A'MusicSync
        public const string modGUID = "AMusicSync";
        public const string modName = "AMusicSync";
        public const string modVersion = "1.0.0";

        public string baseUrl = "";
        public string manifestPath = "";
        public string songDestinationPath = "";
        public bool showConsole = true;

        Process cmd = new Process();
        private readonly Harmony harmony = new Harmony(modGUID);
        private static MusicSyncBase Instance;
        internal ManualLogSource mls;

        void Awake() {
            // Initialize the instance should it not exist.
            if (Instance == null) {
                Instance = this;
            }
            // Setup the logger
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            // Define url/path values
            manifestPath = "manifest.json";
            baseUrl = "https://www.holt-tech.systems/LethalCompany/";
            songDestinationPath = @"BepInEx\Custom Songs\Boombox Music\";

            // Setup console should it be required
            if (showConsole) {
                cmd = new Process();
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.Start();
                WriteLine(cmd, @"prompt :");
                WriteLine(cmd, "cls");
                WriteLine(cmd, @"echo Starting AMusicSync");
                WriteLine(cmd, @"echo PLEASE DON'T CLOSE THIS WINDOW!");
            }

            // Delete existing song files
            Log("[DELETE] Deleting existing songs");
            foreach (var file in Directory.GetFiles(songDestinationPath)) {
                var chunks = file.Split('\\');
                var fileName = chunks[chunks.Length - 1];
                try {
                    File.Delete(GetAbsoluteCustomSongDirectory() + fileName);
                    Log("[DELETE] Deleted " + fileName);
                } catch (Exception ex) {
                    Log(ex);
                }
            }

            // Process songs
            Log("AMusicSync initializing");
            int numOfSongs = 0;
            using (var client = new WebClient()) {
                try {
                    // Download manifest and parse the song URLs from it
                    client.DownloadFile(baseUrl + manifestPath, GetRelativeModDirectory() + "manifest.json");
                    string data = File.ReadAllText(GetAbsoluteModDirectory() + "manifest.json");
                    List<Song> songs = JsonConvert.DeserializeObject<List<Song>>(data);
                    numOfSongs = songs.Count;

                    // Download songs
                    Log("[DL] Downloading songs to " + GetAbsoluteModDirectory());
                    foreach (Song song in songs) {
                        client.DownloadFile(baseUrl + song.path, GetRelativeModDirectory() + song.path);
                        Log("[DL] Finished downloading: " + song.path);
                    }
                    Log("[DL] AMusicSync has finished downloading " + numOfSongs + " songs");

                    // Move downloaded songs to custom boombox directory
                    Log("[MOVE] Moving " + numOfSongs + " songs to " + songDestinationPath);
                    foreach (Song song in songs) {
                        try {
                            Log($"[MOVE] Moving: {song.path} to {GetAbsoluteCustomSongDirectory()}");
                            File.Move(GetAbsoluteModDirectory() + song.path, GetAbsoluteCustomSongDirectory() + song.path);
                        } catch (Exception ex) {
                            Log(ex);
                        }
                    }
                } catch (Exception ex) {
                    Log(ex);
                }
            }
            
            Log("AMusicSync has finished initializing");
            harmony.PatchAll(typeof(MusicSyncBase));

            // Hide console if it was displayed
            if (showConsole) {
                cmd.StandardInput.Flush();
                cmd.StandardInput.Close();
                cmd.WaitForExit();
                Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            }
        }

        // Log messages
        void Log(string text) {
            mls.LogInfo(text);
            WriteLine(cmd, "cls");
            WriteLine(cmd, @"type .\BepInEx\LogOutput.log");
        }
        // Log 
        void Log(Exception ex) {
            mls.LogError(ex.Message);
            WriteLine(cmd, "cls");
            WriteLine(cmd, @"type .\BepInEx\LogOutput.log");
        }
        void WriteLine(Process cmd, string message) {
            if (showConsole) {
                try {
                    cmd.StandardInput.WriteLine(message);
                } catch (Exception ex) {
                    Log(ex);
                }
            }
        }

        // Get Directory Paths
        string GetAbsoluteCustomSongDirectory() => Directory.GetCurrentDirectory() + @"\" + @songDestinationPath;
        string GetAbsoluteModDirectory() => Directory.GetCurrentDirectory() + @"\" + GetRelativeModDirectory();
        string GetRelativeModDirectory() => @"BepInEx\plugins\AMusicSync\";

    }
    
    // Song Class

    public class Song {
        public string path { get; set; }
        public string checksum { get; set; }
    }
}
