using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Microsoft.CSharp;
using JetBrains.Annotations;

namespace Sisus.Debugging
{
	/// <summary>
	/// First time setup:
	/// Create a new project containing this DllBuilderWindow class with and Editor folder.
	/// Give the project the name "Debug-Log-DLL-Builder" to make it easier to follow this guide.
	/// 
	/// Building the DLLs and DLL Installers:
	/// 1. Open the Debug-Log-DLL-Builder project you created during first time setup.
	/// 2. Import the Install Source.unitypackage found inside the Debug.Log Extensions package for the latest source code.
	///	   It can be found at Assets\Sisus\Debug.Log Extensions\Installers\Install Source.unitypackage.
	///	3. Also copy over the Assets\Sisus\Debug.Log Extensions\Scripts folder found inside the Debug.Log Extensions package.
	///	   While these scripts are not included in the built DLLs they are necessary for the project to compile.
	/// 4. Make any changes you want to the script files in the Source directory.
	/// 5. Open the DLL Builder window using the menu item Window > Debugging > Debug.Log Extensions > DLL Builder.
	/// 6. Select the 1st Build Option.
	/// 7. Click "Build DLLs". You will see compile errors after this - don't panic, that is normal.
	/// 8. Click "Set DLL Import Settings". The compile errors should go away.
	/// 9. Click "Build Installer".
	/// 10. Click "Move DLLs Under Streaming Assets". The Build DLLs button should become usable once again.
	/// 11. Select the next Build Option and repeat steps 7 to 11 until you've built all four installers.
	/// 12. Finally click the "Build Source installer". After this you should have 5 items in total inside the \"Installers\" directory.
	/// 13. Create a new empty project. Let's call it "Debug-Log-Extensions-Customized".
	/// 14. Install the latest version of Debug.Log Extensions to this project using the Package Manager.
	/// 15. Copy over all the new installers you've created to Assets\Sisus\Debug.Log Extensions\Installers, replacing the old ones.
	/// 16. Also copy over the 3 DLLs from the builder project directory Assets\StreamingAssets\DLL\UniqueNamespace to the new project at Assets\Sisus\\Debug.Log Extensions\\DLL, replacing the old one.
	/// </summary>
	public class DllBuilderWindow : EditorWindow
	{
		public enum DllVariant
		{
			Editor = 0,
			DebugBuild = 1,
			NormalBuild = 2,
			StrippedBuild = 3,
		}

		private const string GlobalNamespaceInstallerName = "Install In " + GlobalNamespaceDirectoryName;
		private const string GlobalNamespaceStrippingInstallerName = "Install In " + GlobalNamespaceStrippingDirectoryName;
		private const string UniqueNamespaceInstallerName = "Install In " + UniqueNamespaceDirectoryName;
		private const string UniqueNamespaceStrippingInstallerName = "Install In " + UniqueNamespaceStrippingDirectoryName;

		private const string GlobalNamespaceDirectoryName = "Global Namespace";
		private const string GlobalNamespaceStrippingDirectoryName = "Global Namespace With Build Stripping";
		private const string UniqueNamespaceDirectoryName = "Unique Namespace";
		private const string UniqueNamespaceStrippingDirectoryName = "Unique Namespace With Build Stripping";

		private const string SourceInstallerName = "Install Source";

		private static readonly Color completedColor = new Color(0.75f, 1f, 0.75f, 1f); // green
		private static readonly Color selectedColor = new Color(0.6f, 0.8f, 0.8f, 1f); // blue

		[SerializeField]
		private string useNamespace = "Sisus.Debugging";

		[SerializeField]
		private bool enableBuildStripping = false;

		[SerializeField]
		private bool stripFromBuilds;

		[SerializeField]
		private string scriptsDirectory = "Assets/Sisus/Debug.Log Extensions/Source";

		[SerializeField]
		private string dllDirectory = "Assets/Sisus/Debug.Log Extensions/DLL";

		[SerializeField]
		private string installersDirectory = "Assets/Sisus/Debug.Log Extensions/Installers";

		[SerializeField]
		private string[] inputScripts = new string[0];

		[SerializeField]
		private string[] assemblyNames = new string[] { "UnityEditor", "UnityEngine", "System.Core", "System", "UnityEngine.IMGUIModule", "UnityEngine.CoreModule" };

		[SerializeField]
		private bool sourceScriptsUnfolded = false;

		[SerializeField]
		private GUIContent sourceScriptsLabel = new GUIContent("Source Scripts (0)");

		[SerializeField]
		private Vector2 scrollPosition;

		[SerializeField]
		private Vector2 sourceScriptsScrollPosition;

		[SerializeField]
		private bool showFirstTimeInstructions = false;

		[SerializeField]
		private bool showBuildInstructions = false;

		[SerializeField]
		private BuildStep[] buildOptionsProgress = new BuildStep[4];

