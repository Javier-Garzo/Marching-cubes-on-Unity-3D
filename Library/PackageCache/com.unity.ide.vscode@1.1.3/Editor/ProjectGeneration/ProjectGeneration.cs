using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Profiling;

namespace VSCodeEditor
{
    public interface IGenerator
    {
        bool SyncIfNeeded(IEnumerable<string> affectedFiles, IEnumerable<string> reimportedFiles);
        void Sync();
        string SolutionFile();
        string ProjectDirectory { get; }
        void GenerateAll(bool generateAll);
        bool SolutionExists();
    }

    public class ProjectGeneration : IGenerator
    {
        enum ScriptingLanguage
        {
            None,
            CSharp
        }

        public static readonly string MSBuildNamespaceUri = "http://schemas.microsoft.com/developer/msbuild/2003";

        const string k_WindowsNewline = "\r\n";

        const string k_SettingsJson = @"{
    ""files.exclude"":
    {
        ""**/.DS_Store"":true,
        ""**/.git"":true,
        ""**/.gitignore"":true,
        ""**/.gitmodules"":true,
        ""**/*.booproj"":true,
        ""**/*.pidb"":true,
        ""**/*.suo"":true,
        ""**/*.user"":true,
        ""**/*.userprefs"":true,
        ""**/*.unityproj"":true,
        ""**/*.dll"":true,
        ""**/*.exe"":true,
        ""**/*.pdf"":true,
        ""**/*.mid"":true,
        ""**/*.midi"":true,
        ""**/*.wav"":true,
        ""**/*.gif"":true,
        ""**/*.ico"":true,
        ""**/*.jpg"":true,
        ""**/*.jpeg"":true,
        ""**/*.png"":true,
        ""**/*.psd"":true,
        ""**/*.tga"":true,
        ""**/*.tif"":true,
        ""**/*.tiff"":true,
        ""**/*.3ds"":true,
        ""**/*.3DS"":true,
        ""**/*.fbx"":true,
        ""**/*.FBX"":true,
        ""**/*.lxo"":true,
        ""**/*.LXO"":true,
        ""**/*.ma"":true,
        ""**/*.MA"":true,
        ""**/*.obj"":true,
        ""**/*.OBJ"":true,
        ""**/*.asset"":true,
        ""**/*.cubemap"":true,
        ""**/*.flare"":true,
        ""**/*.mat"":true,
        ""**/*.meta"":true,
        ""**/*.prefab"":true,
        ""**/*.unity"":true,
        ""build/"":true,
        ""Build/"":true,
        ""Library/"":true,
        ""library/"":true,
        ""obj/"":true,
        ""Obj/"":true,
        ""ProjectSettings/"":true,
        ""temp/"":true,
        ""Temp/"":true
    }
}";

        /// <summary>
        /// Map source extensions to ScriptingLanguages
        /// </summary>
        static readonly Dictionary<string, ScriptingLanguage> k_BuiltinSupportedExtensions = new Dictionary<string, ScriptingLanguage>
        {
            { "cs", ScriptingLanguage.CSharp },
            { "uxml", ScriptingLanguage.None },
            { "uss", ScriptingLanguage.None },
            { "shader", ScriptingLanguage.None },
            { "compute", ScriptingLanguage.None },
            { "cginc", ScriptingLanguage.None },
            { "hlsl", ScriptingLanguage.None },
            { "glslinc", ScriptingLanguage.None },
            { "template", ScriptingLanguage.None },
            { "raytrace", ScriptingLanguage.None }
        };

        string m_SolutionProjectEntryTemplate = string.Join("\r\n", @"Project(""{{{0}}}"") = ""{1}"", ""{2}"", ""{{{3}}}""", @"EndProject").Replace("    ", "\t");

        string m_SolutionProjectConfigurationTemplate = string.Join("\r\n", @"        {{{0}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU", @"        {{{0}}}.Debug|Any CPU.Build.0 = Debug|Any CPU", @"        {{{0}}}.Release|Any CPU.ActiveCfg = Release|Any CPU", @"        {{{0}}}.Release|Any CPU.Build.0 = Release|Any CPU").Replace("    ", "\t");

