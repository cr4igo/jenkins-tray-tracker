using System;
using Newtonsoft.Json;

namespace Hudson.TrayTracker.Entities
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Project : IComparable<Project>
    {
        private string fullName;

        public Server Server { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("fullName")]
        public string FullName
        {
            get { return string.IsNullOrEmpty(fullName) ? Name : fullName; }
            set { fullName = value; }
        }

        public string DisplayName => FullName.Replace(".", " > ");

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("isFolder")]
        public bool IsFolder { get; set; }

        internal Project ParentProject { get; set; }

        public AllBuildDetails AllBuildDetails { get; set; }

        public BuildStatus Status
        {
            get
            {
                // get a copy of the reference to avoid a race condition
                var details = AllBuildDetails;
                if (details == null) return BuildStatus.UNKNOWN_BUILD_STATUS;
                return details.Status;
            }
        }
        public BuildStatusEnum StatusValue => Status.Value;

        public BuildDetails LastSuccessfulBuild
        {
            get
            {
                // get a copy of the reference to avoid a race condition
                var details = AllBuildDetails;
                if (details == null)
                    return null;
                return details.LastSuccessfulBuild;
            }
        }

        public BuildDetails LastFailedBuild
        {
            get
            {
                // get a copy of the reference to avoid a race condition
                var details = AllBuildDetails;
                if (details == null) return null;

                return details.LastFailedBuild;
            }
        }

        public override int GetHashCode() => FullName.GetHashCode();

        public override bool Equals(object obj)
        {
            Project other = obj as Project;
            if (other == null)
                return false;
            return other.Server.Equals(Server) && other.FullName == FullName;
        }

        public override string ToString() => DisplayName;

        public int CompareTo(Project other) => FullName.CompareTo(other.FullName);
    }
}
