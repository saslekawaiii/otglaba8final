using UnityEngine;

namespace Lab8
{
    public enum NpcRole
    {
        Narrator,
        QuestGiver
    }

    public class Npc : MonoBehaviour, IInteractable
    {
        [SerializeField] private string npcId = "npc";
        [SerializeField] private string npcName = "Житель";
        [SerializeField] private NpcRole role = NpcRole.QuestGiver;
        [SerializeField] private string[] questIds = new string[0];

        public string NpcId => npcId;
        public string NpcName => npcName;
        public NpcRole Role => role;
        public string[] QuestIds => questIds;

        public string GetPrompt()
        {
            return "E - Поговорить: " + npcName;
        }

        public void Interact(PlayerController player)
        {
            UiRoot ui = UiRoot.Instance;
            if (ui != null)
            {
                ui.ShowNpcDialogue(this);
            }
        }

        public void Configure(string id, string displayName, NpcRole npcRole, string[] npcQuestIds)
        {
            npcId = id;
            npcName = displayName;
            role = npcRole;
            questIds = npcQuestIds ?? new string[0];
        }
    }
}
