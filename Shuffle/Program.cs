using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using YamlDotNet.Serialization;

namespace Shuffle {
    class Program {

        static void Main(string[] args) {
            
            var banner =
@"  _________.__            _____  _____.__          
 /   _____/|  |__  __ ___/ ____\/ ____\  |   ____  
 \_____  \ |  |  \|  |  \   __\\   __\|  | _/ __ \ 
 /        \|   Y  \  |  /|  |   |  |  |  |_\  ___/ 
/_______  /|___|  /____/ |__|   |__|  |____/\___  >
        \/      \/                              \/ ";

            Console.WriteLine("Lets play the ADERANT module");
            Console.WriteLine(banner);
            Console.WriteLine();

            var moduleLocations = new string[] {
                @"c:\Source",
                @"C:\tfs\ExpertSuite\Dev\vnext\Modules"
            };

            var additionalTargets = new Target[] {
                new Target("ExpertShare", @"c:\expertshare"),
                new Target("SharedBin", @"C:\AderantExpert\Local\SharedBin")
            };

            var workingFolders = new string[] {
                "Framework",
                "Presentation",
                "Customization",
                "Services.Applications.FirmControl",
                "Services.Query"
            };
            
            var worker = new WorkerQueue();

            IDisposable watches = new CompositeDisposable(DiscoverWatches(worker, moduleLocations, additionalTargets, workingFolders));

            worker.Start();

            bool quit = false;

            while (!quit) {
                Console.WriteLine("Listening for changes... (f) Regenerate factory, (q) to quit.");

                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Q) {
                    Console.WriteLine("Cleaning up and quiting.");
                    quit = true;
                }

                if (key.Key == ConsoleKey.F) {
                    Console.WriteLine("Regenerate factory for (e) ExpertShare (s) SharedBin or (b) Both?");
                    var choice = Console.ReadKey(true);
                    if (choice.Key == ConsoleKey.E || choice.Key == ConsoleKey.B) {
                        RegenerateFactory(@"c:\ExpertShare");
                    }

                    if (choice.Key == ConsoleKey.S || choice.Key == ConsoleKey.B) {
                        RegenerateFactory(@"c:\AderantExpert\Local\SharedBin");
                    }
                    
                }
            }
            
            watches.Dispose();
            worker.Stop();

        }

        

        private static IEnumerable<IDisposable> DiscoverWatches(WorkerQueue worker, string[] moduleLocations, Target[] additionalTargets, string[] workingFolders) {

            var modules = new List<Module>();

            foreach (var moduleLocation in moduleLocations) {
                Console.WriteLine("Discovering modules at: " + moduleLocation);
                modules.AddRange(FindModules(moduleLocation));
            }

            Console.WriteLine($"Autodiscovered {modules.Count} modules." );

            // first pass, cull any modules not provided by working directories
            List<string> workingModules = new List<string>();
            foreach (var workingFolder in workingFolders) {
                var sourceModule = modules.FirstOrDefault(v => v.Name == workingFolder);
                if (sourceModule == null) {
                    continue;
                }
                workingModules.AddRange(sourceModule.Provides);
            }

            foreach (var module in modules) {
                foreach (var providedModule in module.Provides.ToList()) {
                    if (!workingModules.Contains(providedModule)) {
                        module.Provides.Remove(providedModule);
                    }
                }

                foreach (var requiresModule in module.Requires.ToList()) {
                    if (!workingModules.Contains(requiresModule)) {
                        module.Requires.Remove(requiresModule);
                    }
                }
            }

            Console.WriteLine("Setting up module pipelines for modules in working set:");

            // figure out dependencies and set up watches
            foreach (var workingDirectory in workingFolders) {

                var sourceModule = modules.FirstOrDefault(v => v.Name == workingDirectory);
                if (sourceModule == null) {
                    continue;
                }

                //Console.WriteLine($"{sourceModule.Name}");
                //Console.WriteLine($" * Provides: {String.Join(",", sourceModule.Provides)}");
                //Console.WriteLine($" * Requires: {String.Join(",", sourceModule.Requires)}");
                
                // find modules which require anything this module provides
                var targetModules = modules
                    .Where(t => sourceModule.Provides.Any(s => t.Requires.Contains(s)))
                    .Where(t => workingFolders.Contains(t.Name))
                    .ToList();

                // set up watches.
                yield return Shuffle
                    .Pipeline(workingDirectory)
                    .From(sourceModule)
                    .To(targetModules.OfType<PipelineObject>().ToArray())
                    .To(additionalTargets.OfType<PipelineObject>().ToArray())
                    .Subscribe(worker);
            }
            
        }

        private static IEnumerable<Module> FindModules(string branchRoot, int currentLevel = 0) {

            if (currentLevel > 1) {
                yield break;
            }

            foreach (var dir in Directory.EnumerateDirectories(branchRoot)) {

                var projPath = Path.Combine(dir, "Build", "TFSBuild.proj");
                if (File.Exists(projPath)) {

                    var nugetOutput = new List<string>();
                    var input = new List<string>();

                    // we have a build file - are we a git module
                    foreach (var templateFile in Directory.GetFiles(dir, "*.paket.template")) {

                        // dirty parser
                        var templateLines = File.ReadAllLines(templateFile);
                        foreach (var line in templateLines) {
                            if (line == null) {
                                continue;
                            }
                            if (line.Trim().StartsWith("id")) {
                                try {
                                    var output = line.Split(' ')[1];
                                    nugetOutput.Add(output);
                                }
                                catch (Exception) {
                                    continue;
                                }
                            }
                        }
                    }

                    var manifest = Path.Combine(dir, "Build", "DependencyManifest.xml");
                    if (File.Exists(manifest)) {
                        var dependencyManifest = XDocument.Load(manifest);
                        foreach (var node in dependencyManifest.Descendants("ReferencedModule")) {
                            input.Add(node.Attribute("Name")?.Value);
                        }
                    }

                    var paketDependencies = Path.Combine(dir, "paket.dependencies");
                    if (File.Exists(paketDependencies)) {
                            
                        foreach (var line in File.ReadAllLines(paketDependencies)) {
                            if (line == null) {
                                continue;
                            }
                            if (line.Trim().StartsWith("nuget")) {
                                try {
                                    var paketModuleName = line.Split(' ')[1].Trim();
                                    input.Add(paketModuleName);
                                }
                                catch (Exception) {
                                    continue;
                                }
                            }
                        }
                    }

                    var modulePath = dir;
                    var moduleName = Path.GetFileName(dir);
                    if (nugetOutput.Any()) {
                        yield return new Module() {
                            Path = modulePath,
                            Name = moduleName,
                            Provides = nugetOutput,
                            Requires = input.Distinct().ToList()
                        };
                        // git
                    } else {
                        // tfvc
                        yield return new TfsModule() {
                            Path = modulePath,
                            Name = moduleName,
                            Provides = { moduleName },
                            Requires = input.Distinct().ToList()
                        };
                    }
                    
                }
                else {
                    // not a module directory - decend.
                    foreach (var module in FindModules(dir, currentLevel+1)) {
                        yield return module;
                    }
                }
            }

        }


        private static void RegenerateFactory(string target) {

            var file = $"c:\\expertshare\\FactoryResourceGenerator.exe";
            var arguments = $"/f:{target} /sp:\"*.dll,*.exe\" /of:{target}\\factory.bin";

            Console.WriteLine($"Executing: {file} {arguments}");

            var ps = new ProcessStartInfo(file, arguments);
            Process.Start(ps);

        }
        
    }

    
}
