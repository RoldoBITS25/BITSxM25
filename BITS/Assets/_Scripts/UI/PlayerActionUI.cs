using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MultiplayerGame.UI
{
    /// <summary>
    /// Displays player action UI and interaction prompts
    /// </summary>
    public class PlayerActionUI : MonoBehaviour
    {
        [Header("Action Buttons")]
        [SerializeField] private Button grabButton;
        [SerializeField] private Button cutButton;
        [SerializeField] private Button breakButton;

        [Header("Interaction Prompt")]
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TextMeshProUGUI interactionText;

        [Header("Cooldown Indicators")]
        [SerializeField] private Image grabCooldownImage;
        [SerializeField] private Image cutCooldownImage;
        [SerializeField] private Image breakCooldownImage;

        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI playerRoleText;
        [SerializeField] private TextMeshProUGUI playerIdText;

        private PlayerController localPlayer;

        private void Start()
        {
            // Hide UI for spectators
            if (NetworkManager.Instance != null && !NetworkManager.Instance.IsPlayer)
            {
                HideActionButtons();
                
                if (playerRoleText != null)
                {
                    playerRoleText.text = "SPECTATOR MODE";
                    playerRoleText.color = Color.gray;
                }
            }
            else
            {
                if (playerRoleText != null)
                {
                    int playerNum = NetworkManager.Instance?.PlayerNumber ?? 0;
                    playerRoleText.text = $"PLAYER {playerNum}";
                    playerRoleText.color = playerNum == 1 ? Color.blue : Color.red;
                }
            }

            if (playerIdText != null && NetworkManager.Instance != null)
            {
                playerIdText.text = NetworkManager.Instance.PlayerId;
            }

            // Hide interaction prompt initially
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }

        public void ShowInteractionPrompt(string message)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
                if (interactionText != null)
                    interactionText.text = message;
            }
        }

        public void HideInteractionPrompt()
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }

        public void UpdateCooldown(string actionType, float cooldownPercent)
        {
            Image cooldownImage = null;

            switch (actionType)
            {
                case "grab":
                    cooldownImage = grabCooldownImage;
                    break;
                case "cut":
                    cooldownImage = cutCooldownImage;
                    break;
                case "break":
                    cooldownImage = breakCooldownImage;
                    break;
            }

            if (cooldownImage != null)
            {
                cooldownImage.fillAmount = cooldownPercent;
            }
        }

        private void HideActionButtons()
        {
            if (grabButton != null) grabButton.gameObject.SetActive(false);
            if (cutButton != null) cutButton.gameObject.SetActive(false);
            if (breakButton != null) breakButton.gameObject.SetActive(false);
        }
    }
}
