using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Controllers
{
    public class StudentsController : Controller
    {
        private readonly SchoolContext _context;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(SchoolContext context,ILogger<StudentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Students
        public async Task<IActionResult> Index(
             string sortOrder,
             string currentFilter,
             string searchString,
             int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] =
                String.IsNullOrEmpty(sortOrder) ? "LastName_desc" : "";
            ViewData["DateSortParm"] =
                sortOrder == "EnrollmentDate" ? "EnrollmentDate_desc" : "EnrollmentDate";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var students = from s in _context.Students
                           select s;

            if (!String.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => s.LastName.Contains(searchString)
                                       || s.FirstMidName.Contains(searchString));
            }

            if (string.IsNullOrEmpty(sortOrder))
            {
                sortOrder = "LastName";
            }

            bool descending = false;
            if (sortOrder.EndsWith("_desc"))
            {
                sortOrder = sortOrder.Substring(0, sortOrder.Length - 5);
                descending = true;
            }

            if (descending)
            {
                students = students.OrderByDescending(e => EF.Property<object>(e, sortOrder));
            }
            else
            {
                students = students.OrderBy(e => EF.Property<object>(e, sortOrder));
            }

            int pageSize = 3;
            return View(await PaginatedList<Student>.CreateAsync(students.AsNoTracking(),
                pageNumber ?? 1, pageSize));
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
              .Include(s => s.Enrollments)
                  .ThenInclude(e => e.Course)
              .AsNoTracking()
              .FirstOrDefaultAsync(m => m.ID == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("EnrollmentDate,FirstMidName,LastName")] Student student)
        {

            try
            {
                if (ModelState.IsValid)
                {
                    var newStudent = student;
                    _logger.LogInformation(message: "We are about to add this person: "+ student.FullName + " Enrollment date: " + student.EnrollmentDate );
                    _context.Add(student);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException ex )
            {
                //Log the error (uncomment ex variable name and write a log.
                _logger.LogError(message: "Unable to save changes. It Ocurred an error. Details:" + ex);
                ModelState.AddModelError("", "Unable to save changes. " + "Try again, and if the problem persists " + "destroy your computer.");
            }
            return View(student);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        private string GetChanges(Student Old,Student New)
        {
            string changesLogScript = "A row with the Id #" + Old.ID.ToString() + " from the 'Students' table was modified on " + DateTime.Now + "; The following fields were modified: ";
           
            var propsOld = Old.GetType().GetProperties();
            var propsNew = New.GetType().GetProperties();

            for (int i = 1; i < propsOld.Count(); i++)
            {
                
                var oldVal = propsOld[i].GetValue(Old, null);
                var newVal = propsNew[i].GetValue(New, null);
                
                if(oldVal == null || newVal == null || propsOld[i].Name == "FullName"){continue; }//skips if its either null or the calculated field "FullName".

                if (oldVal.ToString() != newVal.ToString()){ changesLogScript += "'" + propsOld[i].Name + "' : from  " + oldVal + " to " + newVal + "; "; }
            }

            return changesLogScript;
        }
        // POST: Students/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            if (id == null)
            {
                _logger.LogError("Not such student with that id");
                return NotFound();
            }
            
            var studentToUpdate = await _context.Students.FirstOrDefaultAsync(s => s.ID == id);
            //We make a copy of the old version before doing the update
            Student oldVersionStudent = new Student { ID = studentToUpdate.ID,FirstMidName = studentToUpdate.FirstMidName, LastName = studentToUpdate.LastName, EnrollmentDate = studentToUpdate.EnrollmentDate };

            if (await TryUpdateModelAsync<Student>(
                studentToUpdate,
                "",
                s => s.FirstMidName, s => s.LastName, s => s.EnrollmentDate))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation(GetChanges(oldVersionStudent, studentToUpdate));
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError("Unable to save student changes, see details: " + ex);
                    ModelState.AddModelError("", "Unable to save changes. " + "Try again, and if the problem persists, " + "burn your pc.");
                }
            }
            return View(studentToUpdate);
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                _logger.LogError("Not such student with that id");
                return NotFound();
            }

            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);

            if (student == null)
            {
                return NotFound();
            }

            if (saveChangesError.GetValueOrDefault())
            {
                ViewData["ErrorMessage"] =
                    "Delete failed. Try again, and if the problem persists " +
                    "see your system administrator.";
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            //Student oldVersionStudent = new Student { ID = student.ID,FirstMidName = student.FirstMidName, LastName = student.LastName, EnrollmentDate = student.EnrollmentDate };

            if (student == null)
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _logger.LogInformation("Deleting row #" + student.ID +" from 'Students' table. The student name is: " + student.FullName + " and this row is being deleted on " + DateTime.Now);
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex )
            {
                _logger.LogError("Unable to delete, see details: " + ex);
                return RedirectToAction(nameof(Delete), new { id = id, saveChangesError = true });
            }
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.ID == id);
        }
    }
}
