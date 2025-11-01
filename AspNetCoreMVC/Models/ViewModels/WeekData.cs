namespace AspNetCoreMVC.Models.ViewModels
{
    public class WeekData
    {
        public List<Schedule> Monday { get; set; } = new();
        public List<Schedule> Tuesday { get; set; } = new();
        public List<Schedule> Wednesday { get; set; } = new();
        public List<Schedule> Thursday { get; set; } = new();
        public List<Schedule> Friday { get; set; } = new();
        public List<Schedule> Saturday { get; set; } = new();
        public List<Schedule> Sunday { get; set; } = new();

        public DateTime StartOfWeek { get; set; }
        public DateTime EndOfWeek { get; set; }
    }
}