		[MenuItem("Window/Debugging/Debug.Log Extensions/DLL Builder"), UsedImplicitly]
		private static void OpenWindow()
		{
			var window = GetWindow<DllBuilderWindow>();
			window.minSize = new Vector2(430f, 435f);
			window.maxSize = new Vector2(430f, 435f);
			window.maxSize = new Vector2(99999f, 99999f);
		}

		[UsedImplicitly]
		private void OnEnable()
		{
			EditorApplication.quitting -= OnApplicationQuit;
			EditorApplication.quitting += OnApplicationQuit;

			titleContent = new GUIContent("DLL Builder");

			if(Directory.Exists(scriptsDirectory))
			{
				inputScripts = Directory.GetFiles(scriptsDirectory, "*.cs");
			}

			sourceScriptsLabel = new GUIContent("Source Scripts (" + inputScripts.Length + ")");

			if(!Directory.Exists(dllDirectory))
			{
				var dll = FindByNameAndExtension("Debug", ".dll");
				if(File.Exists(dll))
				{
					dllDirectory = Path.GetDirectoryName(dll);
				}
			}

			if(!Directory.Exists(installersDirectory))
			{
				installersDirectory = Path.GetDirectoryName(dllDirectory);
				installersDirectory = Path.Combine(installersDirectory, "Installers");
			}

			UpdateAllBuildOptionsProgress();
		}

		private void UpdateAllBuildOptionsProgress()
        {
			UpdateBuildOptionProgress(true, false);
			UpdateBuildOptionProgress(true, true);
			UpdateBuildOptionProgress(false, false);
			UpdateBuildOptionProgress(false, true);
		}

        private void UpdateBuildOptionProgress(bool usingUniqueNamespace, bool usingBuildStripping)
        {
			int index = GetBuildOptionIndex(usingUniqueNamespace, usingBuildStripping);

			if(File.Exists(GetInstallerFilePath(usingUniqueNamespace, usingBuildStripping)))
			{
				if(File.Exists(GetStreamingAssetsFilePath(usingUniqueNamespace, usingBuildStripping, DllVariant.Editor, false)))
				{
					SetBuildOptionProgress(index, BuildStep.MoveDllsUnderStreamingAssets);
					return;
				}
				SetBuildOptionProgress(index, BuildStep.BuildInstallerForDlls);
				return;
			}

			if(!File.Exists(GetDllFilePath(DllVariant.Editor, false)))
            {
				SetBuildOptionProgress(index, BuildStep.None);
				return;
			}
			
			switch(GetBuildOptionProgress(index))
			{
				case BuildStep.None:
				case BuildStep.MoveDllsUnderStreamingAssets:
					SetBuildOptionProgress(index, BuildStep.BuildDlls);
					return;
			}
		}


