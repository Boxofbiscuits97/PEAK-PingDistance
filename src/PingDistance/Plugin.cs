using BepInEx;
using BepInEx.Logging;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using TMPro;

namespace PingDistance;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    private static GameObject? distanceText;
    private static GameObject? distanceInstance;
    private static GameObject? PingDistanceCanvas;

    private void Awake()
    {
        Log = Logger;

        Log.LogInfo($"Plugin {Name} is loaded!");

        Harmony.CreateAndPatchAll(typeof(Plugin));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PointPinger), nameof(PointPinger.ReceivePoint_Rpc))]
    private static void PingDistancePatch(PointPinger __instance, ref Vector3 __0, ref Vector3 __1)
    {
        Vector3 point = __0;
        Vector3 hitNormal = __1;

        Vector3 position = new Vector3(point.x + hitNormal.x, point.y + hitNormal.y, point.z + hitNormal.z);

        PingDistance distanceComponent;

        if (PingDistanceCanvas == null)
        {
            GameObject GUIManager = GameObject.Find("GAME/GUIManager");
            GameObject NamesCanvas = GameObject.Find("GAME/GUIManager/Canvas_Names");
            PingDistanceCanvas = Instantiate(NamesCanvas, GUIManager.transform);
            PingDistanceCanvas.name = "Canvas_PingDistance";

            UIPlayerNames component;
            PingDistanceCanvas.TryGetComponent<UIPlayerNames>(out component);
            Destroy(component);

            Log.LogInfo(PingDistanceCanvas.transform.childCount);
            distanceText = PingDistanceCanvas.transform.GetChild(0).gameObject;
            distanceText.name = "DistanceText";

            for (int i = 0; i < PingDistanceCanvas.transform.childCount; i++)
            {
                GameObject child = PingDistanceCanvas.transform.GetChild(i).gameObject;
                if (child.name.Contains("UI_PlayerName")) Destroy(child);
            }

            PlayerName nameComponent;
            distanceText.TryGetComponent<PlayerName>(out nameComponent);
            Destroy(nameComponent);

            Destroy(distanceText.transform.GetChild(0).gameObject);

            distanceText.AddComponent<PingDistance>();
            distanceText.SetActive(false);
        }

        if (distanceText == null) return;

        if (distanceInstance != null) Destroy(distanceInstance);

        distanceInstance = Instantiate(distanceText, PingDistanceCanvas.transform);
        distanceInstance.SetActive(true);

        distanceComponent = distanceInstance.GetComponent<PingDistance>();
        distanceComponent.position = position;
        distanceComponent.character = __instance.character;

        Destroy(distanceInstance, 1.25f);
    }
}

public class PingDistance : MonoBehaviour
{
    public float scaleFactor = 0.3f;
    public Vector3 position;
    public Character? character;

    private TextMeshProUGUI? tmp;

    void LateUpdate()
    {
        if (Camera.main == null) return;
        if (tmp == null) tmp = GetComponentInChildren<TextMeshProUGUI>();
        

        float angle = Vector3.Angle(Camera.main.transform.forward, position - Camera.main.transform.position);
        tmp.gameObject.SetActive(angle < 90f);

        
        tmp.fontSize = 30f;

        transform.position = Camera.main.WorldToScreenPoint(position);

        float distance = Vector3.Distance(position, Camera.main.transform.position);
        distance = Mathf.Round(distance);

        tmp.text = distance.ToString() + "m";
    }
}