using System;
using UnityEngine;

namespace Game.Audio
{
    // Hangi zeminde hangi ayak sesi. Zemin, üstünde durulan nesnenin malzemesinden anlaşılır.
    // Yeni zemin malzemesi için listeye satır eklenir; sahneye dokunulmaz.
    [CreateAssetMenu(fileName = "Footsteps_", menuName = "Game/Footstep Bank")]
    public class FootstepBank : ScriptableObject
    {
        [Serializable]
        public class Surface
        {
            [SerializeField] private Material[] materials;
            [SerializeField] private SoundSet steps;
            [SerializeField] private SoundSet land;

            public Material[] Materials => materials;
            public SoundSet Steps => steps;
            public SoundSet Land => land;
        }

        [SerializeField] private Surface[] surfaces;
        [Tooltip("Listede olmayan malzeme ya da görünmez zemin için.")]
        [SerializeField] private Surface fallback;

        public Surface Find(Collider ground)
        {
            Renderer renderer = ground.GetComponent<Renderer>();
            Material material = renderer != null ? renderer.sharedMaterial : null;
            foreach (Surface surface in surfaces)
            {
                if (Array.IndexOf(surface.Materials, material) >= 0)
                {
                    return surface;
                }
            }

            return fallback;
        }
    }
}
