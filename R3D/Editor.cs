﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MTV3D65;
using System.IO;
using R3D.Lib;

namespace R3D
{
    public partial class Editor : Form
    {

        //Declarations
        public R3DWorld World;
        public bool DoLoop;

        public TVViewport viewPort;
        public TVEngine Engine;
        public TVLandscape Land;
        public TVTextureFactory TexFact;
        public TVScene Scene;
        public TVInputEngine Inputs;
        public TVGlobals Globals;
        public TVCamera Camera;
        public TVAtmosphere Atmo;
        public TVScreen2DText ScreenText;
        public TVLightEngine Lights;
        public TVMaterialFactory MatFact;
        public TVMathLibrary Maths;
        public TVScreen2DImmediate Screen2D;
        public TVScreen2DText Text2D;

        private float sngPositionX;
        private float sngPositionY;
        private float sngPositionZ;
        private float snglookatX;
        private float snglookatY;
        private float snglookatZ;
        private float sngAngleX;
        private float sngAngleY;
        private float sngWalk;
        private float sngStrafe;

        //Engine Settings
        public bool vSyncON = true;
        public bool initKeys = true;
        public bool initMouse = true;

        //local EntityManager, accounts for all our meshes
        public List<MeshDefinition> EntityManager = new List<MeshDefinition>();
        public List<TVMesh> RawEntityManager = new List<TVMesh>();

        public Editor()
        {

            InitializeComponent();

            //Instantiate our core and run basic setup
            InitCore();

            this.FormClosing += Editor_FormClosing;
            lstEntities.MouseDoubleClick += lstEntities_MouseDoubleClick;

            Scene.SetBackgroundColor(0.5f, 0.8f, 0.9f);

            //Init Camera
            Engine.SetCamera(Camera);
            Camera.SetViewFrustum(World.Camera.DegreesFOV, World.Camera.FarPlane, World.Camera.NearPlane);
            Camera.SetPosition(World.Camera.PosX, World.Camera.PosY, World.Camera.PosZ);

            DoLoop = true;
            Main_Loop();

        }

        private void InitCore()
        {
            if (World == null)
            {
                World = new R3DWorld();
            }

            Engine = new TVEngine();
            Scene = new TVScene();
            Inputs = new TVInputEngine();
            Atmo = new TVAtmosphere();
            MatFact = new TVMaterialFactory();
            TexFact = new TVTextureFactory();
            Globals = new TVGlobals();
            Maths = new TVMathLibrary();
            Screen2D = new TVScreen2DImmediate();
            Text2D = new TVScreen2DText();
            Lights = new TVLightEngine();
            Camera = new TVCamera();

            Engine.SetSearchDirectory(Application.StartupPath);

            Engine.Init3DWindowed(WorldViewPort.Handle);

            viewPort = Engine.GetViewport();
            viewPort.SetAutoResize(true);
            
            Engine.DisplayFPS(true);

            Engine.SetAngleSystem(MTV3D65.CONST_TV_ANGLE.TV_ANGLE_DEGREE);

            Engine.SetDebugFile("C:\\debug.txt");
            Engine.SetDebugMode(true);

            if (initKeys != false || initMouse != false)
            {
                Inputs.Initialize(initKeys, initMouse);
            }

            if (vSyncON)
            {
                Engine.SetVSync(vSyncON);
            }

            this.Show();
            this.Focus();

            TexFact.SetTextureMode(CONST_TV_TEXTUREMODE.TV_TEXTUREMODE_32BITS);

        }

        private void Main_Loop()
        {

            do
            {

                System.Windows.Forms.Application.DoEvents();

                Check_Input();

                Check_Movement();

                if (World.Water != null)
                {
                    World.Water.UpdateWater(ref Camera, ref Maths);
                }

                Engine.Clear();

                Scene.RenderAllMeshes();

                Atmo.Clouds_Render();
                RenderDistant();
                RenderEnvironment();

                if (World.Water != null)
                {
                    World.Water.RenderWater();
                }

                Text2D.Action_BeginText();
                Text2D.TextureFont_DrawText("X:" + sngPositionX, 10, 25, Globals.RGBA(1, 1, 1, 1));
                Text2D.TextureFont_DrawText("Y:" + sngPositionY, 10, 45, Globals.RGBA(1, 1, 1, 1));
                Text2D.Action_EndText();

                Engine.RenderToScreen();

            } while (!(DoLoop == false));

            Destroy();

        }

