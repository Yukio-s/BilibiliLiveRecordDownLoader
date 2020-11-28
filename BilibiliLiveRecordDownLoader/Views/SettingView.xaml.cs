using BilibiliLiveRecordDownLoader.Utils;
using BilibiliLiveRecordDownLoader.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace BilibiliLiveRecordDownLoader.Views
{
	public partial class SettingView
	{
		public SettingView(SettingViewModel viewModel)
		{
			InitializeComponent();
			ViewModel = viewModel;

			this.WhenActivated(d =>
			{
				ViewModel.CreateDiskMonitor().DisposeWith(d);
				ViewModel.CheckStartupStatus();

				this.Bind(ViewModel, vm => vm.Config.MainDir, v => v.MainDirTextBox.Text).DisposeWith(d);

				this.OneWayBind(ViewModel, vm => vm.DiskUsageProgressBarText, v => v.DiskUsageProgressBarTextBlock.Text).DisposeWith(d);

				this.OneWayBind(ViewModel, vm => vm.DiskUsageProgressBarValue, v => v.DiskUsageProgressBar.Value).DisposeWith(d);

				this.OneWayBind(ViewModel, vm => vm.DiskUsageProgressBarValue,
						v => v.DiskUsageProgressBar.Foreground,
						p => p > 90 ? Constants.RedBrush : Constants.NormalDiskUsageBrush).DisposeWith(d);

				this.BindCommand(ViewModel, vm => vm.SelectMainDirCommand, v => v.SelectMainDirButton).DisposeWith(d);

				this.BindCommand(ViewModel, vm => vm.OpenMainDirCommand, v => v.OpenMainDirButton).DisposeWith(d);

				this.Bind(ViewModel,
						vm => vm.Config.DownloadThreads,
						v => v.ThreadsTextBox.Value,
						x => x,
						Convert.ToByte).DisposeWith(d);

				this.Bind(ViewModel, vm => vm.Config.IsCheckPreRelease, v => v.IsCheckPreReleaseSwitch.IsOn).DisposeWith(d);
				this.BindCommand(ViewModel, vm => vm.CheckUpdateCommand, v => v.CheckUpdateButton).DisposeWith(d);
				this.OneWayBind(ViewModel, vm => vm.UpdateStatus, v => v.UpdateStatusTextBlock.Text).DisposeWith(d);

				this.Bind(ViewModel, vm => vm.Config.IsAutoConvertMp4, v => v.IsAutoConvertMp4Switch.IsOn).DisposeWith(d);
				this.Bind(ViewModel, vm => vm.Config.IsDeleteAfterConvert, v => v.IsDeleteAfterConvertSwitch.IsOn).DisposeWith(d);

				this.Bind(ViewModel, vm => vm.IsRunOnStartup, v => v.StartupSwitch.IsOn).DisposeWith(d);
				this.Bind(ViewModel, vm => vm.Config.IsCheckUpdateOnStart, v => v.IsCheckUpdateOnStartSwitch.IsOn).DisposeWith(d);

				Observable.FromEventPattern(StartupSwitch, nameof(StartupSwitch.Toggled)).Subscribe(_ => ViewModel.SwitchStartup(StartupSwitch.IsOn)).DisposeWith(d);
			});
		}
	}
}
