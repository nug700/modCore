﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CodeHatch;
using CodeHatch.AI;
using CodeHatch.Common;
using CodeHatch.Networking.Events;
using CodeHatch.Networking.Events.Player;
using UnityEngine;

namespace modCore
{
    public class ModCore
    {
        #region variables
        public Monitor monitorComp;
        public Console console;
        public static ModCore modCore;
        public static Dictionary<string, List<CoreConsole.CommandDescription>> plugin_Commands;

        ModApi modApi;
        string path = string.Empty;
        static string modFolder = string.Empty;
        static ICollection<IPlugin> plugins;
        Dictionary<string, IPlugin> name_plugin;
        
        #endregion

        #region properties
        /// <summary>
        /// Returns a list of all plugins.
        /// </summary>
        public ICollection<IPlugin> Plugins
        {
            get { return plugins; }
        }

        /// <summary>
        /// Returns the current version of modCore.
        /// </summary>
        public string Version
        {
            get { return "ModCore version 1.2-beta"; }
        }

        /// <summary>
        /// Returns a reference to the mod API.
        /// </summary>
        public ModApi API
        {
            get { return modApi; }
        }

        /// <summary>
        /// The folder config files and models are put into.
        /// </summary>
        public static string ModFolder
        {
            get { return modFolder; }
        }
        #endregion


        #region constructors
        public ModCore()
        {
            //Console.modCore = this;
            ObjImporter.modCore = this;
            modFolder = Environment.CurrentDirectory + "\\StarForge_Data\\mods\\";
            GameObject monitor = new GameObject("monitor");
            monitorComp = monitor.AddComponent<Monitor>();
            monitor.AddComponent<CoreConsole>();
            monitorComp.modCore = this;
            modApi = new ModApi(this);
            monitorComp.modApi = modApi;
            Log("started.");
            StartPlugins();
            EventManager.Subscribe(typeof(PlayerCommandEvent), new EventSubscription.EventSubscriber(SubmitCommand));
            EventManager.Subscribe(typeof(PlayerMessageEvent), new EventSubscription.EventSubscriber(SubmitMessage));
            AddModCoreCommands();
            Alias.Load();
        }
        #endregion
        

        #region methods
        public static void Init() {
            if (modCore == null) {
                modCore = new ModCore();
            }
            else
                LogError("Only one modCore instance allowed.");
        }

        public void StartPlugins()
        {
            try
            {
                name_plugin = new Dictionary<string, IPlugin>();
                //allCommands = new Dictionary<string, CommandDescription>();
                plugin_Commands = new Dictionary<string, List<CoreConsole.CommandDescription>>();

                // search for dll files
                Log("searching for dll files");
                //path = Environment.CurrentDirectory + "\\StarForge_Data\\Managed\\";
                path = modFolder + "\\plugins\\";

                string[] dllFileNames = null;
                if (Directory.Exists(path))
                {
                    dllFileNames = Directory.GetFiles(path, "*.dll");
                }
                else
                {
                    LogError("failed to locate Managed folder.\nPath: " + path);
                    return;
                }

                // load assemblies
                Log("Loading assemblies.");
                ICollection<Assembly> assemblies = new List<Assembly>(dllFileNames.Length);
                foreach (string dllFile in dllFileNames)
                {
                    AssemblyName an = AssemblyName.GetAssemblyName(dllFile);
                    Assembly assembly = Assembly.Load(an);
                    assemblies.Add(assembly);
                }

                if (assemblies.Count > 0)
                {
                    // search for types that implement IPlugin
                    Log("searching for types that implement IPlugin.");
                    Type pluginType = typeof(IPlugin);
                    ICollection<Type> pluginTypes = new List<Type>();
                    foreach (Assembly assembly in assemblies)
                    {
                        if (assembly != null)
                        {
                            Type[] types = assembly.GetTypes();
                            foreach (Type type in types)
                            {
                                if (type.IsInterface || type.IsAbstract)
                                {
                                    continue;
                                }
                                else
                                {
                                    if (type.GetInterface(pluginType.FullName) != null)
                                    {
                                        pluginTypes.Add(type);
                                    }
                                }
                            }
                        }
                    }

                    // create instances from types.
                    Log("creating instances from types.");
                    plugins = new List<IPlugin>(pluginTypes.Count);
                    foreach (Type type in pluginTypes)
                    {
                        IPlugin plugin = (IPlugin)System.Activator.CreateInstance(type);
                        plugin.Init(this);
                        plugins.Add(plugin);
                        name_plugin.Add(plugin.Name, plugin);
                    }

                    Log("Loaded " + plugins.Count + " plugins.");
                }
                else
                {
                    LogWarning("No assemblies to load.");
                }
            }
            catch (Exception e)
            {
                LogError(e);
                LogError("Failed to load mods.");
            }
        }

