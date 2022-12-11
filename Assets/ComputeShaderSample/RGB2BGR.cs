#define GPU
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RGB2BGR : MonoBehaviour
{
    //コンピュートシェーダー
    [SerializeField] private ComputeShader _computeShader;
    //ベースのテクスチャ
    [SerializeField] private Texture2D _basetex;
    //投影するUIオブジェクト
    [SerializeField] private RawImage _renderer;
    //コンピュートシェーダーで計算した結果を受け取る
    private RenderTexture _result;

    [SerializeField] private int[] _groupIdCheckNum = new int[3];
    //cpuyo用
    Texture2D cpu_texture;
    private void Start()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("Comppute Shader is not support.");
            return;
        }

        // RenderTextueの初期化
        _result = new RenderTexture(_basetex.width, _basetex.height, 0, RenderTextureFormat.ARGB32);
        _result.enableRandomWrite = true;
        _result.Create();

#if !GPU       
        RenderTexture.active = _result;
        cpu_texture = new Texture2D(_result.width, _result.height);
        cpu_texture.ReadPixels(new Rect(0, 0, _result.width, _result.height), 0, 0);
#endif
    }
    

    private void Update()
    {
#if GPU
        // RGB2BGRのカーネルインデックス(0)を取得
        var kernelIndex = _computeShader.FindKernel("MASK_DEBUG");

        // 一つのグループの中に何個のスレッドがあるか
        (uint x, uint y, uint z) threadSize = (0,0,0);
        _computeShader.GetKernelThreadGroupSizes(kernelIndex, out threadSize.x, out threadSize.y, out threadSize.z);

        // GPUにデータをコピーする
        _computeShader.SetTexture(kernelIndex, "BaseTexture", _basetex);
        _computeShader.SetTexture(kernelIndex, "Result", _result);
        _computeShader.SetInts("groupIdCheckNum", _groupIdCheckNum);

        // GPUの処理を実行する
        _computeShader.Dispatch(kernelIndex, (_basetex.width / (int)threadSize.x), (_basetex.height / (int)threadSize.y), (int)threadSize.z);

        // テクスチャを適応する
        _renderer.texture = _result;
#else        
        
        for (int x = 0; x < _basetex.width; x++)
        {
            for (int y = 0; y < _basetex.height; y++)
            {
                var c = _basetex.GetPixel(x, y);
                var cc = new Color(c.b,c.g,c.r,c.a);
                cpu_texture.SetPixel(x, y, cc);
            }
        }
        cpu_texture.Apply();
        _renderer.texture = cpu_texture;
#endif
        
    }

    private void OnDestroy()
    {
        _result = null;
    }
}
