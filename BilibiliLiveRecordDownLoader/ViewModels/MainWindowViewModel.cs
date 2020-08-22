﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using BilibiliLiveRecordDownLoader.BilibiliApi;
using BilibiliLiveRecordDownLoader.Utils;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReactiveUI;

namespace BilibiliLiveRecordDownLoader.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IDisposable
    {
        #region 字段

        private string _imageUri;
        private string _name;
        private long _uid;
        private long _level;
        private string _diskUsageProgressBarText;
        private double _diskUsageProgressBarValue;
        private long _roomId;
        private long _shortRoomId;
        private long _recordCount;
        private bool _isLiveRecordBusy;
        private bool _triggerLiveRecordListQuery;

        #endregion

        #region 属性

        public string ImageUri
        {
            get => _imageUri;
            set => this.RaiseAndSetIfChanged(ref _imageUri, value);
        }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public long Uid
        {
            get => _uid;
            set => this.RaiseAndSetIfChanged(ref _uid, value);
        }

        public long Level
        {
            get => _level;
            set => this.RaiseAndSetIfChanged(ref _level, value);
        }

        public string DiskUsageProgressBarText
        {
            get => _diskUsageProgressBarText;
            set => this.RaiseAndSetIfChanged(ref _diskUsageProgressBarText, value);
        }

        public double DiskUsageProgressBarValue
        {
            get => _diskUsageProgressBarValue;
            set => this.RaiseAndSetIfChanged(ref _diskUsageProgressBarValue, value);
        }

        public long RoomId
        {
            get => _roomId;
            set => this.RaiseAndSetIfChanged(ref _roomId, value);
        }

        public long ShortRoomId
        {
            get => _shortRoomId;
            set => this.RaiseAndSetIfChanged(ref _shortRoomId, value);
        }

        public long RecordCount
        {
            get => _recordCount;
            set => this.RaiseAndSetIfChanged(ref _recordCount, value);
        }

        public bool IsLiveRecordBusy
        {
            get => _isLiveRecordBusy;
            set => this.RaiseAndSetIfChanged(ref _isLiveRecordBusy, value);
        }

        public bool TriggerLiveRecordListQuery
        {
            get => _triggerLiveRecordListQuery;
            set => this.RaiseAndSetIfChanged(ref _triggerLiveRecordListQuery, value);
        }

        #endregion

        #region Monitor

        private readonly IDisposable _diskMonitor;
        private readonly IDisposable _roomIdMonitor;

        #endregion

        #region Command

        public ReactiveCommand<Unit, Unit> SelectMainDirCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenMainDirCommand { get; }

        #endregion

        public readonly ConfigViewModel Config;
        private readonly ObservableAsPropertyHelper<IEnumerable<LiveRecordListViewModel>> _liveRecordList;
        public IEnumerable<LiveRecordListViewModel> LiveRecordList => _liveRecordList.Value;

        public MainWindowViewModel()
        {
            Config = new ConfigViewModel(Directory.GetCurrentDirectory());
            Config.LoadAsync().NoWarning();

            _roomIdMonitor = this.WhenAnyValue(x => x.Config.RoomId, x => x.TriggerLiveRecordListQuery)
                    .Throttle(TimeSpan.FromMilliseconds(1000))
                    .DistinctUntilChanged()
                    .Where(i => i.Item1 > 0)
                    .Select(i => i.Item1)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(GetAnchorInfo);

            _diskMonitor = Observable.Interval(TimeSpan.FromSeconds(1))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(GetDiskUsage);

            _liveRecordList = this.WhenAnyValue(x => x.Config.RoomId, x => x.TriggerLiveRecordListQuery)
                    .Throttle(TimeSpan.FromMilliseconds(1000))
                    .DistinctUntilChanged()
                    .Where(i => i.Item1 > 0)
                    .Select(i => i.Item1)
                    .SelectMany(GetRecordList)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .ToProperty(this, nameof(LiveRecordList), deferSubscription: true);

            SelectMainDirCommand = ReactiveCommand.Create(SelectDirectory);
            OpenMainDirCommand = ReactiveCommand.Create(OpenDirectory);
        }

        private void SelectDirectory()
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = false,
                Title = @"选择存储目录",
                AddToMostRecentlyUsedList = false,
                EnsurePathExists = true,
                NavigateToShortcut = true,
                InitialDirectory = Config.MainDir
            };
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Config.MainDir = dlg.FileName;
            }
        }

        private void OpenDirectory()
        {
            Utils.Utils.OpenDir(Config.MainDir);
        }

        private void GetDiskUsage(long _)
        {
            var (availableFreeSpace, totalSize) = Utils.Utils.GetDiskUsage(Config.MainDir);
            if (totalSize != 0)
            {
                DiskUsageProgressBarText = $@"已使用 {Utils.Utils.CountSize(totalSize - availableFreeSpace)}/{Utils.Utils.CountSize(totalSize)} 剩余 {Utils.Utils.CountSize(availableFreeSpace)}";
                var percentage = (totalSize - availableFreeSpace) / (double)totalSize;
                DiskUsageProgressBarValue = percentage * 100;
            }
            else
            {
                DiskUsageProgressBarText = string.Empty;
                DiskUsageProgressBarValue = 0;
            }
        }

        private async void GetAnchorInfo(long roomId)
        {
            try
            {
                using var client = new BililiveApiClient();
                var msg = await client.GetAnchorInfo(roomId);

                if (msg.code != 0 || msg.data?.info == null) return;

                var info = msg.data.info;
                ImageUri = info.face;
                Name = info.uname;
                Uid = info.uid;
                Level = info.platform_user_level;
            }
            catch
            {
                // ignored
            }
        }

        private async Task<IEnumerable<LiveRecordListViewModel>> GetRecordList(long roomId, CancellationToken token)
        {
            try
            {
                IsLiveRecordBusy = true;
                using var client = new BililiveApiClient();
                var roomInitMessage = await client.GetRoomInit(roomId, token);
                if (roomInitMessage != null && roomInitMessage.code == 0
                                            && roomInitMessage.data != null && roomInitMessage.data.room_id > 0)
                {
                    RoomId = roomInitMessage.data.room_id;
                    ShortRoomId = roomInitMessage.data.short_id;
                    var listMessage = await client.GetLiveRecordList(roomInitMessage.data.room_id, 1, 1, token);
                    if (listMessage?.data != null && listMessage.data.count > 0)
                    {
                        var count = listMessage.data.count;
                        RecordCount = count;
                        listMessage = await client.GetLiveRecordList(roomInitMessage.data.room_id, 1, count, token);
                        if (listMessage?.data?.list != null && listMessage.data?.list.Length > 0)
                        {
                            return listMessage.data?.list.Select(x => new LiveRecordListViewModel(x));
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                IsLiveRecordBusy = false;
            }
            return Array.Empty<LiveRecordListViewModel>();
        }

        public void Dispose()
        {
            _diskMonitor?.Dispose();
            _roomIdMonitor?.Dispose();
            Config?.Dispose();
        }
    }
}