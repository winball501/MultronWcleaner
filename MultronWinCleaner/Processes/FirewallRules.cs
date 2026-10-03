using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace MultronWinCleaner.Processes
{
    public static class FirewallRules
    {
        public sealed class InvalidRule
        {
            public string Name { get; set; } = "";
            public string ApplicationPath { get; set; } = "";
            public string Direction { get; set; } = "";
            internal INetFwRule Rule { get; set; }
        }

        private static INetFwPolicy2 GetPolicy()
        {
            Type type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            return (INetFwPolicy2)Activator.CreateInstance(type);
        }

        public static List<InvalidRule> FindInvalidRules()
        {
            var result = new List<InvalidRule>();
            foreach (INetFwRule rule in GetPolicy().Rules)
            {
                string path;
                try
                {
                    path = rule.ApplicationName;
                }
                catch (Exception)
                {
                    continue;
                }

                if (IsMissingProgram(path))
                {
                    result.Add(new InvalidRule
                    {
                        Name = rule.Name ?? "",
                        ApplicationPath = path,
                        Direction = rule.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN ? "Inbound" : "Outbound",
                        Rule = rule
                    });
                }
            }
            result.Sort((a, b) => string.Compare(a.ApplicationPath, b.ApplicationPath, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        private static bool IsMissingProgram(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string expanded = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
            if (expanded.Contains('%') || expanded.StartsWith(@"\\") || !Path.IsPathFullyQualified(expanded))
                return false;

            try
            {
                string root = Path.GetPathRoot(expanded);
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                    return false;
                return !File.Exists(expanded) && !Directory.Exists(expanded);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static (int Removed, List<InvalidRule> Failed) RemoveRules(IEnumerable<InvalidRule> rules)
        {
            INetFwPolicy2 policy = GetPolicy();
            int removed = 0;
            var failed = new List<InvalidRule>();

            foreach (InvalidRule rule in rules)
            {
                bool renamed = false;
                try
                {
                    string uniqueName = "MWC-Remove-" + Guid.NewGuid().ToString("N");
                    rule.Rule.Name = uniqueName;
                    renamed = true;
                    policy.Rules.Remove(uniqueName);
                    removed++;
                }
                catch (Exception)
                {
                    failed.Add(rule);
                    if (renamed)
                    {
                        try { rule.Rule.Name = rule.Name; } catch (Exception) { }
                    }
                }
            }
            return (removed, failed);
        }
    }
}