        static readonly string[] k_ReimportSyncExtensions = { ".dll", ".asmdef" };

        /// <summary>
        /// Map ScriptingLanguages to project extensions
        /// </summary>
        /*static readonly Dictionary<ScriptingLanguage, string> k_ProjectExtensions = new Dictionary<ScriptingLanguage, string>
        {
            { ScriptingLanguage.CSharp, ".csproj" },
            { ScriptingLanguage.None, ".csproj" },
        };*/
        static readonly Regex k_ScriptReferenceExpression = new Regex(
            @"^Library.ScriptAssemblies.(?<dllname>(?<project>.*)\.dll$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        string[] m_ProjectSupportedExtensions = new string[0];
        public string ProjectDirectory { get; }
        bool m_ShouldGenerateAll;

        public void GenerateAll(bool generateAll)
        {
            m_ShouldGenerateAll = generateAll;
        }

        readonly string m_ProjectName;
        readonly IAssemblyNameProvider m_AssemblyNameProvider;
        readonly IFileIO m_FileIOProvider;
        readonly IGUIDGenerator m_GUIDProvider;

        const string k_ToolsVersion = "4.0";
        const string k_ProductVersion = "10.0.20506";
        const string k_BaseDirectory = ".";
        const string k_TargetFrameworkVersion = "v4.7.1";
        const string k_TargetLanguageVersion = "latest";

        public ProjectGeneration(string tempDirectory)
            : this(tempDirectory, new AssemblyNameProvider(), new FileIOProvider(), new GUIDProvider()) { }

        public ProjectGeneration(string tempDirectory, IAssemblyNameProvider assemblyNameProvider, IFileIO fileIO, IGUIDGenerator guidGenerator)
        {
            ProjectDirectory = tempDirectory.Replace('\\', '/');
            m_ProjectName = Path.GetFileName(ProjectDirectory);
            m_AssemblyNameProvider = assemblyNameProvider;
            m_FileIOProvider = fileIO;
            m_GUIDProvider = guidGenerator;
        }

        /// <summary>
        /// Syncs the scripting solution if any affected files are relevant.
        /// </summary>
        /// <returns>
        /// Whether the solution was synced.
        /// </returns>
        /// <param name='affectedFiles'>
        /// A set of files whose status has changed
        /// </param>
        /// <param name="reimportedFiles">
        /// A set of files that got reimported
        /// </param>
        public bool SyncIfNeeded(IEnumerable<string> affectedFiles, IEnumerable<string> reimportedFiles)
        {
            Profiler.BeginSample("SolutionSynchronizerSync");
            SetupProjectSupportedExtensions();

            // Don't sync if we haven't synced before
            if (SolutionExists() && HasFilesBeenModified(affectedFiles, reimportedFiles))
            {
                Sync();

                Profiler.EndSample();
                return true;
            }

            Profiler.EndSample();
            return false;
        }

        bool HasFilesBeenModified(IEnumerable<string> affectedFiles, IEnumerable<string> reimportedFiles)
        {
            return affectedFiles.Any(ShouldFileBePartOfSolution) || reimportedFiles.Any(ShouldSyncOnReimportedAsset);
        }

        static bool ShouldSyncOnReimportedAsset(string asset)
        {
            return k_ReimportSyncExtensions.Contains(new FileInfo(asset).Extension);
        }

        public void Sync()
        {
            SetupProjectSupportedExtensions();
            GenerateAndWriteSolutionAndProjects();
        }

        public bool SolutionExists()
        {
            return m_FileIOProvider.Exists(SolutionFile());
        }

        void SetupProjectSupportedExtensions()
        {
            m_ProjectSupportedExtensions = EditorSettings.projectGenerationUserExtensions;
        }

        bool ShouldFileBePartOfSolution(string file)
        {
            string extension = Path.GetExtension(file);

            // Exclude files coming from packages except if they are internalized.
            if (!m_ShouldGenerateAll && IsInternalizedPackagePath(file))
            {
                return false;
            }

            // Dll's are not scripts but still need to be included..
            if (extension == ".dll")
                return true;

            if (file.ToLower().EndsWith(".asmdef"))
                return true;

            return IsSupportedExtension(extension);
        }

        bool IsSupportedExtension(string extension)
        {
            extension = extension.TrimStart('.');
            if (k_BuiltinSupportedExtensions.ContainsKey(extension))
                return true;
            if (m_ProjectSupportedExtensions.Contains(extension))
                return true;
            return false;
        }

        static ScriptingLanguage ScriptingLanguageFor(Assembly island)
        {
            return ScriptingLanguageFor(GetExtensionOfSourceFiles(island.sourceFiles));
        }

        static string GetExtensionOfSourceFiles(string[] files)
        {
            return files.Length > 0 ? GetExtensionOfSourceFile(files[0]) : "NA";
        }

        static string GetExtensionOfSourceFile(string file)
        {
            var ext = Path.GetExtension(file).ToLower();
            ext = ext.Substring(1); //strip dot
            return ext;
        }

        static ScriptingLanguage ScriptingLanguageFor(string extension)
        {
            return k_BuiltinSupportedExtensions.TryGetValue(extension.TrimStart('.'), out var result)
                ? result
                : ScriptingLanguage.None;
        }

        public void GenerateAndWriteSolutionAndProjects()
        {
            // Only synchronize islands that have associated source files and ones that we actually want in the project.
            // This also filters out DLLs coming from .asmdef files in packages.
            var assemblies = m_AssemblyNameProvider.GetAssemblies(ShouldFileBePartOfSolution);

            var allAssetProjectParts = GenerateAllAssetProjectParts();

            SyncSolution(assemblies);
            var allProjectIslands = RelevantIslandsForMode(assemblies).ToList();
            foreach (Assembly assembly in allProjectIslands)
            {
                var responseFileData = ParseResponseFileData(assembly);
                SyncProject(assembly, allAssetProjectParts, responseFileData, allProjectIslands);
            }

            WriteVSCodeSettingsFiles();
        }

        IEnumerable<ResponseFileData> ParseResponseFileData(Assembly assembly)
        {
            var systemReferenceDirectories = CompilationPipeline.GetSystemAssemblyDirectories(assembly.compilerOptions.ApiCompatibilityLevel);

            Dictionary<string, ResponseFileData> responseFilesData = assembly.compilerOptions.ResponseFiles.ToDictionary(x => x, x => m_AssemblyNameProvider.ParseResponseFile(
                x,
                ProjectDirectory,
                systemReferenceDirectories
            ));

            Dictionary<string, ResponseFileData> responseFilesWithErrors = responseFilesData.Where(x => x.Value.Errors.Any())
                .ToDictionary(x => x.Key, x => x.Value);

            if (responseFilesWithErrors.Any())
            {
                foreach (var error in responseFilesWithErrors)
                foreach (var valueError in error.Value.Errors)
                {
                    Debug.LogError($"{error.Key} Parse Error : {valueError}");
                }
            }

            return responseFilesData.Select(x => x.Value);
        }

        Dictionary<string, string> GenerateAllAssetProjectParts()
        {
            Dictionary<string, StringBuilder> stringBuilders = new Dictionary<string, StringBuilder>();

            foreach (string asset in m_AssemblyNameProvider.GetAllAssetPaths())
            {
                // Exclude files coming from packages except if they are internalized.
                // TODO: We need assets from the assembly API
                if (!m_ShouldGenerateAll && IsInternalizedPackagePath(asset))
                {
                    continue;
                }

                string extension = Path.GetExtension(asset);
                if (IsSupportedExtension(extension) && ScriptingLanguage.None == ScriptingLanguageFor(extension))
                {
                    // Find assembly the asset belongs to by adding script extension and using compilation pipeline.
                    var assemblyName = m_AssemblyNameProvider.GetAssemblyNameFromScriptPath(asset + ".cs");

                    if (string.IsNullOrEmpty(assemblyName))
                    {
                        continue;
                    }

                    assemblyName = Utility.FileNameWithoutExtension(assemblyName);

                    if (!stringBuilders.TryGetValue(assemblyName, out var projectBuilder))
                    {
                        projectBuilder = new StringBuilder();
                        stringBuilders[assemblyName] = projectBuilder;
                    }

                    projectBuilder.Append("     <None Include=\"").Append(EscapedRelativePathFor(asset)).Append("\" />").Append(k_WindowsNewline);
                }
            }

            var result = new Dictionary<string, string>();

            foreach (var entry in stringBuilders)
                result[entry.Key] = entry.Value.ToString();

            return result;
        }

        bool IsInternalizedPackagePath(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return false;
            }

            var packageInfo = m_AssemblyNameProvider.FindForAssetPath(file);
            if (packageInfo == null)
            {
                return false;
            }

            var packageSource = packageInfo.source;
            return packageSource != PackageSource.Embedded && packageSource != PackageSource.Local;
        }

