using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Re4QuadExtremeEditor.src.JSON;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PMD_API;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Re4QuadExtremeEditor.src.Class
{
    /// <summary>
    /// Classe que reprenta a estrutura 3d de uma room (não contem seus objetos)
    /// </summary>
    public class Room
    {
        public class MeshData
        {
            // etapa 1;
            // Layout matches NewAge's GLMeshPart.cs vertex buffer exactly: position(3), normal(3),
            // texcoord(2), color-rgba(4) = 12 floats/vertex. Xcar/PMD meshes fill color with
            // (1,1,1,1) and normal is whatever LoadFromXcar already computed; only SMD/UHD meshes
            // (RoomSmdLoader) ever populate a non-white color or a CPU-normalized normal.
            public float[] vertices;
            public uint[] indices;
            public string texturename;

            // Optional opacity/alpha map (SMD/UHD materials only; null for Xcar/PMD rooms).
            // When set, the fragment shader reads alpha from this texture instead of the
            // diffuse texture's own alpha channel, matching NewAge's RoomShaderFrag.frag.
            // This is how baked shadow/darkness meshes (which have an opaque diffuse but a
            // transparent opacity map) get discarded at render time instead of showing as
            // black patches.
            public string alphaTexturename;

            // SMX-driven face culling for this mesh (SMD/UHD only; SmxFaceCulling.FrontAndBack
            // for Xcar/PMD meshes, meaning "no culling", the same default NewAge uses whenever
            // RenderOnlyFrontFace is off - see Room.Render() for how this is actually applied).
            public SmxFaceCulling faceCulling = SmxFaceCulling.FrontAndBack;

            // etapa 2;
            public int vertexBufferObject; // vertices  VBO
            public int elementBufferObject; // indices  EBO
            public int vertexArrayObject; // VAO
        }

        // todas as meshs
        private List<MeshData> meshes;
        //todas as imagens
        // nome da imagem, conteudo
        private Dictionary<string, Bitmap> textures;
        // nome da imagem, id no GL
        private Dictionary<string, int> texturesGL;

        public RoomInfo GetRoomInfo { get; }

        /// <summary>
        /// Which source this room's model was loaded from (Xcar/Smd/SmdData/Smd0000).
        /// Saved into the .re4qrp project file so re-opening the project reloads the
        /// same source automatically, instead of always defaulting back to Xcar.
        /// </summary>
        public Enums.RoomModelLoadSource ModelSource { get; private set; }

        const float ExtraScale = 10f;

        /// <summary>
        /// Matches NewAge's RoomSelectedObj.RenderOnlyFrontFace exactly: a global toggle,
        /// defaulting to false (no face culling at all — CullFace(FrontAndBack) + Disable),
        /// which the UI can flip on to actually apply each mesh's SMX-derived FaceCulling value.
        /// This is NewAge's real default behavior, not a simplification: SMX's FaceCulling data
        /// is loaded either way, but only actually affects the draw when this is true.
        /// </summary>
        public static bool RenderOnlyFrontFace = false;

        /// <summary>
        /// Matches NewAge's RoomSelectedObj.RenderVertexColor: a global toggle for the
        /// EnableVertexColors shader uniform, which gates whether matColor/per-vertex color
        /// (RGBA baked into the BIN's Vertex_Color_Array) get multiplied into the final result at
        /// all. Defaults to true here (unlike RenderOnlyFrontFace's false default) since this
        /// editor has no exposed UI toggle for it yet, and leaving it on has no visible effect on
        /// rooms that don't actually carry vertex colors (they're always (1,1,1,1) - a no-op
        /// multiply) - so "on" is the safer default until a UI toggle is added.
        /// </summary>
        public static bool RenderVertexColor = true;

        public Room(RoomInfo roomInfo) : this(roomInfo, Enums.RoomModelLoadSource.Xcar)
        {
        }

        public Room(RoomInfo roomInfo, Enums.RoomModelLoadSource modelSource)
        {
            GetRoomInfo = roomInfo;
            ModelSource = modelSource;

            meshes = new List<MeshData>();
            textures = new Dictionary<string, Bitmap>();
            texturesGL = new Dictionary<string, int>();

            if (modelSource == Enums.RoomModelLoadSource.None)
            {
                // don't load any mesh/texture at all
            }
            else if (modelSource == Enums.RoomModelLoadSource.Smd
                || modelSource == Enums.RoomModelLoadSource.SmdData
                || modelSource == Enums.RoomModelLoadSource.Smd0000
                || modelSource == Enums.RoomModelLoadSource.SmdStageRaz0r)
            {
                LoadFromSmd(roomInfo, modelSource);
            }
            else
            {
                LoadFromXcar(roomInfo);
            }

            UploadToGL();
        }

        /// <summary>
        /// Loads meshes/textures from a .SMD file ("Load from SMD" / "Load from Smd (Data)" /
        /// "Load from SMD (0000)"), automatically pulling matching ImagePackHD textures.
        /// </summary>
        private void LoadFromSmd(RoomInfo roomInfo, Enums.RoomModelLoadSource modelSource)
        {
            string smdPath;
            if (modelSource == Enums.RoomModelLoadSource.SmdData)
            {
                smdPath = RoomModelPaths.ResolveSmdDataPath(Globals.DirectoryUHDRE4, roomInfo.RoomKey);
            }
            else if (modelSource == Enums.RoomModelLoadSource.Smd0000)
            {
                smdPath = RoomModelPaths.ResolveSmd0000Path(Globals.DirectoryUHDRE4, roomInfo.RoomKey);
            }
            else if (modelSource == Enums.RoomModelLoadSource.SmdStageRaz0r)
            {
                smdPath = RoomModelPaths.ResolveSmdStageRaz0rPath(Globals.DirectoryUHDRE4, roomInfo.RoomKey);
            }
            else
            {
                smdPath = RoomModelPaths.ResolveSmdPath(Globals.DirectoryUHDRE4, roomInfo.RoomKey);
            }

            string packPath = RoomModelPaths.ResolveImagePackHDPath(Globals.DirectoryUHDRE4, roomInfo.RoomKey);
            string smxPath = RoomModelPaths.ResolveSmxPath(Globals.DirectoryUHDRE4, roomInfo.RoomKey, modelSource);

            if (string.IsNullOrEmpty(smdPath))
            {
                string notFoundMsg = RoomModelPaths.BuildSmdNotFoundMessage(Globals.DirectoryUHDRE4, roomInfo.RoomKey, modelSource);
                MessageBox.Show(
                    notFoundMsg,
                    Re4QuadExtremeEditor.src.Lang.GetText(Enums.eLang.MessageBoxSmdNotFoundTitle),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            List<MeshData> smdMeshes;
            Dictionary<string, Bitmap> smdTextures;

            try
            {
                // smxPath may be null/missing — RoomSmdLoader.Load treats that as "no SMX" and
                // falls back to neutral per-mesh defaults (see its class-level comment).
                RoomSmdLoader.Load(smdPath, packPath, smxPath, out smdMeshes, out smdTextures);
            }
            catch (Exception)
            {
                return;
            }

            meshes.AddRange(smdMeshes);
            foreach (var kv in smdTextures)
            {
                if (!textures.ContainsKey(kv.Key))
                {
                    textures.Add(kv.Key, kv.Value);
                }
            }
        }

        /// <summary>
        /// Loads meshes/textures from Xcar/PMD room files (the editor's original/default pipeline).
        /// </summary>
        private void LoadFromXcar(RoomInfo roomInfo)
        {

            for (int iJson = 0; iJson < roomInfo.RoomJsonFiles.Length; iJson++)
            {
                // obtem o caminho completo
                string JsonPath = Consts.RoomJsonFilesDiretory + roomInfo.RoomJsonFiles[iJson];

                //verifica se ele existe
                if (File.Exists(JsonPath))
                {
                    Dictionary<string, PMD> pmds = new Dictionary<string, PMD>();

                    // obtem o RoomJson class
                    RoomJson rj = new RoomJson(null);
                    try { rj = RoomJsonFile.parseRoomJson(JsonPath); } catch (Exception) { }

                    // obtem o caminho completo do diretorio dos arquivos PMD
                    string PmdDiretory = Globals.xscrDiretory + rj.RoomFolder;

                    // verifica se o diretorio dos arquivos PMD existe
                    if (Directory.Exists(PmdDiretory))
                    {
                        // lista de pmds
                        for (int ipmd = 0; ipmd < rj.PmdList.Count; ipmd++)
                        {
                            string PmdPath = PmdDiretory + rj.PmdList[ipmd];

                            if (File.Exists(PmdPath))
                            {
                                try
                                {
                                    pmds.Add(rj.PmdList[ipmd], PmdDecoder.GetPMD(PmdPath));
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }

                        var PmdKeys = pmds.Keys.ToArray();

                        // parte das texturas
                        HashSet<string> TGAs = new HashSet<string>();
                        for (int i = 0; i < PmdKeys.Length; i++)
                        {
                            for (int it = 0; it < pmds[PmdKeys[i]].TextureNames.Length; it++)
                            {
                                if (!rj.BlackListTextures.Contains(pmds[PmdKeys[i]].TextureNames[it]))
                                {
                                    TGAs.Add(pmds[PmdKeys[i]].TextureNames[it]);
                                }
                            }
                        }

                        var TGAnames = TGAs.ToArray();
                        for (int i = 0; i < TGAnames.Length; i++)
                        {
                            string TgaPath = PmdDiretory + TGAnames[i];
                            if (File.Exists(TgaPath))
                            {
                                try
                                {
                                    TGASharpLib.TGA nTGA = new TGASharpLib.TGA(TgaPath);
                                    if (!textures.ContainsKey(TGAnames[i]))
                                    {
                                        textures.Add(TGAnames[i], nTGA.ToBitmap());
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }

                        // convert

                        for (int ipmd = 0; ipmd < PmdKeys.Length; ipmd++)
                        {
                            string Pmdname = PmdKeys[ipmd];

                            FixPmd fix = new FixPmd(new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1, 1, 1));
                            var rx = Matrix4.CreateRotationX(fix.Rotation.X);
                            var ry = Matrix4.CreateRotationY(fix.Rotation.Y);
                            var rz = Matrix4.CreateRotationZ(fix.Rotation.Z);

                            if (rj.FixPmds.ContainsKey(Pmdname))
                            {
                                fix = rj.FixPmds[Pmdname];
                                rx = Matrix4.CreateRotationX(fix.Rotation.X);
                                ry = Matrix4.CreateRotationY(fix.Rotation.Y);
                                rz = Matrix4.CreateRotationZ(fix.Rotation.Z);
                            }

                            for (int iN = 0; iN < pmds[Pmdname].Nodes.Length; iN++)
                            {
                                for (int iM = 0; iM < pmds[Pmdname].Nodes[iN].Meshs.Length; iM++)
                                {
                                    float[] vertices = new float[pmds[Pmdname].Nodes[iN].Meshs[iM].Vertexs.Length * 12];
                                    uint[] indices = pmds[Pmdname].Nodes[iN].Meshs[iM].Orders;

                                    int vOffset = 0;
                                    for (int iv = 0; iv < pmds[Pmdname].Nodes[iN].Meshs[iM].Vertexs.Length; iv++)
                                    {

                                        var point = new Vector4(pmds[Pmdname].Nodes[iN].Meshs[iM].Vertexs[iv].x, pmds[Pmdname].Nodes[iN].Meshs[iM].Vertexs[iv].y, pmds[Pmdname].Nodes[iN].Meshs[iM].Vertexs[iv].z, 1);
                                        point = point * rx * ry * rz;

                                        int iS = pmds[Pmdname].Nodes[iN].SkeletonIndex;

                                        vertices[vOffset + 0] = (point.X
                                            * pmds[Pmdname].Skeleton[iS][0]
                                            * fix.Scale.X
                                            + pmds[Pmdname].Skeleton[iS][7]
                                            + fix.Position.X)
                                            * ExtraScale;
                                        vertices[vOffset + 1] = (point.Y
                                            * pmds[Pmdname].Skeleton[iS][1]
                                            * fix.Scale.Y
                                            + pmds[Pmdname].Skeleton[iS][8]
                                            + fix.Position.Y)
                                            * ExtraScale;
                                        vertices[vOffset + 2] = (point.Z
                                            * pmds[Pmdname].Skeleton[iS][2]
                                            * fix.Scale.Z
                                            + pmds[Pmdname].Skeleton[iS][9]
                                            + fix.Position.Z)
                                            * ExtraScale;
                                        // normal (indices 3,4,5) left at (0,0,0): Xcar/PMD has no normal data, and a zero
                                        // normal makes the shader's NormalIsZero check fall back to unlit ambient-only
                                        // rendering, i.e. exactly how Xcar/PMD rooms already looked before lighting existed.
                                        vertices[vOffset + 6] = pmds[Pmdname].Nodes[iN].Meshs[iM].Vertexs[iv].tu;
                                        vertices[vOffset + 7] = pmds[Pmdname].Nodes[iN].Meshs[iM].Vertexs[iv].tv;
                                        // color (indices 8,9,10,11): always opaque white for Xcar/PMD (no vertex-color concept)
                                        vertices[vOffset + 8] = 1f;
                                        vertices[vOffset + 9] = 1f;
                                        vertices[vOffset + 10] = 1f;
                                        vertices[vOffset + 11] = 1f;
                                        vOffset += 12;
                                    }

                                    MeshData m = new MeshData();
                                    m.vertices = vertices;
                                    m.indices = indices;
                                    m.texturename = null;
                                    if (textures.ContainsKey(pmds[Pmdname].TextureNames[pmds[Pmdname].Nodes[iN].Meshs[iM].TextureIndex]))
                                    {
                                        m.texturename = pmds[Pmdname].TextureNames[pmds[Pmdname].Nodes[iN].Meshs[iM].TextureIndex];
                                    }
                                    if (rj.BlackListTextures.Contains(pmds[Pmdname].TextureNames[pmds[Pmdname].Nodes[iN].Meshs[iM].TextureIndex]) 
                                        || pmds[Pmdname].TextureNames[pmds[Pmdname].Nodes[iN].Meshs[iM].TextureIndex].Length == 0)
                                    {
                                        m.texturename = Consts.TransparentTextureName;
                                    }
                                    meshes.Add(m);
                                }

                            }
                        }

                    }

                }

            }

        }

        /// <summary>
        /// Uploads all currently-loaded meshes/textures to OpenGL. Shared by both the Xcar and
        /// SMD loading paths so GL buffer setup only lives in one place.
        /// </summary>
        private void UploadToGL()
        {
            // carrega para o GL
            var textureNames = textures.Keys.ToArray();
            for (int i = 0; i < textureNames.Length; i++)
            {
                texturesGL.Add(textureNames[i], Texture.GetTextureIdGL(textures[textureNames[i]]));
            }

            DataBase.ShaderRoom.Use();
            int vertexLocation = DataBase.ShaderRoom.GetAttribLocation("aPosition");
            int normalLocation = DataBase.ShaderRoom.GetAttribLocation("aNormal");
            int texCoordLocation = DataBase.ShaderRoom.GetAttribLocation("aTexCoord");
            int colorLocation = DataBase.ShaderRoom.GetAttribLocation("aColor");


            for (int i = 0; i < meshes.Count; i++)
            {
                if (!Globals.UseOldGL)
                {
                    meshes[i].vertexArrayObject = GL.GenVertexArray();
                    GL.BindVertexArray(meshes[i].vertexArrayObject);
                }
             
                meshes[i].vertexBufferObject = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, meshes[i].vertexBufferObject);
                GL.BufferData(BufferTarget.ArrayBuffer, meshes[i].vertices.Length * sizeof(float), meshes[i].vertices, BufferUsageHint.StaticDraw);

                meshes[i].elementBufferObject = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, meshes[i].elementBufferObject);
                GL.BufferData(BufferTarget.ElementArrayBuffer, meshes[i].indices.Length * sizeof(uint), meshes[i].indices, BufferUsageHint.StaticDraw);

                if (!Globals.UseOldGL)
                {
                    //aPosition
                    GL.EnableVertexAttribArray(vertexLocation);
                    GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 12 * sizeof(float), 0);

                    //aNormal (world-space normal, already rotated+normalized on the CPU; see RoomSmdLoader.cs)
                    if (normalLocation >= 0)
                    {
                        GL.EnableVertexAttribArray(normalLocation);
                        GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 12 * sizeof(float), 3 * sizeof(float));
                    }

                    //aTexCoord
                    GL.EnableVertexAttribArray(texCoordLocation);
                    GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 12 * sizeof(float), 6 * sizeof(float));

                    //aColor (per-vertex RGBA color; see RoomSmdLoader.cs class comment)
                    if (colorLocation >= 0)
                    {
                        GL.EnableVertexAttribArray(colorLocation);
                        GL.VertexAttribPointer(colorLocation, 4, VertexAttribPointerType.Float, false, 12 * sizeof(float), 8 * sizeof(float));
                    }
                }
            }

           //int finish = 0;

        }

        public void ClearGL()
        {
            
            for (int i = 0; i < meshes.Count; i++)
            {
                if (!Globals.UseOldGL)
                {
                    GL.BindVertexArray(meshes[i].vertexArrayObject);
                    GL.DeleteBuffer(meshes[i].vertexArrayObject);
                }

                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
                GL.DeleteBuffer(meshes[i].vertexBufferObject);
                GL.DeleteBuffer(meshes[i].elementBufferObject);

            }

            foreach (var item in texturesGL)
            {
                GL.DeleteBuffer(item.Value);
            }
            
            meshes.Clear();
            textures.Clear();
            texturesGL.Clear();
        }


        public void Render()
        {

            GL.Enable(EnableCap.AlphaTest);
            GL.AlphaFunc(AlphaFunction.Gequal, 0.5f);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            if (!Globals.UseOldGL)
            {
                // Smart lighting (ambient+diffuse from camera-as-light) disabled: it was making
                // rooms render too dark/undifferentiated. Setting this to 0 makes the fragment
                // shader always take its "ambient_result" branch (raw texture color * alpha,
                // no lighting math at all) - i.e. the flat/normal look the room had before the
                // lighting system was added. The lighting code itself is left in place (shader
                // unchanged) in case it needs revisiting later; this is the on/off switch for it.
                DataBase.ShaderRoom.SetInt("EnableNormals", 0);
                DataBase.ShaderRoom.SetInt("EnableVertexColors", RenderVertexColor ? 1 : 0);
                // matColor: confirmed against NewAge's own LoadUhdBinModel/LoadPs2BinModel — every
                // UHD BIN material's MatColor is hardcoded to Vector4.One (UHD BIN materials carry
                // no real per-material tint color at all, unlike .PMD materials which do). So
                // (1,1,1,1) here isn't a placeholder for a missing feature — it's the same value
                // NewAge itself always uses for this room format.
                DataBase.ShaderRoom.SetVector4("matColor", new Vector4(1f, 1f, 1f, 1f));
            }

            GL.ActiveTexture(TextureUnit.Texture0);

            if (Globals.UseOldGL)
            {
                GL.EnableClientState(ArrayCap.VertexArray);
                GL.EnableClientState(ArrayCap.TextureCoordArray);
            }

            for (int i = 0; i < meshes.Count; i++)
            {
                if (!Globals.UseOldGL)
                {
                    GL.BindVertexArray(meshes[i].vertexArrayObject);
                }    

                // Matches NewAge's RoomSelectedObj.Render() exactly: face culling is only ever
                // actually applied when RenderOnlyFrontFace is on; otherwise every mesh draws
                // with culling disabled regardless of its SMX FaceCulling value.
                if (RenderOnlyFrontFace)
                {
                    switch (meshes[i].faceCulling)
                    {
                        case SmxFaceCulling.OnlyFront:
                            GL.Enable(EnableCap.CullFace);
                            GL.CullFace(CullFaceMode.Back);
                            break;
                        case SmxFaceCulling.OnlyBack:
                            GL.Enable(EnableCap.CullFace);
                            GL.CullFace(CullFaceMode.Front);
                            break;
                        default: // FrontAndBack == no culling
                            GL.CullFace(CullFaceMode.FrontAndBack);
                            GL.Disable(EnableCap.CullFace);
                            break;
                    }
                }
                else
                {
                    GL.CullFace(CullFaceMode.FrontAndBack);
                    GL.Disable(EnableCap.CullFace);
                }

                if (meshes[i].texturename != null && texturesGL.ContainsKey(meshes[i].texturename))
                {
                    GL.BindTexture(TextureTarget.Texture2D, texturesGL[meshes[i].texturename]);
                }
                else if (meshes[i].texturename == Consts.TransparentTextureName)
                {
                    GL.BindTexture(TextureTarget.Texture2D, DataBase.TransparentTextureIdGL);
                }
                else
                {
                    GL.BindTexture(TextureTarget.Texture2D, DataBase.NoTextureIdGL);
                }

                if (!Globals.UseOldGL)
                {
                    // Opacity/alpha map (SMD/UHD rooms only). Every mesh must (re)bind texture
                    // unit 1 every draw call — otherwise it silently keeps whatever the PREVIOUS
                    // mesh bound there (GL state leaks across draw calls). Meshes with a real
                    // opacity map bind it; everything else (including all Xcar/PMD meshes, which
                    // never set alphaTexturename) falls back to opaque white, i.e. alpha = 1.0,
                    // so they render exactly as before.
                    GL.ActiveTexture(TextureUnit.Texture1);
                    bool hasRealAlphaMap = meshes[i].alphaTexturename != null && texturesGL.ContainsKey(meshes[i].alphaTexturename);
                    if (hasRealAlphaMap)
                    {
                        GL.BindTexture(TextureTarget.Texture2D, texturesGL[meshes[i].alphaTexturename]);
                    }
                    else
                    {
                        GL.BindTexture(TextureTarget.Texture2D, DataBase.WhiteTextureIdGL);
                    }
                    GL.ActiveTexture(TextureUnit.Texture0);

                    // EnableAlphaChannel is per-mesh, not room-wide: only SMD/UHD materials with a
                    // real opacity_map should override alpha with texture1's value. Xcar/PMD
                    // meshes (and any SMD material with no opacity map) must keep reading alpha
                    // straight from the diffuse texture's own alpha channel (texColor.a in the
                    // shader) — exactly like the original RoomShaderFrag.frag — so their leaf/
                    // cutout transparency edges render correctly instead of using the opaque-white
                    // texture1 fallback as if it were real alpha data.
                    DataBase.ShaderRoom.SetInt("EnableAlphaChannel", hasRealAlphaMap ? 1 : 0);
                }

                if (Globals.UseOldGL)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, meshes[i].vertexBufferObject);
                    GL.VertexPointer(3, VertexPointerType.Float, 12 * sizeof(float), 0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, meshes[i].vertexBufferObject);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, 12 * sizeof(float), 6 * sizeof(float));
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, meshes[i].elementBufferObject);
                }

                GL.DrawElements(PrimitiveType.Triangles, meshes[i].indices.Length, DrawElementsType.UnsignedInt, 0);
            }

            if (Globals.UseOldGL)
            {
                GL.DisableClientState(ArrayCap.VertexArray);
                GL.DisableClientState(ArrayCap.TextureCoordArray);
            }

            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.Blend);

        }

        public void Render_Solid()
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            for (int i = 0; i < meshes.Count; i++)
            {
                if (!Globals.UseOldGL)
                {
                    GL.BindVertexArray(meshes[i].vertexArrayObject);
                }
                GL.BindTexture(TextureTarget.Texture2D, DataBase.SolidTextureIdGL);

                if (Globals.UseOldGL)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, meshes[i].vertexBufferObject);
                    GL.VertexPointer(3, VertexPointerType.Float, 12 * sizeof(float), 0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, meshes[i].vertexBufferObject);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, 12 * sizeof(float), 6 * sizeof(float));
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, meshes[i].elementBufferObject);
                }

                GL.DrawElements(PrimitiveType.Triangles, meshes[i].indices.Length, DrawElementsType.UnsignedInt, 0);
            }

        }


        #region DropToGround

        /// <summary>
        ///  retorna o novo Y (coloca o objeto rente ao chão mais proximo)
        /// </summary>
        /// <param name="pos">posição do objeto</param>
        /// <returns></returns>
        public float DropToGround(Vector3 pos)
        {
            List<float> found = new List<float>();

            for (int im = 0; im < meshes.Count; im++)
            {
                for (int j = 0; j < meshes[im].indices.Length; j += 3)
                {
                    tempTriangle temp;
                    uint index1 = 12 * meshes[im].indices[j];
                    uint index2 = 12 * meshes[im].indices[j + 1];
                    uint index3 = 12 * meshes[im].indices[j + 2];
                    int numVertices = meshes[im].vertices.Length;
                    if (index1 >= numVertices || index2 >= numVertices || index3 >= numVertices)
                    { continue; }
                    temp.a = new Vector3(meshes[im].vertices[index1] * 100f, meshes[im].vertices[index1 + 1] * 100f, meshes[im].vertices[index1 + 2] * 100f);
                    temp.b = new Vector3(meshes[im].vertices[index2] * 100f, meshes[im].vertices[index2 + 1] * 100f, meshes[im].vertices[index2 + 2] * 100f);
                    temp.c = new Vector3(meshes[im].vertices[index3] * 100f, meshes[im].vertices[index3 + 1] * 100f, meshes[im].vertices[index3 + 2] * 100f);
                    if (PointInTriangle(pos.Xz, temp.a.Xz, temp.b.Xz, temp.c.Xz))
                    {
                        found.Add(barryCentric(temp.a, temp.b, temp.c, pos));
                    }

                }
            }


            if (found.Count == 0)
               {return pos.Y; }

            int closest_index = 0;
            float closest_abs = 9999999.0f;
            // Console.WriteLine("Found " + found.Count + " triangles under position");
            for (int i = 0; i < found.Count; i++)
            {
                float abs = Math.Abs(pos.Y - found[i]);
                if (abs < closest_abs)
                {
                    closest_abs = abs;
                    closest_index = i;
                }
            }
            return found[closest_index];
        }

        private struct tempTriangle
        {
            public Vector3 a, b, c;
        }

        private static bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            var s = p0.Y * p2.X - p0.X * p2.Y + (p2.Y - p0.Y) * p.X + (p0.X - p2.X) * p.Y;
            var t = p0.X * p1.Y - p0.Y * p1.X + (p0.Y - p1.Y) * p.X + (p1.X - p0.X) * p.Y;

            if ((s < 0) != (t < 0))
                return false;

            var A = -p1.Y * p2.X + p0.Y * (p2.X - p1.X) + p0.X * (p1.Y - p2.Y) + p1.X * p2.Y;
            if (A < 0.0)
            {
                s = -s;
                t = -t;
                A = -A;
            }
            return s > 0 && t > 0 && (s + t) <= A;
        }

        private static float barryCentric(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 pos)
        {
            float det = (p2.Z - p3.Z) * (p1.X - p3.X) + (p3.X - p2.X) * (p1.Z - p3.Z);
            float l1 = ((p2.Z - p3.Z) * (pos.X - p3.X) + (p3.X - p2.X) * (pos.Z - p3.Z)) / det;
            float l2 = ((p3.Z - p1.Z) * (pos.X - p3.X) + (p1.X - p3.X) * (pos.Z - p3.Z)) / det;
            float l3 = 1.0f - l1 - l2;
            return l1 * p1.Y + l2 * p2.Y + l3 * p3.Y;
        }

        #endregion
    }


}
