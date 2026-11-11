
using UnityEngine;
using System;

namespace Modules.UI.Particle
{
    // 粒の四角の向き (Render Alignment・Horizontal / Vertical Billboard・伸ばして描く粒) を ParticleSystemRenderer に合わせて決める.
    public sealed partial class UIParticleSystem
    {
        //----- params -----

        /// <summary> Horizontal / Vertical Billboard の四角の大きさに掛ける値 (ParticleSystemRenderer と同じ). </summary>
        private const float PlanarBillboardScale = 0.70710678f;

        /// <summary> 向きの基準が自分の平面と同じとみなす誤差. </summary>
        private const float FlatTolerance = 0.0001f;

        /// <summary> 粒の向きを決める、見ている側の設定 (ワールド座標). </summary>
        private struct ViewSettings
        {
            /// <summary> 位置のあるカメラで見ているか (無いときはキャンバスの正面から正射影で見る). </summary>
            public bool HasCamera;
            public bool Orthographic;
            public Vector3 Position;
            public Vector3 Right;
            public Vector3 Up;
            public Vector3 Forward;
            public Quaternion Rotation;
            /// <summary> カメラの傾き (roll) を打ち消す、見ている向きまわりの角度 (度). </summary>
            public float UnrollAngle;
            /// <summary> ワールドからカメラの空間への変換. </summary>
            public Matrix4x4 WorldToCamera;
            /// <summary> 透視投影のとき、奥行き 1 あたりの画面の幅. </summary>
            public float ViewportWidthPerDepth;
            /// <summary> 正射影・カメラが無いときの画面の幅. </summary>
            public float ViewportWidth;
        }

        /// <summary>
        /// 四角を組む向きの基準 (自分のローカル座標の、粒の大きさ 1 あたりの向きと長さ).
        /// ワールドの向きから作るときは、ワールドの軸ごとの拡大率 (lossyScale) を掛けて自分のローカル座標に直す.
        /// UseScale のときは、軸には拡大率を掛けず、粒を回したあとの基準の軸ごとに Scale を掛ける.
        /// </summary>
        private struct OrientationFrame
        {
            public Vector3 X;
            public Vector3 Y;
            public Vector3 Z;
            public bool UseScale;
            public Vector3 Scale;
        }

        /// <summary> 描画のあいだ変わらない向きの設定. </summary>
        private struct OrientationSettings
        {
            public ParticleSystemRenderSpace Alignment;
            public bool AllowRoll;
            /// <summary> カメラの傾きを打ち消すため、粒の回転より先に回す角度 (度. 向きの基準の z まわり). </summary>
            public float RollAngle;
            /// <summary> 粒ごとに向きを求めるか (カメラの位置を向く・速度の向き). </summary>
            public bool PerParticle;
            /// <summary> 粒ごとに変わらない向き. </summary>
            public OrientationFrame Frame;
            /// <summary> カメラの面の向き (伸ばして描く粒に使う). </summary>
            public OrientationFrame ViewFrame;
            /// <summary> Horizontal Billboard の向き (ワールドの X・Z). </summary>
            public OrientationFrame HorizontalFrame;
            /// <summary> Vertical Billboard の向き (見ている向きから決めた水平の向きと、ワールドの上). </summary>
            public OrientationFrame VerticalFrame;
            /// <summary> 自分のワールドの向き (Mesh の Local に使う). </summary>
            public Quaternion LocalRotation;
            /// <summary> 回す前の四角をそのまま自分の平面に描けるか. </summary>
            public bool Flat;
            /// <summary> 3D に回した四角もそのまま描けるか (z がカメラ側). </summary>
            public bool Flat3D;
            /// <summary> 伸ばして描く粒をそのまま自分の平面に描けるか. </summary>
            public bool FlatView;
            public Matrix4x4 WorldToLocal;
            public Matrix4x4 LocalToWorld;
            public Matrix4x4 SimulationToWorld;
            public Vector3 LossyScale;
            /// <summary> 自分のローカル座標の長さ 1 のワールドでの長さ. </summary>
            public float LocalToWorldScale;
        }

        //----- field -----

        private ViewSettings viewSettings = default;

