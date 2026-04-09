using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Models;
using SoftwareDeliveryPlanner.Services;

namespace SoftwareDeliveryPlanner.Forms;

public class MainForm : Form
{
    private PlannerDbContext db = null!;
    private SchedulingEngine scheduler = null!;

    // Dashboard labels
    private Label lblTotalServices = null!;
    private Label lblTotalEstimation = null!;
    private Label lblActiveResources = null!;
    private Label lblOverallFinish = null!;
    private Label lblOnTrack = null!;
    private Label lblAtRisk = null!;
    private Label lblLate = null!;
    private Label lblAvgAssigned = null!;

    // Data grids
    private DataGridView dgvTasks = null!;
    private DataGridView dgvResources = null!;
    private DataGridView dgvAdjustments = null!;
    private DataGridView dgvHolidays = null!;
    private DataGridView dgvCalendar = null!;
    private DataGridView dgvOutput = null!;

    // Timeline
    private ComboBox cboTimelineResource = null!;
    private DateTimePicker dtpTimelineStart = null!;
    private DateTimePicker dtpTimelineEnd = null!;
    private Panel pnlTimelineDays = null!;

    public MainForm()
    {
        InitializeComponent();
        db = new PlannerDbContext();
        db.InitializeDefaultData();
        scheduler = new SchedulingEngine(db);
        LoadData();
        RunScheduler();
    }

    private void InitializeComponent()
    {
        this.Text = "Software Delivery Planner";
        this.Size = new Size(1400, 850);
        this.StartPosition = FormStartPosition.CenterScreen;

        // Menu
        var menuStrip = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add("Export to CSV", null, ExportToCsv);
        fileMenu.DropDownItems.Add("-");
        fileMenu.DropDownItems.Add("Exit", null, (s, e) => Close());
        menuStrip.Items.Add(fileMenu);

        var actionsMenu = new ToolStripMenuItem("Actions");
        actionsMenu.DropDownItems.Add("Run Scheduler", null, (s, e) => RunScheduler());
        actionsMenu.DropDownItems.Add("Refresh", null, (s, e) => LoadData());
        menuStrip.Items.Add(actionsMenu);

        // Toolbar
        var toolbar = new ToolStrip();
        var btnRun = new ToolStripButton("Run Scheduler");
        btnRun.Click += (s, e) => RunScheduler();
        toolbar.Items.Add(btnRun);
        var btnRefresh = new ToolStripButton("Refresh");
        btnRefresh.Click += (s, e) => LoadData();
        toolbar.Items.Add(btnRefresh);

        // Tab Control
        var tabControl = new TabControl();
        tabControl.Dock = DockStyle.Fill;

        tabControl.TabPages.Add(CreateDashboardTab());
        tabControl.TabPages.Add(CreateTasksTab());
        tabControl.TabPages.Add(CreateResourcesTab());
        tabControl.TabPages.Add(CreateAdjustmentsTab());
        tabControl.TabPages.Add(CreateHolidaysTab());
        tabControl.TabPages.Add(CreateCalendarTab());
        tabControl.TabPages.Add(CreateTimelineTab());
        tabControl.TabPages.Add(CreateOutputTab());

        // Layout
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            RowStyles = { new RowStyle(SizeType.AutoSize), new RowStyle(SizeType.AutoSize), new RowStyle(SizeType.Percent, 100) }
        };

        mainPanel.Controls.Add(menuStrip, 0, 0);
        mainPanel.Controls.Add(toolbar, 0, 1);
        mainPanel.Controls.Add(tabControl, 0, 2);

