﻿using System;
using TGC.Core.Camara;
using TGC.Core.Input;
using TGC.Core.Text;

namespace TGC.Group.Model
{
    abstract class Escena
    {
        protected TgcCamera Camera;
        protected String MediaDir;
        protected String ShadersDir;
        protected TgcText2D DrawText;
        protected Drawer2D drawer2D;

        protected float TimeBetweenUpdates;
        protected TgcD3dInput Input;


        public Escena(TgcCamera Camera, String MediaDir, String ShadersDir, TgcText2D DrawText, float TimeBetweenUpdates, TgcD3dInput Input)
        {
            this.Camera = Camera;
            this.MediaDir = MediaDir;
            this.ShadersDir = ShadersDir;
            this.DrawText = DrawText;
            this.TimeBetweenUpdates = TimeBetweenUpdates;
            this.Input = Input;
            drawer2D = new Drawer2D();
        }

        public abstract Escena Update(float ElapsedTime);
        public abstract void Render();
        public abstract void Dispose();

        protected Escena CambiarEscena(Escena escena)
        {
            this.Dispose();
            return escena;
        }
    }
}