        void SyncProject(
            Assembly island,
            Dictionary<string, string> allAssetsProjectParts,
            IEnumerable<ResponseFileData> responseFilesData,
            List<Assembly> allProjectIslands)
        {
            SyncProjectFileIfNotChanged(ProjectFile(island), ProjectText(island, allAssetsProjectParts, responseFilesData, allProjectIslands));
        }

        void SyncProjectFileIfNotChanged(string path, string newContents)
        {
            SyncFileIfNotChanged(path, newContents);
        }

        void SyncSolutionFileIfNotChanged(string path, string newContents)
        {
            SyncFileIfNotChanged(path, newContents);
        }

        void SyncFileIfNotChanged(string filename, string newContents)
        {
            if (m_FileIOProvider.Exists(filename))
            {
                var currentContents = m_FileIOProvider.ReadAllText(filename);

                if (currentContents == newContents)
                {
                    return;
                }
            }

            m_FileIOProvider.WriteAllText(filename, newContents);
        }

        string ProjectText(
            Assembly assembly,
            Dictionary<string, string> allAssetsProjectParts,
            IEnumerable<ResponseFileData> responseFilesData,
            List<Assembly> allProjectIslands)
        {
            var projectBuilder = new StringBuilder(ProjectHeader(assembly, responseFilesData));
            var references = new List<string>();
            var projectReferences = new List<Match>();

            foreach (string file in assembly.sourceFiles)
            {
                if (!ShouldFileBePartOfSolution(file))
                    continue;

                var extension = Path.GetExtension(file).ToLower();
                var fullFile = EscapedRelativePathFor(file);
                if (".dll" != extension)
                {
                    projectBuilder.Append("     <Compile Include=\"").Append(fullFile).Append("\" />").Append(k_WindowsNewline);
                }
                else
                {
                    references.Add(fullFile);
                }
            }

            // Append additional non-script files that should be included in project generation.
            if (allAssetsProjectParts.TryGetValue(assembly.name, out var additionalAssetsForProject))
                projectBuilder.Append(additionalAssetsForProject);

            var islandRefs = references.Union(assembly.allReferences);

            foreach (string reference in islandRefs)
            {
                var match = k_ScriptReferenceExpression.Match(reference);
                if (match.Success)
                {
                    // assume csharp language
                    // Add a reference to a project except if it's a reference to a script assembly
                    // that we are not generating a project for. This will be the case for assemblies
                    // coming from .assembly.json files in non-internalized packages.
                    var dllName = match.Groups["dllname"].Value;
                    if (allProjectIslands.Any(i => Path.GetFileName(i.outputPath) == dllName))
                    {
                        projectReferences.Add(match);
                        continue;
                    }
                }

                string fullReference = Path.IsPathRooted(reference) ? reference : Path.Combine(ProjectDirectory, reference);

                AppendReference(fullReference, projectBuilder);
            }

            var responseRefs = responseFilesData.SelectMany(x => x.FullPathReferences.Select(r => r));
            foreach (var reference in responseRefs)
            {
                AppendReference(reference, projectBuilder);
            }

            if (0 < projectReferences.Count)
            {
                projectBuilder.AppendLine("  </ItemGroup>");
                projectBuilder.AppendLine("  <ItemGroup>");
                foreach (Match reference in projectReferences)
                {
                    var referencedProject = reference.Groups["project"].Value;

                    projectBuilder.Append("    <ProjectReference Include=\"").Append(referencedProject).Append(GetProjectExtension()).Append("\">").Append(k_WindowsNewline);
                    projectBuilder.Append("      <Project>{").Append(ProjectGuid(Path.Combine("Temp", reference.Groups["project"].Value + ".dll"))).Append("}</Project>").Append(k_WindowsNewline);
                    projectBuilder.Append("      <Name>").Append(referencedProject).Append("</Name>").Append(k_WindowsNewline);
                    projectBuilder.AppendLine("    </ProjectReference>");
                }
            }

            projectBuilder.Append(ProjectFooter());
            return projectBuilder.ToString();
        }

