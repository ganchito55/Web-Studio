﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeadingPlugin.Properties;
using HtmlAgilityPack;
using ValidationInterface;

namespace HeadingPlugin
{
    /// <summary>
    /// Plugin to check html heading (h1 h2 h3...) 
    /// </summary>
    [Export(typeof(IValidation))]
    [ExportMetadata("Name", "Heading")]
    [ExportMetadata("After", "Include")]
    public class HeadingPlugin : IValidation
    {
        /// <summary>
        ///     Name of the plugin
        /// </summary>
        public string Name => Strings.Name;

        /// <summary>
        ///     Description
        /// </summary>
        public string Description => Strings.Description;

        /// <summary>
        ///     Category of the plugin
        /// </summary>
        public Category Type { get; } = Category.Seo;

        /// <summary>
        ///     Results of the check method.
        /// </summary>
        public List<AnalysisResult> AnalysisResults { get; }  = new List<AnalysisResult>();

        /// <summary>
        ///     can we automatically fix some errors?
        /// </summary>
        public bool IsAutoFixeable { get; } = false;

        /// <summary>
        ///     Is enabled this plugin
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        ///     Method to validate the project with this plugin
        /// </summary>
        /// <param name="projectPath"></param>
        /// <returns></returns>
        public List<AnalysisResult> Check(string projectPath)
        {
            AnalysisResults.Clear(); //Remove older entries
            //Get the html files in the folder and subfolder
            var filesToCheck = Directory.GetFiles(projectPath, "*.html", SearchOption.AllDirectories);
            List<HeadingModel> models = new List<HeadingModel>();
            foreach (var file in filesToCheck)
            {
                HeadingModel model = new HeadingModel(file);
                model.CheckHeadings();
                models.Add(model);
            }
            GenerateResults(models);
            return AnalysisResults;
        }

        private void GenerateResults(List<HeadingModel> models)
        {
            int h1 = 0,h2=0,h3=0;

            foreach (HeadingModel model in models)
            {
                if (model.H1 == 0) AnalysisResults.Add(Messages.H1NotFound(model.File));
                if (model.H1 > 1) AnalysisResults.Add(Messages.ManyH1Found(model.File));
                if(model.H2 ==0) AnalysisResults.Add(Messages.H2NotFound(model.File));
                h1 += model.H1;
                h2 += model.H2;
                h3 += model.H3;
            }
            AnalysisResults.Add(Messages.TagsCount(h1,h2,h3));
        }

        /// <summary>
        ///     Method to fix automatically some errors
        /// </summary>
        /// <param name="projectPath"></param>
        public void Fix(string projectPath)
        {
            //Do nothing
        }
    }
}