        private OrientationSettings orientationSettings = default;

        //----- method -----

        /// <summary>
        /// 見ている側の設定を読む.
        /// 画面に重ねるキャンバス (と、カメラが無いキャンバス) は、ルートのキャンバスの正面から正射影で見ているとする.
        /// </summary>
        private void ReadViewSettings()
        {
            var settings = new ViewSettings();

            var currentCanvas = canvas;

            Camera viewCamera = null;
            Transform canvasTransform = null;

            var canvasWidth = 0f;

            if (currentCanvas != null)
            {
                var rootCanvas = currentCanvas.rootCanvas;

                canvasTransform = rootCanvas.transform;

                var rootRectTransform = (RectTransform)canvasTransform;

                canvasWidth = canvasTransform.TransformVector(new Vector3(rootRectTransform.rect.width, 0f, 0f)).magnitude;

                switch (rootCanvas.renderMode)
                {
                    case RenderMode.ScreenSpaceCamera:
                        viewCamera = rootCanvas.worldCamera;
                        break;

                    case RenderMode.WorldSpace:
                        viewCamera = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
                        break;
                }
            }

            if (viewCamera != null)
            {
                var cameraTransform = viewCamera.transform;

                settings.HasCamera = true;
                settings.Orthographic = viewCamera.orthographic;
                settings.Position = cameraTransform.position;
                settings.Right = cameraTransform.right;
                settings.Up = cameraTransform.up;
                settings.Forward = cameraTransform.forward;
                settings.Rotation = cameraTransform.rotation;
                settings.WorldToCamera = cameraTransform.worldToLocalMatrix;

                if (viewCamera.orthographic)
                {
                    settings.ViewportWidth = viewCamera.orthographicSize * 2f * viewCamera.aspect;
                }
                else
                {
                    settings.ViewportWidthPerDepth = Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f * viewCamera.aspect;
                }

                // 傾き (roll) の無い上は、ワールドの上と見ている向きから決める.
                var unrolledRight = Vector3.Cross(Vector3.up, settings.Forward);

                if (Tolerance <= unrolledRight.sqrMagnitude)
                {
                    var unrolledUp = Vector3.Cross(settings.Forward, unrolledRight.normalized);

                    settings.UnrollAngle = Vector3.SignedAngle(settings.Up, unrolledUp, settings.Forward);
                }
            }
            else
            {
                var viewTransform = canvasTransform != null ? canvasTransform : GetTransform();

                settings.Orthographic = true;
                settings.Right = viewTransform.right;
                settings.Up = viewTransform.up;
                settings.Forward = viewTransform.forward;
                settings.Rotation = viewTransform.rotation;
                settings.ViewportWidth = canvasWidth;
            }

            viewSettings = settings;
        }