        static void AppendReference(string fullReference, StringBuilder projectBuilder)
        {
            //replace \ with / and \\ with /
            var escapedFullPath = SecurityElement.Escape(fullReference);
            escapedFullPath = escapedFullPath.Replace("\\", "/");
            escapedFullPath = escapedFullPath.Replace("\\\\", "/");
            projectBuilder.Append(" <Reference Include=\"").Append(Utility.FileNameWithoutExtension(escapedFullPath)).Append("\">").Append(k_WindowsNewline);
            projectBuilder.Append(" <HintPath>").Append(escapedFullPath).Append("</HintPath>").Append(k_WindowsNewline);
            projectBuilder.Append(" </Reference>").Append(k_WindowsNewline);
        }

        public string ProjectFile(Assembly assembly)
        {
            var fileBuilder = new StringBuilder(assembly.name);

            //            if (!assembly.flags.HasFlag(AssemblyFlags.EditorAssembly) && m_PlayerAssemblies.Contains(assembly))
            //            {
            //                fileBuilder.Append("-player");
            //            }
            fileBuilder.Append(".csproj");
            return Path.Combine(ProjectDirectory, fileBuilder.ToString());
        }

        public string SolutionFile()
        {
            return Path.Combine(ProjectDirectory, $"{m_ProjectName}.sln");
        }

