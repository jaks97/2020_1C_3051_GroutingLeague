using BulletSharp.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;

namespace TGC.Group.Model
{
    class Rueda : ObjetoJuego
    {
        float rotacion = 0;

        public Rueda(TgcMesh mesh, TGCVector3 translation, TGCVector3 scale) : base(mesh, translation)
        {
            this.scale = scale.ToBulletVector3();
        }
        public Rueda(TgcMesh mesh, TGCVector3 translation) : base(mesh, translation)
        {
            this.scale = Vector3.One;
        }

        public override void Update(float velocidad)
        {
            rotacion += velocidad; // Mejorable
        }

        public void Render(TGCMatrix matrizAuto, Luz luz)
        {
            Mesh.Effect.SetValue("lightColor", TGCVector3.TGCVector3ToFloat3Array(new TGCVector3(luz.Color.R, luz.Color.G, luz.Color.B)));
            Mesh.Effect.SetValue("lightPosition", TGCVector3.TGCVector3ToFloat3Array(luz.Translation));
            Mesh.Effect.SetValue("Ka", .65f);
            Mesh.Effect.SetValue("Kd", .3f);
            Mesh.Effect.SetValue("Ks", 0);
            Mesh.Effect.SetValue("shininess", 0);
            Mesh.Effect.SetValue("reflection", 0);
            mesh.Transform = TGCMatrix.Scaling(new TGCVector3(scale)) * TGCMatrix.RotationX(rotacion) * TGCMatrix.Translation(new TGCVector3(translation)) * matrizAuto;
            mesh.Render();
        }
    }
}
