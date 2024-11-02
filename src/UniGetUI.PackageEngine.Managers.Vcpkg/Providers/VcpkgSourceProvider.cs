using UniGetUI.Core.SettingsEngine;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.VcpkgManager
{
    internal sealed class VcpkgSourceProvider : BaseSourceProvider<PackageManager>
    {
        public VcpkgSourceProvider(Vcpkg manager) : base(manager) { }

		public override string[] GetAddSourceParameters(IManagerSource source)
        {
            return [];
        }

        public override string[] GetRemoveSourceParameters(IManagerSource source)
        {
            return [];
        }

        public override OperationVeredict GetAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            var (vcpkgRootFound, vcpkgRoot) = GetVcpkgRoot();
            string tripletLocation = Path.Join(vcpkgRoot, "triplets");
            string communityTripletLocation = Path.Join(vcpkgRoot, "triplets", "community");
            return vcpkgRootFound && (File.Exists(Path.Join(tripletLocation, source.Name + ".cmake")) ||
                File.Exists(Path.Join(communityTripletLocation, source.Name + ".cmake"))) ?
                OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return OperationVeredict.Succeeded;
        }

        protected override IEnumerable<IManagerSource> GetSources_UnSafe()
        {
            List<ManagerSource> Sources = [];
            // Retrieve all triplets on the system (in %VCPKG_ROOT%\triplets{\community})
            var (vcpkgRootFound, vcpkgRoot) = GetVcpkgRoot();
            if (vcpkgRootFound)
            {
                string tripletLocation = Path.Join(vcpkgRoot, "triplets");
                string communityTripletLocation = Path.Join(vcpkgRoot, "triplets", "community");

                foreach (string tripletFile in Directory.EnumerateFiles(tripletLocation).Concat(Directory.EnumerateFiles(communityTripletLocation)))
                {
                    string triplet = Path.GetFileNameWithoutExtension(tripletFile);
                    Sources.Add(new ManagerSource(Manager, triplet, URI_VCPKG_IO));
                }
            }

            return Sources;
        }

        public static Uri URI_VCPKG_IO = new Uri("https://vcpkg.io/");
        private Tuple<bool, string> GetVcpkgRoot()
        {
            string? vcpkgRoot = Settings.GetValue("CustomVcpkgRoot");
            if (vcpkgRoot == "")
            {
                vcpkgRoot = Environment.GetEnvironmentVariable("VCPKG_ROOT");
            }

            return Tuple.Create(vcpkgRoot != null, vcpkgRoot ?? "");
        }
    }
}
