using System;
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
using TGC.Core.SceneLoader;
using TGC.Core.Shaders;
using TGC.Core.Terrain;
using TGC.Core.Text;

namespace TGC.Group.Model
{
    class EscenaControles : Escena
    {

        private CustomSprite unSprite;
        private Pasto pasto;
        private TgcMesh paredes;
        private TgcSkyBox skyBox;

        public EscenaControles(TgcCamera Camera, string MediaDir, string ShadersDir, TgcText2D DrawText, float TimeBetweenUpdates, TgcD3dInput Input) : base(Camera, MediaDir, ShadersDir, DrawText, TimeBetweenUpdates, Input)
        {
            TgcScene escena = new TgcSceneLoader().loadSceneFromFile(MediaDir + "Cancha-TgcScene.xml");

            pasto = new Pasto(escena.Meshes[0], TGCShaders.Instance.LoadEffect(ShadersDir + "CustomShaders.fx"), 32, .5f);
            paredes = escena.getMeshByName("Box_5");
            Camera.SetCamera(new TGCVector3(20, 10, -20), new TGCVector3(0, 5, -7));

            skyBox = new TgcSkyBox();
            skyBox.Center = new TGCVector3(0, 500, 0);
            skyBox.Size = new TGCVector3(10000, 10000, 10000);
            var texturesPath = MediaDir + "Textures\\SkyBox LostAtSeaDay\\";
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Up, texturesPath + "lostatseaday_up.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Down, texturesPath + "lostatseaday_dn.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Left, texturesPath + "lostatseaday_lf.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Right, texturesPath + "lostatseaday_rt.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Front, texturesPath + "lostatseaday_bk.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Back, texturesPath + "lostatseaday_ft.jpg");
            skyBox.Init();

            unSprite = new CustomSprite();
            unSprite.Bitmap = new CustomBitmap(MediaDir + "Textures\\Controles2.png", D3DDevice.Instance.Device);

            //unSprite.Scaling = new TGCVector2(unSprite.Bitmap.Width, unSprite.Bitmap.Height);
            unSprite.Position = new TGCVector2((float)D3DDevice.Instance.Width / 2 - unSprite.Bitmap.Width / 2, (float)D3DDevice.Instance.Height / 2 - unSprite.Bitmap.Height / 2);
        }

        public override void Dispose()
        {
       
        }

        public override void Render()
        {
            D3DDevice.Instance.Device.Clear(Microsoft.DirectX.Direct3D.ClearFlags.Target | Microsoft.DirectX.Direct3D.ClearFlags.ZBuffer, Color.White, 1.0f, 0);
            D3DDevice.Instance.Device.BeginScene();
            pasto.Render();
            paredes.Render();
            skyBox.Render();
            drawer2D.BeginDrawSprite();
            drawer2D.DrawSprite(unSprite);
            drawer2D.EndDrawSprite();
        }

        public override Escena Update(float ElapsedTime)
        {
            pasto.Update(ElapsedTime);            
            if (Input.keyDown(Key.Escape))
            {
                return CambiarEscena(new EscenaMenu(Camera, MediaDir, ShadersDir, DrawText, TimeBetweenUpdates, Input));
            }

            return this;
        }
    }
}
