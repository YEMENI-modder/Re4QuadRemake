using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Re4QuadExtremeEditor.src.Class;
using Re4QuadExtremeEditor.src.Class.TreeNodeObj;
using Re4QuadExtremeEditor.src.Class.Enums;
using OpenTK;
using System.Windows.Forms;
using System.Drawing;

namespace Re4QuadExtremeEditor.src.Class
{
 

    public static class MoveObj
    {
        public static float objSpeedMultiplier = 1.0f;

        public static bool LockMoveSquareVertical = false;
        public static bool LockMoveSquareHorisontal = false;

        public static bool MoveRelativeCamera = false;
        public static bool KeepOnGround = false;


        [Flags]
        public enum MoveDirection
        {
            Null = 0,
            X = 1,
            Y = 2,
            Z = 4
        }

        public struct ObjKey
        {
            public ushort ID { get; }
            public GroupType Group { get; }

            public ObjKey(ushort ID, GroupType Group) 
            {
                this.ID = ID;
                this.Group = Group;
            }

            public override bool Equals(object obj)
            {
                return (obj is ObjKey key && key.Group == Group && key.ID == ID);
            }
            public override int GetHashCode()
            {
                return (Group.ToString() +"-" + ID.ToString()).GetHashCode();
            }
        }

        public static Dictionary<ObjKey, Vector3[]> GetSavedPosition() 
        {
            Dictionary<ObjKey, Vector3[]> r = new Dictionary<ObjKey, Vector3[]>();
            foreach (var item in DataBase.SelectedNodes.Values)
            {
                if (item is Object3D obj && item.Parent is TreeNodeGroup)
                {
                    r.Add(new ObjKey(obj.ObjLineRef, obj.Group), obj.GetObjPostion_ToMove_General());
                }
            }
            return r;
        }

        public static Dictionary<ObjKey, Vector3[]> GetSavedRotationAngles() 
        {
            Dictionary<ObjKey, Vector3[]> r = new Dictionary<ObjKey, Vector3[]>();
            foreach (var item in DataBase.SelectedNodes.Values)
            {
                if (item is Object3D obj && item.Parent is TreeNodeGroup)
                {
                    r.Add(new ObjKey(obj.ObjLineRef, obj.Group), obj.GetObjRotarionAngles_ToMove());
                }
            }
            return r;
        }

        public static Dictionary<ObjKey, Vector3[]> GetSavedScales()
        {
            Dictionary<ObjKey, Vector3[]> r = new Dictionary<ObjKey, Vector3[]>();
            foreach (var item in DataBase.SelectedNodes.Values)
            {
                if (item is Object3D obj && item.Parent is TreeNodeGroup)
                {
                    r.Add(new ObjKey(obj.ObjLineRef, obj.Group), obj.GetObjScale_ToMove());
                }
            }
            return r;
        }

