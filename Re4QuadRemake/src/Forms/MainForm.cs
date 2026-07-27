using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Re4QuadExtremeEditor.src;
using Re4QuadExtremeEditor.src.Class;
using Re4QuadExtremeEditor.src.Class.CAM;
using Re4QuadExtremeEditor.src.Class.TreeNodeObj;
using Re4QuadExtremeEditor.src.Forms;
using Re4QuadExtremeEditor.src.Class.Enums;
using Re4QuadExtremeEditor.src.Class.MyProperty;
using Re4QuadExtremeEditor.src.Class.MyProperty._EFF_Property;
using Re4QuadExtremeEditor.src.Class.MyProperty.CustomAttribute;
using Re4QuadExtremeEditor.src.Class.ObjMethods;
using Re4QuadExtremeEditor.src.Class.Files;
using Re4QuadExtremeEditor.src.Controls;
using Re4QuadExtremeEditor.src.JSON;
using System.IO;

namespace Re4QuadExtremeEditor
{
    public partial class MainForm : Form
    {
        GLControl glControl;
        readonly Timer myTimer = new Timer();
        readonly Timer renderTimer = new Timer();

        CameraMoveControl cameraMove;
        ObjectMoveControl objectMove;
        Advertising1Control advertising1Control;
        Advertising2Control advertising2Control;

        #region Camera // variaveis para a camera
        Camera camera = new Camera();
        Matrix4 camMtx = Matrix4.Identity;
        Matrix4 ProjMatrix;
        // movimentação da camera
        bool isShiftDown = false, isControlDown = false, isSpaceDown = false;
        bool isMouseDown = false, isMouseMove = false;
        bool isWDown = false, isSDown = false, isADown = false, isDDown = false;
        #endregion

        // Property que fica no PropertyGrid quando não tem nada selecionado;
        readonly NoneProperty none = new NoneProperty();

        // define se esta com o PropertyGrid selecionado;
        bool InPropertyGrid = false;


        UpdateMethods updateMethods;


        public MainForm()
        {
            InitializeComponent();
            propertyGridObjs.SelectedItemWithFocusBackColor = Color.FromArgb(0x70, 0xBB, 0xDB);
            propertyGridObjs.SelectedItemWithFocusForeColor = Globals.DarkMode ? Color.White : Color.Black;
            treeViewObjs.SelectedNodeBackColor = Color.FromArgb(0x70, 0xBB, 0xDB);

            propertyGridObjs.SelectedObjectsChanged += PropertyGridObjs_SelectedObjectsChanged;

            propertyGridObjs.SelectedObject = none;
            DataBase.SelectedNodes = treeViewObjs.SelectedNodes; // vinculo de referencia entra as listas

            camera.getSelectedObject = getSelectedObject;

            glControl = new OpenTK.GLControl();
            glControl.Dock = DockStyle.Fill;
            glControl.Name = "glControl";
            glControl.TabIndex = 999;
            glControl.TabStop = false;
            glControl.Paint += GlControl_Paint;
            glControl.Load += GlControl_Load;
            glControl.KeyDown += GlControl_KeyDown;
            glControl.KeyUp += GlControl_KeyUp;
            glControl.Leave += GlControl_Leave;
            glControl.MouseWheel += GlControl_MouseWheel;
            glControl.MouseMove += GlControl_MouseMove;
            glControl.MouseDown += GlControl_MouseDown;
            glControl.MouseUp += GlControl_MouseUp;
            glControl.MouseLeave += GlControl_MouseLeave;
            glControl.Resize += GlControl_Resize;
            panelGL.Controls.Add(glControl);

            cameraMove = new CameraMoveControl(ref camera, updateGL, UpdateCameraMatrix);
            cameraMove.Location = new Point(panelControls.Width - cameraMove.Width, 0);
            cameraMove.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            cameraMove.Name = "cameraMove";
            cameraMove.TabIndex = 998;
            cameraMove.TabStop = false;
            

            objectMove = new ObjectMoveControl(ref camera, updateGL, UpdateCameraMatrix, UpdatePropertyGrid);
            objectMove.Location = new Point(0, 0);
            objectMove.Anchor = AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Left;
            objectMove.Name = "objectMove";
            objectMove.TabIndex = 995;
            objectMove.TabStop = false;
           

            advertising1Control = new Advertising1Control();
            advertising1Control.Location = new Point(0, 0);
            advertising1Control.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            advertising1Control.Name = "advertising1Control";
            advertising1Control.TabIndex = 997;
            advertising1Control.TabStop = false;
            advertising1Control.Hide();

            advertising2Control = new Advertising2Control();
            advertising2Control.Location = new Point(0, 0);
            advertising2Control.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            advertising2Control.Name = "advertising2Control";
            advertising2Control.TabIndex = 996;
            advertising2Control.TabStop = false;
            advertising2Control.Hide();

            panelControls.Controls.Add(cameraMove);
            panelControls.Controls.Add(advertising1Control);
            panelControls.Controls.Add(advertising2Control);
            panelControls.Controls.Add(objectMove);
            enable_splitContainerRight_Panel2_Resize = true;


            KeyPreview = true;

            myTimer.Tick += updateWASDControls;
            myTimer.Interval = 10;
            myTimer.Enabled = false;

            renderTimer.Tick += (s, e) => glControl.Invalidate();
            renderTimer.Interval = 10; // مؤقت، يُحدَّث بعد تحميل الإعدادات
            renderTimer.Enabled = true;

            camMtx = camera.GetViewMatrix();
            ProjMatrix = ReturnNewProjMatrix();

            // toda os metodos listados abaixos, tem que seguir a sequencia abaixo, se não dara erro.

            Lang.StartAttributeTexts();
            Lang.StartTexts();

            Utils.StartLoadLangList();
            Utils.StartLoadConfigs();
            Utils.StartLoadRoomInfoList();
            Utils.StartLoadObjsInfoLists();
            Utils.StartLoadPromptMessageList();
            Utils.StartLoadLangFile();
            Utils.StartSetTextTranslationLists();

            if (Globals.DarkMode)
                Re4QuadExtremeEditor.src.Forms.OptionsForm.ApplyDarkMode(new System.Collections.Generic.List<Form> { this });
            else
                Re4QuadExtremeEditor.src.Forms.OptionsForm.ApplyLightMode(new System.Collections.Generic.List<Form> { this });

            renderTimer.Interval = 1000 / Globals.TargetFPS;
            Utils.StartEnemyExtraSegmentList();
            Utils.StartSetListBoxsProperty();
            Utils.StartSetListBoxsPropertybjsInfoLists();
            if (Lang.LoadedTranslation) 
            { 
                StartUpdateTranslation();
                cameraMove.StartUpdateTranslation();
                objectMove.StartUpdateTranslation();
            }

            Utils.StartCreateNodes();
            Utils.StartExtraGroup();
            treeViewObjs.Nodes.Add(DataBase.NodeESL);
            treeViewObjs.Nodes.Add(DataBase.NodeETS);
            treeViewObjs.Nodes.Add(DataBase.NodeITA);
            treeViewObjs.Nodes.Add(DataBase.NodeAEV);
            treeViewObjs.Nodes.Add(DataBase.NodeEXTRAS);
            treeViewObjs.Nodes.Add(DataBase.NodeDSE);
            // CAM ainda experimental: nó oculto da árvore intencionalmente (código mantido para uso futuro)
            treeViewObjs.Nodes.Add(DataBase.NodeFSE);
            treeViewObjs.Nodes.Add(DataBase.NodeSAR);
            treeViewObjs.Nodes.Add(DataBase.NodeEAR);
            treeViewObjs.Nodes.Add(DataBase.NodeEMI);
            treeViewObjs.Nodes.Add(DataBase.NodeESE);
            treeViewObjs.Nodes.Add(DataBase.NodeLIT_Groups);
            treeViewObjs.Nodes.Add(DataBase.NodeLIT_Entrys);
            treeViewObjs.Nodes.Add(DataBase.NodeEFF_Table0);
            treeViewObjs.Nodes.Add(DataBase.NodeEFF_Table1);
            treeViewObjs.Nodes.Add(DataBase.NodeEFF_Table2);
            treeViewObjs.Nodes.Add(DataBase.NodeEFF_Table3);
            treeViewObjs.Nodes.Add(DataBase.NodeEFF_Table4);
            treeViewObjs.Nodes.Add(DataBase.NodeEFF_Table6);
            treeViewObjs.Nodes.Add(DataBase.NodeEFF_Table7_Effect_0);
            treeViewObjs.Nodes.Add(DataBase.NodeEFF_Table8_Effect_1);
            treeViewObjs.Nodes.Add(DataBase.NodeEFF_EffectEntry);
            treeViewObjs.Nodes.Add(DataBase.NodeEFF_Table9);

            updateMethods = new UpdateMethods();
            updateMethods.UpdateGL = updateGL;
            updateMethods.UpdatePropertyGrid = UpdatePropertyGrid;
            updateMethods.UpdateTreeViewObjs = UpdateTreeViewObjs;
            updateMethods.UpdateMoveObjSelection = objectMove.UpdateSelection;

            //apenas para testes
            //src.JSON.LangFile.writeLangFile("SourceLang.json");
            //int finish = 0;
        }

        bool enable_splitContainerRight_Panel2_Resize = false;
        private void panelControls_Resize(object sender, EventArgs e)
        {
            if (enable_splitContainerRight_Panel2_Resize)
            {
                int painel2Width = panelControls.Width;
                int quite = painel2Width / 2;

                int adWidth = advertising1Control.Width;
                int adquite = adWidth / 2;

                int ad2Width = advertising2Control.Width;
                int ad2quite = ad2Width / 2;

                if (painel2Width > 640 + advertising2Control.Width)
                {
                    int posX = quite - ad2quite;
                    if (posX < 395)
                    {
                        posX = 395;
                    }
                    advertising1Control.Hide();
                    advertising1Control.Location = new Point(painel2Width, advertising1Control.Location.Y);
                    advertising2Control.Location = new Point(posX, advertising2Control.Location.Y);
                    advertising2Control.Show();
                }
                else if (painel2Width > 640 + advertising1Control.Width)
                {
                    int posX = quite - adquite;
                    if (posX < 395)
                    {
                        posX = 395;
                    }
                    advertising2Control.Hide();
                    advertising2Control.Location = new Point(painel2Width, advertising2Control.Location.Y);
                    advertising1Control.Location = new Point(posX, advertising1Control.Location.Y);
                    advertising1Control.Show();
                }
                else
                {
                    advertising1Control.Hide();
                    advertising2Control.Hide();
                    advertising1Control.Location = new Point(painel2Width, advertising1Control.Location.Y);
                    advertising2Control.Location = new Point(painel2Width, advertising2Control.Location.Y);
                }
            }
        }

        private IObject3D getSelectedObject()
        {
            if (DataBase.LastSelectNode is Object3D node)
            {
                return node;
            }
            return null;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UpdateChecker.CheckForUpdatesAsync(this);
        }

        #region GlControl Events

        private Matrix4 ReturnNewProjMatrix() 
        {
            return Matrix4.CreatePerspectiveFieldOfView(Globals.FOV * ((float)Math.PI / 180.0f), (float)glControl.Width / (float)glControl.Height, 0.01f, 1000000f); //10000f
        }

        private void GlControl_Resize(object sender, EventArgs e)
        {            
            glControl.Context.Update(glControl.WindowInfo);
            GL.Viewport(0, 0, glControl.Width, glControl.Height);
            ProjMatrix = ReturnNewProjMatrix();
            glControl.Invalidate(); 
        }

        private void splitContainer1_SplitterMoving(object sender, SplitterCancelEventArgs e)
        {
            glControl.Invalidate();
        }

        private void GlControl_MouseLeave(object sender, EventArgs e)
        {
            camera.resetMouseStuff();
            isMouseDown = false;
            isMouseMove = false;
        }

