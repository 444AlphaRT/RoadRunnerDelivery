using UnityEngine;
using TMPro;

public class SpeedLimitSign : MonoBehaviour
{
    [Header("Speed Limit (KM/H)")]
    [SerializeField] private float limitKmh = 30f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI signText;

    public float LimitKmh => limitKmh;

    private void Awake()
    {
        Refresh();
    }

    private void Start()
    {
        // Extra safety: updates text after everything is initialized
        Refresh();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Refresh();
    }
#endif

    [ContextMenu("Refresh Sign Text")]
    private void Refresh()
    {
        if (signText == null)
            return;

        signText.text = Mathf.RoundToInt(limitKmh).ToString();
    }
}