        public static void MoveObjPositionXYZ(Object3D obj, MouseEventArgs e, Point oldMouseXY, Vector3[] savedPos, Camera camera, MoveDirection moveDirection, bool invert)
        {
            if (savedPos != null && savedPos.Length >= 1)
            {
                int MousePosX = e.X - oldMouseXY.X;
                int MousePosY = oldMouseXY.Y - e.Y; //invertido
                if (invert)
                {
                    MousePosX = -MousePosX;
                    MousePosY = -MousePosY;
                }

                float sensitivity = 10f * objSpeedMultiplier;

                Vector3 pos = savedPos[0];

                if (moveDirection == MoveDirection.X)
                {
                    if (MoveRelativeCamera)
                    {
                        pos = savedPos[0] + (camera.MoveObjRight * (MousePosX * sensitivity));
                    }
                    else
                    {
                        pos.X = savedPos[0].X + (MousePosX * sensitivity);
                    }
                }
                else if (moveDirection == MoveDirection.Y)
                {
                    pos.Y = savedPos[0].Y + (MousePosY * sensitivity);
                }
                else if (moveDirection == MoveDirection.Z)
                {
                    if (MoveRelativeCamera)
                    {
                        pos = savedPos[0] + (camera.MoveObjFront * (MousePosY * sensitivity));
                    }
                    else
                    {
                        pos.Z = savedPos[0].Z - (MousePosY * sensitivity);
                    }
                }
                else if (moveDirection == (MoveDirection.X | MoveDirection.Y))
                {
                    if (MoveRelativeCamera)
                    {
                        pos = savedPos[0] + (camera.MoveObjRight * (MousePosX * sensitivity));
                        pos.Y = savedPos[0].Y + (MousePosY * sensitivity);
                    }
                    else
                    {
                        pos.X = savedPos[0].X + (MousePosX * sensitivity);
                        pos.Y = savedPos[0].Y + (MousePosY * sensitivity);
                    }
                }
                else if (moveDirection == (MoveDirection.X | MoveDirection.Z))
                {
                    if (MoveRelativeCamera)
                    {
                        pos = savedPos[0] + (camera.MoveObjRight * (MousePosX * sensitivity)) + (camera.MoveObjFront * (MousePosY * sensitivity));
                    }
                    else 
                    {
                        pos.X = savedPos[0].X + (MousePosX * sensitivity);
                        pos.Z = savedPos[0].Z - (MousePosY * sensitivity);
                    }
                }
                else if (moveDirection == (MoveDirection.Y | MoveDirection.Z))
                {
                    if (MoveRelativeCamera)
                    {
                        pos = savedPos[0] + (camera.MoveObjRight * (MousePosX * sensitivity));
                        pos.Y = savedPos[0].Y + (MousePosY * sensitivity);
                    }
                    else
                    {
                        pos.Y = savedPos[0].Y + (MousePosY * sensitivity);
                        pos.Z = savedPos[0].Z + (MousePosX * sensitivity);
                    }
                }

                Vector3[] PosArr = (Vector3[])savedPos.Clone();
                PosArr[0] = pos;

                if (obj.Parent is SpecialNodeGroup s && s.PropertyMethods.GetSpecialType(obj.ObjLineRef) != SpecialType.T03_Items)
                {
                    PosArr[1] = MoveZonePosition(savedPos[1], MousePosX, MousePosY, camera, moveDirection);
                    PosArr[2] = MoveZonePosition(savedPos[2], MousePosX, MousePosY, camera, moveDirection);
                    PosArr[3] = MoveZonePosition(savedPos[3], MousePosX, MousePosY, camera, moveDirection);
                    PosArr[4] = MoveZonePosition(savedPos[4], MousePosX, MousePosY, camera, moveDirection);

                    if (moveDirection.HasFlag(MoveDirection.Y))
                    {
                        Vector3 PosY = savedPos[5];
                        PosY.Y += sensitivity * MousePosY;
                        PosArr[5] = PosY;
                    }
                }

                if (KeepOnGround && !(moveDirection == MoveDirection.Y) && DataBase.SelectedRoom != null)
                {
                    PosArr[0].Y = DataBase.SelectedRoom.DropToGround(PosArr[0]);
                }

                obj.SetObjPostion_ToMove_General(PosArr);

            }

        }

        public static void MoveObjRotationAnglesXYZ(Object3D obj, MouseEventArgs e, Point oldMouseXY, Vector3[] savedPos, MoveDirection moveDirection, bool invert) 
        {
            if (savedPos != null && savedPos.Length >= 1)
            {
                int MousePosX = e.X - oldMouseXY.X;
                if (invert)
                {
                    MousePosX = -MousePosX;
                }

                float sensitivity = 0.005f * objSpeedMultiplier;

                Vector3 angle = savedPos[0];

                if (moveDirection == MoveDirection.X)
                {
                    angle.X += sensitivity * MousePosX;
                }
                else if (moveDirection == MoveDirection.Y)
                {
                    angle.Y += sensitivity * MousePosX;
                }
                else if (moveDirection == MoveDirection.Z)
                {
                    angle.Z += sensitivity * MousePosX;
                }

                Vector3[] Arr = (Vector3[])savedPos.Clone();
                Arr[0] = angle;
                obj.SetObjRotarionAngles_ToMove(Arr);
            }
        }

