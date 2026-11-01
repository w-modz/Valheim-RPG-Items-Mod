using UnityEngine;

namespace Runecarved
{
    internal sealed class RuneLootBeam : MonoBehaviour
    {
        private const float BeamHeight = 4f;
        private static bool missingShaderWarningLogged;
        private static Material beamMaterial;

        private Color color;
        private LineRenderer halo;
        private LineRenderer core;
        private ParticleSystem particles;

        public void Initialize(Color beamColor)
        {
            color = beamColor;
            EnsureBeam();
        }

        private void LateUpdate()
        {
            if (halo != null && core != null)
            {
                UpdateBeam();
            }
        }

        private void EnsureBeam()
        {
            if (halo != null || core != null)
            {
                return;
            }

            if (GetSharedMaterial() == null)
            {
                return;
            }

            halo = CreateLine("RuneLootBeamHalo", beamMaterial, color, 0.26f, 0.1f, 0.25f, 0.08f);
            core = CreateLine("RuneLootBeamCore", beamMaterial, Color.Lerp(color, Color.white, 0.15f), 0.08f, 0.028f, 1f, 0.12f);
            particles = CreateParticles();
            UpdateBeam();
        }

        private ParticleSystem CreateParticles()
        {
            GameObject particleObject = new GameObject("RuneLootBeamParticles");
            particleObject.transform.SetParent(transform, false);
            particleObject.transform.localPosition = Vector3.up * 0.35f;

            ParticleSystem system = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.duration = 1f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = color;
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 30f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = Vector3.up * BeamHeight * 0.5f;
            shape.scale = new Vector3(0.5f, BeamHeight, 0.5f);

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.75f, 0.2f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = beamMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingFudge = 1f;

            system.Play();
            return system;
        }

        private LineRenderer CreateLine(
            string objectName,
            Material material,
            Color beamColor,
            float startWidth,
            float endWidth,
            float startAlpha,
            float endAlpha)
        {
            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = startWidth;
            line.endWidth = endWidth;
            line.sharedMaterial = material;
            line.startColor = WithAlpha(beamColor, startAlpha);
            line.endColor = WithAlpha(beamColor, endAlpha);
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.alignment = LineAlignment.View;
            return line;
        }

        public static Material GetSharedMaterial()
        {
            if (beamMaterial == null)
            {
                beamMaterial = CreateBeamMaterial();
            }

            return beamMaterial;
        }

        private static Material CreateBeamMaterial()
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                if (!missingShaderWarningLogged)
                {
                    Jotunn.Logger.LogWarning("No supported Unity shader was found; rune loot beams will not render.");
                    missingShaderWarningLogged = true;
                }

                return null;
            }

            return new Material(shader)
            {
                color = Color.white,
                renderQueue = 3000
            };
        }

        private void UpdateBeam()
        {
            Vector3 bottom = transform.position + Vector3.up * 0.35f;
            Vector3 top = transform.position + Vector3.up * BeamHeight;
            halo.SetPosition(0, bottom);
            halo.SetPosition(1, top);
            core.SetPosition(0, bottom);
            core.SetPosition(1, top);

            if (particles != null)
            {
                particles.transform.SetPositionAndRotation(bottom, Quaternion.identity);
            }
        }

        private static Color WithAlpha(Color value, float alpha)
        {
            value.a = alpha;
            return value;
        }
    }
}
