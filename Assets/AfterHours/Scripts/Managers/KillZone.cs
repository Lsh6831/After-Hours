using UnityEngine;

/// <summary>
/// 플레이어가 바닥 아래 킬존에 닿으면 체크포인트 리스폰을 요청합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CheckpointRespawnManager respawnManager;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (respawnManager != null)
        {
            respawnManager.RespawnPlayer();
        }
    }
}