        public static void MoveObjScaleXYZ(Object3D obj, MouseEventArgs e, Point oldMouseXY, Vector3[] savedPos, MoveDirection moveDirection, bool invert)
        {
            if (savedPos != null && savedPos.Length >= 1)
            {
                int MousePosX = e.X - oldMouseXY.X;
                int MousePosY = oldMouseXY.Y - e.Y; //invertido
                if (invert)
                {
                    MousePosX = -MousePosX;
                    MousePosY = -MousePosY;
                }

                float sensitivity = 0.001f * objSpeedMultiplier;

                Vector3 scale = savedPos[0];

                if (moveDirection == MoveDirection.X)
                {
                    scale.X += sensitivity * MousePosX;
                }
                else if (moveDirection == MoveDirection.Y)
                {
                    scale.Y += sensitivity * MousePosX;
                }
                else if (moveDirection == MoveDirection.Z)
                {
                    scale.Z += sensitivity * MousePosX;
                }
                else if (moveDirection.HasFlag(MoveDirection.X | MoveDirection.Y | MoveDirection.Z))
                {
                    scale.X += sensitivity * MousePosY;
                    scale.Y += sensitivity * MousePosY;
                    scale.Z += sensitivity * MousePosY;
                }

                Vector3[] Arr = (Vector3[])savedPos.Clone();
                Arr[0] = scale;
                obj.SetObjScale_ToMove(Arr);

            }
        }

