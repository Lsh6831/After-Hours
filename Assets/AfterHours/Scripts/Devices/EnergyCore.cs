using UnityEngine;

/// <summary>
/// CoreStation에 넣을 수 있는 에너지 코어의 활성 상태와 빛 연출을 관리합니다.
/// GrabTarget과 같은 오브젝트에 붙여 Grab Pack으로 이동할 수 있게 사용합니다.
/// </summary>
[RequireComponent(typeof(GrabTarget))]
public class EnergyCore : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Renderer emissionRenderer;
    [SerializeField] private Light coreLight;

    [Header("상태")]
    [SerializeField] private bool isActive = true;

    [Header("빛 설정")]
    [SerializeField] private Color activeEmissionColor = new Color(0f, 0.7f, 1f);
    [SerializeField] private Color inactiveEmissionColor = Color.black;
    [SerializeField] private float activeLightIntensity = 4f;
    [SerializeField] private float inactiveLightIntensity = 0f;

    public bool IsActive => isActive;

    public bool CanChargeStation => isActive;

    private Material runtimeEmissionMaterial;

    private void Awake()
    {
        if (emissionRenderer != null)
        {
            // 원본 머티리얼 에셋을 직접 바꾸지 않도록 런타임 인스턴스를 사용합니다.
            runtimeEmissionMaterial = emissionRenderer.material;
        }

        ApplyVisualState();
    }

    private void OnValidate()
    {
        ApplyVisualState();
    }

    public void Activate()
    {
        SetActiveState(true);
    }

    public void Deactivate()
    {
        SetActiveState(false);
    }

    public void SetActiveState(bool active)
    {
        isActive = active;
        ApplyVisualState();
    }

    public void OnInsertedIntoStation()
    {
        // CoreStation에 들어간 뒤에도 충전된 코어처럼 빛나도록 활성 상태를 유지합니다.
        SetActiveState(true);
    }

    private void ApplyVisualState()
    {
        Color emissionColor = isActive ? activeEmissionColor : inactiveEmissionColor;
        float lightIntensity = isActive ? activeLightIntensity : inactiveLightIntensity;

        if (coreLight != null)
        {
            coreLight.color = activeEmissionColor;
            coreLight.intensity = lightIntensity;
        }

        Material material = runtimeEmissionMaterial;
        if (material == null && emissionRenderer != null)
        {
            material = Application.isPlaying ? emissionRenderer.material : emissionRenderer.sharedMaterial;
        }

        if (material == null)
        {
            return;
        }

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emissionColor);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", isActive ? activeEmissionColor : Color.gray);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", isActive ? activeEmissionColor : Color.gray);
        }
    }
}
