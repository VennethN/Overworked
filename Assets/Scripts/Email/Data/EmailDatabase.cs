using System.Collections.Generic;
using UnityEngine;
using Overworked.Email.Data;

namespace Overworked.Email
{
    public class EmailDatabase
    {
        private readonly Dictionary<string, EmailDefinition> _emailsById = new();
        private readonly Dictionary<string, List<EmailDefinition>> _emailsByPool = new();

        public int Count => _emailsById.Count;

        public void LoadFromResources(string[] jsonResourcePaths)
        {
            foreach (string path in jsonResourcePaths)
            {
                TextAsset asset = Resources.Load<TextAsset>(path);
                if (asset == null)
                {
                    Debug.LogWarning($"EmailDatabase: Could not load JSON at Resources/{path}");
                    continue;
                }

                // Derive pool name from file path: "Data/Emails/emails_general" -> "general"
                string poolName = ExtractPoolName(path);

                EmailCollection collection = JsonUtility.FromJson<EmailCollection>(asset.text);
                if (collection?.emails == null) continue;

                foreach (EmailDefinition def in collection.emails)
                {
                    def.ParseEnums();
                    _emailsById[def.id] = def;

                    // Index by pool
                    if (!_emailsByPool.ContainsKey(poolName))
                        _emailsByPool[poolName] = new List<EmailDefinition>();
                    _emailsByPool[poolName].Add(def);
                }
            }

            Debug.Log($"EmailDatabase: Loaded {_emailsById.Count} emails across {_emailsByPool.Count} pools.");
        }

        private string ExtractPoolName(string resourcePath)
        {
            // "Data/Emails/emails_general" -> "general"
            // "Data/Emails/emails_story_day1" -> "story_day1"
            string fileName = resourcePath;
            int lastSlash = fileName.LastIndexOf('/');
            if (lastSlash >= 0)
                fileName = fileName.Substring(lastSlash + 1);

            if (fileName.StartsWith("emails_"))
                fileName = fileName.Substring("emails_".Length);

            return fileName;
        }

        public EmailDefinition GetById(string id)
        {
            return _emailsById.GetValueOrDefault(id);
        }

        /// <summary>
        /// Pick a random email from the specified pools, optionally filtered by type and tags.
        /// Only emails belonging to the listed pools are candidates.
        /// </summary>
        public EmailDefinition GetRandomFromPools(string[] poolNames, string[] typeFilter, string[] tagFilter)
        {
            if (poolNames == null || poolNames.Length == 0) return null;

            List<EmailDefinition> candidates = new();

            foreach (string pool in poolNames)
            {
                if (_emailsByPool.TryGetValue(pool, out var list))
                    candidates.AddRange(list);
            }

            // Filter by type if specified
            if (typeFilter != null && typeFilter.Length > 0)
            {
                candidates.RemoveAll(def =>
                {
                    foreach (string typeName in typeFilter)
                    {
                        EmailType type = typeName.ToLower() switch
                        {
                            "reply" => EmailType.Reply,
                            "task" => EmailType.Task,
                            "spam" => EmailType.Spam,
                            "info" => EmailType.Info,
                            _ => EmailType.Info
                        };
                        if (def.parsedType == type) return false;
                    }
                    return true;
                });
            }

            // Filter by tags if specified (email must have at least one matching tag)
            if (tagFilter != null && tagFilter.Length > 0)
            {
                candidates.RemoveAll(def =>
                {
                    if (def.tags == null) return true;
                    foreach (string requiredTag in tagFilter)
                    {
                        foreach (string emailTag in def.tags)
                        {
                            if (emailTag == requiredTag) return false;
                        }
                    }
                    return true;
                });
            }

            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)];
        }

        // Keep old method signature for backward compatibility with spawner
        public EmailDefinition GetRandomFromPool(string[] typeNames, string[] tags)
        {
            // Use all pools except "responses"
            List<string> pools = new();
            foreach (var key in _emailsByPool.Keys)
            {
                if (key != "responses")
                    pools.Add(key);
            }
            return GetRandomFromPools(pools.ToArray(), typeNames, tags);
        }
    }
}
