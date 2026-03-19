using System.Collections.Generic;
using UnityEngine;
using Overworked.Email.Data;

namespace Overworked.Email
{
    public class EmailDatabase
    {
        private readonly Dictionary<string, EmailDefinition> _emailsById = new();
        private readonly Dictionary<string, List<EmailDefinition>> _emailsByTag = new();
        private readonly Dictionary<EmailType, List<EmailDefinition>> _emailsByType = new();

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

                EmailCollection collection = JsonUtility.FromJson<EmailCollection>(asset.text);
                if (collection?.emails == null) continue;

                foreach (EmailDefinition def in collection.emails)
                {
                    def.ParseEnums();
                    RegisterEmail(def);
                }
            }

            Debug.Log($"EmailDatabase: Loaded {_emailsById.Count} emails.");
        }

        private void RegisterEmail(EmailDefinition def)
        {
            if (string.IsNullOrEmpty(def.id))
            {
                Debug.LogWarning("EmailDatabase: Skipping email with no id.");
                return;
            }

            _emailsById[def.id] = def;

            // Index by type
            if (!_emailsByType.ContainsKey(def.parsedType))
                _emailsByType[def.parsedType] = new List<EmailDefinition>();
            _emailsByType[def.parsedType].Add(def);

            // Index by tags
            if (def.tags != null)
            {
                foreach (string tag in def.tags)
                {
                    if (!_emailsByTag.ContainsKey(tag))
                        _emailsByTag[tag] = new List<EmailDefinition>();
                    _emailsByTag[tag].Add(def);
                }
            }
        }

        public EmailDefinition GetById(string id)
        {
            return _emailsById.GetValueOrDefault(id);
        }

        public List<EmailDefinition> GetByTag(string tag)
        {
            return _emailsByTag.GetValueOrDefault(tag, new List<EmailDefinition>());
        }

        public List<EmailDefinition> GetByType(EmailType type)
        {
            return _emailsByType.GetValueOrDefault(type, new List<EmailDefinition>());
        }

        public EmailDefinition GetRandom(EmailType type)
        {
            List<EmailDefinition> pool = GetByType(type);
            if (pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }

        public EmailDefinition GetRandomWithTag(string tag)
        {
            List<EmailDefinition> pool = GetByTag(tag);
            if (pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }

        public EmailDefinition GetRandomFromPool(string[] typeNames, string[] tags)
        {
            List<EmailDefinition> candidates = new();

            if (typeNames != null)
            {
                foreach (string typeName in typeNames)
                {
                    EmailType type = typeName.ToLower() switch
                    {
                        "reply" => EmailType.Reply,
                        "task" => EmailType.Task,
                        "spam" => EmailType.Spam,
                        "info" => EmailType.Info,
                        _ => EmailType.Info
                    };
                    candidates.AddRange(GetByType(type));
                }
            }

            // Filter by tags if specified
            if (tags != null && tags.Length > 0)
            {
                candidates = candidates.FindAll(def =>
                {
                    if (def.tags == null) return false;
                    foreach (string requiredTag in tags)
                    {
                        bool found = false;
                        foreach (string emailTag in def.tags)
                        {
                            if (emailTag == requiredTag) { found = true; break; }
                        }
                        if (found) return true;
                    }
                    return false;
                });
            }

            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}
