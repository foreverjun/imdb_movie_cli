﻿namespace IMDBCLI.model;

public class MovieActor
{
    public string MovieEntryId { get; set; }
    public string ActorEntryId { get; set; }

    public MovieEntry MovieEntry { get; set; } = null!;
    public ActorEntry ActorEntry { get; set; } = null!;
}

public class MovieDirector
{
    public string MovieEntryId { get; set; }
    public string DirectorEntryId { get; set; }
    
    public MovieEntry MovieEntry { get; set; } = null!;
    public DirectorEntry DirectorEntry { get; set; } = null!;
}

public class MovieTag
{
    public string MovieEntryId { get; set; }
    public string TagEntryId { get; set; }
    
    public MovieEntry MovieEntry { get; set; } = null!;
    public TagEntry TagEntry { get; set; } = null!;
}