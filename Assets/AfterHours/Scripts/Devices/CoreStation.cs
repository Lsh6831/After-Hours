using System.Collections;
using UnityEngine;

/// <summary>
/// EnergyCore가 Trigger 영역에 들어오면 코어를 고정하고 충전 후 연결된 문을 엽니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CoreStation : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform stationHoldPoint;
    [SerializeField] private SecurityDoor linkedDoor;
    [SerializeField] private ParticleSystem chargeParticles;
    [SerializeField] private AudioSource chargeAudio;

    [Header("충전 설정")]
    [SerializeField] private float chargeTime = 3f;

    private bool isCharging;
    private bool isCharged;
    private EnergyCore currentCore;
    private Coroutine chargeRoutine;

    private void Reset()
    {
        Collider stationCollider = GetComponent<Collider>();
        stationCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider stationCollider = GetComponent<Collider>();
        if (stationCollider != null)
        {
            stationCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCharging || isCharged)
        {
            return;
        }

        EnergyCore energyCore = other.GetComponentInParent<EnergyCore>();
        if (energyCore == null || !energyCore.CanChargeStation)
        {
            return;
        }

        StartCharge(energyCore);
    }

    private void StartCharge(EnergyCore energyCore)
    {
        if (stationHoldPoint == null)
        {
            Debug.LogWarning($"{name}에 stationHoldPoint가 연결되지 않았습니다.");
            return;
        }

        currentCore = energyCore;
        isCharging = true;

        GrabTarget grabTarget = currentCore.GetComponent<GrabTarget>();
        if (grabTarget != null)
        {
            grabTarget.Capture(stationHoldPoint);
        }
        else
        {
            currentCore.transform.SetPositionAndRotation(stationHoldPoint.position, stationHoldPoint.rotation);
        }

        currentCore.OnInsertedIntoStation();

        if (chargeParticles != null)
        {
            chargeParticles.Play();
        }

        if (chargeAudio != null)
        {
            chargeAudio.Play();
        }

        chargeRoutine = StartCoroutine(ChargeRoutine());
    }

    private IEnumerator ChargeRoutine()
    {
        // 충전 시간 동안 중복 작동을 막고, 완료 후 문을 엽니다.
        yield return new WaitForSeconds(chargeTime);

        isCharging = false;
        isCharged = true;
        chargeRoutine = null;

        if (chargeParticles != null)
        {
            chargeParticles.Stop();
        }

        if (linkedDoor != null)
        {
            linkedDoor.OpenDoor();
        }
    }
}
