﻿namespace StreamerbotPlugin.Models
{
    public class ApiResponse
    {
        public string id { get; set; }
        public int count { get; set; }
        public Action[] actions { get; set; }
        public string status { get; set; }
    }

    // Define classes to match the JSON structure
    public class Action
    {
        public string id { get; set; }
        public string name { get; set; }
        public string group { get; set; }
        public bool enabled { get; set; }
        public int subaction_count { get; set; }

        public Action(string id, string name, string group, bool enabled, int subaction_count)
        {
            this.id = id;
            this.name = name;
            this.enabled = enabled;
            this.group = group;
            this.subaction_count = subaction_count;
        }

        // Override the ToString() method to return the action's name
        public override string ToString()
        {
            return name;
        }
    }
}
