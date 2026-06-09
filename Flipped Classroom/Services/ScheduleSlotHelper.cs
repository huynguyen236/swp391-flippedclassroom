using Flipped_Classroom.Models;

namespace Flipped_Classroom.Services;

/// <summary>
/// Định nghĩa các slot lịch học cố định và generate lịch tự động.
/// </summary>
public static class ScheduleSlotHelper
{
    public record SlotDefinition(string SlotName, string DisplayName, DayOfWeek[] Days, TimeOnly StartTime, TimeOnly EndTime);

    private static readonly Dictionary<string, SlotDefinition> Slots = new()
    {
        ["N5"] = new SlotDefinition("N5", "N5 — Thứ 2 & Thứ 4 (08:00–10:00)",
            new[] { DayOfWeek.Monday, DayOfWeek.Wednesday }, new TimeOnly(8, 0), new TimeOnly(10, 0)),

        ["N4"] = new SlotDefinition("N4", "N4 — Thứ 3 & Thứ 5 (08:00–10:00)",
            new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, new TimeOnly(8, 0), new TimeOnly(10, 0)),

        ["N3"] = new SlotDefinition("N3", "N3 — Thứ 2 & Thứ 4 (10:00–12:00)",
            new[] { DayOfWeek.Monday, DayOfWeek.Wednesday }, new TimeOnly(10, 0), new TimeOnly(12, 0)),

        ["N2"] = new SlotDefinition("N2", "N2 — Thứ 3 & Thứ 5 (10:00–12:00)",
            new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, new TimeOnly(10, 0), new TimeOnly(12, 0)),

        ["N1"] = new SlotDefinition("N1", "N1 — Thứ 6 & Thứ 7 (08:00–10:00)",
            new[] { DayOfWeek.Friday, DayOfWeek.Saturday }, new TimeOnly(8, 0), new TimeOnly(10, 0)),
    };

    /// <summary>
    /// Lấy tất cả slot names cho dropdown.
    /// </summary>
    public static List<SlotDefinition> GetAllSlots() => Slots.Values.ToList();

    /// <summary>
    /// Lấy slot definition theo tên.
    /// </summary>
    public static SlotDefinition? GetSlot(string slotName)
    {
        return Slots.TryGetValue(slotName, out var slot) ? slot : null;
    }

    /// <summary>
    /// Generate danh sách ClassSchedule từ startDate đến endDate dựa trên slot đã chọn.
    /// </summary>
    public static List<ClassSchedule> GenerateSchedules(int classId, DateOnly startDate, DateOnly endDate, string slotName)
    {
        var slot = GetSlot(slotName);
        if (slot == null)
            return new List<ClassSchedule>();

        var schedules = new List<ClassSchedule>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            if (slot.Days.Contains(currentDate.DayOfWeek))
            {
                schedules.Add(new ClassSchedule
                {
                    ClassId = classId,
                    StudyDate = currentDate,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    Room = null
                });
            }
            currentDate = currentDate.AddDays(1);
        }

        return schedules;
    }

    /// <summary>
    /// Lấy tên thứ trong tuần bằng tiếng Việt.
    /// </summary>
    public static string GetVietnameseDayName(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "Thứ 2",
            DayOfWeek.Tuesday => "Thứ 3",
            DayOfWeek.Wednesday => "Thứ 4",
            DayOfWeek.Thursday => "Thứ 5",
            DayOfWeek.Friday => "Thứ 6",
            DayOfWeek.Saturday => "Thứ 7",
            DayOfWeek.Sunday => "Chủ nhật",
            _ => day.ToString()
        };
    }
}
