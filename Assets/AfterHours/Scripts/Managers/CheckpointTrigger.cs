using UnityEngine;

/// <summary>
/// 플레이어가 지나가면 현재 리스폰 지점을 해당 스폰 포인트로 갱신합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CheckpointRespawnManager respawnManager;
    [SerializeField] private Transform spawnPoint;

    private bool hasActivated;

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
        if (hasActivated || !other.CompareTag("Player"))
        {
            return;
        }

        hasActivated = true;

        if (respawnManager != null)
        {
            respawnManager.SetCheckpoint(spawnPoint);
        }
    }
}