        // triggerZone
        public static void MoveTriggerZonePositionXZ(Object3D obj, MouseEventArgs e, Point oldMouseXY, Vector3[] savedPos, Camera camera, MoveDirection moveDirection, bool invert, MoveObjType MoveObjTypeSelected, SpecialZoneCategory category)
        {
            if (savedPos != null && savedPos.Length >= 5)
            {
                int MousePosX = e.X - oldMouseXY.X;
                int MousePosY = oldMouseXY.Y - e.Y; // invertido
                if (invert)
                {
                    MousePosX = -MousePosX;
                    MousePosY = -MousePosY;
                }

                Vector3[] PosArr = (Vector3[])savedPos.Clone();

                if (category == SpecialZoneCategory.Category01)
                {
                    if (MoveObjTypeSelected.HasFlag(MoveObjType.__AllPointsXZ))
                    {
                        PosArr[1] = MoveZonePosition(savedPos[1], MousePosX, MousePosY, camera, moveDirection);
                        PosArr[2] = MoveZonePosition(savedPos[2], MousePosX, MousePosY, camera, moveDirection);
                        PosArr[3] = MoveZonePosition(savedPos[3], MousePosX, MousePosY, camera, moveDirection);
                        PosArr[4] = MoveZonePosition(savedPos[4], MousePosX, MousePosY, camera, moveDirection);
                    }
                    else if (MoveObjTypeSelected.HasFlag(MoveObjType.__Point0XZ))
                    {
                        PosArr[1] = MoveZonePosition(savedPos[1], MousePosX, MousePosY, camera, moveDirection);
                    }
                    else if (MoveObjTypeSelected.HasFlag(MoveObjType.__Point1XZ))
                    {
                        PosArr[2] = MoveZonePosition(savedPos[2], MousePosX, MousePosY, camera, moveDirection);
                    }
                    else if (MoveObjTypeSelected.HasFlag(MoveObjType.__Point2XZ))
                    {
                        PosArr[3] = MoveZonePosition(savedPos[3], MousePosX, MousePosY, camera, moveDirection);
                    }
                    else if (MoveObjTypeSelected.HasFlag(MoveObjType.__Point3XZ))
                    {
                        PosArr[4] = MoveZonePosition(savedPos[4], MousePosX, MousePosY, camera, moveDirection);
                    }
                    else if (MoveObjTypeSelected.HasFlag(MoveObjType.__WallPoint01and12XZ))
                    {
                        //MoveZoneWall(ref PosArr[1], ref PosArr[2], MousePosX);
                        if (moveDirection.HasFlag(MoveDirection.X) && !moveDirection.HasFlag(MoveDirection.Z))
                        {
                            MoveZoneWall(ref PosArr[1], ref PosArr[2], MousePosX);
                        }
                        else if (moveDirection.HasFlag(MoveDirection.Z) && !moveDirection.HasFlag(MoveDirection.X))
                        {
                            MoveZoneWall(ref PosArr[2], ref PosArr[3], MousePosX);
                        }
                        else if (moveDirection == (MoveDirection.X | MoveDirection.Z))
                        {
                            MoveZoneWall(ref PosArr[1], ref PosArr[2], MousePosX);
                            MoveZoneWall(ref PosArr[2], ref PosArr[3], MousePosX);
                        }
                    }
                    else if (MoveObjTypeSelected.HasFlag(MoveObjType.__WallPoint12and23XZ))
                    {
                        //MoveZoneWall(ref PosArr[2], ref PosArr[3], MousePosX);
                        if (moveDirection.HasFlag(MoveDirection.X) && !moveDirection.HasFlag(MoveDirection.Z))
                        {
                            MoveZoneWall(ref PosArr[2], ref PosArr[3], MousePosX);
                        }
                        else if (moveDirection.HasFlag(MoveDirection.Z) && !moveDirection.HasFlag(MoveDirection.X))
                        {
                            MoveZoneWall(ref PosArr[3], ref PosArr[4], MousePosX);
                        }
                        else if (moveDirection == (MoveDirection.X | MoveDirection.Z))
                        {
                            MoveZoneWall(ref PosArr[2], ref PosArr[3], MousePosX);
                            MoveZoneWall(ref PosArr[3], ref PosArr[4], MousePosX);
                        }
                    }
                    else if (MoveObjTypeSelected.HasFlag(MoveObjType.__Wallpoint23and30XZ))
                    {
                        //MoveZoneWall(ref PosArr[3], ref PosArr[4], MousePosX);
                        if (moveDirection.HasFlag(MoveDirection.X) && !moveDirection.HasFlag(MoveDirection.Z))
                        {
                            MoveZoneWall(ref PosArr[3], ref PosArr[4], MousePosX);
                        }
                        else if (moveDirection.HasFlag(MoveDirection.Z) && !moveDirection.HasFlag(MoveDirection.X))
                        {
                            MoveZoneWall(ref PosArr[4], ref PosArr[1], MousePosX);
                        }
                        else if (moveDirection == (MoveDirection.X | MoveDirection.Z))
                        {
                            MoveZoneWall(ref PosArr[3], ref PosArr[4], MousePosX);
                            MoveZoneWall(ref PosArr[4], ref PosArr[1], MousePosX);
                        }
                    }
                    else if (MoveObjTypeSelected.HasFlag(MoveObjType.__WallPoint30and01XZ))
                    {
                        //MoveZoneWall(ref PosArr[4], ref PosArr[1], MousePosX);
                        if (moveDirection.HasFlag(MoveDirection.X) && !moveDirection.HasFlag(MoveDirection.Z))
                        {
                            MoveZoneWall(ref PosArr[4], ref PosArr[1], MousePosX);
                        }
                        else if (moveDirection.HasFlag(MoveDirection.Z) && !moveDirection.HasFlag(MoveDirection.X))
                        {
                            MoveZoneWall(ref PosArr[1], ref PosArr[2], MousePosX);
                        }
                        else if (moveDirection == (MoveDirection.X | MoveDirection.Z))
                        {
                            MoveZoneWall(ref PosArr[4], ref PosArr[1], MousePosX);
                            MoveZoneWall(ref PosArr[1], ref PosArr[2], MousePosX);
                        }
                    }

                    for (int i = 1; i <= 4; i++)
                    {
                        if (float.IsNaN(PosArr[i].X) || float.IsNaN(PosArr[i].Y))
                        {
                            PosArr[i] = savedPos[i];
                        }
                    }
                   
                }
                else if (category == SpecialZoneCategory.Category02)
                {
                   PosArr[1] = MoveZonePosition(savedPos[1], MousePosX, MousePosY, camera, moveDirection);
                }

                obj.SetObjPostion_ToMove_General(PosArr);
            }
        }

