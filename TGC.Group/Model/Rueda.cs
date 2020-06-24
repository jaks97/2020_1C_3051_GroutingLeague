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
    class Rueda
    {
        TGCVector3 translation;
        TgcMesh mesh;
        float rotacion = 0;

        public Rueda(TgcMesh mesh, TGCVector3 translation)
        {
            this.mesh = mesh;
            this.translation = translation;
        }

        public void Update(float velocidad)
        {
            rotacion += velocidad; // Mejorable
        }

        public void Render(TGCMatrix matrizAuto)
        {
            mesh.Transform = TGCMatrix.RotationX(rotacion) * TGCMatrix.Translation(translation) * matrizAuto;
            mesh.Render();
        }
    }
}
