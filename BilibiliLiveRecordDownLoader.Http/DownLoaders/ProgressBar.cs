﻿using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace BilibiliLiveRecordDownLoader.Http.DownLoaders
{
    public class ProgressBar : IDisposable
    {
        private readonly Subject<double> _progress = new Subject<double>();
        public IObservable<double> ProgressUpdated => _progress.AsObservable();

        public void Report(double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            _progress.OnNext(value);
        }

        public void Dispose()
        {
            _progress.OnCompleted();
        }
    }
}