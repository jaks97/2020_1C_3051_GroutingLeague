﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DirectX.DirectInput;
using TGC.Core.Camara;
using TGC.Core.Direct3D;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.Text;

namespace TGC.Group.Model
{
    class EscenaControles : Escena
    {

        CustomSprite unSprite;

        public EscenaControles(TgcCamera Camera, string MediaDir, string ShadersDir, TgcText2D DrawText, float TimeBetweenUpdates, TgcD3dInput Input) : base(Camera, MediaDir, ShadersDir, DrawText, TimeBetweenUpdates, Input)
        {
            unSprite = new CustomSprite();
            unSprite.Bitmap = new CustomBitmap(MediaDir + "Textures\\Controles2.png", D3DDevice.Instance.Device);

            unSprite.Scaling = new TGCVector2((float)D3DDevice.Instance.Width / unSprite.Bitmap.Width, (float)D3DDevice.Instance.Height / unSprite.Bitmap.Height);
            unSprite.Position = new TGCVector2(0, 0);
        }

        public override void Dispose()
        {
       
        }

        public override void Render()
        {
            D3DDevice.Instance.Device.Clear(Microsoft.DirectX.Direct3D.ClearFlags.Target | Microsoft.DirectX.Direct3D.ClearFlags.ZBuffer, Color.White, 1.0f, 0);
            D3DDevice.Instance.Device.BeginScene();
            drawer2D.BeginDrawSprite();
            drawer2D.DrawSprite(unSprite);
            drawer2D.EndDrawSprite();
        }

        public override Escena Update(float ElapsedTime)
        {
            
            if (Input.keyDown(Key.Escape))
            {
                return CambiarEscena(new EscenaMenu(Camera, MediaDir, ShadersDir, DrawText, TimeBetweenUpdates, Input));
            }

            return this;
        }
    }
}
