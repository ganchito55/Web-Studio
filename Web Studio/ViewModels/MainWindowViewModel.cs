﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FastObservableCollection;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using TreeViewExplorerControl;
using ValidationInterface;
using ValidationInterface.MessageTypes;
using Web_Studio.Editor;
using Web_Studio.Events;
using Web_Studio.Localization;
using Web_Studio.Models.PluginManager;
using Web_Studio.Models.Project;
using Web_Studio.Utils;

namespace Web_Studio.ViewModels
{
    /// <summary>
    ///     ViewModel for MainWindow
    /// </summary>
    public class MainWindowViewModel : BindableBase
    {   
        /// <summary>
        ///     Default constructor, it loads the values from user config.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public MainWindowViewModel()
        {  
            //Initialize properties
            ItemRemovedRequest = new InteractionRequest<IConfirmation>();

            //Manage commands
            SelectedItemChangedCommand = new DelegateCommand(SelectedItemChanged); 
            GenerateCommand = new DelegateCommand(Generate);  
            BusyControlCancelCommand = new DelegateCommand(BusyControlCancel);
            ExplorerControlItemRemovedCommand = new DelegateCommand<INode>(ExplorerControlItemRemoved);

            //Manage events
            EventSystem.Subscribe<FontSizeChangedEvent>(ManageChangedFont);
            EventSystem.Subscribe<ShowLineNumbersEvent>(ManageChangedShowLineNumbers);
            EventSystem.Subscribe<ClosedDocumentEvent>(ManageDocumentClosed);   

            //Worker
            GenerationWorker = new BackgroundWorker();
            GenerationWorker.DoWork += GenerationWorkerOnDoWork;
            GenerationWorker.RunWorkerCompleted += GenerationWorkerOnRunWorkerCompleted;
            GenerationWorker.WorkerSupportsCancellation = true;
        }

        /// <summary>
        /// Datacontext for Menu and popups
        /// </summary>
        public MenuViewModel Menu { get; } = new MenuViewModel();

        #region Editor
        private EditorViewModel _activeDocument;
        /// <summary>
        /// Active document
        /// </summary>
        public EditorViewModel ActiveDocument
        {
            get { return _activeDocument; }
            set { SetProperty(ref _activeDocument, value); }
        }

        /// <summary>
        ///     Remove closed document
        /// </summary>
        /// <param name="obj"></param>
        private void ManageDocumentClosed(ClosedDocumentEvent obj)
        {
            ProjectModel.Instance.Documents.Remove(obj.ClosedDocument);
        }

        /// <summary>
        ///     Method to manage the chage show line numbers event
        /// </summary>
        /// <param name="obj"></param>
        private void ManageChangedShowLineNumbers(ShowLineNumbersEvent obj)
        {
            foreach (var editorViewModel in ProjectModel.Instance.Documents) //Update all editors
            {
                editorViewModel.EditorShowLineNumbers = obj.ShowLineNumbers;
            }
        }

        /// <summary>
        ///     Method to manage the change font event
        /// </summary>
        /// <param name="obj"></param>
        private void ManageChangedFont(FontSizeChangedEvent obj)
        {  
            foreach (var editorViewModel in ProjectModel.Instance.Documents)
            {
                editorViewModel.EditorFontSize = obj.FontSize; 
            }
        }

        #endregion

        #region Explorer control

        /// <summary>
        ///     Command to manage removed item changed
        /// </summary>
        public DelegateCommand<INode> ExplorerControlItemRemovedCommand { get; private set; }

        /// <summary>
        /// Request to show the removed item window
        /// </summary>
        public InteractionRequest<IConfirmation> ItemRemovedRequest { get; }

