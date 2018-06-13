using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;

namespace Hitachi.Tester
{
    public class SpecialFileClass
    {

        /// <summary>
        /// Checks if requested file is in the special folder for this application.  If there returns true;
        /// If not checks for file in Application startup folder.  If there copies to special folder and returns true.
        /// If file not found anywhere, returns false.
        /// </summary>
        /// <param name="filenameWithoutPath"></param>
        /// <returns></returns>
        public static bool IsThisFileInSpecialFolder(string filenameWithoutPath)
        {
            bool retVal = false;

            // Try to find in the special application folder
            string specialFolderFile = System.IO.Path.Combine(SpecialFileFolder, filenameWithoutPath);
            if (File.Exists(specialFolderFile))
            {
                return true;
            }

            // Try to find in the application folder.
            string appFolderFile = System.IO.Path.Combine(Application.StartupPath, filenameWithoutPath);
            if (!File.Exists(specialFolderFile))
            {
                retVal = CopyFileToSpecialFolder(filenameWithoutPath);
            }
            return retVal;
        }



      /// <summary>
      /// Copies passed in filename from the application directory to special file folder. 
      /// </summary>
      /// <param name="filenameWithoutPath"></param>
      /// <returns>success</returns>
        public static bool CopyFileToSpecialFolder(string filenameWithoutPath)
        {
            return CopyFileToSpecialFolder(filenameWithoutPath, filenameWithoutPath);
        }

        /// <summary>
        /// Copies a file from application directory to special data directory and optionally changes name.
        /// </summary>
        /// <param name="sourceFilenameWithoutPath"></param>
        /// <param name="destinationFilenameWithoutPath"></param>
        /// <returns></returns>
        public static bool CopyFileToSpecialFolder(string sourceFilenameWithoutPath, string destinationFilenameWithoutPath)
        {
            bool retVal = false;

            // make up from path.
            string appFolderFile = System.IO.Path.Combine(Application.StartupPath, sourceFilenameWithoutPath);
            // make up to path
            string specialFolderFile = System.IO.Path.Combine(SpecialFileFolder, destinationFilenameWithoutPath);

            // Copy file to special folder
            FileStream fs1 = null;
            FileStream fs2 = null;

            try
            {
                fs1 = new FileStream(appFolderFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs2 = new FileStream(specialFolderFile, FileMode.Create, FileAccess.Write, FileShare.Read);

                TxFile(fs1, fs2);
                fs1.Close();
                fs2.Flush();
                fs2.Close();
                retVal = true;
            }
            catch
            {
                retVal = false;
            }
            finally
            {
                if (fs1 != null)
                {
                    fs1.Dispose();
                }
                if (fs2 != null)
                {
                    fs2.Dispose();
                }
            }
            return retVal;
        }

        /// <summary>
        /// Returns path to where we store our temp, history, and configuration files.
        /// If path does not exist, this will create it first.
        /// </summary>
        public static string SpecialFileFolder
        {
            get
            {
                // Make up the path strings that we use.
                string theHgstSpecialDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "HGST");
                string theAppSubPath = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
                string theWholeSpecialPath = System.IO.Path.Combine(theHgstSpecialDataPath, theAppSubPath);

                _CheckAndFixPermissions(theHgstSpecialDataPath);
                _CheckAndFixPermissions(theWholeSpecialPath);

                return theWholeSpecialPath;  
            }
        }

        /// <summary>
        /// Checks our access level and adds write access if we do not have it.
        /// </summary>
        /// <param name="directory"></param>
        private static void _CheckAndFixPermissions(string directory)
        {
            DirectoryInfo directoryInfo;

            // Check for the HGST path
            if (!Directory.Exists(directory))
            {
                // make new one and get info
                directoryInfo = Directory.CreateDirectory(directory);
            }
            else
            {
                // exists so just get info
                directoryInfo = new DirectoryInfo(directory);
            }

            _CheckAndFixPermissions(directoryInfo);
        }