		[UsedImplicitly]
		private void OnGUI()
		{
			scrollPosition = GUILayout.BeginScrollView(scrollPosition);

			GUILayout.Label("Instructions", EditorStyles.boldLabel);

			showFirstTimeInstructions = GUILayout.Toggle(showFirstTimeInstructions, "First Time Setup", EditorStyles.foldout);
			if(showFirstTimeInstructions)
			{
				EditorGUILayout.HelpBox(new GUIContent(
					"Create a new project containing this DllBuilderWindow class inside an \"Editor\" folder.\n" +
					"Give the project the name \"Debug-Log-DLL-Builder\" to make it easier to follow this guide."));
			}

			GUILayout.Space(5f);
			showBuildInstructions = GUILayout.Toggle(showBuildInstructions, "Building DLL Installers", EditorStyles.foldout);
			if(showBuildInstructions)
			{
				EditorGUILayout.HelpBox(new GUIContent(
					"1. Open the Debug-Log-DLL-Builder project you created during first time setup.\n" +
					"2. Import the Install Source.unitypackage found inside the Debug.Log Extensions package for the latest source code.\n" +
						"     It can be found at Assets\\Sisus\\Debug.Log Extensions\\Installers\\Install Source.unitypackage.\n" +
					"3. Also copy over the Assets\\Sisus\\Debug.Log Extensions\\Scripts folder found inside the Debug.Log Extensions package to get rid of compile errors.\n" +
						"     While these scripts are not included in the built DLLs they are necessary for the project to compile.\n" +
					"4. Make any changes you want to the script files in the Source directory.\n" +
					"5. Open the DLL Builder window using the menu item Window > Debugging > Debug.Log Extensions/DLL Builder.\n" +
					"6. Select the 1st Build Option.\n" +
					"7. Click \"Build DLLs\". You will see compile errors after this - don't panic, that is normal.\n" +
					"8. Click \"Set DLL Import Settings\". The compile errors should go away.\n" +
					"9. Click \"Build Installer\" to finally build the installer from the DLLs.\n" +
					"10. Click \"Move DLLs Under Streaming Assets\" to stash the DLLs away. The Build DLLs button should become usable again.\n" +
					"11. Select the next Build Option and repeat steps 6 to 9 until you've built all four installers.\n" +
					"12. Finally click the \"Build Source installer\". After this you should have 5 items in total inside the \"Installers\" directory.\n" +
					"13. Create a new empty project. Let's call it \"Debug-Log-Extensions-Customized\".\n" +
					"14. Install the latest version of Debug.Log Extensions to this project using the Package Manager.\n" +
					"15. Copy over all the new installers you've created to Assets\\Sisus\\Debug.Log Extensions\\Installers, replacing the old ones.\n" +
					"16. Also copy over the 3 DLLs from the builder project directory Assets\\StreamingAssets\\DLL\\UniqueNamespace to the new project at Assets\\Sisus\\Debug.Log Extensions\\DLL, replacing the old one."));
			}

			GUILayout.Space(5f);
			GUILayout.Label("Directory Paths", EditorStyles.boldLabel);

			bool sourceDirectoryExists = Directory.Exists(scriptsDirectory);
			GUI.color = sourceDirectoryExists ? Color.white : Color.red;

			string setInputDirectory = EditorGUILayout.DelayedTextField("Source Directory", scriptsDirectory);
			if(!string.Equals(setInputDirectory, scriptsDirectory))
			{
				Undo.RecordObject(this, "Source Directory");
				scriptsDirectory = setInputDirectory.Replace('/', '\\');
				if(Directory.Exists(scriptsDirectory))
				{
					inputScripts = Directory.GetFiles(scriptsDirectory, "*.cs");
				}
				else
				{
					UnityEngine.Debug.LogError("Input Directory \"" + scriptsDirectory + "\" does not exist...");
				}
			}

			if(inputScripts.Length > 0)
			{
				sourceScriptsUnfolded = GUILayout.Toggle(sourceScriptsUnfolded, sourceScriptsLabel, EditorStyles.foldout);
				if(sourceScriptsUnfolded)
				{
					sourceScriptsScrollPosition = GUILayout.BeginScrollView(sourceScriptsScrollPosition, GUILayout.MaxHeight(100f));
					{
						GUILayout.BeginHorizontal();
						{
							GUILayout.Space(10f);
							GUILayout.BeginVertical();
							{
								GUI.enabled = false;
								foreach(var script in inputScripts)
								{
									GUILayout.Label(Path.GetFileName(script));
								}
								GUI.enabled = true;
							}
							GUILayout.EndVertical();
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.EndScrollView();
				}
			}

			bool dllDirectoryExists = Directory.Exists(dllDirectory);
			GUI.color = dllDirectoryExists ? Color.white : Color.red;

			string setOutputDirectory = EditorGUILayout.TextField("DLL Directory", dllDirectory);
			if(!string.Equals(setOutputDirectory, dllDirectory))
			{
				Undo.RecordObject(this, "DLL Directory");
				dllDirectory = setOutputDirectory.Replace('/', '\\');
			}

			GUI.color = !Directory.Exists(installersDirectory) ? Color.red : Color.white;

			string setInstallersDirectory = EditorGUILayout.TextField("Installers Directory", installersDirectory);
			if(!string.Equals(setInstallersDirectory, installersDirectory))
			{
				Undo.RecordObject(this, "Installers Directory");
				installersDirectory = setInstallersDirectory.Replace('/', '\\');
			}

			GUI.color = Color.white;

			GUILayout.Space(5f);

			GUILayout.BeginHorizontal();

			EditorGUILayout.PrefixLabel(" ");
			GUI.Label(GUILayoutUtility.GetLastRect(), "Build Options", EditorStyles.boldLabel);

			int selectedBuildOptionIndex = GetSelectedBuildOptionIndex();

			GUI.color = GetBuildOptionButtonColor(0, selectedBuildOptionIndex == 0);
			if(GUILayout.Button("1", GUILayout.Width(25f)))
			{
				useNamespace = "Sisus.Debugging";
				enableBuildStripping = false;
			}
			GUI.color = GetBuildOptionButtonColor(1, selectedBuildOptionIndex == 1);
			if(GUILayout.Button("2", GUILayout.Width(25f)))
			{
				useNamespace = "Sisus.Debugging";
				enableBuildStripping = true;
			}
			GUI.color = GetBuildOptionButtonColor(2, selectedBuildOptionIndex == 2);
			if(GUILayout.Button("3", GUILayout.Width(25f)))
			{
				useNamespace = "";
				enableBuildStripping = false;
			}
			GUI.color = GetBuildOptionButtonColor(3, selectedBuildOptionIndex == 3);
			if(GUILayout.Button("4", GUILayout.Width(25f)))
			{
				useNamespace = "";
				enableBuildStripping = true;
			}
			GUI.color = Color.white;

			GUILayout.EndHorizontal();

			string setNamespace = EditorGUILayout.TextField("Use Namespace", useNamespace);
			if(!string.Equals(setNamespace, useNamespace))
			{
				Undo.RecordObject(this, "Use Namespace");
				useNamespace = setNamespace.Replace(" ", "").Replace("\r", "").Replace("\n", "");
			}

			bool setEnableBuildStripping = EditorGUILayout.Toggle("Build Stripping", enableBuildStripping);
			if(setEnableBuildStripping != enableBuildStripping)
			{
				Undo.RecordObject(this, "Build Stripping");
				enableBuildStripping = setEnableBuildStripping;
			}

			GUILayout.EndScrollView();

			GUILayout.Space(10f);

			bool busy = EditorApplication.isUpdating || EditorApplication.isCompiling;
			if(inputScripts.Length == 0 || string.IsNullOrEmpty(dllDirectory) || busy || !sourceDirectoryExists)
			{
				GUI.enabled = false;
			}

			BuildStep lastDoneStep = GetSelectedBuildOptionLastDoneStep();

			GUI.color = lastDoneStep >= BuildStep.BuildDlls ? completedColor : Color.white;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10f);

				if(GUILayout.Button("Build DLLs"))
				{
					if(EditorUtility.DisplayDialog("Confirm Build DLLs", "Build all three DLL variants now from " + inputScripts.Length + " scripts " + (enableBuildStripping ? "using" : "without") + " build stripping?", "Build", "Cancel"))
					{
						SetSelectedBuildOptionProgress(BuildStep.BuildDlls);

						AssetDatabase.StartAssetEditing();

						bool usingUniqueNamespace = !string.IsNullOrEmpty(useNamespace);

						BuildUsingSettings(usingUniqueNamespace, DllVariant.Editor);
						BuildUsingSettings(usingUniqueNamespace, DllVariant.DebugBuild);
						BuildUsingSettings(usingUniqueNamespace, enableBuildStripping ? DllVariant.StrippedBuild : DllVariant.NormalBuild);

						if(File.Exists(GetDllFilePath(DllVariant.Editor, false)))
						{
							HideSourceScripts();
						}

						AssetDatabase.StopAssetEditing();
						AssetDatabase.Refresh();
					}
				}

				GUILayout.Space(10f);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10f);

			bool dllExists = File.Exists(Path.Combine(dllDirectory, GetDllLocalPath(DllVariant.NormalBuild)));

			GUI.enabled = dllExists && !busy;
			GUI.color = lastDoneStep >= BuildStep.SetDllImportSettings ? completedColor : Color.white;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10f);