        string ProjectHeader(
            Assembly assembly,
            IEnumerable<ResponseFileData> responseFilesData
        )
        {
            // TODO: .Concat(EditorUserBuildSettings.activeScriptCompilationDefines)
            var arguments = new object[]
            {
                k_ToolsVersion,
                k_ProductVersion,
                ProjectGuid(assembly.name),
                string.Join(";", new[] { "DEBUG", "TRACE" }.Concat(assembly.defines).Concat(responseFilesData.SelectMany(x => x.Defines)).Distinct().ToArray()),
                MSBuildNamespaceUri,
                assembly.name,
                EditorSettings.projectGenerationRootNamespace,
                k_TargetFrameworkVersion,
                k_TargetLanguageVersion,
                k_BaseDirectory,
                assembly.compilerOptions.AllowUnsafeCode | responseFilesData.Any(x => x.Unsafe)
            };

            try
            {
                return string.Format(GetProjectHeaderTemplate(), arguments);
            }
            catch (Exception)
            {
                throw new NotSupportedException("Failed creating c# project because the c# project header did not have the correct amount of arguments, which is " + arguments.Length);
            }
        }

        static string GetSolutionText()
        {
            return string.Join("\r\n", @"", @"Microsoft Visual Studio Solution File, Format Version {0}", @"# Visual Studio {1}", @"{2}", @"Global", @"    GlobalSection(SolutionConfigurationPlatforms) = preSolution", @"        Debug|Any CPU = Debug|Any CPU", @"        Release|Any CPU = Release|Any CPU", @"    EndGlobalSection", @"    GlobalSection(ProjectConfigurationPlatforms) = postSolution", @"{3}", @"    EndGlobalSection", @"    GlobalSection(SolutionProperties) = preSolution", @"        HideSolutionNode = FALSE", @"    EndGlobalSection", @"EndGlobal", @"").Replace("    ", "\t");
        }

        static string GetProjectFooterTemplate()
        {
            return string.Join("\r\n", @"  </ItemGroup>", @"  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />", @"  <!-- To modify your build process, add your task inside one of the targets below and uncomment it.", @"       Other similar extension points exist, see Microsoft.Common.targets.", @"  <Target Name=""BeforeBuild"">", @"  </Target>", @"  <Target Name=""AfterBuild"">", @"  </Target>", @"  -->", @"</Project>", @"");
        }

