using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using System.Data;
using System.Threading;
using Microsoft.TeamFoundation.Server;
using MTMCopyTool.Infrastructure;
using MTMCopyTool.Helpers;
using MTMCopyTool.Properties;

namespace MTMCopyTool.ViewModels
{
    public class ConnectionsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ICommand ConnectSourceCommand { get; private set; }
        public ICommand ConnectTargetCommand { get; private set; }
        public ICommand DisconnectTargetCommand { get; private set; }
        
        public ConnectionsViewModel()
        {
            TfsShared.Instance.Connected += Instance_Connected;
            if (!string.IsNullOrEmpty(Settings.Default.SourceTFS))
            {
                SourceWorking = true;
                TfsShared.Instance.Connect(true);
            }
            if (!string.IsNullOrEmpty(Settings.Default.TargetTFS))
            {
                TargetWorking = true;
                TfsShared.Instance.Connect(false);
            }

            BypassRules = Settings.Default.BypassRules;

            CanSetBypassRules = true;
            ConnectSourceCommand = new DelegateCommand(ConnectSource, CanWork);
            ConnectTargetCommand = new DelegateCommand(ConnectTarget, CanWork);
            DisconnectTargetCommand = new DelegateCommand(DisconnectTarget, () => !string.IsNullOrEmpty(TargetTFS) && !TargetWorking);
        }

        void Instance_Connected(object sender, ConnectionArgs e)
        {
            if (e.Success)
            {
                if (e.IsSource)
                {
                    this.SourceTFS = TfsShared.Instance.SourceTFS.Name;
                    this.SourceProject = TfsShared.Instance.SourceProject.Name;
                    this.SourceWorking = false;
                }
                else
                {
                    this.TargetTFS = TfsShared.Instance.TargetTFS.Name;
                    this.TargetProject = TfsShared.Instance.TargetProject.Name;
                    this.TargetWorking = false;
                    CanSetBypassRules = false;
                }
            }
            else
            {
                if (e.IsSource)
                {
                    this.SourceTFS = this.SourceProject = string.Empty;
                    this.SourceWorking = false;
                }
                else
                {
                    this.TargetTFS = this.TargetProject = string.Empty;
                    this.TargetWorking = false;
                    CanSetBypassRules = true;
                }
                Working = false;
            }
        }

        private bool CanWork()
        {
            return !Working;
        }

        private void ConnectSource()
        {
            Settings.Default.SourceTFS = string.Empty;
            Settings.Default.Save();

            SourceWorking = true;
            TfsShared.Instance.Connect(true);
        }

        private void ConnectTarget()
        {
            Settings.Default.TargetTFS = string.Empty;
            Settings.Default.Save();

            TargetWorking = true;
            TfsShared.Instance.Connect(false, BypassRules);
        }

        private void DisconnectTarget()
        {
            Settings.Default.TargetTFS = string.Empty;
            Settings.Default.Save();
            TargetWorking = false;
            TfsShared.Instance.Disconnect(false);
        }
      
        private string _sourceTfs;
        public string SourceTFS
        {
            get { return _sourceTfs; }
            set
            {
                if (value == _sourceTfs) return;
                _sourceTfs = value;
                this.OnPropertyChanged("SourceTFS");
            }
        }

        private string _sourceProject;
        public string SourceProject
        {
            get { return _sourceProject; }
            set
            {
                if (value == _sourceProject) return;
                _sourceProject = value;
                this.OnPropertyChanged("SourceProject");
            }
        }

        private string _targetTFS;
        public string TargetTFS
        {
            get { return _targetTFS; }
            set
            {
                if (value == _targetTFS) return;
                _targetTFS = value;
                this.OnPropertyChanged("TargetTFS");
            }
        }

        private string _targetProject;
        public string TargetProject
        {
            get { return _targetProject; }
            set
            {
                if (value == _targetProject) return;
                _targetProject = value;
                this.OnPropertyChanged("TargetProject");
            }
        }

        private bool _sourceWorking;
        public bool SourceWorking
        {
            get { return _sourceWorking; }
            set
            {
                if (value == _sourceWorking) return;
                _sourceWorking = value;

                if (!_sourceWorking && !_targetworking)
                    Working = false;
                else
                    Working = true;
            }
        }

        private bool _targetworking;
        public bool TargetWorking
        {
            get { return _targetworking; }
            set
            {
                if (value == _targetworking) return;
                _targetworking = value;

                if (!_sourceWorking && !_targetworking)
                    Working = false;
                else
                {
                    Working = true;
                    CanSetBypassRules = false;
                }
            }
        }

        private bool _bypassRules;
        public bool BypassRules
        {
            get { return _bypassRules; }
            set
            {
                if (value == _bypassRules) return;
                _bypassRules = value;

                Settings.Default.BypassRules = value;
                Settings.Default.Save();

                this.OnPropertyChanged("BypassRules");
            }
        }

        private bool _canSetBypassRules;
        public bool CanSetBypassRules
        {
            get { return _canSetBypassRules; }
            set
            {
                if (value == _canSetBypassRules) return;
                _canSetBypassRules = value;

                this.OnPropertyChanged("CanSetBypassRules");
            }
        }

        private bool _working;
        public bool Working
        {
            get { return _working; }
            set
            {
                if (value == _working) return;
                _working = value;
                this.OnPropertyChanged("Working");
            }
        }
    }
}