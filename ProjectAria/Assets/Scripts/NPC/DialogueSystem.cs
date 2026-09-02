// ============================================================
// DialogueSystem.cs
// Branching dialogue tree. Choices, conditions, rewards, hooks.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.Quest;

namespace ProjectAria.NPC
{
    [CreateAssetMenu(fileName = "Dialogue_", menuName = "Aria/NPC/DialogueTree", order = 8)]
    public class DialogueTree : ScriptableObject
    {
        public int Id;
        public DialogueNode Root;
    }

    [System.Serializable]
    public class DialogueNode
    {
        public int Id;
        public string SpeakerName;
        [TextArea(2, 6)] public string Text;
        public DialogueChoice[] Choices;
        public DialogueNode Next;
        public string QuestToStart;
        public string QuestToComplete;
        public int FriendshipDelta;
        public int[] RequiredItemIds;
        public int[] RewardItemIds;
    }

    [System.Serializable]
    public class DialogueChoice
    {
        public string Label;
        public DialogueNode Target;
        public int FriendshipRequired;
    }

    public class DialogueSystem : MonoBehaviour
    {
        public DialogueTree CurrentTree { get; private set; }
        public DialogueNode CurrentNode { get; private set; }
        public NPCController OwnerNpc;
        public Player.PlayerController CurrentPlayer;

        public event System.Action<DialogueNode> OnNodeEntered;
        public event System.Action OnDialogueEnded;

        public void StartDialogue(DialogueTree tree, Player.PlayerController player)
        {
            if (tree == null) return;
            CurrentTree = tree;
            CurrentNode = tree.Root;
            CurrentPlayer = player;
            EventBus.Publish(new DialogueStartedEvent(OwnerNpc != null ? OwnerNpc.GetInstanceID() : 0));
            EnterNode(CurrentNode);
        }

        public void SelectChoice(DialogueChoice choice)
        {
            if (CurrentNode == null || choice == null) return;
            if (CurrentNode.FriendshipDelta != 0 && OwnerNpc != null) OwnerNpc.AddFriendship(CurrentNode.FriendshipDelta);
            if (choice.Target != null)
            {
                CurrentNode = choice.Target;
                EnterNode(CurrentNode);
            }
            else EndDialogue();
        }

        public void EndDialogue()
        {
            CurrentNode = null;
            CurrentTree = null;
            EventBus.Publish(new DialogueEndedEvent(OwnerNpc != null ? OwnerNpc.GetInstanceID() : 0));
            OnDialogueEnded?.Invoke();
            if (OwnerNpc != null) OwnerNpc.SetTalking(false);
        }

        private void EnterNode(DialogueNode n)
        {
            if (n == null) { EndDialogue(); return; }
            // Quest start
            if (!string.IsNullOrEmpty(n.QuestToStart))
            {
                QuestSystem qs = ServiceLocator.Get<QuestSystem>();
                qs?.StartQuest(n.QuestToStart);
            }
            // Quest complete
            if (!string.IsNullOrEmpty(n.QuestToComplete))
            {
                QuestSystem qs = ServiceLocator.Get<QuestSystem>();
                qs?.CompleteQuest(n.QuestToComplete);
            }
            // Rewards
            if (n.RewardItemIds != null)
            {
                var inv = CurrentPlayer.GetComponent<Player.PlayerInventory>();
                if (inv != null) foreach (var id in n.RewardItemIds) inv.AddItem(id, 1);
            }
            // Auto-advance if no choices
            if (n.Choices == null || n.Choices.Length == 0)
            {
                if (n.Next != null) { CurrentNode = n.Next; EnterNode(n.Next); return; }
                // Otherwise end on Enter
            }
            OnNodeEntered?.Invoke(n);
        }
    }
}
