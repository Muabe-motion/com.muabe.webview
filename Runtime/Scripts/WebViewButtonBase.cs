using UnityEngine;
using UnityEngine.UI;

namespace Muabe.WebView
{
    /// <summary>
    /// Base class for WebView-related buttons with common functionality
    /// </summary>
    [RequireComponent(typeof(Button))]
    public abstract class WebViewButtonBase : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        protected Text statusText;

        [SerializeField]
        protected Text buttonLabel;

        protected Button button;
        protected string originalButtonLabel;
        protected string originalStatusLabel;

        protected virtual void Awake()
        {
            button = GetComponent<Button>();
            AutoAssignUIReferences();
            CacheOriginalLabels();
            button.onClick.AddListener(OnButtonClicked);
        }

        protected virtual void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        protected virtual void AutoAssignUIReferences()
        {
            if (buttonLabel == null)
            {
                buttonLabel = GetComponentInChildren<Text>(true);
            }

            if (statusText == null)
            {
                statusText = buttonLabel;
            }
        }

        protected virtual void CacheOriginalLabels()
        {
            if (buttonLabel != null && string.IsNullOrEmpty(originalButtonLabel))
            {
                originalButtonLabel = buttonLabel.text;
            }

            if (statusText != null && string.IsNullOrEmpty(originalStatusLabel))
            {
                originalStatusLabel = statusText.text;
            }
        }

        protected void UpdateStatusLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
                return;

            if (statusText != null)
            {
                statusText.text = label;
            }

            if (buttonLabel != null)
            {
                buttonLabel.text = label;
            }
        }

        public void ResetStatusLabel()
        {
            if (buttonLabel != null && !string.IsNullOrEmpty(originalButtonLabel))
            {
                buttonLabel.text = originalButtonLabel;
            }

            if (statusText != null && !string.IsNullOrEmpty(originalStatusLabel))
            {
                statusText.text = originalStatusLabel;
            }
        }

        protected void SetButtonInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        protected abstract void OnButtonClicked();
    }
}
