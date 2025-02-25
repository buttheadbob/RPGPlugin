using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace RPGPlugin
{
    public class SampleConfig : configBase
    {
        // Use this collection should store your XP to Action values.
        public override ObservableCollection<KeyValuePair<string, double>> ExpRatio { get; set; } =
            new ObservableCollection<KeyValuePair<string, double>>();

        //test skill point system
        public override ObservableCollection<KeyValuePair<int, int>> SkillPoints { get; set; } =
            new ObservableCollection<KeyValuePair<int, int>>();

        /// <inheritdoc />
        public override string ViewName { get; } = "Sample";
        
        public override void init()
        {
            // Always run this method first!
            // base.init();
            
            // Set your default XP values here, they will load before your config fill
            // will, and will be used if no config file has been created at this point.
            // See MinerClass for an example.
        }

        public override void LoadConfig()
        {
            // This will load your default settings when called.  If none exist you
            // should create default settings in a new method.
            
            // Returns your config file in json format if it exists, null if not.
            // string data = GetConfig().Result;
            // if (data == null) return;
        }

        public override Task SaveConfig()
        {
            // This will create your config file if needed, and save all data to your
            // config file.  Filename will be the name of your class and location
            // is in the Torch/Instance/RPGPlugin folder.
            
            // Send your config settings in json format to: await SaveConfig(jsonData)
            // See the MinerClass for an example.
            return Task.CompletedTask;
        }
        
        public override void RegisterClass()
        {
            /*
            Tuple<string, string> RoleToRegister = new Tuple<string, string>("This Is The Class Title", "This Is A Description Of The Class");
            
            if (!Roles.Instance.Config.RegisteredRoles.Contains(RoleToRegister))
                Roles.Instance.Config.RegisteredRoles.Add(RoleToRegister);                
            */
        }
    }
}