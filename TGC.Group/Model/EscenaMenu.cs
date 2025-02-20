﻿using System.Drawing;
using TGC.Core.Camara;
using TGC.Core.Direct3D;
using TGC.Core.Input;
using TGC.Core.Text;
using Microsoft.DirectX.DirectInput;
using TGC.Core.SceneLoader;
using TGC.Core.Mathematica;
using System.Collections.Generic;
using TGC.Core.Terrain;
using System;
using TGC.Group.Model._2D;
using System.Linq;
using TGC.Core.Shaders;
using TGC.Core.Sound;
using Microsoft.DirectX.Direct3D;

namespace TGC.Group.Model
{
    class Boton
    {
        private HUDSprite menuItem;
        private HUDSprite menuItemSelec;
        public HUDTexto texto;
        

        public Boton(CustomBitmap sprite, CustomBitmap spriteSelec, string texto, int indice, Drawer2D drawer)
        {
            CustomSprite menuSprite = new CustomSprite();
            menuSprite.Bitmap = sprite;
            CustomSprite menuSpriteSelec = new CustomSprite();
            menuSpriteSelec.Bitmap = spriteSelec;

            menuItem = new HUDSprite(HUD.AnclajeHorizontal.LEFT, HUD.AnclajeVertical.TOP, new TGCVector2(0.05f, 0.5f + (float)indice / 17), new TGCVector2(1, 1), drawer, menuSprite);
            menuItemSelec = new HUDSprite(HUD.AnclajeHorizontal.LEFT, HUD.AnclajeVertical.TOP, new TGCVector2(0.05f, 0.5f + (float)indice / 17), new TGCVector2(1, 1), drawer, menuSpriteSelec);
            menuItem.Init();
            menuItemSelec.Init();

            TgcText2D texto2D = new TgcText2D();
            texto2D.Align = TgcText2D.TextAlign.CENTER;
            texto2D.Size = new Size((int)(menuItem.Sprite.Scaling.X * 350), 20);
            texto2D.changeFont(new System.Drawing.Font("Calibri", D3DDevice.Instance.Width / 96f, FontStyle.Italic | FontStyle.Bold));
            texto2D.Text = texto;

            this.texto = new HUDTexto(HUD.AnclajeHorizontal.LEFT, HUD.AnclajeVertical.TOP, new TGCVector2(0.1f, 0.5175f + (float)indice / 17), drawer, texto2D);
            this.texto.Init();
        }

        public void Render(bool seleccionado)
        {
            if (seleccionado)
            {
                menuItemSelec.Render();
                texto.Texto2D.Color = Color.FromArgb(0, 101, 225);
            }
            else
            {
                menuItem.Render();
                texto.Texto2D.Color = Color.White;
            }
            texto.Render();
        }
        public void Dispose()
        {
            menuItem.Dispose();
            menuItemSelec.Dispose();
            texto.Dispose();
        }

        public bool checkCollision(TGCVector2 posicion)
        {
            return posicion.X > menuItem.Sprite.Position.X && posicion.X < menuItem.Sprite.Position.X + menuItem.Sprite.Scaling.X * 512
                && posicion.Y > menuItem.Sprite.Position.Y && posicion.Y < menuItem.Sprite.Position.Y + menuItem.Sprite.Scaling.Y * 50;
        }
    }
    class EscenaMenu : Escena
    {
        enum Items
        {
            INICIAR,
            CONTROLES,
            CAMBIARVEHICULO,
            DOSJUGADORES,
            SALIR
        }

        private Pasto pasto;
        private TgcMesh paredes;
        private TgcSkyBox skyBox;
        private List<Boton> botones = new List<Boton>();
        private int botonSeleccionado;

        private bool dia = true;

        public int BotonSeleccionado
        {
            get => botonSeleccionado;
            set => botonSeleccionado = Math.Max(0, Math.Min(botones.Count - 1, value));
        }

        private List<Jugador> jugadores = new List<Jugador>();
        private int jugadorActivo = 0;

        private int JugadorActivo
        {
            get => jugadorActivo;
            set
            {
                if (value >= jugadores.Count) jugadorActivo = 0;
                else if (value < 0) jugadorActivo = jugadores.Count - 1;
                else jugadorActivo = value;
            }
        }

        private Luz sol;
        private TgcMp3Player mp3Player;       

