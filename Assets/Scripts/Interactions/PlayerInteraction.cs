using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LostKingdom
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Tooltip("Press this key once to interact with the nearest available object.")]
        public Key interactKey = Key.Enter;
        readonly List<InteractionTarget> nearby = new List<InteractionTarget>();
        public void Register(InteractionTarget target) { if (!nearby.Contains(target)) nearby.Add(target); }
        public void Unregister(InteractionTarget target) { nearby.Remove(target); }
        void Update()
        {
            if (!GameManager.Instance.IsPlaying) return;
            InteractionTarget closest = null;
            float best = float.MaxValue;
            for (int i = nearby.Count - 1; i >= 0; i--)
            {
                var item = nearby[i];
                if (!item) { nearby.RemoveAt(i); continue; }
                if (!item || !item.Available) continue;
                float d = (item.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; closest = item; }
            }
            GameManager.Instance.ui.SetPrompt(closest ? closest.Prompt : "");
            if (closest && DemoInput.Interact(interactKey)) closest.Interact();
        }
    }
}
