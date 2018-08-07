using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shuffle;

namespace Tests {
    [TestClass]
    public class ModuleTests {

        [TestMethod]
        public void ShouldReturnABinPath() {
            
            Module module = new Module();
            module.Path = @"C:\TestModule";

            Assert.AreEqual(@"C:\TestModule\Bin\Module", module.BinPath);
            
        }

        [TestMethod]
        public void ModuleToModule() {

            Module sourceModule = new Module {
                Path = @"C:\Module2"
            };
            sourceModule.NuGetPackageNames.Add("Package1");
            sourceModule.NuGetPackageNames.Add("Package2");


            Module targetModule = new Module {
                Path = @"C:\Module1"
            };
            
            var paths = sourceModule.GetTargetPaths(targetModule).ToList();
            Assert.IsTrue(paths.Contains(@"\packages\Package1\lib"));
            Assert.IsTrue(paths.Contains(@"\packages\Package2\lib"));

        }

        [TestMethod]
        public void ModuleToTfsModule() {

            Module sourceModule = new Module {
                Path = @"C:\Module2"
            };
            sourceModule.NuGetPackageNames.Add("Package1");
            sourceModule.NuGetPackageNames.Add("Package2");


            TfsModule targetModule = new TfsModule {
                Path = @"C:\Module1"
            };
            
            var paths = sourceModule.GetTargetPaths(targetModule).ToList();
            Assert.IsTrue(paths.Contains(@"\packages\Package1\lib"));
            Assert.IsTrue(paths.Contains(@"\packages\Package2\lib"));

        }

        [TestMethod]
        public void ModuleToTargetPath() {

            Module sourceModule = new Module {
                Path = @"C:\Module2"
            };
            sourceModule.NuGetPackageNames.Add("Package1");
            sourceModule.NuGetPackageNames.Add("Package2");
            
            Target target = new Target("ExpertShare", @"c:\ExpertShare");
            
            var paths = sourceModule.GetTargetPaths(target).ToList();
            Assert.IsTrue(paths.Contains(@""));

        }


        [TestMethod]
        public void TfsModuleToModule() {

            TfsModule sourceModule = new TfsModule {
                Path = @"C:\Module2"
            };

            Module targetModule = new Module {
                Path = @"C:\Module1"
            };
            targetModule.NuGetPackageNames.Add("Package1");
            targetModule.NuGetPackageNames.Add("Package2");

            var paths = sourceModule.GetTargetPaths(targetModule).ToList();
            Assert.IsTrue(paths.Contains(@"\Dependencies"));

        }

        [TestMethod]
        public void TfsModuleToTargetPath() {

            TfsModule sourceModule = new TfsModule {
                Path = @"C:\Module2"
            };

            Target target = new Target("ExpertShare", @"c:\ExpertShare");

            var paths = sourceModule.GetTargetPaths(target).ToList();
            Assert.IsTrue(paths.Contains(@""));

        }

       
    }
}
