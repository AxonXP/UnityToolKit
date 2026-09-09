using System.Collections.Generic;
using System.ComponentModel;
using SaintsField.Playa;
using UnityEngine;

public class ToolTip : MonoBehaviour
{
    [LayoutStart("Base", ELayout.FoldoutBox)]
    [SaintsField.ReadOnly] public bool open;
    [SerializeField] protected float timeOpen;
    protected float openTimer;
    [SerializeField] GameObject toolTipPrefab;
    [HideInInspector] public GameObject currentToolTip;
    Transform canvas;
    Camera mainCamera;
    [SerializeField] Vector3 spawnPoint;

    virtual protected void Awake()
    {
        canvas = GameObject.Find("Canvas").transform;
        mainCamera = Camera.main;
        CreateToolTip();
    }

    void LateUpdate()
    {
        if (currentToolTip != null)
            currentToolTip.GetComponent<RectTransform>().position = GetToolTipWorldPoint();
    }

    Vector3 GetToolTipWorldPoint()
    {
        var screenPoint = mainCamera.WorldToScreenPoint(transform.position + spawnPoint);

        // canvas is Screen Space - Overlay, which has no real camera of its own -
        // ScreenPointToWorldPointInRectangle requires a null camera for that render mode
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvas as RectTransform, screenPoint, null, out Vector3 worldPoint
        );

        return worldPoint;
    }

    public void CreateToolTip()
    {
        currentToolTip = Instantiate(toolTipPrefab, canvas);
        currentToolTip.GetComponent<RectTransform>().position = GetToolTipWorldPoint();
    }
    public void OpenTip()
    {
        openTimer = timeOpen;
        open = true;
        currentToolTip.SetActive(true);
    }
    public void CloseTip()
    {
        open = false;
        currentToolTip.SetActive(false);
    }
    public void FillText(string objectName, string text)
    {
        Transform target = FindToolTipChild(objectName);
        if (target == null) return;

        target.GetComponent<TMPro.TextMeshProUGUI>().text = text;
    }
    public void FillImage(string objectName, Sprite image)
    {
        Transform target = FindToolTipChild(objectName);
        if (target == null) return;

        target.GetComponent<UnityEngine.UI.Image>().sprite = image;
    }

    // Callers (e.g. BuyableItem.Start()) can't guarantee this ToolTip's own Start() - which spawns
    // currentToolTip - has already run, and not every tooltip prefab variant (e.g. "Price Tab") has
    // every named child (e.g. "Title"/"Description"/"Icon"), so both are treated as a soft miss
    // rather than a NullReferenceException.
    Transform FindToolTipChild(string objectName)
    {
        if (currentToolTip == null)
        {
            Debug.LogWarning($"{name}: tooltip fill requested before the tooltip instance existed.", this);
            return null;
        }

        Transform target = currentToolTip.transform.Find(objectName);
        if (target == null)
            Debug.LogWarning($"{name}: tooltip has no child named \"{objectName}\".", this);

        return target;
    }
}