        /// <summary>
        /// Method to manage the remove command, ask for confirmation, and close removed documents
        /// </summary>
        /// <param name="node"></param>
        private void ExplorerControlItemRemoved(INode node)
        {
            if (node is FileNode) //Is a File
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    var contenTextBox = new TextBlock
                    {
                        Text = Strings.RemoveFileDescription,
                        Foreground = new SolidColorBrush(Colors.White)
                    };
                    ItemRemovedRequest.Raise(
                        new Confirmation {Content = contenTextBox, Title = Strings.RemoveFolderTitle},
                        c =>
                        {
                            if (c.Confirmed)
                            {
                                try
                                {
                                    File.Delete(node.FullPath);
                                    ProjectModel.Instance.Documents.Remove(ProjectModel.Instance.Documents.FirstOrDefault(t => t.ToolTip == node.FullPath));
                                    //Remove document
                                }
                                catch (Exception e)
                                {
                                    Telemetry.Telemetry.TelemetryClient.TrackException(e);
                                }
                            }
                        });
                });
            }
            if (node is FolderNode)
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    var contenTextBox = new TextBlock
                    {
                        Text = Strings.RemoveFolderDescription,
                        Foreground = new SolidColorBrush(Colors.White)
                    };
                    ItemRemovedRequest.Raise(
                        new Confirmation {Content = contenTextBox, Title = Strings.RemoveFolderTitle},
                        c =>
                        {
                            if (c.Confirmed)
                            {
                                try
                                {
                                    Directory.Delete(node.FullPath, true);
                                    var documentsToRemove = ProjectModel.Instance.Documents.Where(t => t.ToolTip.Contains(node.FullPath));
                                    foreach (var document in documentsToRemove.ToList())
                                    {
                                        ProjectModel.Instance.Documents.Remove(document); //Remove document
                                    }
                                }
                                catch (Exception e)
                                {
                                   Telemetry.Telemetry.TelemetryClient.TrackException(e);
                                }
                            }
                        });
                });
            }
        }

        private bool _selectedItemIsFolder;
        private string _selectedItemName;
        private string _selectedItemPath;

        /// <summary>
        ///     Name of the selected item in the explorer view
        /// </summary>
        public string SelectedItemName
        {
            get { return _selectedItemName; }
            set { SetProperty(ref _selectedItemName, value); }
        }

        /// <summary>
        ///     Path to the selected item in the explorer view
        /// </summary>
        public string SelectedItemPath
        {
            get { return _selectedItemPath; }
            set { SetProperty(ref _selectedItemPath, value); }
        }

        /// <summary>
        ///     Is a folder the selected item in the explorer view
        /// </summary>
        public bool SelectedItemIsFolder
        {
            get { return _selectedItemIsFolder; }
            set { SetProperty(ref _selectedItemIsFolder, value); }
        }

        /// <summary>
        ///     Command to manage the selected item changed "event"
        /// </summary>
        public DelegateCommand SelectedItemChangedCommand { get; private set; }

        /// <summary>
        ///     Open the selected file and display it if you can do it
        /// </summary>
        private void SelectedItemChanged()
        {
            var s = Path.GetExtension(SelectedItemPath);
            if (SelectedItemIsFolder || s==null) return;
            var extension = s.ToLower();
          
            switch (extension)  //Only open these extensions
            {
                case ".html":
                case ".css":
                case ".js":
                case ".ws":
                    break;
                default:
                    return;
            }

            foreach (var doc in ProjectModel.Instance.Documents)
            {
                doc.IsSelected = false;
            }

            EditorViewModel myEditor = ProjectModel.Instance.SearchOrCreateDocument(SelectedItemPath);
            myEditor.IsSelected = true;
        }

       
        #endregion

        #region Messages 
        private int _errorMessages;
        private int _warningMessages;
        private AnalysisResult _messageSelected; 

        /// <summary>
        /// Collection with the messages generated by the plugins
        /// </summary>
        public FastObservableCollection<AnalysisResult> Results => ProjectModel.Instance.Results;

        /// <summary>
        /// Command to manage the generate option
        /// </summary>
        public DelegateCommand GenerateCommand { get; private set; }

        /// <summary>
        /// Generate the new project
        /// </summary>
        private void Generate()
        {
            if (ProjectModel.Instance.FullPath != null && !IsGeneratingProject)
            {
                Telemetry.Telemetry.TelemetryClient.TrackEvent("Generation");
                Results.Clear();
                NumberOfRulesProcessed = 0;
                
                NumberOfRules = CalculateNumberOfRules(); //Check and fix each plugin
                IsGeneratingProject = true;
                ProjectModel.Instance.CopySourceToRelease();
                EventSystem.Publish(new MessageContainerVisibilityChangedEvent {IsVisible = true});  //Make visible messages container  
                GenerationWorker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Calculate the number of rules that it has to process
        /// </summary>
        /// <returns></returns>
        private int CalculateNumberOfRules()
        {
            int counter = 0;
            foreach (Lazy<IValidation, IValidationMetadata> plugin in ValidationPluginManager.Plugins)
            {
                if (!plugin.Value.IsEnabled) continue;
                counter++; // first check
                if (plugin.Value.IsAutoFixeable) counter += 2; //fix and second check 
            }
            return counter;
        }

        /// <summary>
        /// Run plugins and fixes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="doWorkEventArgs"></param>
        private void GenerationWorkerOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            string releasePath = Path.Combine(ProjectModel.Instance.FullPath, "release");
            _errorMessages = 0;
            _warningMessages = 0;

            //Check loop
            foreach (Lazy<IValidation, IValidationMetadata> t in ValidationPluginManager.Plugins)
            {
                if (GenerationWorker.CancellationPending)  //Manage the cancelation event
                {
                    doWorkEventArgs.Cancel = true;
                    return;
                }
                var plugin = t.Value;
                if (!plugin.IsEnabled) continue;

                var tempResults = plugin.Check(releasePath);
                CountMessageTypes(tempResults);
                UpdateStatusOfGeneration(tempResults);
            }

            //Fix loop and recheck loop
            foreach (Lazy<IValidation, IValidationMetadata> t in ValidationPluginManager.Plugins)
            {
                if (GenerationWorker.CancellationPending)  ////Manage the cancelation event
                {
                    doWorkEventArgs.Cancel = true;
                    return;
                }
                var plugin = t.Value;

                if (!plugin.IsAutoFixeable || !plugin.IsEnabled) continue;

                //Fix
                var tempResults = t.Value.Fix(releasePath);
                CountMessageTypes(tempResults);
                if (tempResults != null && tempResults.Count > 0)  tempResults.Insert(0,new AnalysisResult("",0,plugin.Name,Strings.FixMessages,InfoType.Instance));

                UpdateStatusOfGeneration(tempResults);

                //Recheck
                var checkResults = plugin.Check(releasePath);
                CountMessageTypes(checkResults);  

                if(checkResults!=null && checkResults.Count > 0) checkResults.Insert(0,new AnalysisResult("",0,plugin.Name,Strings.NotFixedErrors,InfoType.Instance));
                UpdateStatusOfGeneration(checkResults);
            }

        }

        /// <summary>
        ///  This method update the UI with the actual status of the generation process
        /// </summary>
        /// <param name="results"></param>
        private void UpdateStatusOfGeneration(List<AnalysisResult> results)
        {
            Application.Current.Dispatcher.BeginInvoke((Action) delegate //Update UI
            {
                Results.AddRange(results);
                NumberOfRulesProcessed++;
            });
        }

        /// <summary>
        ///  Count how many messages are error messages and the number of warning messages
        /// </summary>
        /// <param name="results"></param>
        private void CountMessageTypes(List<AnalysisResult> results)
        {
            foreach (AnalysisResult analysisResult in results ?? Enumerable.Empty<AnalysisResult>())
            {
                if (analysisResult.Type == ErrorType.Instance) _errorMessages++;
                if (analysisResult.Type == WarningType.Instance) _warningMessages++;
            }
        }

        /// <summary>
        ///  When worker finishes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="runWorkerCompletedEventArgs"></param>
        private void GenerationWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            IsGeneratingProject = false;
            Telemetry.Telemetry.TelemetryClient.TrackMetric("Errors",_errorMessages);  
            Telemetry.Telemetry.TelemetryClient.TrackMetric("Warnings",_warningMessages);
            Notifications.RaiseGeneratedNotification(_errorMessages,_warningMessages);
        }
        
        /// <summary>
        /// the worker for project generation
        /// </summary>
        private BackgroundWorker GenerationWorker { get; set; }    

        /// <summary>
        /// Selected message in message list control
        /// </summary>
        public AnalysisResult MessageSelected
        {
            get { return _messageSelected; }
            set
            {
                SetProperty(ref _messageSelected, value);
                if (_messageSelected!= null && _messageSelected.File != "" && File.Exists(_messageSelected.File))  //Project message
                {
                    GoToMessageLine();  
                }
            }
        }

        /// <summary>
        /// When a message is selected, open the document of the message and go to the line
        /// </summary>
        private void GoToMessageLine()
        {
            foreach (var doc in ProjectModel.Instance.Documents)     //put select property to false
            {
                doc.IsSelected = false;
            }
            var myEditor = ProjectModel.Instance.SearchOrCreateDocument(MessageSelected.File);
            myEditor.IsSelected = true;
            myEditor.ScrollToLine = MessageSelected.Line;
        }

        #endregion

        #region BusyControl
        private bool _isGeneratingProject;
        private int _numberOfRules;
        private int _numberOfRulesProcessed; 

        /// <summary>
        /// True if we are generating the project => enable busycontrol
        /// </summary>
        public bool IsGeneratingProject
        {
            get { return _isGeneratingProject; }
            set { SetProperty(ref _isGeneratingProject, value); }
        }

        /// <summary>
        /// Total number of rules to process
        /// </summary>
        public int NumberOfRules
        {
            get { return _numberOfRules; }
            set { SetProperty(ref _numberOfRules, value); }
        }

        /// <summary>
        /// Number of rules that we have already processed
        /// </summary>
        public int NumberOfRulesProcessed
        {
            get { return _numberOfRulesProcessed; }
            set { SetProperty(ref _numberOfRulesProcessed, value); }
        }

        /// <summary>
        /// Manage the cancel button
        /// </summary>
        public DelegateCommand BusyControlCancelCommand { get; private set; }

        /// <summary>
        /// Cancel the project generation
        /// </summary>
        private void BusyControlCancel()
        {
          GenerationWorker.CancelAsync();
          Telemetry.Telemetry.TelemetryClient.TrackEvent("Cancel Generation");    
        } 

        #endregion
    }
}