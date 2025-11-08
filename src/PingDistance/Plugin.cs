using BepInEx;
using BepInEx.Logging;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using TMPro;

namespace PingDistance;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    private static GameObject? TemplateText;
    private static readonly Dictionary<Character, GameObject> DistanceInstances = [];
    private static GameObject? PingDistanceCanvas;

    private void Awake()
    {
        Log = Logger;

        Log.LogInfo($"Plugin {Name} Version {Version} is loaded!");

        Harmony.CreateAndPatchAll(typeof(Plugin));
    }

    [HarmonyPatch(typeof(PointPinger), nameof(PointPinger.Awake))]
    [HarmonyPrefix]
    private static void PointPinger_Awake_Prefix(PointPinger __instance)
    {
        if (PingDistanceCanvas == null)
        {
            CreatePingDistanceCanvas();
            DistanceInstances.Clear();
        }
    }

    [HarmonyPatch(typeof(PointPinger), nameof(PointPinger.ReceivePoint_Rpc))]
    [HarmonyPrefix]
    private static void PointPinger_ReceivePoint_Rpc_Prefix(PointPinger __instance, ref Vector3 point, ref Vector3 hitNormal)
    {
        if (PingDistanceCanvas == null || TemplateText == null)
        {
            Log.LogError("PingDistanceCanvas or templateText is not instantiated");
            return;
        }

        Character Character = __instance.character;
        Vector3 Position = point + hitNormal;

        if (DistanceInstances.TryGetValue(Character, out GameObject? OldInstance) && OldInstance != null)
        {
            Destroy(OldInstance);
        }

        GameObject NewInstance = Instantiate(TemplateText, PingDistanceCanvas.transform);
        NewInstance.SetActive(true);

        PingDistance DistanceComponent = NewInstance.GetComponent<PingDistance>();
        DistanceComponent.Position = Position;
        DistanceComponent.Character = Character;

        DistanceInstances[Character] = NewInstance;

        Destroy(NewInstance, 1.25f);
    }

    private static void CreatePingDistanceCanvas()
    {
        GameObject GUIManager = GameObject.Find("GAME/GUIManager");
        GameObject NamesCanvas = GameObject.Find("GAME/GUIManager/Canvas_Names");
        PingDistanceCanvas = Instantiate(NamesCanvas, GUIManager.transform);
        PingDistanceCanvas.name = "Canvas_PingDistance";

        if (PingDistanceCanvas.TryGetComponent<UIPlayerNames>(out UIPlayerNames Component))
            Destroy(Component);

        TemplateText = PingDistanceCanvas.transform.GetChild(0).gameObject;
        Destroy(TemplateText.transform.GetChild(0).gameObject);
        if (TemplateText.TryGetComponent<PlayerName>(out PlayerName? nameComponent))
            Destroy(nameComponent);

        TemplateText.name = "DistanceText";
        TemplateText.AddComponent<PingDistance>();
        TemplateText.SetActive(false);

        for (int i = 0; i < PingDistanceCanvas.transform.childCount; i++)
        {
            GameObject Child = PingDistanceCanvas.transform.GetChild(i).gameObject;
            if (Child.name.Contains("UI_PlayerName")) Destroy(Child);
        }

        Log.LogInfo("PingDistanceCanvas created");
    }
}

public class PingDistance : MonoBehaviour
{
    public Vector3 Position;
    public Character? Character;
    private TextMeshProUGUI? TMP;

    void LateUpdate()
    {
        if (Camera.main == null) return;

        Color color = Character != null ? Character.refs.customization.PlayerColor : Color.white;
        float distance = Mathf.Round(Vector3.Distance(Position, Camera.main.transform.position));
        float angle = Vector3.Angle(Camera.main.transform.forward, Position - Camera.main.transform.position);
        if (TMP == null) TMP = GetComponentInChildren<TextMeshProUGUI>();

        transform.position = Camera.main.WorldToScreenPoint(Position);

        TMP.fontSize = 30f;
        TMP.color = color;
        TMP.text = $"{distance}m";
        TMP.gameObject.SetActive(angle < 90f);
    }
}