        /// <summary> 描画のあいだ変わらない向きの設定を読む. </summary>
        private void ReadOrientationSettings(ParticleSystemRenderer particleSystemRenderer, ParticleSystem.MainModule mainModule)
        {
            var transform = GetTransform();

            var settings = new OrientationSettings()
            {
                Alignment = particleSystemRenderer.alignment,
                AllowRoll = particleSystemRenderer.allowRoll,
                WorldToLocal = transform.worldToLocalMatrix,
                LocalToWorld = transform.localToWorldMatrix,
                LossyScale = transform.lossyScale,
                LocalRotation = transform.rotation,
            };

            settings.LocalToWorldScale = settings.LocalToWorld.MultiplyVector(Vector3.right).magnitude;

            Matrix4x4 simulationToLocal;

            settings.SimulationToWorld = TryGetSimulationToLocalMatrix(mainModule, out simulationToLocal) ?
                settings.LocalToWorld * simulationToLocal :
                settings.LocalToWorld;

            var view = viewSettings;

            settings.ViewFrame = ToLocalFrame(ref settings, view.Right, view.Up, -view.Forward);
            // ※ Horizontal / Vertical Billboard の z は、X・Y の面の裏 (pivot の z をずらす向き).
            var verticalRight = GetVerticalBillboardRight(view);

            settings.HorizontalFrame = ToLocalFrame(ref settings, Vector3.right, Vector3.forward, Vector3.up);
            settings.VerticalFrame = ToLocalFrame(ref settings, verticalRight, Vector3.up, Vector3.Cross(Vector3.up, verticalRight));

            switch (settings.Alignment)
            {
                case ParticleSystemRenderSpace.World:
                    settings.Frame = ToLocalFrame(ref settings, Vector3.right, Vector3.up, Vector3.forward);
                    break;

                case ParticleSystemRenderSpace.Local:
                    // 自分のローカル座標の軸 (拡大率は Transform のとおりに掛かる).
                    settings.Frame = new OrientationFrame() { X = Vector3.right, Y = Vector3.up, Z = Vector3.forward };
                    break;

                case ParticleSystemRenderSpace.Facing:
                    // カメラの位置が無いときは、見ている向きを向く.
                    if (view.HasCamera)
                    {
                        settings.PerParticle = true;
                    }
                    else
                    {
                        settings.Frame = ToLocalFrame(ref settings, view.Right, view.Up, view.Forward);
                    }

                    settings.RollAngle = settings.AllowRoll ? 0f : view.UnrollAngle;
                    break;

                case ParticleSystemRenderSpace.Velocity:
                    settings.PerParticle = true;
                    break;

                default:
                    settings.Frame = settings.ViewFrame;
                    settings.RollAngle = settings.AllowRoll ? 0f : view.UnrollAngle;
                    break;
            }

            settings.Flat = !settings.PerParticle && settings.RollAngle == 0f && IsFlatFrame(settings.Frame);
            settings.Flat3D = settings.Flat && IsSameDirection(settings.Frame.Z, Vector3.back);
            settings.FlatView = IsFlatFrame(settings.ViewFrame);

            orientationSettings = settings;
        }

        /// <summary> ワールドの向きを、ワールドの軸ごとの拡大率を掛けて自分のローカル座標に直す. </summary>
        private static OrientationFrame ToLocalFrame(ref OrientationSettings settings, Vector3 x, Vector3 y, Vector3 z)
        {
            return new OrientationFrame()
            {
                X = ToLocalVector(ref settings, x),
                Y = ToLocalVector(ref settings, y),
                Z = ToLocalVector(ref settings, z),
            };
        }

        /// <summary> ワールドの向きと長さ (粒の大きさの単位) を自分のローカル座標に直す. </summary>
        private static Vector3 ToLocalVector(ref OrientationSettings settings, Vector3 vector)
        {
            return settings.WorldToLocal.MultiplyVector(Vector3.Scale(settings.LossyScale, vector));
        }

        /// <summary> ワールドの向きを拡大率を掛けずに自分のローカル座標に直し、拡大率は粒を回したあとの軸ごとに掛ける基準にする. </summary>
        private static OrientationFrame ToScaledLocalFrame(ref OrientationSettings settings, Vector3 x, Vector3 y, Vector3 z)
        {
            var worldToLocal = settings.WorldToLocal;

            return new OrientationFrame()
            {
                X = worldToLocal.MultiplyVector(x),
                Y = worldToLocal.MultiplyVector(y),
                Z = worldToLocal.MultiplyVector(z),
                UseScale = true,
                Scale = settings.LossyScale,
            };
        }

        /// <summary>
        /// Vertical Billboard の横の向き (ワールド).
        /// ParticleSystemRenderer と同じく、見ている向きの z と右の z を並べた水平の向きにする.
        /// </summary>
        private static Vector3 GetVerticalBillboardRight(ViewSettings view)
        {
            var right = new Vector3(view.Forward.z, 0f, view.Right.z);

            if (right.sqrMagnitude < Tolerance) { return Vector3.right; }

            return right.normalized;
        }

        private static bool IsFlatFrame(OrientationFrame frame)
        {
            return IsSameDirection(frame.X, Vector3.right) && IsSameDirection(frame.Y, Vector3.up);
        }

        private static bool IsSameDirection(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude < FlatTolerance * FlatTolerance;
        }

