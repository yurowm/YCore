using System.Linq;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Effects {
    public class ParticleSystemShot : MonoBehaviour {
        
        public ParticleSystem[] particleSystems;
        
        public void ShotParticles() {
            particleSystems
                .Where(ps => ps)
                .ForEach(ps => ps.Play(true));
        }
    }
}