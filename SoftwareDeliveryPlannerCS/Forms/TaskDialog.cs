using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Forms;

public class TaskDialog : Form
{
    private PlannerDbContext db;
    private TaskItem? task;

    private TextBox txtServiceName = null!;
    private NumericUpDown numEstimation = null!;
    private NumericUpDown numMaxDev = null!;
    private DateTimePicker dtpStrictDate = null!;
    private NumericUpDown numPriority = null!;
    private DateTimePicker dtpOverrideStart = null!;
    private NumericUpDown numOverrideDev = null!;
    private TextBox txtComments = null!;

    public TaskDialog(PlannerDbContext db, TaskItem? task = null)
    {
        this.db = db;
        this.task = task;

        InitializeComponent();

        if (task != null)
        {
            txtServiceName.Text = task.ServiceName;
            numEstimation.Value = (decimal)task.DevEstimation;
            numMaxDev.Value = (decimal)task.MaxDev;
            if (task.StrictDate.HasValue)
                dtpStrictDate.Value = task.StrictDate.Value;
            numPriority.Value = task.Priority;
            if (task.OverrideStart.HasValue)
                dtpOverrideStart.Value = task.OverrideStart.Value;
            if (task.OverrideDev.HasValue)
                numOverrideDev.Value = (decimal)task.OverrideDev.Value;
            txtComments.Text = task.Comments ?? "";
        }
    }

    private void InitializeComponent()
    {
        this.Text = task == null ? "Add Service" : "Edit Service";
        this.Size = new Size(500, 450);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 2,
            RowCount = 10
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        table.Controls.Add(new Label { Text = "Service Name:" }, 0, 0);
        table.Controls.Add(txtServiceName = new TextBox { Dock = DockStyle.Fill }, 1, 0);

        table.Controls.Add(new Label { Text = "DEV Estimation (Days):" }, 0, 1);
        table.Controls.Add(numEstimation = new NumericUpDown { Minimum = 0.5m, Maximum = 9999, Increment = 0.5m, DecimalPlaces = 1, Value = 1 }, 1, 1);

        table.Controls.Add(new Label { Text = "Max # Of Dev:" }, 0, 2);
        table.Controls.Add(numMaxDev = new NumericUpDown { Minimum = 0.5m, Maximum = 10, Increment = 0.5m, DecimalPlaces = 1, Value = 1 }, 1, 2);

        table.Controls.Add(new Label { Text = "Strict Date:" }, 0, 3);
        table.Controls.Add(dtpStrictDate = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false }, 1, 3);

        table.Controls.Add(new Label { Text = "Priority (1-10):" }, 0, 4);
        table.Controls.Add(numPriority = new NumericUpDown { Minimum = 1, Maximum = 10, Value = 5 }, 1, 4);

        table.Controls.Add(new Label { Text = "Override Start:" }, 0, 5);
        table.Controls.Add(dtpOverrideStart = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false }, 1, 5);

        table.Controls.Add(new Label { Text = "Override Dev:" }, 0, 6);
        table.Controls.Add(numOverrideDev = new NumericUpDown { Minimum = 0, Maximum = 10, Increment = 0.5m, DecimalPlaces = 1 }, 1, 6);

        table.Controls.Add(new Label { Text = "Comments:" }, 0, 7);
        table.Controls.Add(txtComments = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80 }, 1, 7);

        var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var btnSave = new Button { Text = "Save", Width = 80 };
        btnSave.Click += (s, e) => Save();
        var btnCancel = new Button { Text = "Cancel", Width = 80 };
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnSave);

        table.Controls.Add(btnPanel, 0, 9);
        table.SetColumnSpan(btnPanel, 2);

        this.Controls.Add(table);
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(txtServiceName.Text))
        {
            MessageBox.Show("Service name is required");
            return;
        }

        if (task == null)
        {
            // Generate new task ID
            var maxId = db.Tasks.Any() ? db.Tasks.Max(t => int.Parse(t.TaskId.Replace("SVC-", ""))) : 0;
            task = new TaskItem { TaskId = $"SVC-{maxId + 1:D3}" };
            db.Tasks.Add(task);
        }

        task.ServiceName = txtServiceName.Text;
        task.DevEstimation = (double)numEstimation.Value;
        task.MaxDev = (double)numMaxDev.Value;
        task.StrictDate = dtpStrictDate.Checked ? dtpStrictDate.Value.Date : null;
        task.Priority = (int)numPriority.Value;
        task.OverrideStart = dtpOverrideStart.Checked ? dtpOverrideStart.Value.Date : null;
        task.OverrideDev = numOverrideDev.Value > 0 ? (double)numOverrideDev.Value : null;
        task.Comments = txtComments.Text;
        task.UpdatedAt = DateTime.Now;

        db.SaveChanges();
        DialogResult = DialogResult.OK;
        Close();
    }
}
