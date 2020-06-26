using BulletSharp.Math;
using Microsoft.DirectX;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Mathematica;

namespace TGC.Group.Model
{
    class AnimacionGol
    {
        public Boolean Activo { get; set; }
        public float time = 0;
        private const float DURACION_ANIMACION = 5;
        private Pelota pelota;
        public AnimacionGol(Pelota pelota)
        {
            Activo = false;
            this.pelota = pelota;
        }

        public void Update(float ElapsedTime)
        {
            time += ElapsedTime;
            if (time >= DURACION_ANIMACION)
            {
                time = 0;
                Activo = false;
                pelota.Mesh.Technique = "BlinnPhong";
            }
        }

        public void AnimarGol(List<Jugador> objetos, Color colorEquipo)
        {
            if (!Activo)
            {
                Activo = true;
                pelota.Mesh.Technique = "Explosion";
                pelota.Mesh.Effect.SetValue("colorEquipo", TGCVector3.TGCVector3ToFloat3Array(new TGCVector3(colorEquipo.R, colorEquipo.G, colorEquipo.B)));
                pelota.Time = 0;
                foreach (var objeto in objetos)
                {
                    var direccion = objeto.Translation - pelota.Translation;
                    var fuerza = TGCVector3.Normalize(direccion) * (1000000f / direccion.Length());
                    objeto.Cuerpo.ApplyCentralForce(fuerza.ToBulletVector3());
                }
            }

        }

    }
}
