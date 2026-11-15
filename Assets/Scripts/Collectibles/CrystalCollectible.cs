using System.Collections;
using UnityEngine;

namespace LostKingdom
{
    public class CrystalCollectible : InteractionTarget
    {
        public MagicalGate gate;
        public Transform visual;
        bool collected;
        ParticleSystem aura;
        Vector3 crystalScale, crystalPosition;
        public override bool Available => !collected && gate.IsOpen;
        public override string Prompt => "Press E to collect the Crystal of Light.";
        void Start()
        {
            crystalScale = visual.localScale;
            crystalPosition = visual.localPosition;
            var auraObject = new GameObject("Crystal mote aura"); auraObject.transform.SetParent(visual, false);
            aura = auraObject.AddComponent<ParticleSystem>();
            var main = aura.main; main.loop = true; main.startLifetime = 1.7f; main.startSpeed = .22f; main.startSize = .08f;
            main.startColor = new Color(.45f, .8f, 1f, .68f); main.maxParticles = 40;
            var emission = aura.emission; emission.rateOverTime = 10;
            var shape = aura.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .72f;
            var renderer = aura.GetComponent<ParticleSystemRenderer>();
            var spark = Resources.Load<Material>("LostKingdomSpark"); if (spark) renderer.sharedMaterial = spark;
            aura.Play();
        }
        void Update()
        {
            if (!GameManager.Instance.IsPlaying) return;
            visual.Rotate(0, 30 * Time.deltaTime, 0);
            visual.localPosition = Vector3.up * (1.8f + Mathf.Sin(Time.time * 1.5f) * .2f);
        }
        public override void Interact()
        {
            if (!Available) return;
            collected = true;
            if (aura) aura.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            CollectionEffect.Play(transform.position + Vector3.up * 2, Color.cyan);
            GameManager.Instance.CollectCrystal();
            StartCoroutine(CollectRise());
        }
        IEnumerator CollectRise()
        {
            const float duration = .8f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                visual.localPosition = crystalPosition + Vector3.up * (1.5f * t);
                visual.localScale = crystalScale * (1 - t);
                yield return null;
            }
            visual.gameObject.SetActive(false);
        }
    }
}
