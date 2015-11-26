namespace AsotListener.Models
{
    public class Episode: BaseModel
    {
        private string name;
        private string url;
        private EpisodeStatus status = EpisodeStatus.CanBeLoaded;
        
        public string Name 
        { 
            get { return name; }
            private set { SetField(ref name, value, "Name"); }
            
            // TODO: Check if this is valid
            // private set { SetField(ref status, value, nameof(Name); }
        }
        
        public string Url
        { 
            get { return url; }
            private set { SetField(ref url, value, "Url"); }
            
            // TODO: Check if this is valid
            // private set { SetField(ref status, value, nameof(Url); }
        }
        
        public EpisodeStatus Status
        { 
            get { return status; }
            private set { SetField(ref status, value, "Status"); }
            
            // TODO: Check if this is valid
            // private set { SetField(ref status, value, nameof(Status); }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
