﻿using BedrockLauncher.Classes;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BedrockLauncher.UpdateProcessor;
using System.Linq;
using ExtensionsDotNET;
using BedrockLauncher.Core.Classes;
using BedrockLauncher.ViewModels;
using System.Collections.ObjectModel;

namespace BedrockLauncher.Downloaders
{
    public class VersionDownloader
    {
        private HttpClient _client = new HttpClient();
        private Win10StoreNetwork _store_manager = new Win10StoreNetwork();

        private string cacheFile => LauncherModel.Default.FilepathManager.GetVersionsFilePath();
        private string userCacheFile => LauncherModel.Default.FilepathManager.GetUserVersionsFilePath();
        private string technicalUserCacheFile => LauncherModel.Default.FilepathManager.GetUserVersionsTechnicalFilePath();

        public void EnableUserAuthorization()
        {
            _store_manager.setMSAUserToken(Win10AuthenticationManager.GetWUToken(Properties.LauncherSettings.Default.CurrentInsiderAccount));
        }
        public void PraseDB(ObservableCollection<BLVersion> list, Win10VersionDBManager.Win10VersionJsonDb db)
        {
            foreach (var v in db.list)
            {
                if (!list.ToList().Exists(x => x.UUID == v.uuid || x.Name == v.version)) list.Add(new BLVersion(v.uuid, v.version, v.isBeta));
            }
            list.Sort((x, y) => Compare(x, y));

            int Compare(MCVersion x, MCVersion y)
            {
                try
                {
                    var a = new Version(x.Name);
                    var b = new Version(y.Name);
                    return b.CompareTo(a);
                }
                catch
                {
                    return y.Name.CompareTo(x.Name);
                }

            }
        }

        private async Task DownloadFile(string url, string to, DownloadProgress progress, CancellationToken cancellationToken)
        {
            using (var resp = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                using (var inStream = await resp.Content.ReadAsStreamAsync())
                using (var outStream = new FileStream(to, FileMode.Create))
                {
                    long? totalSize = resp.Content.Headers.ContentLength;
                    progress(0, totalSize);
                    long transferred = 0;
                    byte[] buf = new byte[1024 * 1024];
                    while (true)
                    {
                        int n = await inStream.ReadAsync(buf, 0, buf.Length, cancellationToken);
                        if (n == 0)
                            break;
                        await outStream.WriteAsync(buf, 0, n, cancellationToken);
                        transferred += n;
                        progress(transferred, totalSize);
                    }
                }
            }
        }
        public async Task Download(string versionName, string updateIdentity, int revisionNumber, string destination, DownloadProgress progress, CancellationToken cancellationToken)
        {
            string link = await _store_manager.getDownloadLink(updateIdentity, revisionNumber, true);
            if (link == null)
                throw new ArgumentException(string.Format("Bad updateIdentity for {0}", versionName));
            System.Diagnostics.Debug.WriteLine("Resolved download link: " + link);
            await DownloadFile(link, destination, progress, cancellationToken);
        }


        private int SetMSAUserToken()
        {
            try
            {
                _store_manager.setMSAUserToken(Win10AuthenticationManager.GetWUToken(Properties.LauncherSettings.Default.CurrentInsiderAccount));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error while Authenticating UserToken for Version Fetching:\n" + ex);
            }
            return 1;
        }

        public async Task UpdateVersions(ObservableCollection<BLVersion> versions)
        {
            await ThreadingExtensions.StartSTATask<int>(() => SetMSAUserToken());
            versions.Clear();

            LoadFromLocalCache(versions);
            await LoadFromURL(versions);
            LoadFromUserCache(versions);
            await LoadFromAPI(versions);
            await LoadFromAPI_Technical();
        }

