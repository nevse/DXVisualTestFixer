﻿using DXVisualTestFixer.Common;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.ViewModels {
    public class TestInfoViewModel : BindableBase, INavigationAware {
        MergerdTestViewType _MergerdTestViewType;
        ITestInfoModel _TestInfo;

        public MergerdTestViewType MergerdTestViewType {
            get { return _MergerdTestViewType; }
            set { SetProperty(ref _MergerdTestViewType, value); }
        }
        public ITestInfoModel TestInfo {
            get { return _TestInfo; }
            set { SetProperty(ref _TestInfo, value); }
        }

        public void OnNavigatedTo(NavigationContext navigationContext) {
            TestInfo = navigationContext.Parameters[MainViewModel.NavigationParameter_Test] as ITestInfoModel;
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext) {
            
        }

        public void ChangeView(bool reverse) {
            MergerdTestViewType = GetNextMergedTestViewType(reverse);
        }
        MergerdTestViewType GetNextMergedTestViewType(bool reverse) {
            switch(MergerdTestViewType) {
                case MergerdTestViewType.Diff:
                    return reverse ? MergerdTestViewType.Current : MergerdTestViewType.Before;
                case MergerdTestViewType.Before:
                    return reverse ? MergerdTestViewType.Diff : MergerdTestViewType.Current;
                case MergerdTestViewType.Current:
                    return reverse ? MergerdTestViewType.Before : MergerdTestViewType.Diff;
                default:
                    return MergerdTestViewType;
            }
        }
    }
}