        /// <summary> 粒ごとに決める向き (カメラの位置を向く・速度の向き). </summary>
        private OrientationFrame GetParticleFrame(Vector3 worldPosition, Vector3 simulationVelocity)
        {
            var settings = orientationSettings;
            var view = viewSettings;

            if (settings.Alignment == ParticleSystemRenderSpace.Velocity)
            {
                // 速度の向きに面を向け、シミュレーション空間の Z を面に落とした向きを上にする.
                var normal = simulationVelocity.normalized;

                if (normal.sqrMagnitude < Tolerance)
                {
                    return settings.ViewFrame;
                }

                var up = Vector3.forward - normal * normal.z;

                if (up.sqrMagnitude < Tolerance)
                {
                    up = Vector3.up - normal * normal.y;
                }

                up.Normalize();

                var right = Vector3.Cross(up, normal);

                // ※ ParticleSystemRenderer と同じく、横と法線をワールドへ写し、上はその 2 つの外積にする (縦横で違う拡大率では長さが 1 にならない).
                //   拡大率は、粒を回したあとの四角の軸ごとに掛ける.
                var simulationToWorld = settings.SimulationToWorld;

                var worldRight = simulationToWorld.MultiplyVector(right).normalized;
                var worldNormal = simulationToWorld.MultiplyVector(normal).normalized;

                return ToScaledLocalFrame(ref settings, worldRight, Vector3.Cross(worldNormal, worldRight), worldNormal);
            }

            // カメラの位置から粒への向きに面を向け、カメラの上を上にする.
            var forward = (worldPosition - view.Position).normalized;

            var facingRight = Vector3.Cross(view.Up, forward);

            if (facingRight.sqrMagnitude < Tolerance)
            {
                return settings.ViewFrame;
            }

            facingRight.Normalize();

            var facingUp = Vector3.Cross(forward, facingRight);

            // ※ 拡大率は、粒を回したあとの四角の軸ごとに掛ける.
            return ToScaledLocalFrame(ref settings, facingRight, facingUp, forward);
        }

        /// <summary> 向きの基準に沿って四角を組む (halfX・halfY・offset は基準の座標). </summary>
        private static void SetFramedQuad(UIVertex[] quad, Vector3 center, OrientationFrame frame, Vector3 halfX, Vector3 halfY, Vector3 offset, Color32 color, Vector4 uv)
        {
            var x = frame.X * halfX.x + frame.Y * halfX.y + frame.Z * halfX.z;
            var y = frame.X * halfY.x + frame.Y * halfY.y + frame.Z * halfY.z;
            var o = frame.X * offset.x + frame.Y * offset.y + frame.Z * offset.z;

            var c = center + o;

            quad[0].position = c - x - y;
            quad[1].position = c - x + y;
            quad[2].position = c + x + y;
            quad[3].position = c + x - y;

            quad[0].color = color;
            quad[1].color = color;
            quad[2].color = color;
            quad[3].color = color;

            quad[0].uv0 = new Vector2(uv.x, uv.y);
            quad[1].uv0 = new Vector2(uv.x, uv.w);
            quad[2].uv0 = new Vector2(uv.z, uv.w);
            quad[3].uv0 = new Vector2(uv.z, uv.y);
        }

        /// <summary>
        /// 向きの基準に沿って回した四角 (粒の回転・3D の回転・回転の中心を含む).
        /// rotation: 基準の面の中の回転 (度. 反時計回り). rotation3D は is3dRotation のときだけ使う.
        /// rollAngle: 粒の回転より先に、基準の z まわりに回す角度 (度).
        /// </summary>
        private static void SetOrientedBillboardQuad(UIVertex[] quad, Vector3 center, OrientationFrame frame, float rotation, bool is3dRotation, Vector3 rotation3D, float rollAngle, Vector3 size, float lengthScale, Vector3 pivotOffset, Color32 color, Vector4 uv)
        {
            // ※ ParticleSystemRenderer と同じく、3D の回転は各軸を逆向きに回す.
            var particleRotation = is3dRotation ?
                Quaternion.Euler(-rotation3D.x, -rotation3D.y, -rotation3D.z) :
                Quaternion.Euler(0f, 0f, rotation);

            if (rollAngle != 0f)
            {
                particleRotation *= Quaternion.Euler(0f, 0f, rollAngle);
            }

            var halfX = particleRotation * new Vector3(size.x * 0.5f * lengthScale, 0f, 0f);
            var halfY = particleRotation * new Vector3(0f, size.y * 0.5f, 0f);
            var offset = particleRotation * new Vector3(pivotOffset.x * lengthScale, pivotOffset.y, pivotOffset.z);

            if (frame.UseScale)
            {
                halfX = Vector3.Scale(frame.Scale, halfX);
                halfY = Vector3.Scale(frame.Scale, halfY);
                offset = Vector3.Scale(frame.Scale, offset);
            }

            SetFramedQuad(quad, center, frame, halfX, halfY, offset, color, uv);
        }

