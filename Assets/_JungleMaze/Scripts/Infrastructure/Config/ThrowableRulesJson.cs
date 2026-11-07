using System.Collections.Generic;
using System.IO;
using JungleMaze.World.Throwables;
using Newtonsoft.Json.Linq;

namespace JungleMaze.Infrastructure.Config
{
    /// <summary>
    /// Reads the thrown and special rows of the packaged runtime weapons.json (the same file the server hashes and
    /// loads) into <see cref="ThrowableRules"/>, so offline practice uses the server's numbers.
    /// </summary>
    public static class ThrowableRulesJson
    {
        public static ThrowableRules Load(string streamingAssets) =>
            Parse(File.ReadAllText(Path.Combine(streamingAssets, "JungleMaze", "runtime", "weapons.json")));

        public static ThrowableRules Parse(string json)
        {
            var rules = new List<ThrowableRule>();
            foreach (var row in JArray.Parse(json))
            {
                var category = (string)row["category"];
                if (category != "Thrown" && category != "Special") continue;
                var entry = row;
                rules.Add(ThrowableRules.FromTable((string)entry["id"], key =>
                {
                    var token = entry[key];
                    return token != null && (token.Type == JTokenType.Float || token.Type == JTokenType.Integer) ? (double?)token.Value<double>() : null;
                }));
            }
            return new ThrowableRules(rules);
        }
    }
}
