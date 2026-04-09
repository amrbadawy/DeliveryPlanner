using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Forms;

public class AdjustmentDialog : Form
{
    private PlannerDbContext db;

    private ComboBox cboResource = null!;
    private DateTimePicker dtpStart = null!;
    private DateTimePicker dtpEnd = null!;
    private NumericUpDown numAvailability = null!;
    private ComboBox cboType = null!;
    private TextBox txtNotes = null!;

    public AdjustmentDialog(PlannerDbContext db)
    {
        this.db = db;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Add Adjustment";
        this.Size = new Size(400, 350);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 2,
            RowCount = 7
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        table.Controls.Add(new Label { Text = "Resource:" }, 0, 0);
        table.Controls.Add(cboResource = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList }, 1, 0);
        foreach (var r in db.Resources) cboResource.Items.Add(r.ResourceName);
        if (cboResource.Items.Count > 0) cboResource.SelectedIndex = 0;

        table.Controls.Add(new Label { Text = "Start Date:" }, 0, 1);
        table.Controls.Add(dtpStart = new DateTimePicker { Format = DateTimePickerFormat.Short }, 1, 1);

        table.Controls.Add(new Label { Text = "End Date:" }, 0, 2);
        table.Controls.Add(dtpEnd = new DateTimePicker { Format = DateTimePickerFormat.Short }, 1, 2);

        table.Controls.Add(new Label { Text = "Availability %:" }, 0, 3);
        table.Controls.Add(numAvailability = new NumericUpDown { Minimum = 0, Maximum = 100, Value = 0 }, 1, 3);

        table.Controls.Add(new Label { Text = "Type:" }, 0, 4);
        table.Controls.Add(cboType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList }, 1, 4);
        cboType.Items.AddRange(new[] { "Vacation", "Training", "Support", "Reduced", "Other" });
        cboType.SelectedIndex = 0;

        table.Controls.Add(new Label { Text = "Notes:" }, 0, 5);
        table.Controls.Add(txtNotes = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 }, 1, 5);

        var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft };
        var btnSave = new Button { Text = "Save", Width = 80 };
        btnSave.Click += (s, e) => Save();
        var btnCancel = new Button { Text = "Cancel", Width = 80 };
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnSave);

        table.Controls.Add(btnPanel, 0, 6);
        table.SetColumnSpan(btnPanel, 2);

        this.Controls.Add(table);
    }

    private void Save()
    {
        if (cboResource.SelectedIndex < 0)
        {
            MessageBox.Show("Please select a resource");
            return;
        }

        var resourceName = cboResource.SelectedItem?.ToString();
        var resource = db.Resources.FirstOrDefault(r => r.ResourceName == resourceName);
        if (resource == null) return;

        var adjustment = new Adjustment
        {
            ResourceId = resource.ResourceId,
            AdjStart = dtpStart.Value.Date,
            AdjEnd = dtpEnd.Value.Date,
            AvailabilityPct = (double)numAvailability.Value,
            AdjType = cboType.SelectedItem?.ToString() ?? "Other",
            Notes = txtNotes.Text
        };

        db.Adjustments.Add(adjustment);
        db.SaveChanges();
        DialogResult = DialogResult.OK;
        Close();
    }
}