        /// <summary>
        /// 向きの基準の X・Y の面に置いた四角 (Horizontal / Vertical Billboard).
        /// ParticleSystemRenderer と同じく、単位の四角の角を粒の回転で回してから、粒の大きさを X・Y に掛ける.
        /// pivot も ParticleSystemRenderer と同じく、粒と一緒には回さず、X・Y は回した最初の角の向きの成分を掛け、z は大きさ.x を単位に面の裏へずらす.
        /// </summary>
        private static void SetPlanarBillboardQuad(UIVertex[] quad, OrientationFrame frame, Vector3 center, float rotationDegree, Vector3 size, Vector3 pivot, Color32 color, Vector4 uv)
        {
            var angle = rotationDegree * Mathf.Deg2Rad;

            var sin = Mathf.Sin(angle);
            var cos = Mathf.Cos(angle);

            var halfWidth = size.x * 0.5f * PlanarBillboardScale;
            var halfHeight = size.y * 0.5f * PlanarBillboardScale;

            if (pivot != Vector3.zero)
            {
                center += frame.X * (pivot.x * size.x * PlanarBillboardScale * (cos - sin)) +
                          frame.Y * (pivot.y * size.y * PlanarBillboardScale * (cos + sin)) +
                          frame.Z * (pivot.z * size.x);
            }

            quad[0].position = center + GetPlanarCorner(frame, -1f, -1f, sin, cos, halfWidth, halfHeight);
            quad[1].position = center + GetPlanarCorner(frame, -1f,  1f, sin, cos, halfWidth, halfHeight);
            quad[2].position = center + GetPlanarCorner(frame,  1f,  1f, sin, cos, halfWidth, halfHeight);
            quad[3].position = center + GetPlanarCorner(frame,  1f, -1f, sin, cos, halfWidth, halfHeight);

            quad[0].color = color;
            quad[1].color = color;
            quad[2].color = color;
            quad[3].color = color;

            quad[0].uv0 = new Vector2(uv.x, uv.y);
            quad[1].uv0 = new Vector2(uv.x, uv.w);
            quad[2].uv0 = new Vector2(uv.z, uv.w);
            quad[3].uv0 = new Vector2(uv.z, uv.y);
        }

        private static Vector3 GetPlanarCorner(OrientationFrame frame, float signX, float signY, float sin, float cos, float halfWidth, float halfHeight)
        {
            var a = signX * cos + signY * sin;
            var b = -signX * sin + signY * cos;

            return frame.X * (a * halfWidth) + frame.Y * (b * halfHeight);
        }