        this.Controls.Add(mainPanel);
    }

    private TabPage CreateDashboardTab()
    {
        var tab = new TabPage("Dashboard");

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15),
            ColumnCount = 4,
            RowCount = 2
        };

        for (int i = 0; i < 4; i++)
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        panel.Controls.Add(CreateKpiCard("Total Services", lblTotalServices = new Label { Text = "0" }), 0, 0);
        panel.Controls.Add(CreateKpiCard("Total Estimation", lblTotalEstimation = new Label { Text = "0 days" }), 1, 0);
        panel.Controls.Add(CreateKpiCard("Active Resources", lblActiveResources = new Label { Text = "0" }), 2, 0);
        panel.Controls.Add(CreateKpiCard("Projected Finish", lblOverallFinish = new Label { Text = "TBD" }), 3, 0);

        panel.Controls.Add(CreateKpiCard("On Track", lblOnTrack = new Label { Text = "0", ForeColor = Color.Green }), 0, 1);
        panel.Controls.Add(CreateKpiCard("At Risk", lblAtRisk = new Label { Text = "0", ForeColor = Color.Orange }), 1, 1);
        panel.Controls.Add(CreateKpiCard("Late", lblLate = new Label { Text = "0", ForeColor = Color.Red }), 2, 1);
        panel.Controls.Add(CreateKpiCard("Avg Assigned", lblAvgAssigned = new Label { Text = "0" }), 3, 1);

        tab.Controls.Add(panel);
        return tab;
    }

    private Panel CreateKpiCard(string title, Label valueLabel)
    {
        var card = new Panel
        {
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(180, 70),
            Margin = new Padding(5)
        };

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(0, 5, 0, 0)
        };

        valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.MiddleCenter;
        valueLabel.Font = new Font(valueLabel.Font.Name, 16, FontStyle.Bold);

        card.Controls.Add(valueLabel);
        card.Controls.Add(titleLabel);

        return card;
    }

    private TabPage CreateTasksTab()
    {
        var tab = new TabPage("Task Register");

        var toolbar = new ToolStrip();
        toolbar.Items.Add(new ToolStripButton("Add Service", null, AddTask));
        toolbar.Items.Add(new ToolStripButton("Edit", null, EditTask));
        toolbar.Items.Add(new ToolStripButton("Delete", null, DeleteTask));

        dgvTasks = CreateGrid();
        dgvTasks.Dock = DockStyle.Fill;

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, RowStyles = { new RowStyle(SizeType.AutoSize), new RowStyle(SizeType.Percent, 100) } };
        panel.Controls.Add(toolbar, 0, 0);
        panel.Controls.Add(dgvTasks, 0, 1);

        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateResourcesTab()
    {
        var tab = new TabPage("Resources");

        var toolbar = new ToolStrip();
        toolbar.Items.Add(new ToolStripButton("Add Resource", null, AddResource));
        toolbar.Items.Add(new ToolStripButton("Edit", null, EditResource));
        toolbar.Items.Add(new ToolStripButton("Delete", null, DeleteResource));

        dgvResources = CreateGrid();
        dgvResources.Dock = DockStyle.Fill;

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, RowStyles = { new RowStyle(SizeType.AutoSize), new RowStyle(SizeType.Percent, 100) } };
        panel.Controls.Add(toolbar, 0, 0);
        panel.Controls.Add(dgvResources, 0, 1);

        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateAdjustmentsTab()
    {
        var tab = new TabPage("Adjustments");

        var toolbar = new ToolStrip();
        toolbar.Items.Add(new ToolStripButton("Add Adjustment", null, AddAdjustment));
        toolbar.Items.Add(new ToolStripButton("Delete", null, DeleteAdjustment));

        dgvAdjustments = CreateGrid();
        dgvAdjustments.Dock = DockStyle.Fill;

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, RowStyles = { new RowStyle(SizeType.AutoSize), new RowStyle(SizeType.Percent, 100) } };
        panel.Controls.Add(toolbar, 0, 0);
        panel.Controls.Add(dgvAdjustments, 0, 1);

        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateHolidaysTab()
    {
        var tab = new TabPage("Holidays");

        var toolbar = new ToolStrip();
        toolbar.Items.Add(new ToolStripButton("Add Holiday", null, AddHoliday));
        toolbar.Items.Add(new ToolStripButton("Edit Holiday", null, EditHoliday));
        toolbar.Items.Add(new ToolStripButton("Delete", null, DeleteHoliday));

        dgvHolidays = CreateGrid();
        dgvHolidays.Dock = DockStyle.Fill;

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, RowStyles = { new RowStyle(SizeType.AutoSize), new RowStyle(SizeType.Percent, 100) } };
        panel.Controls.Add(toolbar, 0, 0);
        panel.Controls.Add(dgvHolidays, 0, 1);

        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateCalendarTab()
    {
        var tab = new TabPage("Calendar");
        dgvCalendar = CreateGrid();
        dgvCalendar.Dock = DockStyle.Fill;
        tab.Controls.Add(dgvCalendar);
        return tab;
    }

    private TabPage CreateTimelineTab()
    {
        var tab = new TabPage("Employee Timeline");

        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            Height = 40,
            Padding = new Padding(5)
        };

        filterPanel.Controls.Add(new Label { Text = "Resource:", AutoSize = true });
        cboTimelineResource = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
        cboTimelineResource.SelectedIndexChanged += (s, e) => LoadEmployeeTimeline();
        filterPanel.Controls.Add(cboTimelineResource);

        filterPanel.Controls.Add(new Label { Text = "  Start:", AutoSize = true, Padding = new Padding(10, 0, 0, 0) });
        dtpTimelineStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110 };
        dtpTimelineStart.ValueChanged += (s, e) => LoadEmployeeTimeline();
        filterPanel.Controls.Add(dtpTimelineStart);

        filterPanel.Controls.Add(new Label { Text = "  End:", AutoSize = true, Padding = new Padding(10, 0, 0, 0) });
        dtpTimelineEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110 };
        dtpTimelineEnd.ValueChanged += (s, e) => LoadEmployeeTimeline();
        filterPanel.Controls.Add(dtpTimelineEnd);

        var btnRefresh = new Button { Text = "Refresh", Width = 80 };
        btnRefresh.Click += (s, e) => LoadEmployeeTimeline();
        filterPanel.Controls.Add(btnRefresh);

        pnlTimelineDays = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };

        tab.Controls.Add(pnlTimelineDays);
        tab.Controls.Add(filterPanel);

        return tab;
    }

    private TabPage CreateOutputTab()
    {
        var tab = new TabPage("Output Plan");

        var toolbar = new ToolStrip();
        toolbar.Items.Add(new ToolStripButton("Export to CSV", null, ExportToCsv));

        dgvOutput = CreateGrid();
        dgvOutput.Dock = DockStyle.Fill;

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, RowStyles = { new RowStyle(SizeType.AutoSize), new RowStyle(SizeType.Percent, 100) } };
        panel.Controls.Add(toolbar, 0, 0);
        panel.Controls.Add(dgvOutput, 0, 1);

        tab.Controls.Add(panel);
        return tab;
    }

    private DataGridView CreateGrid()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None
        };
    }

    private void LoadData()
    {
        // Timeline setup
        cboTimelineResource.Items.Clear();
        foreach (var r in db.Resources.Where(x => x.Active == "Yes").OrderBy(x => x.ResourceName))
            cboTimelineResource.Items.Add(r.ResourceId);
        if (cboTimelineResource.Items.Count > 0)
            cboTimelineResource.SelectedIndex = 0;

        dtpTimelineStart.Value = DateTime.Today.AddDays(-7);
        dtpTimelineEnd.Value = DateTime.Today.AddMonths(2);

        // Load Tasks
        dgvTasks.DataSource = db.Tasks.OrderBy(t => t.SchedulingRank ?? 999).ToList().Select(t => new
        {
            t.TaskId,
            t.ServiceName,
            t.DevEstimation,
            t.MaxDev,
            StrictDate = t.StrictDate?.ToString("yyyy-MM-dd") ?? "",
            t.Priority,
            t.AssignedDev,
            PlannedStart = t.PlannedStart?.ToString("yyyy-MM-dd") ?? "",
            PlannedFinish = t.PlannedFinish?.ToString("yyyy-MM-dd") ?? "",
            t.Duration,
            t.Status,
            t.DeliveryRisk
        }).ToList();

        // Resources
        dgvResources.DataSource = db.Resources.ToList();

        // Adjustments
        dgvAdjustments.DataSource = db.Adjustments.ToList();

        // Holidays
        dgvHolidays.DataSource = db.Holidays.OrderBy(h => h.HolidayDate).ToList();

        // Calendar
        dgvCalendar.DataSource = db.Calendar.Take(365).ToList();

        // Output
        dgvOutput.DataSource = scheduler.GetOutputPlan();

        // Dashboard
        var kpis = scheduler.GetDashboardKPIs();
        lblTotalServices.Text = kpis["total_services"].ToString();
        lblTotalEstimation.Text = $"{kpis["total_estimation"]} days";
        lblActiveResources.Text = $"{kpis["active_resources"]} devs ({kpis["total_capacity"]:F1})";
        lblOverallFinish.Text = kpis["overall_finish"]?.ToString() ?? "TBD";
        lblOnTrack.Text = kpis["on_track"].ToString();
        lblAtRisk.Text = kpis["at_risk"].ToString();
        lblLate.Text = kpis["late"].ToString();
        lblAvgAssigned.Text = kpis["avg_assigned"].ToString();

        LoadEmployeeTimeline();
    }

    private void LoadEmployeeTimeline()
    {
        pnlTimelineDays.Controls.Clear();
        if (cboTimelineResource.SelectedItem == null) return;

        var resourceId = cboTimelineResource.SelectedItem.ToString()!;
        var startDate = dtpTimelineStart.Value.Date;
        var endDate = dtpTimelineEnd.Value.Date;

        var adjustments = db.Adjustments.Where(a => a.ResourceId == resourceId).ToList();
        var tasks = db.Tasks.ToList();
        var holidays = db.Holidays.ToList();
        
        var resources = db.Resources.Where(r => r.Active == "Yes").OrderBy(r => r.ResourceName).ToList();
        var devIndex = resources.FindIndex(r => r.ResourceId == resourceId) + 1;

        var current = startDate;
        while (current <= endDate)
        {
            var dayOfWeek = (int)current.DayOfWeek;
            var isWeekend = dayOfWeek == 5 || dayOfWeek == 6;
            var isHoliday = holidays.Any(h => h.HolidayDate.Date == current.Date);
            
            var adjustment = adjustments.FirstOrDefault(a => a.AdjStart.Date <= current.Date && a.AdjEnd.Date >= current.Date);
            
            TaskItem? workingTask = null;
            if (devIndex > 0)
            {
                foreach (var task in tasks)
                {
                    if (task.AssignedDev.HasValue && 
                        Math.Floor(task.AssignedDev.Value) >= devIndex &&
                        task.PlannedStart.HasValue && task.PlannedFinish.HasValue &&
                        current.Date >= task.PlannedStart.Value.Date && current.Date <= task.PlannedFinish.Value.Date)
                    {
                        workingTask = task;
                        break;
                    }
                }
            }

            var dayCard = CreateDayCard(current, dayOfWeek, isWeekend, isHoliday, adjustment, workingTask, holidays);
            pnlTimelineDays.Controls.Add(dayCard);

            current = current.AddDays(1);
        }
    }

    private Panel CreateDayCard(DateTime date, int dayOfWeek, bool isWeekend, bool isHoliday, Adjustment? adjustment, TaskItem? task, List<Holiday> holidays)
    {
        var card = new Panel { Width = 100, Height = 70, Margin = new Padding(2) };

        Color bgColor;
        string statusText;

        if (isWeekend)
        {
            bgColor = Color.LightGray;
            statusText = dayOfWeek == 5 ? "Friday" : "Saturday";
        }
        else if (isHoliday)
        {
            bgColor = Color.LightYellow;
            var h = holidays.First(x => x.HolidayDate.Date == date.Date);
            statusText = h.HolidayName.Length > 12 ? h.HolidayName.Substring(0, 12) + ".." : h.HolidayName;
        }
        else if (adjustment != null)
        {
            bgColor = Color.LightBlue;
            statusText = adjustment.AdjType;
        }
        else if (task != null)
        {
            bgColor = Color.LightGreen;
            statusText = task.ServiceName.Length > 12 ? task.ServiceName.Substring(0, 12) + ".." : task.ServiceName;
        }
        else
        {
            bgColor = Color.White;
            statusText = "Free";
        }

        card.BackColor = bgColor;
        card.BorderStyle = BorderStyle.FixedSingle;

        var dayLabel = new Label
        {
            Text = $"{date:dd} {date:ddd}",
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.Name, 9, FontStyle.Bold),
            Height = 20
        };

        var statusLabel = new Label
        {
            Text = statusText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.Name, 8)
        };

        card.Controls.Add(statusLabel);
        card.Controls.Add(dayLabel);

        return card;
    }

    private void RunScheduler()
    {
        try
        {
            var result = scheduler.RunScheduler();
            LoadData();
            MessageBox.Show(result, "Scheduler Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #region Event Handlers
    private void AddTask(object? sender, EventArgs e)
    {
        var dialog = new TaskDialog(db);
        if (dialog.ShowDialog() == DialogResult.OK) { LoadData(); RunScheduler(); }
    }

    private void EditTask(object? sender, EventArgs e)
    {
        if (dgvTasks.SelectedRows.Count == 0) return;
        var taskId = dgvTasks.SelectedRows[0].Cells["TaskId"].Value?.ToString();
        if (string.IsNullOrEmpty(taskId)) return;
        var task = db.Tasks.FirstOrDefault(t => t.TaskId == taskId);
        if (task == null) return;
        var dialog = new TaskDialog(db, task);
        if (dialog.ShowDialog() == DialogResult.OK) { LoadData(); RunScheduler(); }
    }

    private void DeleteTask(object? sender, EventArgs e)
    {
        if (dgvTasks.SelectedRows.Count == 0) return;
        var taskId = dgvTasks.SelectedRows[0].Cells["TaskId"].Value?.ToString();
        if (string.IsNullOrEmpty(taskId)) return;
        if (MessageBox.Show($"Delete {taskId}?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            var task = db.Tasks.FirstOrDefault(t => t.TaskId == taskId);
            if (task != null) { db.Tasks.Remove(task); db.SaveChanges(); LoadData(); RunScheduler(); }
        }
    }

    private void AddResource(object? sender, EventArgs e)
    {
        var dialog = new ResourceDialog(db);
        if (dialog.ShowDialog() == DialogResult.OK) { LoadData(); RunScheduler(); }
    }

    private void EditResource(object? sender, EventArgs e)
    {
        if (dgvResources.SelectedRows.Count == 0) return;
        var resourceId = dgvResources.SelectedRows[0].Cells["ResourceId"].Value?.ToString();
        if (string.IsNullOrEmpty(resourceId)) return;
        var resource = db.Resources.FirstOrDefault(r => r.ResourceId == resourceId);
        if (resource == null) return;
        var dialog = new ResourceDialog(db, resource);
        if (dialog.ShowDialog() == DialogResult.OK) { LoadData(); RunScheduler(); }
    }

    private void DeleteResource(object? sender, EventArgs e)
    {
        if (dgvResources.SelectedRows.Count == 0) return;
        var resourceId = dgvResources.SelectedRows[0].Cells["ResourceId"].Value?.ToString();
        if (string.IsNullOrEmpty(resourceId)) return;
        if (MessageBox.Show($"Delete {resourceId}?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            var resource = db.Resources.FirstOrDefault(r => r.ResourceId == resourceId);
            if (resource != null) { db.Resources.Remove(resource); db.SaveChanges(); LoadData(); RunScheduler(); }
        }
    }

    private void AddAdjustment(object? sender, EventArgs e)
    {
        var dialog = new AdjustmentDialog(db);
        if (dialog.ShowDialog() == DialogResult.OK) { LoadData(); RunScheduler(); }
    }

    private void DeleteAdjustment(object? sender, EventArgs e)
    {
        if (dgvAdjustments.SelectedRows.Count == 0) return;
        var id = Convert.ToInt32(dgvAdjustments.SelectedRows[0].Cells["Id"].Value);
        if (MessageBox.Show($"Delete?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            var adj = db.Adjustments.Find(id);
            if (adj != null) { db.Adjustments.Remove(adj); db.SaveChanges(); LoadData(); RunScheduler(); }
        }
    }

    private void AddHoliday(object? sender, EventArgs e)
    {
        var dialog = new HolidayDialog(db);
        if (dialog.ShowDialog() == DialogResult.OK) { LoadData(); RunScheduler(); }
    }

    private void EditHoliday(object? sender, EventArgs e)
    {
        if (dgvHolidays.SelectedRows.Count == 0) return;
        var id = Convert.ToInt32(dgvHolidays.SelectedRows[0].Cells["Id"].Value);
        var holiday = db.Holidays.Find(id);
        if (holiday == null) return;
        var dialog = new HolidayDialog(db, holiday);
        if (dialog.ShowDialog() == DialogResult.OK) { LoadData(); RunScheduler(); }
    }

    private void DeleteHoliday(object? sender, EventArgs e)
    {
        if (dgvHolidays.SelectedRows.Count == 0) return;
        var id = Convert.ToInt32(dgvHolidays.SelectedRows[0].Cells["Id"].Value);
        if (MessageBox.Show($"Delete?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            var holiday = db.Holidays.Find(id);
            if (holiday != null) { db.Holidays.Remove(holiday); db.SaveChanges(); LoadData(); RunScheduler(); }
        }
    }

    private void ExportToCsv(object? sender, EventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "CSV|*.csv", DefaultExt = "csv" };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var output = scheduler.GetOutputPlan();
                using var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8);
                writer.WriteLine("Num,Task ID,Service,Dev,Start,Finish,Days,Est,Strict,Status,Risk");
                foreach (var item in output)
                {
                    var name = item["service_name"]?.ToString()?.Replace(",", ";") ?? "";
                    writer.WriteLine($"{item["num"]},{item["task_id"]},{name},{item["assigned_dev"]},{item["planned_start"]},{item["planned_finish"]},{item["duration"]},{item["dev_estimation"]},{item["strict_date"]},{item["status"]},{item["delivery_risk"]}");
                }
                MessageBox.Show("Exported!", "Done");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }
    }
    #endregion
}
