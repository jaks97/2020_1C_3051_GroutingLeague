using Microsoft.DirectX.Direct3D;
using System.Collections.Generic;
using System.Linq;
using TGC.Core.Direct3D;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;

namespace TGC.Group.Model
{
    class Pasto : ObjetoJuego
    {
        private float altura;
        public List<TgcMesh> capas;
        private float time;

        public Pasto(TgcMesh mesh, Effect effect, int capasCount, float altura) : base(mesh, default, default, default)
        {
            this.altura = altura;
            capas = new List<TgcMesh>();
            for (int i = 0; i < capasCount; i++)
            {
                TgcMesh pasto = mesh.createMeshInstance("Pasto" + i);
                pasto.Position += TGCVector3.Up * (i * altura / capasCount);
                pasto.Transform = TGCMatrix.Scaling(pasto.Scale) * TGCMatrix.RotationYawPitchRoll(pasto.Rotation.Y, pasto.Rotation.X, pasto.Rotation.Z) * TGCMatrix.Translation(pasto.Position);
                pasto.Effect = effect.Clone(D3DDevice.Instance.Device);
                pasto.Technique = "Pasto";
                pasto.Effect.SetValue("nivel", (float)i / capasCount);
                capas.Add(pasto);
            }
        }

        public override void Update(float elapsedTime)
        {
            time += elapsedTime;
        }

        public void Render(bool lod = false)
        {
            if (lod)
                capas.First().Render();
            else
            {
                foreach (var capa in capas)
                {
                    capa.Effect.SetValue("time", time);
                    capa.Render();
                }
            }
        }
    }
}