        public void SubmitCommand(IEvent baseEvent)
        {
            PlayerCommandEvent theEvent = (PlayerCommandEvent)baseEvent;
            string message = theEvent.Command;

            if (message.StartsWith("/"))
            {
                CoreConsole.Execute(message.Remove(0, 1));
                #region old
                //string[] args = message.Split(' ');
                /*string inputCommand = string.Empty;
                string commandFromAliase = Alias.GetCommand(args[0].Replace("/", ""));

                if (!commandFromAliase.Equals(string.Empty))
                    inputCommand = "/" + commandFromAliase;
                else
                    inputCommand = args[0];

                switch (inputCommand.ToLower())
                {
                    case "/cheat":
                    case "/say":
                    case "/me":
                    case "/suicide":
                        break;

                    case "/alias":
                        #region alias
                        if (args.Length > 1)
                        {
                            switch (args[1].ToLower())
                            {
                                case "-add":
                                    if (args.Length == 4)
                                    {
                                        Alias.AddAlias(args[2], args[3]);
                                        Alias.Save();
                                    }
                                    else
                                        PrintError("Invalid number of arguments. Got " + args.Length + ", expected 4.");
                                    break;

                                case "-remove":
                                    if (args.Length == 3)
                                    {
                                        Alias.RemoveAlias(args[2]);
                                        Alias.Save();
                                    }
                                    else
                                        PrintError("Invalid number of arguments, Got " + args.Length + ", expected 3.");
                                    break;

                                case "-list":
                                    Dictionary<string, string> aliases = Alias.Aliases;
                                    Print("");
                                    Print("Available aliases (<alias>, <command>): ");
                                    foreach (string alias in aliases.Keys)
                                    {
                                        Print("-- " + alias + ", " + aliases[alias]);
                                    }
                                    Print("");
                                    break;

                                case "-reload":
                                    Alias.Load();
                                    break;

                                case "-h":
                                    Print("");
                                    Print("Available actions: ");
                                    Print("-add <alias> <command> : adds an alias for command.");
                                    Print("-remove <alias> : Removes the alias.");
                                    Print("-list : lists all aliases.");
                                    Print("-reload : Loads the alias list from the file.");
                                    Print("-h : Shows this prompt.");
                                    Print("");
                                    break;

                                default:
                                    PrintError("Unknown action \"" + args[1] + "\".");
                                    break;
                            }
                        }
                        else
                            PrintError("Invalid number of arguments.");
                        break;
                        #endregion

                    case "/exit":
                        Application.Quit();
                        break;
                        
                    case "/help":
                        #region help
                        string tabString = string.Empty;
                        string gray = string.Empty;
                        string Red = string.Empty;
                        if (Application.loadedLevel == 2)
                        {
                            tabString = "[00ff00]--[ffffff]";
                            gray = "[ffffff]";
                            Red = "[ff0000]";
                        }

                        if (args.Length == 1)
                        {
                            Print("");
                            Print("Available commands (<> = required, [] = optional): ");
                            foreach (CommandDescription cmd in allCommands.Values)
                            {
                                Print(tabString + "/" + cmd.command + " " + cmd.command_args + " - " + cmd.description_small + "." + gray);
                            }
                            Print("");
                        }
                        else if (args.Length == 3 && args[1].ToLower().StartsWith("-p"))
                        {
                            if (plugin_Commands.ContainsKey(args[2].ToLower()))
                            {
                                Print("");
                                Print("Available commands (<> = required, [] = optional): ");
                                CommandDescription[] pluginCmds = plugin_Commands[args[2].ToLower()];
                                foreach (CommandDescription cmd in pluginCmds)
                                {
                                    Print(tabString + "/" + cmd.command + " " + cmd.command_args + ": " + cmd.description_small + "." + gray);
                                }
                                Print("");
                            }
                            else
                            {
                                PrintError("Plugin not found.");
                            }
                        }
                        else if (args.Length == 2 && !args[1].ToLower().StartsWith("-p"))
                        {
                            if (allCommands.ContainsKey(args[1]))
                            {
                                Print("");
                                Print("Command description (<> = required, [] = optional): ");
                                Print(Red + "Command: " + gray + "/" + allCommands[args[1]].command + " " + allCommands[args[1]].command_args + gray);
                                if (allCommands[args[1]].Equals(string.Empty))
                                {
                                    Print(Red + "Description: " + gray + allCommands[args[1]].description_small + "." + gray);
                                }
                                else
                                {
                                    Print(Red + "Short description: " + gray + allCommands[args[1]].description_small + "." + gray);
                                    Print(Red + "Long description: " + gray + allCommands[args[1]].description_Long + "." + gray);
                                }
                                Print("");
                            }
                            else
                                PrintError("Command not found.");
                        }
                        else if (args.Length == 2 && args[1].ToLower().StartsWith("-p"))
                        {
                            PrintError("Please specify a plugin.");
                        }
                        else if (args.Length > 3)
                        {
                            PrintError("To many arguments.");
                        }
                        else
                            PrintError("Unknown error.");

                        break;
                        #endregion

                    case "/plugins":
                        #region plugins
                        Print("");
                        Print("Plugins: ");
                        if (plugins != null)
                        {
                            foreach (IPlugin plugin in plugins)
                            {
                                Print("--" + plugin.Name);
                            }
                        }
                        break;
                        #endregion

                    case "/version":
                        Print(Version);
                        break;

                    case "/reload":
                        #region reload
                        StartPlugins();
                        Alias.Load();
                        Print("Plugins reloaded successfully.");
                        break;
                        #endregion

                    default:
                        // send to other mods for processing.
                        #region default
                        bool received = false;
                        if (allCommands.ContainsKey(args[0].Replace("/","")))
                        {
                            if (plugins != null)
                            {
                                foreach (IPlugin plugin in plugins)
                                {
                                    if (plugin != null)
                                    {
                                        plugin.Submit(message);
                                    }
                                }
                            }
                            else
                                PrintError("plugins list is null.");
                        }
                        else
                            PrintError("Command \"" + args[0] + "\" doesn't exist.");
                        break;
                        #endregion
                }*/
                #endregion
            }
            else if (plugins != null)
            {

            }
        }

