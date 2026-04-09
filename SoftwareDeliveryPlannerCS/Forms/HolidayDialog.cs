using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Forms;

public class HolidayDialog : Form
{
    private PlannerDbContext db;
    private Holiday? holiday;

    private TextBox txtName = null!;
    private DateTimePicker dtpDate = null!;
    private ComboBox cboType = null!;
    private TextBox txtNotes = null!;

    public HolidayDialog(PlannerDbContext db, Holiday? holiday = null)
    {
        this.db = db;
        this.holiday = holiday;
        InitializeComponent();

        if (holiday != null)
        {
            txtName.Text = holiday.HolidayName;
            dtpDate.Value = holiday.HolidayDate;
            cboType.SelectedItem = holiday.HolidayType;
            txtNotes.Text = holiday.Notes ?? "";
        }
    }

    private void InitializeComponent()
    {
        this.Text = holiday == null ? "Add Holiday" : "Edit Holiday";
        this.Size = new Size(400, 300);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 2,
            RowCount = 5
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        table.Controls.Add(new Label { Text = "Holiday Name:" }, 0, 0);
        table.Controls.Add(txtName = new TextBox { Dock = DockStyle.Fill }, 1, 0);

        table.Controls.Add(new Label { Text = "Date:" }, 0, 1);
        table.Controls.Add(dtpDate = new DateTimePicker { Format = DateTimePickerFormat.Short }, 1, 1);

        table.Controls.Add(new Label { Text = "Type:" }, 0, 2);
        table.Controls.Add(cboType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList }, 1, 2);
        cboType.Items.AddRange(new[] { "National", "Religious", "Other" });
        cboType.SelectedIndex = 0;

        table.Controls.Add(new Label { Text = "Notes:" }, 0, 3);
        table.Controls.Add(txtNotes = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 }, 1, 3);

        var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft };
        var btnSave = new Button { Text = "Save", Width = 80 };
        btnSave.Click += (s, e) => Save();
        var btnCancel = new Button { Text = "Cancel", Width = 80 };
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnSave);

        table.Controls.Add(btnPanel, 0, 4);
        table.SetColumnSpan(btnPanel, 2);

        this.Controls.Add(table);
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Holiday name is required");
            return;
        }

        if (holiday == null)
        {
            holiday = new Holiday();
            db.Holidays.Add(holiday);
        }

        holiday.HolidayName = txtName.Text;
        holiday.HolidayDate = dtpDate.Value.Date;
        holiday.HolidayType = cboType.SelectedItem?.ToString() ?? "National";
        holiday.Notes = txtNotes.Text;

        db.SaveChanges();
        DialogResult = DialogResult.OK;
        Close();
    }
}
