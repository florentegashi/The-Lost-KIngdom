using UnityEngine;

namespace LostKingdom
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ForestCheckpoint : MonoBehaviour
    {
        public string checkpointName = "Forest trail";
        void Reset() => GetComponent<BoxCollider>().isTrigger = true;
        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerMovement>();
            if (player && GameManager.Instance) GameManager.Instance.RegisterCheckpoint(player.transform.position, checkpointName);
        }
    }
}
