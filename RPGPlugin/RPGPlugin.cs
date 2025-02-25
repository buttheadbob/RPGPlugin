﻿using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml.Serialization;
using RPGPlugin.PointManagementSystem;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Managers.PatchManager;
using Torch.Session;
using VRage.GameServices;
using RPGPlugin.Utils;
using Torch.Collections;
using Timer = System.Timers.Timer;


namespace RPGPlugin
{
    public class Roles : TorchPluginBase, IWpfPlugin
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static ConcurrentDictionary<ulong, PlayerManager> PlayerManagers = new ConcurrentDictionary<ulong, PlayerManager>();
        public static Dictionary<string, configBase> classConfigs = new Dictionary<string, configBase>();
        public static Dictionary<string, ClassesBase> roles = new Dictionary<string, ClassesBase>();
        public MtObservableSortedDictionary<string, UserControl> classViews = new MtObservableSortedDictionary<string, UserControl>();
        public readonly Dispatcher MainDispatcher = Dispatcher.CurrentDispatcher;
        private Timer _delayManagers = new Timer(TimeSpan.FromSeconds(5).TotalMilliseconds);
        public PatchManager patchManager;
        public PatchContext patchContext;
        public static IChatManagerServer ChatManager => Instance.Torch.CurrentSession.Managers.GetManager<IChatManagerServer>();

        public static Roles Instance { get; private set; }
        public bool ServerOnline;
        public bool DelayFinished;

        public RolesControl _control;

        public UserControl GetControl() => _control ?? (_control = new RolesControl(this));
        private Persistent<RPGPluginConfig> _config;
        public RPGPluginConfig Config => _config?.Data;

        public override async void Init(ITorchBase torch)
        {
            base.Init(torch);
            Roles.Log.Warn($"MainPlugin Thread => {Thread.CurrentThread.ManagedThreadId}");
            Instance = this;
            await SetupConfig();

            _delayManagers.Stop();
            _delayManagers.Elapsed += DelayManagersOnElapsed;
            TorchSessionManager sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            patchManager = torch.Managers.GetManager<PatchManager>();
            patchContext = patchManager.AcquireContext();
            DrillPatch.Patch(patchContext);
            await Save();
            await RoleAgent.LoadAllRoles();; // Give the system time to write and release the saved config file
        }

        private void DelayManagersOnElapsed(object sender, ElapsedEventArgs e)
        {
            Log.Warn("Delay Timer has finished.");
            if (!ServerOnline) return;
            if (MySession.Static.SessionSimSpeedServer < 0.7) return;
            _delayManagers.Stop();
            _delayManagers.Elapsed -= DelayManagersOnElapsed;

            foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
            {
                PlayerManager _pm = new PlayerManager();
                _pm.InitAsync(player.Id.SteamId);
                Log.Warn("Loaded data for player " + player.DisplayName);
                if (!PlayerManagers.TryAdd(player.Id.SteamId, _pm))
                {
                    Log.Error($"Player {player.DisplayName} [{player.Id.SteamId}] datafile could not be loaded.");
                }
            }

            MyMultiplayer.Static.ClientJoined += PlayerConnected;
            MyMultiplayer.Static.ClientLeft += PlayerDisconnected;
            DelayFinished = true;
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {
                case TorchSessionState.Loaded:
                    Log.Info("Session Loaded!");
                    _delayManagers.Start();
                    ServerOnline = true;
                    RoleAgent.OnLoaded();
                    break;

                case TorchSessionState.Unloading:
                    ServerOnline = false;
                    Log.Info("Session Unloading!");
                    MyMultiplayer.Static.ClientJoined -= PlayerConnected;
                    MyMultiplayer.Static.ClientLeft -= PlayerDisconnected;
                    SaveAllPlayersForShutDown();
                    RoleAgent.Unload();
                    break;
            }
        }

        private async void PlayerDisconnected(ulong steamID, MyChatMemberStateChangeEnum myChatMemberStateChangeEnum)
        {
            // Unload them from the system, free up resources.
            if (!PlayerManagers.ContainsKey(steamID))
            {
                Log.Error($"Unable to save profile for player [SteamID:{steamID}], it was probably not loaded.");
                return;
            }
            await PlayerManagers[steamID].SavePlayerData();
            PlayerManagers.TryRemove(steamID, out PlayerManager _);
        }

        private async void SaveAllPlayersForShutDown()
        {
            foreach (KeyValuePair<ulong, PlayerManager> manager in PlayerManagers)
            {
                await manager.Value.SavePlayerData();
                Log.Info("Roles Tracking Finished For " + manager.Key);
            }
            PlayerManagers.Clear();
        }

        private static void PlayerConnected(ulong steamID, string s)
        {
            PlayerManager roleManager = new PlayerManager();
            roleManager.InitAsync(steamID);
            PlayerManagers.TryAdd(steamID, roleManager);
        }

        private async Task SetupConfig()
        {
            string dataPath = Path.Combine(StoragePath, "RPGPlugin");
            string playerDataPath = Path.Combine(dataPath, "Player Data");

            // create directories if they do not exist
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(playerDataPath);
            string configFile = Path.Combine(StoragePath, "RPGPluginConfig.xml");

            if (!File.Exists(configFile))
            {
                _config = new Persistent<RPGPluginConfig>(configFile);
                _config.Save();
            }
            else
            {
                try
                {
                    _config = Persistent<RPGPluginConfig>.Load(configFile);
                }
                catch (Exception e)
                {
                    Log.Warn(e);
                }
                
                if (_config?.Data == null)
                {
                    Log.Info("Create Default Config, because none was found!");
                    _config = new Persistent<RPGPluginConfig>(configFile, new RPGPluginConfig());
                    _config.Save();
                }
                else
                {
                    try
                    {
                        var xmlSerializer = new XmlSerializer(typeof(RPGPluginConfig));
                        using (var streamReader = new StreamReader(configFile))
                        {
                            var configData = (RPGPluginConfig)xmlSerializer.Deserialize(streamReader);
                            _config = new Persistent<RPGPluginConfig>(configFile, configData);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warn(e);
                    }
                    await Save();
                }
            }
        }

        public Task Save()
        {
            try
            {
                _config.Save();                
                Log.Info("Main Configuration Saved.");
                return Task.CompletedTask;
            }
            catch (IOException e)
            {
                Log.Warn(e, "Main Configuration Saved during plugin loading.");
                return Task.FromException<IOException>(e);
            }
        }

        private async Task WaitThenSetupClasses()
        {
            // Had issues were roles were loaded faster than the config was created... causes issues.
            await Task.Run(async () =>
            {
                while (Config == null)
                {
                    // If the config is null, wait a second and try again. Repeat until config is loaded.
                    Thread.Sleep(1000);
                }

                await RoleAgent.LoadAllRoles();
            });
            
        }
    }
}
