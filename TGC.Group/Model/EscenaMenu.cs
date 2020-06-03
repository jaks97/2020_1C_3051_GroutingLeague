﻿using System.Drawing;
using TGC.Core.Camara;
using TGC.Core.Direct3D;
using TGC.Core.Input;
using TGC.Core.Text;
using Microsoft.DirectX.DirectInput;

namespace TGC.Group.Model
{
    class EscenaMenu : Escena
    {
        private TgcText2D texto;
        public EscenaMenu(TgcCamera Camera, string MediaDir, TgcText2D DrawText, float TimeBetweenUpdates, TgcD3dInput Input) : base(Camera, MediaDir, DrawText, TimeBetweenUpdates, Input)
        {
            texto = new TgcText2D();
            texto.Align = TgcText2D.TextAlign.CENTER;
            texto.Position = new Point(0, D3DDevice.Instance.Height / 2);
            texto.Color = Color.White;
            texto.changeFont(new Font("TimesNewRoman", 50, FontStyle.Bold));
            texto.Text = "Espacio para empezar";
        }
        public override void Dispose()
        {
            texto.Dispose();
        }

        public override void Render()
        {
            texto.render();
        }

        public override Escena Update(float ElapsedTime)
        {
            if (Input.keyDown(Key.Space))
            {
                return CambiarEscena(new EscenaJuego(Camera, MediaDir, DrawText, TimeBetweenUpdates, Input));
            }
            return this;
        }
    }
}