        static string GetProjectHeaderTemplate()
        {
            var header = new[]
            {
                @"<?xml version=""1.0"" encoding=""utf-8""?>",
                @"<Project ToolsVersion=""{0}"" DefaultTargets=""Build"" xmlns=""{4}"">",
                @"  <PropertyGroup>",
                @"    <LangVersion>{8}</LangVersion>",
                @"  </PropertyGroup>",
                @"  <PropertyGroup>",
                @"    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>",
                @"    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>",
                @"    <ProductVersion>{1}</ProductVersion>",
                @"    <SchemaVersion>2.0</SchemaVersion>",
                @"    <RootNamespace>{6}</RootNamespace>",
                @"    <ProjectGuid>{{{2}}}</ProjectGuid>",
                @"    <OutputType>Library</OutputType>",
                @"    <AppDesignerFolder>Properties</AppDesignerFolder>",
                @"    <AssemblyName>{5}</AssemblyName>",
                @"    <TargetFrameworkVersion>{7}</TargetFrameworkVersion>",
                @"    <FileAlignment>512</FileAlignment>",
                @"    <BaseDirectory>{9}</BaseDirectory>",
                @"  </PropertyGroup>",
                @"  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">",
                @"    <DebugSymbols>true</DebugSymbols>",
                @"    <DebugType>full</DebugType>",
                @"    <Optimize>false</Optimize>",
                @"    <OutputPath>Temp\bin\Debug\</OutputPath>",
                @"    <DefineConstants>{3}</DefineConstants>",
                @"    <ErrorReport>prompt</ErrorReport>",
                @"    <WarningLevel>4</WarningLevel>",
                @"    <NoWarn>0169</NoWarn>",
                @"    <AllowUnsafeBlocks>{10}</AllowUnsafeBlocks>",
                @"  </PropertyGroup>",
                @"  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">",
                @"    <DebugType>pdbonly</DebugType>",
                @"    <Optimize>true</Optimize>",
                @"    <OutputPath>Temp\bin\Release\</OutputPath>",
                @"    <ErrorReport>prompt</ErrorReport>",
                @"    <WarningLevel>4</WarningLevel>",
                @"    <NoWarn>0169</NoWarn>",
                @"    <AllowUnsafeBlocks>{10}</AllowUnsafeBlocks>",
                @"  </PropertyGroup>"
            };

            var forceExplicitReferences = new[]
            {
                @"  <PropertyGroup>",
                @"    <NoConfig>true</NoConfig>",
                @"    <NoStdLib>true</NoStdLib>",
                @"    <AddAdditionalExplicitAssemblyReferences>false</AddAdditionalExplicitAssemblyReferences>",
                @"    <ImplicitlyExpandNETStandardFacades>false</ImplicitlyExpandNETStandardFacades>",
                @"    <ImplicitlyExpandDesignTimeFacades>false</ImplicitlyExpandDesignTimeFacades>",
                @"  </PropertyGroup>"
            };

            var itemGroupStart = new[]
            {
                @"  <ItemGroup>",
                @""
            };

            var text = header.Concat(forceExplicitReferences).Concat(itemGroupStart).ToArray();
            return string.Join("\r\n", text);
        }

        void SyncSolution(IEnumerable<Assembly> islands)
        {
            SyncSolutionFileIfNotChanged(SolutionFile(), SolutionText(islands));
        }

        string SolutionText(IEnumerable<Assembly> islands)
        {
            var fileversion = "11.00";
            var vsversion = "2010";

            var relevantIslands = RelevantIslandsForMode(islands);
            string projectEntries = GetProjectEntries(relevantIslands);
            string projectConfigurations = string.Join(k_WindowsNewline, relevantIslands.Select(i => GetProjectActiveConfigurations(ProjectGuid(i.name))).ToArray());
            return string.Format(GetSolutionText(), fileversion, vsversion, projectEntries, projectConfigurations);
        }

        static IEnumerable<Assembly> RelevantIslandsForMode(IEnumerable<Assembly> islands)
        {
            IEnumerable<Assembly> relevantIslands = islands.Where(i => ScriptingLanguage.CSharp == ScriptingLanguageFor(i));
            return relevantIslands;
        }