        private static Vector3 MoveZonePosition(Vector3 savedPos, int MousePosX, int MousePosY, Camera camera, MoveDirection moveDirection) 
        {
            Vector3 vec = savedPos;
            float sensitivity = 10f * objSpeedMultiplier;

            if (moveDirection == MoveDirection.X)
            {
                if (MoveRelativeCamera)
                {
                    vec = savedPos + (camera.MoveObjRight * (MousePosX * sensitivity));
                }
                else
                {
                    vec.X = savedPos.X + (MousePosX * sensitivity);
                }
            }
            else if (moveDirection == MoveDirection.Z)
            {
                if (MoveRelativeCamera)
                {
                    vec = savedPos + (camera.MoveObjFront * (MousePosY * sensitivity));
                }
                else
                {
                    vec.Z = savedPos.Z + (MousePosY * sensitivity);
                }
            }
            else if (moveDirection == (MoveDirection.X | MoveDirection.Z))
            {
                if (MoveRelativeCamera)
                {
                    vec = savedPos + (camera.MoveObjRight * (MousePosX * sensitivity)) + (camera.MoveObjFront * (MousePosY * sensitivity));
                }
                else
                {
                    vec.X = savedPos.X + (MousePosX * sensitivity);
                    vec.Z = savedPos.Z + (MousePosY * sensitivity);
                }
            }
            return vec;
        }

        private static void MoveZoneWall(ref Vector3 pointA, ref Vector3 pointB, int MousePos) 
        {
            float sensitivity = 10f * objSpeedMultiplier;
            Vector3 direction = Vector3.Normalize((pointA - pointB) / 100);
            float yaw = (float)Math.Atan2(direction.Z, direction.X);
            Vector3 side = Vector3.Zero;
            side.X = (float)Math.Cos(yaw);
            side.Z = (float)Math.Sin(yaw);
            side = Vector3.Normalize(side);
            Vector3 front  = Vector3.Normalize(Vector3.Cross(side, Vector3.UnitY));
            pointA += front * sensitivity * MousePos;
            pointB += front * sensitivity * MousePos;
        }

        public static void MoveTriggerZonePositionY(Object3D obj, MouseEventArgs e, Point oldMouseXY, Vector3[] savedPos, bool invert)
        {
            if (savedPos != null && savedPos.Length >= 6)
            {
                int MousePosY = oldMouseXY.Y - e.Y; // pode ser invertido
                if (invert)
                {
                    MousePosY = -MousePosY;
                }

                float sensitivity = 10f * objSpeedMultiplier;

                Vector3 PosY = savedPos[5];
                PosY.Y += sensitivity * MousePosY;

                Vector3[] Arr = (Vector3[])savedPos.Clone();
                Arr[5] = PosY;
                obj.SetObjPostion_ToMove_General(Arr);
            }
        }