				if(GUILayout.Button("Set DLL Import Settings", GUILayout.Height(25f)))
				{
					SetSelectedBuildOptionProgress(BuildStep.SetDllImportSettings);

					AssetDatabase.StartAssetEditing();

					Directory.CreateDirectory(dllDirectory);
					bool usingUniqueNamespace = !string.IsNullOrEmpty(useNamespace);
					SetDLLImportSettings(usingUniqueNamespace, DllVariant.Editor);
					SetDLLImportSettings(usingUniqueNamespace, DllVariant.DebugBuild);
					SetDLLImportSettings(usingUniqueNamespace, enableBuildStripping ? DllVariant.StrippedBuild : DllVariant.NormalBuild);
					AssetDatabase.StopAssetEditing();
				}

				GUILayout.Space(10f);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10f);

			GUI.enabled = dllExists && !busy;
			GUI.color = lastDoneStep >= BuildStep.CopyOverXmlDocumentaion ? completedColor : Color.white;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10f);

				if(GUILayout.Button("Copy Over Documentation XML", GUILayout.Height(25f)))
				{
					SetSelectedBuildOptionProgress(BuildStep.CopyOverXmlDocumentaion);

					AssetDatabase.StartAssetEditing();

					string editorDllPath = GetDllFilePath(DllVariant.Editor, true);
					string xmlFromPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Debug-Log-Extensions.xml");
					string xmlToPath = Path.Combine(Path.GetDirectoryName(editorDllPath), "Debug-Log-Extensions.xml");
					if(File.Exists(xmlToPath))
					{
						File.Delete(xmlToPath);
					}
					File.Copy(xmlFromPath, xmlToPath);
					AssetDatabase.StopAssetEditing();
				}

				GUILayout.Space(10f);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10f);

			GUI.enabled = dllExists && !busy;
			GUI.color = lastDoneStep >= BuildStep.BuildInstallerForDlls ? completedColor : Color.white;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10f);

				if(GUILayout.Button("Build Installer", GUILayout.Height(25f)))
				{
					SetSelectedBuildOptionProgress(BuildStep.BuildInstallerForDlls);

					if(EditorUtility.DisplayDialog("Confirm Build Installer", "Build DLL installer now from DLLs found in the \n\"" + dllDirectory + "\" directory?", "Build", "Cancel"))
					{
						AssetDatabase.StartAssetEditing();

						Directory.CreateDirectory(installersDirectory);

						if(!string.IsNullOrEmpty(useNamespace))
						{
							CreateInstallerForDlls(enableBuildStripping ? UniqueNamespaceStrippingInstallerName : UniqueNamespaceInstallerName);
						}
						else
						{
							CreateInstallerForDlls(enableBuildStripping ? GlobalNamespaceStrippingInstallerName : GlobalNamespaceInstallerName);
						}

						AssetDatabase.StopAssetEditing();
						AssetDatabase.Refresh();
					}
				}

				GUILayout.Space(10f);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10f);

			GUI.enabled = dllExists && !busy;
			GUI.color = lastDoneStep >= BuildStep.MoveDllsUnderStreamingAssets ? completedColor : Color.white;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10f);

				if(GUILayout.Button("Move DLLs Under Streaming Assets", GUILayout.Height(25f)))
				{
					SetSelectedBuildOptionProgress(BuildStep.MoveDllsUnderStreamingAssets);

					AssetDatabase.StartAssetEditing();

					MoveUnderStreamingAssets(DllVariant.Editor);
					MoveUnderStreamingAssets(DllVariant.DebugBuild);
					MoveUnderStreamingAssets(enableBuildStripping ? DllVariant.StrippedBuild : DllVariant.NormalBuild);

					UnhideSourceScripts();

					AssetDatabase.StopAssetEditing();
					AssetDatabase.Refresh();
				}

				GUILayout.Space(10f);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10f);

			string sourceInstallerPath = Path.Combine(installersDirectory, SourceInstallerName + ".unitypackage");

			GUI.enabled = sourceDirectoryExists && !busy;
			GUI.color = File.Exists(sourceInstallerPath) ? completedColor : Color.white;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10f);

				if(GUILayout.Button("Build Source Installer", GUILayout.Height(20f)))
				{
					if(EditorUtility.DisplayDialog("Confirm Build Source Installer", "Build source installer now from " + inputScripts.Length + " scripts?", "Build", "Cancel"))
					{
						AssetDatabase.StartAssetEditing();

						Directory.CreateDirectory(installersDirectory);

						string dllBuilderPath = FindByNameAndExtension("DllBuilderWindow", ".cs");

						AssetDatabase.ExportPackage(new string[] { scriptsDirectory, dllBuilderPath }, sourceInstallerPath, ExportPackageOptions.Recurse);

						AssetDatabase.StopAssetEditing();
						AssetDatabase.Refresh();
					}
				}
				GUILayout.Space(10f);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10f);
		}

		private Color GetBuildOptionButtonColor(int index, bool isSelected)
        {
			bool completed = GetBuildOptionProgress(index) == BuildStep.MoveDllsUnderStreamingAssets;
			if(isSelected)
			{
				return selectedColor;
			}
			if(completed)
			{
				return completedColor;
			}
			return Color.white;
		}

		private int GetBuildOptionIndex(bool usingUniqueNamespace, bool usingBuildStripping)
        {
			if(usingUniqueNamespace)
			{
				return usingBuildStripping ? 1 : 0;
			}
			return usingBuildStripping ? 3 : 2;
		}

		private int GetSelectedBuildOptionIndex()
        {
			return GetBuildOptionIndex(useNamespace.Length > 0, enableBuildStripping);
		}

		private BuildStep GetSelectedBuildOptionLastDoneStep()
        {
			return GetBuildOptionProgress(GetSelectedBuildOptionIndex());
        }

		private BuildStep GetBuildOptionProgress(int index)
		{
			return buildOptionsProgress[index];
		}

		private void SetSelectedBuildOptionProgress(BuildStep value)
		{
			SetBuildOptionProgress(GetSelectedBuildOptionIndex(), value);
		}

		private void SetBuildOptionProgress(int index, BuildStep value)
		{
			buildOptionsProgress[index] = value;
			EditorUtility.SetDirty(this);
		}

		private enum BuildStep
		{
			None = 0,

			BuildDlls = 2,
			SetDllImportSettings = 3,
			CopyOverXmlDocumentaion = 4,
			BuildInstallerForDlls = 5,
			MoveDllsUnderStreamingAssets = 6
		}

		private void OnApplicationQuit()
        {
			buildOptionsProgress = new BuildStep[4];
        }

		private void OnProjectChange()
		{
			UpdateAllBuildOptionsProgress();
		}

		private void HideSourceScripts()
        {
			if(!Directory.Exists(scriptsDirectory))
			{
				return;
			}

			string hiddenPath = GetHiddenScriptsPath();

			MoveDirectoryWithMetaData(scriptsDirectory, hiddenPath);
		}

		private string GetHiddenScriptsPath()
        {
			return Path.Combine(Path.GetDirectoryName(scriptsDirectory), "." + Path.GetFileName(scriptsDirectory));
		}

		private void UnhideSourceScripts()
		{
			string hiddenPath = GetHiddenScriptsPath();

			if(!Directory.Exists(hiddenPath))
            {
				return;
            }

			MoveDirectoryWithMetaData(hiddenPath, scriptsDirectory);
		}

		[CanBeNull]
		private string BuildUsingSettings(bool usingUniqueNamespace, DllVariant variant)
		{
			string dllPath = BuildDLL(usingUniqueNamespace, variant);
			if(!string.IsNullOrEmpty(dllPath) && File.Exists(dllPath))
			{
				UnityEngine.Debug.Log("Successfully built DLL at \"" + dllPath + "\".");
			}
			else
			{
				UnityEngine.Debug.LogWarning("Failed building DLL at " + (!string.IsNullOrEmpty(dllPath) ? dllPath : Path.Combine(dllDirectory, "Debug.dll")) +".");
			}
			return dllPath;
		}

		/// <summary>
		/// Tries to builds DLL and returns path to DLL if successful, otherwise returns null.
		/// </summary>
		/// <returns> Path to DLL or null if failed.</returns>
		[CanBeNull]
		private string BuildDLL(bool usingUniqueNamespace, DllVariant variant)
		{
			if(string.IsNullOrEmpty(scriptsDirectory))
			{
				UnityEngine.Debug.LogError("Input directory must be specified.");
				return null;
			}

			if(!Directory.Exists(scriptsDirectory))
			{
				UnityEngine.Debug.LogError("Input directory \""+ scriptsDirectory + "\" does not exist.");
				return null;
			}

			var files = Directory.GetFiles(scriptsDirectory, "*.cs", SearchOption.AllDirectories);

			if(files.Length == 0)
			{
				UnityEngine.Debug.LogError("Input directory \"" + scriptsDirectory + "\" contained no script assets with the extensions \".cs\".");
				return null;
			}

			var assemblyReferences = new List<string>(6);
			foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if(Array.IndexOf(assemblyNames, assembly.GetName().Name) != -1)
				{
					assemblyReferences.Add(assembly.Location);
				}
			}

			var preprocessorDirectives = new List<string>(5);

			if(usingUniqueNamespace)
            {
				preprocessorDirectives.Add("DEBUG_LOG_EXTENSIONS_INSIDE_UNIQUE_NAMESPACE");
            }

			if(enableBuildStripping)
			{
				preprocessorDirectives.Add("DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED");
			}

			switch(variant)
            {
                case DllVariant.Editor:
					preprocessorDirectives.Add("TRACE");
					preprocessorDirectives.Add("UNITY_ASSERTIONS");
					preprocessorDirectives.Add("DEBUG");
					preprocessorDirectives.Add("UNITY_EDITOR");
					break;
                case DllVariant.DebugBuild:
					preprocessorDirectives.Add("TRACE");
					preprocessorDirectives.Add("UNITY_ASSERTIONS");
					preprocessorDirectives.Add("DEBUG");
					preprocessorDirectives.Add("DEVELOPMENT_BUILD");
					
					break;
                case DllVariant.NormalBuild:
					preprocessorDirectives.Add("TRACE");
					preprocessorDirectives.Add("UNITY_ASSERTIONS");
					break;
                case DllVariant.StrippedBuild:
					break;
				default:
					throw new IndexOutOfRangeException("Unsupported DLLVariant: " + variant);
            }

			string createDllAtPath = GetDllFilePath(variant, true);
			var createdDll = BuildDLL(files, assemblyReferences.ToArray(), createDllAtPath, preprocessorDirectives);
			return createdDll != null ? createdDll.Location : null;
        }

		[CanBeNull]
		private void SetDLLImportSettings(bool usingUniqueNamespace, DllVariant variant)
        {
			string dllFilePath = GetDllFilePath(variant, true);

			var importer = (PluginImporter)AssetImporter.GetAtPath(dllFilePath);

			if(importer == null)
            {
				UnityEngine.Debug.LogWarning("Can not set DLL import settings because it was not found at expected path: " + dllFilePath);
				return;
			}

			if(!usingUniqueNamespace)
			{
				importer.GetType().GetProperty("IsExplicitlyReferenced", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(importer, true, null);
			}

            switch(variant)
            {
                case DllVariant.Editor:
					importer.SetCompatibleWithAnyPlatform(false);
					importer.SetCompatibleWithEditor(true);
					
					break;
                case DllVariant.DebugBuild:
					importer.SetCompatibleWithAnyPlatform(true);
					importer.SetExcludeEditorFromAnyPlatform(true);
					importer.DefineConstraints = new[] { "DEVELOPMENT_BUILD" };
					break;
                case DllVariant.NormalBuild:
				case DllVariant.StrippedBuild:
					importer.SetCompatibleWithAnyPlatform(true);
					importer.SetExcludeEditorFromAnyPlatform(true);
					importer.DefineConstraints = new[] { "!DEVELOPMENT_BUILD" };
					break;
				default:
					throw new IndexOutOfRangeException("Unsupported DLLVariant: " + variant);
			}
			importer.SaveAndReimport();
		}

        private void OnDisable()
        {
			EditorApplication.quitting -= OnApplicationQuit;
		}

		private string GetStreamingAssetsDirectoryPath(bool usingUniqueNamespace, bool usingBuildStripping)
        {
			string destinationDirectory = Path.Combine(Application.streamingAssetsPath, "DLL");
			if(usingUniqueNamespace)
			{
				return Path.Combine(destinationDirectory, usingBuildStripping ? UniqueNamespaceStrippingDirectoryName : UniqueNamespaceDirectoryName);
			}
			else
			{
				return Path.Combine(destinationDirectory, usingBuildStripping ? GlobalNamespaceStrippingDirectoryName : GlobalNamespaceDirectoryName);
			}
		}

		private string GetStreamingAssetsFilePath(bool usingUniqueNamespace, bool usingBuildStripping, DllVariant variant, bool createDirectoryIfMissing)
		{
			string directoryPath = GetStreamingAssetsDirectoryPath(usingUniqueNamespace, usingBuildStripping);
			string filePath = Path.Combine(directoryPath, GetDllLocalPath(variant));
			if(createDirectoryIfMissing)
            {
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			}

			return filePath;
		}

		private void MoveUnderStreamingAssets(DllVariant variant)
        {
			string sourcePath = GetDllFilePath(variant, false);

			if(!File.Exists(sourcePath))
            {
				UnityEngine.Debug.LogWarning("Can not move DLL under streaming assets directory because it was not found at expected path: " + sourcePath);
				return;
            }

			try
			{
				string destinationPath = GetStreamingAssetsFilePath(!string.IsNullOrEmpty(useNamespace), enableBuildStripping, variant, true);

				BackupMetaData(sourcePath);
				MoveFileWithMetaData(sourcePath, destinationPath);
			}
			catch(Exception e)
            {
				UnityEngine.Debug.LogWarning(e);
            }
        }

		private static void MoveDirectoryWithMetaData(string sourcePath, string destinationPath)
        {
			Directory.Move(sourcePath, destinationPath);
			MoveMetaData(sourcePath, destinationPath);
		}

		private static void MoveFileWithMetaData(string sourcePath, string destinationPath)
        {
			if(File.Exists(destinationPath))
			{
				File.Delete(destinationPath);
			}
			File.Move(sourcePath, destinationPath);
			MoveMetaData(sourcePath, destinationPath);
		}

		private static void MoveMetaData(string sourcePath, string destinationPath)
        {
			string metaSourcePath = sourcePath + ".meta";
			if(!File.Exists(metaSourcePath))
			{
				return;
			}
			string metaDestinationPath = destinationPath + ".meta";
			if(File.Exists(metaDestinationPath))
			{
				File.Delete(metaDestinationPath);
			}
			File.Move(metaSourcePath, metaDestinationPath);
		}

		private static void BuildDLLMetaData(string dllPath)
        {
			string metaDataBackupPath = Path.Combine(GetDLLMetaDataBackupDirectoryPath(), Path.GetFileName(dllPath));
			if(!File.Exists(dllPath))
            {
				return;
            }
			CopyMetaData(metaDataBackupPath, dllPath + ".meta", true, false);
		}

		private static void BackupMetaData(string sourcePath)
        {
			string backupDirectoryPath = GetDLLMetaDataBackupDirectoryPath();
			Directory.CreateDirectory(backupDirectoryPath);
			CopyMetaData(sourcePath, Path.Combine(backupDirectoryPath, Path.GetFileName(sourcePath)), false, true);
		}

		private static string GetDLLMetaDataBackupDirectoryPath()
        {
			return Path.Combine(Application.streamingAssetsPath, "Metadata");
		}

		private static void CopyMetaData(string sourcePath, string destinationPath, bool sourceIsBackup, bool destinationIsBackup)
		{
			string metaSourcePath = sourcePath + (sourceIsBackup ? ".meta.backup" : ".meta");
			if(!File.Exists(metaSourcePath))
			{
				return;
			}
			string metaDestinationPath = destinationPath + (destinationIsBackup ? ".meta.backup" : ".meta");
			if(File.Exists(metaDestinationPath))
			{
				File.Delete(metaDestinationPath);
			}
			File.Copy(metaSourcePath, metaDestinationPath);
		}

		private string GetDllFilePath(DllVariant variant, bool createDirectoryIfMissing)
        {
			string filePath = Path.Combine(dllDirectory, GetDllLocalPath(variant));
			if(createDirectoryIfMissing)
            {
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			}

			return filePath;
        }

		private string GetDllLocalPath(DllVariant variant)
        {
			switch(variant)
			{
				case DllVariant.Editor:
					return "Editor/Debug-Log-Extensions.dll";
				case DllVariant.DebugBuild:
					return "Debug/Debug-Log-Extensions.dll";
				case DllVariant.NormalBuild:
				case DllVariant.StrippedBuild:
					return "Release/Debug-Log-Extensions.dll";
				default:
					throw new IndexOutOfRangeException("Unsupported DLLVariant: " + variant);
			}
		}

		private string GetInstallerFilePath(bool usingUniqueNamespace, bool usingBuildStripping)
		{
			return Path.Combine(installersDirectory, GetInstallerFileName(usingUniqueNamespace, usingBuildStripping));
		}

		private string GetInstallerFileName(bool usingUniqueNamespace, bool usingBuildStripping)
        {
			if(usingUniqueNamespace)
			{
				return (usingBuildStripping ? UniqueNamespaceStrippingInstallerName : UniqueNamespaceInstallerName) + ".unitypackage";
			}
			return (usingBuildStripping ? GlobalNamespaceStrippingInstallerName : GlobalNamespaceInstallerName) + ".unitypackage";
		}

        [CanBeNull]
		private static Assembly BuildDLL(string[] scriptFilePaths, string[] assemblyReferences, string dllOutputPath, List<string> preprocessorDirectives)
		{
			BuildDLLMetaData(dllOutputPath);

			var providerOptions = new Dictionary<string, string> { { "CompilerVersion", "v3.5" } };
			var provider = new CSharpCodeProvider(providerOptions); 
			var parameters = new CompilerParameters();

			foreach(var assemblyLocation in assemblyReferences)
			{
				parameters.ReferencedAssemblies.Add(assemblyLocation);
			}

			parameters.GenerateExecutable = false;
			parameters.GenerateInMemory = false;
			parameters.OutputAssembly = dllOutputPath;
			parameters.IncludeDebugInformation = false;

			// NOTE: the documentation is created at the root of the project directory!
			string documentationFileName = Path.GetFileNameWithoutExtension(dllOutputPath) + ".xml";
			parameters.CompilerOptions += "/doc:" + documentationFileName;

			if(preprocessorDirectives.Count > 0)
			{
				parameters.CompilerOptions += " /d:" + string.Join(";", preprocessorDirectives);
			}

			var result = provider.CompileAssemblyFromFile(parameters, scriptFilePaths);
			if(result.Errors.Count > 0)
			{
				foreach(CompilerError error in result.Errors)
				{
					if(!string.IsNullOrEmpty(error.ErrorNumber))
					{
						UnityEngine.Debug.LogWarning(error.ErrorNumber + ": " + error.ErrorText);
					}
					else
					{
						UnityEngine.Debug.LogWarning(error.ErrorText);
					}
				}
			}

			return result.CompiledAssembly;
		}

		private void CreateInstallerForDlls(string packageName)
		{
			string[] dllPaths = new string[]
			{
				Path.Combine(dllDirectory, GetDllLocalPath(DllVariant.Editor)),
				Path.Combine(dllDirectory, GetDllLocalPath(DllVariant.DebugBuild)),
				Path.Combine(dllDirectory, GetDllLocalPath(DllVariant.NormalBuild)),
			};

			#if DEV_MODE
			foreach(var path in dllPaths)
			{
				Debug.Assert(File.Exists(path));
			}
			#endif

			string installerPath = Path.Combine(installersDirectory, packageName + ".unitypackage");
			
			if(File.Exists(installerPath))
			{
				AssetDatabase.DeleteAsset(installerPath);
			}

			AssetDatabase.ExportPackage(dllPaths, installerPath, ExportPackageOptions.Default);

			if(File.Exists(installerPath))
			{
				UnityEngine.Debug.Log("Successfully built installer \"" + installerPath + "\" from DLLs in \"" + dllDirectory + "\".");
			}
			else
			{
				UnityEngine.Debug.LogWarning("Failed building installer \"" + installerPath + "\" from DLLs in \"" + dllDirectory + "\".");
			}
		}

		[NotNull]
		private  static string FindByNameAndExtension([NotNull]string byName, [NotNull]string byExtension)
		{
			var guids = AssetDatabase.FindAssets(byName);

			int count = guids.Length;
			if(count == 0)
			{
				return null;
			}

			for(int n = count - 1; n >= 0; n--)
			{
				var guid = guids[n];
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if(string.Equals(Path.GetFileNameWithoutExtension(path), byName, StringComparison.OrdinalIgnoreCase) && string.Equals(Path.GetExtension(path), byExtension, StringComparison.OrdinalIgnoreCase))
				{
					return path;
				}
			}

			return "";
		}
    }
}