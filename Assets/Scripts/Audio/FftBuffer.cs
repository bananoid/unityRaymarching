using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Profiling;
using UnityEngine;

namespace Lasp.Vfx
{
    // Cooley–Tukey FFT vectorized/parallelized with the Burst compiler

    public sealed class FftBuffer : System.IDisposable
    {
        #region Public properties

        public int Width => _N;
        public NativeArray<float> Spectrum => _O;
        public NativeArray<float> BeatSignals => _B;
        
        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            if (_I.IsCreated) _I.Dispose();
            if (_O.IsCreated) _O.Dispose();
            if (_B.IsCreated) _B.Dispose();
            if (_FS1.IsCreated) _FS1.Dispose();
            if (_FS2.IsCreated) _FS2.Dispose();

            if (_W.IsCreated) _W.Dispose();
            if (_P.IsCreated) _P.Dispose();
            if (_T.IsCreated) _T.Dispose();
        }

        #endregion

        #region Public methods

        public FftBuffer(int width)
        {
            _N = width;
            _logN = (int)math.log2(width);

            _I = PersistentMemory.New<float>(_N);
            _O = PersistentMemory.New<float>(_N);
            _B = PersistentMemory.New<float>(_N);
            _FS1 = PersistentMemory.New<float>(_N);
            _FS2 = PersistentMemory.New<float>(_N);
            // for (var i = 0; i < _N/2; i++) {
            //     _B[i] = 0;
            //     _FS1[i] = _B[i]; 
            //     _FS2[i] = _B[i]; 
            // }
        
            InitializeWindow();
            BuildPermutationTable();
            BuildTwiddleFactors();
        }

        // Push audio data to the FIFO buffer.
        public void Push(NativeSlice<float> data)
        {
            var length = data.Length;

            if (length == 0) return;

            if (length < _N)
            {
                // The data is smaller than the buffer: Dequeue and copy
                var part = _N - length;
                NativeArray<float>.Copy(_I, _N - part, _I, 0, part);
                data.CopyTo(_I.GetSubArray(part, length));
            }
            else
            {
                // The data is larger than the buffer: Simple fill
                data.Slice(length - _N).CopyTo(_I);
            }
        }

        // Analyze the input buffer to calculate spectrum data.
        public void Analyze()
        {
            Profiler.BeginSample("Spectrum Analyer FFT");

            using (var X = TempJobMemory.New<float4>(_N / 2))
            {
                // Bit-reversal permutation and first DFT pass
                new FirstPassJob { I = _I, W = _W, P = _P, X = X }.Run(_N / 2);

                // 2nd and later DFT passes
                for (var i = 0; i < _logN - 1; i++)
                {
                    var T_slice = new NativeSlice<TFactor>(_T, _N / 4 * i);
                    new DftPassJob { T = T_slice, X = X }.Run(_N / 4);
                }

                // Postprocess (power spectrum calculation)
                var O2 = _O.Reinterpret<float2>(sizeof(float));
                new PostprocessJob { X = X, O = O2, s = 2.0f / _N }.Run(_N / 2);

                
                // var B2 = _B.Reinterpret<float2>(sizeof(float));
            }

            // using (var BX = TempJobMemory.New<float>( _N / 2 )){
            
            // new BeatDetectionJob { S = _O, B = _B }.Run( _N / 2 );

            // }
            
            if(Time.realtimeSinceStartup > 1){
                int bRad = 8;
                // for(var i=bRad; i<_N/2-bRad;i++){
                //     for(var j=1; j<=bRad; j++){
                //         _FS1[i] += _FS1[i+j] + _FS1[i-j];
                //         _FS1[i] /= 3;         
                //     }     
                // }        
                for(var j=0; j<bRad; j++){
                    for(var i=1; i<_N/2-1;i++){
                        _O[i] += _O[i+1] + _O[i-1];
                        _O[i] /= 3;         
                    }     
                }        
                    
                NativeArray<float>.Copy( _O, _B);    

                // for(var i=0; i<_N/2;i++){
                // // NativeArray<float>.Copy( _FS1, _B);
                //     _FS1[i] = math.max(_FS1[i], _O[i]);
                //     _FS1[i] *= 1 - 0.2f;
                //     _FS2[i] = math.lerp(_FS2[i], _O[i], 0.01f);
                //     // _FS2[i] = math.max(_FS2[i], _O[i]);
                //     // _FS2[i] -= 0.0004f;
                //     _B[i] =  math.abs(_FS2[i] - _FS1[i]);    
                //     // _B[i] =  math.step(_FS1[i], _FS2[i]);
                // }

                // for(var i=0; i<_N;i++){
                //     if(i>_N/2){
                //         _B[i] = _B[i - _N/2 ]; 
                //     }
                // }
            }else{
                for(var i=0; i<_N/2;i++){
                    _FS1[i] = 0;
                    _FS2[i] = 0;
                    _B[i] =  0;    
                } 
            }
            
            Profiler.EndSample();
        }

        #endregion

        #region Hanning window function

