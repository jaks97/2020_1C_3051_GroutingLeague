using BulletSharp.Math;
using Microsoft.DirectX.DirectInput;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using TGC.Core.BulletPhysics;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Sound;

namespace TGC.Group.Model
{
    class Jugador : ObjetoJuego
    {
        //Objetos de juego
        private String nombre;
        public Controles controles;
        private TGCVector3 posicionInicial;
        private TGCVector3 rotacionInicial;
        private const float VELOCIDAD_LINEAL_MAX = 100;
        private const float VELOCIDAD_ANGULAR_MAX = 5;
        public List<Rueda> ruedas;
        public bool Acelerando = false;

        private Boolean EnElAire => translation.Y - (AABB.calculateSize().Y / 2f) > .1f;

        public Vector3 Frente => Vector3.Transform(new Vector3(0, 0, -1), rotation); // Vector que apunta siempre al frente del auto
        public Vector3 Normal => Vector3.Transform(new Vector3(0, 1, 0), rotation); // Vector que apunta siempre al techo del auto

        private int turbo = 100;
        public int Turbo
        {
            get => turbo;
            private set => turbo = Math.Min(value, 100);
        }


        public Jugador(String nombre, TgcMesh mesh, List<Rueda> ruedas, TGCVector3 translation=new TGCVector3(), TGCVector3 rotation=new TGCVector3(), float angle=0) :base(mesh,translation,rotation,angle)
        {
            this.nombre = nombre;

            this.translation.Y += 10;

            cuerpo = BulletRigidBodyFactory.Instance.CreateBox(Mesh.BoundingBox.calculateSize()  * 0.5f, 2f, new TGCVector3(translation), rotation.X, rotation.Y, rotation.Z, 1f, true);
            cuerpo.SetSleepingThresholds(0, 0);

            this.ruedas = ruedas;

            posicionInicial = new TGCVector3(translation);
            rotacionInicial = new TGCVector3(rotation);

            Ka = .65f;
            Kd = .3f;
            Ks = .5f;
            shininess = 200;
            reflection = .2f;
        }

        public void ReiniciarJugador()
        {
            cuerpo.WorldTransform = TGCMatrix.RotationYawPitchRoll(rotacionInicial.X, rotacionInicial.Y, rotacionInicial.Z).ToBulletMatrix() * TGCMatrix.Translation(posicionInicial).ToBulletMatrix();
            cuerpo.LinearVelocity = Vector3.Zero;
            cuerpo.AngularVelocity = Vector3.Zero;
            turbo = 100;
        }

        public void Reubicar(TGCVector3 translation, TGCVector3 rotation)
        {
            posicionInicial = translation;
            rotacionInicial = rotation;
            ReiniciarJugador();
        }

        private void HandleInputSuelo(TgcD3dInput input)
        {
            cuerpo.AngularVelocity = new Vector3(cuerpo.AngularVelocity.X, 0, cuerpo.AngularVelocity.Z);
            Vector3 velocidadLineal = cuerpo.LinearVelocity;
            Vector3 nuevaVel = velocidadLineal;
            nuevaVel.Y = 0;
            nuevaVel = (nuevaVel.Dot(Frente) / Frente.LengthSquared) * Frente;
            nuevaVel.Y = velocidadLineal.Y;
            cuerpo.LinearVelocity = nuevaVel;
            if (input.keyDown(controles.teclaAvanzar))
            {
                Acelerando = true;
                cuerpo.ApplyCentralForce(Frente * 50);
            }
            if (input.keyDown(controles.teclaRetroceder))
            {
                cuerpo.ApplyCentralForce(Frente * -50);
            }
            if (velocidadLineal.Length > 1)
            {
                Vector3 rotacion = Vector3.Transform(new Vector3(1, 0, 0), rotation) * Math.Min(5000f / (velocidadLineal.Length * .2f), 1000f) * Math.Sign(velocidadLineal.Dot(Frente));
                if (input.keyDown(controles.teclaDerecha))
                {
                    cuerpo.ApplyForce(rotacion, Frente * -5);
                    cuerpo.ApplyForce(-rotacion, Frente * 5);
                }
                if (input.keyDown(controles.teclaIzquierda))
                {
                    cuerpo.ApplyForce(rotacion, Frente * 5);
                    cuerpo.ApplyForce(-rotacion, Frente * -5);
                }
            }
            if (input.keyDown(controles.teclaSalto))
            {
                cuerpo.ApplyCentralForce(new Vector3(0, 1000, 0));
            }

        }

