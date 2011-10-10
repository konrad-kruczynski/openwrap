using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenFileSystem.IO;
using OpenFileSystem.IO.FileSystems.Local;
using OpenWrap.PackageManagement.Exporters;
using OpenWrap.PackageManagement.Exporters.Assemblies;
using OpenWrap.PackageModel;
using OpenWrap.PackageModel.Serialization;
using OpenWrap.Repositories;
using Path = OpenFileSystem.IO.Path;

namespace OpenWrap.PackageManagement.Monitoring
{
    // TODO: Implement file monitoring in the IFileSystem implementation and remove FileSystemEventHandler
    // TODO: Refactor *and* put the code under test, in its current form its evil.
    public class PackageDescriptorMonitor : IPackageDescriptorMonitor
    {
        readonly Dictionary<Path, DescriptorSubscriptions> _notificationClients = new Dictionary<Path, DescriptorSubscriptions>();

        IPackageManager PackageManager
        {
            get { return Services.ServiceLocator.GetService<IPackageManager>(); }
        }

        public void Initialize()
        {
        }

        public void RegisterListener(IFile wrapFile, IPackageRepository projectRepository, IResolvedAssembliesUpdateListener listener)
        {
            if (!wrapFile.Exists)
                return;

            if (projectRepository == null) throw new ArgumentNullException("projectRepository");
            if (listener == null) throw new ArgumentNullException("listener");

            var descriptor = GetDescriptor(wrapFile, projectRepository);
            if (listener.IsLongRunning)
                descriptor.Clients.Add(listener);

            NotifyClient(wrapFile, listener);
        }
        public void UnregisterListener(IResolvedAssembliesUpdateListener listener)
        {
            List<DescriptorSubscriptions> registrationsForClient;
            lock (_notificationClients)
            {
                registrationsForClient = _notificationClients.Values.Where(x => x.Clients.Contains(listener)).ToList();
            }
            foreach (var registraiton in registrationsForClient)
                registraiton.Clients.Remove(listener);
        }

        DescriptorSubscriptions GetDescriptor(IFile wrapPath, IPackageRepository packageRepository)
        {
            lock (_notificationClients)
            {
                DescriptorSubscriptions descriptorSubscriptions;
                if (!_notificationClients.TryGetValue(wrapPath.Path, out descriptorSubscriptions))
                    _notificationClients.Add(wrapPath.Path, descriptorSubscriptions = new DescriptorSubscriptions(wrapPath, packageRepository, HandleWrapFileUpdate));
                return descriptorSubscriptions;
            }
        }

        void HandleWrapFileUpdate(object sender, FileSystemEventArgs e)
        {
            NotifyAllClients(LocalFileSystem.Instance.GetFile(e.FullPath));
        }

        DescriptorSubscriptions GetSubsriptionsFor(IFile wrapPath)
        {
            lock (_notificationClients)
                return !_notificationClients.ContainsKey(wrapPath.Path) ? null : _notificationClients[wrapPath.Path];
        }

        void NotifyAllClients(IFile wrapPath)
        {
            NotifyCore(wrapPath);
        }

        void NotifyClient(IFile wrapPath, IResolvedAssembliesUpdateListener listener)
        {
            NotifyCore(wrapPath, new[] { listener });
        }
        void NotifyCore(IFile wrapPath, IEnumerable<IResolvedAssembliesUpdateListener> listeners = null)
        {

            var subscriptions = GetSubsriptionsFor(wrapPath);
            if (subscriptions == null) return;

            subscriptions.Repository.RefreshPackages();
            var descriptor = new PackageDescriptorReader()
                    .ReadAll(wrapPath.Parent)
                    .Where(x => x.Value.File.Path == wrapPath.Path)
                    .Select(x => x.Value.Value)
                    .SingleOrDefault();

            foreach (var listener in listeners ?? subscriptions.Clients)
            {
                if (descriptor == null)
                    listener.AssembliesError(string.Format("Descriptor for '{0}' has a file name that does not match the package name, or is in an invalid scope. Check the scope of the project is correct.", wrapPath.Path));
                else
                    listener.AssembliesUpdated(PackageManager.GetProjectAssemblyReferences(descriptor, subscriptions.Repository, listener.Environment, false));
            }
        }
        class DescriptorSubscriptions
        {
            public DescriptorSubscriptions(IFile path, IPackageRepository repository, FileSystemEventHandler handler)
            {
                Repository = repository;
                Clients = new List<IResolvedAssembliesUpdateListener>();
                FileSystemWatcher = new FileSystemWatcher(System.IO.Path.GetDirectoryName(path.Path.FullPath), System.IO.Path.GetFileName(path.Path.FullPath))
                {
                    NotifyFilter = NotifyFilters.LastWrite
                };
                FileSystemWatcher.Changed += handler;
                FileSystemWatcher.EnableRaisingEvents = true;
            }

            public List<IResolvedAssembliesUpdateListener> Clients { get; set; }
            public FileSystemWatcher FileSystemWatcher { get; set; }
            public IPackageRepository Repository { get; set; }
        }
    }
}