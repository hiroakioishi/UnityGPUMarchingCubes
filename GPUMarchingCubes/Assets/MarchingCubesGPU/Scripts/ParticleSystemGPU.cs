using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace irishoak
{
    public class ParticleSystemGPU : MonoBehaviour
    {

        public ComputeShader KernelCS;
        public ComputeShader DataFieldCS;

        public int ParticleNum = 16384;

        public Vector3 CubeSize = new Vector3 (32, 32, 32);
        Vector3 _cubeSize;
        Vector3 _cubeStep;
        public Vector3 GridCenter = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 GridSize   = new Vector3(2.0f, 2.0f, 2.0f);

        ComputeBuffer _particleBufferRO;
        ComputeBuffer _particleBufferRW;

        public Material ParticleRenderMat;
        
        public Vector3 Gravity;
        public float   LifeTimeMin = 0.5f;
        public float   LifeTimeMax = 1.0f;

        float _timeStep;

        RenderTexture _dataFieldRenderTex;

        public VolumeTexDebugger VolumeTexDebugger;

        public bool EnableDrawDebugVolumeTex = false;
        public bool EnableDrawDebugParticle  = false;

        #region Accessor
        public RenderTexture GetDataFieldTex ()
        {
            return this._dataFieldRenderTex;
        }
        #endregion

        #region MonoBehaviour Functions
        void Start()
        {
            InitParams();
            InitBuffers();
            InitParticle();
        }

        void Update()
        {
            UpdateParticle();
            UdpateDataField();
        }

        void OnRenderObject()
        {
            if (EnableDrawDebugVolumeTex)
            {
                if (VolumeTexDebugger != null)
                {
                    VolumeTexDebugger.DrawVolumeTex(
                        _dataFieldRenderTex,
                        _dataFieldRenderTex.width,
                        _dataFieldRenderTex.height,
                        _dataFieldRenderTex.volumeDepth
                    );
                }
            }
            if (EnableDrawDebugParticle)
            {
                RenderParticle();
            }
        }

        void OnDestroy()
        {
            DeleteBuffers();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(GridCenter, GridSize);
        }
        #endregion

        #region Private Functions
        void InitParams()
        {
            _cubeSize = CubeSize;
            _cubeStep = new Vector3(2.0f / _cubeSize.x, 2.0f / _cubeSize.y, 2.0f / _cubeSize.z);
        }

        void InitBuffers()
        {
            var particleArr = new SimpleParticle[ParticleNum];
            for (int i = 0; i < ParticleNum; i++)
            {
                particleArr[i] = new SimpleParticle(
                    Vector3.zero,
                    Random.insideUnitSphere,
                    Color.white,
                    0.0f
                );
            }
            _particleBufferRO = new ComputeBuffer(ParticleNum, Marshal.SizeOf(typeof(SimpleParticle)));
            _particleBufferRW = new ComputeBuffer(ParticleNum, Marshal.SizeOf(typeof(SimpleParticle)));
            _particleBufferRO.SetData(particleArr);
            _particleBufferRW.SetData(particleArr);

            // 空間上のパーティクルの分布を示すボリュームデータの初期化
            _dataFieldRenderTex = new RenderTexture((int)_cubeSize.x, (int)_cubeSize.y, 0, RenderTextureFormat.RFloat);
            _dataFieldRenderTex.dimension         = UnityEngine.Rendering.TextureDimension.Tex3D;
            _dataFieldRenderTex.filterMode        = FilterMode.Point;
            _dataFieldRenderTex.volumeDepth       = (int)_cubeSize.z;
            _dataFieldRenderTex.enableRandomWrite = true;
            _dataFieldRenderTex.wrapMode          = TextureWrapMode.Clamp;
            _dataFieldRenderTex.hideFlags         = HideFlags.HideAndDontSave;
            _dataFieldRenderTex.Create();

        }

        void DeleteBuffers()
        {
            if (_particleBufferRO != null)
            {
                _particleBufferRO.Release();
            }
            _particleBufferRO = null;

            if (_particleBufferRW != null)
            {
                _particleBufferRW.Release();
            }
            _particleBufferRW = null;

            if(_dataFieldRenderTex != null)
            {
                DestroyImmediate(_dataFieldRenderTex);
            }
            _dataFieldRenderTex = null;
        }

        void SwapBuffers(ref ComputeBuffer src, ref ComputeBuffer dst)
        {
            ComputeBuffer tmp = src;
            src = dst;
            dst = tmp;
        }

        void InitParticle()
        {
            var id = KernelCS.FindKernel("InitCS");    
        }

        void UpdateParticle()
        {
            _timeStep = Time.deltaTime;

            var id = KernelCS.FindKernel("UpdateCS");
            KernelCS.SetBuffer (id, "_ParticleBufferRO", _particleBufferRO  );
            KernelCS.SetBuffer (id, "_ParticleBufferRW", _particleBufferRW  );
            KernelCS.SetTexture(id, "_DataFieldTexRW",   _dataFieldRenderTex);
            KernelCS.SetInts  ("_GridNum", new int[3]{ (int)_cubeSize.x, (int)_cubeSize.y, (int)_cubeSize.z});
            KernelCS.SetVector("_GridCenter",  GridCenter );
            KernelCS.SetVector("_GridSize",    GridSize   );
            KernelCS.SetFloat ("_TimeStep",    _timeStep  );
            KernelCS.SetVector("_Gravity",     Gravity * 0.01f );
            KernelCS.SetFloat ("_LifeTimeMin", LifeTimeMin);
            KernelCS.SetFloat ("_LifeTimeMax", LifeTimeMax);
            KernelCS.Dispatch(id, ParticleNum / 32, 1, 1);

            SwapBuffers(ref _particleBufferRO, ref _particleBufferRW);
        }

        void RenderParticle()
        {
            ParticleRenderMat.SetPass(0);
            ParticleRenderMat.SetBuffer("_ParticleBuffer", _particleBufferRO);
            Graphics.DrawProcedural(MeshTopology.Points, _particleBufferRO.count, 0);
        }

        void UdpateDataField()
        {
            var id = DataFieldCS.FindKernel("ClearDataFieldCS");
            DataFieldCS.SetTexture(id, "_DataFieldTexRW", _dataFieldRenderTex);
            DataFieldCS.Dispatch(id, _dataFieldRenderTex.width / 8, _dataFieldRenderTex.height / 8, _dataFieldRenderTex.volumeDepth / 8);

            id = DataFieldCS.FindKernel("UpdateDataFieldCS");
            DataFieldCS.SetBuffer (id, "_ParticleBufferRO", _particleBufferRO);
            DataFieldCS.SetTexture(id, "_DataFieldTexRW",   _dataFieldRenderTex);
            DataFieldCS.SetInts  ("_GridNum", new int[3] { (int)_cubeSize.x, (int)_cubeSize.y, (int)_cubeSize.z });
            DataFieldCS.SetVector("_GridCenter", GridCenter);
            DataFieldCS.SetVector("_GridSize",   GridSize  );
            DataFieldCS.Dispatch(id, ParticleNum / 32, 1, 1);
        }
        #endregion
    }
}