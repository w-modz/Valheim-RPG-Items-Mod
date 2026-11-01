using UnityEngine;

namespace Runecarved
{
    internal static class RarityItemParticles
    {
        private const string ParticleObjectName = "RunecarvedRarityParticles";

        public static void Apply(ItemDrop itemDrop, Color? color)
        {
            if (color.HasValue)
            {
                Ensure(itemDrop.transform, color.Value, Vector3.up * 0.15f, 0.1f);
            }
            else
            {
                Remove(itemDrop.transform);
            }
        }

        public static void Ensure(Transform parent, Color color, Vector3 localPosition, float radius)
        {
            Transform existing = parent.Find(ParticleObjectName);
            ParticleSystem system = existing != null ? existing.GetComponent<ParticleSystem>() : null;
            if (system == null)
            {
                system = CreateSystem(parent);
                if (system == null)
                {
                    return;
                }
            }

            system.transform.localPosition = localPosition;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.radius = radius;
            ParticleSystem.MainModule main = system.main;
            main.startColor = color;
        }

        public static void Remove(Transform parent)
        {
            Transform existing = parent.Find(ParticleObjectName);
            if (existing != null)
            {
                Object.Destroy(existing.gameObject);
            }
        }

        private static ParticleSystem CreateSystem(Transform parent)
        {
            Material material = RuneLootBeam.GetSharedMaterial();
            if (material == null)
            {
                return null;
            }

            GameObject particleObject = new GameObject(ParticleObjectName);
            particleObject.transform.SetParent(parent, false);

            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 1f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 10f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.75f, 0.2f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            particles.Play();
            return particles;
        }
    }
}
