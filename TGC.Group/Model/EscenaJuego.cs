using BulletSharp;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TGC.Core.Camara;
using TGC.Core.Direct3D;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.Particle;
using TGC.Core.SceneLoader;
using TGC.Core.Shaders;
using TGC.Core.Terrain;
using TGC.Core.Text;
using TGC.Group.Model._2D;
using TGC.Core.Sound;

namespace TGC.Group.Model
{
    class EscenaJuego : Escena
    {
        //Objetos de escena
        private TgcScene escena;
        private Pasto pasto;
        private TgcSkyBox skyBox;
        private Luz sol;
        private bool dia;

        //Objetos de juego
        private List<Jugador> jugadores = new List<Jugador>();
        private Jugador jugadorActivo;
        private Jugador jugadorDos;
        private CamaraJugador camara;
        private CamaraJugador camaraDos;
        private Pelota pelota;
        private List<Turbo> turbos;
        private Paredes paredes;
        private Arco[] arcos;

        private int golequipo1 = 0;
        private int golequipo2 = 0;
        private double tiempoRestante = 300;
        private AnimacionGol animacionGol;
        //private ParticleEmitter emitter;

        //Objetos de fisica
        protected DiscreteDynamicsWorld dynamicsWorld;
        protected CollisionDispatcher dispatcher;
        protected DefaultCollisionConfiguration collisionConfiguration;
        protected SequentialImpulseConstraintSolver constraintSolver;
        protected BroadphaseInterface overlappingPairCache;
        private RigidBody floorBody;

        // 2D
        private UIEscenaJuego ui;

        private TgcScreenQuad screenQuad;
        private Microsoft.DirectX.Direct3D.Effect effect;
        private CubeTexture g_pCubeMap; // Cubemap para Env Shader
        private int frameNumber; // Numero de frame
        private TgcMp3Player mp3Gol;
        private bool PantallaDividida => jugadorDos != null;
        private Texture renderTargetBloom;
        private Surface depthStencil;

