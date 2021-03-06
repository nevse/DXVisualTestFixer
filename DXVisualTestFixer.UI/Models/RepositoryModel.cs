﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Native;
using JetBrains.Annotations;
using Microsoft.Practices.ServiceLocation;

namespace DXVisualTestFixer.UI.Models {
	public class RepositoryModel : BindableBase {
		readonly Dispatcher _dispatcher;
		readonly IPlatformInfo _platform;
		
		public readonly Repository Repository;

		public RepositoryModel(Repository source, IPlatformInfo platform) {
			Repository = source;
			Version = Repository.Version;
			Path = Repository.Path;
			_dispatcher = Dispatcher.CurrentDispatcher;
			var platformProvider = ServiceLocator.Current.GetInstance<IPlatformProvider>();
			_platform = platform;
			UpdateDownloadState();
		}
		
		public string Version {
			get { return GetProperty(() => Version); }
			set { SetProperty(() => Version, value, OnVersionChanged); }
		}

		public string Path {
			get { return GetProperty(() => Path); }
			set { SetProperty(() => Path, value, OnPathChanged); }
		}

		public DownloadState DownloadState {
			get { return GetProperty(() => DownloadState); }
			set { SetProperty(() => DownloadState, value); }
		}

		void OnVersionChanged() {
			Repository.Version = Version;
		}

		void OnPathChanged() {
			Repository.Path = Path;
		}

		public static void ActualizeRepositories(IPlatformInfo platform, ICollection<RepositoryModel> repositories, string filePath) {
			var savedVersions = repositories.Select(r => r.Version).ToList();
			foreach(var ver in RepositoryLoader.GetVersions(platform).Where(v => !savedVersions.Contains(v)))
			foreach(var directoryPath in Directory.GetDirectories(filePath)) {
				var dirName = System.IO.Path.GetFileName(directoryPath);
				var localPath = string.Format(platform.LocalPath, ver);
				if(dirName != localPath) continue;
				if(!File.Exists(directoryPath + "\\VisualTestsConfig.xml"))
					continue;
				var repository = new RepositoryModel(Repository.CreateRegular(platform.Name, ver, directoryPath + "\\"), platform); 
				repositories.Add(repository);
				InitializeBinIfNeed(repository.Path, repository.Version);
			}
		}

		public static void InitializeBinIfNeed(string repositoryPath, string version) {
			static bool TryCreateDirectoryLink(string workPath, string targetPath) {
				if(!Directory.Exists(workPath)) return false;
				FileSystemHelper.Create(workPath, targetPath);
				return true;
			}

			var workPaths = new[] {
				System.IO.Path.Combine(repositoryPath, "..", version, "Bin"),
				System.IO.Path.Combine(repositoryPath, "..", $"20{version}", "Bin"),
				System.IO.Path.Combine("c:\\Work", version, "Bin"),
				System.IO.Path.Combine("c:\\Work", $"20{version}", "Bin"),
				System.IO.Path.Combine("d:\\Work", version, "Bin"),
				System.IO.Path.Combine("d:\\Work", $"20{version}", "Bin")
			};
			
			if(!workPaths.Any(Directory.Exists))
				return;
			
			var binPath = System.IO.Path.Combine(repositoryPath, "Bin");
			if(Directory.Exists(binPath)) {
				var pathInfo = new FileInfo(binPath);
				if(pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
					return;
				var oldBinPath = System.IO.Path.Combine(repositoryPath, "Bin_old");
				if(Directory.Exists(oldBinPath))
					return;
				try {
					Directory.Move(binPath, oldBinPath);
				}
				catch {
					return;
				}
			}

			foreach(var workPath in workPaths)
				if(TryCreateDirectoryLink(workPath, binPath)) return;	
		}
		
		

		public void UpdateDownloadState() {
			DownloadState = GetDownloadState();
		}

		DownloadState GetDownloadState() {
			if(!Directory.Exists(Path) || !Directory.EnumerateFileSystemEntries(Path).Any())
				return DownloadState.ReadyToDownload;
			return File.Exists(System.IO.Path.Combine(Path, ".gitlab-ci.yml")) ? DownloadState.Downloaded : DownloadState.CanNotDownload;
		}

		[UsedImplicitly]
		public void Download() {
			if(DownloadState == DownloadState.ReadyToDownload) Task.Factory.StartNew(DownloadAsync);
		}

		async Task DownloadAsync() {
			await _dispatcher.BeginInvoke(new Action(() => { DownloadState = DownloadState.Downloading; }));
			var git = ServiceLocator.Current.GetInstance<IGitWorker>();
			if(!await git.Clone(_platform.GitRepository, Repository)) {
				await _dispatcher.BeginInvoke(new Action(() => { DownloadState = DownloadState.CanNotDownload; }));
				return;
			}
			InitializeBinIfNeed(Repository.Path, Repository.Version);
			await _dispatcher.BeginInvoke(new Action(() => { DownloadState = DownloadState.Downloaded; }));
		}
	}

	public enum DownloadState {
		ReadyToDownload,
		Downloading,
		Downloaded,
		CanNotDownload
	}
}