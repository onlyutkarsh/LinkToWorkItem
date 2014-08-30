using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;

namespace LinkToWorkItem.Entities
{
    public class WorkItemSearchResult
    {
        public WorkItem WorkItem { get; set; }

        public Exception Exception { get; set; }

        public string ErrorMessage { get; set; }
    }
}