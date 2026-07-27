using System;
using System.IO;
using System.Linq;
using Re4QuadExtremeEditor.src.Class.Enums;

namespace Re4QuadExtremeEditor.src.Class
{
    /// <summary>
    /// Resolves on-disk paths for the alternate room model sources (SMD, SMD (Data), ImagePackHD),
    /// based on the RE4 UHD game folder layout described by the user.
    /// </summary>
    public static class RoomModelPaths
    {
        /// <summary>
        /// Extracts the room identifier after the leading "r"/"R" (keeps letters, e.g. "r11b" -> "11b", "r100" -> "100").
        /// Unlike Utils.ExtractRoomNumber, this keeps trailing letters since room folder/pack names can include them
        /// (e.g. R11B).
        /// </summary>
        private static string ExtractRoomKeyBody(string roomKey)
        {
            if (string.IsNullOrEmpty(roomKey)) { return null; }
            string trimmed = roomKey.Trim();
            if (trimmed.Length > 0 && (trimmed[0] == 'r' || trimmed[0] == 'R'))
            {
                trimmed = trimmed.Substring(1);
            }
            return trimmed.Length > 0 ? trimmed : null;
        }

        /// <summary>
        /// Case-insensitively finds a subdirectory of parentDir named exactly folderName, or null if not found.
        /// </summary>
        private static string FindDirectoryCaseInsensitive(string parentDir, string folderName)
        {
            if (!Directory.Exists(parentDir)) { return null; }
            try
            {
                return Directory.GetDirectories(parentDir)
                    .FirstOrDefault(d => string.Equals(Path.GetFileName(d), folderName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception) { return null; }
        }

        private static string FindFileCaseInsensitive(string parentDir, string fileName)
        {
            if (!Directory.Exists(parentDir)) { return null; }
            try
            {
                return Directory.GetFiles(parentDir)
                    .FirstOrDefault(f => string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// Debug helper: returns a human-readable trace of each step ResolveSmdPath takes,
        /// to diagnose why a path wasn't found (missing BIO4, missing STX/ST folder, wrong file name, etc.).
        /// </summary>
        public static string DebugTraceSmdPath(string gameDirectory, string roomKey)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string roomNumber = ExtractRoomKeyBody(roomKey);
            sb.AppendLine("roomKey: " + roomKey + " -> roomNumber: " + (roomNumber ?? "(null)"));

            if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory))
            {
                sb.AppendLine("gameDirectory invalid or missing: " + gameDirectory);
                return sb.ToString();
            }
            sb.AppendLine("gameDirectory OK: " + gameDirectory);

            string bio4 = FindDirectoryCaseInsensitive(gameDirectory, "BIO4");
            sb.AppendLine("BIO4 folder: " + (bio4 ?? "NOT FOUND"));
            if (bio4 == null) { return sb.ToString(); }

            string roomFolderName = "R" + roomNumber;
            string firstDigit = roomNumber[0].ToString();

            string stxDir = FindDirectoryCaseInsensitive(bio4, "ST" + firstDigit);
            sb.AppendLine("ST" + firstDigit + " folder: " + (stxDir ?? "NOT FOUND"));
            if (stxDir != null)
            {
                string roomDir = FindDirectoryCaseInsensitive(stxDir, roomFolderName);
                sb.AppendLine(roomFolderName + " folder inside ST" + firstDigit + ": " + (roomDir ?? "NOT FOUND"));
                if (roomDir != null)
                {
                    sb.AppendLine("Files inside: " + string.Join(", ", Directory.GetFiles(roomDir).Select(Path.GetFileName)));
                }
            }

            string stDir = FindDirectoryCaseInsensitive(bio4, "ST");
            sb.AppendLine("ST folder: " + (stDir ?? "NOT FOUND"));
            if (stDir != null)
            {
                string roomDir = FindDirectoryCaseInsensitive(stDir, roomFolderName);
                sb.AppendLine(roomFolderName + " folder inside ST: " + (roomDir ?? "NOT FOUND"));
                if (roomDir != null)
                {
                    sb.AppendLine("Files inside: " + string.Join(", ", Directory.GetFiles(roomDir).Select(Path.GetFileName)));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds a user-facing message describing exactly which folder was searched and which
        /// file name was expected for a given SMD load source, so "SMD not found" isn't a silent
        /// no-op. Does not throw; always returns a usable string even with a missing game directory.
        /// </summary>
        public static string BuildSmdNotFoundMessage(string gameDirectory, string roomKey, RoomModelLoadSource modelSource)
        {
            string roomNumber = ExtractRoomKeyBody(roomKey) ?? "?";
            string roomFolderName = "R" + roomNumber;
            string firstDigit = roomNumber.Length > 0 ? roomNumber[0].ToString() : "?";

            string expectedFileNames;
            string expectedFolder;

            if (modelSource == RoomModelLoadSource.SmdData)
            {
                expectedFileNames = "0000.SMD";
                expectedFolder = Path.Combine(gameDirectory ?? "(game folder not set)", "BIO4", "Data", roomFolderName);
            }
            else if (modelSource == RoomModelLoadSource.SmdStageRaz0r)
            {
                expectedFileNames = "0000.SMD";
                expectedFolder = Path.Combine(gameDirectory ?? "(game folder not set)", "FILES", "STAGE", roomFolderName);
            }
            else if (modelSource == RoomModelLoadSource.Smd0000)
            {
                expectedFileNames = "0000.SMD";
                expectedFolder = Path.Combine(gameDirectory ?? "(game folder not set)", "BIO4", "ST" + firstDigit, roomFolderName)
                    + "  (or ST" + Path.DirectorySeparatorChar + roomFolderName + " as fallback)";
            }
            else
            {
                expectedFileNames = roomFolderName + "_004.SMD  (or " + roomFolderName + "_04.SMD)";
                expectedFolder = Path.Combine(gameDirectory ?? "(game folder not set)", "BIO4", "ST" + firstDigit, roomFolderName);
            }

            return string.Format(
                Re4QuadExtremeEditor.src.Lang.GetText(Enums.eLang.MessageBoxSmdNotFoundBody),
                expectedFileNames,
                expectedFolder);
        }

        /// <summary>
        /// Resolves the .SMD path for "Load from SMD (0000)":
        /// same folder location as the regular "Load from SMD" (STX\RXXX, falling back to ST\RXXX),
        /// but the file itself is always named "0000.SMD" instead of "RXXX_004.SMD"/"RXXX_04.SMD".
        /// This is a more reliable variant when the RXXX_0*.SMD naming isn't found.
        /// </summary>
        public static string ResolveSmd0000Path(string gameDirectory, string roomKey)
        {
            string roomNumber = ExtractRoomKeyBody(roomKey);
            if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory) || roomNumber == null)
            {
                return null;
            }

            string bio4 = FindDirectoryCaseInsensitive(gameDirectory, "BIO4");
            if (bio4 == null) { return null; }

            string roomFolderName = "R" + roomNumber;
            string firstDigit = roomNumber[0].ToString();

            // primary: STX\RXXX\0000.SMD
            string stxDir = FindDirectoryCaseInsensitive(bio4, "ST" + firstDigit);
            if (stxDir != null)
            {
                string roomDir = FindDirectoryCaseInsensitive(stxDir, roomFolderName);
                if (roomDir != null)
                {
                    string file = FindFileCaseInsensitive(roomDir, "0000.SMD");
                    if (file != null) { return file; }
                }
            }

            // fallback: ST\RXXX\0000.SMD
            string stDir = FindDirectoryCaseInsensitive(bio4, "ST");
            if (stDir != null)
            {
                string roomDir = FindDirectoryCaseInsensitive(stDir, roomFolderName);
                if (roomDir != null)
                {
                    string file = FindFileCaseInsensitive(roomDir, "0000.SMD");
                    if (file != null) { return file; }
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves the .SMD path for "Load from SMD":
        /// [GAME]\BIO4\STX\RXXX\RXXX_004.SMD, falling back to [GAME]\BIO4\STX\RXXX\RXXX_04.SMD
        /// (X = first digit of the room number; same STX folder, different file name).
        /// </summary>
        public static string ResolveSmdPath(string gameDirectory, string roomKey)
        {
            string roomNumber = ExtractRoomKeyBody(roomKey);
            if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory) || roomNumber == null)
            {
                return null;
            }

            string bio4 = FindDirectoryCaseInsensitive(gameDirectory, "BIO4");
            if (bio4 == null) { return null; }

            string roomFolderName = "R" + roomNumber;
            string firstDigit = roomNumber[0].ToString();

            // primary: STX\RXXX\RXXX_004.SMD
            string stxDir = FindDirectoryCaseInsensitive(bio4, "ST" + firstDigit);
            if (stxDir != null)
            {
                string roomDir = FindDirectoryCaseInsensitive(stxDir, roomFolderName);
                if (roomDir != null)
                {
                    string file = FindFileCaseInsensitive(roomDir, roomFolderName + "_004.SMD");
                    if (file != null) { return file; }
                }
            }

            // fallback: STX\RXXX\RXXX_04.SMD (same STX folder, different file name)
            if (stxDir != null)
            {
                string roomDir = FindDirectoryCaseInsensitive(stxDir, roomFolderName);
                if (roomDir != null)
                {
                    string file = FindFileCaseInsensitive(roomDir, roomFolderName + "_04.SMD");
                    if (file != null) { return file; }
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves the .SMD path for "Load from Smd (Data)":
        /// [GAME]\BIO4\Data\RXXX\0000.SMD
        /// </summary>
        public static string ResolveSmdDataPath(string gameDirectory, string roomKey)
        {
            string roomNumber = ExtractRoomKeyBody(roomKey);
            if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory) || roomNumber == null)
            {
                return null;
            }

            string bio4 = FindDirectoryCaseInsensitive(gameDirectory, "BIO4");
            if (bio4 == null) { return null; }

            string data = FindDirectoryCaseInsensitive(bio4, "Data");
            if (data == null) { return null; }

            string roomDir = FindDirectoryCaseInsensitive(data, "R" + roomNumber);
            if (roomDir == null) { return null; }

            return FindFileCaseInsensitive(roomDir, "0000.SMD");
        }

        /// <summary>
        /// Resolves the .SMD path for "Load from Smd (Stage) (Raz0r)":
        /// [GAME]\FILES\STAGE\Rxxx\0000.SMD
        /// This is the Raz0r DLL layout's stage folder. Unlike every other SMD source, there is
        /// no "BIO4" folder in the path at all — "FILES" sits directly under the game directory.
        /// </summary>
        public static string ResolveSmdStageRaz0rPath(string gameDirectory, string roomKey)
        {
            string roomNumber = ExtractRoomKeyBody(roomKey);
            if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory) || roomNumber == null)
            {
                return null;
            }

            string filesDir = FindDirectoryCaseInsensitive(gameDirectory, "FILES");
            if (filesDir == null) { return null; }

            string stageDir = FindDirectoryCaseInsensitive(filesDir, "STAGE");
            if (stageDir == null) { return null; }

            string roomDir = FindDirectoryCaseInsensitive(stageDir, "R" + roomNumber);
            if (roomDir == null) { return null; }

            return FindFileCaseInsensitive(roomDir, "0000.SMD");
        }

        /// <summary>
        /// Resolves the .SMX path that matches a given .SMD load source. SMX is optional: if
        /// no matching file is found, this returns null and the caller (RoomSmdLoader) simply
        /// skips SMX-driven behavior (FaceCulling/AlphaHierarchy/OpacityHierarchy default values),
        /// exactly like a room with no SMX would behave in NewAge.
        ///
        /// Naming per source (confirmed with the project owner):
        ///   - Smd (STX\RXXX\RXXX_004.SMD):      STX\RXXX\RXXX_005.SMX, fallback RXXX_05.SMX
        ///     (matches the SMD fallback pair: _004/_04 -> _005/_05)
        ///   - SmdData (BIO4\Data\RXXX\0000.SMD): BIO4\Data\RXXX\0000.SMX
        ///   - Smd0000 (STX\RXXX\0000.SMD):       STX\RXXX\0000.SMX
        /// </summary>
        public static string ResolveSmxPath(string gameDirectory, string roomKey, Enums.RoomModelLoadSource modelSource)
        {
            string roomNumber = ExtractRoomKeyBody(roomKey);
            if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory) || roomNumber == null)
            {
                return null;
            }

            string roomFolderName = "R" + roomNumber;

            // SmdStageRaz0r lives entirely outside "BIO4" (FILES\STAGE\Rxxx\...), so it's handled
            // separately before the BIO4 lookup below.
            if (modelSource == Enums.RoomModelLoadSource.SmdStageRaz0r)
            {
                string filesDir = FindDirectoryCaseInsensitive(gameDirectory, "FILES");
                if (filesDir == null) { return null; }

                string stageDir = FindDirectoryCaseInsensitive(filesDir, "STAGE");
                if (stageDir == null) { return null; }

                string stageRoomDir = FindDirectoryCaseInsensitive(stageDir, roomFolderName);
                if (stageRoomDir == null) { return null; }

                return FindFileCaseInsensitive(stageRoomDir, "0000.SMX");
            }

            string bio4 = FindDirectoryCaseInsensitive(gameDirectory, "BIO4");
            if (bio4 == null) { return null; }

            string firstDigit = roomNumber[0].ToString();

            if (modelSource == Enums.RoomModelLoadSource.SmdData)
            {
                string data = FindDirectoryCaseInsensitive(bio4, "Data");
                if (data == null) { return null; }

                string roomDir = FindDirectoryCaseInsensitive(data, roomFolderName);
                if (roomDir == null) { return null; }

                return FindFileCaseInsensitive(roomDir, "0000.SMX");
            }

            if (modelSource == Enums.RoomModelLoadSource.Smd0000)
            {
                string stxDir0 = FindDirectoryCaseInsensitive(bio4, "ST" + firstDigit);
                if (stxDir0 != null)
                {
                    string roomDir = FindDirectoryCaseInsensitive(stxDir0, roomFolderName);
                    if (roomDir != null)
                    {
                        string file = FindFileCaseInsensitive(roomDir, "0000.SMX");
                        if (file != null) { return file; }
                    }
                }

                string stDir0 = FindDirectoryCaseInsensitive(bio4, "ST");
                if (stDir0 != null)
                {
                    string roomDir = FindDirectoryCaseInsensitive(stDir0, roomFolderName);
                    if (roomDir != null)
                    {
                        string file = FindFileCaseInsensitive(roomDir, "0000.SMX");
                        if (file != null) { return file; }
                    }
                }

                return null;
            }

            // default: matches ResolveSmdPath's own STX/RXXX_004.SMD + RXXX_04.SMD pair.
            string stxDir = FindDirectoryCaseInsensitive(bio4, "ST" + firstDigit);
            if (stxDir != null)
            {
                string roomDir = FindDirectoryCaseInsensitive(stxDir, roomFolderName);
                if (roomDir != null)
                {
                    string file = FindFileCaseInsensitive(roomDir, roomFolderName + "_005.SMX");
                    if (file != null) { return file; }

                    file = FindFileCaseInsensitive(roomDir, roomFolderName + "_05.SMX");
                    if (file != null) { return file; }
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves the ImagePackHD .pack path for a room:
        /// [GAME]\BIO4\ImagePackHD\4400XXXX.pack
        /// where XXXX is the room number as a fixed 4-digit hex string (e.g. r100 -> 0100, r11b -> 011B).
        ///
        /// If the plain .pack isn't found, falls back to the same file name with ".lfs" appended
        /// (e.g. 4400XXXX.pack.lfs) and converts it in place via Tools\Re4lfs.exe (run hidden, no
        /// console window), then returns the resulting .pack path. This mirrors NewAge's LFS
        /// packaging without requiring the user to run the conversion manually.
        /// </summary>
        public static string ResolveImagePackHDPath(string gameDirectory, string roomKey)
        {
            string roomNumber = ExtractRoomKeyBody(roomKey);
            if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory) || roomNumber == null)
            {
                return null;
            }

            string bio4 = FindDirectoryCaseInsensitive(gameDirectory, "BIO4");
            if (bio4 == null) { return null; }

            string packDir = FindDirectoryCaseInsensitive(bio4, "ImagePackHD");
            if (packDir == null) { return null; }

            // roomNumber may contain hex letters already (e.g. "11b"); pad to 4 hex digits.
            string hexPart = roomNumber.ToUpperInvariant().PadLeft(4, '0');
            string packFileName = "4400" + hexPart + ".pack";

            string packFile = FindFileCaseInsensitive(packDir, packFileName);
            if (packFile != null) { return packFile; }

            // fallback: same name + ".lfs" (e.g. 4400XXXX.pack.lfs), converted via Tools\Re4lfs.exe
            string lfsFileName = packFileName + ".lfs";
            string lfsFile = FindFileCaseInsensitive(packDir, lfsFileName);
            if (lfsFile == null) { return null; }

            return Re4LfsConverter.ConvertAndGetPackPath(lfsFile);
        }
    }
}
