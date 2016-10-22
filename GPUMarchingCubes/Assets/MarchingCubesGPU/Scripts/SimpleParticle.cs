using UnityEngine;
using System.Collections;

namespace irishoak
{
    public struct SimpleParticle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Color   color;
        public float   life;

        public SimpleParticle(Vector3 pos, Vector3 vel)
        {
            this.position = pos;
            this.velocity = vel;
            this.color    = Color.white;
            this.life     = 1.0f;
        }

        public SimpleParticle(Vector3 pos, Vector3 vel, Color col, float life)
        {
            this.position = pos;
            this.velocity = vel;
            this.color    = col;
            this.life     = life;
        }
    }
}