        /// <summary>
        /// 速度の向きに伸ばした四角 (粒の位置が先頭で、速度の逆向きへ伸ばす).
        /// 幅は、画面上での速度の向き (カメラの位置から見た向き) に直交させる.
        /// pivot は、幅の向きに大きさ.x、伸ばす向きに大きさ.y の 2 倍を単位にする.
        /// 面積が無いとき (速度がちょうど見ている向き) は false.
        /// </summary>
        private bool SetOrientedStretchedQuad(UIVertex[] quad, Vector3 center, Vector3 simulationVelocity, Vector3 size, float lengthScale, float velocityScale, Vector3 pivot, Color32 color, Vector4 uv)
        {
            var settings = orientationSettings;
            var view = viewSettings;

            var worldPosition = settings.LocalToWorld.MultiplyPoint3x4(center);
            var worldVelocity = settings.SimulationToWorld.MultiplyVector(simulationVelocity);

            var speed = worldVelocity.magnitude;

            if (speed <= 0f) { return false; }

            var direction = worldVelocity / speed;

            // 画面上での速度の向き.
            Vector2 screenDirection;

            if (view.HasCamera)
            {
                var cameraPosition = view.WorldToCamera.MultiplyPoint3x4(worldPosition);
                var cameraVelocity = view.WorldToCamera.MultiplyVector(worldVelocity);

                screenDirection = new Vector2(cameraVelocity.x, cameraVelocity.y);

                if (Tolerance < Math.Abs(cameraPosition.z))
                {
                    screenDirection.x -= cameraPosition.x * cameraVelocity.z / cameraPosition.z;
                    screenDirection.y -= cameraPosition.y * cameraVelocity.z / cameraPosition.z;
                }
            }
            else
            {
                screenDirection = new Vector2(Vector3.Dot(worldVelocity, view.Right), Vector3.Dot(worldVelocity, view.Up));
            }

            if (screenDirection.sqrMagnitude < Tolerance * Tolerance) { return false; }

            screenDirection.Normalize();

            // 速度の向きの左 (画面上).
            var left = view.Right * -screenDirection.y + view.Up * screenDirection.x;

            var halfWidth = ToLocalVector(ref settings, left * (size.x * 0.5f));

            // 大きさの分は拡大率を掛け、速さの分はワールドの長さのまま自分のローカル座標に直す.
            var tail = ToLocalVector(ref settings, direction * -(size.y * lengthScale)) +
                       settings.WorldToLocal.MultiplyVector(direction * -(speed * velocityScale));

            var head = center;

            if (pivot != Vector3.zero)
            {
                head += ToLocalVector(ref settings, left * -(pivot.x * size.x) + direction * (pivot.y * 2f * size.y));
            }

            quad[0].position = head + halfWidth + tail;
            quad[1].position = head + halfWidth;
            quad[2].position = head - halfWidth;
            quad[3].position = head - halfWidth + tail;

            quad[0].color = color;
            quad[1].color = color;
            quad[2].color = color;
            quad[3].color = color;

            quad[0].uv0 = new Vector2(uv.z, uv.y);
            quad[1].uv0 = new Vector2(uv.x, uv.y);
            quad[2].uv0 = new Vector2(uv.x, uv.w);
            quad[3].uv0 = new Vector2(uv.z, uv.w);

            return true;
        }

