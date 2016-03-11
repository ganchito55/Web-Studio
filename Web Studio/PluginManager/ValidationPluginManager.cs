﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using ValidationInterface;
using Web_Studio.Models;

namespace Web_Studio.PluginManager
{
    /// <summary>
    ///    Class to manage the validation plugins
    /// </summary>
    public class ValidationPluginManager
    {
        private static List<Lazy<IValidation, IValidationMetadata>> _plugins;

        /// <summary>
        /// List of plugins that implements IValidation interface
        /// </summary>
        public static List<Lazy<IValidation, IValidationMetadata>> Plugins
        {
            get
            {
                  if(_plugins==null) Load();
                return _plugins;
            }
            private set { _plugins = value; }
        }

        /// <summary>
        /// Load the validation plugins
        /// </summary>
        private  static void Load()
        {
            GenericMefPluginLoader<Lazy<IValidation, IValidationMetadata>> loader = new GenericMefPluginLoader<Lazy<IValidation, IValidationMetadata>>("Plugins\\Check");
            Sort(loader.Plugins);
        }

        /// <summary>
        /// Sort the plugins to get an ordered collection
        /// </summary>
        /// <param name="plugins"></param>
        private static void Sort(IEnumerable<Lazy<IValidation, IValidationMetadata>> plugins)
        {
            Queue<Lazy<IValidation,IValidationMetadata>> queue = new Queue<Lazy<IValidation, IValidationMetadata>>();
            List<Lazy<IValidation, IValidationMetadata>> sortedList = new List<Lazy<IValidation, IValidationMetadata>>();

            //Get the firt elements and add it to the queue
            var elements = plugins.Where(p => p.Metadata.After.Equals(""));
            foreach (Lazy<IValidation, IValidationMetadata> element in elements)
            {
                queue.Enqueue(element);
            }

            while (queue.Count > 0)
            {
                //Get the first element, add to the sorted list, and enqueue the other elements that depend of this element (child elements)
                var element = queue.Dequeue();
                sortedList.Add(element);
                var childElements = plugins.Where(p => p.Metadata.After.Equals(element.Metadata.Name));
                foreach (Lazy<IValidation, IValidationMetadata> childElement in childElements)
                {
                    queue.Enqueue(childElement);
                }

            }
            Plugins = sortedList;
        }
    }
}