        public Win10VersionDBManager.Win10VersionJsonDb LoadFromUserCache(ObservableCollection<BLVersion> versions)
        {
            try
            {
                Win10VersionDBManager.Win10VersionJsonDb db = new Win10VersionDBManager.Win10VersionJsonDb();
                db.ReadJson(userCacheFile);
                PraseDB(versions, db);
                return db;
            }
            catch (FileNotFoundException e)
            {
                // ignore
                System.Diagnostics.Debug.WriteLine("Version list user cache load failed:\n" + e.ToString());
                return new Win10VersionDBManager.Win10VersionJsonDb();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Version list user cache load failed:\n" + e.ToString());
                return new Win10VersionDBManager.Win10VersionJsonDb();
            }
        }
        public Win10VersionDBManager.Win10VersionJsonDb LoadFromLocalCache(ObservableCollection<BLVersion> versions)
        {
            try
            {
                Win10VersionDBManager.Win10VersionJsonDb db = new Win10VersionDBManager.Win10VersionJsonDb();
                db.ReadJson(cacheFile);
                PraseDB(versions, db);
                return db;
            }
            catch (FileNotFoundException e)
            {
                // ignore
                System.Diagnostics.Debug.WriteLine("Version list local cache load failed:\n" + e.ToString());
                return new Win10VersionDBManager.Win10VersionJsonDb();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Version list local cache load failed:\n" + e.ToString());
                return new Win10VersionDBManager.Win10VersionJsonDb();
            }
        }
        public Win10VersionDBManager.Win10VersionTextDb LoadFromTechCache()
        {
            try
            {
                Win10VersionDBManager.Win10VersionTextDb db = new Win10VersionDBManager.Win10VersionTextDb();
                db.Read(technicalUserCacheFile);
                return db;
            }
            catch (FileNotFoundException e)
            {
                // ignore
                System.Diagnostics.Debug.WriteLine("Version list technical cache load failed:\n" + e.ToString());
                return new Win10VersionDBManager.Win10VersionTextDb();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Version list technical cache load failed:\n" + e.ToString());
                return new Win10VersionDBManager.Win10VersionTextDb();
            }
        }

        public async Task LoadFromAPI_Technical()
        {
            try
            {
                var db = LoadFromTechCache();
                var config = await _store_manager.fetchConfigLastChanged();
                var cookie = await _store_manager.fetchCookie(config, false);
                var knownVersions = db.releaseList.ToList().ConvertAll(x => x.uuid);
                db.AddVersion(await Win10StoreManager.CheckForVersions(_store_manager, cookie, knownVersions, false), false);
                db.Write(technicalUserCacheFile);
                config = await _store_manager.fetchConfigLastChanged();
                cookie = await _store_manager.fetchCookie(config, true);
                knownVersions = db.betaList.ToList().ConvertAll(x => x.uuid);
                db.AddVersion(await Win10StoreManager.CheckForVersions(_store_manager, cookie, knownVersions, true), true);
                db.Write(technicalUserCacheFile);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Version list update check failed:\n" + e.ToString());
            }
        }
        public async Task LoadFromAPI(ObservableCollection<BLVersion> versions)
        {
            try
            {
                var db = LoadFromUserCache(versions);
                var config = await _store_manager.fetchConfigLastChanged();
                var cookie = await _store_manager.fetchCookie(config, false);
                var knownVersions = db.list.ToList().ConvertAll(x => x.uuid);
                db.AddVersion(await Win10StoreManager.CheckForVersions(_store_manager, cookie, knownVersions, false), false);
                db.WriteJson(userCacheFile);
                PraseDB(versions, db);
                config = await _store_manager.fetchConfigLastChanged();
                cookie = await _store_manager.fetchCookie(config, true);
                knownVersions = db.list.ToList().ConvertAll(x => x.uuid);
                db.AddVersion(await Win10StoreManager.CheckForVersions(_store_manager, cookie, knownVersions, true), true);
                db.WriteJson(userCacheFile);
                PraseDB(versions, db);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Version list update check failed:\n" + e.ToString());
            }
        }
        public async Task LoadFromURL(ObservableCollection<BLVersion> versions)
        {
            try
            {
                Win10VersionDBManager.Win10VersionJsonDb db = new Win10VersionDBManager.Win10VersionJsonDb();
                var resp = await _client.GetAsync("https://mrarm.io/r/w10-vdb");
                resp.EnsureSuccessStatusCode();
                var data = await resp.Content.ReadAsStringAsync();
                db.PraseJson(data);
                db.WriteJson(cacheFile);
                PraseDB(versions, db);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Version list download failed:\n" + e.ToString());
            }

        }

        public delegate void DownloadProgress(long current, long? total);
    }
}