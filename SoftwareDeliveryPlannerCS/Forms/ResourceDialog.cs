using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Forms;

public class ResourceDialog : Form
{
    private PlannerDbContext db;
    private TeamMember? resource;

    private TextBox txtResourceId = null!;
    private TextBox txtResourceName = null!;
    private ComboBox cboRole = null!;
    private ComboBox cboTeam = null!;
    private NumericUpDown numAvailability = null!;
    private NumericUpDown numCapacity = null!;
    private DateTimePicker dtpStartDate = null!;
    private DateTimePicker dtpEndDate = null!;
    private ComboBox cboActive = null!;
    private TextBox txtNotes = null!;

    public ResourceDialog(PlannerDbContext db, TeamMember? resource = null)
    {
        this.db = db;
        this.resource = resource;

        InitializeComponent();

        if (resource != null)
        {
            txtResourceId.Text = resource.ResourceId;
            txtResourceId.Enabled = false;
            txtResourceName.Text = resource.ResourceName;
            cboRole.SelectedItem = resource.Role;
            cboTeam.SelectedItem = resource.Team;
            numAvailability.Value = (decimal)resource.AvailabilityPct;
            numCapacity.Value = (decimal)resource.DailyCapacity;
            dtpStartDate.Value = resource.StartDate;
            if (resource.EndDate.HasValue)
            {
                dtpEndDate.Checked = true;
                dtpEndDate.Value = resource.EndDate.Value;
            }
            cboActive.SelectedItem = resource.Active;
            txtNotes.Text = resource.Notes ?? "";
        }
    }

    private void InitializeComponent()
    {
        this.Text = resource == null ? "Add Resource" : "Edit Resource";
        this.Size = new Size(450, 500);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 2,
            RowCount = 11
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        table.Controls.Add(new Label { Text = "Resource ID:" }, 0, 0);
        table.Controls.Add(txtResourceId = new TextBox { Dock = DockStyle.Fill }, 1, 0);

        table.Controls.Add(new Label { Text = "Resource Name:" }, 0, 1);
        table.Controls.Add(txtResourceName = new TextBox { Dock = DockStyle.Fill }, 1, 1);

        table.Controls.Add(new Label { Text = "Role:" }, 0, 2);
        table.Controls.Add(cboRole = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList }, 1, 2);
        cboRole.Items.AddRange(new[] { "Developer", "Designer", "QA", "Architect", "Manager" });
        cboRole.SelectedIndex = 0;

        table.Controls.Add(new Label { Text = "Team:" }, 0, 3);
        table.Controls.Add(cboTeam = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList }, 1, 3);
        cboTeam.Items.AddRange(new[] { "Delivery", "Development", "QA", "DevOps" });
        cboTeam.SelectedIndex = 0;

        table.Controls.Add(new Label { Text = "Availability %:" }, 0, 4);
        table.Controls.Add(numAvailability = new NumericUpDown { Minimum = 0, Maximum = 100, Value = 100 }, 1, 4);

        table.Controls.Add(new Label { Text = "Daily Capacity:" }, 0, 5);
        table.Controls.Add(numCapacity = new NumericUpDown { Minimum = 0.1m, Maximum = 2.0m, Increment = 0.1m, DecimalPlaces = 1, Value = 1.0m }, 1, 5);

        table.Controls.Add(new Label { Text = "Start Date:" }, 0, 6);
        table.Controls.Add(dtpStartDate = new DateTimePicker { Format = DateTimePickerFormat.Short }, 1, 6);

        table.Controls.Add(new Label { Text = "End Date:" }, 0, 7);
        table.Controls.Add(dtpEndDate = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false }, 1, 7);

        table.Controls.Add(new Label { Text = "Active:" }, 0, 8);
        table.Controls.Add(cboActive = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList }, 1, 8);
        cboActive.Items.AddRange(new[] { "Yes", "No" });
        cboActive.SelectedIndex = 0;

        table.Controls.Add(new Label { Text = "Notes:" }, 0, 9);
        table.Controls.Add(txtNotes = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 }, 1, 9);

        var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft };
        var btnSave = new Button { Text = "Save", Width = 80 };
        btnSave.Click += (s, e) => Save();
        var btnCancel = new Button { Text = "Cancel", Width = 80 };
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnSave);

        table.Controls.Add(btnPanel, 0, 10);
        table.SetColumnSpan(btnPanel, 2);

        this.Controls.Add(table);
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(txtResourceId.Text))
        {
            MessageBox.Show("Resource ID is required");
            return;
        }
        if (string.IsNullOrWhiteSpace(txtResourceName.Text))
        {
            MessageBox.Show("Resource name is required");
            return;
        }

        if (resource == null)
        {
            resource = new TeamMember { ResourceId = txtResourceId.Text };
            db.Resources.Add(resource);
        }

        resource.ResourceName = txtResourceName.Text;
        resource.Role = cboRole.SelectedItem?.ToString() ?? "Developer";
        resource.Team = cboTeam.SelectedItem?.ToString() ?? "Delivery";
        resource.AvailabilityPct = (double)numAvailability.Value;
        resource.DailyCapacity = (double)numCapacity.Value;
        resource.StartDate = dtpStartDate.Value.Date;
        resource.EndDate = dtpEndDate.Checked ? dtpEndDate.Value.Date : null;
        resource.Active = cboActive.SelectedItem?.ToString() ?? "Yes";
        resource.Notes = txtNotes.Text;

        db.SaveChanges();
        DialogResult = DialogResult.OK;
        Close();
    }
}
