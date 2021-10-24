using HDDL.IO.Disk;
using HDDL.IO.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Details how the ScannerKernal should handle script side-loading
    /// </summary>
    public class ScriptLoadingDetails
    {
        /// <summary>
        /// If SideLoadSource poinits to a folder, this search pattern will be used to obtain files to load
        /// </summary>
        public string SourceFolderSearchPattern { get; set; }

        /// <summary>
        /// The path to the side-load script file or a directory containing script(s)
        /// </summary>
        public string SideLoadSource { get; private set; }

        /// <summary>
        /// The path to the initial load script file
        /// </summary>
        public string InitialLoadSource { get; private set; }

        /// <summary>
        /// Whether or not to delete the side-load script after executing it
        /// </summary>
        public bool DeleteSideLoadSource { get; private set; }

        /// <summary>
        /// If DeleteSideLoadScript is false then the side-load script will have an extension tacked onto it to prevent unintentional re-execution
        /// </summary>
        public string SideLoadScriptCompletionExtension { get; private set; }

        /// <summary>
        /// Whether side-loading continue during runtime through passive scans of the directed path
        /// </summary>
        public bool MonitorDuringRuntime { get; private set; }

        /// <summary>
        /// Create a script loading details by specifically assigns values to each item in the details
        /// </summary>
        /// <param name="initialLoadSource">The path to the initial load script file</param>
        /// <param name="sideLoadSource">The path to the side-load script file or a directory containing script(s)</param>
        /// <param name="deleteSideLoadSource">Whether or not to delete the side-load script after executing it</param>
        /// <param name="sideLoadScriptCompletionExtension">If DeleteSideLoadScript is false then the side-load script will have an extension tacked onto it to prevent unintentional re-execution</param>
        /// <param name="sourceFolderSearchPattern">If SideLoadSource poinits to a folder, this search pattern will be used to obtain files to load</param>
        /// <param name="monitorDuringRuntime">Whether side-loading continue during runtime through passive scans of the directed path</param>
        /// 
        public ScriptLoadingDetails(
            string initialLoadSource,
            string sideLoadSource,
            bool deleteSideLoadSource,
            string sideLoadScriptCompletionExtension,
            bool monitorDuringRuntime = true,
            string sourceFolderSearchPattern = "*.hdsl")
        {
            ProcessValues(initialLoadSource, sideLoadSource, deleteSideLoadSource, sideLoadScriptCompletionExtension, monitorDuringRuntime, sourceFolderSearchPattern);
        }

        /// <summary>
        /// Create a script loading details by reading an already loaded ini file
        /// </summary>
        /// <param name="iniFile">The loaded ini file</param>
        public ScriptLoadingDetails(
            IniFileManager iniFile)
        {
            AcceptValuesFromIni(iniFile);
        }

        /// <summary>
        /// Create a script loading details from an ini file
        /// </summary>
        /// <param name="iniFilePath">The ini file's path</param>
        public ScriptLoadingDetails(string iniFilePath)
        {
            if (File.Exists(iniFilePath))
            {
                // load an ini file expected to match this structure
                var iniFile = IniFileManager.Explore(iniFilePath, true, false, false,
                    new IniSubsection("HDSL_DB", null,
                        new IniValue("DatabaseLocation", defaultValue: "file database.db")),
                    new IniSubsection("HDSL_Passives", null,
                        new IniValue("InitialScript", defaultValue: ""),
                        new IniValue("SideloadScript", defaultValue: ""),
                        new IniValue("DeleteSideloadScriptAfterConsumption", defaultValue: "False"),
                        new IniValue("ConsumedSideloadScriptExtension", defaultValue: ".done")));

                AcceptValuesFromIni(iniFile);
            }
            else
            {
                throw new FileNotFoundException(iniFilePath);
            }
        }

        /// <summary>
        /// Obtains values from an ini file and sends them to the ProcessValues method
        /// </summary>
        /// <param name="iniFile">The already loaded file to explore</param>
        private void AcceptValuesFromIni(IniFileManager iniFile)
        {
            var delete = false;
            if (!bool.TryParse(iniFile[@"HDSL_Passives>DeleteSideloadScriptAfterConsumption"].Value, out delete))
            {
                throw new ArgumentException($"Ini file value 'HDSL_Passives > DeleteSideloadScriptAfterConsumption' must be either 'true' or 'false'.");
            }

            var monitor = false;
            if (!bool.TryParse(iniFile[@"HDSL_Passives>MonitorDuringRuntime"].Value, out monitor))
            {
                throw new ArgumentException($"Ini file value 'HDSL_Passives > MonitorDuringRuntime' must be either 'true' or 'false'.");
            }

            ProcessValues(
                iniFile[@"HDSL_Passives>InitialScript"]?.Value,
                iniFile[@"HDSL_Passives>SideloadScript"]?.Value,
                delete,
                iniFile[@"HDSL_Passives>ConsumedSideloadScriptExtension"]?.Value,
                monitor,
                iniFile[@"HDSL_Passives>SourceFolderSearchPattern"]?.Value);
        }

        /// <summary>
        /// Validates the values and stores them for use
        /// </summary>
        /// <param name="initialLoadSource">The path to the initial load script file</param>
        /// <param name="sideLoadSource">The path to the side-load script file or a directory containing script(s)</param>
        /// <param name="deleteSideLoadSource">Whether or not to delete the side-load script after executing it</param>
        /// <param name="sideLoadScriptCompletionExtension">If DeleteSideLoadScript is false then the side-load script will have an extension tacked onto it to prevent unintentional re-execution</param>
        /// <param name="sourceFolderSearchPattern">If SideLoadSource poinits to a folder, this search pattern will be used to obtain files to load</param>
        /// <param name="monitorDuringRuntime">Whether side-loading continue during runtime through passive scans of the directed path</param>
        private void ProcessValues(
            string initialLoadSource,
            string sideLoadSource,
            bool deleteSideLoadSource,
            string sideLoadScriptCompletionExtension,
            bool monitorDuringRuntime,
            string sourceFolderSearchPattern
            )
        {
            if (!string.IsNullOrWhiteSpace(sideLoadScriptCompletionExtension))
            {
                if (!sideLoadScriptCompletionExtension.StartsWith("."))
                {
                    SideLoadScriptCompletionExtension = $".{sideLoadScriptCompletionExtension}";
                }
                else
                {
                    SideLoadScriptCompletionExtension = sideLoadScriptCompletionExtension;
                }
            }

            InitialLoadSource = PathHelper.EnsurePath(initialLoadSource);
            SideLoadSource = PathHelper.EnsurePath(sideLoadSource, true);
            SourceFolderSearchPattern = sourceFolderSearchPattern;
            DeleteSideLoadSource = deleteSideLoadSource;

            // to passively monitor, the user must provide an existing file or directory path
            MonitorDuringRuntime = monitorDuringRuntime && !string.IsNullOrWhiteSpace(SideLoadSource) ? true : false;
        }

        /// <summary>
        /// Loads and returns the scripts pointed to by the side-loading details' information
        /// </summary>
        /// <returns>An array of file paths</returns>
        public string[] GetScripts()
        {
            var files = new List<string>();
            if (File.Exists(SideLoadSource))
            {
                files.Add(SideLoadSource);
            }
            else if (Directory.Exists(SideLoadSource))
            {
                foreach (var file in Directory.GetFiles(SideLoadSource, SourceFolderSearchPattern))
                {
                    files.Add(file);
                }
            }

            return files.ToArray();
        }
    }
}
