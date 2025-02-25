﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RPGPlugin.Utils;

namespace RPGPlugin
{
    public sealed class WarriorConfig : configBase
    {
        // Definition of the ExpRatio property, which stores experience point values for individual minerals
        

        /// <inheritdoc />
        public override ObservableCollection<KeyValuePair<string, double>> ExpRatio { get; set; } = new ObservableCollection<KeyValuePair<string, double>>();

        //test skill point system
        public override ObservableCollection<KeyValuePair<int, int>> SkillPoints { get; set; } = new ObservableCollection<KeyValuePair<int, int>>();

        /// <inheritdoc />
        public override string ViewName { get; } = "Warrior";
        
        public override void init()
        {
            // Initialize class with default settings, will be replaced if a config file is loaded.
            ExpRatio.Add(new KeyValuePair<string, double>("EnemyPlayer",        25  ));
            ExpRatio.Add(new KeyValuePair<string, double>("EnemySmallBlock",    0.2 ));
            ExpRatio.Add(new KeyValuePair<string, double>("EnemyLargeBlock",    0.8 ));

            //test skill point system
            SkillPoints.Add(new KeyValuePair<int, int>(2, 1));
        }
        
        public override void RegisterClass()
        {
            SerializableTuple<string, string> RoleToRegister = new SerializableTuple<string, string>{Item1 = "Warrior", Item2 = "Specialized in battles and destruction of other engineers things!."};

            if (Roles.Instance.Config.RegisteredRoles.Any(Role => Role.Item1.Equals(RoleToRegister.Item1, StringComparison.OrdinalIgnoreCase))) return;
            
            Roles.Instance.Config.RegisteredRoles.Add(RoleToRegister);
            Roles.Log.Warn($"Registered New Class: {RoleToRegister.Item1}");
        }

        public override void LoadConfig()
        {
            string data = GetConfig().Result;
            if (data == null) return;
            WarriorConfig classConfig = JsonConvert.DeserializeObject<WarriorConfig>(data);
            ExpRatio = classConfig.ExpRatio;
        }

        public override async Task SaveConfig()
        {
            string jsonData = JsonConvert.SerializeObject(this, Formatting.Indented);
            await SaveConfig(jsonData);
        }
    }
}