        public void SubmitMessage(IEvent baseEvent) {
            PlayerMessageEvent theEvent = (PlayerMessageEvent)baseEvent;
            string message = theEvent.Message;

            foreach (IPlugin plugin in plugins) {
                if (plugin != null)
                    plugin.Submit(message);
            }
        }

        public static void Log(object message)
        {
            Debug.Log("modCore.dll: " + message.ToString());
            CoreConsole.Log("modCore.dll: " + message.ToString());
        }

        public static void LogWarning(object message)
        {
            Debug.Log("modCore.dll Warning: " + message.ToString());
            CoreConsole.Log("modCore.dll Warning: " + message.ToString());
        }

        public static void LogError(object message)
        {
            Debug.Log("modCore.dll error: " + message.ToString());
            CoreConsole.Log("modCore.dll error: " + message.ToString());
        }

        public static void LogError(Exception e)
        {
            Debug.Log("modCore.dll error: \nmessage: " + e.Message + ",\nSource: " + e.Source + ",\nStackTrace: " + e.StackTrace);
            CoreConsole.Log("modCore.dll error: \nmessage: " + e.Message + ",\nSource: " + e.Source);
        }

        public static void Print_AsPlayer(object message)
        {
            if (Application.loadedLevel == 2)
                Console.Submit(message.ToString(), true);
            
        }

        public static void Print(object message)
        {
            if (Application.loadedLevel == 2)
            {
                Console.AddMessage(message.ToString());
                CoreConsole.Log(message.ToString());
            }
            else
            {
                CoreConsole.Log(message.ToString());
            }
        }

