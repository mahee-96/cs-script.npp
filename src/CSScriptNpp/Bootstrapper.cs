using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace CSScriptNpp
{
    public static class Bootstrapper
    {
        public static string pluginDir;

        public static string dependenciesDir;
        public static string syntaxerDir;

        public static void InitSyntaxer(string sourceDir)
        {
            // needs to be a separate routine to avoid premature assembly loading
            DeploySyntaxer(sourceDir);
            CSScriptIntellisense.Syntaxer.StartServer();
        }

        public static void DeploySyntaxer(string sourceDir)
        {
            dependenciesDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                                         .PathJoin("CS-Script",
                                                   "CSScriptNpp",
                                                    Assembly.GetExecutingAssembly().GetName().Version.ToString());

            syntaxerDir =
            CSScriptIntellisense.Syntaxer.syntaxerDir = dependenciesDir.PathJoin("Roslyn");
            CSScriptIntellisense.Syntaxer.cscsFile = pluginDir.PathJoin("cscs.exe");

            var cscs = sourceDir.PathJoin("cscs.exe");

#if !DEBUG
            if (!Directory.Exists(syntaxerDir))
#endif
            {
                Directory.CreateDirectory(syntaxerDir);

                CSScriptIntellisense.Syntaxer.Exit();
                try
                {
                    File.Copy(sourceDir.PathJoin("syntaxer.exe"),
                                                  syntaxerDir.PathJoin("syntaxer.exe"),
                                                  true);
                }
                catch { }
            }
        }

        public static void Init()
        {
            try
            {
                // Debug.Assert(false);

                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

                //RoslynHelper.Init();
                //must be a separate method to allow assembly probing
                pluginDir = Assembly.GetExecutingAssembly().Location.GetDirName().PathJoin("CSScriptNpp");

                InitSyntaxer(pluginDir);

                Environment.SetEnvironmentVariable("CSScriptNpp_dir", pluginDir);
                Environment.SetEnvironmentVariable("NPP_HOSTING", "true");
                ConnectPlugins();
            }
            catch (Exception e)
            {
                var customLog = Path.Combine(Plugin.LogDir, "last_startup_error.txt");
                File.WriteAllText(customLog, e.ToString());
                throw;
            }
        }

        static bool ExistAndNotOlderThan(string file, string fileToCompareTo)
        {
            return File.Exists(file) && new Version(FileVersionInfo.GetVersionInfo(file).ProductVersion) >= new Version(FileVersionInfo.GetVersionInfo(fileToCompareTo).ProductVersion);
        }

        static void notinuse_RestorePluginTree(string pluginDir)
        {
            MessageBox.Show(" RestorePluginTree(string pluginDir)");
            try
            {
                var files = Directory.GetDirectories(pluginDir)
                                     .SelectMany(x => Directory.GetFiles(x))
                                     .Select(x => new { FileInSubDir = x, FileInRoot = Path.Combine(pluginDir, Path.GetFileName(x)) })
                                     .Where(x => ExistAndNotOlderThan(x.FileInRoot, x.FileInSubDir))
                                     .Select(x => x.FileInRoot)
                                     .ToArray();

                foreach (var item in files)
                    try
                    {
                        File.Delete(item);
                    }
                    catch { }
            }
            catch { }
        }

        static void ConnectPlugins()
        {
            try
            {
                CSScriptHelper.NotifyClient = CSScriptIntellisense.Npp.SetStatusbarLabel;
                CSScriptIntellisense.CSScriptHelper.GetEngineExe = () => CSScriptHelper.cscs_exe;
                UltraSharp.Cecil.Reflector.GetCodeCompileOutput = CSScriptHelper.GetCodeCompileOutput;
            }
            catch (Exception e)
            {
                e.LogAsError();
            }

            CSScriptIntellisense.Logger.Error = PluginLogger.Error;
            CSScriptIntellisense.Logger.Debug = PluginLogger.Debug;
            Logger.LogAsDebug("Plugin has been loaded");
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string rootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                if (args.Name.StartsWith("CSScriptIntellisense,"))
                    return Assembly.LoadFrom(Path.Combine(rootDir, @"CSScriptNpp\CSScriptIntellisense.dll"));
                else if (args.Name.StartsWith("CSScriptLibrary,"))
                    return Assembly.LoadFrom(Path.Combine(rootDir, @"CSScriptNpp\CSScriptLibrary.dll"));
                // else if (args.Name.StartsWith("NLog,"))
                //     return Assembly.LoadFrom(Path.Combine(rootDir, @"CSScriptNpp\NLog.dll"));
                else if (args.Name == Assembly.GetExecutingAssembly().FullName)
                    return Assembly.GetExecutingAssembly();
            }
            catch { }
            return null;
            //return RoslynHelper.CurrentDomain_AssemblyResolve(sender, args);
        }

        //for future use
        static Assembly Probe(string dir, string asmName)
        {
            var file = Path.Combine(dir, asmName.Split(',')[0] + ".dll");
            if (File.Exists(file))
                return Assembly.LoadFrom(file);
            else
                return null;
        }
    }

    //internal class RoslynHelper
    //{
    //    static bool initialized;
    //    static string probingDir;
    //    static public void Init()
    //    {
    //        if (!initialized)
    //        {
    //            Debug.Assert(false);
    //            initialized = true;

    //            probingDir = Assembly.GetExecutingAssembly().Location.GetDirName();
    //            if (!File.Exists(probingDir.PathJoin("Microsoft.CodeAnalysis.dll")))
    //            {
    //                probingDir = probingDir.PathJoin("Roslyn.Intellisense");
    //                if (!File.Exists(probingDir.PathJoin("Microsoft.CodeAnalysis.dll")))
    //                    probingDir = probingDir.PathJoin("Roslyn");
    //            }

    //            if (File.Exists(probingDir.PathJoin("Microsoft.CodeAnalysis.dll")))
    //                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    //        }
    //    }

    //    public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    //    {
    //        var file = probingDir.PathJoin(args.Name.Split(',').First() + ".dll");
    //        if (File.Exists(file))
    //            return Assembly.LoadFrom(file);
    //        return null;
    //    }
    //}
}