        NativeArray<float> _W;

        void InitializeWindow()
        {
            _W = PersistentMemory.New<float>(_N);
            for (var i = 0; i < _N; i++)
                _W[i] = (1 - math.cos(2 * math.PI * i / (_N - 1))) / 2;
        }

        #endregion

        #region Private members

        readonly int _N;
        readonly int _logN;
        NativeArray<float> _I;
        NativeArray<float> _O;
        NativeArray<float> _B;
        NativeArray<float> _FS1;
        NativeArray<float> _FS2;

        #endregion

        #region Bit-reversal permutation table

        NativeArray<int2> _P;

        void BuildPermutationTable()
        {
            _P = PersistentMemory.New<int2>(_N / 2);
            for (var i = 0; i < _N; i += 2)
                _P[i / 2] = math.int2(Permutate(i), Permutate(i + 1));
        }

        int Permutate(int x)
          => Enumerable.Range(0, _logN)
             .Aggregate(0, (a, i) => a += ((x >> i) & 1) << (_logN - 1 - i));

        #endregion

        #region Precalculated twiddle factors

        struct TFactor
        {
            public int2 I;
            public float2 W;

            public int i1 => I.x;
            public int i2 => I.y;

            public float4 W4
              => math.float4(W.x, math.sqrt(1 - W.x * W.x),
                             W.y, math.sqrt(1 - W.y * W.y));
        }

        NativeArray<TFactor> _T;

        void BuildTwiddleFactors()
        {
            _T = PersistentMemory.New<TFactor>((_logN - 1) * (_N / 4));

            var i = 0;
            for (var m = 4; m <= _N; m <<= 1)
                for (var k = 0; k < _N; k += m)
                    for (var j = 0; j < m / 2; j += 2)
                        _T[i++] = new TFactor
                          { I = math.int2((k + j) / 2, (k + j + m / 2) / 2),
                            W = math.cos(-2 * math.PI / m * math.float2(j, j + 1)) };
        }

        #endregion

        #region First pass job

        [Unity.Burst.BurstCompile(CompileSynchronously = true)]
        struct FirstPassJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> I;
            [ReadOnly] public NativeArray<float> W;
            [ReadOnly] public NativeArray<int2> P;
            [WriteOnly] public NativeArray<float4> X;

            public void Execute(int i)
            {
                var i1 = P[i].x;
                var i2 = P[i].y;
                var a1 = I[i1] * W[i1];
                var a2 = I[i2] * W[i2];
                X[i] = math.float4(a1 + a2, 0, a1 - a2, 0);
            }
        }

        #endregion

        #region DFT pass job

        [Unity.Burst.BurstCompile(CompileSynchronously = true)]
        struct DftPassJob : IJobParallelFor
        {
            [ReadOnly] public NativeSlice<TFactor> T;
            [NativeDisableParallelForRestriction] public NativeArray<float4> X;

            static float4 Mulc(float4 a, float4 b)
              => a.xxzz * b.xyzw + math.float4(-1, 1, -1, 1) * a.yyww * b.yxwz;

            public void Execute(int i)
            {
                var t = T[i];
                var e = X[t.i1];
                var o = Mulc(t.W4, X[t.i2]);
                X[t.i1] = e + o;
                X[t.i2] = e - o;
            }
        }

        #endregion

        #region Postprocess Job

        [Unity.Burst.BurstCompile(CompileSynchronously = true)]
        struct PostprocessJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float4> X;
            [WriteOnly] public NativeArray<float2> O;
            public float s;

            public void Execute(int i)
            {
                var x = X[i];
                O[i] = math.float2(math.length(x.xy), math.length(x.zw)) * s;
            }
        }

        #endregion

        #region BeatDetection Job
        [Unity.Burst.BurstCompile(CompileSynchronously = true)]
        struct BeatDetectionJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> S;
            public NativeArray<float> B;
     
            public void Execute(int i)
            {
                // // var x = X[i];
                // // O[i] = x;
                // // B[i] = math.sin(S[i] * math.PI * 2 * 60) * 0.5f + 0.5f;
                // float s = S[i];
                // float pb = PB[i]; 
                // float b = math.sin(i * 0.03f) * .5f + .5f + pb * 0.001f;
                // b += s * 20.1f;
                // // B[i] = b + pb; 
                // // B[i] = math.lerp(PB[i], S[i], 1f );
                // B[i] = b;
                // // PB[i] = b;

                B[i] = S[i] + B[i] * 0.1f;
            }
        }
        #endregion


        #region Native array utilities

        static class TempJobMemory
        {
            public static NativeArray<T> New<T>(int size) where T : unmanaged
              => new NativeArray<T>(size, Allocator.TempJob,
                                    NativeArrayOptions.UninitializedMemory);
        }

        static class PersistentMemory
        {
            public static NativeArray<T> New<T>(int size) where T : unmanaged
              => new NativeArray<T>(size, Allocator.Persistent,
                                    NativeArrayOptions.UninitializedMemory);
        }

        #endregion
    }
}
