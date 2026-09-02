// ============================================================
// QuestSystem.cs
// Main / Side / Daily / Event quests. Objectives, rewards, triggers.
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAria.Core;

namespace ProjectAria.Quest
{
    public enum QuestType { Main, Side, Daily, Event, Hidden }
    public enum ObjectiveType { Kill, Collect, Talk, Reach, Craft, Build, Mine, Fish, Farm, Custom }

    [CreateAssetMenu(fileName = "Objective_", menuName = "Aria/Quest/Objective", order = 9)]
    public class QuestObjectiveDef : ScriptableObject
    {
        public string Id;
        public ObjectiveType Type;
        public int TargetId; // block/item/npc id depending on type
        public int RequiredAmount = 1;
        [TextArea] public string Description;
    }

    [CreateAssetMenu(fileName = "Quest_", menuName = "Aria/Quest/Definition", order = 10)]
    public class QuestDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public QuestType Type;
        [TextArea] public string Description;
        public QuestObjectiveDef[] Objectives;
        public int XpReward;
        public int[] ItemRewards;
        public int MoneyReward;
        public int FriendshipReward;
        public string FollowupQuestId;
    }

    public class QuestRuntime
    {
        public QuestDefinition Def;
        public int[] Progress;
        public bool Completed;
        public bool TurnedIn;
    }

    public class QuestSystem : IService
    {
        private readonly Dictionary<string, QuestRuntime> _active = new();
        private readonly HashSet<string> _completed = new();
        public event Action<QuestDefinition> OnStarted;
        public event Action<QuestDefinition> OnCompleted;
        public event Action<QuestDefinition, string, int> OnObjectiveUpdated;

        public IEnumerable<QuestRuntime> Active => _active.Values;
        public IEnumerable<string> Completed => _completed;

        public void Register(QuestDefinition def)
        {
            // No-op here; loaded via QuestDatabase
        }

        public bool StartQuest(string id)
        {
            if (_active.ContainsKey(id) || _completed.Contains(id)) return false;
            var def = QuestDatabase.Get(id);
            if (def == null) return false;
            var rt = new QuestRuntime { Def = def, Progress = new int[def.Objectives.Length] };
            _active[id] = rt;
            OnStarted?.Invoke(def);
            EventBus.Publish(new QuestStartedEvent(id));
            return true;
        }

        public bool CompleteQuest(string id)
        {
            if (!_active.TryGetValue(id, out var rt)) return false;
            for (int i = 0; i < rt.Progress.Length; i++)
                if (rt.Progress[i] < rt.Def.Objectives[i].RequiredAmount) return false;
            _active.Remove(id);
            _completed.Add(id);
            rt.Completed = true;
            OnCompleted?.Invoke(rt.Def);
            EventBus.Publish(new QuestCompletedEvent(id));
            if (!string.IsNullOrEmpty(rt.Def.FollowupQuestId)) StartQuest(rt.Def.FollowupQuestId);
            return true;
        }

        public void UpdateObjective(string questId, string objectiveId, int amount = 1)
        {
            if (!_active.TryGetValue(questId, out var rt)) return;
            for (int i = 0; i < rt.Def.Objectives.Length; i++)
            {
                if (rt.Def.Objectives[i].Id == objectiveId)
                {
                    rt.Progress[i] = Mathf.Min(rt.Def.Objectives[i].RequiredAmount, rt.Progress[i] + amount);
                    OnObjectiveUpdated?.Invoke(rt.Def, objectiveId, rt.Progress[i]);
                    EventBus.Publish(new QuestObjectiveUpdatedEvent(questId, objectiveId, rt.Progress[i]));
                    if (rt.Progress[i] >= rt.Def.Objectives[i].RequiredAmount) AutoCompleteIfDone(rt);
                    return;
                }
            }
        }

        public void NotifyEvent(ObjectiveType type, int targetId, int amount = 1)
        {
            foreach (var kv in _active)
            {
                var def = kv.Value.Def;
                for (int i = 0; i < def.Objectives.Length; i++)
                {
                    var o = def.Objectives[i];
                    if (o.Type == type && (o.TargetId == 0 || o.TargetId == targetId))
                    {
                        UpdateObjective(def.Id, o.Id, amount);
                    }
                }
            }
        }

        private void AutoCompleteIfDone(QuestRuntime rt)
        {
            for (int i = 0; i < rt.Progress.Length; i++)
                if (rt.Progress[i] < rt.Def.Objectives[i].RequiredAmount) return;
            // Auto-complete fires on next turn-in (not automatic)
        }
    }

    public static class QuestDatabase
    {
        private static readonly Dictionary<string, QuestDefinition> _byId = new();

        public static void Register(QuestDefinition q)
        {
            if (q == null || _byId.ContainsKey(q.Id)) return;
            _byId[q.Id] = q;
        }
        public static QuestDefinition Get(string id) => _byId.TryGetValue(id, out var q) ? q : null;
        public static IEnumerable<QuestDefinition> All => _byId.Values;
    }
}
