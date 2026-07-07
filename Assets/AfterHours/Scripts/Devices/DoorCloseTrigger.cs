using UnityEngine;

/// <summary>
/// 플레이어가 다음 지점에 들어오면 뒤쪽 문을 빠르게 닫습니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DoorCloseTrigger : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private SecurityDoor doorToClose;

    [Header("설정")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

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
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        hasTriggered = true;

        if (doorToClose != null)
        {
            doorToClose.CloseDoor();
        }
    }
}
