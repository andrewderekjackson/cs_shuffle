using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Shuffle {
    class Program {

        static void Main(string[] args) {

            //ModuleBuilder env = new ModuleBuilder() {
            //    ExpertShare = @"C:\ExpertShare",
            //};

            //foreach (var dir in Directory.GetDirectories(@"c:\source")) {
            //    env.Sources.Add(dir);
            //}
            //env.Sources.Add(@"C:\tfs\ExpertSuite\Dev\vnext\Modules\Services.Query");

            //env.Build();

            
            var worker = new WorkerQueue();
            worker.Start();

            // LoadPlan();

            IDisposable watches = new CompositeDisposable(CreateWatches(worker));

            bool quit = false;

            while (!quit) {
                Console.WriteLine("Listening for changes... (f) Regenerate factory, (q) to quit.");

                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Q) {
                    Console.WriteLine("Cleaning up an quiting.");
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

        private static void RegenerateFactory(string target) {

            var file = $"c:\\expertshare\\FactoryResourceGenerator.exe";
            var arguments = $"/f:{target} /sp:\"*.dll,*.exe\" /of:{target}\\factory.bin";

            Console.WriteLine($"Executing: {file} {arguments}");

            var ps = new ProcessStartInfo(file, arguments);
            Process.Start(ps);

        }

        private static Plan LoadPlan() {
            var contents = File.ReadAllText("config.yaml");

            var deserializer = new DeserializerBuilder().Build();
            var plan = deserializer.Deserialize<Plan>(contents);

            return plan;
        }

        private static IEnumerable<IDisposable> CreateWatches(WorkerQueue worker) {

            Target ExpertShare = new Target("ExpertShare", @"c:\expertshare");
            Target SharedBin = new Target("SharedBin", @"C:\AderantExpert\Local\SharedBin");

            // module definitions

            var database = new Module() {
                Name = "Database",
                Path = @"C:\Source\Database",
                NuGetPackageNames = {
                    "Aderant.Database",
                    "Aderant.Libraries.Models"
                }
            };

            var framework = new Module() {
                Name = "Framework",
                Path = @"C:\Source\ExpertSuite\Framework",
                NuGetPackageNames = {
                    "Aderant.Framework.Core"
                }
            };

            var presentation = new Module() {
                Name = "Presentation",
                Path = @"C:\Source\Presentation",
                NuGetPackageNames = {
                    "Aderant.Presentation.Core"
                }
            };

            var customization = new Module() {
                Name = "Customization",
                Path = @"C:\Source\ExpertSuite\Customization",
                NuGetPackageNames = {
                    "Aderant.Customization",
                    "Aderant.SystemMapBuilder"
                }
            };

            var billing = new Module() {
                Name = "Billing",
                Path = @"C:\Source\Billing",
                NuGetPackageNames = {
                    "Aderant.TaskBasedBilling.Core",
                    "Aderant.Billing.Service",
                    "Aderant.Billing",
                    "Aderant.Billing.Core",
                    "Aderant.Billing.Client"
                }
            };

            var expenses = new Module() {
                Name = "Expenses",
                Path = @"C:\Source\AccountsPayable",
                NuGetPackageNames = {
                }
            };

            var inquiries = new Module() {
                Name = "Inquiries",
                Path = @"C:\Source\Inquiries",
                NuGetPackageNames = {
                    "Aderant.Inquiries",
                }
            };

            var appUnderTest = expenses;


            var query = new TfsModule() {
                Name = "Query",
                Path = @"C:\tfs\ExpertSuite\Dev\vnext\Modules\Services.Query",
            };

            var firmService = new TfsModule() {
                Name = "FirmService",
                Path = @"C:\tfs\ExpertSuite\Dev\vnext\Modules\Services.Applications.FirmControl",
            };

            var firmPresentation = new TfsModule() {
                Name = "FirmPresentation",
                Path = @"C:\tfs\ExpertSuite\Dev\vnext\Modules\Libraries.Presentation.Firm",
            };

            var securityService = new TfsModule() {
                Name = "SecurityService",
                Path = @"C:\tfs\ExpertSuite\Dev\vnext\Modules\Services.Security",
            };

            yield return Shuffle
                .Pipeline("Framework")
                .From(framework)
                .To(query, presentation, customization, appUnderTest, firmService,firmPresentation,securityService)
                .To(ExpertShare)
                .To(SharedBin)
                .Subscribe(worker);

            yield return Shuffle
                .Pipeline("Presentation")
                .From(presentation)
                .To(query, customization, appUnderTest, firmService)
                .To(ExpertShare)
                .Subscribe(worker);

            yield return Shuffle
                .Pipeline("FirmService")
                .From(firmService, @"Aderant.FirmControl*.dll",@"Aderant.FirmControl*.pdb")
                .To(ExpertShare, customization, appUnderTest, SharedBin, firmPresentation)
                .Subscribe(worker);

            yield return Shuffle
                .Pipeline("FirmPresentation")
                .From(firmPresentation, @"Aderant.Framework.Presentation.Firm*.dll",@"Aderant.Framework.Presentation.Firm*.pdb")
                .To(ExpertShare, SharedBin, appUnderTest)
                .Subscribe(worker);

            yield return Shuffle
                .Pipeline("Query Service")
                .From(query, @"Aderant.Query*.dll",@"Aderant.Query*.pdb")
                .To(customization, appUnderTest)
                .To(ExpertShare)
                .To(SharedBin)
                .Subscribe(worker);
            
            yield return Shuffle
                .Pipeline("Customization")
                .From(customization)
                .To(billing, inquiries,appUnderTest)
                .To(ExpertShare)
                .To(SharedBin)
                .Subscribe(worker);

            yield return Shuffle
                .Pipeline("Security")
                .From(securityService)
                .To(ExpertShare)
                .To(SharedBin)
                .Subscribe(worker);

            yield return Shuffle
                .Pipeline("Billing")
                .From(billing, @"*.dll", @"*.pdb")
                .To(ExpertShare)
                //.To(SharedBin)
                .Subscribe(worker);

            yield return Shuffle
                .Pipeline(appUnderTest.Name)
                .From(appUnderTest, @"*.dll", @"*.pdb")
                .To(ExpertShare)
                //.To(SharedBin)
                .Subscribe(worker);

            //yield return Shuffle
            //    .Pipeline("Inquiries")
            //    .From(inquiries, @"*.*")
            //    .To(ExpertShare)
            //    .Subscribe(worker);



            //var serializer = new SerializerBuilder().Build();
            //var yaml = serializer.Serialize(plan);
            //File.WriteAllText(@"c:\source\cs_shuffle\Shuffle\config.yaml", yaml);


        }

    }

    
}
