using UnityEngine;

namespace LostKingdom
{
    public class CrystalCollectible : InteractionTarget
    {
        public MagicalGate gate;
        public Transform visual;
        bool collected;
        ParticleSystem aura;
        public override bool Available => !collected && gate.IsOpen;
        public override string Prompt => "Press E to collect the Crystal of Light.";
        void Start()
        {
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
            collected = true; visual.gameObject.SetActive(false);
            if (aura) aura.Stop();
            CollectionEffect.Play(transform.position + Vector3.up * 2, Color.cyan);
            GameManager.Instance.CollectCrystal();
        }
    }
}
