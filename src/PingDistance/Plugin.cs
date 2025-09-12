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
    private static GameObject? templateText;
    private static readonly Dictionary<Character, GameObject> distanceInstances = [];
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
            distanceInstances.Clear();
        }
    }

    [HarmonyPatch(typeof(PointPinger), nameof(PointPinger.ReceivePoint_Rpc))]
    [HarmonyPrefix]
    private static void PointPinger_ReceivePoint_Rpc_Prefix(PointPinger __instance, ref Vector3 point, ref Vector3 hitNormal)
    {
        if (PingDistanceCanvas == null || templateText == null)
        {
            Log.LogError("PingDistanceCanvas or templateText is not instantiated");
            return;
        }

        Character character = __instance.character;
        Vector3 position = point + hitNormal;

        if (distanceInstances.TryGetValue(character, out GameObject? oldInstance) && oldInstance != null)
        {
            Destroy(oldInstance);
        }

        GameObject newInstance = Instantiate(templateText, PingDistanceCanvas.transform);
        newInstance.SetActive(true);

        PingDistance distanceComponent = newInstance.GetComponent<PingDistance>();
        distanceComponent.position = position;
        distanceComponent.character = character;

        distanceInstances[character] = newInstance;

        Destroy(newInstance, 1.25f);
    }

    private static void CreatePingDistanceCanvas()
    {
        GameObject GUIManager = GameObject.Find("GAME/GUIManager");
        GameObject NamesCanvas = GameObject.Find("GAME/GUIManager/Canvas_Names");
        PingDistanceCanvas = Instantiate(NamesCanvas, GUIManager.transform);
        PingDistanceCanvas.name = "Canvas_PingDistance";

        if (PingDistanceCanvas.TryGetComponent<UIPlayerNames>(out UIPlayerNames component))
            Destroy(component);

        templateText = PingDistanceCanvas.transform.GetChild(0).gameObject;
        Destroy(templateText.transform.GetChild(0).gameObject);
        if (templateText.TryGetComponent<PlayerName>(out PlayerName? nameComponent))
            Destroy(nameComponent);

        templateText.name = "DistanceText";
        templateText.AddComponent<PingDistance>();
        templateText.SetActive(false);

        for (int i = 0; i < PingDistanceCanvas.transform.childCount; i++)
        {
            GameObject child = PingDistanceCanvas.transform.GetChild(i).gameObject;
            if (child.name.Contains("UI_PlayerName")) Destroy(child);
        }

        Log.LogInfo("PingDistanceCanvas created");
    }
}

public class PingDistance : MonoBehaviour
{
    public Vector3 position;
    public Character? character;
    private TextMeshProUGUI? tmp;

    void LateUpdate()
    {
        if (Camera.main == null) return;

        Color color = character != null ? character.refs.customization.PlayerColor : Color.white;
        float distance = Mathf.Round(Vector3.Distance(position, Camera.main.transform.position));
        float angle = Vector3.Angle(Camera.main.transform.forward, position - Camera.main.transform.position);
        if (tmp == null) tmp = GetComponentInChildren<TextMeshProUGUI>();

        transform.position = Camera.main.WorldToScreenPoint(position);

        tmp.fontSize = 30f;
        tmp.color = color;
        tmp.text = $"{distance}m";
        tmp.gameObject.SetActive(angle < 90f);
    }
}