        private void Editor_FormClosing(object sender, FormClosingEventArgs e)
        {
            Destroy(true);
        }


        public void InitLoadedWorld()
        {

            DoLoop = false;
            Main_Loop();

            InitCore();

            if (World != null)
            {
                // Init skybox
                if (World.SkyBox != null)
                {
                    World.SkyBox.CreateSkybox(ref TexFact, ref Atmo, ref Globals);
                }

                // Init lighting setup
                Lights.CreateDirectionalLight(new TV_3DVECTOR(0, -0.9f, -0.8f), 1, 1, 0.95f, "Sunlight", 1f);

                // Init Landscape
                if (World.Landscape != null)
                {
                    World.Landscape.CreateLandscape(ref Land, ref Scene, ref TexFact, ref MatFact, ref Globals);
                }

                //Init Waterplane
                if (World.Water != null)
                {
                    World.Water.CreateWaterPlane(ref TexFact, ref Scene, ref Camera, ref Maths, ref Globals);
                }  

                if (World.EntityManager != null && World.EntityManager.Count > 0)
                {

                    EntityManager = World.EntityManager;
                    World.LoadMeshesFromEM(ref RawEntityManager, ref EntityManager, ref Scene, ref TexFact);

                    BindEntities();

                }

                DoLoop = true;
                Main_Loop();

            }

        }

        public void RenderDistant()
        {
            if (Atmo != null) { Atmo.Atmosphere_Render(); }
        }

        public void RenderEnvironment()
        {
            if (Land != null) { Land.Render(); }   
        }

        private void Check_Input()
        {
            //Camera Movement

            if (Inputs.IsKeyPressed(CONST_TV_KEY.TV_KEY_W) == true)
            {
                sngWalk = 1;
            }
            else if (Inputs.IsKeyPressed(CONST_TV_KEY.TV_KEY_S) == true)
            {
                sngWalk = -1;
            }


            if (Inputs.IsKeyPressed(CONST_TV_KEY.TV_KEY_A) == true)
            {
                sngStrafe = 1;
            }
            else if (Inputs.IsKeyPressed(CONST_TV_KEY.TV_KEY_D) == true)
            {
                sngStrafe = -1;
            }

            if (Inputs.IsKeyPressed(CONST_TV_KEY.TV_KEY_Q) == true)
            {
                int tmpMouseX = 0;
                int tmpMouseY = 0;

                Inputs.GetMouseState(ref tmpMouseX, ref tmpMouseY);

                sngAngleX = sngAngleX - ((float)tmpMouseY / 100);
                sngAngleY = sngAngleY - ((float)tmpMouseX / 100);
            }

        }


        private void Check_Movement()
        {
            if (sngAngleX > 1.3f)
                sngAngleX = 1.3f;
            if (sngAngleX < -1.3)
                sngAngleX = -1.3f;

            if (sngWalk > 0)
            {
                sngWalk = sngWalk - (float)0.005 * (float)Engine.TimeElapsed();
                if (sngWalk < 0)
                    sngWalk = 0;
            }else if(sngWalk < 0)
            {
                sngWalk = sngWalk + (float)0.005 * (float)Engine.TimeElapsed();
                if (sngWalk > 0)
                    sngWalk = 0;
            }


            if (sngStrafe > 0)
            {
                sngStrafe = sngStrafe - (float)0.005 * (float)Engine.TimeElapsed();
                if (sngStrafe < 0)
                    sngStrafe = 0;
            }else if (sngStrafe < 0)
            {
                sngStrafe = sngStrafe + (float)0.005 * (float)Engine.TimeElapsed();
                if (sngStrafe > 0)
                    sngStrafe = 0;
            }

            sngPositionX = sngPositionX + ((float)Math.Cos(sngAngleY) * sngWalk / 2 * Engine.TimeElapsed()) + (float)Math.Cos(sngAngleY + 3.141596 / 2) * sngStrafe / 5 * Engine.TimeElapsed();
            sngPositionZ = sngPositionZ + ((float)Math.Sin(sngAngleY) * sngWalk / 2 * Engine.TimeElapsed()) + (float)Math.Sin(sngAngleY + 3.141596 / 2) * sngStrafe / 5 * Engine.TimeElapsed();

            if (Land != null)
            {
                sngPositionY = Land.GetHeight(sngPositionX, sngPositionZ) + 30;
            }else
            {
                sngPositionY = 40f;
            }


            snglookatX = sngPositionX + (float)Math.Cos(sngAngleY);
            snglookatY = sngPositionY + (float)Math.Tan(sngAngleX);
            snglookatZ = sngPositionZ + (float)Math.Sin(sngAngleY);

            Scene.SetCamera(sngPositionX, sngPositionY, sngPositionZ, snglookatX, snglookatY, snglookatZ);

        }