        public static void MoveTriggerZoneHeight(Object3D obj, MouseEventArgs e, Point oldMouseXY, Vector3[] savedPos, bool invert)
        {
            if (savedPos != null && savedPos.Length >= 6)
            {
                int MousePosX = e.X - oldMouseXY.X;
                if (invert)
                {
                    MousePosX = -MousePosX;
                }

                float sensitivity = 5f * objSpeedMultiplier;

                Vector3 Height = savedPos[5];
                Height.Z += sensitivity * MousePosX;

                Vector3[] Arr = (Vector3[])savedPos.Clone();
                Arr[5] = Height;
                obj.SetObjPostion_ToMove_General(Arr);
            }
        }

        public static void MoveZoneRotate(Object3D obj, MouseEventArgs e, Point oldMouseXY, Vector3[] savedPos, bool invert) // triggerZone And AshleyZone
        {
            if (savedPos != null && savedPos.Length >= 7)
            {
                int MousePosX = e.X - oldMouseXY.X;
                if (invert)
                {
                    MousePosX = -MousePosX;
                }

                float sensitivity = 0.002f * objSpeedMultiplier;

                Vector3[] Arr = (Vector3[])savedPos.Clone();

                Matrix4 rotation = Matrix4.CreateRotationY(sensitivity * MousePosX);

                Arr[1] = (new Vector4(savedPos[1] - savedPos[6], 1) * rotation).Xyz + savedPos[6];
                Arr[2] = (new Vector4(savedPos[2] - savedPos[6], 1) * rotation).Xyz + savedPos[6];
                Arr[3] = (new Vector4(savedPos[3] - savedPos[6], 1) * rotation).Xyz + savedPos[6];
                Arr[4] = (new Vector4(savedPos[4] - savedPos[6], 1) * rotation).Xyz + savedPos[6];

                obj.SetObjPostion_ToMove_General(Arr);

            }
        }

        public static void MoveTriggerZoneScale(Object3D obj, MouseEventArgs e, Point oldMouseXY, Vector3[] savedPos, bool invert, SpecialZoneCategory category)
        {
            if (savedPos != null && savedPos.Length >= 6)
            {
                int MousePosX = e.X - oldMouseXY.X;
                if (invert)
                {
                    MousePosX = -MousePosX;
                }
                Vector3[] Arr = (Vector3[])savedPos.Clone();

                if (category == SpecialZoneCategory.Category01)
                {
                    //float sensitivity = 2f * Globals.objSpeedMultiplier;
                    //Arr[1].X = savedPos[1].X - (sensitivity * MousePosX);
                    //Arr[1].Z = savedPos[1].Z - (sensitivity * MousePosX);
                    //Arr[2].X = savedPos[2].X + (sensitivity * MousePosX);
                    //Arr[2].Z = savedPos[2].Z - (sensitivity * MousePosX);
                    //Arr[3].X = savedPos[3].X + (sensitivity * MousePosX);
                    //Arr[3].Z = savedPos[3].Z + (sensitivity * MousePosX);
                    //Arr[4].X = savedPos[4].X - (sensitivity * MousePosX);
                    //Arr[4].Z = savedPos[4].Z + (sensitivity * MousePosX);
                    MoveZoneWall(ref Arr[1], ref Arr[2], MousePosX);
                    MoveZoneWall(ref Arr[2], ref Arr[3], MousePosX);
                    MoveZoneWall(ref Arr[3], ref Arr[4], MousePosX);
                    MoveZoneWall(ref Arr[4], ref Arr[1], MousePosX);

                    for (int i = 1; i <= 4; i++)
                    {
                        if (float.IsNaN(Arr[i].X) || float.IsNaN(Arr[i].Z))
                        {
                            Arr[i] = savedPos[i];
                        }
                    }
                }
                else if (category == SpecialZoneCategory.Category02)
                {
                    float sensitivity = 5f * objSpeedMultiplier;
                    Vector3 Scale = savedPos[5];
                    Scale.X += sensitivity * MousePosX;
                    Arr[5] = Scale;
                }

                obj.SetObjPostion_ToMove_General(Arr);

            }

        }