        private void GlControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                camera.resetMouseStuff();
                isMouseDown = false;
                isMouseMove = false;
                camera.SaveCameraPosition();
                if (!isWDown && !isSDown && !isADown && !isDDown && !isMouseMove && !isShiftDown && !isSpaceDown)
                {
                    myTimer.Enabled = false;
                }
            }    
        }

        private void GlControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                camera.resetMouseStuff();
                isMouseDown = true;
                isMouseMove = true;
                camera.SaveCameraPosition();
                myTimer.Enabled = true;
            }       
            if (e.Button == MouseButtons.Right)
            {
                selectObject(e.X, e.Y);
                glControl.Invalidate();
            }
        }

        /// <summary>
        /// metodo destinado para a seleção dos objetos no ambiente GL
        /// </summary>
        private void selectObject(int mx, int my)
        {
            int h = glControl.Height;
            TheRender.RenderToSelect(ref camMtx, ref ProjMatrix); // renderiza o ambiente GL no modo seleção.
            byte[] pixel = new byte[4];
            GL.ReadPixels(mx, h - my, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, pixel);

            //Console.WriteLine("pixel[0]: " + pixel[0]); // lineID
            //Console.WriteLine("pixel[1]: " + pixel[1]); // lineID
            //Console.WriteLine("pixel[2]: " + pixel[2]); // id da lista
            //Console.WriteLine("pixel[3]: " + pixel[3]);

            // listas
            // aviso: proibido usar os valores 0 e 255, pois fazem parte das cores preta (renderização do cenario) e da cor branca (fundo);
            // 1 = enemy
            // 2 = etcmodel
            // 3 = ITA
            // 4 = AEV
            // 5 = Extras
            // 6 = EMI
            // 7 = ESE
            // 8 = LIT Entrys
            // 9 = EFF Table7 (Effect Group)
            // 10 = EFF Table8 (Effect Group)
            // 11 = EFF EffectEntry
            // 12 = EFF Table9
            // 13 = FSE
            // 14 = SAR
            // 15 = EAR
            // 16 = DSE
            // 17 = LIT Groups
            if (pixel[2] > 0 && pixel[2] < 18) // caso adicionar novas lista aumentar o valor de 18;
            {
                ushort LineID = BitConverter.ToUInt16(pixel, 0);

                TreeNode selected = null;
                switch (pixel[2])
                {
                    case 1:
                        int index1 = DataBase.NodeESL.Nodes.IndexOfKey(LineID.ToString());
                        if (index1 > -1)
                        {
                            selected = DataBase.NodeESL.Nodes[index1];
                        }
                        break;
                    case 2:
                        int index2 = DataBase.NodeETS.Nodes.IndexOfKey(LineID.ToString());
                        if (index2 > -1)
                        {
                            selected = DataBase.NodeETS.Nodes[index2];
                        }
                        break;
                    case 3:
                        int index3 = DataBase.NodeITA.Nodes.IndexOfKey(LineID.ToString());
                        if (index3 > -1)
                        {
                            selected = DataBase.NodeITA.Nodes[index3];
                        }
                        break;
                    case 4:
                        int index4 = DataBase.NodeAEV.Nodes.IndexOfKey(LineID.ToString());
                        if (index4 > -1)
                        {
                            selected = DataBase.NodeAEV.Nodes[index4];
                        }
                        break;
                    case 5:
                        int index5 = DataBase.NodeEXTRAS.Nodes.IndexOfKey(LineID.ToString());
                        if (index5 > -1)
                        {
                            selected = DataBase.NodeEXTRAS.Nodes[index5];
                        }
                        break;
                    case 6:
                        int index6 = DataBase.NodeEMI.Nodes.IndexOfKey(LineID.ToString());
                        if (index6 > -1)
                        {
                            selected = DataBase.NodeEMI.Nodes[index6];
                        }
                        break;
                    case 7:
                        int index7 = DataBase.NodeESE.Nodes.IndexOfKey(LineID.ToString());
                        if (index7 > -1)
                        {
                            selected = DataBase.NodeESE.Nodes[index7];
                        }
                        break;
                    case 8:
                        int index8 = DataBase.NodeLIT_Entrys.Nodes.IndexOfKey(LineID.ToString());
                        if (index8 > -1)
                        {
                            selected = DataBase.NodeLIT_Entrys.Nodes[index8];
                        }
                        break;
                    case 9:
                        int index9 = DataBase.NodeEFF_Table7_Effect_0.Nodes.IndexOfKey(LineID.ToString());
                        if (index9 > -1)
                        {
                            selected = DataBase.NodeEFF_Table7_Effect_0.Nodes[index9];
                        }
                        break;
                    case 10:
                        int index10 = DataBase.NodeEFF_Table8_Effect_1.Nodes.IndexOfKey(LineID.ToString());
                        if (index10 > -1)
                        {
                            selected = DataBase.NodeEFF_Table8_Effect_1.Nodes[index10];
                        }
                        break;
                    case 11:
                        int index11 = DataBase.NodeEFF_EffectEntry.Nodes.IndexOfKey(LineID.ToString());
                        if (index11 > -1)
                        {
                            selected = DataBase.NodeEFF_EffectEntry.Nodes[index11];
                        }
                        break;
                    case 12:
                        int index12 = DataBase.NodeEFF_Table9.Nodes.IndexOfKey(LineID.ToString());
                        if (index12 > -1)
                        {
                            selected = DataBase.NodeEFF_Table9.Nodes[index12];
                        }
                        break;
                    case 13:
                        int index13 = DataBase.NodeFSE.Nodes.IndexOfKey(LineID.ToString());
                        if (index13 > -1)
                        {
                            selected = DataBase.NodeFSE.Nodes[index13];
                        }
                        break;
                    case 14:
                        int index14 = DataBase.NodeSAR.Nodes.IndexOfKey(LineID.ToString());
                        if (index14 > -1)
                        {
                            selected = DataBase.NodeSAR.Nodes[index14];
                        }
                        break;
                    case 15:
                        int index15 = DataBase.NodeEAR.Nodes.IndexOfKey(LineID.ToString());
                        if (index15 > -1)
                        {
                            selected = DataBase.NodeEAR.Nodes[index15];
                        }
                        break;
                    case 16:
                        int index16 = DataBase.NodeDSE.Nodes.IndexOfKey(LineID.ToString());
                        if (index16 > -1)
                        {
                            selected = DataBase.NodeDSE.Nodes[index16];
                        }
                        break;
                    case 17:
                        int index17 = DataBase.NodeLIT_Groups.Nodes.IndexOfKey(LineID.ToString());
                        if (index17 > -1)
                        {
                            selected = DataBase.NodeLIT_Groups.Nodes[index17];
                        }
                        break;
                }

                if (selected != null)
                {
                    if (isControlDown) // add ou remove da seleção
                    {
                        treeViewObjs.ToSelectMultiNode(selected);
                    }
                    else // seleciona so esse
                    {
                        treeViewObjs.ToSelectSingleNode(selected);
                    }

                }
            }
        }

        private void GlControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown && e.Button == MouseButtons.Left)
            {
                camera.updateCameraOffsetMatrixWithMouse(isControlDown, e.X, e.Y);
                camMtx = camera.GetViewMatrix();
            }
        }

        private void GlControl_MouseWheel(object sender, MouseEventArgs e)
        {
            camera.resetMouseStuff();
            camera.updateCameraMatrixWithScrollWheel((int)(e.Delta * 0.5f));
            camMtx = camera.GetViewMatrix();
            camera.SaveCameraPosition();
            glControl.Invalidate();
        }

        private void GlControl_Leave(object sender, EventArgs e)
        {
            isWDown = false;
            isSDown = false;
            isADown = false;
            isDDown = false;
            isSpaceDown = false;
            isShiftDown = false;
            isControlDown = false;
            isMouseDown = false;
            isMouseMove = false;
            myTimer.Enabled = false;
        }

        private void GlControl_KeyUp(object sender, KeyEventArgs e)
        {
            isShiftDown = e.Shift;
            isControlDown = e.Control;
            switch (e.KeyCode)
            {
                case Keys.W: isWDown = false; break;
                case Keys.S: isSDown = false; break;
                case Keys.A: isADown = false; break;
                case Keys.D: isDDown = false; break;
                case Keys.Space: isSpaceDown = false; break;
            }
            if (!isWDown && !isSDown && !isADown && !isDDown && !isMouseMove && !isShiftDown && !isSpaceDown)
            {
                myTimer.Enabled = false;
            }
            if (isControlDown)
            {
                camera.SaveCameraPosition();
                camera.resetMouseStuff();
            }
        }

        private void GlControl_KeyDown(object sender, KeyEventArgs e)
        {
            isShiftDown = e.Shift;
            isControlDown = e.Control;
            switch (e.KeyCode)
            {
                case Keys.W:
                    isWDown = true;
                    myTimer.Enabled = true;
                    break;
                case Keys.S:
                    isSDown = true;
                    myTimer.Enabled = true;
                    break;
                case Keys.A:
                    isADown = true;
                    myTimer.Enabled = true;
                    break;
                case Keys.D:
                    isDDown = true;
                    myTimer.Enabled = true;
                    break;
                case Keys.Space:
                    isSpaceDown = true;
                    myTimer.Enabled = true;
                    break;
            }
            if (isShiftDown)
            {
                myTimer.Enabled = true;
            }
            if (isControlDown)
            {
                camera.SaveCameraPosition();
                camera.resetMouseStuff();
            }

        }

        /// <summary>
        /// Atualiza a movimentação de wasd, e cria os "frames" da renderização.
        /// </summary>
        private void updateWASDControls(object sender, EventArgs e)
        {
            if (!isControlDown && camera.CamMode == Camera.CameraMode.FLY)
            {
                if (isWDown)
                {
                    camera.updateCameraToFront();
                }
                if (isSDown)
                {
                    camera.updateCameraToBack();
                }
                if (isDDown)
                {
                    camera.updateCameraToRight();
                }
                if (isADown)
                {
                    camera.updateCameraToLeft();
                }

                if (isShiftDown)
                {
                    camera.updateCameraToDown();
                }

                if (isSpaceDown)
                {
                    camera.updateCameraToUp();
                }

                if (isWDown || isSDown || isDDown || isADown || isShiftDown || isSpaceDown || isMouseMove)
                {
                    camMtx = camera.GetViewMatrix();
                    glControl.Invalidate();
                }

            }
            else 
            {
                glControl.Invalidate();
            }
        }


        private bool glResourcesLoaded = false;
        private string pendingProjectPathToOpen = null;

        private void GlControl_Load(object sender, EventArgs e)
        {
            Globals.OpenGLVersion = GL.GetString(StringName.Version);

            Utils.Defines_The_OpenGL_Used();

            GL.Viewport(0, 0, glControl.Width, glControl.Height);
            GL.ClearColor(Globals.SkyColor);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Texture2D);
            GL.LineWidth(1.5f);

            DataBase.NoTextureIdGL = Texture.GetTextureIdGL(Properties.Resources.NoTexture);
            DataBase.TransparentTextureIdGL = Texture.GetTextureIdGL(Properties.Resources.Transparent);
            DataBase.SolidTextureIdGL = Texture.GetTextureIdGL(Properties.Resources.SolidTexture);

            using (var whiteBmp = new System.Drawing.Bitmap(1, 1))
            {
                whiteBmp.SetPixel(0, 0, System.Drawing.Color.White);
                DataBase.WhiteTextureIdGL = Texture.GetTextureIdGL(whiteBmp);
            }

            if (Globals.UseOldGL)
            {
                Utils.StartLoadNoShader_OldGL();
            }
            else 
            {
                Utils.StartLoadShader();
            }     

            Utils.StartLoadObjsModels();

            glControl.SwapBuffers();

            glResourcesLoaded = true;

            if (!string.IsNullOrEmpty(pendingProjectPathToOpen))
            {
                string path = pendingProjectPathToOpen;
                pendingProjectPathToOpen = null;
                BeginInvoke((MethodInvoker)(() => OpenProjectFile(path)));
            }
        }


        private void GlControl_Paint(object sender, PaintEventArgs e)
        {
            //TheRender.RenderToSelect(ref camMtx, ref ProjMatrix); // este é da seleção
            TheRender.Render(ref camMtx, ref ProjMatrix, camera.SelectedObjPosY(), camera.Position); // rederiza todos os objetos do GL;
            glControl.SwapBuffers();
        }

        #endregion


        #region botões do menu edit

        private void toolStripMenuItemAddNewObj_Click(object sender, EventArgs e)
        {
            AddNewObjForm form = new AddNewObjForm();
            form.OnButtonOk_Click += OnButtonOk_Click;
            form.ShowDialog();
        }

        private void OnButtonOk_Click()
        {
            glControl.Invalidate();
        }

        private void toolStripMenuItemDeleteSelectedObj_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Lang.GetText(eLang.DeleteObjDialog), Lang.GetText(eLang.DeleteObjWarning), MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                foreach (Object3D item in treeViewObjs.SelectedNodes.Values)
                {
                    if (item.Group == GroupType.ETS)
                    {                    
                        ((EtcModelNodeGroup)item.Parent).ChangeAmountMethods.RemoveLineID(item.ObjLineRef);
                        item.Remove();
                    }
                    else if (item.Group == GroupType.ITA || item.Group == GroupType.AEV)
                    {
                        DataBase.Extras.RemoveObj(item.ObjLineRef, Utils.GroupTypeToSpecialFileFormat(item.Group));
                        ((SpecialNodeGroup)item.Parent).ChangeAmountMethods.RemoveLineID(item.ObjLineRef);
                        item.Remove();
                    }
                    else if (item.Parent is Re4QuadExtremeEditor.src.Class.Interfaces.INodeChangeAmount changeAmountParent)
                    {
                        changeAmountParent.ChangeAmountMethods.RemoveLineID(item.ObjLineRef);
                        item.Remove();
                    }
                }
                treeViewObjs.SelectedNodes = null;
                glControl.Invalidate();
            }
        }

        private void toolStripMenuItemMoveUp_Click(object sender, EventArgs e)
        {
            var ordernedSelectedNodes = treeViewObjs.SelectedNodes.Values.OrderBy(n => n.Index);
            foreach (Object3D item in ordernedSelectedNodes)
            {
                if (item.Group == GroupType.ETS || item.Group == GroupType.ITA || item.Group == GroupType.AEV)
                {
                    int index = item.Index;     
                    if (index > 0)
                    {
                        var Parent = item.Parent;
                        item.Remove();
                        Parent.Nodes.Insert(index -1, item);
                    }
                }
            }
        }

        private void toolStripMenuItemMoveDown_Click(object sender, EventArgs e)
        {
            var invSelectedNodes = treeViewObjs.SelectedNodes.Values.OrderByDescending(n => n.Index);
            foreach (Object3D item in invSelectedNodes)
            {
                if (item.Group == GroupType.ETS || item.Group == GroupType.ITA || item.Group == GroupType.AEV)
                {
                    int index = item.Index;
                    var Parent = item.Parent;
                    if (index < Parent.GetNodeCount(false) -1)
                    {
                        item.Remove();
                        Parent.Nodes.Insert(index +1, item);
                    }                 
                }
            }
        }


        private void toolStripMenuItemSearch_Click(object sender, EventArgs e)
        {
            var selectedObj = propertyGridObjs.SelectedObject;
            if (selectedObj is EnemyProperty enemy)
            {
                SearchForm search = new SearchForm(ListBoxProperty.EnemiesList.Values.ToArray(), new UshortObjForListBox(enemy.ReturnUshortFirstSearchSelect(), ""));
                search.Search += enemy.Searched;
                search.ShowDialog();
            }
            else if (selectedObj is EtcModelProperty etcModel)
            {
                SearchForm search = new SearchForm(ListBoxProperty.EtcmodelsList.Values.ToArray(), new UshortObjForListBox(etcModel.ReturnUshortFirstSearchSelect(), ""));
                search.Search += etcModel.Searched;
                search.ShowDialog();
            }
            else if (selectedObj is SpecialProperty special)
            {
                var specialType = special.GetSpecialType();
                if (specialType == SpecialType.T03_Items || specialType == SpecialType.T11_ItemDependentEvents)
                {
                    SearchForm search = new SearchForm(ListBoxProperty.ItemsList.Values.ToArray(), new UshortObjForListBox(special.ReturnUshortFirstSearchSelect(), ""));
                    search.Search += special.Searched;
                    search.ShowDialog();
                }
            }
        }


        #endregion


        #region Botoes do menu

        private void SelectRoom_onLoadButtonClick(object sender, EventArgs e)
        {
            if (sender is string == false)
            {
                string text = Lang.GetText(eLang.SelectedRoom) + ": " + sender.ToString();
                if (text.Length > 100)
                {
                    text = text.Substring(0,100);
                    text += "...";
                }
                toolStripMenuItemSelectRoom.Text = text;
            }
            else
            {
                toolStripMenuItemSelectRoom.Text = Lang.GetText(eLang.SelectRoom);
            }

            if (Globals.AutoDefinedRoom)
            {
                if (DataBase.SelectedRoom != null)
                {
                    toolStripTextBoxDefinedRoom.Text = DataBase.SelectedRoom.GetRoomInfo.RoomId.ToString("X4");
                }
                else
                {
                    toolStripTextBoxDefinedRoom.Text = "0000";
                }
            }
        }

        private void toolStripMenuItemSelectRoom_Click(object sender, EventArgs e)
        {
            SelectRoomForm selectRoom = new SelectRoomForm();
            selectRoom.onLoadButtonClick += SelectRoom_onLoadButtonClick;
            if (Globals.DarkMode)
                Re4QuadExtremeEditor.src.Forms.OptionsForm.ApplyDarkMode(new System.Collections.Generic.List<Form> { selectRoom });
            selectRoom.ShowDialog();

            if (DataBase.SelectedRoom != null && selectRoom.SelectedFileTypesToLoad != null && selectRoom.SelectedFileTypesToLoad.Count > 0)
            {
                LoadRoomFilesFromGameDirectory(DataBase.SelectedRoom.GetRoomInfo, selectRoom.SelectedDllVersion, selectRoom.SelectedFileTypesToLoad);
            }

            glControl.Invalidate();
        }

        /// <summary>
        /// Resolves and opens the checked file types (AEV/ITA/ETS/DSE/FSE/SAR/EAR/EMI/ESE/LIT/EFFBLOB/ESL)
        /// for the given room, from the RE4 UHD game directory configured in Options, according to
        /// the selected DLL/patch layout (Qingsheng / Raz0r / Without Any DLL). ESL is special-cased:
        /// it's not per-room, it's one of two fixed village-wide files chosen by room number.
        /// </summary>
        private void LoadRoomFilesFromGameDirectory(RoomInfo room, LoadRoomDllVersion version, List<string> fileTypes)
        {
            if (string.IsNullOrEmpty(Globals.DirectoryUHDRE4) || !System.IO.Directory.Exists(Globals.DirectoryUHDRE4))
            {
                MessageBox.Show("RE4 UHD game directory is not set (or doesn't exist). Set it first in Options > Diretory > RE4 UHD Game Path.",
                    "Load Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<string> notFound = new List<string>();

            foreach (string ext in fileTypes)
            {
                if (ext.Equals("ESL", StringComparison.OrdinalIgnoreCase))
                {
                    // ESL is not per-room like the other file types: RE4 UHD only ships two
                    // ESL files for the whole village area, and which one applies depends on
                    // the room's number, regardless of DLL/patch layout (path is always fixed):
                    //   R100-R109 -> [GAME]\BIO4\Etc\Emleon00.esl
                    //   R10A-R11F -> [GAME]\BIO4\Etc\Emleon01.esl
                    string eslFileName = ResolveVillageEslFileName(room.RoomKey);
                    if (eslFileName == null)
                    {
                        notFound.Add(ext);
                        continue;
                    }

                    string eslPath = System.IO.Path.Combine(Globals.DirectoryUHDRE4, "BIO4", "Etc", eslFileName);
                    if (!File.Exists(eslPath))
                    {
                        notFound.Add(ext);
                        continue;
                    }

                    try
                    {
                        FileInfo eslFileInfo = new FileInfo(eslPath);
                        if (eslFileInfo.Length == 0) { notFound.Add(ext); continue; }
                        using (FileStream stream = eslFileInfo.OpenRead())
                        {
                            FileManager.LoadFileESL(stream, eslFileInfo);
                            Globals.FilePathESL = eslPath;
                        }
                    }
                    catch (Exception)
                    {
                        notFound.Add(ext);
                    }
                    continue;
                }

                string path = Utils.ResolveRoomFilePath(Globals.DirectoryUHDRE4, version, room.RoomKey, ext);
                if (string.IsNullOrEmpty(path))
                {
                    notFound.Add(ext);
                    continue;
                }

                try
                {
                    FileInfo fileInfo = new FileInfo(path);
                    if (fileInfo.Length == 0) { notFound.Add(ext); continue; }
                    using (FileStream stream = fileInfo.OpenRead())
                    {
                        switch (ext.ToUpperInvariant())
                        {
                            case "AEV": FileManager.LoadFileAEV_UHD(stream, fileInfo); Globals.FilePathAEV = path; break;
                            case "ITA": FileManager.LoadFileITA_UHD(stream, fileInfo); Globals.FilePathITA = path; break;
                            case "ETS": FileManager.LoadFileETS_UHD(stream, fileInfo); Globals.FilePathETS = path; break;
                            case "DSE": FileManager.LoadFileDSE(stream, fileInfo); Globals.FilePathDSE = path; break;
                            case "FSE": FileManager.LoadFileFSE(stream, fileInfo); Globals.FilePathFSE = path; break;
                            case "SAR": FileManager.LoadFileSAR(stream, fileInfo); Globals.FilePathSAR = path; break;
                            case "EAR": FileManager.LoadFileEAR(stream, fileInfo); Globals.FilePathEAR = path; break;
                            case "EMI": FileManager.LoadFileEMI_UHD(stream, fileInfo); Globals.FilePathEMI = path; break;
                            case "ESE": FileManager.LoadFileESE_UHD(stream, fileInfo); Globals.FilePathESE = path; break;
                            case "LIT": FileManager.LoadFileLIT_UHD(stream, fileInfo); Globals.FilePathLIT = path; break;
                            case "EFFBLOB": FileManager.LoadFileEFFBLOB(stream, SimpleEndianBinaryIO.Endianness.LittleEndian); Globals.FilePathEFFBLOB = path; break;
                            default: notFound.Add(ext); break;
                        }
                    }
                }
                catch (Exception)
                {
                    notFound.Add(ext);
                }
            }

            TreeViewUpdateSelecteds();
            glControl.Invalidate();

            if (notFound.Count > 0)
            {
                MessageBox.Show("Could not find/load: " + string.Join(", ", notFound) + " for room " + room.RoomKey + ".",
                    "Load Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Resolves which of the two village ESL files applies to the given room key (e.g. "R108"),
        /// based on the room number, regardless of DLL/patch layout (the ESL path is always fixed):
        ///   R100-R109 -> Emleon00.esl
        ///   R10A-R11F -> Emleon01.esl
        /// Returns null if the room number is outside the village range (0x100-0x11F) this covers.
        /// </summary>
        private static string ResolveVillageEslFileName(string roomKey)
        {
            if (string.IsNullOrEmpty(roomKey) || roomKey.Length < 2) { return null; }

            string hexPart = roomKey.Substring(1); // strip leading "R"
            if (!int.TryParse(hexPart, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out int roomNumber))
            {
                return null;
            }

            if (roomNumber >= 0x100 && roomNumber <= 0x109) { return "Emleon00.esl"; }
            if (roomNumber >= 0x10A && roomNumber <= 0x11F) { return "Emleon01.esl"; }
            return null;
        }

        private void toolStripMenuItemClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // --- Standalone CAM test (temporary - will be merged into the normal
        // room LoadFile flow once round-trip load/save is confirmed to work) ---
        private void toolStripMenuItemOpenCAM_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "CAM files (*.CAM)|*.CAM|All files (*.*)|*.*";
                dialog.Title = "Open CAM";
                if (dialog.ShowDialog() != DialogResult.OK) { return; }

                try
                {
                    DataBase.FileCAM = CamFile.Read(dialog.FileName);
                    Globals.FilePathCAM = dialog.FileName;

                    DataBase.NodeCAM.Nodes.Clear();

                    TreeNode triggersRoot = new TreeNode("TriggerZones (" + DataBase.FileCAM.Table2.Count + ")");
                    TreeNode camerasRoot = new TreeNode("Cameras (" + DataBase.FileCAM.Table3.Count + ")");

                    for (int i = 0; i < DataBase.FileCAM.Table2.Count; i++)
                    {
                        var t2 = DataBase.FileCAM.Table2[i];
                        TreeNode node = new TreeNode(
                            "[" + i.ToString("00") + "] " + t2.Coords.Count + " points, Height=" + t2.Height.ToString("0.0"));
                        node.Tag = new CamPickTag { Kind = CamPickKind.TriggerZone, Index = i };
                        triggersRoot.Nodes.Add(node);
                    }

                    for (int i = 0; i < DataBase.FileCAM.Table3.Count; i++)
                    {
                        var t3 = DataBase.FileCAM.Table3[i];
                        string camTypeText = CamFile.CamTypeNames.ContainsKey(t3.CamType)
                            ? CamFile.CamTypeNames[t3.CamType]
                            : "Unknown";
                        TreeNode node = new TreeNode(
                            "[" + i.ToString("00") + "] CamId=" + t3.CamId + " - " + camTypeText +
                            " (" + t3.Positions.Count + " pos)");
                        node.Tag = new CamPickTag { Kind = CamPickKind.Camera, Index = i };
                        camerasRoot.Nodes.Add(node);
                    }

                    DataBase.NodeCAM.Nodes.Add(triggersRoot);
                    DataBase.NodeCAM.Nodes.Add(camerasRoot);
                    DataBase.NodeCAM.Expand();
                    triggersRoot.Expand();
                    camerasRoot.Expand();

                    glControl.Invalidate();

                    var validation = DataBase.FileCAM.Validate();
                    string msg = "Loaded OK.\nTable1: " + DataBase.FileCAM.Table1.Count +
                        "  Table2: " + DataBase.FileCAM.Table2.Count +
                        "  Table3: " + DataBase.FileCAM.Table3.Count +
                        "  Table4: " + DataBase.FileCAM.Table4.Count;

                    msg += "\n\n--- Debug per entry ---";
                    for (int i = 0; i < DataBase.FileCAM.Table1.Count; i++)
                    {
                        var dt1 = DataBase.FileCAM.Table1[i];
                        var dt3 = dt1.Table3Index < DataBase.FileCAM.Table3.Count ? DataBase.FileCAM.Table3[dt1.Table3Index] : null;
                        if (dt3 == null) { continue; }
                        msg += "\n#" + i + " t2idx=" + dt1.Table2Index + " t3idx=" + dt1.Table3Index +
                            " CamId=" + dt3.CamId + " CamType=" + dt3.CamType +
                            " BufCount=" + dt3.BufCount +
                            " Pos=" + dt3.Positions.Count + " Tgt=" + dt3.Targets.Count +
                            " Addr1=0x" + dt3.BufAddr1.ToString("X") + " Addr2=0x" + dt3.BufAddr2.ToString("X");
                    }

                    if (validation.Count > 0)
                    {
                        msg += "\n\nValidation notes:\n" + string.Join("\n", validation);
                    }
                    MessageBox.Show(msg, "CAM Loaded");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to load CAM:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void toolStripMenuItemSaveCAM_Click(object sender, EventArgs e)
        {
            if (DataBase.FileCAM == null)
            {
                MessageBox.Show("No CAM file loaded.", "Error");
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "CAM files (*.CAM)|*.CAM|All files (*.*)|*.*";
                dialog.Title = "Save CAM (Test)";
                dialog.FileName = string.IsNullOrEmpty(Globals.FilePathCAM) ? "output.CAM" : System.IO.Path.GetFileName(Globals.FilePathCAM);
                if (dialog.ShowDialog() != DialogResult.OK) { return; }

                try
                {
                    DataBase.FileCAM.Write(dialog.FileName);
                    MessageBox.Show("Saved successfully.\n\nTip: compare byte-for-byte against the original with a hex diff tool to confirm round-trip accuracy.", "CAM Saved");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to save CAM:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void toolStripMenuItemCredits_Click(object sender, EventArgs e)
        {
            CreditsForm form = new CreditsForm();
            form.ShowDialog();
        }

        private void toolStripMenuItemOptions_Click(object sender, EventArgs e)
        {
            OptionsForm form = new OptionsForm();
            form.onLoadButtonClick += SelectRoom_onLoadButtonClick;
            form.OnOKButtonClick += UpdateTreeViewObjs;
            form.OnOKButtonClick += UpdatePropertyGrid;
            if (Globals.DarkMode)
                Re4QuadExtremeEditor.src.Forms.OptionsForm.ApplyDarkMode(new System.Collections.Generic.List<Form> { form });
            form.ShowDialog();
            glControl.Invalidate();
        }


        #endregion


        #region botoes do menu view

        private void toolStripMenuItemHideRoomModel_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideRoomModel.Checked = !toolStripMenuItemHideRoomModel.Checked;
            Globals.RenderRoom = !toolStripMenuItemHideRoomModel.Checked;
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideEnemyESL_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideEnemyESL.Checked = !toolStripMenuItemHideEnemyESL.Checked;
            Globals.RenderEnemyESL = !toolStripMenuItemHideEnemyESL.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideEtcmodelETS_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideEtcmodelETS.Checked = !toolStripMenuItemHideEtcmodelETS.Checked;
            Globals.RenderEtcmodelETS = !toolStripMenuItemHideEtcmodelETS.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideItemsITA_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideItemsITA.Checked = !toolStripMenuItemHideItemsITA.Checked;
            Globals.RenderItemsITA = !toolStripMenuItemHideItemsITA.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideEventsAEV_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideEventsAEV.Checked = !toolStripMenuItemHideEventsAEV.Checked;
            Globals.RenderEventsAEV = !toolStripMenuItemHideEventsAEV.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        // NOTE: These types (DSE/FSE/SAR/EAR/EMI/ESE/LIT) don't yet have their position/rotation
        // offsets wired into MethodsForGL, so toggling these currently has no visible effect in the
        // 3D scene. The toggles are wired and ready; once the offsets are supplied, the render calls
        // in TheRender.cs will start drawing these objects and these flags will take effect.
        private void toolStripMenuItemHideDSE_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideDSE.Checked = !toolStripMenuItemHideDSE.Checked;
            Globals.RenderFileDSE = !toolStripMenuItemHideDSE.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideFSE_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideFSE.Checked = !toolStripMenuItemHideFSE.Checked;
            Globals.RenderFileFSE = !toolStripMenuItemHideFSE.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideSAR_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideSAR.Checked = !toolStripMenuItemHideSAR.Checked;
            Globals.RenderFileSAR = !toolStripMenuItemHideSAR.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideEAR_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideEAR.Checked = !toolStripMenuItemHideEAR.Checked;
            Globals.RenderFileEAR = !toolStripMenuItemHideEAR.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideEMI_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideEMI.Checked = !toolStripMenuItemHideEMI.Checked;
            Globals.RenderFileEMI = !toolStripMenuItemHideEMI.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideESE_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideESE.Checked = !toolStripMenuItemHideESE.Checked;
            Globals.RenderFileESE = !toolStripMenuItemHideESE.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideLITGroups_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideLITGroups.Checked = !toolStripMenuItemHideLITGroups.Checked;
            Globals.RenderFileLIT_Groups = !toolStripMenuItemHideLITGroups.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideLITEntrys_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideLITEntrys.Checked = !toolStripMenuItemHideLITEntrys.Checked;
            Globals.RenderFileLIT_Entrys = !toolStripMenuItemHideLITEntrys.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideEFFBLOB_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideEFFBLOB.Checked = !toolStripMenuItemHideEFFBLOB.Checked;
            Globals.RenderFileEFFBLOB = !toolStripMenuItemHideEFFBLOB.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }


        private void toolStripMenuItemHideDesabledEnemy_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideDesabledEnemy.Checked = !toolStripMenuItemHideDesabledEnemy.Checked;
            Globals.RenderDisabledEnemy = !toolStripMenuItemHideDesabledEnemy.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripTextBoxDefinedRoom_TextChanged(object sender, EventArgs e)
        {
            Globals.RenderEnemyFromDefinedRoom = ushort.Parse(toolStripTextBoxDefinedRoom.Text, System.Globalization.NumberStyles.HexNumber);
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripTextBoxDefinedRoom_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (char.IsDigit(e.KeyChar) 
                || e.KeyChar == 'A'
                || e.KeyChar == 'B'
                || e.KeyChar == 'C'
                || e.KeyChar == 'D'
                || e.KeyChar == 'E'
                || e.KeyChar == 'F'
                || e.KeyChar == 'a'
                || e.KeyChar == 'b'
                || e.KeyChar == 'c'
                || e.KeyChar == 'd'
                || e.KeyChar == 'e'
                || e.KeyChar == 'f'
                )
            {
                if (toolStripTextBoxDefinedRoom.SelectionStart < toolStripTextBoxDefinedRoom.TextLength)
                {
                    int CacheSelectionStart = toolStripTextBoxDefinedRoom.SelectionStart;
                    StringBuilder sb = new StringBuilder(toolStripTextBoxDefinedRoom.Text);
                    sb[toolStripTextBoxDefinedRoom.SelectionStart] = e.KeyChar;
                    toolStripTextBoxDefinedRoom.Text = sb.ToString();
                    toolStripTextBoxDefinedRoom.SelectionStart = CacheSelectionStart + 1;
                }
            }
            e.Handled = true;
        }


        private void toolStripMenuItemShowOnlyDefinedRoom_Click(object sender, EventArgs e)
        {
            toolStripMenuItemShowOnlyDefinedRoom.Checked = !toolStripMenuItemShowOnlyDefinedRoom.Checked;
            Globals.RenderDontShowOnlyDefinedRoom = !toolStripMenuItemShowOnlyDefinedRoom.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemAutoDefineRoom_Click(object sender, EventArgs e)
        {
            toolStripMenuItemAutoDefineRoom.Checked = !toolStripMenuItemAutoDefineRoom.Checked;
            Globals.AutoDefinedRoom = toolStripMenuItemAutoDefineRoom.Checked;
        }

        private void toolStripMenuItemHideItemTriggerZone_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideItemTriggerZone.Checked = !toolStripMenuItemHideItemTriggerZone.Checked;
            Globals.RenderItemTriggerZone = !toolStripMenuItemHideItemTriggerZone.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideItemTriggerRadius_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideItemTriggerRadius.Checked = !toolStripMenuItemHideItemTriggerRadius.Checked;
            Globals.RenderItemTriggerRadius = !toolStripMenuItemHideItemTriggerRadius.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }


        private void toolStripMenuItemItemPositionAtAssociatedObjectLocation_Click(object sender, EventArgs e)
        {
            toolStripMenuItemItemPositionAtAssociatedObjectLocation.Checked = !toolStripMenuItemItemPositionAtAssociatedObjectLocation.Checked;
            Globals.RenderItemPositionAtAssociatedObjectLocation = toolStripMenuItemItemPositionAtAssociatedObjectLocation.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideExtraObjs_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideExtraObjs.Checked = !toolStripMenuItemHideExtraObjs.Checked;
            Globals.RenderExtraObjs = !toolStripMenuItemHideExtraObjs.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideSpecialTriggerZone_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideSpecialTriggerZone.Checked = !toolStripMenuItemHideSpecialTriggerZone.Checked;
            Globals.RenderSpecialTriggerZone = !toolStripMenuItemHideSpecialTriggerZone.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemUseMoreSpecialColors_Click(object sender, EventArgs e)
        {
            toolStripMenuItemUseMoreSpecialColors.Checked = !toolStripMenuItemUseMoreSpecialColors.Checked;
            Globals.UseMoreSpecialColors = toolStripMenuItemUseMoreSpecialColors.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemEtcModelUseScale_Click(object sender, EventArgs e)
        {
            toolStripMenuItemEtcModelUseScale.Checked = !toolStripMenuItemEtcModelUseScale.Checked;
            Globals.RenderEtcmodelUsingScale = toolStripMenuItemEtcModelUseScale.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideExtraExceptWarpDoor_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideExtraExceptWarpDoor.Checked = !toolStripMenuItemHideExtraExceptWarpDoor.Checked;
            Globals.HideExtraExceptWarpDoor = toolStripMenuItemHideExtraExceptWarpDoor.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemHideOnlyWarpDoor_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideOnlyWarpDoor.Checked = !toolStripMenuItemHideOnlyWarpDoor.Checked;
            Globals.RenderExtraWarpDoor = !toolStripMenuItemHideOnlyWarpDoor.Checked;
            treeViewObjs.Refresh();
            glControl.Invalidate();
        }

        private void toolStripMenuItemNodeDisplayNameInHex_Click(object sender, EventArgs e)
        {
            toolStripMenuItemNodeDisplayNameInHex.Checked = !toolStripMenuItemNodeDisplayNameInHex.Checked;
            Globals.TreeNodeRenderHexValues = toolStripMenuItemNodeDisplayNameInHex.Checked;
            if (Globals.TreeNodeRenderHexValues)
            {
                treeViewObjs.Font = Globals.TreeNodeFontHex;
            }
            else 
            {
                treeViewObjs.Font = Globals.TreeNodeFontText;
            }
            treeViewObjs.Refresh();
        }

        private void toolStripMenuItemRefresh_Click(object sender, EventArgs e)
        {
            glControl.Invalidate();
            treeViewObjs.Refresh();
            propertyGridObjs.Refresh();
            glControl.Update(); // Needed after calling propertyGridObjs.Refresh();
        }

        private void toolStripMenuItemResetCamera_Click(object sender, EventArgs e)
        {
            cameraMove.ResetCamera();
        }

        /// <summary>
        /// "Move to the Camera": moves every currently selected object (regardless of file type
        /// - ESL, ITA, AEV, EAR, SAR, EMI, ESE, DSE, FSE, LIT, EtcModel, Special, EFFBLOB...) so
        /// it lands at the camera's current position, instead of moving the camera to the object
        /// (which "Reset Camera"/orbit already does the other way around).
        /// </summary>
        private void toolStripMenuItemMoveToCamera_Click(object sender, EventArgs e)
        {
            MoveObj.MoveSelectedObjectsToCamera(camera);
            updateGL();
            UpdatePropertyGrid();
        }

        #endregion


        #region propertyGridObjs and TreeViewObjs

        private void updateGL() 
        {
            glControl.Invalidate();
        }

        private void UpdateCameraMatrix() 
        {
            camMtx = camera.GetViewMatrix();
        }

        public void UpdatePropertyGrid() 
        {
            propertyGridObjs.Refresh();
            glControl.Update(); // Needed after calling propertyGridObjs.Refresh();
        }

        public void UpdateTreeViewObjs()
        {
            treeViewObjs.Refresh();
        }


        private void propertyGridObjs_Enter(object sender, EventArgs e)
        {
            InPropertyGrid = true;
        }

        /// <summary>
        /// PropertyGrid resets some of its internal colors (notably property/category label
        /// colors) whenever SelectedObject changes and new rows are created, which is why
        /// entries could still show up black even after DarkMode.Apply() was called once at
        /// startup. Re-applying here keeps every newly displayed row themed correctly.
        /// </summary>
        private void PropertyGridObjs_SelectedObjectsChanged(object sender, EventArgs e)
        {
            if (Globals.DarkMode)
            {
                propertyGridObjs.BackColor = DarkMode.PropertyBack;
                propertyGridObjs.ViewBackColor = DarkMode.PropertyBack;
                propertyGridObjs.ViewForeColor = Color.White;
                propertyGridObjs.LineColor = DarkMode.BorderColor;
                propertyGridObjs.CategoryForeColor = DarkMode.AccentColor;
                propertyGridObjs.HelpBackColor = Color.FromArgb(35, 35, 35);
                propertyGridObjs.HelpForeColor = Color.White;
            }
        }

        private void propertyGridObjs_Leave(object sender, EventArgs e)
        {
            InPropertyGrid = false;
        }

        private void propertyGridObjs_PropertySortChanged(object sender, EventArgs e)
        {
            if (propertyGridObjs.PropertySort == PropertySort.CategorizedAlphabetical)
               {propertyGridObjs.PropertySort = PropertySort.Categorized;}
        }


        private void propertyGridObjs_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            propertyGridObjs.Refresh();
            treeViewObjs.Refresh();
        }

        private void propertyGridObjs_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
        {
            //
            if (camera.isOrbitCamera())
            {
                camera.UpdateCameraOrbitOnChangeValue();
                camMtx = camera.GetViewMatrix();
            }
        }

        private void TreeViewUpdateSelecteds()
        {
            treeViewObjs.SelectedNodes = null;
            propertyGridObjs.SelectedObject = none;
        }

        private void treeViewObjs_AfterSelect(object sender, TreeViewEventArgs e)
        {
            bool OldLastNodeIsNull = !(DataBase.LastSelectNode is Object3D);
            //Console.WriteLine(e.Node);
            //Console.WriteLine(treeViewObjs.SelectedNodes.Count);
            if (e.Node == null || e.Node.Parent == null || treeViewObjs.SelectedNodes.Count == 0)
            {
                propertyGridObjs.SelectedObject = none;
                DataBase.LastSelectNode = null;
            }
            else if (treeViewObjs.SelectedNodes.Count == 1 && e.Node is Object3D node)
            {
                DataBase.LastSelectNode = node;

                if (node.Group == GroupType.ESL)
                {
                    EnemyProperty p = new EnemyProperty(node.ObjLineRef, updateMethods, ((EnemyNodeGroup)node.Parent).PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.ETS)
                {
                    EtcModelProperty p = new EtcModelProperty(node.ObjLineRef, updateMethods, ((EtcModelNodeGroup)node.Parent).PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.ITA)
                {
                    SpecialProperty p = new SpecialProperty(node.ObjLineRef, updateMethods, ((SpecialNodeGroup)node.Parent).PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.AEV)
                {
                    SpecialProperty p = new SpecialProperty(node.ObjLineRef, updateMethods, ((SpecialNodeGroup)node.Parent).PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.FSE)
                {
                    NewAge_FSE_Property p = new NewAge_FSE_Property(node.ObjLineRef, updateMethods, DataBase.NodeFSE.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.SAR)
                {
                    NewAge_ESAR_Property p = new NewAge_ESAR_Property(node.ObjLineRef, updateMethods, DataBase.NodeSAR.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.EAR)
                {
                    NewAge_ESAR_Property p = new NewAge_ESAR_Property(node.ObjLineRef, updateMethods, DataBase.NodeEAR.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.EMI)
                {
                    NewAge_EMI_Property p = new NewAge_EMI_Property(node.ObjLineRef, updateMethods, DataBase.NodeEMI.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.ESE)
                {
                    NewAge_ESE_Property p = new NewAge_ESE_Property(node.ObjLineRef, updateMethods, DataBase.NodeESE.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.DSE)
                {
                    NewAge_DSE_Property p = new NewAge_DSE_Property(node.ObjLineRef, updateMethods, DataBase.NodeDSE.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.LIT_GROUPS)
                {
                    NewAge_LIT_Group_Property p = new NewAge_LIT_Group_Property(node.ObjLineRef, updateMethods, DataBase.NodeLIT_Groups.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.LIT_ENTRYS)
                {
                    NewAge_LIT_Entry_Property p = new NewAge_LIT_Entry_Property(node.ObjLineRef, updateMethods, DataBase.NodeLIT_Entrys.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.EFF_Table7_Effect_0)
                {
                    NewAge_EFF_EffectGroup_Property p = new NewAge_EFF_EffectGroup_Property(node.ObjLineRef, GroupType.EFF_Table7_Effect_0, updateMethods, DataBase.NodeEFF_Table7_Effect_0.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.EFF_Table8_Effect_1)
                {
                    NewAge_EFF_EffectGroup_Property p = new NewAge_EFF_EffectGroup_Property(node.ObjLineRef, GroupType.EFF_Table8_Effect_1, updateMethods, DataBase.NodeEFF_Table8_Effect_1.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.EFF_EffectEntry)
                {
                    NewAge_EFF_EffectEntry_Property p = new NewAge_EFF_EffectEntry_Property(node.ObjLineRef, updateMethods, DataBase.NodeEFF_EffectEntry.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.EFF_Table9)
                {
                    NewAge_EFF_Table9Entry_Property p = new NewAge_EFF_Table9Entry_Property(node.ObjLineRef, updateMethods, DataBase.NodeEFF_Table9.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.EFF_Table2)
                {
                    EFF_Table2_Property p = new EFF_Table2_Property(node.ObjLineRef, updateMethods, DataBase.NodeEFF_Table2.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.EFF_Table4)
                {
                    EFF_Table4_Property p = new EFF_Table4_Property(node.ObjLineRef, updateMethods, DataBase.NodeEFF_Table4.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.EFF_Table0)
                {
                    EFF_Table0_Property p = new EFF_Table0_Property(node.ObjLineRef, updateMethods, DataBase.NodeEFF_Table0.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.EFF_Table1)
                {
                    EFF_Table1_Property p = new EFF_Table1_Property(node.ObjLineRef, updateMethods, DataBase.NodeEFF_Table1.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.EFF_Table3)
                {
                    EFF_Table3_Property p = new EFF_Table3_Property(node.ObjLineRef, updateMethods, DataBase.NodeEFF_Table3.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.EFF_Table6)
                {
                    EFF_Table6_Property p = new EFF_Table6_Property(node.ObjLineRef, updateMethods, DataBase.NodeEFF_Table6.PropertyMethods);
                    propertyGridObjs.SelectedObject = p;
                }
                else if (node.Group == GroupType.EXTRAS)
                {
                    var r = DataBase.Extras.AssociationList[node.ObjLineRef];
                    if (r.FileFormat == SpecialFileFormat.AEV)
                    {
                        SpecialProperty p = new SpecialProperty(r.LineID, updateMethods, DataBase.NodeAEV.PropertyMethods, true);
                        propertyGridObjs.SelectedObject = p;
                    }
                    else if (r.FileFormat == SpecialFileFormat.ITA)
                    {
                        SpecialProperty p = new SpecialProperty(r.LineID, updateMethods, DataBase.NodeITA.PropertyMethods, true);
                        propertyGridObjs.SelectedObject = p;
                    }
                    else
                    {
                        propertyGridObjs.SelectedObject = none;
                    }

                }
            }
            else if (treeViewObjs.SelectedNodes.Count > 1)
            {
                DataBase.LastSelectNode = treeViewObjs.SelectedNodes.Last().Value;

                MultiSelectProperty p = new MultiSelectProperty(updateMethods);
                p.LoadContent(treeViewObjs.SelectedNodes.Values.ToList());
                propertyGridObjs.SelectedObject = p;
            }
            else 
            {
                propertyGridObjs.SelectedObject = none;
                DataBase.LastSelectNode = null;
            }
            if (camera.isOrbitCamera())
            {
                if (OldLastNodeIsNull)
                {
                    camera.ResetOrbitToSelectedObject();
                }
                camera.UpdateCameraOrbitOnChangeObj();
                camMtx = camera.GetViewMatrix();
            }
            objectMove.UpdateSelection();
            glControl.Invalidate();
        }  

        #endregion


        #region Gerenciamento de arquivos //new

        private void toolStripMenuItemNewESL_Click(object sender, EventArgs e)
        {
            FileManager.NewFileESL();
            Globals.FilePathESL = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }

        private void toolStripMenuItemNewETS_Classic_Click(object sender, EventArgs e)
        {
            FileManager.NewFileETS(Re4Version.Classic);
            Globals.FilePathETS = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }

        private void toolStripMenuItemNewETS_UHD_Click(object sender, EventArgs e)
        {
            FileManager.NewFileETS(Re4Version.UHD);
            Globals.FilePathETS = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }

        private void toolStripMenuItemNewITA_Classic_Click(object sender, EventArgs e)
        {
            FileManager.NewFileITA(Re4Version.Classic);
            Globals.FilePathITA = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }

        private void toolStripMenuItemNewITA_UHD_Click(object sender, EventArgs e)
        {
            FileManager.NewFileITA(Re4Version.UHD);
            Globals.FilePathITA = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }

        private void toolStripMenuItemNewAEV_Classic_Click(object sender, EventArgs e)
        {
            FileManager.NewFileAEV(Re4Version.Classic);
            Globals.FilePathAEV = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }

        private void toolStripMenuItemNewAEV_UHD_Click(object sender, EventArgs e)
        {
            FileManager.NewFileAEV(Re4Version.UHD);
            Globals.FilePathAEV = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }

        #endregion

        #region Gerenciamento de arquivos //open

        private bool OpenIsUHD = false;
        private void toolStripMenuItemOpenESL_Click(object sender, EventArgs e)
        {
            SetInitialDirectory(openFileDialogESL, "ESL");
            openFileDialogESL.ShowDialog();
        }
        private void toolStripMenuItemOpenETS_Classic_Click(object sender, EventArgs e)
        {
            OpenIsUHD = false;
            SetInitialDirectory(openFileDialogETS, "ETS");
            openFileDialogETS.ShowDialog();
        }
        private void toolStripMenuItemOpenETS_UHD_Click(object sender, EventArgs e)
        {
            OpenIsUHD = true;
            SetInitialDirectory(openFileDialogETS, "ETS");
            openFileDialogETS.ShowDialog();
        }
        private void toolStripMenuItemOpenITA_Classic_Click(object sender, EventArgs e)
        {
            OpenIsUHD = false;
            SetInitialDirectory(openFileDialogITA, "ITA");
            openFileDialogITA.ShowDialog();
        }

        private void toolStripMenuItemOpenITA_UHD_Click(object sender, EventArgs e)
        {
            OpenIsUHD = true;
            SetInitialDirectory(openFileDialogITA, "ITA");
            openFileDialogITA.ShowDialog();
        }

        private void toolStripMenuItemOpenAEV_Classic_Click(object sender, EventArgs e)
        {
            OpenIsUHD = false;
            SetInitialDirectory(openFileDialogAEV, "AEV");
            openFileDialogAEV.ShowDialog();
        }

        private void toolStripMenuItemOpenAEV_UHD_Click(object sender, EventArgs e)
        {
            OpenIsUHD = true;
            SetInitialDirectory(openFileDialogAEV, "AEV");
            openFileDialogAEV.ShowDialog();
        }

        private void openFileDialogESL_FileOk(object sender, CancelEventArgs e)
        {
            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(openFileDialogESL.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                e.Cancel = true;
                return;
            }
            if (fileInfo != null)
            {
                if (fileInfo.Length > 0x1000000)
                {
                    MessageBox.Show(Lang.GetText(eLang.MessageBoxFile16MB), Lang.GetText(eLang.MessageBoxWarningTitle), MessageBoxButtons.OK);
                    e.Cancel = true;
                    return;
                }
                else if (fileInfo.Length == 0)
                {
                    MessageBox.Show(Lang.GetText(eLang.MessageBoxFile0MB), Lang.GetText(eLang.MessageBoxWarningTitle), MessageBoxButtons.OK);
                    e.Cancel = true;
                    return;
                }
                else
                {
                    FileStream file = null;
                    try
                    {
                        file = fileInfo.OpenRead();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                        e.Cancel = true;
                        return;
                    }
                    if (file != null && fileInfo != null)
                    {
                        FileManager.LoadFileESL(file, fileInfo);
                        file.Close();
                        Globals.FilePathESL = openFileDialogESL.FileName;
                        SaveLastDirectory(Globals.FilePathESL, "ESL");
                        openFileDialogESL.FileName = null;
                        TreeViewUpdateSelecteds();
                        glControl.Invalidate();
                    }
 
                }
            }

        }

        private void openFileDialogETS_FileOk(object sender, CancelEventArgs e)
        {
            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(openFileDialogETS.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                e.Cancel = true;
                return;
            }
            if (fileInfo != null)
            {
                if (fileInfo.Length > 0x1000000)
                {
                    MessageBox.Show(Lang.GetText(eLang.MessageBoxFile16MB), Lang.GetText(eLang.MessageBoxWarningTitle), MessageBoxButtons.OK);
                    e.Cancel = true;
                    return;
                }
                else if (fileInfo.Length == 0)
                {
                    MessageBox.Show(Lang.GetText(eLang.MessageBoxFile0MB), Lang.GetText(eLang.MessageBoxWarningTitle), MessageBoxButtons.OK);
                    e.Cancel = true;
                    return;
                }
                else if (fileInfo.Length < 16)
                {
                    MessageBox.Show(Lang.GetText(eLang.MessageBoxFile16Bytes), Lang.GetText(eLang.MessageBoxWarningTitle), MessageBoxButtons.OK);
                    e.Cancel = true;
                    return;
                }
                else
                {
                    FileStream file = null;
                    try
                    {
                        file = fileInfo.OpenRead();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                        e.Cancel = true;
                        return;
                    }
                    if (file != null && fileInfo != null)
                    {
                        if (OpenIsUHD)
                        {
                            FileManager.LoadFileETS_UHD(file, fileInfo);
                        }
                        else
                        {
                            FileManager.LoadFileETS_Classic(file, fileInfo);
                        }
                        file.Close();
                        Globals.FilePathETS = openFileDialogETS.FileName;
                        SaveLastDirectory(Globals.FilePathETS, "ETS");
                        openFileDialogETS.FileName = null;
                        TreeViewUpdateSelecteds();
                        glControl.Invalidate();
                    }
                }
            }
        }
        private void openFileDialogITA_FileOk(object sender, CancelEventArgs e)
        {
            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(openFileDialogITA.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                e.Cancel = true;
                return;
            }
            if (fileInfo != null)
            {
                if (fileInfo.Length > 0x1000000)
                {
                    MessageBox.Show(Lang.GetText(eLang.MessageBoxFile16MB), Lang.GetText(eLang.MessageBoxWarningTitle), MessageBoxButtons.OK);
                    e.Cancel = true;
                    return;
                }
                else if (fileInfo.Length == 0)
                {
                    MessageBox.Show(Lang.GetText(eLang.MessageBoxFile0MB), Lang.GetText(eLang.MessageBoxWarningTitle), MessageBoxButtons.OK);
                    e.Cancel = true;
                    return;
                }
                else if (fileInfo.Length < 16)
                {
                    MessageBox.Show(Lang.GetText(eLang.MessageBoxFile16Bytes), Lang.GetText(eLang.MessageBoxWarningTitle), MessageBoxButtons.OK);
                    e.Cancel = true;
                    return;
                }
                else
                {
                    FileStream file = null;
                    try
                    {
                        file = fileInfo.OpenRead();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                        e.Cancel = true;
                        return;
                    }
                    if (file != null && fileInfo != null)
                    {
                        if (OpenIsUHD)
                        {
                            FileManager.LoadFileITA_UHD(file, fileInfo);
                        }
                        else
                        {
                            FileManager.LoadFileITA_Classic(file, fileInfo);
                        }
                        file.Close();
                        Globals.FilePathITA = openFileDialogITA.FileName;
                        SaveLastDirectory(Globals.FilePathITA, "ITA");
                        openFileDialogITA.FileName = null;
                        TreeViewUpdateSelecteds();
                        glControl.Invalidate();
                    }
                }
            }
        }
        private void openFileDialogAEV_FileOk(object sender, CancelEventArgs e)
        {
            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(openFileDialogAEV.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                e.Cancel = true;
                return;
            }
            if (fileInfo != null)
            {
                if (fileInfo.Length > 0x1000000)
                {
                    MessageBox.Show(Lang.GetText(eLang.MessageBoxFile16MB), Lang.GetText(eLang.MessageBoxWarningTitle), MessageBoxButtons.OK);
                    e.Cancel = true;
                    return;
                }
                else if (fileInfo.Length == 0)
                {
                    MessageBox.Show(Lang.GetText(eLang.MessageBoxFile0MB), Lang.GetText(eLang.MessageBoxWarningTitle), MessageBoxButtons.OK);
                    e.Cancel = true;
                    return;
                }
                else if (fileInfo.Length < 16)
                {
                    MessageBox.Show(Lang.GetText(eLang.MessageBoxFile16Bytes), Lang.GetText(eLang.MessageBoxWarningTitle), MessageBoxButtons.OK);
                    e.Cancel = true;
                    return;
                }
                else
                {
                    FileStream file = null;
                    try
                    {
                        file = fileInfo.OpenRead();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                        e.Cancel = true;
                        return;
                    }
                    if (file != null && fileInfo != null)
                    {
                        if (OpenIsUHD)
                        {
                            FileManager.LoadFileAEV_UHD(file, fileInfo);
                        }
                        else
                        {
                            FileManager.LoadFileAEV_Classic(file, fileInfo);
                        }
                        file.Close();
                        Globals.FilePathAEV = openFileDialogAEV.FileName;
                        SaveLastDirectory(Globals.FilePathAEV, "AEV");
                        openFileDialogAEV.FileName = null;
                        TreeViewUpdateSelecteds();
                        glControl.Invalidate();
                    }
                }
            }
        }
        #endregion

        #region Gerenciamento de arquivos //Clear

        private void toolStripMenuItemClear_DropDownOpening(object sender, EventArgs e)
        {
            toolStripMenuItemClearESL.Enabled = DataBase.FileESL != null;
            toolStripMenuItemClearETS.Enabled = DataBase.FileETS != null;
            toolStripMenuItemClearITA.Enabled = DataBase.FileITA != null;
            toolStripMenuItemClearAEV.Enabled = DataBase.FileAEV != null;
            toolStripMenuItemClearDSE.Enabled = DataBase.FileDSE != null;
            toolStripMenuItemClearFSE.Enabled = DataBase.FileFSE != null;
            toolStripMenuItemClearSAR.Enabled = DataBase.FileSAR != null;
            toolStripMenuItemClearEAR.Enabled = DataBase.FileEAR != null;
            toolStripMenuItemClearEMI.Enabled = DataBase.FileEMI != null;
            toolStripMenuItemClearESE.Enabled = DataBase.FileESE != null;
            toolStripMenuItemClearLIT.Enabled = DataBase.FileLIT != null;
            toolStripMenuItemClearEFFBLOB.Enabled = DataBase.FileEFF != null;
        }

        private void toolStripMenuItemClearAll_Click(object sender, EventArgs e)
        {
            if (DataBase.FileESL != null) toolStripMenuItemClearESL_Click(sender, e);
            if (DataBase.FileETS != null) toolStripMenuItemClearETS_Click(sender, e);
            if (DataBase.FileITA != null) toolStripMenuItemClearITA_Click(sender, e);
            if (DataBase.FileAEV != null) toolStripMenuItemClearAEV_Click(sender, e);
            if (DataBase.FileDSE != null) toolStripMenuItemClearDSE_Click(sender, e);
            if (DataBase.FileFSE != null) toolStripMenuItemClearFSE_Click(sender, e);
            if (DataBase.FileSAR != null) toolStripMenuItemClearSAR_Click(sender, e);
            if (DataBase.FileEAR != null) toolStripMenuItemClearEAR_Click(sender, e);
            if (DataBase.FileEMI != null) toolStripMenuItemClearEMI_Click(sender, e);
            if (DataBase.FileESE != null) toolStripMenuItemClearESE_Click(sender, e);
            if (DataBase.FileLIT != null) toolStripMenuItemClearLIT_Click(sender, e);
            if (DataBase.FileEFF != null) toolStripMenuItemClearEFFBLOB_Click(sender, e);
        }

        private void toolStripMenuItemClearESL_Click(object sender, EventArgs e)
        {
            FileManager.ClearESL();
            Globals.FilePathESL = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }

        private void toolStripMenuItemClearETS_Click(object sender, EventArgs e)
        {
            FileManager.ClearETS();
            Globals.FilePathETS = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }

        private void toolStripMenuItemClearITA_Click(object sender, EventArgs e)
        {
            FileManager.ClearITA();
            Globals.FilePathITA = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }

        private void toolStripMenuItemClearAEV_Click(object sender, EventArgs e)
        {
            FileManager.ClearAEV();
            Globals.FilePathAEV = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }

        private void toolStripMenuItemClearDSE_Click(object sender, EventArgs e)
        {
            FileManager.ClearDSE();
            Globals.FilePathDSE = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }
        private void toolStripMenuItemClearFSE_Click(object sender, EventArgs e)
        {
            FileManager.ClearFSE();
            Globals.FilePathFSE = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }
        private void toolStripMenuItemClearSAR_Click(object sender, EventArgs e)
        {
            FileManager.ClearSAR();
            Globals.FilePathSAR = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }
        private void toolStripMenuItemClearEAR_Click(object sender, EventArgs e)
        {
            FileManager.ClearEAR();
            Globals.FilePathEAR = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }
        private void toolStripMenuItemClearEMI_Click(object sender, EventArgs e)
        {
            FileManager.ClearEMI();
            Globals.FilePathEMI = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }
        private void toolStripMenuItemClearESE_Click(object sender, EventArgs e)
        {
            FileManager.ClearESE();
            Globals.FilePathESE = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }
        private void toolStripMenuItemClearLIT_Click(object sender, EventArgs e)
        {
            FileManager.ClearLIT();
            Globals.FilePathLIT = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }
        private void toolStripMenuItemClearEFFBLOB_Click(object sender, EventArgs e)
        {
            FileManager.ClearEFF();
            Globals.FilePathEFFBLOB = null;
            TreeViewUpdateSelecteds();
            glControl.Invalidate();
        }



        #endregion

        #region Gerenciamento de arquivos //Save As..

        private void toolStripMenuItemSaveAs_DropDownOpening(object sender, EventArgs e)
        {
            toolStripMenuItemSaveAsESL.Enabled = DataBase.FileESL != null;
            toolStripMenuItemSaveAsETS.Enabled = DataBase.FileETS != null;
            toolStripMenuItemSaveAsITA.Enabled = DataBase.FileITA != null;
            toolStripMenuItemSaveAsAEV.Enabled = DataBase.FileAEV != null;
            toolStripMenuItemSaveAsDSE.Enabled = DataBase.FileDSE != null;
            toolStripMenuItemSaveAsFSE.Enabled = DataBase.FileFSE != null;
            toolStripMenuItemSaveAsSAR.Enabled = DataBase.FileSAR != null;
            toolStripMenuItemSaveAsEAR.Enabled = DataBase.FileEAR != null;
            toolStripMenuItemSaveAsEMI.Enabled = DataBase.FileEMI != null;
            toolStripMenuItemSaveAsESE.Enabled = DataBase.FileESE != null;
            toolStripMenuItemSaveAsLIT.Enabled = DataBase.FileLIT != null;
            toolStripMenuItemSaveAsEFFBLOB.Enabled = DataBase.FileEFF != null;

            if (DataBase.FileETS != null && DataBase.FileETS.GetRe4Version == Re4Version.Classic)
            {
                toolStripMenuItemSaveAsETS.Text = Lang.GetText(eLang.toolStripMenuItemSaveAsETS_Classic);
            }
            else if (DataBase.FileETS != null && DataBase.FileETS.GetRe4Version == Re4Version.UHD)
            {
                toolStripMenuItemSaveAsETS.Text = Lang.GetText(eLang.toolStripMenuItemSaveAsETS_UHD);
            }
            else 
            {
                toolStripMenuItemSaveAsETS.Text = Lang.GetText(eLang.toolStripMenuItemSaveAsETS);
            }

            if (DataBase.FileITA != null && DataBase.FileITA.GetRe4Version == Re4Version.Classic)
            {
                toolStripMenuItemSaveAsITA.Text = Lang.GetText(eLang.toolStripMenuItemSaveAsITA_Classic);
            }
            else if (DataBase.FileITA != null && DataBase.FileITA.GetRe4Version == Re4Version.UHD)
            {
                toolStripMenuItemSaveAsITA.Text = Lang.GetText(eLang.toolStripMenuItemSaveAsITA_UHD);
            }
            else
            {
                toolStripMenuItemSaveAsITA.Text = Lang.GetText(eLang.toolStripMenuItemSaveAsITA);
            }

            if (DataBase.FileAEV != null && DataBase.FileAEV.GetRe4Version == Re4Version.Classic)
            {
                toolStripMenuItemSaveAsAEV.Text = Lang.GetText(eLang.toolStripMenuItemSaveAsAEV_Classic);
            }
            else if (DataBase.FileAEV != null && DataBase.FileAEV.GetRe4Version == Re4Version.UHD)
            {
                toolStripMenuItemSaveAsAEV.Text = Lang.GetText(eLang.toolStripMenuItemSaveAsAEV_UHD);
            }
            else
            {
                toolStripMenuItemSaveAsAEV.Text = Lang.GetText(eLang.toolStripMenuItemSaveAsAEV);
            }
        }

        private void toolStripMenuItemSaveAsESL_Click(object sender, EventArgs e)
        {
            saveFileDialogESL.FileName = Globals.FilePathESL;
            SetInitialDirectory(saveFileDialogESL, "ESL");
            saveFileDialogESL.ShowDialog();
        }

        private void toolStripMenuItemSaveAsETS_Click(object sender, EventArgs e)
        {
            saveFileDialogETS.FileName = Globals.FilePathETS;
            SetInitialDirectory(saveFileDialogETS, "ETS");
            saveFileDialogETS.ShowDialog();
        }

        private void toolStripMenuItemSaveAsITA_Click(object sender, EventArgs e)
        {
            saveFileDialogITA.FileName = Globals.FilePathITA;
            SetInitialDirectory(saveFileDialogITA, "ITA");
            saveFileDialogITA.ShowDialog();
        }

        private void toolStripMenuItemSaveAsAEV_Click(object sender, EventArgs e)
        {
            saveFileDialogAEV.FileName = Globals.FilePathAEV;
            SetInitialDirectory(saveFileDialogAEV, "AEV");
            saveFileDialogAEV.ShowDialog();
        }

        private void saveFileDialogESL_FileOk(object sender, CancelEventArgs e)
        {
            FileInfo file = null;
            FileStream stream = null;
            try
            {
                file = new FileInfo(saveFileDialogESL.FileName);
                stream = file.Create();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                e.Cancel = true;
                return;
            }

            if (file != null && stream != null)
            {
                FileManager.SaveFileESL(stream);
                stream.Close();
                Globals.FilePathESL = saveFileDialogESL.FileName;
                saveFileDialogESL.FileName = null;
            }
            
        }

        private void saveFileDialogETS_FileOk(object sender, CancelEventArgs e)
        {
            FileInfo file = null;
            FileStream stream = null;
            try
            {
                file = new FileInfo(saveFileDialogETS.FileName);
                stream = file.Create();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                e.Cancel = true;
                return;
            }

            if (file != null && stream != null)
            {
                FileManager.SaveFileETS(stream);
                stream.Close();
                Globals.FilePathETS = saveFileDialogETS.FileName;
                saveFileDialogETS.FileName = null;
            }
        }

        private void saveFileDialogITA_FileOk(object sender, CancelEventArgs e)
        {
            FileInfo file = null;
            FileStream stream = null;
            try
            {
                file = new FileInfo(saveFileDialogITA.FileName);
                stream = file.Create();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                e.Cancel = true;
                return;
            }

            if (file != null && stream != null)
            {
                FileManager.SaveFileITA(stream);
                stream.Close();
                Globals.FilePathITA = saveFileDialogITA.FileName;
                saveFileDialogITA.FileName = null;
            }
        }

        private void saveFileDialogAEV_FileOk(object sender, CancelEventArgs e)
        {
            FileInfo file = null;
            FileStream stream = null;
            try
            {
                file = new FileInfo(saveFileDialogAEV.FileName);
                stream = file.Create();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                e.Cancel = true;
                return;
            }

            if (file != null && stream != null)
            {
                FileManager.SaveFileAEV(stream);
                stream.Close();
                Globals.FilePathAEV = saveFileDialogAEV.FileName;
                saveFileDialogAEV.FileName = null;
            }
        }

        #endregion

        #region Gerenciamento de arquivos //Save

        private void toolStripMenuItemSave_DropDownOpening(object sender, EventArgs e)
        {
            toolStripMenuItemSaveESL.Enabled = DataBase.FileESL != null;
            toolStripMenuItemSaveETS.Enabled = DataBase.FileETS != null;
            toolStripMenuItemSaveITA.Enabled = DataBase.FileITA != null;
            toolStripMenuItemSaveAEV.Enabled = DataBase.FileAEV != null;
            toolStripMenuItemSaveDSE.Enabled = DataBase.FileDSE != null;
            toolStripMenuItemSaveFSE.Enabled = DataBase.FileFSE != null;
            toolStripMenuItemSaveSAR.Enabled = DataBase.FileSAR != null;
            toolStripMenuItemSaveEAR.Enabled = DataBase.FileEAR != null;
            toolStripMenuItemSaveEMI.Enabled = DataBase.FileEMI != null;
            toolStripMenuItemSaveESE.Enabled = DataBase.FileESE != null;
            toolStripMenuItemSaveLIT.Enabled = DataBase.FileLIT != null;
            toolStripMenuItemSaveEFFBLOB.Enabled = DataBase.FileEFF != null;

            if (DataBase.FileETS != null && DataBase.FileETS.GetRe4Version == Re4Version.Classic)
            {
                toolStripMenuItemSaveETS.Text = Lang.GetText(eLang.toolStripMenuItemSaveETS_Classic);
            }
            else if (DataBase.FileETS != null && DataBase.FileETS.GetRe4Version == Re4Version.UHD)
            {
                toolStripMenuItemSaveETS.Text = Lang.GetText(eLang.toolStripMenuItemSaveETS_UHD);
            }
            else
            {
                toolStripMenuItemSaveETS.Text = Lang.GetText(eLang.toolStripMenuItemSaveETS);
            }

            if (DataBase.FileITA != null && DataBase.FileITA.GetRe4Version == Re4Version.Classic)
            {
                toolStripMenuItemSaveITA.Text = Lang.GetText(eLang.toolStripMenuItemSaveITA_Classic);
            }
            else if (DataBase.FileITA != null && DataBase.FileITA.GetRe4Version == Re4Version.UHD)
            {
                toolStripMenuItemSaveITA.Text = Lang.GetText(eLang.toolStripMenuItemSaveITA_UHD);
            }
            else
            {
                toolStripMenuItemSaveITA.Text = Lang.GetText(eLang.toolStripMenuItemSaveITA);
            }

            if (DataBase.FileAEV != null && DataBase.FileAEV.GetRe4Version == Re4Version.Classic)
            {
                toolStripMenuItemSaveAEV.Text = Lang.GetText(eLang.toolStripMenuItemSaveAEV_Classic);
            }
            else if (DataBase.FileAEV != null && DataBase.FileAEV.GetRe4Version == Re4Version.UHD)
            {
                toolStripMenuItemSaveAEV.Text = Lang.GetText(eLang.toolStripMenuItemSaveAEV_UHD);
            }
            else
            {
                toolStripMenuItemSaveAEV.Text = Lang.GetText(eLang.toolStripMenuItemSaveAEV);
            }

        }

        private void toolStripMenuItemSaveAll_Click(object sender, EventArgs e)
        {
            if (DataBase.FileESL != null) toolStripMenuItemSaveESL_Click(sender, e);
            if (DataBase.FileETS != null) toolStripMenuItemSaveETS_Click(sender, e);
            if (DataBase.FileITA != null) toolStripMenuItemSaveITA_Click(sender, e);
            if (DataBase.FileAEV != null) toolStripMenuItemSaveAEV_Click(sender, e);
            if (DataBase.FileDSE != null) toolStripMenuItemSaveDSE_Click(sender, e);
            if (DataBase.FileFSE != null) toolStripMenuItemSaveFSE_Click(sender, e);
            if (DataBase.FileSAR != null) toolStripMenuItemSaveSAR_Click(sender, e);
            if (DataBase.FileEAR != null) toolStripMenuItemSaveEAR_Click(sender, e);
            if (DataBase.FileEMI != null) toolStripMenuItemSaveEMI_Click(sender, e);
            if (DataBase.FileESE != null) toolStripMenuItemSaveESE_Click(sender, e);
            if (DataBase.FileLIT != null) toolStripMenuItemSaveLIT_Click(sender, e);
            if (DataBase.FileEFF != null) toolStripMenuItemSaveEFFBLOB_Click(sender, e);
        }

        /// <summary>
        /// Salva o projeto ativo diretamente no caminho já conhecido (Globals.LastProjectPath),
        /// sem abrir nenhum dialog. Usado pelo fluxo de confirmação de saída.
        /// Retorna true se conseguiu salvar.
        /// </summary>
        private bool SaveActiveProjectDirectly()
        {
            if (string.IsNullOrEmpty(Globals.LastProjectPath) || !Globals.IsProjectActive)
            {
                return false;
            }

            try
            {
                ProjectManager.SaveProject(Globals.LastProjectPath, camera);
                SaveLastProjectPath(Globals.LastProjectPath);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void toolStripMenuItemSaveProject_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "RE4 Quad Remake Project|*" + ProjectManager.ProjectExtension;
                dlg.DefaultExt = ProjectManager.ProjectExtension;
                dlg.Title = "Save Re4 Quad Project";
                if (!string.IsNullOrEmpty(Globals.LastProjectPath))
                {
                    dlg.InitialDirectory = System.IO.Path.GetDirectoryName(Globals.LastProjectPath);
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ProjectManager.SaveProject(dlg.FileName, camera);
                        SaveLastProjectPath(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void toolStripMenuItemOpenProject_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "RE4 Quad Remake Project|*" + ProjectManager.ProjectExtension;
                dlg.DefaultExt = ProjectManager.ProjectExtension;
                dlg.Title = "Open Re4 Quad Project";
                if (!string.IsNullOrEmpty(Globals.LastProjectPath))
                {
                    dlg.InitialDirectory = System.IO.Path.GetDirectoryName(Globals.LastProjectPath);
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    OpenProjectFile(dlg.FileName);
                }
            }
        }

        private void SaveLastProjectPath(string path)
        {
            Globals.LastProjectPath = path;
            Globals.IsProjectActive = true;
            if (Globals.BackupConfigs != null)
            {
                Globals.BackupConfigs.LastProjectPath = path;
                try { Re4QuadExtremeEditor.src.JSON.ConfigsFile.writeConfigsFile(Consts.ConfigsFileDiretory, Globals.BackupConfigs); } catch { }
            }
        }

        public void OpenProjectFileWhenReady(string path)
        {
            if (glResourcesLoaded)
            {
                OpenProjectFile(path);
            }
            else
            {
                pendingProjectPathToOpen = path;
            }
        }

        public void OpenProjectFile(string path)
        {
            try
            {
                ProjectManager.LoadProject(path, camera);

                if (DataBase.SelectedRoom != null)
                {
                    SelectRoom_onLoadButtonClick(DataBase.SelectedRoom.GetRoomInfo, EventArgs.Empty);
                }
                else
                {
                    SelectRoom_onLoadButtonClick(string.Empty, EventArgs.Empty);
                }

                TreeViewUpdateSelecteds();
                glControl.Invalidate();

                SaveLastProjectPath(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripMenuItemSaveESL_Click(object sender, EventArgs e)
        {
            FileInfo file = null;
            FileStream stream = null;
            try
            {
                file = new FileInfo(Globals.FilePathESL);
                stream = file.Create();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                saveFileDialogESL.FileName = Globals.FilePathESL;
                SetInitialDirectory(saveFileDialogESL, "ESL");
            saveFileDialogESL.ShowDialog();
                return;
            }

            if (file != null && stream != null)
            {
                FileManager.SaveFileESL(stream);
                stream.Close();
            }
        }

        private void toolStripMenuItemSaveETS_Click(object sender, EventArgs e)
        {
            FileInfo file = null;
            FileStream stream = null;
            try
            {
                file = new FileInfo(Globals.FilePathETS);
                stream = file.Create();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                saveFileDialogETS.FileName = Globals.FilePathETS;
                SetInitialDirectory(saveFileDialogETS, "ETS");
            saveFileDialogETS.ShowDialog();
                return;
            }

            if (file != null && stream != null)
            {
                FileManager.SaveFileETS(stream);
                stream.Close();
            }
        }

        private void toolStripMenuItemSaveITA_Click(object sender, EventArgs e)
        {
            FileInfo file = null;
            FileStream stream = null;
            try
            {
                file = new FileInfo(Globals.FilePathITA);
                stream = file.Create();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                saveFileDialogITA.FileName = Globals.FilePathITA;
                SetInitialDirectory(saveFileDialogITA, "ITA");
            saveFileDialogITA.ShowDialog();
                return;
            }

            if (file != null && stream != null)
            {
                FileManager.SaveFileITA(stream);
                stream.Close();
            }
        }

        private void toolStripMenuItemSaveAEV_Click(object sender, EventArgs e)
        {
            FileInfo file = null;
            FileStream stream = null;
            try
            {
                file = new FileInfo(Globals.FilePathAEV);
                stream = file.Create();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                saveFileDialogAEV.FileName = Globals.FilePathAEV;
                SetInitialDirectory(saveFileDialogAEV, "AEV");
            saveFileDialogAEV.ShowDialog();
                return;
            }

            if (file != null && stream != null)
            {
                FileManager.SaveFileAEV(stream);
                stream.Close();
            }
        }


        // ===== NewAge Open Handlers =====
        private void toolStripMenuItemOpenDSE_Click(object sender, EventArgs e) { SetInitialDirectory(openFileDialogDSE, "DSE"); openFileDialogDSE.ShowDialog(); }
        private void toolStripMenuItemOpenFSE_Click(object sender, EventArgs e) { SetInitialDirectory(openFileDialogFSE, "FSE"); openFileDialogFSE.ShowDialog(); }
        private void toolStripMenuItemOpenSAR_Click(object sender, EventArgs e) { SetInitialDirectory(openFileDialogSAR, "SAR"); openFileDialogSAR.ShowDialog(); }
        private void toolStripMenuItemOpenEAR_Click(object sender, EventArgs e) { SetInitialDirectory(openFileDialogEAR, "EAR"); openFileDialogEAR.ShowDialog(); }
        private void toolStripMenuItemOpenEMI_Click(object sender, EventArgs e) { SetInitialDirectory(openFileDialogEMI, "EMI"); openFileDialogEMI.ShowDialog(); }
        private void toolStripMenuItemOpenESE_Click(object sender, EventArgs e) { SetInitialDirectory(openFileDialogESE, "ESE"); openFileDialogESE.ShowDialog(); }
        private void toolStripMenuItemOpenLIT_Click(object sender, EventArgs e) { SetInitialDirectory(openFileDialogLIT, "LIT"); openFileDialogLIT.ShowDialog(); }
        private void toolStripMenuItemOpenEFFBLOB_Click(object sender, EventArgs e) { SetInitialDirectory(openFileDialogEFFBLOB, "EFFBLOB"); openFileDialogEFFBLOB.ShowDialog(); }

        private void OpenNewAgeFile(System.Windows.Forms.OpenFileDialog dlg, Action<FileStream, FileInfo> loadFunc, ref string filePath, string fileType = null)
        {
            FileInfo fileInfo = null;
            try { fileInfo = new FileInfo(dlg.FileName); }
            catch (Exception ex) { MessageBox.Show(ex.Message); return; }
            if (fileInfo == null || fileInfo.Length == 0) return;
            FileStream file = null;
            try { file = fileInfo.OpenRead(); }
            catch (Exception ex) { MessageBox.Show(ex.Message); return; }
            if (file != null)
            {
                loadFunc(file, fileInfo);
                file.Close();
                filePath = dlg.FileName;
                SaveLastDirectory(filePath, fileType);
                dlg.FileName = null;
                TreeViewUpdateSelecteds();
                glControl.Invalidate();
            }
        }

        private void openFileDialogDSE_FileOk(object sender, CancelEventArgs e)    { string p = Globals.FilePathDSE;    OpenNewAgeFile(openFileDialogDSE,    FileManager.LoadFileDSE,          ref p, "DSE"); Globals.FilePathDSE    = p; }
        private void openFileDialogFSE_FileOk(object sender, CancelEventArgs e)    { string p = Globals.FilePathFSE;    OpenNewAgeFile(openFileDialogFSE,    FileManager.LoadFileFSE,          ref p, "FSE"); Globals.FilePathFSE    = p; }
        private void openFileDialogSAR_FileOk(object sender, CancelEventArgs e)    { string p = Globals.FilePathSAR;    OpenNewAgeFile(openFileDialogSAR,    FileManager.LoadFileSAR,          ref p, "SAR"); Globals.FilePathSAR    = p; }
        private void openFileDialogEAR_FileOk(object sender, CancelEventArgs e)    { string p = Globals.FilePathEAR;    OpenNewAgeFile(openFileDialogEAR,    FileManager.LoadFileEAR,          ref p, "EAR"); Globals.FilePathEAR    = p; }
        private void openFileDialogEMI_FileOk(object sender, CancelEventArgs e)    { string p = Globals.FilePathEMI;    OpenNewAgeFile(openFileDialogEMI,    FileManager.LoadFileEMI_UHD,      ref p, "EMI"); Globals.FilePathEMI    = p; }
        private void openFileDialogESE_FileOk(object sender, CancelEventArgs e)    { string p = Globals.FilePathESE;    OpenNewAgeFile(openFileDialogESE,    FileManager.LoadFileESE_UHD,      ref p, "ESE"); Globals.FilePathESE    = p; }
        private void openFileDialogLIT_FileOk(object sender, CancelEventArgs e)    { string p = Globals.FilePathLIT;    OpenNewAgeFile(openFileDialogLIT,    FileManager.LoadFileLIT_UHD,      ref p, "LIT"); Globals.FilePathLIT    = p; }
        private void openFileDialogEFFBLOB_FileOk(object sender, CancelEventArgs e){ string p = Globals.FilePathEFFBLOB;OpenNewAgeFile(openFileDialogEFFBLOB,(stream, info) => FileManager.LoadFileEFFBLOB(stream, SimpleEndianBinaryIO.Endianness.LittleEndian), ref p, "EFFBLOB"); Globals.FilePathEFFBLOB= p; }

        // ===== NewAge Save Handlers =====
        private void SaveNewAgeFile(string filePath, Action<FileStream> saveFunc, System.Windows.Forms.SaveFileDialog dlg = null, string fileType = null)
        {
            FileInfo file = null; FileStream stream = null;
            try { file = new FileInfo(filePath); stream = file.Create(); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                if (dlg != null) { dlg.FileName = filePath; SetInitialDirectory(dlg, fileType); dlg.ShowDialog(); }
                return;
            }
            if (file != null && stream != null) { saveFunc(stream); stream.Close(); }
        }

        private void toolStripMenuItemSaveDSE_Click(object sender, EventArgs e)     { SaveNewAgeFile(Globals.FilePathDSE,     FileManager.SaveFileDSE,     saveFileDialogDSE, "DSE"); }
        private void toolStripMenuItemSaveFSE_Click(object sender, EventArgs e)     { SaveNewAgeFile(Globals.FilePathFSE,     FileManager.SaveFileFSE,     saveFileDialogFSE, "FSE"); }
        private void toolStripMenuItemSaveSAR_Click(object sender, EventArgs e)     { SaveNewAgeFile(Globals.FilePathSAR,     FileManager.SaveFileSAR,     saveFileDialogSAR, "SAR"); }
        private void toolStripMenuItemSaveEAR_Click(object sender, EventArgs e)     { SaveNewAgeFile(Globals.FilePathEAR,     FileManager.SaveFileEAR,     saveFileDialogEAR, "EAR"); }
        private void toolStripMenuItemSaveEMI_Click(object sender, EventArgs e)     { SaveNewAgeFile(Globals.FilePathEMI,     FileManager.SaveFileEMI,     saveFileDialogEMI, "EMI"); }
        private void toolStripMenuItemSaveESE_Click(object sender, EventArgs e)     { SaveNewAgeFile(Globals.FilePathESE,     FileManager.SaveFileESE,     saveFileDialogESE, "ESE"); }
        private void toolStripMenuItemSaveLIT_Click(object sender, EventArgs e)     { SaveNewAgeFile(Globals.FilePathLIT,     FileManager.SaveFileLIT,     saveFileDialogLIT, "LIT"); }
        private void toolStripMenuItemSaveEFFBLOB_Click(object sender, EventArgs e) { SaveNewAgeFile(Globals.FilePathEFFBLOB, FileManager.SaveFileEFFBLOB, saveFileDialogEFFBLOB, "EFFBLOB"); }

        private void toolStripMenuItemSaveAsDSE_Click(object sender, EventArgs e)     { saveFileDialogDSE.FileName     = Globals.FilePathDSE;     SetInitialDirectory(saveFileDialogDSE, "DSE");     saveFileDialogDSE.ShowDialog(); }
        private void toolStripMenuItemSaveAsFSE_Click(object sender, EventArgs e)     { saveFileDialogFSE.FileName     = Globals.FilePathFSE;     SetInitialDirectory(saveFileDialogFSE, "FSE");     saveFileDialogFSE.ShowDialog(); }
        private void toolStripMenuItemSaveAsSAR_Click(object sender, EventArgs e)     { saveFileDialogSAR.FileName     = Globals.FilePathSAR;     SetInitialDirectory(saveFileDialogSAR, "SAR");     saveFileDialogSAR.ShowDialog(); }
        private void toolStripMenuItemSaveAsEAR_Click(object sender, EventArgs e)     { saveFileDialogEAR.FileName     = Globals.FilePathEAR;     SetInitialDirectory(saveFileDialogEAR, "EAR");     saveFileDialogEAR.ShowDialog(); }
        private void toolStripMenuItemSaveAsEMI_Click(object sender, EventArgs e)     { saveFileDialogEMI.FileName     = Globals.FilePathEMI;     SetInitialDirectory(saveFileDialogEMI, "EMI");     saveFileDialogEMI.ShowDialog(); }
        private void toolStripMenuItemSaveAsESE_Click(object sender, EventArgs e)     { saveFileDialogESE.FileName     = Globals.FilePathESE;     SetInitialDirectory(saveFileDialogESE, "ESE");     saveFileDialogESE.ShowDialog(); }
        private void toolStripMenuItemSaveAsLIT_Click(object sender, EventArgs e)     { saveFileDialogLIT.FileName     = Globals.FilePathLIT;     SetInitialDirectory(saveFileDialogLIT, "LIT");     saveFileDialogLIT.ShowDialog(); }
        private void toolStripMenuItemSaveAsEFFBLOB_Click(object sender, EventArgs e) { saveFileDialogEFFBLOB.FileName = Globals.FilePathEFFBLOB; SetInitialDirectory(saveFileDialogEFFBLOB, "EFFBLOB"); saveFileDialogEFFBLOB.ShowDialog(); }

        private void saveFileDialogDSE_FileOk(object sender, CancelEventArgs e)     { var f = new FileInfo(saveFileDialogDSE.FileName);     var s = f.Create(); FileManager.SaveFileDSE(s);     s.Close(); Globals.FilePathDSE     = saveFileDialogDSE.FileName;     saveFileDialogDSE.FileName     = null; }
        private void saveFileDialogFSE_FileOk(object sender, CancelEventArgs e)     { var f = new FileInfo(saveFileDialogFSE.FileName);     var s = f.Create(); FileManager.SaveFileFSE(s);     s.Close(); Globals.FilePathFSE     = saveFileDialogFSE.FileName;     saveFileDialogFSE.FileName     = null; }
        private void saveFileDialogSAR_FileOk(object sender, CancelEventArgs e)     { var f = new FileInfo(saveFileDialogSAR.FileName);     var s = f.Create(); FileManager.SaveFileSAR(s);     s.Close(); Globals.FilePathSAR     = saveFileDialogSAR.FileName;     saveFileDialogSAR.FileName     = null; }
        private void saveFileDialogEAR_FileOk(object sender, CancelEventArgs e)     { var f = new FileInfo(saveFileDialogEAR.FileName);     var s = f.Create(); FileManager.SaveFileEAR(s);     s.Close(); Globals.FilePathEAR     = saveFileDialogEAR.FileName;     saveFileDialogEAR.FileName     = null; }
        private void saveFileDialogEMI_FileOk(object sender, CancelEventArgs e)     { var f = new FileInfo(saveFileDialogEMI.FileName);     var s = f.Create(); FileManager.SaveFileEMI(s);     s.Close(); Globals.FilePathEMI     = saveFileDialogEMI.FileName;     saveFileDialogEMI.FileName     = null; }
        private void saveFileDialogESE_FileOk(object sender, CancelEventArgs e)     { var f = new FileInfo(saveFileDialogESE.FileName);     var s = f.Create(); FileManager.SaveFileESE(s);     s.Close(); Globals.FilePathESE     = saveFileDialogESE.FileName;     saveFileDialogESE.FileName     = null; }
        private void saveFileDialogLIT_FileOk(object sender, CancelEventArgs e)     { var f = new FileInfo(saveFileDialogLIT.FileName);     var s = f.Create(); FileManager.SaveFileLIT(s);     s.Close(); Globals.FilePathLIT     = saveFileDialogLIT.FileName;     saveFileDialogLIT.FileName     = null; }
        private void saveFileDialogEFFBLOB_FileOk(object sender, CancelEventArgs e) { var f = new FileInfo(saveFileDialogEFFBLOB.FileName); var s = f.Create(); FileManager.SaveFileEFFBLOB(s); s.Close(); Globals.FilePathEFFBLOB = saveFileDialogEFFBLOB.FileName; saveFileDialogEFFBLOB.FileName = null; }

        private void toolStripMenuItemSaveDirectories_DropDownOpening(object sender, EventArgs e)
        {
            toolStripMenuItemDiretory_ESL.Text = Lang.GetText(eLang.DiretoryESL) + " " + Globals.FilePathESL;
            toolStripMenuItemDiretory_ETS.Text = Lang.GetText(eLang.DiretoryETS) + " " + Globals.FilePathETS;
            toolStripMenuItemDiretory_ITA.Text = Lang.GetText(eLang.DiretoryITA) + " " + Globals.FilePathITA;
            toolStripMenuItemDiretory_AEV.Text = Lang.GetText(eLang.DiretoryAEV) + " " + Globals.FilePathAEV;
            toolStripMenuItemDiretory_DSE.Text = Lang.GetText(eLang.DiretoryDSE) + " " + Globals.FilePathDSE;
            toolStripMenuItemDiretory_FSE.Text = Lang.GetText(eLang.DiretoryFSE) + " " + Globals.FilePathFSE;
            toolStripMenuItemDiretory_SAR.Text = Lang.GetText(eLang.DiretorySAR) + " " + Globals.FilePathSAR;
            toolStripMenuItemDiretory_EAR.Text = Lang.GetText(eLang.DiretoryEAR) + " " + Globals.FilePathEAR;
            toolStripMenuItemDiretory_EMI.Text = Lang.GetText(eLang.DiretoryEMI) + " " + Globals.FilePathEMI;
            toolStripMenuItemDiretory_ESE.Text = Lang.GetText(eLang.DiretoryESE) + " " + Globals.FilePathESE;
            toolStripMenuItemDiretory_LIT.Text = Lang.GetText(eLang.DiretoryLIT) + " " + Globals.FilePathLIT;
            toolStripMenuItemDiretory_EFFBLOB.Text = Lang.GetText(eLang.DiretoryEFFBLOB) + " " + Globals.FilePathEFFBLOB;
        }

        #endregion

        #region Gerenciamento de arquivos //Save Convert

        private void toolStripMenuItemSaveConverter_DropDownOpening(object sender, EventArgs e)
        {
            toolStripMenuItemSaveConverterETS.Enabled = DataBase.FileETS != null;
            toolStripMenuItemSaveConverterITA.Enabled = DataBase.FileITA != null;
            toolStripMenuItemSaveConverterAEV.Enabled = DataBase.FileAEV != null;

            if (DataBase.FileETS != null && DataBase.FileETS.GetRe4Version == Re4Version.Classic)
            {
                toolStripMenuItemSaveConverterETS.Text = Lang.GetText(eLang.toolStripMenuItemSaveConverterETS_UHD);
            }
            else if (DataBase.FileETS != null && DataBase.FileETS.GetRe4Version == Re4Version.UHD)
            {
                toolStripMenuItemSaveConverterETS.Text = Lang.GetText(eLang.toolStripMenuItemSaveConverterETS_Classic);
            }
            else
            {
                toolStripMenuItemSaveConverterETS.Text = Lang.GetText(eLang.toolStripMenuItemSaveConverterETS);
            }

            if (DataBase.FileITA != null && DataBase.FileITA.GetRe4Version == Re4Version.Classic)
            {
                toolStripMenuItemSaveConverterITA.Text = Lang.GetText(eLang.toolStripMenuItemSaveConverterITA_UHD);
            }
            else if (DataBase.FileITA != null && DataBase.FileITA.GetRe4Version == Re4Version.UHD)
            {
                toolStripMenuItemSaveConverterITA.Text = Lang.GetText(eLang.toolStripMenuItemSaveConverterITA_Classic);
            }
            else
            {
                toolStripMenuItemSaveConverterITA.Text = Lang.GetText(eLang.toolStripMenuItemSaveConverterITA);
            }

            if (DataBase.FileAEV != null && DataBase.FileAEV.GetRe4Version == Re4Version.Classic)
            {
                toolStripMenuItemSaveConverterAEV.Text = Lang.GetText(eLang.toolStripMenuItemSaveConverterAEV_UHD);
            }
            else if (DataBase.FileAEV != null && DataBase.FileAEV.GetRe4Version == Re4Version.UHD)
            {
                toolStripMenuItemSaveConverterAEV.Text = Lang.GetText(eLang.toolStripMenuItemSaveConverterAEV_Classic);
            }
            else
            {
                toolStripMenuItemSaveConverterAEV.Text = Lang.GetText(eLang.toolStripMenuItemSaveConverterAEV);
            }
        }

        private void toolStripMenuItemSaveConverterETS_Click(object sender, EventArgs e)
        {
            saveFileDialogConvertETS.ShowDialog();
        }

        private void toolStripMenuItemSaveConverterITA_Click(object sender, EventArgs e)
        {
            saveFileDialogConvertITA.ShowDialog();
        }

        private void toolStripMenuItemSaveConverterAEV_Click(object sender, EventArgs e)
        {
            saveFileDialogConvertAEV.ShowDialog();
        }

        private void saveFileDialogConvertETS_FileOk(object sender, CancelEventArgs e)
        {
            FileInfo file = null;
            FileStream stream = null;
            try
            {
                file = new FileInfo(saveFileDialogConvertETS.FileName);
                stream = file.Create();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                e.Cancel = true;
                return;
            }

            if (file != null && stream != null)
            {
                FileManager.SaveConvertFileETS(stream);
                stream.Close();
            }
        }

        private void saveFileDialogConvertITA_FileOk(object sender, CancelEventArgs e)
        {
            FileInfo file = null;
            FileStream stream = null;
            try
            {
                file = new FileInfo(saveFileDialogConvertITA.FileName);
                stream = file.Create();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                e.Cancel = true;
                return;
            }

            if (file != null && stream != null)
            {
                FileManager.SaveConvertFileITA(stream);
                stream.Close();
            }
        }

        private void saveFileDialogConvertAEV_FileOk(object sender, CancelEventArgs e)
        {
            FileInfo file = null;
            FileStream stream = null;
            try
            {
                file = new FileInfo(saveFileDialogConvertAEV.FileName);
                stream = file.Create();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Lang.GetText(eLang.MessageBoxErrorTitle), MessageBoxButtons.OK);
                e.Cancel = true;
                return;
            }

            if (file != null && stream != null)
            {
                FileManager.SaveConvertFileAEV(stream);
                stream.Close();
            }
        }


        #endregion




        #region MainForm events/ metodos


        private void StartUpdateTranslation()
        {
            // menu principal
            toolStripMenuItemFile.Text = Lang.GetText(eLang.toolStripMenuItemFile);
            toolStripMenuItemEdit.Text = Lang.GetText(eLang.toolStripMenuItemEdit);
            toolStripMenuItemView.Text = Lang.GetText(eLang.toolStripMenuItemView);
            toolStripMenuItemMisc.Text = Lang.GetText(eLang.toolStripMenuItemMisc);
            toolStripMenuItemSelectRoom.Text = Lang.GetText(eLang.SelectRoom);
            //submenu File
            toolStripMenuItemNewFile.Text = Lang.GetText(eLang.toolStripMenuItemNewFile);
            toolStripMenuItemOpen.Text = Lang.GetText(eLang.toolStripMenuItemOpen);
            toolStripMenuItemSave.Text = Lang.GetText(eLang.toolStripMenuItemSave);
            toolStripMenuItemSaveAs.Text = Lang.GetText(eLang.toolStripMenuItemSaveAs);
            toolStripMenuItemSaveConverter.Text = Lang.GetText(eLang.toolStripMenuItemSaveConverter);
            toolStripMenuItemClear.Text = Lang.GetText(eLang.toolStripMenuItemClear);
            toolStripMenuItemClose.Text = Lang.GetText(eLang.toolStripMenuItemClose);
            // subsubmenu New
            toolStripMenuItemNewESL.Text = Lang.GetText(eLang.toolStripMenuItemNewESL);
            toolStripMenuItemNewETS_Classic.Text = Lang.GetText(eLang.toolStripMenuItemNewETS_Classic);
            toolStripMenuItemNewITA_Classic.Text = Lang.GetText(eLang.toolStripMenuItemNewITA_Classic);
            toolStripMenuItemNewAEV_Classic.Text = Lang.GetText(eLang.toolStripMenuItemNewAEV_Classic);
            toolStripMenuItemNewETS_UHD.Text = Lang.GetText(eLang.toolStripMenuItemNewETS_UHD);
            toolStripMenuItemNewITA_UHD.Text = Lang.GetText(eLang.toolStripMenuItemNewITA_UHD);
            toolStripMenuItemNewAEV_UHD.Text = Lang.GetText(eLang.toolStripMenuItemNewAEV_UHD);
            // subsubmenu Open
            toolStripMenuItemOpenESL.Text = Lang.GetText(eLang.toolStripMenuItemOpenESL);
            toolStripMenuItemOpenETS_Classic.Text = Lang.GetText(eLang.toolStripMenuItemOpenETS_Classic);
            toolStripMenuItemOpenITA_Classic.Text = Lang.GetText(eLang.toolStripMenuItemOpenITA_Classic);
            toolStripMenuItemOpenAEV_Classic.Text = Lang.GetText(eLang.toolStripMenuItemOpenAEV_Classic);
            toolStripMenuItemOpenETS_UHD.Text = Lang.GetText(eLang.toolStripMenuItemOpenETS_UHD);
            toolStripMenuItemOpenITA_UHD.Text = Lang.GetText(eLang.toolStripMenuItemOpenITA_UHD);
            toolStripMenuItemOpenAEV_UHD.Text = Lang.GetText(eLang.toolStripMenuItemOpenAEV_UHD);
            // subsubmenu Save
            toolStripMenuItemSaveESL.Text = Lang.GetText(eLang.toolStripMenuItemSaveESL);
            toolStripMenuItemSaveETS.Text = Lang.GetText(eLang.toolStripMenuItemSaveETS);
            toolStripMenuItemSaveITA.Text = Lang.GetText(eLang.toolStripMenuItemSaveITA);
            toolStripMenuItemSaveAEV.Text = Lang.GetText(eLang.toolStripMenuItemSaveAEV);
            toolStripMenuItemSaveDirectories.Text = Lang.GetText(eLang.toolStripMenuItemSaveDirectories);
            // subsubmenu Save As...
            toolStripMenuItemSaveAsESL.Text = Lang.GetText(eLang.toolStripMenuItemSaveAsESL);
            toolStripMenuItemSaveAsETS.Text = Lang.GetText(eLang.toolStripMenuItemSaveAsETS);
            toolStripMenuItemSaveAsITA.Text = Lang.GetText(eLang.toolStripMenuItemSaveAsITA);
            toolStripMenuItemSaveAsAEV.Text = Lang.GetText(eLang.toolStripMenuItemSaveAsAEV);
            // subsubmenu Save As (Convert)
            toolStripMenuItemSaveConverterETS.Text = Lang.GetText(eLang.toolStripMenuItemSaveConverterETS);
            toolStripMenuItemSaveConverterITA.Text = Lang.GetText(eLang.toolStripMenuItemSaveConverterITA);
            toolStripMenuItemSaveConverterAEV.Text = Lang.GetText(eLang.toolStripMenuItemSaveConverterAEV);
            // subsubmenu Clear
            toolStripMenuItemClearESL.Text = Lang.GetText(eLang.toolStripMenuItemClearESL);
            toolStripMenuItemClearETS.Text = Lang.GetText(eLang.toolStripMenuItemClearETS);
            toolStripMenuItemClearITA.Text = Lang.GetText(eLang.toolStripMenuItemClearITA);
            toolStripMenuItemClearAEV.Text = Lang.GetText(eLang.toolStripMenuItemClearAEV);

            // sub menu edit
            toolStripMenuItemAddNewObj.Text = Lang.GetText(eLang.toolStripMenuItemAddNewObj);
            toolStripMenuItemDeleteSelectedObj.Text = Lang.GetText(eLang.toolStripMenuItemDeleteSelectedObj);
            toolStripMenuItemMoveUp.Text = Lang.GetText(eLang.toolStripMenuItemMoveUp);
            toolStripMenuItemMoveDown.Text = Lang.GetText(eLang.toolStripMenuItemMoveDown);
            toolStripMenuItemSearch.Text = Lang.GetText(eLang.toolStripMenuItemSearch);

            // sub menu Misc
            toolStripMenuItemOptions.Text = Lang.GetText(eLang.toolStripMenuItemOptions);
            toolStripMenuItemCredits.Text = Lang.GetText(eLang.toolStripMenuItemCredits);

            // sub menu View
            toolStripMenuItemHideRoomModel.Text = Lang.GetText(eLang.toolStripMenuItemHideRoomModel);
            toolStripMenuItemHideEnemyESL.Text = Lang.GetText(eLang.toolStripMenuItemHideEnemyESL);
            toolStripMenuItemHideEtcmodelETS.Text = Lang.GetText(eLang.toolStripMenuItemHideEtcmodelETS);
            toolStripMenuItemHideItemsITA.Text = Lang.GetText(eLang.toolStripMenuItemHideItemsITA);
            toolStripMenuItemHideEventsAEV.Text = Lang.GetText(eLang.toolStripMenuItemHideEventsAEV);
            toolStripMenuItemSubMenuEnemy.Text = Lang.GetText(eLang.toolStripMenuItemSubMenuEnemy);
            toolStripMenuItemSubMenuItem.Text = Lang.GetText(eLang.toolStripMenuItemSubMenuItem);
            toolStripMenuItemSubMenuSpecial.Text = Lang.GetText(eLang.toolStripMenuItemSubMenuSpecial);
            toolStripMenuItemSubMenuEtcModel.Text = Lang.GetText(eLang.toolStripMenuItemSubMenuEtcModel);
            toolStripMenuItemNodeDisplayNameInHex.Text = Lang.GetText(eLang.toolStripMenuItemNodeDisplayNameInHex);
            toolStripMenuItemResetCamera.Text = Lang.GetText(eLang.toolStripMenuItemResetCamera);
            toolStripMenuItemRefresh.Text = Lang.GetText(eLang.toolStripMenuItemRefresh);

            // sub menus de view
            toolStripMenuItemHideDesabledEnemy.Text = Lang.GetText(eLang.toolStripMenuItemHideDesabledEnemy);
            toolStripMenuItemShowOnlyDefinedRoom.Text = Lang.GetText(eLang.toolStripMenuItemShowOnlyDefinedRoom);
            toolStripMenuItemAutoDefineRoom.Text = Lang.GetText(eLang.toolStripMenuItemAutoDefineRoom);
            toolStripMenuItemItemPositionAtAssociatedObjectLocation.Text = Lang.GetText(eLang.toolStripMenuItemItemPositionAtAssociatedObjectLocation);
            toolStripMenuItemHideItemTriggerZone.Text = Lang.GetText(eLang.toolStripMenuItemHideItemTriggerZone);
            toolStripMenuItemHideItemTriggerRadius.Text = Lang.GetText(eLang.toolStripMenuItemHideItemTriggerRadius);
            toolStripMenuItemHideSpecialTriggerZone.Text = Lang.GetText(eLang.toolStripMenuItemHideSpecialTriggerZone);
            toolStripMenuItemHideExtraObjs.Text = Lang.GetText(eLang.toolStripMenuItemHideExtraObjs);
            toolStripMenuItemHideOnlyWarpDoor.Text = Lang.GetText(eLang.toolStripMenuItemHideOnlyWarpDoor);
            toolStripMenuItemHideExtraExceptWarpDoor.Text = Lang.GetText(eLang.toolStripMenuItemHideExtraExceptWarpDoor);
            toolStripMenuItemUseMoreSpecialColors.Text = Lang.GetText(eLang.toolStripMenuItemUseMoreSpecialColors);
            toolStripMenuItemEtcModelUseScale.Text = Lang.GetText(eLang.toolStripMenuItemEtcModelUseScale);

            //save and open windows
            openFileDialogAEV.Title = Lang.GetText(eLang.openFileDialogAEV);
            openFileDialogESL.Title = Lang.GetText(eLang.openFileDialogESL);
            openFileDialogETS.Title = Lang.GetText(eLang.openFileDialogETS);
            openFileDialogITA.Title = Lang.GetText(eLang.openFileDialogITA);
            saveFileDialogAEV.Title = Lang.GetText(eLang.saveFileDialogAEV);
            saveFileDialogConvertAEV.Title = Lang.GetText(eLang.saveFileDialogConvertAEV);
            saveFileDialogConvertETS.Title = Lang.GetText(eLang.saveFileDialogConvertETS);
            saveFileDialogConvertITA.Title = Lang.GetText(eLang.saveFileDialogConvertITA);
            saveFileDialogESL.Title = Lang.GetText(eLang.saveFileDialogESL);
            saveFileDialogETS.Title = Lang.GetText(eLang.saveFileDialogETS);
            saveFileDialogITA.Title = Lang.GetText(eLang.saveFileDialogITA);

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            using (Re4QuadExtremeEditor.src.Forms.ExitConfirmationForm exitForm = new Re4QuadExtremeEditor.src.Forms.ExitConfirmationForm(Globals.IsProjectActive))
            {
                DialogResult dialogResult = exitForm.ShowDialog(this);

                if (dialogResult != DialogResult.OK)
                {
                    // Cancel (ou fechado via X da própria janela): não sai do programa.
                    e.Cancel = true;
                    return;
                }

                switch (exitForm.Result)
                {
                    case Re4QuadExtremeEditor.src.Class.Enums.ExitDialogResult.SaveProject:
                        SaveActiveProjectDirectly();
                        break;

                    case Re4QuadExtremeEditor.src.Class.Enums.ExitDialogResult.SaveAll:
                        try
                        {
                            toolStripMenuItemSaveAll_Click(this, EventArgs.Empty);
                        }
                        catch { /* mesmo se algo falhar ao salvar, o programa deve fechar normalmente */ }
                        break;

                    case Re4QuadExtremeEditor.src.Class.Enums.ExitDialogResult.QuitWithoutSaving:
                        // não faz nada, só fecha
                        break;
                }

                e.Cancel = false;
            }
        }


        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // entrada de teclas para açoes especiais
            cameraMove.isControlDown = e.Control;

            // Ctrl+Z -> Undo last position/rotation/scale change
            if (e.Control && !e.Shift && e.KeyCode == Keys.Z)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                if (UndoManager.Undo())
                {
                    if (camera.isOrbitCamera())
                    {
                        camera.UpdateCameraOrbitOnChangeValue();
                        UpdateCameraMatrix();
                    }
                    updateGL();
                    UpdatePropertyGrid();
                }
                return;
            }

            // Ctrl+Shift+S -> Save Project (checked first: Shift also has Control set)
            if (e.Control && e.Shift && e.KeyCode == Keys.S)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                toolStripMenuItemSaveProject_Click(this, EventArgs.Empty);
                return;
            }

            // Ctrl+S -> Save All
            if (e.Control && !e.Shift && e.KeyCode == Keys.S)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                toolStripMenuItemSaveAll_Click(this, EventArgs.Empty);
                return;
            }

            #region usado em propery
            // proibe a estrada de caracteres que não vão nos campos de numeros
            if (InPropertyGrid && propertyGridObjs.SelectedGridItem != null && propertyGridObjs.SelectedGridItem.PropertyDescriptor != null)
            {

                if (propertyGridObjs.SelectedGridItem.PropertyDescriptor.Attributes.Contains(new DecNumberAttribute()))
                {

                    e.SuppressKeyPress = true;
                    if (KeysCheck.KeyIsNum(e.KeyCode))
                    {
                        e.SuppressKeyPress = false;
                    }
                    if (e.Control)
                    {
                        e.SuppressKeyPress = false;
                    }
                    if (e.Alt || e.Shift || e.KeyCode == Keys.Alt)
                    {
                        e.SuppressKeyPress = true;
                    }
                    if (KeysCheck.KeyIsEssential(e.KeyCode))
                    {
                        e.SuppressKeyPress = false;
                    }

                }

                if (propertyGridObjs.SelectedGridItem.PropertyDescriptor.Attributes.Contains(new DecNegativeNumberAttribute()))
                {

                    e.SuppressKeyPress = true;
                    if (KeysCheck.KeyIsNum(e.KeyCode))
                    {
                        e.SuppressKeyPress = false;
                    }
                    if (KeysCheck.KeyIsMinus(e.KeyCode))
                    {
                        e.SuppressKeyPress = false;
                    }
                    if (e.Control)
                    {
                        e.SuppressKeyPress = false;
                    }
                    if (e.Alt || e.Shift || e.KeyCode == Keys.Alt)
                    {
                        e.SuppressKeyPress = true;
                    }
                    if (KeysCheck.KeyIsEssential(e.KeyCode))
                    {
                        e.SuppressKeyPress = false;
                    }

                }

                if (propertyGridObjs.SelectedGridItem.PropertyDescriptor.Attributes.Contains(new HexNumberAttribute()))
                {

                    e.SuppressKeyPress = true;
                    if (KeysCheck.KeyIsNum(e.KeyCode))
                    {
                        e.SuppressKeyPress = false;
                    }
                    if (e.Shift)
                    {
                        e.SuppressKeyPress = true;
                    }
                    if (KeysCheck.KeyIsHex(e.KeyCode))
                    {
                        e.SuppressKeyPress = false;
                    }
                    if (e.Control)
                    {
                        e.SuppressKeyPress = false;
                    }
                    if (e.Alt || e.KeyCode == Keys.Alt)
                    {
                        e.SuppressKeyPress = true;
                    }
                    if (KeysCheck.KeyIsEssential(e.KeyCode))
                    {
                        e.SuppressKeyPress = false;
                    }

                }

                if (propertyGridObjs.SelectedGridItem.PropertyDescriptor.Attributes.Contains(new FloatNumberAttribute()))
                {

                    e.SuppressKeyPress = true;
                    if (KeysCheck.KeyIsNum(e.KeyCode))
                    {
                        e.SuppressKeyPress = false;
                    }
                    if (KeysCheck.KeyIsMinus(e.KeyCode))
                    {
                        e.SuppressKeyPress = false;
                    }
                    if (KeysCheck.KeyIsCommaDot(e.KeyCode))
                    {
                        e.SuppressKeyPress = false;
                    }
                    if (KeysCheck.KeyIsOnlyDot(e.KeyValue))
                    {
                        e.SuppressKeyPress = false;
                    }
                    if (e.Control)
                    {
                        e.SuppressKeyPress = false;
                    }
                    if (e.Alt || e.Shift || e.KeyCode == Keys.Alt)
                    {
                        e.SuppressKeyPress = true;
                    }
                    if (KeysCheck.KeyIsEssential(e.KeyCode))
                    {
                        e.SuppressKeyPress = false;
                    }
                }

                if (propertyGridObjs.SelectedGridItem.PropertyDescriptor.Attributes.Contains(new NoKeyAttribute()))
                {
                    e.SuppressKeyPress = true;
                    if (KeysCheck.KeyIsEssentialNoKey(e.KeyCode))
                    {
                        e.SuppressKeyPress = false;
                    }
                }
            }

            #endregion
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            cameraMove.isControlDown = e.Control;
        }

        public void UpdateRenderFPS(int fps)
        {
            renderTimer.Interval = 1000 / fps;
        }

        private string GetLastDirectoryForType(string fileType)
        {
            switch (fileType)
            {
                case "ESL": return Globals.LastDirectoryESL;
                case "ETS": return Globals.LastDirectoryETS;
                case "ITA": return Globals.LastDirectoryITA;
                case "AEV": return Globals.LastDirectoryAEV;
                case "DSE": return Globals.LastDirectoryDSE;
                case "FSE": return Globals.LastDirectoryFSE;
                case "SAR": return Globals.LastDirectorySAR;
                case "EAR": return Globals.LastDirectoryEAR;
                case "EMI": return Globals.LastDirectoryEMI;
                case "ESE": return Globals.LastDirectoryESE;
                case "LIT": return Globals.LastDirectoryLIT;
                case "EFFBLOB": return Globals.LastDirectoryEFFBLOB;
                default: return Globals.LastDirectory;
            }
        }

        private void SetLastDirectoryForType(string fileType, string directory)
        {
            switch (fileType)
            {
                case "ESL": Globals.LastDirectoryESL = directory; Globals.BackupConfigs.LastDirectoryESL = directory; break;
                case "ETS": Globals.LastDirectoryETS = directory; Globals.BackupConfigs.LastDirectoryETS = directory; break;
                case "ITA": Globals.LastDirectoryITA = directory; Globals.BackupConfigs.LastDirectoryITA = directory; break;
                case "AEV": Globals.LastDirectoryAEV = directory; Globals.BackupConfigs.LastDirectoryAEV = directory; break;
                case "DSE": Globals.LastDirectoryDSE = directory; Globals.BackupConfigs.LastDirectoryDSE = directory; break;
                case "FSE": Globals.LastDirectoryFSE = directory; Globals.BackupConfigs.LastDirectoryFSE = directory; break;
                case "SAR": Globals.LastDirectorySAR = directory; Globals.BackupConfigs.LastDirectorySAR = directory; break;
                case "EAR": Globals.LastDirectoryEAR = directory; Globals.BackupConfigs.LastDirectoryEAR = directory; break;
                case "EMI": Globals.LastDirectoryEMI = directory; Globals.BackupConfigs.LastDirectoryEMI = directory; break;
                case "ESE": Globals.LastDirectoryESE = directory; Globals.BackupConfigs.LastDirectoryESE = directory; break;
                case "LIT": Globals.LastDirectoryLIT = directory; Globals.BackupConfigs.LastDirectoryLIT = directory; break;
                case "EFFBLOB": Globals.LastDirectoryEFFBLOB = directory; Globals.BackupConfigs.LastDirectoryEFFBLOB = directory; break;
                default: Globals.LastDirectory = directory; Globals.BackupConfigs.LastDirectory = directory; break;
            }
        }

        private void SetInitialDirectory(System.Windows.Forms.FileDialog dialog, string fileType = null)
        {
            string dir = GetLastDirectoryForType(fileType);
            if (!string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir))
                dialog.InitialDirectory = dir;
        }

        private void SaveLastDirectory(string filePath, string fileType = null)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                string directory = System.IO.Path.GetDirectoryName(filePath);
                SetLastDirectoryForType(fileType, directory);
                try { Re4QuadExtremeEditor.src.JSON.ConfigsFile.writeConfigsFile(Consts.ConfigsFileDiretory, Globals.BackupConfigs); } catch { }
            }
        }

        #endregion

    }
}