        public EscenaJuego(TgcCamera Camera, string MediaDir, string ShadersDir, TgcText2D DrawText, float TimeBetweenUpdates, TgcD3dInput Input, List<Jugador> jugadores, Jugador jugadorActivo, Jugador segundoJugador = null, bool dia = true) : base(Camera, MediaDir, ShadersDir, DrawText, TimeBetweenUpdates, Input)
        {
            this.dia = dia;
            screenQuad = new TgcScreenQuad();

            initFisica();

            initMeshes();

            this.jugadores = jugadores;
            this.jugadorActivo = jugadorActivo;
            this.jugadorDos = segundoJugador;
            initJugadores();

            sol = new Luz(Color.White, new TGCVector3(0, 70, -130));
            
            mp3Gol = new TgcMp3Player();
            mp3Gol.FileName = MediaDir + "Music\\Gol.mp3";

            pelota = new Pelota(escena.getMeshByName("Pelota"), new TGCVector3(0f, 50f, 0));
            pelota.Mesh.Effect.SetValue("texPerlin", TextureLoader.FromFile(D3DDevice.Instance.Device, MediaDir + "Textures\\PerlinNoise.png"));
            dynamicsWorld.AddRigidBody(pelota.Cuerpo);

            paredes = new Paredes(escena.getMeshByName("Box_5"));
            dynamicsWorld.AddRigidBody(paredes.Cuerpo);

            arcos = new Arco[2];

            arcos[0] = new Arco(escena.getMeshByName("Arco"), FastMath.PI);
            arcos[1] = new Arco(escena.getMeshByName("Arco"), 0);

            dynamicsWorld.AddRigidBody(arcos[0].Cuerpo);
            dynamicsWorld.AddRigidBody(arcos[1].Cuerpo);

            camara = new CamaraJugador(jugadorActivo, pelota, Camera, paredes.Mesh.createBoundingBox());
            if (PantallaDividida)
                camaraDos = new CamaraJugador(segundoJugador, pelota, Camera, paredes.Mesh.createBoundingBox());

            ui = new UIEscenaJuego();
            ui.Init(MediaDir,drawer2D);

            animacionGol = new AnimacionGol(pelota);


            //Cargar shader con efectos de Post-Procesado
            effect = TGCShaders.Instance.LoadEffect(ShadersDir + "CustomShaders.fx");

            //Configurar Technique dentro del shader
            effect.Technique = "PostProcess";
            effect.SetValue("screenWidth", D3DDevice.Instance.Device.PresentationParameters.BackBufferWidth);
            effect.SetValue("screenHeight", D3DDevice.Instance.Device.PresentationParameters.BackBufferHeight);

            g_pCubeMap = new CubeTexture(D3DDevice.Instance.Device, 64, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            //Creamos un Render Targer sobre el cual se va a dibujar la pantalla
            renderTargetBloom = new Texture(D3DDevice.Instance.Device, D3DDevice.Instance.Device.PresentationParameters.BackBufferWidth / 2,
                D3DDevice.Instance.Device.PresentationParameters.BackBufferHeight / 2, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            depthStencil = D3DDevice.Instance.Device.CreateDepthStencilSurface(D3DDevice.Instance.Device.PresentationParameters.BackBufferWidth,
                 D3DDevice.Instance.Device.PresentationParameters.BackBufferHeight, DepthFormat.D24S8, MultiSampleType.None, 0, true);
        }

        private void initFisica()
        {
            //Creamos el mundo fisico por defecto.
            collisionConfiguration = new DefaultCollisionConfiguration();

            dispatcher = new CollisionDispatcher(collisionConfiguration);
            GImpactCollisionAlgorithm.RegisterAlgorithm(dispatcher);

            constraintSolver = new SequentialImpulseConstraintSolver();

            overlappingPairCache = new DbvtBroadphase();

            dynamicsWorld = new DiscreteDynamicsWorld(dispatcher, overlappingPairCache, constraintSolver, collisionConfiguration);
            dynamicsWorld.Gravity = new TGCVector3(0, -10f, 0).ToBulletVector3();

            //Creamos el cuerpo del piso
            StaticPlaneShape floorShape = new StaticPlaneShape(TGCVector3.Up.ToBulletVector3(), 0);
            DefaultMotionState floorMotionState = new DefaultMotionState();
            RigidBodyConstructionInfo floorInfo = new RigidBodyConstructionInfo(0, floorMotionState, floorShape);

            floorBody = new RigidBody(floorInfo);
            floorBody.Restitution = 1.25f;
            dynamicsWorld.AddRigidBody(floorBody);
        }

        private void initMeshes()
        {
            //Crear SkyBox
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

            Microsoft.DirectX.Direct3D.Effect customShaders = TGCShaders.Instance.LoadEffect(ShadersDir + "CustomShaders.fx");

            //cargar escena
            escena = new TgcSceneLoader().loadSceneFromFile(MediaDir + "Cancha-TgcScene.xml");

            pasto = new Pasto(escena.Meshes[0], customShaders.Clone(D3DDevice.Instance.Device), 20, .5f);

            TgcMesh meshTurbo = escena.getMeshByName("Turbo");

            turbos = new List<Turbo>()
            {
                new Turbo(meshTurbo, new TGCVector3(80, -.2f, 100)),
                new Turbo(meshTurbo, new TGCVector3(-80, -.2f, -100)),
                new Turbo(meshTurbo, new TGCVector3(80, -.2f, -100)),
                new Turbo(meshTurbo, new TGCVector3(-80, -.2f, 100)),
                new Turbo(meshTurbo, new TGCVector3(0, -.2f, 130)),
                new Turbo(meshTurbo, new TGCVector3(0, -.2f, -130)),
                new Turbo(meshTurbo, new TGCVector3(0, -.2f, 250)),
                new Turbo(meshTurbo, new TGCVector3(0, -.2f, -250)),
                new Turbo(meshTurbo, new TGCVector3(220, -.2f, 0), 100),
                new Turbo(meshTurbo, new TGCVector3(-220, -.2f, 0), 100),
                new Turbo(meshTurbo, new TGCVector3(220, -.2f, 300), 100),
                new Turbo(meshTurbo, new TGCVector3(-220, -.2f, -300), 100),
                new Turbo(meshTurbo, new TGCVector3(-220, -.2f, 300), 100),
                new Turbo(meshTurbo, new TGCVector3(220, -.2f, -300), 100)
            };

            foreach (TgcMesh mesh in escena.Meshes)
            {
                mesh.Effect = customShaders.Clone(D3DDevice.Instance.Device);
                mesh.Technique = "BlinnPhong";
            }
        }

        private void initJugadores()
        {
            jugadores[0].Reubicar(new TGCVector3(-20, 0, 100), new TGCVector3(0, 0, 0));
            jugadores[1].Reubicar(new TGCVector3(0, 0, -30), new TGCVector3(FastMath.PI, 0, 0));
            jugadores[2].Reubicar(new TGCVector3(0, 0, 30), new TGCVector3(0, 0, 0));
            jugadores[3].Reubicar(new TGCVector3(20, 0, -100), new TGCVector3(FastMath.PI, 0, 0));
            foreach(var jugador in jugadores)
            {
                dynamicsWorld.AddRigidBody(jugador.Cuerpo);
            }
        }

        private void Reubicar()
        {
            pelota.ReiniciarPelota();
            mp3Gol.closeFile();
            jugadores.ForEach(jugador => jugador.ReiniciarJugador());
            turbos.ForEach(turbo => turbo.Reiniciar());
        }


        public override Escena Update(float ElapsedTime)
        {
            frameNumber++;
            tiempoRestante -= ElapsedTime;

            if (tiempoRestante <= 0)
            {
                return CambiarEscena(new EscenaGameOver(Camera, MediaDir, ShadersDir, DrawText, TimeBetweenUpdates, Input));
            }

            dynamicsWorld.StepSimulation(ElapsedTime, 10, TimeBetweenUpdates);

            foreach (var turbo in turbos)
            {
                turbo.Update(ElapsedTime);
            }

            foreach (Jugador jugador in jugadores)
            {
                jugador.Update(ElapsedTime);

                // Esto quedo medio feo, capaz estaria bueno trasladar esta logica a Turbo? O a Jugador?:
                Turbo turboEnContacto = turbos.Find(turbo => turbo.CheckCollideWith(jugador)); // Nunca vamos a tocar mas de 1 turbo en simultaneo
                if (turboEnContacto != null)
                    jugador.RecogerTurbo(turboEnContacto);
            }

            pasto.Update(ElapsedTime);

            camara.Update();
            pelota.Update(ElapsedTime);

            jugadorActivo.HandleInput(Input);
            if (PantallaDividida)
                jugadorDos.HandleInput(Input);
            if (Input.keyDown(Key.Escape))
            {
                return CambiarEscena(new EscenaMenu(Camera, MediaDir, ShadersDir, DrawText, TimeBetweenUpdates, Input));
            }

            ui.TextoTurbo = jugadorActivo.Turbo.ToString();
            ui.ColorTextoTurbo = Color.FromArgb(255, 255 - (int)(jugadorActivo.Turbo * 2.55), 255 - (int)(Math.Min(jugadorActivo.Turbo, 50) * 4.55));
            if (PantallaDividida)
            {
                ui.TextoTurboDos = jugadorDos.Turbo.ToString();
                ui.ColorTextoTurboDos = Color.FromArgb(255, 255 - (int)(jugadorDos.Turbo * 2.55), 255 - (int)(Math.Min(jugadorDos.Turbo, 50) * 4.55));
            }
            ui.TextoGolAzul = golequipo1.ToString();
            ui.TextoGolRojo = golequipo2.ToString();
            ui.TextoReloj = String.Format("{0:0}:{1:00}", Math.Floor(tiempoRestante / 60), tiempoRestante % 60);

            if (animacionGol.Activo)
            {
                pelota.Cuerpo.ActivationState = ActivationState.IslandSleeping;
                animacionGol.Update(ElapsedTime);
                if (!animacionGol.Activo)
                    Reubicar();
            }
            else
            {
                if (arcos[0].CheckCollideWith(pelota))
                {
                    golequipo1++;
                    mp3Gol.play(true);
                    animacionGol.AnimarGol(jugadores, Color.Blue);
                }

                if (arcos[1].CheckCollideWith(pelota))
                {
                    golequipo2++;
                    mp3Gol.play(true);
                    animacionGol.AnimarGol(jugadores, Color.Red);
                }
            }

            return this;
        }
        
        private void renderScene(bool cubemap = false)
        {

            skyBox.Render();

            if(!cubemap)
                pelota.Mesh.Effect.SetValue("normal_map", TextureLoader.FromFile(D3DDevice.Instance.Device, MediaDir + "Textures\\pelotaNormalMap.png")); // TODO: Estamos cargando siempre esta textura del disco. Esto habria que evitarlo

            pelota.Render(sol);

            pasto.Render(cubemap);

            if(!cubemap)
            foreach (var jugador in jugadores)
            {
                if (jugador.Translation != Camera.Position)
                {
                    jugador.Mesh.Effect.SetValue("eyePosition", TGCVector3.TGCVector3ToFloat3Array(Camera.Position));
                    jugador.Render(sol);
                }
            }

            arcos[0].Render(sol);
            arcos[1].Render(sol);

            foreach (var turbo in turbos)
            {
                turbo.Render(sol);
            }

            paredes.Render(sol);
        }

        private void renderCubemap(TGCVector3 worldPos)
        {
            // ojo: es fundamental que el fov sea de 90 grados.
            // asi que re-genero la matriz de proyeccion
            D3DDevice.Instance.Device.Transform.Projection = TGCMatrix.PerspectiveFovLH(Geometry.DegreeToRadian(90.0f), 1f, 1f, 10000f).ToMatrix();

            // En vez de renderizar todas las caras en todos los frames (Como en el codigo comentado de arriba), renderizo una cara por cada frame
            var nFace = (CubeMapFace)(frameNumber % 6);
            var pFace = g_pCubeMap.GetCubeMapSurface(nFace, 0);
            D3DDevice.Instance.Device.SetRenderTarget(0, pFace);
            TGCVector3 Dir, VUP;
            switch (nFace)
            {
                default:
                case CubeMapFace.PositiveX:
                    Dir = new TGCVector3(1, 0, 0);
                    VUP = TGCVector3.Up;
                    break;
                case CubeMapFace.NegativeX:
                    Dir = new TGCVector3(-1, 0, 0);
                    VUP = TGCVector3.Up;
                    break;
                case CubeMapFace.PositiveY:
                    Dir = TGCVector3.Up;
                    VUP = new TGCVector3(0, 0, -1);
                    break;
                case CubeMapFace.NegativeY:
                    Dir = TGCVector3.Down;
                    VUP = new TGCVector3(0, 0, 1);
                    break;
                case CubeMapFace.PositiveZ:
                    Dir = new TGCVector3(0, 0, 1);
                    VUP = TGCVector3.Up;
                    break;
                case CubeMapFace.NegativeZ:
                    Dir = new TGCVector3(0, 0, -1);
                    VUP = TGCVector3.Up;
                    break;
            }

            //como queremos usar la camara rotacional pero siguendo a un objetivo comentamos el seteo del view.
            D3DDevice.Instance.Device.Transform.View = TGCMatrix.LookAtLH(worldPos, Dir, VUP);

            D3DDevice.Instance.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            D3DDevice.Instance.Device.BeginScene();

            //Renderizar
            renderScene(true);

            D3DDevice.Instance.Device.EndScene();
        }

        // Metodo que renderiza objetos luminosos para el bloom
        private void renderLuminoso()
        {
            foreach (var turbo in turbos)
                if (turbo.Activo)
                    turbo.Render(sol);

            if (animacionGol.Activo)
                pelota.Render(sol);

            foreach (var jugador in jugadores)
            {
                if (jugador.Translation != Camera.Position)
                {
                    jugador.Mesh.Technique = "Negro";
                    jugador.ruedas[0].Mesh.Technique = "Negro";
                    jugador.Render(sol);
                    jugador.Mesh.Technique = "BlinnPhong";
                    jugador.ruedas[0].Mesh.Technique = "BlinnPhong";
                }
            }

            arcos[0].Mesh.Technique = "Negro";
            arcos[0].Render(sol);
            arcos[1].Render(sol);
            arcos[0].Mesh.Technique = "BlinnPhong";

            pasto.capas.First().Technique = "Negro";
            pasto.Render(true);
        }

        // Renderiza todo incluido el postprocess en el Texture pasado
        public Texture RenderCompleto(Texture renderTarget2D)
        {
            var d3dDevice = D3DDevice.Instance.Device;

            //Cargamos el Render Targer al cual se va a dibujar la escena 3D. Antes nos guardamos el surface original
            //En vez de dibujar a la pantalla, dibujamos a un buffer auxiliar, nuestro Render Target.
            var pSurf = renderTarget2D.GetSurfaceLevel(0);
            d3dDevice.SetRenderTarget(0, pSurf);
            d3dDevice.DepthStencilSurface = depthStencil;
            var rectangle = renderTarget2D.Device.ScissorRectangle;

            // Restauro el estado de las transformaciones
            d3dDevice.Transform.View = Camera.GetViewMatrix().ToMatrix();
            d3dDevice.Transform.Projection = TGCMatrix.PerspectiveFovLH(Geometry.DegreeToRadian(PantallaDividida ? 65 : 45), (float)rectangle.Width / rectangle.Height, 1f, 10000f).ToMatrix();

            // dibujo pp dicho
            d3dDevice.BeginScene();
            d3dDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            renderScene();
            d3dDevice.EndScene();

            //Liberar memoria de surface de Render Target
            pSurf.Dispose();

            //Si quisieramos ver que se dibujo, podemos guardar el resultado a una textura en un archivo para debugear su resultado (ojo, es lento)
            //TextureLoader.Save(this.ShadersDir + "render_target.bmp", ImageFileFormat.Bmp, renderTarget2D);

            // Renderizamos lo que va a tener bloom

            //Cargamos el Render Targer al cual se va a dibujar la escena 3D. Antes nos guardamos el surface original
            //En vez de dibujar a la pantalla, dibujamos a un buffer auxiliar, nuestro Render Target.
            pSurf = renderTargetBloom.GetSurfaceLevel(0);
            d3dDevice.SetRenderTarget(0, pSurf);
            d3dDevice.DepthStencilSurface = depthStencil;

            // dibujo pp dicho
            d3dDevice.BeginScene();
            d3dDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            renderLuminoso();
            d3dDevice.EndScene();

            //Liberar memoria de surface de Render Target
            pSurf.Dispose();

            return renderTarget2D;
        }

        public void PostProcess(Texture render)
        {
            var d3dDevice = D3DDevice.Instance.Device;
            //Arrancamos la escena
            d3dDevice.BeginScene();
            d3dDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

            //Cargamos parametros en el shader de Post-Procesado
            effect.SetValue("texDiffuseMap", render);
            effect.SetValue("texBloom", renderTargetBloom);
            effect.SetValue("activo", Input.keyDown(Key.B)); // Para debugear nomas

            d3dDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            screenQuad.render(effect);
        }

        public override void Render()
        {
            var d3dDevice = D3DDevice.Instance.Device;

            var pOldRT = d3dDevice.GetRenderTarget(0);
            renderCubemap(jugadorActivo.Translation); // Solo hago un cubemap del jugador activo y lo uso en los demas por performance
            foreach (var jugador in jugadores)
            {
                //var g_pCubeMap = cubemap(jugador.Translation);
                jugador.Mesh.Effect.SetValue("g_txCubeMap", g_pCubeMap);
                //g_pCubeMap.Dispose();
            }
            //g_pCubeMap.Dispose();
            var oDS = d3dDevice.DepthStencilSurface;
            if (PantallaDividida)
            {
                var renderTargetUno = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth / 2,
                    d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
                RenderCompleto(renderTargetUno);
                var renderTargetPP1 = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth / 2,
                    d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
                var pSurf = renderTargetPP1.GetSurfaceLevel(0);
                d3dDevice.SetRenderTarget(0, pSurf);
                d3dDevice.DepthStencilSurface = depthStencil;
                PostProcess(renderTargetUno);
                d3dDevice.EndScene();
                pSurf.Dispose();
                

                var renderTargetDos = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth / 2,
                    d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
                camaraDos.Update();
                RenderCompleto(renderTargetDos);
                var renderTargetPP2 = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth / 2,
                   d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
                pSurf = renderTargetPP2.GetSurfaceLevel(0);
                d3dDevice.SetRenderTarget(0, pSurf);
                d3dDevice.DepthStencilSurface = depthStencil;
                PostProcess(renderTargetDos);
                d3dDevice.EndScene();
                pSurf.Dispose();

                d3dDevice.SetRenderTarget(0, pOldRT);
                d3dDevice.DepthStencilSurface = oDS;
                //Arrancamos la escena
                d3dDevice.BeginScene();
                d3dDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

                var effect = TGCShaders.Instance.LoadEffect(ShadersDir + "CustomShaders.fx");
                effect.Technique = "SplitScreen";
                //Cargamos parametros en el shader de Post-Procesado
                effect.SetValue("texPrimerJugador", renderTargetPP1);
                effect.SetValue("texSegundoJugador", renderTargetPP2);

                screenQuad.render(effect);

            }
            else
            {
                var renderTargetUno = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth,
                    d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
                RenderCompleto(renderTargetUno);
                //Ahora volvemos a restaurar el Render Target original (osea dibujar a la pantalla)
                d3dDevice.SetRenderTarget(0, pOldRT);
                d3dDevice.DepthStencilSurface = oDS;
                PostProcess(renderTargetUno);
            }
            ui.Render(PantallaDividida);
            pOldRT.Dispose();

        }
        public override void Dispose()
        {
            dynamicsWorld.Dispose();
            foreach (Jugador jugador in jugadores)
            {
                jugador.Dispose();
            }

            pelota.Dispose();

            escena.DisposeAll();

            foreach (Turbo turbo in turbos)
            {
                turbo.Dispose();
            }

            mp3Gol.closeFile();
        }
    }
}