        public static void PrintWarning(object message)
        {
            if (Application.loadedLevel == 2)
            {
                Console.AddWarning("Warning: " + message.ToString());
                CoreConsole.Log("Warning: " + message.ToString());
            }
            else
            {
                CoreConsole.Log("Warning: " + message.ToString());
            }
        }

        public static void PrintError(object message)
        {
            if (Application.loadedLevel == 2)
            {
                Console.AddError("Error: " + message.ToString());
                CoreConsole.Log("Error: " + message.ToString());
            }
            else
            {
                CoreConsole.Log("Error: " + message.ToString());
            }
        }

        public static void RegisterCommand(CoreConsole.CommandDescription desc) {
            CoreConsole.RegisterCommand(desc);
            if (plugin_Commands.ContainsKey(desc.plugin))
                plugin_Commands[desc.plugin].Add(desc);
            else
                plugin_Commands[desc.plugin] = new List<CoreConsole.CommandDescription>();
        }

        public IPlugin GetPlugin(string name)
        {
            if (name_plugin.ContainsKey(name))
                return name_plugin[name];
            else
                return null;
        }

        private void AddModCoreCommands()
        {
            RegisterCommand(new CoreConsole.CommandDescription("modCore", "alias", "<-action> [alias] [command]", "Add and remove aliases", "Used to add and removed aliases to the game. The second argument is the action. If the action is \"-add\", then the third argument is the alias and the forth is the command the alias is for. If the action is \"-remove\", the the third argument should be the alias you want removed. Use \"-h\" for the action to get a list of available actions.", AliasCmd));
            RegisterCommand(new CoreConsole.CommandDescription("modCore", "exit", string.Empty, "exits the game", ExitCmd));
            RegisterCommand(new CoreConsole.CommandDescription("modCore", "plugins", string.Empty, "lists installed plugins", PluginsCmd));
            RegisterCommand(new CoreConsole.CommandDescription("modCore", "version", string.Empty, "Prints the current version of modCore.", VersionCmd));
            RegisterCommand(new CoreConsole.CommandDescription("modCore", "reload", string.Empty, "Rescans and restarts plugins.", ReloadCmd));
        }

        object AliasCmd(params string[] args) {
            if (args.Length > 1) {
                switch (args[1].ToLower()) {
                    case "-add":
                        if (args.Length == 4) {
                            Alias.AddAlias(args[2], args[3]);
                            Alias.Save();
                        }
                        else
                            PrintError("Invalid number of arguments. Got " + args.Length + ", expected 4.");
                        break;

                    case "-remove":
                        if (args.Length == 3) {
                            Alias.RemoveAlias(args[2]);
                            Alias.Save();
                        }
                        else
                            PrintError("Invalid number of arguments, Got " + args.Length + ", expected 3.");
                        break;

                    case "-list":
                        Dictionary<string, string> aliases = Alias.Aliases;
                        Print("");
                        Print("Available aliases (<alias>, <command>): ");
                        foreach (string alias in aliases.Keys) {
                            Print("-- " + alias + ", " + aliases[alias]);
                        }
                        Print("");
                        break;

                    case "-reload":
                        Alias.Load();
                        break;

                    case "-h":
                        Print("");
                        Print("Available actions: ");
                        Print("-add <alias> <command> : adds an alias for command.");
                        Print("-remove <alias> : Removes the alias.");
                        Print("-list : lists all aliases.");
                        Print("-reload : Loads the alias list from the file.");
                        Print("-h : Shows this prompt.");
                        Print("");
                        break;

                    default:
                        PrintError("Unknown action \"" + args[1] + "\".");
                        break;
                }
            }
            else
                PrintError("Invalid number of arguments.");
            return "";
        }

        object ExitCmd(params string[] args) {
            Application.Quit();
            return "";
        }

        object PluginsCmd(params string[] args) {
            Print("");
            Print("Plugins: ");
            if (plugins != null) {
                foreach (IPlugin plugin in plugins) {
                    Print("--" + plugin.Name);
                }
            }
            return "";
        }

        object VersionCmd(params string[] args) {
            Print(Version);
            return "";
        }

        object ReloadCmd(params string[] args) {
            StartPlugins();
            Alias.Load();
            Print("Plugins reloaded successfully.");
            return "";
        }
        #endregion

    }
}
