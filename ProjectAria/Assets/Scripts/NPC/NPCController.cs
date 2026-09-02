// ============================================================
// NPCController.cs
// NPC with schedule (waypoints by time), personality, dialogue, friendship.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectAria.Core;
using ProjectAria.Quest;

namespace ProjectAria.NPC
{
    public enum NPCPersonality { Friendly, Shy, Gruff, Cheerful, Mysterious, Stoic, Merchant }

    [CreateAssetMenu(fileName = "NPC_", menuName = "Aria/NPC/Definition", order = 7)]
    public class NPCDefinition : ScriptableObject
    {
        public int Id;
        public string DisplayName;
        public NPCPersonality Personality;
        public Sprite Portrait;
        public int[] LikesItems;
        public int[] DislikesItems;
        public DialogueTree StartingDialogue;
        public ScheduleEntry[] Schedule;
    }

    [System.Serializable]
    public struct ScheduleEntry
    {
        public int StartHour;
        public int EndHour;
        public Vector3 Position;
        public string Activity;
    }

    public class NPCController : MonoBehaviour, Player.IInteractable
    {
        public NPCDefinition Definition;
        public int FriendshipLevel { get; private set; } = 0;
        public string DisplayName => Definition != null ? Definition.DisplayName : "NPC";
        public Transform Transform => transform;

        private NavMeshAgent _agent;
        private DialogueSystem _dialogue;
        private Vector3 _home;
        private bool _talking;
        private float _repathTimer;

        public void Init(NPCDefinition def)
        {
            Definition = def;
            _home = transform.position;
            _agent = GetComponent<NavMeshAgent>();
            _dialogue = GetComponent<DialogueSystem>();
        }

        public bool CanInteract(Player.PlayerController player) => !_talking;
        public void OnInteract(Player.PlayerController player)
        {
            if (_dialogue != null) _dialogue.StartDialogue(Definition.StartingDialogue, player);
        }

        public void AddFriendship(int amount) => FriendshipLevel = Mathf.Clamp(FriendshipLevel + amount, 0, 10);

        private void Update()
        {
            if (_talking) return;
            var time = ServiceLocator.Get<TimeSystem>();
            if (time == null || Definition == null) return;
            Vector3 target = _home;
            foreach (var entry in Definition.Schedule)
            {
                if (time.CurrentHour >= entry.StartHour && time.CurrentHour < entry.EndHour)
                {
                    target = entry.Position;
                    break;
                }
            }
            _repathTimer -= Time.deltaTime;
            if (_repathTimer <= 0f && _agent != null && _agent.isOnNavMesh)
            {
                _agent.SetDestination(target);
                _repathTimer = 2f;
            }
        }

        public void SetTalking(bool v) => _talking = v;
    }
}
