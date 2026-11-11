
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Modules.UI.Particle
{
    // Custom Vertex Streams を ParticleSystemRenderer と同じ並びで頂点の UV (と法線・接線) に詰める.
    // 位置・向きの値 (Center・Velocity・Speed) は、頂点の位置と同じルートのキャンバスの座標で詰める.
    public sealed partial class UIParticleSystem
    {
        //----- params -----

        /// <summary> 頂点の UV に詰められる値の数 (uv0〜uv3 に 4 つずつ). </summary>
        private const int MaxStreamValueCount = 16;

        /// <summary> StableRandom の X の乱数 (Y・Z・W は 1 ずつ足した種から作る). </summary>
        private const uint StableRandomId = 0x3fb09a55;

        /// <summary> VaryingRandom の種に足す、寿命の割合 (0〜1) に掛ける値. </summary>
        private const float VaryingRandomSteps = 10000000f;

        /// <summary> ColorPackedAsTwoFloats の下位の値に掛ける値 (ParticleSystemRenderer と同じ). </summary>
        private const float PackedColorScale = 0.999f / 255f;

        /// <summary> ParticleSystemRenderer がリニア色空間で色を詰めるときの、ガンマの 8 ビットからリニアの 8 ビットへの変換表. </summary>
        private static readonly byte[] GammaToLinearBytes =
        {
            0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 4,
            4, 4, 4, 4, 5, 5, 5, 5, 5, 6, 6, 6, 6, 7, 7, 7, 8, 8, 8, 8, 9, 9, 9, 10, 10, 10, 11, 11, 12, 12,
            12, 13, 13, 14, 14, 14, 15, 15, 16, 16, 17, 17, 17, 18, 18, 19, 19, 20, 21, 21, 22, 22, 23, 23, 24, 24, 25, 26,
            26, 27, 27, 28, 29, 29, 30, 30, 31, 32, 32, 33, 34, 34, 35, 36, 37, 37, 38, 39, 40, 40, 41, 42, 43, 44, 45, 45,
            46, 47, 48, 49, 50, 51, 52, 53, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72,
            73, 75, 76, 77, 78, 79, 80, 82, 83, 84, 85, 86, 88, 89, 90, 91, 93, 94, 95, 97, 98, 99, 101, 102, 103, 105, 106, 107,
            109, 110, 111, 113, 114, 116, 117, 118, 120, 121, 123, 124, 125, 127, 128, 130, 131, 133, 134, 136, 137, 139, 141, 142, 144, 145,
            147, 149, 150, 152, 154, 156, 157, 159, 161, 163, 164, 166, 168, 170, 171, 173, 175, 177, 179, 181, 183, 184, 186, 188, 190, 192,
            194, 196, 198, 200, 202, 204, 206, 208, 210, 212, 214, 216, 218, 220, 222, 224, 227, 229, 231, 233, 235, 237, 239, 242, 244, 246,
            248, 250, 253, 255,
        };

        /// <summary> 頂点の番号ごとの、VertexHelper の頂点の番号 (四角ごとに上書きして使い回す). </summary>
        private static readonly int[] quadVertexIndices = new int[4];

        /// <summary> 四角の頂点の順 (左下・左上・右上・右下) ごとの、ParticleSystemRenderer の頂点の番号. </summary>
        private static readonly int[] BillboardVertexIds = { 3, 0, 1, 2 };

        /// <summary> 伸ばした四角の頂点の順ごとの、ParticleSystemRenderer の頂点の番号. </summary>
        private static readonly int[] StretchVertexIds = { 2, 3, 0, 1 };

        /// <summary> 四角の法線の作り方 (ParticleSystemRenderer の描き方ごとの違い). </summary>
        private enum QuadNormalMode
        {
            /// <summary> 角の向きに √½ を掛けて面の向きと混ぜる. </summary>
            Billboard,
            /// <summary> 角の向きをそのまま面の向きと混ぜる (Horizontal / Vertical Billboard・Freeform Stretching). </summary>
            Planar,
            /// <summary> 角の代わりに横・前後の軸の向きを使い、面の向きに縦横の比を掛けない (Stretched Billboard). </summary>
            Stretch,
        }

        /// <summary> UV に詰めるストリームと、詰める位置 (uv0.x から数えた番号). </summary>
        private struct VertexStreamSlot
        {
            public ParticleSystemVertexStream Stream;
            public int Offset;
            public int Count;
        }

        //----- field -----

        [SerializeField]
        private bool useCustomVertexStreams = false;

        private readonly List<ParticleSystemVertexStream> vertexStreams = new List<ParticleSystemVertexStream>();

        private VertexStreamSlot[] vertexStreamSlots = new VertexStreamSlot[0];

        private int vertexStreamSlotCount = 0;

        /// <summary> UV に詰める値の数. </summary>
        private int vertexStreamValueCount = 0;

        private bool useNormalStream = false;

        private bool useTangentStream = false;

        /// <summary> 選ばれている UV〜UV4 のストリームの数. </summary>
        private int uvStreamCount = 0;

        private readonly float[] vertexStreamValues = new float[MaxStreamValueCount];

        /// <summary> 四角の頂点の番号ごとの角の向き (粒ごとに上書きして使い回す). </summary>
        private readonly Vector3[] quadCorners = new Vector3[4];

        private readonly List<Vector4> customData1 = new List<Vector4>();

        private readonly List<Vector4> customData2 = new List<Vector4>();

        /// <summary> ワールドからルートのキャンバスの座標への変換. </summary>
        private Matrix4x4 worldToCanvas = Matrix4x4.identity;

        private float normalDirection = 1f;

        [NonSerialized]
        private bool vertexStreamWarned = false;

        #if UNITY_EDITOR

        [NonSerialized]
        private UnityEditor.SerializedObject rendererSerializedObject = null;

        [NonSerialized]
        private UnityEditor.SerializedProperty useCustomVertexStreamsProperty = null;

        #endif

        //----- property -----

        /// <summary>
        /// ParticleSystemRenderer の Custom Vertex Streams が有効か.
        /// 実行中に読む API が無いため、エディタでレンダラーの値を写して保存する.
        /// 実行中に SetActiveVertexStreams でストリームを変えたときは true にする.
        /// </summary>
        public bool UseCustomVertexStreams
        {
            get { return useCustomVertexStreams; }
            set { useCustomVertexStreams = value; }
        }

        //----- method -----

        #if UNITY_EDITOR

        /// <summary> エディタでレンダラーの Custom Vertex Streams の有効・無効を写す. </summary>
        private void SyncCustomVertexStreams(ParticleSystemRenderer particleSystemRenderer)
        {
            if (rendererSerializedObject == null || rendererSerializedObject.targetObject != particleSystemRenderer)
            {
                rendererSerializedObject = new UnityEditor.SerializedObject(particleSystemRenderer);

                useCustomVertexStreamsProperty = rendererSerializedObject.FindProperty("m_UseCustomVertexStreams");
            }

            if (useCustomVertexStreamsProperty == null) { return; }

            rendererSerializedObject.UpdateIfRequiredOrScript();

            var value = useCustomVertexStreamsProperty.boolValue;

            if (useCustomVertexStreams == value) { return; }

            useCustomVertexStreams = value;

            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        #endif

        /// <summary>
        /// 描画のあいだ変わらないストリームの並びを読む.
        /// Custom Vertex Streams が無効なときは false (UV だけを詰める).
        /// </summary>
        private bool ReadVertexStreams(ParticleSystemRenderer particleSystemRenderer, int particleCount)
        {
            vertexStreamSlotCount = 0;
            vertexStreamValueCount = 0;
            useNormalStream = false;
            useTangentStream = false;
            uvStreamCount = 0;

            if (!useCustomVertexStreams) { return false; }

            particleSystemRenderer.GetActiveVertexStreams(vertexStreams);

            if (vertexStreamSlots.Length < vertexStreams.Count)
            {
                vertexStreamSlots = new VertexStreamSlot[vertexStreams.Count];
            }

            var offset = 0;
            var overflow = false;

            for (var i = 0; i < vertexStreams.Count; ++i)
            {
                var stream = vertexStreams[i];

                switch (stream)
                {
                    // ※ 位置・色は頂点の位置・色に、法線・接線は頂点の法線・接線に入れる.
                    case ParticleSystemVertexStream.Position:
                    case ParticleSystemVertexStream.Color:
                        continue;

                    case ParticleSystemVertexStream.Normal:
                        useNormalStream = true;
                        continue;

                    case ParticleSystemVertexStream.Tangent:
                        useTangentStream = true;
                        continue;
                }

                if (ParticleSystemVertexStream.UV <= stream && stream <= ParticleSystemVertexStream.UV4)
                {
                    uvStreamCount++;
                }

                var count = GetVertexStreamValueCount(stream);

                if (MaxStreamValueCount < offset + count)
                {
                    overflow = true;
                }

                vertexStreamSlots[vertexStreamSlotCount++] = new VertexStreamSlot() { Stream = stream, Offset = offset, Count = count };

                offset += count;

                if (!IsSupportedVertexStream(stream) && !vertexStreamWarned)
                {
                    vertexStreamWarned = true;

                    Debug.LogError($"UIParticleSystem : VertexStream {stream} is not supported. (filled with zero)");
                }
            }

            vertexStreamValueCount = Math.Min(offset, MaxStreamValueCount);

            if (overflow && !vertexStreamWarned)
            {
                vertexStreamWarned = true;

                Debug.LogError($"UIParticleSystem : VertexStreams exceed {MaxStreamValueCount} values. (values over the limit are dropped)");
            }

            normalDirection = particleSystemRenderer.normalDirection;

            // ルートのキャンバスの座標 (頂点の位置と同じ座標) への変換.
            var currentCanvas = canvas;

            worldToCanvas = currentCanvas != null ? currentCanvas.rootCanvas.transform.worldToLocalMatrix : GetTransform().worldToLocalMatrix;

            ReadCustomData(particleCount);

            AddCanvasShaderChannels();

            return true;
        }

        /// <summary> カスタムデータを使うストリームがあるとき、粒ごとのカスタムデータを読む. </summary>
        private void ReadCustomData(int particleCount)
        {
            var useCustom1 = false;
            var useCustom2 = false;

            for (var i = 0; i < vertexStreamSlotCount; ++i)
            {
                var stream = vertexStreamSlots[i].Stream;

                useCustom1 |= ParticleSystemVertexStream.Custom1X <= stream && stream <= ParticleSystemVertexStream.Custom1XYZW;
                useCustom2 |= ParticleSystemVertexStream.Custom2X <= stream && stream <= ParticleSystemVertexStream.Custom2XYZW;
            }

            var particleSystem = GetParticleSystem();

            customData1.Clear();
            customData2.Clear();

            if (useCustom1) { particleSystem.GetCustomParticleData(customData1, ParticleSystemCustomData.Custom1); }
            if (useCustom2) { particleSystem.GetCustomParticleData(customData2, ParticleSystemCustomData.Custom2); }
        }

        /// <summary> 詰めた値を GPU へ送るのに要るチャンネルを、キャンバスの Additional Shader Channels に足す. </summary>
        private void AddCanvasShaderChannels()
        {
            var currentCanvas = canvas;

            if (currentCanvas == null) { return; }

            var channels = AdditionalCanvasShaderChannels.None;

            if (4 < vertexStreamValueCount) { channels |= AdditionalCanvasShaderChannels.TexCoord1; }
            if (8 < vertexStreamValueCount) { channels |= AdditionalCanvasShaderChannels.TexCoord2; }
            if (12 < vertexStreamValueCount) { channels |= AdditionalCanvasShaderChannels.TexCoord3; }

            if (useNormalStream) { channels |= AdditionalCanvasShaderChannels.Normal; }
            if (useTangentStream) { channels |= AdditionalCanvasShaderChannels.Tangent; }

            if ((currentCanvas.additionalShaderChannels & channels) == channels) { return; }

            currentCanvas.additionalShaderChannels |= channels;
        }

        /// <summary> ストリームが UV に詰める値の数. </summary>
        private static int GetVertexStreamValueCount(ParticleSystemVertexStream stream)
        {
            switch (stream)
            {
                case ParticleSystemVertexStream.UV:
                case ParticleSystemVertexStream.UV2:
                case ParticleSystemVertexStream.UV3:
                case ParticleSystemVertexStream.UV4:
                case ParticleSystemVertexStream.SizeXY:
                case ParticleSystemVertexStream.StableRandomXY:
                case ParticleSystemVertexStream.VaryingRandomXY:
                case ParticleSystemVertexStream.Custom1XY:
                case ParticleSystemVertexStream.Custom2XY:
                case ParticleSystemVertexStream.NoiseSumXY:
                case ParticleSystemVertexStream.NoiseImpulseXY:
                case ParticleSystemVertexStream.ColorPackedAsTwoFloats:
                    return 2;

                case ParticleSystemVertexStream.Center:
                case ParticleSystemVertexStream.SizeXYZ:
                case ParticleSystemVertexStream.Rotation3D:
                case ParticleSystemVertexStream.RotationSpeed3D:
                case ParticleSystemVertexStream.Velocity:
                case ParticleSystemVertexStream.StableRandomXYZ:
                case ParticleSystemVertexStream.VaryingRandomXYZ:
                case ParticleSystemVertexStream.Custom1XYZ:
                case ParticleSystemVertexStream.Custom2XYZ:
                case ParticleSystemVertexStream.NoiseSumXYZ:
                case ParticleSystemVertexStream.NoiseImpulseXYZ:
                case ParticleSystemVertexStream.MeshAxisOfRotation:
                case ParticleSystemVertexStream.NextTrailCenter:
                case ParticleSystemVertexStream.PreviousTrailCenter:
                    return 3;

                case ParticleSystemVertexStream.StableRandomXYZW:
                case ParticleSystemVertexStream.VaryingRandomXYZW:
                case ParticleSystemVertexStream.Custom1XYZW:
                case ParticleSystemVertexStream.Custom2XYZW:
                    return 4;
            }

            return 1;
        }

        /// <summary> UIParticleSystem が値を作れるストリームか (作れないストリームは 0 を詰める). </summary>
        private static bool IsSupportedVertexStream(ParticleSystemVertexStream stream)
        {
            switch (stream)
            {
                case ParticleSystemVertexStream.NoiseSumX:
                case ParticleSystemVertexStream.NoiseSumXY:
                case ParticleSystemVertexStream.NoiseSumXYZ:
                case ParticleSystemVertexStream.NoiseImpulseX:
                case ParticleSystemVertexStream.NoiseImpulseXY:
                case ParticleSystemVertexStream.NoiseImpulseXYZ:
                case ParticleSystemVertexStream.NextTrailCenter:
                case ParticleSystemVertexStream.PreviousTrailCenter:
                case ParticleSystemVertexStream.PercentageAlongTrail:
                case ParticleSystemVertexStream.TrailWidth:
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 粒ごとに決まるストリームの値を詰める (頂点ごとに変わる値は頂点を書くときに詰める).
        /// center: 粒の位置 (自分のローカル座標). sheetFrame: コマ送りのコマの位置 (小数を含む).
        /// </summary>
        private void SetParticleStreamValues(ParticleSystem.Particle particle, int particleIndex, Vector3 center, Vector3 simulationVelocity, Vector3 size, Color32 color, float sheetFrame)
        {
            var values = vertexStreamValues;

            Array.Clear(values, 0, values.Length);

            for (var i = 0; i < vertexStreamSlotCount; ++i)
            {
                var slot = vertexStreamSlots[i];

                var offset = slot.Offset;

                switch (slot.Stream)
                {
                    case ParticleSystemVertexStream.AnimBlend:
                        WriteStreamValue(values, offset, sheetFrame - Mathf.Floor(sheetFrame));
                        break;

                    case ParticleSystemVertexStream.AnimFrame:
                        WriteStreamValue(values, offset, sheetFrame);
                        break;

                    case ParticleSystemVertexStream.Center:
                        WriteStreamVector(values, offset, slot.Count, GetCanvasPosition(center));
                        break;

                    case ParticleSystemVertexStream.SizeX:
                    case ParticleSystemVertexStream.SizeXY:
                    case ParticleSystemVertexStream.SizeXYZ:
                        WriteStreamVector(values, offset, slot.Count, size);
                        break;

                    case ParticleSystemVertexStream.Rotation:
                        WriteStreamValue(values, offset, particle.rotation * Mathf.Deg2Rad);
                        break;

                    case ParticleSystemVertexStream.Rotation3D:
                        WriteStreamVector(values, offset, slot.Count, particle.rotation3D * Mathf.Deg2Rad);
                        break;

                    case ParticleSystemVertexStream.RotationSpeed:
                        WriteStreamValue(values, offset, particle.angularVelocity * Mathf.Deg2Rad);
                        break;

                    case ParticleSystemVertexStream.RotationSpeed3D:
                        WriteStreamVector(values, offset, slot.Count, particle.angularVelocity3D * Mathf.Deg2Rad);
                        break;

                    case ParticleSystemVertexStream.Velocity:
                        WriteStreamVector(values, offset, slot.Count, GetCanvasVelocity(simulationVelocity));
                        break;

                    case ParticleSystemVertexStream.Speed:
                        WriteStreamValue(values, offset, GetCanvasVelocity(simulationVelocity).magnitude);
                        break;

                    case ParticleSystemVertexStream.AgePercent:
                        WriteStreamValue(values, offset, 0f < particle.startLifetime ? 1f - particle.remainingLifetime / particle.startLifetime : 0f);
                        break;

                    case ParticleSystemVertexStream.InvStartLifetime:
                        WriteStreamValue(values, offset, 0f < particle.startLifetime ? 1f / particle.startLifetime : 0f);
                        break;

                    case ParticleSystemVertexStream.StableRandomX:
                    case ParticleSystemVertexStream.StableRandomXY:
                    case ParticleSystemVertexStream.StableRandomXYZ:
                    case ParticleSystemVertexStream.StableRandomXYZW:
                        WriteStreamVector(values, offset, slot.Count, GetStableRandom(particle));
                        break;

                    case ParticleSystemVertexStream.VaryingRandomX:
                    case ParticleSystemVertexStream.VaryingRandomXY:
                    case ParticleSystemVertexStream.VaryingRandomXYZ:
                    case ParticleSystemVertexStream.VaryingRandomXYZW:
                        WriteStreamVector(values, offset, slot.Count, GetVaryingRandom(particle));
                        break;

                    case ParticleSystemVertexStream.Custom1X:
                    case ParticleSystemVertexStream.Custom1XY:
                    case ParticleSystemVertexStream.Custom1XYZ:
                    case ParticleSystemVertexStream.Custom1XYZW:
                        WriteStreamVector(values, offset, slot.Count, particleIndex < customData1.Count ? customData1[particleIndex] : Vector4.zero);
                        break;

                    case ParticleSystemVertexStream.Custom2X:
                    case ParticleSystemVertexStream.Custom2XY:
                    case ParticleSystemVertexStream.Custom2XYZ:
                    case ParticleSystemVertexStream.Custom2XYZW:
                        WriteStreamVector(values, offset, slot.Count, particleIndex < customData2.Count ? customData2[particleIndex] : Vector4.zero);
                        break;

                    case ParticleSystemVertexStream.MeshIndex:
                        WriteStreamValue(values, offset, GetParticleMeshIndex(particle));
                        break;

                    case ParticleSystemVertexStream.ParticleIndex:
                        WriteStreamValue(values, offset, particleIndex);
                        break;

                    case ParticleSystemVertexStream.MeshAxisOfRotation:
                        WriteStreamVector(values, offset, slot.Count, particle.axisOfRotation);
                        break;

                    // ※ メッシュの頂点色は掛けない、粒の色.
                    case ParticleSystemVertexStream.ColorPackedAsTwoFloats:
                        WriteStreamVector(values, offset, slot.Count, PackColor(color));
                        break;
                }
            }
        }

        /// <summary>
        /// 頂点ごとに変わるストリームの値を詰めて、頂点の UV に写す.
        /// uv3 / uv4: UV3・UV4 のストリームの値 (メッシュの 3・4 つ目の UV. それ以外は 0).
        /// </summary>
        private void SetVertexStreams(ref UIVertex vertex, int vertexId, Vector2 uv, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            var values = vertexStreamValues;

            for (var i = 0; i < vertexStreamSlotCount; ++i)
            {
                var slot = vertexStreamSlots[i];

                var offset = slot.Offset;

                switch (slot.Stream)
                {
                    case ParticleSystemVertexStream.UV:
                        WriteStreamVector(values, offset, 2, uv);
                        break;

                    case ParticleSystemVertexStream.UV2:
                        WriteStreamVector(values, offset, 2, uv2);
                        break;

                    case ParticleSystemVertexStream.UV3:
                        WriteStreamVector(values, offset, 2, uv3);
                        break;

                    case ParticleSystemVertexStream.UV4:
                        WriteStreamVector(values, offset, 2, uv4);
                        break;

                    case ParticleSystemVertexStream.VertexID:
                        WriteStreamValue(values, offset, vertexId);
                        break;
                }
            }

            vertex.uv0 = new Vector4(values[0], values[1], values[2], values[3]);
            vertex.uv1 = new Vector4(values[4], values[5], values[6], values[7]);
            vertex.uv2 = new Vector4(values[8], values[9], values[10], values[11]);
            vertex.uv3 = new Vector4(values[12], values[13], values[14], values[15]);
        }

        /// <summary>
        /// 四角の頂点にストリームの値を詰める.
        /// vertexIds: 四角の頂点の順ごとの ParticleSystemRenderer の頂点の番号.
        /// center: 法線の角の向きを測る位置 (四角と同じ座標).
        /// uv / uv2: 頂点の番号 0〜3 の角に対応する UV の範囲 (x, y が左下、z, w が右上).
        /// </summary>
        private void SetQuadVertexStreams(UIVertex[] quad, int[] vertexIds, QuadNormalMode normalMode, Vector3 center, Vector4 uv, Vector4 uv2)
        {
            if (useNormalStream || useTangentStream)
            {
                SetQuadNormals(quad, vertexIds, normalMode, center, uv);
            }

            for (var i = 0; i < 4; ++i)
            {
                var vertexId = vertexIds[i];

                SetVertexStreams(ref quad[i], vertexId, GetQuadCornerUv(uv, vertexId), GetQuadCornerUv(uv2, vertexId), Vector2.zero, Vector2.zero);
            }
        }

        /// <summary>
        /// ストリームを詰めた四角を足す.
        /// ※ VertexHelper.AddUIVertexQuad は uv2・uv3 を写さないので、頂点を1つずつ足す.
        ///   三角形は ParticleSystemRenderer と同じく、頂点の番号 (0, 1, 2) と (0, 2, 3) で張る
        ///   (頂点ごとの値の補間が対角線で変わるため).
        /// </summary>
        private static void AddStreamQuad(VertexHelper vh, UIVertex[] quad, int[] vertexIds)
        {
            var startIndex = vh.currentVertCount;

            var indices = quadVertexIndices;

            for (var i = 0; i < 4; ++i)
            {
                vh.AddVert(quad[i]);

                indices[vertexIds[i]] = startIndex + i;
            }

            vh.AddTriangle(indices[0], indices[1], indices[2]);
            vh.AddTriangle(indices[0], indices[2], indices[3]);
        }

        /// <summary> 頂点の番号の角の UV (0: 左上, 1: 右上, 2: 右下, 3: 左下). </summary>
        private static Vector2 GetQuadCornerUv(Vector4 uv, int vertexId)
        {
            switch (vertexId)
            {
                case 0: return new Vector2(uv.x, uv.w);
                case 1: return new Vector2(uv.z, uv.w);
                case 2: return new Vector2(uv.z, uv.y);
            }

            return new Vector2(uv.x, uv.y);
        }

        /// <summary>
        /// 四角の法線・接線 (頂点の番号 0: 左上, 1: 右上, 2: 右下, 3: 左下 の角で決める).
        /// ParticleSystemRenderer と同じく、法線は角の向きと、頂点 0 と 1 の角の向きの外積 (面の向き) を cos(normalDirection × 90°) で混ぜる.
        /// 角の向きは、頂点 0・1 が center から頂点への向きで、頂点 2・3 はそれぞれ頂点 0・1 の逆向き.
        /// 接線は UV の u が増える向きで、w は -1 に UV の反転の向きを掛ける.
        /// uv: 頂点の番号 0〜3 の角に対応する UV の範囲.
        /// </summary>
        private void SetQuadNormals(UIVertex[] quad, int[] vertexIds, QuadNormalMode normalMode, Vector3 center, Vector4 uv)
        {
            Vector3 topLeft = Vector3.zero, topRight = Vector3.zero, bottomRight = Vector3.zero, bottomLeft = Vector3.zero;

            for (var i = 0; i < 4; ++i)
            {
                switch (vertexIds[i])
                {
                    case 0: topLeft = quad[i].position; break;
                    case 1: topRight = quad[i].position; break;
                    case 2: bottomRight = quad[i].position; break;
                    default: bottomLeft = quad[i].position; break;
                }
            }

            var right = bottomRight - bottomLeft;
            var up = topLeft - bottomLeft;

            var width = right.magnitude;
            var height = up.magnitude;

            var mix = Mathf.Cos(normalDirection * Mathf.PI * 0.5f);

            var cornerRate = normalMode == QuadNormalMode.Billboard ? PlanarBillboardScale * mix : mix;

            // UV の反転の向き (u・v が角の並びと逆に増えるときは -1).
            var flipU = uv.z < uv.x ? -1f : 1f;
            var flipV = uv.w < uv.y ? -1f : 1f;

            var tangentDirection = 0f < width ? right * (flipU / width) : Vector3.right;

            var tangent = new Vector4(tangentDirection.x, tangentDirection.y, tangentDirection.z, -flipU * flipV);

            var rightDirection = 0f < width ? right / width : Vector3.zero;
            var upDirection = 0f < height ? up / height : Vector3.zero;

            // 頂点の番号ごとの角の向き.
            // ※ Stretched Billboard は、角の代わりに 0: 左の辺の向き, 1: 下の辺の向き, 2・3: その逆 を使う.
            var corners = quadCorners;

            if (normalMode == QuadNormalMode.Stretch)
            {
                corners[0] = upDirection;
                corners[1] = rightDirection;
                corners[2] = -upDirection;
                corners[3] = -rightDirection;
            }
            else
            {
                corners[0] = (topLeft - center).normalized;
                corners[1] = (topRight - center).normalized;
                corners[2] = -corners[0];
                corners[3] = -corners[1];
            }

            var plane = Vector3.Cross(corners[0], corners[1]) * (1f - cornerRate);

            for (var i = 0; i < 4; ++i)
            {
                quad[i].normal = corners[vertexIds[i]] * cornerRate + plane;
                quad[i].tangent = tangent;
            }
        }

        /// <summary> 粒ごとに変わらない乱数 (StableRandom. ParticleSystemRenderer と同じく、成分ごとに続いた番号の種から作る). </summary>
        private static Vector4 GetStableRandom(ParticleSystem.Particle particle)
        {
            var seed = particle.randomSeed + StableRandomId;

            return new Vector4(GenerateRandom(seed), GenerateRandom(seed + 1u), GenerateRandom(seed + 2u), GenerateRandom(seed + 3u));
        }

        /// <summary>
        /// 粒の寿命で変わる乱数 (VaryingRandom).
        /// ParticleSystemRenderer と同じく、StableRandom の種に寿命の割合 × VaryingRandomSteps を足した種から作る.
        /// </summary>
        private static Vector4 GetVaryingRandom(ParticleSystem.Particle particle)
        {
            var agePercent = 0f < particle.startLifetime ? 1f - particle.remainingLifetime / particle.startLifetime : 0f;

            var seed = particle.randomSeed + StableRandomId + (uint)Math.Max(0f, agePercent * VaryingRandomSteps);

            return new Vector4(GenerateRandom(seed), GenerateRandom(seed + 1u), GenerateRandom(seed + 2u), GenerateRandom(seed + 3u));
        }

        /// <summary> Center のストリームの値を詰め直す (center: 自分のローカル座標). </summary>
        private void SetCenterStreamValue(Vector3 center)
        {
            var position = GetCanvasPosition(center);

            for (var i = 0; i < vertexStreamSlotCount; ++i)
            {
                var slot = vertexStreamSlots[i];

                if (slot.Stream != ParticleSystemVertexStream.Center) { continue; }

                WriteStreamVector(vertexStreamValues, slot.Offset, slot.Count, position);
            }
        }

        /// <summary> 自分のローカル座標の位置をルートのキャンバスの座標に直す. </summary>
        private Vector3 GetCanvasPosition(Vector3 localPosition)
        {
            return worldToCanvas.MultiplyPoint3x4(orientationSettings.LocalToWorld.MultiplyPoint3x4(localPosition));
        }

        /// <summary> ワールドの速度をルートのキャンバスの座標に直す. </summary>
        private Vector3 GetCanvasVelocity(Vector3 simulationVelocity)
        {
            return worldToCanvas.MultiplyVector(orientationSettings.SimulationToWorld.MultiplyVector(simulationVelocity));
        }

        /// <summary>
        /// ColorPackedAsTwoFloats の値 (ParticleSystemRenderer と同じく、上位が R・B、下位が G・A).
        /// リニア色空間では RGB を ParticleSystemRenderer と同じ変換表でリニアにした 8 ビットの値を詰める.
        /// </summary>
        private static Vector2 PackColor(Color32 color)
        {
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                color = new Color32(GammaToLinearBytes[color.r], GammaToLinearBytes[color.g], GammaToLinearBytes[color.b], color.a);
            }

            return new Vector2(color.r + color.g * PackedColorScale, color.b + color.a * PackedColorScale);
        }

        private static void WriteStreamValue(float[] values, int offset, float value)
        {
            if (MaxStreamValueCount <= offset) { return; }

            values[offset] = value;
        }

        private static void WriteStreamVector(float[] values, int offset, int count, Vector4 value)
        {
            for (var i = 0; i < count; ++i)
            {
                WriteStreamValue(values, offset + i, value[i]);
            }
        }
    }
}