        public static void MoveAshleyZoneScale(Object3D obj, MouseEventArgs e, Point oldMouseXY, Vector3[] savedPos, bool invert)
        {
            if (savedPos != null && savedPos.Length >= 5)
            {
                int MousePosX = e.X - oldMouseXY.X;
                if (invert)
                {
                    MousePosX = -MousePosX;
                }
                Vector3[] Arr = (Vector3[])savedPos.Clone();
                MoveZoneWall(ref Arr[1], ref Arr[2], MousePosX);
                MoveZoneWall(ref Arr[2], ref Arr[3], MousePosX);
                MoveZoneWall(ref Arr[3], ref Arr[4], MousePosX);
                MoveZoneWall(ref Arr[4], ref Arr[1], MousePosX);
                for (int i = 1; i <= 4; i++)
                {
                    if (float.IsNaN(Arr[i].X) || float.IsNaN(Arr[i].Z))
                    {
                        Arr[i] = savedPos[i];
                    }
                }
                obj.SetObjPostion_ToMove_General(Arr);
            }
        }

        /// <summary>
        /// Moves every currently selected object (of any type: ESL, ITA, AEV, EAR, SAR, EMI, ESE,
        /// DSE, FSE, LIT, EtcModel, Special, EFFBLOB...) so that its main position ([0] in the
        /// GetObjPostion_ToMove_General array) lands exactly on the camera's current position.
        ///
        /// IMPORTANT: GetObjPostion_ToMove_General and GetObjPosition_ToCamera use different unit
        /// scales *per file type* (e.g. ESL's ToMove_General values are the raw value * 10, while
        /// its ToCamera value is the raw value / 10 - other types use yet other factors, or none
        /// at all). camera.Position is only meaningful in the ToCamera scale (it's built from/
        /// compared against ToCamera values everywhere else in the renderer), so a plain subtraction
        /// against ToMove_General would be off by whatever that type's scale factor happens to be.
        /// To stay correct for every type without hardcoding a factor per file, each object's own
        /// per-axis ratio between its ToMove_General[0] and its ToCamera value is computed on the
        /// spot and used to convert the camera's position into that object's ToMove_General scale
        /// before the shift is applied. Every other point the object owns (trigger zone corners,
        /// height/radius helpers, etc.) is shifted by the same delta, preserving the object's
        /// shape/size - this mirrors how MoveObjPositionXYZ drags a whole trigger zone rather than
        /// collapsing it onto one point. Registers a single undo transaction for the whole operation.
        /// </summary>
        public static void MoveSelectedObjectsToCamera(Camera camera)
        {
            if (DataBase.SelectedNodes == null || DataBase.SelectedNodes.Count == 0 || camera == null)
            {
                return;
            }

            Vector3 cameraPos = camera.Position;

            UndoManager.BeginTransaction();

            foreach (var item in DataBase.SelectedNodes.Values)
            {
                if (!(item is Object3D obj) || !(item.Parent is TreeNodeGroup)) continue;

                Vector3[] currentPos;
                try { currentPos = obj.GetObjPostion_ToMove_General(); }
                catch (Exception) { continue; }

                if (currentPos == null || currentPos.Length == 0) continue;

                Vector3 cameraSpacePos;
                try { cameraSpacePos = obj.GetObjPosition_ToCamera(); }
                catch (Exception) { continue; }

                Vector3 delta;

                // Trigger-only objects (e.g. AEV with no attached item) always report
                // currentPos[0] == Vector3.Zero from GetObjPostion_ToMove_General, since [0]
                // is only populated for T03_Items. Deriving a per-axis scale factor from a
                // zero reference collapses to zero on every axis, producing zero delta and no
                // movement.
                //
                // GetObjPosition_ToCamera (cameraSpacePos) already computes the correct full
                // X/Y/Z reference point for every file type, trigger-only included - each type
                // implements it independently (e.g. FileSpecialGroup/ESAR/FSE all average the
                // zone corners for X/Z and the TrueY min/max for Y). Use it directly as the
                // reference instead of reconstructing one from GetObjPostion_ToMove_General's
                // pos[6], which only computes X/Z and leaves Y at zero.
                bool isTriggerOnly = currentPos.Length > 6 && currentPos[0] == Vector3.Zero;

                if (isTriggerOnly)
                {
                    // GetObjPosition_ToCamera for trigger-only types builds cameraSpacePos from
                    // raw-value/100f reads (see GetTriggerZone / GetTriggerZonePos_ToCamera in
                    // FileSpecialGroup/BaseTriggerZoneGroup - every corner, TrueY, MoreHeight,
                    // and CircleRadius read there is divided by 100f). camera.Position lives in
                    // that same /100 "render" space. GetObjPostion_ToMove_General's corners
                    // (pos[1-4]/[5]/[6]), by contrast, are the raw values with no division.
                    // So a render-space delta must be scaled back up by 100 before it's added to
                    // the raw corners, or the shift lands 100x too small relative to the box's
                    // own size - which is what "grows/distorts instead of moving" looks like when
                    // it's applied uniformly against numbers that are already 100x larger.
                    delta = (cameraPos - cameraSpacePos) * 100f;
                }
                else
                {
                    // per-axis scale factor between this object's own ToMove_General[0] and its
                    // ToCamera position (e.g. 10x for ESL, 1x for AEV/ITA...). Falls back to 1 (no
                    // conversion) on any axis where the ToCamera value is ~0, since a ratio can't be
                    // derived from a division by zero there; this only matters for objects that
                    // legitimately sit exactly on that axis's origin, an edge case not worth the risk
                    // of introducing a NaN/Infinity into the object's saved position.
                    float scaleX = SafeRatio(currentPos[0].X, cameraSpacePos.X);
                    float scaleY = SafeRatio(currentPos[0].Y, cameraSpacePos.Y);
                    float scaleZ = SafeRatio(currentPos[0].Z, cameraSpacePos.Z);

                    Vector3 cameraPosInObjScale = new Vector3(
                        cameraPos.X * scaleX,
                        cameraPos.Y * scaleY,
                        cameraPos.Z * scaleZ);

                    delta = cameraPosInObjScale - currentPos[0];
                }

                Vector3[] newPos = (Vector3[])currentPos.Clone();
                for (int i = 0; i < newPos.Length; i++)
                {
                    if (isTriggerOnly && i == 5)
                    {
                        // [5] packs (radius, TrueY, moreHeight) as scalars, not a spatial
                        // point. Radius and moreHeight must stay untouched; only TrueY (Y)
                        // shifts with the trigger's vertical movement.
                        newPos[5] = new Vector3(currentPos[5].X, currentPos[5].Y + delta.Y, currentPos[5].Z);
                        continue;
                    }

                    // index 5 (when present, non-trigger case) is not a spatial point: it
                    // packs TriggerZoneCircleRadius/TrueY/MoreHeight scalar values into a
                    // Vector3 for convenience, so it must be left untouched rather than
                    // shifted like a position.
                    if (i == 5) continue;

                    newPos[i] = currentPos[i] + delta;
                }

                try { obj.SetObjPostion_ToMove_General(newPos); } catch (Exception) { }
            }

            UndoManager.CommitTransaction();
        }

        /// <summary>
        /// Returns moveScaleValue / cameraScaleValue, i.e. the multiplier that converts a
        /// ToCamera-scale coordinate into ToMove_General scale for one axis. Returns 1 (treat as
        /// already-matching scales) if cameraScaleValue is too close to zero to divide by safely.
        /// </summary>
        private static float SafeRatio(float moveScaleValue, float cameraScaleValue)
        {
            const float epsilon = 0.0001f;
            if (Math.Abs(cameraScaleValue) < epsilon)
            {
                return 1f;
            }
            return moveScaleValue / cameraScaleValue;
        }

    }
}