        /// <summary>
        /// Freeform Stretching の四角 (粒の位置が中心).
        /// カメラの位置を向いた四角を粒の回転で回し、角を速度の向きに沿って伸ばす.
        /// 伸ばす倍率は、ParticleSystemRenderer と同じく lengthScale × 拡大率の x + ワールドの速さ × velocityScale ÷ 大きさ.y.
        /// pivot は粒の回転で回し (z は大きさ.x を単位にカメラから遠ざかる向き)、角と同じく伸ばす.
        /// rotation: 粒の回転 (ラジアン. 反時計回り).
        /// 速度が 0 のときは false.
        /// </summary>
        private bool SetFreeformStretchedQuad(UIVertex[] quad, Vector3 center, Vector3 simulationVelocity, Vector3 size, float rotation, float lengthScale, float velocityScale, bool rotateWithStretchDirection, Vector3 pivot, Color32 color, Vector4 uv)
        {
            var settings = orientationSettings;
            var view = viewSettings;

            var worldVelocity = settings.SimulationToWorld.MultiplyVector(simulationVelocity);

            var speed = worldVelocity.magnitude;

            if (speed <= 0f || size.y <= 0f) { return false; }

            var direction = worldVelocity / speed;

            // カメラの位置から粒への向きに面を向け、カメラの上を上にする.
            var forward = view.Forward;

            if (view.HasCamera)
            {
                var toParticle = settings.LocalToWorld.MultiplyPoint3x4(center) - view.Position;

                if (Tolerance <= toParticle.sqrMagnitude)
                {
                    forward = toParticle.normalized;
                }
            }

            var right = Vector3.Cross(view.Up, forward);

            right = Tolerance <= right.sqrMagnitude ? right.normalized : view.Right;

            var up = Vector3.Cross(forward, right);

            // ※ 拡大率は、Facing と同じく四角の軸ごとに掛ける.
            var lossyScale = settings.LossyScale;

            var scaledRight = right * lossyScale.x;
            var scaledUp = up * lossyScale.y;

            // 伸ばす向きへ回すときは、面に落とした速度の向きと、拡大率を掛けた上・右から角度を求める.
            // ※ 面に落とした速度が無いときは、面の上を速度の向きとする.
            if (rotateWithStretchDirection)
            {
                var planar = worldVelocity - forward * Vector3.Dot(worldVelocity, forward);

                planar = Tolerance <= planar.sqrMagnitude ? planar.normalized : up;

                var sin = Mathf.Clamp(Vector3.Dot(planar, scaledUp), -1f, 1f);
                var cos = Mathf.Sqrt(1f - sin * sin);

                if (Vector3.Dot(planar, scaledRight) < 0f)
                {
                    cos = -cos;
                }

                rotation += Mathf.Atan2(sin, cos) + Mathf.PI;
            }

            var length = lengthScale * lossyScale.x + velocityScale * speed / size.y;

            var rotationSin = Mathf.Sin(rotation);
            var rotationCos = Mathf.Cos(rotation);

            var halfWidth = size.x * 0.5f;
            var halfHeight = size.y * 0.5f;

            if (pivot != Vector3.zero)
            {
                var pivotX = pivot.x * size.x;
                var pivotY = pivot.y * size.y;

                var offset = scaledRight * (pivotX * rotationCos - pivotY * rotationSin) +
                             scaledUp * (pivotX * rotationSin + pivotY * rotationCos) +
                             forward * (pivot.z * size.x * lossyScale.z);

                offset += direction * (Vector3.Dot(offset, direction) * (length - 1f));

                center += settings.WorldToLocal.MultiplyVector(offset);
            }

            quad[0].position = center + GetFreeformCorner(ref settings, -halfWidth, -halfHeight, rotationSin, rotationCos, scaledRight, scaledUp, direction, length);
            quad[1].position = center + GetFreeformCorner(ref settings, -halfWidth,  halfHeight, rotationSin, rotationCos, scaledRight, scaledUp, direction, length);
            quad[2].position = center + GetFreeformCorner(ref settings,  halfWidth,  halfHeight, rotationSin, rotationCos, scaledRight, scaledUp, direction, length);
            quad[3].position = center + GetFreeformCorner(ref settings,  halfWidth, -halfHeight, rotationSin, rotationCos, scaledRight, scaledUp, direction, length);

            quad[0].color = color;
            quad[1].color = color;
            quad[2].color = color;
            quad[3].color = color;

            quad[0].uv0 = new Vector2(uv.x, uv.y);
            quad[1].uv0 = new Vector2(uv.x, uv.w);
            quad[2].uv0 = new Vector2(uv.z, uv.w);
            quad[3].uv0 = new Vector2(uv.z, uv.y);

            return true;
        }

        /// <summary> Freeform Stretching の角の、粒の位置からのずれ (自分のローカル座標). </summary>
        private static Vector3 GetFreeformCorner(ref OrientationSettings settings, float x, float y, float sin, float cos, Vector3 right, Vector3 up, Vector3 direction, float length)
        {
            var corner = right * (x * cos - y * sin) + up * (x * sin + y * cos);

            corner += direction * (Vector3.Dot(corner, direction) * (length - 1f));

            return settings.WorldToLocal.MultiplyVector(corner);
        }

        /// <summary>
        /// 粒の位置での画面の幅 (自分のローカル座標の長さ).
        /// 透視投影では、粒の奥行きでの画面の幅にする.
        /// </summary>
        private float GetViewportWidth(Vector3 center)
        {
            var settings = orientationSettings;
            var view = viewSettings;

            float width;

            if (view.HasCamera && !view.Orthographic)
            {
                var depth = Vector3.Dot(settings.LocalToWorld.MultiplyPoint3x4(center) - view.Position, view.Forward);

                width = depth * view.ViewportWidthPerDepth;
            }
            else
            {
                width = view.ViewportWidth;
            }

            return 0f < settings.LocalToWorldScale ? width / settings.LocalToWorldScale : 0f;
        }
    }
}
