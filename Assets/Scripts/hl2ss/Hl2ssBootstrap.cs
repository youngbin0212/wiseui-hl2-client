// 시퀀싱: 시작 시 hl2ss(PV+depth) ON → FP init 후 Stop() 으로 카메라 반납 → PhotoCapture 가 인수.
// (hl2ss 와 PhotoCapture 가 HoloLens 에서 동시 불가 → 시간 분리.)
// Research Mode capability 는 Editor/BuildPostProcessor.cs 가 빌드 후 자동 추가.

using UnityEngine;

public class Hl2ssBootstrap : MonoBehaviour
{
    public static bool Running = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var go = new GameObject("Hl2ssBootstrap");
        DontDestroyOnLoad(go);
        go.AddComponent<Hl2ssBootstrap>();
    }

#if !UNITY_EDITOR
    void Start()
    {
        hl2ss.UpdateCoordinateSystem();
        // EET(Extended Eye Tracker) ON: 등록 단계에서 PC 가 시선 ray 를 직접 받는다.
        //   기기→PC 로 ray 를 따로 보낼 필요 없음. PC 가 depth(PV 정합)에 반복조회해 히트픽셀 계산.
        //   스트림은 클라이언트가 붙을 때만 실제로 도는 pull 방식이라 미사용 시 오버헤드 ≈ 0.
        //   전제: manifest 의 gazeInput capability (BuildPostProcessor.cs 가 주입).
        //         RM    PV    MC     SI     RC     SM     SU     VI     MQ     EET   EA     EV     MQX
        hl2ss.Initialize(true, true, false, false, false, false, false, false, false, true, false, false, false);
        Running = true;
        Debug.Log("[hl2ss] INIT streaming (PV+depth+EET) — FP init 용");
    }

    void Update()
    {
        if (Running) hl2ss.CheckForErrors();
    }
#endif

    // FP init 끝난 뒤 호출 — 모든 스트림 끄고 카메라 반납 (PhotoCapture 가 쓸 수 있게)
    public static void Stop()
    {
#if !UNITY_EDITOR
        hl2ss.Initialize(false, false, false, false, false, false, false, false, false, false, false, false, false);
        Running = false;
        Debug.Log("[hl2ss] STOPPED (release camera for PhotoCapture)");
#endif
    }
}
