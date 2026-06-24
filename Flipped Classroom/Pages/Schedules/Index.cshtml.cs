using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Flipped_Classroom.Pages.Schedules
{
    [Authorize(Roles = "Admin,Manager")]
    public class IndexModel : PageModel
    {
        private readonly IScheduleService _scheduleService;

        public IndexModel(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        public IList<Class> ClassList { get; set; } = new List<Class>();
        public List<ScheduleSlotHelper.SlotDefinition> AvailableSlots { get; set; } = new();

        public async Task OnGetAsync()
        {
            ClassList = await _scheduleService.GetClassScheduleOverviewListAsync();
            AvailableSlots = ScheduleSlotHelper.GetAllSlots();
        }

        public async Task<IActionResult> OnPostAssignSlotAsync(int classId, string slotName)
        {
            var result = await _scheduleService.AssignSlotToClassAsync(classId, slotName);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
            }
            else
            {
                TempData["Success"] = result.Message;
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveScheduleAsync(int classId)
        {
            var success = await _scheduleService.RemoveScheduleFromClassAsync(classId);
            if (success)
            {
                TempData["Success"] = "Đã xóa lịch học.";
            }
            return RedirectToPage();
        }
    }
}