        private void Destroy(bool exit = false)
        {
            //Destruction of objects to clear memory.
            Engine = null;
            Scene = null;
            Inputs = null;
            Atmo = null;
            MatFact = null;
            TexFact = null;
            Globals = null;
            Maths = null;
            Screen2D = null;
            Text2D = null;
            Lights = null;
            Camera = null;

            if (exit) { System.Environment.Exit(0); }          
        }

        private void ExitToolStripMenuItem2_Click(System.Object sender, System.EventArgs e)
        {
            Destroy(true);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = saveFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                string file = saveFileDialog.FileName;
                World.EntityManager = EntityManager;
                World.SaveWorld(file);
                MessageBox.Show("World saved!");
            }

        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {

            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                string file = openFileDialog.FileName;
                World.LoadWorld(file);
                EntityManager = World.EntityManager;
                InitLoadedWorld();

            }

        }

        private void bNewLand_Click(object sender, EventArgs e)
        {
            DialogResult dr;
            
            LandscapeDefinition LandSc = new LandscapeDefinition();

            using (R3D.Landscape.LandscapeEditor LandEdit = new R3D.Landscape.LandscapeEditor(ref LandSc))
            {
                dr = LandEdit.ShowDialog();
            }

            if (dr == DialogResult.OK)
            {
                //free memory
                Land = null;

                World.Landscape = LandSc;
                TV_3DVECTOR lv = new TV_3DVECTOR(0, -0.9F, -0.8F);
                Lights.CreateDirectionalLight(lv, 1, 1, 0.95F, "Sunlight", 1.0F);
                World.Landscape.CreateLandscape(ref Land, ref Scene, ref TexFact, ref MatFact, ref Globals);

            }

        }

        void lstEntities_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DialogResult dr;
            int index = this.lstEntities.IndexFromPoint(e.Location);
            if (index != System.Windows.Forms.ListBox.NoMatches)
            {
                MeshDefinition Mesh = (MeshDefinition)lstEntities.Items[index];
                using (R3D.Editors.MeshEditor meshEdit = new R3D.Editors.MeshEditor(ref Mesh))
                {
                   dr = meshEdit.ShowDialog();
                }

                if (dr == DialogResult.OK)
                {
                    Mesh.CreateMesh(ref RawEntityManager, ref EntityManager, ref Scene, ref TexFact);
                    BindEntities();
                }       

            }
        }

        private void BindEntities()
        {
            //clear out previous
            lstEntities.DataSource = null;

            lstEntities.DataSource = EntityManager;
            lstEntities.DisplayMember = "MeshName";
            lstEntities.ValueMember = "ID";
        }

        private void bCreateMesh_Click(object sender, EventArgs e)
        {
            DialogResult dr;
            MeshDefinition Mesh = new MeshDefinition();
            using (R3D.Editors.MeshEditor meshEdit = new R3D.Editors.MeshEditor(ref Mesh))
            {
                dr = meshEdit.ShowDialog();
            }

            if (dr == DialogResult.OK)
            {
                Mesh.CreateMesh(ref RawEntityManager, ref EntityManager, ref Scene, ref TexFact);
                BindEntities();
            }

        }

        private void bCreateModel_Click(object sender, EventArgs e)
        {

        }
    }

}