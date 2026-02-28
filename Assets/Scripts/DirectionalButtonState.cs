using UnityEngine;
using UnityEngine.UI;

// Syncs on-screen directional buttons with keyboard keys.
[DisallowMultipleComponent]
public class DirectionalButtonState : MonoBehaviour
{
    [SerializeField] private KeyCode[] keyBindings = { KeyCode.W };
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;
    [SerializeField] private Animator animator;
    [SerializeField] private string animatorBool = "Pressed";

    void Reset()
    {
        targetImage = GetComponent<Image>();
        if (targetImage != null)
        {
            normalSprite = targetImage.sprite;
        }
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        bool isPressed = IsAnyBoundKeyPressed();

        UpdateSprite(isPressed);

        if (animator != null && !string.IsNullOrEmpty(animatorBool))
        {
            animator.SetBool(animatorBool, isPressed);
        }
    }

    void UpdateSprite(bool isPressed)
    {
        if (targetImage == null)
        {
            return;
        }

        if (isPressed)
        {
            if (pressedSprite != null && targetImage.sprite != pressedSprite)
            {
                targetImage.sprite = pressedSprite;
            }
        }
        else
        {
            if (normalSprite != null && targetImage.sprite != normalSprite)
            {
                targetImage.sprite = normalSprite;
            }
        }
    }

    bool IsAnyBoundKeyPressed()
    {
        if (keyBindings == null || keyBindings.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < keyBindings.Length; i++)
        {
            if (Input.GetKey(keyBindings[i]))
            {
                return true;
            }
        }

        return false;
    }
}
