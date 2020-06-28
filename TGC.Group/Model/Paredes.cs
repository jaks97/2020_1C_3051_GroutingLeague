using BulletSharp;
using TGC.Core.BulletPhysics;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;

namespace TGC.Group.Model
{
    class Paredes: IObjetoJuego
    {
        
        public TgcMesh    Mesh { get; }
        public RigidBody  Cuerpo { get; }

        public Paredes( TgcMesh mesh)
        {
            Mesh = mesh;
            Cuerpo = BulletRigidBodyFactory.Instance.CreateRigidBodyFromTgcMesh(mesh);
        }

        public void Update(float ElapsedTime)
        {
        }

        public void Render()
        { }

        public void Render(Luz luz)
        {
            Mesh.Effect.SetValue("lightColor", TGCVector3.TGCVector3ToFloat3Array(new TGCVector3(luz.Color.R, luz.Color.G, luz.Color.B)));
            Mesh.Effect.SetValue("lightPosition", TGCVector3.TGCVector3ToFloat3Array(luz.Translation));
            Mesh.Effect.SetValue("Ka", 0.8f);
            Mesh.Effect.SetValue("Kd", 0.2f);
            Mesh.Effect.SetValue("Ks", 0);
            Mesh.Effect.SetValue("shininess", 0);
            Mesh.Effect.SetValue("reflection", 0);
            Mesh.Render();
        }

        public void Dispose()
        {
        }
    }
}