        private void HandleInputAire(TgcD3dInput input)
        {
            
            Vector3 fuerzaPitch = Vector3.Transform(new Vector3(0, 1, 0), rotation) * 50;
            Vector3 fuerzaYaw = Vector3.Transform(new Vector3(1, 0, 0), rotation) * 50;
            if (input.keyDown(controles.teclaAvanzar))
            {
                cuerpo.ApplyForce(fuerzaPitch, Vector3.Transform(new Vector3(0, 0, 5), rotation));
                cuerpo.ApplyForce(-fuerzaPitch, Vector3.Transform(new Vector3(0, 0, -5), rotation));
            }
            if (input.keyDown(controles.teclaRetroceder))
            {
                cuerpo.ApplyForce(fuerzaPitch, Vector3.Transform(new Vector3(0, 0, -5), rotation));
                cuerpo.ApplyForce(-fuerzaPitch, Vector3.Transform(new Vector3(0, 0, 5), rotation));
            }
            if (input.keyDown(controles.teclaDerecha))
            {
                cuerpo.ApplyForce(fuerzaYaw, Vector3.Transform(new Vector3(0, 0, 5), rotation));
                cuerpo.ApplyForce(-fuerzaYaw, Vector3.Transform(new Vector3(0, 0, -5), rotation));
            }
            if (input.keyDown(controles.teclaIzquierda))
            {
                cuerpo.ApplyForce(fuerzaYaw, Vector3.Transform(new Vector3(0, 0, -5), rotation));
                cuerpo.ApplyForce(-fuerzaYaw, Vector3.Transform(new Vector3(0, 0, 5), rotation));
            }
        }

        private void HandleInputTurbo(TgcD3dInput input)
        {
            if (input.keyDown(controles.teclaTurbo) && Turbo > 0)
            {
                cuerpo.ApplyCentralForce(Vector3.Transform(new Vector3(0, 0, -50), rotation));
                Turbo--;
            }
        }

        public void HandleInput(TgcD3dInput input)
        {
            Acelerando = false;
            if (EnElAire || Math.Abs(Frente.Y) > .2f || Normal.Dot(Vector3.UnitY) < 0.9f)
                HandleInputAire(input);
            else
                HandleInputSuelo(input);
            HandleInputTurbo(input);

            if (cuerpo.AngularVelocity.Length > VELOCIDAD_ANGULAR_MAX)
                cuerpo.AngularVelocity = VELOCIDAD_ANGULAR_MAX * cuerpo.AngularVelocity / cuerpo.AngularVelocity.Length;
            if (cuerpo.LinearVelocity.Length > VELOCIDAD_LINEAL_MAX)
                cuerpo.LinearVelocity = VELOCIDAD_LINEAL_MAX * cuerpo.LinearVelocity / cuerpo.LinearVelocity.Length;
        }

        public void RecogerTurbo(Turbo turbo)
        {
            if (Turbo < 100)
                Turbo += turbo.Usar();
        }

        public override void Update(float elapsedTime)
        {
            base.Update(elapsedTime);
            foreach (var rueda in ruedas)
                rueda.Update(cuerpo.LinearVelocity.Length * 0.01f * -Math.Sign(cuerpo.LinearVelocity.Dot(Frente)));
        }

        public override void Render(Luz luz)
        {
            base.Render(luz);
            foreach (var rueda in ruedas)
                rueda.Render(Mesh.Transform, luz);
        }
    }
}