        public EscenaMenu(TgcCamera Camera, string MediaDir, string ShadersDir, TgcText2D DrawText, float TimeBetweenUpdates, TgcD3dInput Input) : base(Camera, MediaDir, ShadersDir, DrawText, TimeBetweenUpdates, Input)
        {
            this.MediaDir = MediaDir;
            TgcScene escena = new TgcSceneLoader().loadSceneFromFile(MediaDir + "Cancha-TgcScene.xml");

            pasto = new Pasto(escena.Meshes[0], TGCShaders.Instance.LoadEffect(ShadersDir + "CustomShaders.fx"), 32, .5f);
            paredes = escena.getMeshByName("Box_5");
            Camera.SetCamera(new TGCVector3(20, 10, -20), new TGCVector3(0, 5, -7));
            initJugadores(escena);
            mp3Player = new TgcMp3Player();
            mp3Player.FileName= MediaDir + "Music\\Inicio.mp3";
            mp3Player.play(true);

            sol = new Luz(Color.White, new TGCVector3(0, 30, -50));

            CustomBitmap menuItem = new CustomBitmap(MediaDir + "\\Textures\\HUD\\menuItem.png", D3DDevice.Instance.Device);
            CustomBitmap menuItemSelec = new CustomBitmap(MediaDir + "\\Textures\\HUD\\menuItemSelec.png", D3DDevice.Instance.Device);

            botones.Add(new Boton(menuItem, menuItemSelec, "Entrenamiento", 0, drawer2D));
            botones.Add(new Boton(menuItem, menuItemSelec, "Controles", 1, drawer2D));
            botones.Add(new Boton(menuItem, menuItemSelec, "< Cambiar vehículo >", 2, drawer2D));
            botones.Add(new Boton(menuItem, menuItemSelec, "Competencia", 3, drawer2D));
            botones.Add(new Boton(menuItem, menuItemSelec, "Salir", 4, drawer2D));

            initSkyBox();

            // Restauro el estado de las transformaciones
            D3DDevice.Instance.Device.Transform.View = Camera.GetViewMatrix().ToMatrix();
            D3DDevice.Instance.Device.Transform.Projection = TGCMatrix.PerspectiveFovLH(Geometry.DegreeToRadian(45), D3DDevice.Instance.AspectRatio, 1f, 10000f).ToMatrix();
        }

        private void initSkyBox()
        {
            skyBox = new TgcSkyBox();
            skyBox.Center = new TGCVector3(0, 500, 0);
            skyBox.Size = new TGCVector3(10000, 10000, 10000);
            var texturesPath = MediaDir + "Textures\\SkyBox LostAtSea" + (dia ? "Day\\" : "Night\\");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Up, texturesPath + "lostatseaday_up.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Down, texturesPath + "lostatseaday_dn.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Left, texturesPath + "lostatseaday_lf.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Right, texturesPath + "lostatseaday_rt.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Front, texturesPath + "lostatseaday_bk.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Back, texturesPath + "lostatseaday_ft.jpg");
            skyBox.Init();
        }

        private void initJugadores(TgcScene escena)
        {
            List<Rueda> ruedas = new List<Rueda>();
            ruedas.Add(new Rueda(escena.getMeshByName("AutoRueda"), new TGCVector3(4.5f, -1f, 5)));
            ruedas.Add(new Rueda(escena.getMeshByName("AutoRueda"), new TGCVector3(4.5f, -1f, -4.8f)));
            ruedas.Add(new Rueda(escena.getMeshByName("AutoRueda"), new TGCVector3(-4.5f, -1f, 5)));
            ruedas.Add(new Rueda(escena.getMeshByName("AutoRueda"), new TGCVector3(-4.5f, -1f, -4.8f)));
            Jugador auto = new Jugador("Auto", escena.Meshes[2], ruedas, new TGCVector3(0, 5, 0), new TGCVector3(-.5f, 0, 0));

            ruedas = new List<Rueda>();
            ruedas.Add(new Rueda(escena.getMeshByName("TractorRueda"), new TGCVector3(3.5f, -.5f, 4)));
            ruedas.Add(new Rueda(escena.getMeshByName("TractorRueda"), new TGCVector3(-3.5f, -.5f, 4)));
            ruedas.Add(new Rueda(escena.getMeshByName("TractorRueda"), new TGCVector3(2.5f, -1f, -5), new TGCVector3(.6f,.6f,.6f)));
            ruedas.Add(new Rueda(escena.getMeshByName("TractorRueda"), new TGCVector3(-2.5f, -1f, -5), new TGCVector3(.6f, .6f, .6f)));
            Jugador tractor = new Jugador("Tractor", escena.Meshes[5], ruedas, new TGCVector3(0, 5, 0), new TGCVector3(-.5f, 0, 0));

            ruedas = new List<Rueda>();
            ruedas.Add(new Rueda(escena.getMeshByName("PatrulleroRueda"), new TGCVector3(3.5f, -1.7f, 7.2f)));
            ruedas.Add(new Rueda(escena.getMeshByName("PatrulleroRueda"), new TGCVector3(3.5f, -1.7f, -7f)));
            ruedas.Add(new Rueda(escena.getMeshByName("PatrulleroRueda"), new TGCVector3(-3.5f, -1.7f, 7.2f)));
            ruedas.Add(new Rueda(escena.getMeshByName("PatrulleroRueda"), new TGCVector3(-3.5f, -1.7f, -7f)));
            Jugador patrullero = new Jugador("Patrullero", escena.Meshes[3], ruedas, new TGCVector3(0, 5, 0), new TGCVector3(-.5f, 0, 0));

            ruedas = new List<Rueda>();
            ruedas.Add(new Rueda(escena.getMeshByName("TanqueRueda"), new TGCVector3(4.3f, -2.5f, 5.7f)));
            ruedas.Add(new Rueda(escena.getMeshByName("TanqueRueda"), new TGCVector3(4.3f, -2.5f, -5.3f)));
            ruedas.Add(new Rueda(escena.getMeshByName("TanqueRueda"), new TGCVector3(-4.3f, -2.5f, 5.7f)));
            ruedas.Add(new Rueda(escena.getMeshByName("TanqueRueda"), new TGCVector3(-4.3f, -2.5f, -5.3f)));
            ruedas.Add(new Rueda(escena.getMeshByName("TanqueRueda"), new TGCVector3(-4.3f, -2.5f, 0.2f)));
            ruedas.Add(new Rueda(escena.getMeshByName("TanqueRueda"), new TGCVector3(4.3f, -2.5f, 0.2f)));
            Jugador tanque = new Jugador("Tanque", escena.Meshes[4], ruedas, new TGCVector3(0, 5, 0), new TGCVector3(-.5f, 0, 0));

            jugadores.Add(auto);
            jugadores.Add(tractor);
            jugadores.Add(patrullero);
            jugadores.Add(tanque);

            foreach(var jugador in jugadores)
            {
                jugador.Mesh.Effect = TGCShaders.Instance.LoadEffect(ShadersDir + "CustomShaders.fx");
                jugador.Mesh.Technique = "BlinnPhong";
                foreach(var rueda in jugador.ruedas)
                {
                    rueda.Mesh.Effect = TGCShaders.Instance.LoadEffect(ShadersDir + "CustomShaders.fx");
                    rueda.Mesh.Technique = "BlinnPhong";
                }
            }
        }
        public override void Dispose()
        {
            botones.ForEach(boton => boton.Dispose());
        }

