using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hudson.TrayTracker.Utils
{
    internal static class JenkinsUtils
    {
        /// <summary>
        /// Determines whether the specified project class should be considered a folder
        /// </summary>
        /// <remarks>
        /// When asking Jenkins for its projects, projects nested inside a folder do not show up. 
        /// One has to retrieve the list of sub-project by requesting each folder project.
        /// Folder projects class ends either by 'WorkflowMultiBranchProject' or 'Folder'.
        /// </remarks>
        /// <param name="projectClass"></param>
        /// <returns></returns>
        public static bool IsFolder(string projectClass)
        {
            var projectType = projectClass.Substring(projectClass.LastIndexOf('.') + 1);
            return projectType == "WorkflowMultiBranchProject" ||
                projectType == "Folder";
        }
    }
}