        /// <summary>
        /// Checks our access level and adds write access if we do not have it.
        /// </summary>
        /// <param name="directoryInfo"></param>
        private static void _CheckAndFixPermissions(DirectoryInfo directoryInfo)
        {
            SecurityIdentifier securityIdentifier = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            bool foundWrite = false;
            bool foundAppend = false;
            bool foundCreateDir = false;
            bool foundCreateFile = false;
            bool foundDeleteFile = false;
            bool foundModify = false;
            bool foundReadExecute = false;

            AccessRule accessRule;

            // Get security info
            DirectorySecurity directorySecurity = directoryInfo.GetAccessControl();
            
            // Loop through each permission and look for write access
            foreach (FileSystemAccessRule rule in directorySecurity.GetAccessRules(true, true, typeof(NTAccount)))
            {
                if (rule.FileSystemRights == FileSystemRights.Write)
                {
                    // We can write
                    foundWrite = true;
                }
                if (rule.FileSystemRights == FileSystemRights.AppendData)
                {
                    foundAppend = true;
                }
                if (rule.FileSystemRights == FileSystemRights.CreateDirectories)
                {
                    foundCreateDir = true;
                }
                if (rule.FileSystemRights == FileSystemRights.CreateFiles)
                {
                    foundCreateFile = true;
                }
                if (rule.FileSystemRights == FileSystemRights.Delete)
                {
                    foundDeleteFile = true;
                }
                if (rule.FileSystemRights == FileSystemRights.Modify)
                {
                    foundModify = true;
                }
                if (rule.FileSystemRights == FileSystemRights.ReadAndExecute)
                {
                    foundReadExecute = true;
                }

            }

            // If we cannot write
            if (!foundWrite || !foundAppend || !foundCreateDir || !foundCreateFile || !foundDeleteFile || !foundModify || !foundReadExecute)
            {
                // add write access
                bool modified;
                accessRule = new FileSystemAccessRule(
                    securityIdentifier,
                    FileSystemRights.AppendData |
                    FileSystemRights.CreateDirectories |
                    FileSystemRights.CreateFiles |
                    FileSystemRights.Delete |
                    FileSystemRights.Modify |
                    FileSystemRights.ReadAndExecute |
                    FileSystemRights.Write, 
                    AccessControlType.Allow);
                directorySecurity.ModifyAccessRule(AccessControlModification.Add, accessRule, out modified);
                try
                {
                    directoryInfo.SetAccessControl(directorySecurity);
                }
                catch { }
            }
        }

        /// <summary>
        /// Takes sub directory name and returns entire path including special file directory.
        /// If directory does not exist, then creates.
        /// </summary>
        /// <param name="subDirectory"></param>
        /// <returns></returns>
        public static string GetSpecialFileFolderWithSubDir(string subDirectory)
        { 
            // define stuff to use
            DirectoryInfo directoryInfo;

            string theWholeSpecialPath = System.IO.Path.Combine(SpecialFileFolder, subDirectory);

            // Check for the HGST path
            if (!Directory.Exists(theWholeSpecialPath))
            {
                directoryInfo = Directory.CreateDirectory(theWholeSpecialPath);
            }
            else
            {
                directoryInfo = new DirectoryInfo(theWholeSpecialPath);
            }

            _CheckAndFixPermissions(directoryInfo);

            return theWholeSpecialPath;
        }

        /// <summary>
        /// Checks if requested file is in the given subpath off of the special folder for this applicaiton.  If there returns true;
        /// If not checks for file in Application startup folder.  If there copies to special folder sub path and returns true.
        /// If file not found anywhere, returns false.
        /// </summary>
        /// <param name="subPath">The sub path from the Special folder directory.</param>
        /// <param name="filenameWithoutPath">Just the filename.</param>
        /// <returns>True if file is there.</returns>
        public static bool IsThisFileInSpecialFolderSubPath(string subPath, string filenameWithoutPath)
        {
            bool retVal = false;
            string specialFileFolderAndSubPath = GetSpecialFileFolderWithSubDir(subPath);
            // Try to find in the special application folder
            string specialFolderFile = System.IO.Path.Combine(specialFileFolderAndSubPath, filenameWithoutPath);
            if (File.Exists(specialFolderFile))
            {
                return true;
            }

            // Try to find in the application folder.
            string appFolderFile = System.IO.Path.Combine(Application.StartupPath, filenameWithoutPath);
            if (!File.Exists(specialFolderFile))
            {
                // Copy file to special folder
                FileStream fs1 = null;
                FileStream fs2 = null;

                try
                {
                    fs1 = new FileStream(appFolderFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                    fs2 = new FileStream(specialFolderFile, FileMode.Create, FileAccess.Write, FileShare.Read);

                    TxFile(fs1, fs2);
                    fs1.Close();
                    fs2.Flush();
                    fs2.Close();
                    retVal = true;
                }
                catch
                {
                    retVal = false;
                }
                finally
                {
                    if (fs1 != null)
                    {
                        fs1.Dispose();
                    }
                    if (fs2 != null)
                    {
                        fs2.Dispose();
                    }
                }
            }
            return retVal;
        }

        /// <summary>
        /// Transfer file from fromFile stream to toFile stream.
        /// Caller must open and Close the streams.
        /// </summary>
        /// <param name="fromFile"></param>
        /// <param name="toFile"></param>
        public static void TxFile(Stream fromFile, Stream toFile)
        {
            byte[] byteArray = new byte[8192];
            int howMany;
            do
            {
                howMany = fromFile.Read(byteArray, 0, 8192);
                if (howMany > 0) toFile.Write(byteArray, 0, howMany);
            } while (howMany > 0);
        }


    }
}