        /// <summary>
        /// Get a Project("{guid}") = "MyProject", "MyProject.csproj", "{projectguid}"
        /// entry for each relevant language
        /// </summary>
        string GetProjectEntries(IEnumerable<Assembly> islands)
        {
            var projectEntries = islands.Select(i => string.Format(
                m_SolutionProjectEntryTemplate,
                SolutionGuid(i),
                i.name,
                Path.GetFileName(ProjectFile(i)),
                ProjectGuid(i.name)
            ));

            return string.Join(k_WindowsNewline, projectEntries.ToArray());
        }

        /// <summary>
        /// Generate the active configuration string for a given project guid
        /// </summary>
        string GetProjectActiveConfigurations(string projectGuid)
        {
            return string.Format(
                m_SolutionProjectConfigurationTemplate,
                projectGuid);
        }

        string EscapedRelativePathFor(string file)
        {
            var projectDir = ProjectDirectory.Replace('/', '\\');
            file = file.Replace('/', '\\');
            var path = SkipPathPrefix(file, projectDir);

            var packageInfo = m_AssemblyNameProvider.FindForAssetPath(path.Replace('\\', '/'));
            if (packageInfo != null)
            {
                // We have to normalize the path, because the PackageManagerRemapper assumes
                // dir seperators will be os specific.
                var absolutePath = Path.GetFullPath(NormalizePath(path)).Replace('/', '\\');
                path = SkipPathPrefix(absolutePath, projectDir);
            }

            return SecurityElement.Escape(path);
        }

        static string SkipPathPrefix(string path, string prefix)
        {
            if (path.StartsWith($@"{prefix}\"))
                return path.Substring(prefix.Length + 1);
            return path;
        }

        static string NormalizePath(string path)
        {
            if (Path.DirectorySeparatorChar == '\\')
                return path.Replace('/', Path.DirectorySeparatorChar);
            return path.Replace('\\', Path.DirectorySeparatorChar);
        }

        string ProjectGuid(string assembly)
        {
            return m_GUIDProvider.ProjectGuid(m_ProjectName, assembly);
        }

        string SolutionGuid(Assembly island)
        {
            return m_GUIDProvider.SolutionGuid(m_ProjectName, GetExtensionOfSourceFiles(island.sourceFiles));
        }

        static string ProjectFooter()
        {
            return GetProjectFooterTemplate();
        }

        static string GetProjectExtension()
        {
            return ".csproj";
        }

        void WriteVSCodeSettingsFiles()
        {
            var vsCodeDirectory = Path.Combine(ProjectDirectory, ".vscode");

            if (!m_FileIOProvider.Exists(vsCodeDirectory))
                m_FileIOProvider.CreateDirectory(vsCodeDirectory);

            var vsCodeSettingsJson = Path.Combine(vsCodeDirectory, "settings.json");

            if (!m_FileIOProvider.Exists(vsCodeSettingsJson))
                m_FileIOProvider.WriteAllText(vsCodeSettingsJson, k_SettingsJson);
        }
    }

    public static class SolutionGuidGenerator
    {
        public static string GuidForProject(string projectName)
        {
            return ComputeGuidHashFor(projectName + "salt");
        }

        public static string GuidForSolution(string projectName, string sourceFileExtension)
        {
            if (sourceFileExtension.ToLower() == "cs")

                // GUID for a C# class library: http://www.codeproject.com/Reference/720512/List-of-Visual-Studio-Project-Type-GUIDs
                return "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";

            return ComputeGuidHashFor(projectName);
        }

        static string ComputeGuidHashFor(string input)
        {
            var hash = MD5.Create().ComputeHash(Encoding.Default.GetBytes(input));
            return HashAsGuid(HashToString(hash));
        }

        static string HashAsGuid(string hash)
        {
            var guid = hash.Substring(0, 8) + "-" + hash.Substring(8, 4) + "-" + hash.Substring(12, 4) + "-" + hash.Substring(16, 4) + "-" + hash.Substring(20, 12);
            return guid.ToUpper();
        }

        static string HashToString(byte[] bs)
        {
            var sb = new StringBuilder();
            foreach (byte b in bs)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
