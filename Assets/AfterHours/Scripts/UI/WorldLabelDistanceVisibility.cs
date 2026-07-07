using UnityEngine;

/// <summary>
/// 월드에 배치된 라벨이 카메라에서 멀어지면 보이지 않게 처리합니다.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class WorldLabelDistanceVisibility : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform cameraTransform;

    [Header("표시 거리")]
    [SerializeField] private float visibleDistance = 32f;

    private Renderer labelRenderer;

    private void Awake()
    {
        labelRenderer = GetComponent<Renderer>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (labelRenderer == null || cameraTransform == null)
        {
            return;
        }

        float sqrDistance = (cameraTransform.position - transform.position).sqrMagnitude;
        bool shouldShow = sqrDistance <= visibleDistance * visibleDistance;

        if (labelRenderer.enabled != shouldShow)
        {
            labelRenderer.enabled = shouldShow;
        }
    }
}