        public override void Render()
        {
            D3DDevice.Instance.Device.Clear(Microsoft.DirectX.Direct3D.ClearFlags.Target | Microsoft.DirectX.Direct3D.ClearFlags.ZBuffer, Color.White, 1.0f, 0);
            D3DDevice.Instance.Device.BeginScene();
            //TexturesManager.Instance.clearAll();

            skyBox.Render();
            pasto.Render();
            paredes.Render();

            for (int i = 0; i < botones.Count; i++)
                botones[i].Render(i == botonSeleccionado);


            jugadores[jugadorActivo].Mesh.Effect.SetValue("eyePosition", TGCVector3.TGCVector3ToFloat3Array(Camera.Position));
            jugadores[jugadorActivo].Render(sol);
        }

        private float tiempoMovido = 0; // Workaround por el evento de las teclas
        public override Escena Update(float ElapsedTime)
        {
            Boton mouse = botones.FirstOrDefault(boton => boton.checkCollision(new TGCVector2(Input.Xpos, Input.Ypos)));
            if (Input.XposRelative != 0 && Input.YposRelative != 0)
                botonSeleccionado = botones.IndexOf(mouse);

            if (Input.keyDown(Key.Return) || (mouse != null && Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT)))
                switch ((Items)botonSeleccionado)
                {
                    case Items.INICIAR:
                        mp3Player.closeFile();
                        jugadores[jugadorActivo].controles = new Controles(Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow, Key.Space, Key.LeftControl);
                        return CambiarEscena(new EscenaJuego(Camera, MediaDir, ShadersDir, DrawText, TimeBetweenUpdates, Input, jugadores, jugadores[jugadorActivo], null, dia));
                    case Items.CONTROLES:
                        mp3Player.closeFile();
                        return CambiarEscena(new EscenaControles(Camera, MediaDir, ShadersDir, DrawText, TimeBetweenUpdates, Input));
                    case Items.SALIR:
                        mp3Player.closeFile();
                        Form.GameForm.ActiveForm.Close();
                        break;
                    case Items.DOSJUGADORES:
                        {
                            var jugadorUno = jugadores[JugadorActivo];
                            jugadorUno.controles = new Controles(Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow, Key.Space, Key.RightControl);
                            ++JugadorActivo;
                            var jugadorDos = jugadores[JugadorActivo];
                            jugadorDos.controles = new Controles(Key.W, Key.S, Key.A, Key.D, Key.LeftShift, Key.LeftAlt);
                            mp3Player.closeFile();
                            return CambiarEscena(new EscenaJuego(Camera, MediaDir, ShadersDir, DrawText, TimeBetweenUpdates, Input, jugadores, jugadorUno, jugadorDos, dia));
                        }
                }

            if(tiempoMovido <= 0)
            {
                if ((Items)botonSeleccionado == Items.CAMBIARVEHICULO)
                {
                    if (Input.keyDown(Key.RightArrow) || Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT))
                    {
                        JugadorActivo++;
                        tiempoMovido = 0.2f;
                    }
                    if (Input.keyDown(Key.LeftArrow))
                    {
                        JugadorActivo--;
                        tiempoMovido = 0.2f;
                    }
                }
                if (Input.keyDown(Key.UpArrow))
                {
                    BotonSeleccionado--;
                    tiempoMovido = 0.2f;
                }
                if (Input.keyDown(Key.DownArrow))
                {
                    BotonSeleccionado++;
                    tiempoMovido = 0.2f;
                }
            }
            else
                tiempoMovido -= ElapsedTime;

            pasto.Update(ElapsedTime);

            return this;
        }
    }
}
