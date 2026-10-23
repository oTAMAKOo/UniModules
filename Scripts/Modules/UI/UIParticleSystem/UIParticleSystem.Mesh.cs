
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Modules.UI.Particle
{
    // Render Mode が Mesh の粒を ParticleSystemRenderer と同じ形で描く.
    public sealed partial class UIParticleSystem
    {
        //----- params -----

        /// <summary> 1回の描画で作る頂点の数の上限 (uGUI のメッシュの頂点の上限). </summary>
        private const int MaxVertexCount = 65000;

        /// <summary> メッシュの反転の乱数 (この種から続けて X・Y・Z の順に作る). </summary>
        private const uint MeshFlipRandomId = 0x6e4f2bc1;

        /// <summary> 粒に使うメッシュの頂点 (メッシュが替わったときだけ読み直す). </summary>
        private sealed class ParticleMeshData
        {
            public Mesh Mesh;
            public Vector3[] Vertices;
            public Vector2[] Uvs;
            /// <summary> 2〜4つ目の UV (UV2〜UV4 のストリームに使う. メッシュに無いときは1つ前の UV). </summary>
            public Vector2[] Uvs2;
            public Vector2[] Uvs3;
            public Vector2[] Uvs4;
            public Vector3[] Normals;
            public Vector4[] Tangents;
            public Color32[] Colors;
            public int[] Triangles;
            /// <summary> メッシュの原点から最も遠い頂点までの長さ. </summary>
            public float Radius;
            /// <summary> メッシュの bounds の大きさ (pivot の単位). </summary>
            public Vector3 BoundsSize;
        }

        //----- field -----

        private Mesh[] particleMeshes = null;

        private ParticleMeshData[] particleMeshData = null;

        private int particleMeshCount = 0;

        [NonSerialized]
        private bool meshReadableWarned = false;

        [NonSerialized]
        private bool vertexLimitWarned = false;

        //----- method -----

        /// <summary> レンダラーのメッシュを読む (頂点はメッシュが替わったときだけ読み直す). </summary>
        private void ReadParticleMeshes(ParticleSystemRenderer particleSystemRenderer)
        {
            var count = particleSystemRenderer.meshCount;

            if (particleMeshes == null || particleMeshes.Length < count)
            {
                particleMeshes = new Mesh[count];

                var data = new ParticleMeshData[count];

                if (particleMeshData != null)
                {
                    Array.Copy(particleMeshData, data, Math.Min(particleMeshData.Length, count));
                }

                particleMeshData = data;
            }

            particleMeshCount = 0 < count ? particleSystemRenderer.GetMeshes(particleMeshes) : 0;

            for (var i = 0; i < particleMeshCount; ++i)
            {
                var mesh = particleMeshes[i];

                var data = particleMeshData[i];

                if (data != null && data.Mesh == mesh) { continue; }

                particleMeshData[i] = LoadParticleMesh(mesh);
            }
        }

        /// <summary> メッシュの頂点を読む (読めないメッシュは null). </summary>
        private ParticleMeshData LoadParticleMesh(Mesh mesh)
        {
            if (mesh == null) { return null; }

            // ※ Read/Write が無効なメッシュは、ビルドしたアプリでは頂点を読めない.
            if (!mesh.isReadable)
            {
                if (!meshReadableWarned)
                {
                    meshReadableWarned = true;

                    Debug.LogError($"UIParticleSystem : Mesh {mesh.name} is not readable. (enable Read/Write in the import settings)");
                }

                if (!Application.isEditor) { return null; }
            }

            var data = new ParticleMeshData()
            {
                Mesh = mesh,
                Vertices = mesh.vertices,
                Triangles = mesh.triangles,
                BoundsSize = mesh.bounds.size,
            };

            var vertexCount = data.Vertices.Length;

            var uvs = mesh.uv;
            var uvs2 = mesh.uv2;
            var uvs3 = mesh.uv3;
            var uvs4 = mesh.uv4;
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            var colors = mesh.colors32;

            data.Uvs = uvs.Length == vertexCount ? uvs : null;

            // ※ ParticleSystemRenderer と同じく、UV2〜UV4 のストリームはメッシュにその UV が無いとき1つ前のストリームと同じ UV を使う.
            data.Uvs2 = uvs2.Length == vertexCount ? uvs2 : data.Uvs;
            data.Uvs3 = uvs3.Length == vertexCount ? uvs3 : data.Uvs2;
            data.Uvs4 = uvs4.Length == vertexCount ? uvs4 : data.Uvs3;
            data.Normals = normals.Length == vertexCount ? normals : null;
            data.Tangents = tangents.Length == vertexCount ? tangents : null;
            data.Colors = colors.Length == vertexCount ? colors : null;

            var radius = 0f;

            for (var i = 0; i < data.Vertices.Length; ++i)
            {
                radius = Math.Max(radius, data.Vertices[i].sqrMagnitude);
            }

            data.Radius = Mathf.Sqrt(radius);

            return data;
        }

        /// <summary> 粒に使うメッシュ (無いときは null). </summary>
        private ParticleMeshData GetParticleMeshData(ParticleSystem.Particle particle)
        {
            if (particleMeshCount <= 0) { return null; }

            return particleMeshData[Mathf.Clamp(GetParticleMeshIndex(particle), 0, particleMeshCount - 1)];
        }

        /// <summary>
        /// 粒のメッシュの番号.
        /// ※ Particle.GetMeshIndex は、Render Mode が Mesh でメッシュがあるときだけ呼ぶ (それ以外で呼ぶとエディタが落ちる). それ以外は 0.
        /// </summary>
        private int GetParticleMeshIndex(ParticleSystem.Particle particle)
        {
            if (rendererSettings.RenderMode != ParticleSystemRenderMode.Mesh || particleMeshCount <= 1) { return 0; }

            return particle.GetMeshIndex(GetParticleSystem());
        }

        /// <summary> 粒に使うメッシュの、原点から最も遠い頂点までの長さの最大. </summary>
        private float GetParticleMeshRadius()
        {
            var radius = 0f;

            for (var i = 0; i < particleMeshCount; ++i)
            {
                var data = particleMeshData[i];

                if (data == null) { continue; }

                radius = Math.Max(radius, data.Radius);
            }

            return radius;
        }

        /// <summary>
        /// 粒のメッシュを置く向き (ワールド).
        /// View はカメラの向き、Facing はカメラの位置から粒への向き、Velocity は速度の向きを前にする.
        /// </summary>
        private Quaternion GetMeshFrameRotation(Vector3 worldCenter, Vector3 simulationVelocity)
        {
            var settings = orientationSettings;
            var view = viewSettings;

            switch (settings.Alignment)
            {
                case ParticleSystemRenderSpace.World:
                    return Quaternion.identity;

                case ParticleSystemRenderSpace.Local:
                    return settings.LocalRotation;

                case ParticleSystemRenderSpace.Facing:
                    {
                        if (!view.HasCamera) { return view.Rotation; }

                        var forward = worldCenter - view.Position;

                        if (forward.sqrMagnitude < Tolerance) { return view.Rotation; }

                        return Quaternion.LookRotation(forward, view.Up);
                    }

                case ParticleSystemRenderSpace.Velocity:
                    {
                        var velocity = settings.SimulationToWorld.MultiplyVector(simulationVelocity);

                        if (velocity.sqrMagnitude < Tolerance) { return Quaternion.identity; }

                        return Quaternion.LookRotation(velocity, Vector3.up);
                    }
            }

            return view.Rotation;
        }

        /// <summary>
        /// 粒のメッシュの頂点を、自分のローカル座標へ写す行列.
        /// ParticleSystemRenderer と同じく、向き × 拡大率 × 粒の回転 × 大きさ の順に掛ける.
        /// 反転はメッシュの軸ごとに鏡に映す. pivot はメッシュの bounds の大きさ × 粒の大きさを単位にし、z は逆向きにずらす (反転した軸は pivot も反転する).
        /// normalMatrix: 法線・接線を写す行列 (ParticleSystemRenderer と同じく、拡大率と大きさを逆数にした行列. 写したあとに長さを 1 にする).
        /// </summary>
        private Matrix4x4 GetParticleMeshMatrix(ParticleSystem.Particle particle, ParticleMeshData data, Vector3 center, Vector3 simulationVelocity, Vector3 size, out Matrix4x4 normalMatrix)
        {
            var settings = orientationSettings;

            var worldCenter = settings.LocalToWorld.MultiplyPoint3x4(center);

            var particleRotation = Quaternion.Euler(particle.rotation3D);

            if (settings.RollAngle != 0f)
            {
                particleRotation *= Quaternion.Euler(0f, 0f, settings.RollAngle);
            }

            var frameRotation = Matrix4x4.Rotate(GetMeshFrameRotation(worldCenter, simulationVelocity));
            var rotation = Matrix4x4.Rotate(particleRotation);

            var linear = frameRotation * Matrix4x4.Scale(settings.LossyScale) * rotation;

            var meshSize = size;

            if (rendererSettings.UseFlip)
            {
                meshSize = Vector3.Scale(meshSize, GetMeshFlipSigns(particle));
            }

            normalMatrix = settings.WorldToLocal * frameRotation * Matrix4x4.Scale(Reciprocal(settings.LossyScale)) * rotation * Matrix4x4.Scale(Reciprocal(meshSize));

            if (rendererSettings.UsePivot)
            {
                var pivot = Vector3.Scale(rendererSettings.Pivot, data.BoundsSize);

                worldCenter += linear.MultiplyVector(new Vector3(pivot.x * meshSize.x, pivot.y * meshSize.y, -pivot.z * meshSize.z));
            }

            var matrix = linear * Matrix4x4.Scale(meshSize);

            matrix.SetColumn(3, new Vector4(worldCenter.x, worldCenter.y, worldCenter.z, 1f));

            return settings.WorldToLocal * matrix;
        }

        /// <summary> メッシュの UV を次のコマの範囲に写す (UV が無いときは 0). </summary>
        private static Vector2 GetNextFrameMeshUv(Vector2[] uvs, int index, Vector4 nextUv, Vector2 nextUvSize)
        {
            if (uvs == null) { return Vector2.zero; }

            var uv = uvs[index];

            return new Vector2(nextUv.x + uv.x * nextUvSize.x, nextUv.y + uv.y * nextUvSize.y);
        }

        /// <summary> 成分ごとの逆数 (0 は 0 のまま). </summary>
        private static Vector3 Reciprocal(Vector3 value)
        {
            return new Vector3(
                value.x != 0f ? 1f / value.x : 0f,
                value.y != 0f ? 1f / value.y : 0f,
                value.z != 0f ? 1f / value.z : 0f);
        }

        /// <summary> 粒ごとの乱数で決める、メッシュを鏡に映す軸 (-1 で反転). </summary>
        private Vector3 GetMeshFlipSigns(ParticleSystem.Particle particle)
        {
            var flip = rendererSettings.Flip;
            var flipZ = rendererSettings.FlipZ;

            float randomX, randomY, randomZ;

            GenerateRandoms(particle.randomSeed + MeshFlipRandomId, out randomX, out randomY, out randomZ);

            return new Vector3(
                0f < flip.x && randomX < flip.x ? -1f : 1f,
                0f < flip.y && randomY < flip.y ? -1f : 1f,
                0f < flipZ && randomZ < flipZ ? -1f : 1f);
        }

        /// <summary>
        /// 粒のメッシュの頂点を足す.
        /// 色は ParticleSystemRenderer と同じく、粒の色 (リニア色空間ではリニアにした色) にメッシュの頂点色をそのまま掛ける.
        /// UV はメッシュの UV をコマの範囲に写す. ストリームの UV2〜UV4 は、メッシュの 2〜4つ目の UV (無いときは1つ前のストリームと同じ UV) を次のコマの範囲に写す.
        /// ParticleSystemRenderer と同じく、UVn のストリームは n が選ばれている UV〜UV4 のストリームの数以下のときだけ詰め、それ以外は 0.
        /// 頂点の数が上限を超えるときは足さずに false.
        /// </summary>
        private bool AddParticleMesh(VertexHelper vh, ParticleMeshData data, Matrix4x4 matrix, Matrix4x4 normalMatrix, Color32 color, Vector4 uv, bool useVertexStreams, Vector4 nextUv)
        {
            var vertices = data.Vertices;
            var uvs = data.Uvs;
            var uvs2 = data.Uvs2;
            var uvs3 = data.Uvs3;
            var uvs4 = data.Uvs4;
            var normals = data.Normals;
            var tangents = data.Tangents;
            var colors = data.Colors;
            var triangles = data.Triangles;

            var startIndex = vh.currentVertCount;

            if (MaxVertexCount < startIndex + vertices.Length)
            {
                if (!vertexLimitWarned)
                {
                    vertexLimitWarned = true;

                    Debug.LogError($"UIParticleSystem : Vertex count exceeds the limit {MaxVertexCount}. (particles over the limit are not rendered)");
                }

                return false;
            }

            var isLinear = colors != null && QualitySettings.activeColorSpace == ColorSpace.Linear;

            var particleColor = isLinear ? ((Color)color).linear : (Color)color;

            var uvSize = new Vector2(uv.z - uv.x, uv.w - uv.y);
            var nextUvSize = new Vector2(nextUv.z - nextUv.x, nextUv.w - nextUv.y);

            var useNormals = useVertexStreams && useNormalStream;
            var useTangents = useVertexStreams && useTangentStream;

            var vertex = UIVertex.simpleVert;

            vertex.color = color;

            for (var i = 0; i < vertices.Length; ++i)
            {
                vertex.position = matrix.MultiplyPoint3x4(vertices[i]);

                if (colors != null)
                {
                    var vertexColor = particleColor * (Color)colors[i];

                    vertex.color = isLinear ? vertexColor.gamma : vertexColor;
                }

                var meshUv = uvs != null ? uvs[i] : Vector2.zero;

                var vertexUv = new Vector2(uv.x + meshUv.x * uvSize.x, uv.y + meshUv.y * uvSize.y);

                vertex.uv0 = vertexUv;

                if (useVertexStreams)
                {
                    var vertexUv2 = 2 <= uvStreamCount ? GetNextFrameMeshUv(uvs2, i, nextUv, nextUvSize) : Vector2.zero;
                    var vertexUv3 = 3 <= uvStreamCount ? GetNextFrameMeshUv(uvs3, i, nextUv, nextUvSize) : Vector2.zero;
                    var vertexUv4 = 4 <= uvStreamCount ? GetNextFrameMeshUv(uvs4, i, nextUv, nextUvSize) : Vector2.zero;

                    if (useNormals && normals != null)
                    {
                        vertex.normal = normalMatrix.MultiplyVector(normals[i]).normalized;
                    }

                    if (useTangents && tangents != null)
                    {
                        var tangent = tangents[i];

                        var direction = normalMatrix.MultiplyVector(tangent).normalized;

                        vertex.tangent = new Vector4(direction.x, direction.y, direction.z, tangent.w);
                    }

                    SetVertexStreams(ref vertex, i, vertexUv, vertexUv2, vertexUv3, vertexUv4);
                }

                vh.AddVert(vertex);
            }

            for (var i = 0; i + 2 < triangles.Length; i += 3)
            {
                vh.AddTriangle(startIndex + triangles[i], startIndex + triangles[i + 1], startIndex + triangles[i + 2]);
            }

            return true;
        }
    }
}
