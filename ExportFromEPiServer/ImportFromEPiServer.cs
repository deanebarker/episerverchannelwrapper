using System;
using System.Collections.Generic;
using EPiServer;
using EPiServer.BaseLibrary.Scheduling;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServerChannelLib;
using EPiServerChannelLib.RecordManagers;

namespace ExportFromEPiServer
{
    [ScheduledPlugIn(DisplayName = "Export to Content Channel")]
    public class ExportFromEPiServer : JobBase
    {
        private int parentId = 3;
        private bool stopSignaled;

        public ExportFromEPiServer()
        {
            IsStoppable = true;
        }

        public override void Stop()
        {
            stopSignaled = true;
        }

        public override string Execute()
        {
            // Open the channel
            var channel = new EPiServerChannel("Press Releases", "http://sandbox2.local/", string.Empty, "page.importer", "page.importer");
            channel.RecordManager = new DDSRecordManager();

            int counter = 0;
            foreach (var page in DataFactory.Instance.GetChildren(new PageReference(parentId)))
            {
                var dictionary = new Dictionary<string, object>();

                // Turn this page into a dictionary
                foreach (PropertyData property in page.Property)
                {
                    dictionary.Add(property.Name, property.Value);
                }

                channel.Process(dictionary);

                counter++;
            }

            if (stopSignaled)
            {
                return "Job stopped.";
            }

            return String.Concat(counter, "record(s) processed.");
        }
    }
}