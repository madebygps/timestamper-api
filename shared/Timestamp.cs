namespace serverlesstimestamper.shared;

public class Timestamp
{

    public Timestamp(string time, string summary)
    {
        this.time = time;
        this.summary = summary;
    }
    
    
    public string time { get; set; }
    public string summary { get; set; }
}