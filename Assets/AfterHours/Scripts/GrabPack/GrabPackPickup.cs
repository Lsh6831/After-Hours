using UnityEngine;

/// <summary>
/// 플레이어가 Grab Pack 장비에 닿으면 GrabPackController 사용을 해금합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GrabPackPickup : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private GrabPackController grabPackController;
    [SerializeField] private MissionManager missionManager;

    [Header("미션 설정")]
    [SerializeField] private string completionObjectiveId = "test_grab_gear";
    [SerializeField] private bool completeMissionOnPickup = true;
    [SerializeField] private bool hideAfterPickup;

    private bool hasPickedUp;

    private void Awake()
    {
        Collider pickupCollider = GetComponent<Collider>();
        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasPickedUp || !other.CompareTag("Player"))
        {
            return;
        }

        hasPickedUp = true;

        if (grabPackController != null)
        {
            grabPackController.SetGrabPackUsable(true);
        }

        if (completeMissionOnPickup && missionManager != null)
        {
            missionManager.CompleteMission(completionObjectiveId);
        }

        if (hideAfterPickup)
        {
            gameObject.SetActive(false);
        }
    }
}
