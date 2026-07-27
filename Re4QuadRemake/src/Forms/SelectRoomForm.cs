using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Re4QuadExtremeEditor.src.JSON;
using Re4QuadExtremeEditor.src.Class;
using Re4QuadExtremeEditor.src.Class.Enums;

namespace Re4QuadExtremeEditor.src.Forms
{
    public partial class SelectRoomForm : Form
    {
        /// <summary>
        /// evendo que acontece depois de clicar em load;
        /// </summary>
        public event EventHandler onLoadButtonClick;

        /// <summary>
        /// DLL/patch layout selected for auto-loading the room's NewAge files.
        /// </summary>
        public LoadRoomDllVersion SelectedDllVersion
        {
            get
            {
                if (comboBoxSelectVersion.SelectedIndex < 0) { return LoadRoomDllVersion.Qingsheng; }
                return (LoadRoomDllVersion)comboBoxSelectVersion.SelectedIndex;
            }
        }

        /// <summary>
        /// Which source/format the room's 3D model should be loaded from (Xcar, Smd, Smd (Data), ImagePackHD).
        /// Index 0 in the combo box is "None"; the enum values start at index 1, hence the -1 offset.
        /// </summary>
        public RoomModelLoadSource SelectedModelSource
        {
            get
            {
                if (comboBoxModelSource.SelectedIndex < 0) { return RoomModelLoadSource.Xcar; }
                if (comboBoxModelSource.SelectedIndex == 0) { return RoomModelLoadSource.None; }
                return (RoomModelLoadSource)(comboBoxModelSource.SelectedIndex - 1);
            }
        }

        /// <summary>
        /// The list of file extensions (no dot, e.g. "AEV", "ITA") the user checked in "Load Files".
        /// </summary>
        public List<string> SelectedFileTypesToLoad { get; private set; } = new List<string>();

        public SelectRoomForm()
        {
            InitializeComponent();

            KeyPreview = true;
            comboBoxRoomList.Items.Add(Lang.GetText(eLang.NoneRoom));
            comboBoxRoomList.SelectedIndex = 0;
            comboBoxRoomList.Items.AddRange(DataBase.RoomList.ToArray());
            if (DataBase.SelectedRoom != null)
            {
                if (comboBoxRoomList.Items.Contains(DataBase.SelectedRoom.GetRoomInfo))
                {
                    comboBoxRoomList.SelectedIndex = comboBoxRoomList.Items.IndexOf(DataBase.SelectedRoom.GetRoomInfo);
                }
            }

            comboBoxSelectVersion.Items.Clear();
            comboBoxSelectVersion.Items.Add("Qingsheng DLL (Take from Data)");
            comboBoxSelectVersion.Items.Add("Raz0r DLL (Take from STAGE\\RXXX)");
            comboBoxSelectVersion.Items.Add("Without Any DLL (Take from STX)");
            comboBoxSelectVersion.SelectedIndex = (int)Globals.LastSelectedDllVersion;

            comboBoxModelSource.Items.Clear();
            comboBoxModelSource.Items.Add("None [Don't load any model]");
            comboBoxModelSource.Items.Add("Load from Xscar [Normal if you don't edit Smd]");
            comboBoxModelSource.Items.Add("Load from Smd [UHD]");
            comboBoxModelSource.Items.Add("Load from SMD (data) [UHD]");
            comboBoxModelSource.Items.Add("Load from SMD (0000) [UHD]");
            comboBoxModelSource.Items.Add("Load from Smd (Stage) (Raz0r) [UHD]");
            // combo index 0 is "None" (enum value 5); Xcar/Smd/SmdData/Smd0000/SmdStageRaz0r
            // (enum 0-4) sit at combo index 1-5.
            comboBoxModelSource.SelectedIndex = Globals.LastSelectedModelSource == RoomModelLoadSource.None
                ? 0
                : (int)Globals.LastSelectedModelSource + 1;

            comboBoxRoomList.KeyPress += comboBoxRoomList_KeyPress;

            UpdateLoadFilesButtonText();

            if (Lang.LoadedTranslation)
            {
                StartUpdateTranslation();
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonLoadFiles_Click(object sender, EventArgs e)
        {
            contextMenuStripLoadFiles.Show(buttonLoadFiles, new Point(0, buttonLoadFiles.Height));
        }

        private void toolStripMenuItemLoadFileType_Click(object sender, EventArgs e)
        {
            // CheckOnClick already toggled Checked; just refresh the button label.
            UpdateLoadFilesButtonText();
        }

        private void contextMenuStripLoadFiles_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            // keep the checklist open when the user is just clicking items
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
            {
                e.Cancel = true;
            }
        }

        private void UpdateLoadFilesButtonText()
        {
            int count = contextMenuStripLoadFiles.Items.OfType<ToolStripMenuItem>().Count(i => i.Checked);
            buttonLoadFiles.Text = "Load Files (" + count + " selected)";
        }

        // accumulates keystrokes typed in quick succession for type-ahead search in comboBoxRoomList (e.g. typing "R108")
        private string _roomListSearchBuffer = "";
        private DateTime _roomListSearchLastKeyTime = DateTime.MinValue;
        private static readonly TimeSpan RoomListSearchResetDelay = TimeSpan.FromSeconds(1);

        private void comboBoxRoomList_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) { return; }

            DateTime now = DateTime.Now;
            if (now - _roomListSearchLastKeyTime > RoomListSearchResetDelay)
            {
                _roomListSearchBuffer = "";
            }
            _roomListSearchLastKeyTime = now;
            _roomListSearchBuffer += e.KeyChar;

            for (int i = 0; i < comboBoxRoomList.Items.Count; i++)
            {
                string itemText = comboBoxRoomList.Items[i].ToString();
                if (itemText.StartsWith(_roomListSearchBuffer, StringComparison.OrdinalIgnoreCase))
                {
                    comboBoxRoomList.SelectedIndex = i;
                    break;
                }
            }

            e.Handled = true;
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            buttonLoad.Enabled = false;
            buttonCancel.Enabled = false;

            Globals.LastSelectedDllVersion = SelectedDllVersion;
            Globals.LastSelectedModelSource = SelectedModelSource;

            SelectedFileTypesToLoad = contextMenuStripLoadFiles.Items.OfType<ToolStripMenuItem>()
                .Where(i => i.Checked)
                .Select(i => i.Tag as string ?? i.Text.Replace(" FILE", "").Trim())
                .ToList();

            // remove a antiga
            if (DataBase.SelectedRoom != null)
            {
                DataBase.SelectedRoom.ClearGL();
                DataBase.SelectedRoom = null;
                GC.Collect();
            }
            // cria uma nova
            if (comboBoxRoomList.SelectedItem is RoomInfo)
            {
                DataBase.SelectedRoom = new Room((RoomInfo)comboBoxRoomList.SelectedItem, SelectedModelSource);
                GC.Collect();
            }
            onLoadButtonClick?.Invoke(comboBoxRoomList.SelectedItem, new EventArgs());
            Close();
        }

        private void SelectRoomForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private void StartUpdateTranslation() 
        {
            this.Text = Lang.GetText(eLang.SelectRoomForm);
            labelInfo.Text = Lang.GetText(eLang.labelInfo);
            buttonLoad.Text = Lang.GetText(eLang.buttonLoad);
            buttonCancel.Text = Lang.GetText(eLang.buttonCancel2);
        }

    }
}
