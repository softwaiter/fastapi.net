using System;
using System.Collections.Generic;

namespace CodeM.FastApi.DbUpgrade
{
    internal class UpgradeVersion
    {
        private List<string> sCommands = new List<string>();

        public UpgradeVersion(int version)
        {
            this.Version = version;
        }

        public int Version { get; set; }

        public void AddCommand(string command)
        {
            sCommands.Add(command);
        }

        public void Enum(Action<string> callback)
        {
            foreach (string command in sCommands)
            {
                callback(command);
            }
        }
    }
}
