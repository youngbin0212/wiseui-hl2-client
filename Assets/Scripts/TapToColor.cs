// 3a-1: Air-tap으로 큐브 색 순환.
// 큐브 GameObject에 이 컴포넌트를 붙이면 됨 (큐브엔 BoxCollider 기본 포함).
// MRTK 입력 시스템이 hand ray / pinch를 자동으로 OnPointerClicked로 보내줌.

using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class TapToColor : MonoBehaviour, IMixedRealityPointerHandler
{
    private MeshRenderer _renderer;
    private readonly Color[] _colors =
    {
        Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta
    };
    private int _idx = 0;

    void Start()
    {
        _renderer = GetComponent<MeshRenderer>();
        if (_renderer == null)
        {
            Debug.LogError("[TapToColor] MeshRenderer not found on " + gameObject.name);
        }
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData) { }
    public void OnPointerDragged(MixedRealityPointerEventData eventData) { }
    public void OnPointerUp(MixedRealityPointerEventData eventData) { }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        _idx = (_idx + 1) % _colors.Length;
        if (_renderer != null)
        {
            _renderer.material.color = _colors[_idx];
        }
        Debug.Log($"[TapToColor] tap detected → color = {_colors[_idx]}");
    }
}
