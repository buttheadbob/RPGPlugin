﻿using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using NLog.Fluent;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace RPGPlugin.PointManagementSystem
{
    public class PointManager
    {
        // Miner Reward Rates
        private Dictionary<string, double> ExpRatio = new Dictionary<string, double>()
        {
            // Using a flat rate system until later.....
            
            // Add any custom ore subtype definitions needed here
            ["Stone"] = 0.2,
            ["Silicon"] = 0.3,
            ["Iron"] = 0.3,
            ["Nickel"] = 0.3,
            ["Cobalt"] = 0.4,
            ["Magnesium"] = 0.4,
            ["Silver"] = 0.5,
            ["Gold"] = 0.5,
            ["Platinum"] = 0.55,
            ["Uranium"] = 0.6,
            ["Ice"] = 0.35
        };
        
        public PointManager()
        {
        }

        // MINING SECTION
        public async void ShipDrillCollected(string entityname, long entityid, string gridname, long gridid, string typeid, string subtypeid, float amount)
        {
            await ShipDrillCollectedAsync(entityname, entityid, gridname, gridid, typeid, subtypeid, amount);
        }

        private async Task ShipDrillCollectedAsync(string entityname, long entityid, string gridname, long gridid, string typeid, string subtypeid, float amount)
        {
            IMyEntity Entity = MyAPIGateway.Entities.GetEntityById(entityid);
            
            // Get Grid
            var ShipDrill = Entity as MyShipDrill;
            if (ShipDrill == null) return;
            
            // Had thought to search for the pilot but searching every block on every grid every 10 ticks is not nice to the server.. 
            // So rewarding the grid owner is the easiest way to go for now.
            
            if (ShipDrill.OwnerId == 0)
            {
                Roles.Log.Error("Grid is mining but owner is unknown.  Unable to assign points.");
                return;
            }
            
            // If not a miner, no points given.
            if (Roles.PlayerManagers[ShipDrill.OwnerId].GetRole() != PlayerManager.FromRoles.Miner) return;
            
            // Calculate exp points.   !!!! Amount is not given, will need to look for another way or patch.
            //double points = amount * ExpRatio[subtypeid];

            Roles.Log.Info("Reward exp for mining " + subtypeid);
            await Roles.PlayerManagers[ShipDrill.OwnerId].AddMinerExp(0.007);  
            // Tested with 0.3 and that was allot of points very fast with 13 drills!!
            // Thousands of points in 1 minute.  So will need to be very small numbers!!!
        }
        
        // WARRIOR SECTION
    }
}