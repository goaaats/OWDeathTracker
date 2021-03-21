using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Goaaats.DeathTracker
{
    class DeathTracking
    {
        public DeathTracking()
        {
            TrackedDeaths = new List<Death>();
        }

        public class Death
        {
            public string ProfileName { get; set; }

            public DeathType DeathType { get; set; }
            public int LoopCount { get; set; }
            public float SecondsElapsed { get; set; }

            public float PositionX { get; set; }
            public float PositionY { get; set; }
            public float PositionZ { get; set; }

            public Sector.Name SectorName { get; set; }
        }

        public List<Death> TrackedDeaths { get; set; }

        public void Load(string path)
        {
            var savePath = Path.Combine(path, "Deaths.json");

            if (File.Exists(savePath))
                TrackedDeaths = JsonConvert.DeserializeObject<List<Death>>(File.ReadAllText(savePath));
        }

        public void Save(string path)
        {
            File.WriteAllText(Path.Combine(path, "Deaths.json"), JsonConvert.SerializeObject(TrackedDeaths));
        }

        public IEnumerable<Death> GetAllForProfile(string name) => TrackedDeaths.Where(x => x.ProfileName == name);

        public void Reset(string playerName)
        {
            Debug.Log($"Resetting deaths for {playerName}");
            TrackedDeaths = TrackedDeaths.Where(x => x.ProfileName != playerName).ToList();
        }
    }
}
